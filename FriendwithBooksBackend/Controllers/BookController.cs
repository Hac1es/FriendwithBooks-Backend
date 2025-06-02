using FriendwithBooksBackend.Interfaces;
using FriendwithBooksBackend.Models;
using FriendwithBooksBackend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FriendwithBooksBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookController : ControllerBase
    {
        private readonly IBookRepository _bookRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IUserRepository _userRepository;

        public BookController(IBookRepository bookRepository, ICategoryRepository categoryRepository, IReviewRepository reviewRepository, IUserRepository userRepository)
        {
            _bookRepository = bookRepository;
            _categoryRepository = categoryRepository;
            _reviewRepository = reviewRepository;
            _userRepository = userRepository;
        }

        // GET: api/Books/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookById(int id)
        {
            var bookData = await _bookRepository.GetBooks()
                .Where(b => b.BookID == id)
                .Select(b => new
                {
                    b.BookID,
                    b.Title,
                    b.Author,
                    b.Description,
                    b.Price,
                    b.Stock,
                    b.ImgURL1,
                    b.ImgURL2,
                    b.ImgURL3,
                    b.AgeGroup,
                    b.AvgRating,
                    b.TotalRating,
                    b.CategoryID,
                    b.Supplier,
                    b.PublishYear,
                    b.Language,
                    b.PageNum,
                    b.Binding
                })
                .FirstOrDefaultAsync();

            if (bookData == null)
            {
                return NotFound(new { message = "Book not found" });
            }

            var allCategories = await _categoryRepository.GetCategories().ToListAsync();
            var categoryDict = allCategories.ToDictionary(c => c.CategoryID);
            var path = new List<object>();

            int? currentId = bookData.CategoryID;

            while (currentId != null && categoryDict.TryGetValue(currentId.Value, out var category))
            {
                path.Insert(0, new
                {
                    category.CategoryID,
                    category.CategoryName
                });

                currentId = category.ParentID;
            }

            var relatedBooks = await _bookRepository.GetBooks()
            .Where(b => b.CategoryID == bookData.CategoryID && b.BookID > bookData.BookID)
            .OrderBy(b => b.BookID)
            .Take(5)
            .Select(b => new
            {
                b.BookID,
                b.Title,
                b.Price,
                b.Discount,
                b.ImgURL1
            })
            .ToListAsync();
            return Ok(new
            {
                book = bookData,
                categoryPath = path,
                relatedBooks = relatedBooks
            });
        }

        //GET: api/Book/category
        [HttpGet("category")]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _categoryRepository.GetCategories()
                .Where(c => c.ParentID == null)
                .Select(parent => new
                {
                    Parent = parent.CategoryName ?? string.Empty,
                    SubCategories = _categoryRepository.GetCategories()
                        .Where(c => c.ParentID == parent.CategoryID)
                        .Select(sub => sub.CategoryName ?? string.Empty)
                        .ToList()
                })
                .ToDictionaryAsync(x => x.Parent, x => x.SubCategories);

            return Ok(categories);
        }

        // GET: api/Book/query?page={page}&promo={promo}&price={price}
        // &priceMin={}&priceMax={}&age={age}&type={type}
        // &category={category}&name={name}
        [HttpGet("query")]
        public async Task<IActionResult> GetBooksByQueries([FromQuery] int page = 1,
            [FromQuery] bool? promo = null,
            [FromQuery] string? price = null,
            [FromQuery] string? priceMin = null,
            [FromQuery] string? priceMax = null,
            [FromQuery] string? age = null,
            [FromQuery] string? type = null,
            [FromQuery] int? category = null,
            [FromQuery] string? name = null)
        {
            if (page < 1) page = 1;
            int pageSize = 12;

            var query = _bookRepository.GetBooks();

            // Lọc "Khuyến mãi"
            if (promo == true)
            {
                query = query.Where(b => b.Discount > 0);
            }

            // Lọc "Giá"
            if (!string.IsNullOrEmpty(price))
            {
                switch (price)
                {
                    case "lt100":
                        query = query.Where(b => b.Price < 100000);
                        break;
                    case "btw100_300":
                        query = query.Where(b => b.Price >= 100000 && b.Price <= 300000);
                        break;
                    case "gt300":
                        query = query.Where(b => b.Price > 300000);
                        break;
                }
            }
            else if (!string.IsNullOrEmpty(priceMin) && !string.IsNullOrEmpty(priceMax))
            {
                query = query.Where(b => b.Price >= decimal.Parse(priceMin) && b.Price <= decimal.Parse(priceMax));
            }

            //Lọc "Độ tuổi"
            if (!string.IsNullOrEmpty(age))
            {
                query = query.Where(b => b.AgeGroup == age); // tùy cột bên DB bạn dùng
            }

            // Lọc "Hình thức"
            if (!string.IsNullOrEmpty(type))
            {
                switch(type)
                {
                    case "soft":
                        query = query.Where(b => b.Binding == "Bìa mềm");
                        break;
                    case "hard":
                        query = query.Where(b => b.Binding == "Bìa cứng");
                        break;
                    case "leather":
                        query = query.Where(b => b.Binding == "Bìa da");
                        break;
                } 
            }

            // Lọc "Danh mục"
            if (category.HasValue)
            {
                query = query.Where(b => b.CategoryID == category.Value);
            }

            // Lọc "Tên sách"
            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(b => b.Title.ToLower().Replace(" ", "").Contains(name.Replace(" ", "").ToLower()));
            }

            // Phân trang
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var books = await query
                .OrderBy(b => b.BookID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new
                {
                    b.BookID,
                    b.Title,
                    b.Author,
                    b.Price,
                    b.ImgURL1,
                    b.Discount
                })
                .ToListAsync();

            return Ok(new
            {
                currentPage = page,
                pageSize,
                totalItems,
                totalPages,
                items = books
            });
        }
        
        // GET: api/Book/{id}/reviews
        [HttpGet("{id}/reviews")]
        public async Task<IActionResult> GetReviewsById(int id)
        {
            var reviews = await _reviewRepository.GetReviews()
                .Where(r => r.BookID == id)
                .Join(_userRepository.GetUsers(), r => r.UserID, u => u.UserID, (r, u) => new {
                    r.ReviewID,
                    r.BookID,
                    r.UserID,
                    r.Rating,
                    r.Comment,
                    r.ReviewDate,
                    u.FullName,
                })
                .ToListAsync();
            return Ok(reviews);
        }

        //PUT: api/Book/addReview
        [HttpPut("addReview")]
        public async Task<IActionResult> AddReview([FromBody] ReviewRequest reviewReq)
        {
            if (reviewReq == null || !ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid review data" });
            }
            // Validate that the book exists
            var bookExists = await _bookRepository.GetBooks().AnyAsync(b => b.BookID == reviewReq.BookID);
            if (!bookExists)
            {
                return NotFound(new { message = "Book not found" });
            }
            var review = new Review
            {
                Rating = reviewReq.Rating,
                Comment = reviewReq.Comment,
                UserID = reviewReq.UserID,
                ReviewDate = DateTime.UtcNow,
                BookID = reviewReq.BookID
            };
            // Save the review to the database
            await _reviewRepository.AddReviewAsync(review);
            return Ok(new { message = "Review added successfully" });
        }
    }
}