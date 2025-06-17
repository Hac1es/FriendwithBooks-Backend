using FriendwithBooksBackend.Data;
using FriendwithBooksBackend.Interfaces;
using FriendwithBooksBackend.Models;

namespace FriendwithBooksBackend.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly DataContext _context;
        public CategoryRepository(DataContext context)
        {
            _context = context;
        }
        public IQueryable<Category> GetCategories()
        {
            return _context.Categories.AsQueryable();
        }
        public void Add(Category category)
        {
            if (category == null)
            {
                throw new ArgumentNullException(nameof(category), "Category cannot be null");
            }
            _context.Categories.Add(category);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public void Delete(Category category)
        {
            if (category == null)
            {
                throw new ArgumentNullException(nameof(category), "Category cannot be null");
            }
            _context.Categories.Remove(category);
        }
    }
}
