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
                    b.Discount,
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

            // Kiểm tra FlashSale hiện tại cho sách này
            var currentTime = DateTime.UtcNow;
            var activeFlashSale = await _bookRepository.GetFlashSale()
                .Where(fs => fs.BookID == id && 
                            fs.StartTime <= currentTime && 
                            fs.EndTime >= currentTime)
                .OrderByDescending(fs => fs.DiscountPercent)
                .FirstOrDefaultAsync();

            var finalDiscount = activeFlashSale?.DiscountPercent ?? bookData.Discount;
            var isFlashSale = activeFlashSale != null;

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
                b.Author,
                b.Price,
                b.Discount,
                b.ImgURL1
            })
            .ToListAsync();

            // Áp dụng FlashSale cho related books
            var relatedBooksWithFlashSale = new List<object>();
            foreach (var book in relatedBooks)
            {
                var bookFlashSale = await _bookRepository.GetFlashSale()
                    .Where(fs => fs.BookID == book.BookID && 
                                fs.StartTime <= currentTime && 
                                fs.EndTime >= currentTime)
                    .OrderByDescending(fs => fs.DiscountPercent)
                    .FirstOrDefaultAsync();

                var bookFinalDiscount = bookFlashSale?.DiscountPercent ?? book.Discount;
                var bookIsFlashSale = bookFlashSale != null;

                relatedBooksWithFlashSale.Add(new
                {
                    book.BookID,
                    book.Title,
                    book.Author,
                    book.Price,
                    Discount = bookFinalDiscount,
                    book.ImgURL1,
                    FlashSale = bookIsFlashSale
                });
            }

            return Ok(new
            {
                book = new
                {
                    bookData.BookID,
                    bookData.Title,
                    bookData.Author,
                    bookData.Description,
                    bookData.Price,
                    bookData.Stock,
                    bookData.ImgURL1,
                    bookData.ImgURL2,
                    bookData.ImgURL3,
                    bookData.AgeGroup,
                    bookData.AvgRating,
                    bookData.TotalRating,
                    bookData.CategoryID,
                    bookData.Supplier,
                    bookData.PublishYear,
                    bookData.Language,
                    bookData.PageNum,
                    bookData.Binding,
                    Discount = finalDiscount,
                    isFlashSale
                },
                categoryPath = path,
                relatedBooks = relatedBooksWithFlashSale
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
                        .Select(sub => new
                        {
                            Name = sub.CategoryName ?? string.Empty,
                            TotalStock = _bookRepository.GetBooks()
                                .Where(b => b.CategoryID == sub.CategoryID)
                                .Sum(b => b.Stock),
                            sub.CategoryID,
                        })
                        .ToList()
                })
                .ToDictionaryAsync(x => x.Parent, x => x.SubCategories);

            return Ok(categories);
        }

        //POST: api/Book/category
        [HttpPost("category")]
        public async Task<IActionResult> PostCategory([FromBody] CreateCategoryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CategoryName))
                return BadRequest("Tên danh mục không được để trống.");

            // Kiểm tra trùng tên ở cùng cấp
            if (request.ParentName == null)
            {
                // Thêm danh mục cha, kiểm tra trùng với các cha
                var exists = await _categoryRepository.GetCategories()
                    .AnyAsync(c => c.ParentID == null && c.CategoryName.ToLower() == request.CategoryName.Trim().ToLower());
                if (exists)
                    return BadRequest("Danh mục cha đã tồn tại.");

                var newCategory = new Category
                {
                    CategoryName = request.CategoryName.Trim(),
                    ParentID = null
                };
                _categoryRepository.Add(newCategory);
                await _categoryRepository.SaveChangesAsync();
                return Ok(newCategory);
            }
            else
            {
                // Tìm parent
                var parent = await _categoryRepository.GetCategories()
                    .FirstOrDefaultAsync(c => c.ParentID == null && c.CategoryName == request.ParentName);

                // Kiểm tra trùng tên con trong cùng cha
                var exists = await _categoryRepository.GetCategories()
                    .AnyAsync(c => c.ParentID == parent.CategoryID && c.CategoryName.ToLower() == request.CategoryName.Trim().ToLower());
                if (exists)
                    return BadRequest("Danh mục con đã tồn tại trong danh mục cha này.");

                var newCategory = new Category
                {
                    CategoryName = request.CategoryName.Trim(),
                    ParentID = parent.CategoryID
                };
                _categoryRepository.Add(newCategory);
                await _categoryRepository.SaveChangesAsync();
                return Ok(newCategory);
            }
        }

        // PUT: api/Book/category/{id}
        [HttpPut("category/{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NewName))
                return BadRequest("Tên danh mục mới không được để trống.");

            var category = await _categoryRepository.GetCategories()
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
                return NotFound("Không tìm thấy danh mục.");

            // Kiểm tra xem có phải đang cố gắng chuyển một danh mục con thành cha của chính nó không
            if (request.NewParentName != null)
            {
                var newParent = await _categoryRepository.GetCategories()
                    .FirstOrDefaultAsync(c => c.ParentID == null && c.CategoryName == request.NewParentName);
                
                if (newParent == null)
                    return NotFound("Không tìm thấy danh mục cha mới.");

                // Kiểm tra vòng lặp: nếu danh mục con đang cố gắng trở thành cha của chính nó
                if (newParent.CategoryID == id)
                    return BadRequest("Không thể chuyển một danh mục thành cha của chính nó.");

                // Kiểm tra trùng tên với các danh mục con khác trong cùng danh mục cha mới
                var exists = await _categoryRepository.GetCategories()
                    .AnyAsync(c => c.ParentID == newParent.CategoryID 
                        && c.CategoryID != id 
                        && c.CategoryName.ToLower() == request.NewName.Trim().ToLower());
                if (exists)
                    return BadRequest("Đã tồn tại danh mục con khác với tên này trong danh mục cha mới.");
            }
            else
            {
                // Nếu chuyển thành danh mục cha, kiểm tra trùng tên với các danh mục cha khác
                var exists = await _categoryRepository.GetCategories()
                    .AnyAsync(c => c.ParentID == null 
                        && c.CategoryID != id 
                        && c.CategoryName.ToLower() == request.NewName.Trim().ToLower());
                if (exists)
                    return BadRequest("Đã tồn tại danh mục cha khác với tên này.");
            }

            // Cập nhật thông tin
            category.CategoryName = request.NewName.Trim();
            if (request.NewParentName != null)
            {
                var newParent = await _categoryRepository.GetCategories()
                    .FirstOrDefaultAsync(c => c.ParentID == null && c.CategoryName == request.NewParentName);
                category.ParentID = newParent.CategoryID;
            }
            else
            {
                category.ParentID = null;
            }

            await _categoryRepository.SaveChangesAsync();
            return Ok(category);
        }

        // DELETE: api/Book/category/{name}
        [HttpDelete("category/{name}")]
        public async Task<IActionResult> DeleteCategory(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Tên danh mục không được để trống.");

            var category = await _categoryRepository.GetCategories()
                .FirstOrDefaultAsync(c => c.CategoryName.ToLower() == name.Trim().ToLower());

            if (category == null)
                return NotFound("Không tìm thấy danh mục.");

            // Kiểm tra xem có danh mục con nào không
            var hasChildren = await _categoryRepository.GetCategories()
                .AnyAsync(c => c.ParentID == category.CategoryID);
            if (hasChildren)
                return BadRequest("Không thể xóa danh mục này vì nó có chứa các danh mục con.");

            // Kiểm tra xem có sách nào thuộc danh mục này không
            var hasBooks = await _bookRepository.GetBooks()
                .AnyAsync(b => b.CategoryID == category.CategoryID);
            if (hasBooks)
                return BadRequest("Không thể xóa danh mục này vì nó có chứa sách.");

            _categoryRepository.Delete(category);
            await _categoryRepository.SaveChangesAsync();
            return Ok(new { message = "Xóa danh mục thành công." });
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
            int pageSize = 20;

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

            // Áp dụng FlashSale cho tất cả sách
            var currentTime = DateTime.UtcNow;
            var booksWithFlashSale = new List<object>();
            
            foreach (var book in books)
            {
                var activeFlashSale = await _bookRepository.GetFlashSale()
                    .Where(fs => fs.BookID == book.BookID && 
                                fs.StartTime <= currentTime && 
                                fs.EndTime >= currentTime)
                    .OrderByDescending(fs => fs.DiscountPercent)
                    .FirstOrDefaultAsync();

                var finalDiscount = activeFlashSale?.DiscountPercent ?? book.Discount;
                var isFlashSale = activeFlashSale != null;

                booksWithFlashSale.Add(new
                {
                    book.BookID,
                    book.Title,
                    book.Author,
                    book.Price,
                    book.ImgURL1,
                    Discount = finalDiscount,
                    isFlashSale
                });
            }

            return Ok(new
            {
                currentPage = page,
                pageSize,
                totalItems,
                totalPages,
                items = booksWithFlashSale
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

        // GET: api/Book/admin/query?page={page}&pageSize={pageSize}&title={title}&id={id}&promo={promo}&price={price}
        // &age={age}&type={type}&categoryId={categoryId}
        [HttpGet("admin/query")]
        public async Task<IActionResult> GetBooksForAdmin([FromQuery] int page = 1,
            [FromQuery] int pageSize = 12,
            [FromQuery] string? title = null,
            [FromQuery] int? id = null,
            [FromQuery] bool? promo = null,
            [FromQuery] string? price = null,
            [FromQuery] string? age = null,
            [FromQuery] string? type = null,
            [FromQuery] int? categoryId = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 12;

            var query = _bookRepository.GetBooks();

            // Tìm kiếm theo ID
            if (id.HasValue)
            {
                query = query.Where(b => b.BookID == id.Value);
            }

            // Tìm kiếm theo tên sách
            if (!string.IsNullOrEmpty(title))
            {
                query = query.Where(b => b.Title.ToLower().Replace(" ", "").Contains(title.Replace(" ", "").ToLower()));
            }

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

            // Lọc "Độ tuổi"
            if (!string.IsNullOrEmpty(age))
            {
                query = query.Where(b => b.AgeGroup == age);
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
            if (categoryId.HasValue)
            {
                query = query.Where(b => b.CategoryID == categoryId.Value);
            }

            // Phân trang
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Trả về đầy đủ thông tin cho admin
            var books = await query
                .OrderBy(b => b.BookID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new
                {
                    b.BookID,
                    b.Title,
                    b.Author,
                    b.Description,
                    b.Price,
                    b.Discount,
                    b.Stock,
                    b.ImgURL1,
                    b.ImgURL2,
                    b.ImgURL3,
                    b.Supplier,
                    b.PublishYear,
                    b.PageNum,
                    b.Language,
                    b.AgeGroup,
                    b.CategoryID,
                    b.Binding,
                    b.AvgRating,
                    b.TotalRating
                })
                .ToListAsync();

            // Áp dụng FlashSale cho tất cả sách
            var currentTime = DateTime.UtcNow;
            var booksWithFlashSale = new List<object>();
            
            foreach (var book in books)
            {
                var activeFlashSale = await _bookRepository.GetFlashSale()
                    .Where(fs => fs.BookID == book.BookID && 
                                fs.StartTime <= currentTime && 
                                fs.EndTime >= currentTime)
                    .OrderByDescending(fs => fs.DiscountPercent)
                    .FirstOrDefaultAsync();

                var finalDiscount = activeFlashSale?.DiscountPercent ?? book.Discount;
                var isFlashSale = activeFlashSale != null;

                booksWithFlashSale.Add(new
                {
                    book.BookID,
                    book.Title,
                    book.Author,
                    book.Description,
                    book.Price,
                    book.Stock,
                    book.ImgURL1,
                    book.ImgURL2,
                    book.ImgURL3,
                    book.Supplier,
                    book.PublishYear,
                    book.PageNum,
                    book.Language,
                    book.AgeGroup,
                    book.CategoryID,
                    book.Binding,
                    book.AvgRating,
                    book.TotalRating,
                    Discount = finalDiscount,
                    isFlashSale
                });
            }

            return Ok(new
            {
                currentPage = page,
                pageSize,
                totalItems,
                totalPages,
                items = booksWithFlashSale
            });
        }
        public class CreateCategoryRequest
        {
            public string? CategoryName { get; set; }
            public string? ParentName { get; set; } // null nếu là danh mục cha
        }

        public class UpdateCategoryRequest
        {
            public string? NewName { get; set; }
            public string? NewParentName { get; set; } // null nếu muốn chuyển thành danh mục cha
        }
    }
}