using FriendwithBooksBackend.Interfaces;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using FriendwithBooksBackend.Hubs;

namespace FriendwithBooksBackend.Services
{
    public interface IChatService
    {
        Task<object> SendMessageAsync(int senderId, int receiverId, string message);
        Task<IEnumerable<object>> GetConversationHistoryAsync(int userId, int partnerId, int page = 1, int pageSize = 20);
        Task<IEnumerable<object>> GetLatestMessagesAsync(int userId, int partnerId, string lastMessageId = null);
        Task<List<ChatService.ConversationPreview>> GetPartnersAsync(int userId);
        Task<bool> DeleteConversationAsync(int userId, int partnerId);
    }

    public class ChatService : IChatService
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly IUserRepository _userRepository;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatService(
            IUserRepository userRepository,
            IHubContext<ChatHub> hubContext)
        {
            _firestoreDb = FirestoreDb.Create("trotot-f1fdf");
            _userRepository = userRepository;
            _hubContext = hubContext;
        }

        private string GetConversationId(int userId1, int userId2)
        {
            return userId1 < userId2 ? $"{userId1}_{userId2}" : $"{userId2}_{userId1}";
        }

        public async Task<object> SendMessageAsync(int senderId, int receiverId, string message)
        {
            var (fullName, avatar) = await GetUserFullNameAndAvatar(senderId);
            
            if (fullName == null) throw new UnauthorizedAccessException();

            var doc = new Dictionary<string, object>
            {
                { "sender", fullName },
                { "message", message },
                { "timestamp", Timestamp.GetCurrentTimestamp() },
                { "senderId", senderId },
                { "receiverId", receiverId },
            };

            var conversationId = GetConversationId(senderId, receiverId);
            var messageRef = await _firestoreDb
                .Collection("conversations")
                .Document(conversationId)
                .Collection("messages")
                .AddAsync(doc);

            var messageObject = new
            {
                id = messageRef.Id,
                sender = fullName,
                senderAvatar = avatar,
                message = message,
                timestamp = DateTime.UtcNow,
                senderId = senderId,
                receiverId = receiverId,
                isAdmin = senderId == 152
            };

            // Send real-time message to receiver
            await _hubContext.Clients.User(receiverId.ToString())
                .SendAsync("ReceiveMessage", messageObject);

            return messageObject;
        }

        private async Task<(string FullName, string Avatar)> GetUserFullNameAndAvatar(int userId)
        {
            var user = await _userRepository.GetUsers().FirstOrDefaultAsync(u => u.UserID == userId);
            return (user?.FullName ?? "Unknown User", user?.Avatar ?? "");
        }

        public async Task<IEnumerable<object>> GetConversationHistoryAsync(int userId, int partnerId, int page = 1, int pageSize = 20)
        {
            var conversationId = GetConversationId(userId, partnerId);
            var query = _firestoreDb
                .Collection("conversations")
                .Document(conversationId)
                .Collection("messages")
                .OrderByDescending("timestamp")
                .Limit(pageSize)
                .Offset((page - 1) * pageSize);

            var messagesSnapshot = await query.GetSnapshotAsync();
            var totalCount = await _firestoreDb
                .Collection("conversations")
                .Document(conversationId)
                .Collection("messages")
                .Count()
                .GetSnapshotAsync();

            var messages = new List<object>();
            foreach (var doc in messagesSnapshot)
            {
                var senderId = doc.GetValue<int>("senderId");
                var (senderFullName, senderAvatar) = await GetUserFullNameAndAvatar(senderId);

                messages.Add(new
                {
                    id = doc.Id,
                    sender = senderFullName,
                    senderAvatar = senderAvatar,
                    message = doc.GetValue<string>("message"),
                    timestamp = doc.GetValue<Timestamp>("timestamp").ToDateTime(),
                    senderId = senderId,
                    receiverId = doc.GetValue<int>("receiverId"),
                    isAdmin = senderId == 1
                });
            }

            return messages.OrderBy(m => ((DateTime)m.GetType().GetProperty("timestamp").GetValue(m)));
        }

