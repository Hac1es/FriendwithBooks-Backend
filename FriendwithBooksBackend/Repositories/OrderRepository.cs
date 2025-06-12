using FriendwithBooksBackend.Data;
using FriendwithBooksBackend.Interfaces;
using FriendwithBooksBackend.Models;
using System.Linq;

namespace FriendwithBooksBackend.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly DataContext _context;

        public OrderRepository(DataContext context)
        {
            _context = context;
        }

        public IQueryable<Order> GetOrders()
        {
            return _context.Orders;
        }

        // Implement additional methods as needed
    }
}