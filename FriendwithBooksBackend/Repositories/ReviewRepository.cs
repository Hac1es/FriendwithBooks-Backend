using FriendwithBooksBackend.Data;
using FriendwithBooksBackend.Interfaces;
using FriendwithBooksBackend.Models;

namespace FriendwithBooksBackend.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly DataContext _context;
        public ReviewRepository(DataContext context)
        {
            _context = context;
        }
        public IQueryable<Review> GetReviews()
        {
            return _context.Reviews.AsQueryable();
        }

        public async Task AddReviewAsync(Review review)
        {
            if (review == null)
            {
                throw new ArgumentNullException(nameof(review), "Review cannot be null");
            }
            await _context.Reviews.AddAsync(review);
            await _context.SaveChangesAsync();
        }
    }
}
