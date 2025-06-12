using FriendwithBooksBackend.Models;

namespace FriendwithBooksBackend.Interfaces
{
    public interface ICategoryRepository
    {
        IQueryable<Category> GetCategories();
    }
}
