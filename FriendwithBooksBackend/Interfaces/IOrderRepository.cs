using FriendwithBooksBackend.Models;
using System.Linq;

namespace FriendwithBooksBackend.Interfaces
{
    public interface IOrderRepository
    {
        IQueryable<Order> GetOrders();
        // Add additional methods as needed:
        // Task<Order> GetOrderByIdAsync(int id);
        // Task AddOrderAsync(Order order);
        // Task UpdateOrderAsync(Order order);
        // Task DeleteOrderAsync(int id);
    }
}