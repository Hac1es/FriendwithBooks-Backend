using FriendwithBooksBackend.Models;

namespace FriendwithBooksBackend.Interfaces
{
    public interface IBookRepository
    {
        IQueryable<Book> GetBooks();
        IQueryable<FlashSale> GetFlashSale();
    }
}
