using FriendwithBooksBackend.Models;

namespace FriendwithBooksBackend.Interfaces
{
    public interface IReviewRepository
    {
        IQueryable<Review> GetReviews();
        Task AddReviewAsync(Review review);
    }
}
