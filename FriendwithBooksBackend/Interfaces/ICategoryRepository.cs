using FriendwithBooksBackend.Models;

namespace FriendwithBooksBackend.Interfaces
{
    public interface ICategoryRepository
    {
        IQueryable<Category> GetCategories();
        // Thêm mới một category
        void Add(Category category);
        // Lưu thay đổi vào database
        Task SaveChangesAsync();
        // Xóa một category
        void Delete(Category category);
    }
}
