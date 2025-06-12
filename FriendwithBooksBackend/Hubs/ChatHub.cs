using FriendwithBooksBackend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using FriendwithBooksBackend.Services;

namespace FriendwithBooksBackend.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;

        public ChatHub(IChatService chatService)
        {
            _chatService = chatService;
        }

        public async Task SendMessage(string message, int recvID)
        {
            try
            {
                var userId = int.Parse(Context.User?.FindFirst("userId")?.Value ?? "0");
                if (userId == 0) throw new UnauthorizedAccessException();
                await _chatService.SendMessageAsync(userId, recvID, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendMessage Exception: " + ex.Message);
                Console.WriteLine("StackTrace: " + ex.StackTrace);
                if (ex.InnerException != null)
                    Console.WriteLine("Inner: " + ex.InnerException.Message);
                throw;
            }
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst("userId")?.Value ?? Context.ConnectionId;
            if (Context.User?.Claims?.Any() == true)
            {
                Console.WriteLine("Token authenticated successfully.");
            }
            else
            {
                Console.WriteLine("No claims found. Token invalid or not parsed.");
            }
            Console.WriteLine($"User connected: {userId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst("userId")?.Value ?? Context.ConnectionId;
            Console.WriteLine($"User disconnected: {userId}");
            if (exception != null)
            {
                Console.WriteLine("Exception: " + exception.Message);
                if (exception.InnerException != null)
                    Console.WriteLine("Inner: " + exception.InnerException.Message);
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
