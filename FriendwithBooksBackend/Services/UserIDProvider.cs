using Microsoft.AspNetCore.SignalR;

namespace FriendwithBooksBackend.Services
{
    public class UserIDProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            // Assuming the user ID is stored in the connection's User property
            return connection.User?.FindFirst("userId")?.Value ?? connection.ConnectionId;
        }
    }
}