        public async Task<IEnumerable<object>> GetLatestMessagesAsync(int userId, int partnerId, string lastMessageId = null)
        {
            var conversationId = GetConversationId(userId, partnerId);
            var query = _firestoreDb
                .Collection("conversations")
                .Document(conversationId)
                .Collection("messages")
                .OrderBy("timestamp");

            if (!string.IsNullOrEmpty(lastMessageId))
            {
                var lastMessage = await _firestoreDb
                    .Collection("conversations")
                    .Document(conversationId)
                    .Collection("messages")
                    .Document(lastMessageId)
                    .GetSnapshotAsync();

                if (lastMessage.Exists)
                {
                    var lastTimestamp = lastMessage.GetValue<Timestamp>("timestamp");
                    query = query.WhereGreaterThan("timestamp", lastTimestamp);
                }
            }

            var messagesSnapshot = await query.GetSnapshotAsync();
            var messages = new List<object>();
            foreach (var doc in messagesSnapshot)
            {
                var senderId = doc.GetValue<int>("senderId");
                var (senderFullName, senderAvatar) = await GetUserFullNameAndAvatar(senderId);

                messages.Add(new
                {
                    id = doc.Id,
                    sender = senderFullName,
                    senderAvatar = senderAvatar,
                    message = doc.GetValue<string>("message"),
                    timestamp = doc.GetValue<Timestamp>("timestamp").ToDateTime(),
                    senderId = senderId,
                    receiverId = doc.GetValue<int>("receiverId"),
                    isAdmin = senderId == 1
                });
            }
            return messages;
        }

        public async Task<List<ConversationPreview>> GetPartnersAsync(int userId)
        {
            var conversationsRef = _firestoreDb.Collection("conversations");
            var conversationSnapshots = await conversationsRef.ListDocumentsAsync().ToListAsync();

            var previews = new List<ConversationPreview>();

            foreach (var convDoc in conversationSnapshots)
            {
                var convId = convDoc.Id;

                var idParts = convId.Split('_');
                if (!idParts.Contains(userId.ToString())) continue;

                var partnerId = int.Parse(idParts.First(id => id != userId.ToString()));

                var messagesRef = convDoc.Collection("messages")
                    .OrderByDescending("timestamp")
                    .Limit(1);

                var lastMsgSnap = await messagesRef.GetSnapshotAsync();
                var lastMessage = lastMsgSnap.FirstOrDefault();

                if (lastMessage == null) continue;

                var msgData = lastMessage.ToDictionary();
                var lastMessageSenderId = msgData.TryGetValue("senderId", out var senderIdObj) && int.TryParse(senderIdObj.ToString(), out var senderId) ? senderId : 0;

                var (partnerName, partnerAvatar) = await GetUserFullNameAndAvatar(partnerId);

                bool isAdminSendLast = (lastMessageSenderId == 1);

                previews.Add(new ConversationPreview
                {
                    ConversationId = convId,
                    PartnerId = partnerId,
                    PartnerName = partnerName,
                    PartnerAvatar = partnerAvatar,
                    LastMessage = msgData["message"]?.ToString(),
                    Timestamp = msgData["timestamp"] is Timestamp ts ? ts.ToDateTime() : DateTime.MinValue,
                    isAdminSendLast = isAdminSendLast,
                });
            }

            return previews.OrderByDescending(p => p.Timestamp).ToList();
        }

        public async Task<bool> DeleteConversationAsync(int userId, int partnerId)
        {
            var conversationId = GetConversationId(userId, partnerId);
            var convDoc = _firestoreDb.Collection("conversations").Document(conversationId);

            // Xóa tất cả messages trong conversation
            var messages = await convDoc.Collection("messages").ListDocumentsAsync().ToListAsync();
            var deleteTasks = messages.Select(msg => msg.DeleteAsync());
            await Task.WhenAll(deleteTasks);

            // Xóa document conversation (dù không có field, vẫn nên gọi)
            await convDoc.DeleteAsync();
            return true;
        }

        public class ConversationPreview
        {
            public string? ConversationId { get; set; }
            public int PartnerId { get; set; }
            public string? LastMessage { get; set; }
            public DateTime Timestamp { get; set; }
            public string? PartnerName { get; set; }
            public string? PartnerAvatar { get; set; }
            public bool isAdminSendLast { get; set; }
        }


    }
} 