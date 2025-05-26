using FriendwithBooksBackend.Models;

namespace FriendwithBooksBackend.Interfaces
{
    public interface IUserRepository
    {
        IQueryable<User> GetUsers();
    } 
}
