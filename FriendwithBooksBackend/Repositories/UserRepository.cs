using FriendwithBooksBackend.Data;
using FriendwithBooksBackend.Interfaces;
using FriendwithBooksBackend.Models;

namespace FriendwithBooksBackend.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly DataContext _context;
        public UserRepository(DataContext context)
        {
            _context = context;
        }
        public IQueryable<User> GetUsers()
        {
            return _context.Users.AsQueryable();
        }
    }
}
