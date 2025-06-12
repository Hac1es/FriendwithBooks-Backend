using FriendwithBooksBackend.Data;
using FriendwithBooksBackend.Interfaces;
using FriendwithBooksBackend.Models;

namespace FriendwithBooksBackend.Repositories
{
    public class BookRepository : IBookRepository
    {
        private readonly DataContext _context;

        public BookRepository(DataContext context)
        {
            _context = context;
        }

        public IQueryable<Book> GetBooks()
        {
            return _context.Books;
        }
        public IQueryable<FlashSale> GetFlashSale()
        {
            return _context.FlashSales;
        }
    }
}
