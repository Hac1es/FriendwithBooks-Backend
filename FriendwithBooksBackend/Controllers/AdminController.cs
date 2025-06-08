using FriendwithBooksBackend.Interfaces;
using FriendwithBooksBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace FriendwithBooksBackend.Controllers
{
    [Route("api/admin")]
    [ApiController]
    // In a real application, add authentication middleware and authorize attribute
    // [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IBookRepository _bookRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMemoryCache _cache;

        public AdminController(
            IBookRepository bookRepository,
            ICategoryRepository categoryRepository,
            IOrderRepository orderRepository,
            IUserRepository userRepository,
            IMemoryCache cache)
        {
            _bookRepository = bookRepository;
            _categoryRepository = categoryRepository;
            _orderRepository = orderRepository;
            _userRepository = userRepository;
            _cache = cache;
        }

        #region Product Management

        // GET: api/admin/products
        [HttpGet("products")]
        public async Task<IActionResult> GetAllProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var query = _bookRepository.GetBooks();
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
                    b.Stock,
                    b.CategoryID,
                    b.Discount,
                    b.ImgURL1,
                    b.ImgURL2,
                    b.ImgURL3,
                    b.AvgRating,
                    b.Description,
                    b.Supplier,
                    b.PublishYear,
                    b.PageNum,
                    b.Language,
                    b.Binding,
                    b.AgeGroup
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

        // GET: api/admin/products/5
        [HttpGet("products/{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var book = await _bookRepository.GetBooks()
                .Where(b => b.BookID == id)
                .FirstOrDefaultAsync();

            if (book == null)
            {
                return NotFound(new { message = "Book not found" });
            }

            return Ok(book);
        }

        // POST: api/admin/products
        [HttpPost("products")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateBookRequest request)
        {
            try
            {
                // Validate the request
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                        );

                    return BadRequest(new { message = "Validation failed", errors });
                }

                // Additional business validation
                if (request.Price <= 0)
                    return BadRequest(new { message = "Price must be greater than 0" });

                if (request.Discount < 0 || request.Discount > 100)
                    return BadRequest(new { message = "Discount must be between 0 and 100" });

                if (request.Stock < 0)
                    return BadRequest(new { message = "Stock cannot be negative" });

                // Check if book with same title exists in same category
                var exists = await _bookRepository.GetBooks()
                    .AnyAsync(b => b.Title == request.Title && b.CategoryID == request.CategoryID);
                if (exists)
                    return BadRequest(new { message = "Book with the same title already exists in this category." });

                // Create new book
                var book = new Book
                {
                    Title = request.Title,
                    Author = request.Author,
                    Description = request.Description ?? "",
                    Price = request.Price,
                    Stock = request.Stock,
                    CategoryID = request.CategoryID,
                    Discount = request.Discount,
                    ImgURL1 = request.ImgURL1 ?? "",
                    ImgURL2 = request.ImgURL2 ?? "",
                    ImgURL3 = request.ImgURL3 ?? "",
                    Supplier = request.Supplier ?? "",
                    PublishYear = DateTime.SpecifyKind(new DateTime(request.PublishYear, 1, 1), DateTimeKind.Utc),
                    PageNum = request.PageNum.ToString(),
                    Language = request.Language ?? "Tiếng Việt",
                    Binding = request.Binding ?? "Bìa mềm",
                    AgeGroup = request.AgeGroup ?? "all",
                    AvgRating = 0,
                    TotalRating = 0
                };

                var context = HttpContext.RequestServices.GetService(typeof(FriendwithBooksBackend.Data.DataContext)) as FriendwithBooksBackend.Data.DataContext;
                if (context == null)
                    return StatusCode(500, new { message = "Database context not found." });

                context.Books.Add(book);
                await context.SaveChangesAsync();

                _cache.Remove("BestSellerData");
                if (book.Discount > 0)
                    _cache.Remove("FlashSaleData");

                return CreatedAtAction(nameof(GetProductById), new { id = book.BookID }, book);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}", details = ex.InnerException?.Message });
            }
        }

        // PUT: api/admin/products/{id}
        [HttpPut("products/{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateBookRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                        );

                    return BadRequest(new { message = "Validation failed", errors });
                }

                // Additional business validation
                if (request.Price <= 0)
                    return BadRequest(new { message = "Price must be greater than 0" });

                if (request.Discount < 0 || request.Discount > 100)
                    return BadRequest(new { message = "Discount must be between 0 and 100" });

                if (request.Stock < 0)
                    return BadRequest(new { message = "Stock cannot be negative" });

                var context = HttpContext.RequestServices.GetService(typeof(FriendwithBooksBackend.Data.DataContext)) as FriendwithBooksBackend.Data.DataContext;
                if (context == null)
                    return StatusCode(500, new { message = "Database context not found." });

                var existingBook = await context.Books.FirstOrDefaultAsync(b => b.BookID == id);
                if (existingBook == null)
                    return NotFound(new { message = "Book not found" });

                // Update fields
                existingBook.Title = request.Title;
                existingBook.Author = request.Author;
                existingBook.Description = request.Description ?? "";
                existingBook.Price = request.Price;
                existingBook.Stock = request.Stock;
                existingBook.ImgURL1 = request.ImgURL1 ?? "";
                existingBook.ImgURL2 = request.ImgURL2 ?? "";
                existingBook.ImgURL3 = request.ImgURL3 ?? "";
                existingBook.AgeGroup = request.AgeGroup ?? "all";
                existingBook.CategoryID = request.CategoryID;
                existingBook.Supplier = request.Supplier ?? "";
                existingBook.PublishYear = new DateTime(request.PublishYear, 1, 1); 
                existingBook.Language = request.Language ?? "Tiếng Việt";
                existingBook.PageNum = request.PageNum.ToString();
                existingBook.Binding = request.Binding ?? "Bìa mềm";
                existingBook.Discount = request.Discount;

                await context.SaveChangesAsync();

                _cache.Remove("BestSellerData");
                if (existingBook.Discount > 0)
                    _cache.Remove("FlashSaleData");

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }


        #endregion

        #region Order Management

        // GET: api/admin/orders
        [HttpGet("orders")]
        public async Task<IActionResult> GetAllOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var query = _orderRepository.GetOrders();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new
                {
                    o.OrderID,
                    o.UserID,
                    CustomerName = o.User.FullName,
                    o.OrderDate,
                    o.TotalAmount,
                    o.Status,
                    o.PaymentMethodID
                })
                .ToListAsync();

            return Ok(new
            {
                currentPage = page,
                pageSize,
                totalItems,
                totalPages,
                items = orders
            });
        }

        #endregion

        #region User Management

        // GET: api/admin/users
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var query = _userRepository.GetUsers();
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var users = await query
                .OrderBy(u => u.UserID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.UserID,
                    u.FullName,
                    u.Email,
                    u.Phone,
                    u.RegistrationDate,
                    u.Role
                })
                .ToListAsync();

            return Ok(new
            {
                currentPage = page,
                pageSize,
                totalItems,
                totalPages,
                items = users
            });
        }

        // GET: api/admin/users/5
        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _userRepository.GetUsers()
                .Where(u => u.UserID == id)
                .Select(u => new
                {
                    u.UserID,
                    u.FullName,
                    u.Email,
                    u.Phone,
                    u.Avatar,
                    u.RegistrationDate,
                    u.Role,
                    Orders = u.Orders.Select(o => new
                    {
                        o.OrderID,
                        o.OrderDate,
                        o.TotalAmount,
                        o.Status
                    })
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(user);
        }

        // DELETE: api/admin/users/5
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _userRepository.GetUsers()
                .Where(u => u.UserID == id)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            try
            {
                // Implementation depends on actual repository methods
                // This is a placeholder for the actual implementation
                // await _userRepository.DeleteUser(id);

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        // PUT: api/admin/users/5/role
        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UserRoleUpdate update)
        {
            var user = await _userRepository.GetUsers()
                .Where(u => u.UserID == id)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            try
            {
                user.Role = update.Role;
                // Implementation depends on actual repository methods
                // This is a placeholder for the actual implementation
                // await _userRepository.UpdateUser(user);

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        #endregion

        #region Statistics

        // GET: api/admin/statistics/sales
        [HttpGet("statistics/sales")]
        public async Task<IActionResult> GetSalesStatistics([FromQuery] string period = "month")
        {
            try
            {
                DateTime startDate;
                DateTime endDate = DateTime.Now;

                switch (period.ToLower())
                {
                    case "day":
                        startDate = DateTime.Now.AddDays(-1);
                        break;
                    case "week":
                        startDate = DateTime.Now.AddDays(-7);
                        break;
                    case "year":
                        startDate = DateTime.Now.AddYears(-1);
                        break;
                    case "month":
                    default:
                        startDate = DateTime.Now.AddMonths(-1);
                        break;
                }

                var orders = await _orderRepository.GetOrders()
                    .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate && o.Status == "Completed")
                    .ToListAsync();

                var dailySales = orders
                    .GroupBy(o => o.OrderDate.Date)
                    .Select(g => new
                    {
                        Date = g.Key.ToString("yyyy-MM-dd"),
                        Revenue = g.Sum(o => o.TotalAmount),
                        OrderCount = g.Count()
                    })
                    .OrderBy(x => x.Date)
                    .ToList();

                var totalRevenue = orders.Sum(o => o.TotalAmount);
                var orderCount = orders.Count;
                var avgOrderValue = orderCount > 0 ? totalRevenue / orderCount : 0;

                return Ok(new
                {
                    Period = period,
                    TotalRevenue = totalRevenue,
                    OrderCount = orderCount,
                    AverageOrderValue = avgOrderValue,
                    DailySales = dailySales
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        // GET: api/admin/statistics/products
        [HttpGet("statistics/products")]
        public async Task<IActionResult> GetProductStatistics()
        {
            try
            {
                // Get best sellers (top 10)
                var bestSellers = await _orderRepository.GetOrders()
                    .Where(o => o.Status == "Completed")
                    .SelectMany(o => o.OrderDetails)
                    .GroupBy(od => od.BookID)
                    .Select(g => new
                    {
                        BookID = g.Key,
                        Title = g.First().Book.Title,
                        TotalSold = g.Sum(od => od.Quantity),
                        TotalRevenue = g.Sum(od => od.Quantity * od.UnitPrice)
                    })
                    .OrderByDescending(x => x.TotalSold)
                    .Take(10)
                    .ToListAsync();

                // Get low stock products (stock < 10)
                var lowStock = await _bookRepository.GetBooks()
                    .Where(b => b.Stock < 10)
                    .Select(b => new
                    {
                        b.BookID,
                        b.Title,
                        b.Stock
                    })
                    .ToListAsync();

                // Get product counts by category
                var productsByCategory = await _bookRepository.GetBooks()
                    .GroupBy(b => b.CategoryID)
                    .Select(g => new
                    {
                        CategoryID = g.Key,
                        CategoryName = g.First().Category.CategoryName,
                        ProductCount = g.Count()
                    })
                    .OrderByDescending(x => x.ProductCount)
                    .ToListAsync();

                return Ok(new
                {
                    BestSellers = bestSellers,
                    LowStockProducts = lowStock,
                    ProductsByCategory = productsByCategory,
                    TotalProducts = await _bookRepository.GetBooks().CountAsync()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        // GET: api/admin/statistics/users
        [HttpGet("statistics/users")]
        public async Task<IActionResult> GetUserStatistics([FromQuery] string period = "month")
        {
            try
            {
                DateTime startDate;
                DateTime endDate = DateTime.Now;

                switch (period.ToLower())
                {
                    case "day":
                        startDate = DateTime.Now.AddDays(-1);
                        break;
                    case "week":
                        startDate = DateTime.Now.AddDays(-7);
                        break;
                    case "year":
                        startDate = DateTime.Now.AddYears(-1);
                        break;
                    case "month":
                    default:
                        startDate = DateTime.Now.AddMonths(-1);
                        break;
                }

                // New users in selected period
                var newUsers = await _userRepository.GetUsers()
                    .Where(u => u.RegistrationDate >= startDate && u.RegistrationDate <= endDate)
                    .CountAsync();

                // Total users
                var totalUsers = await _userRepository.GetUsers().CountAsync();

                // Active users (users with orders in the period)
                var activeUsers = await _orderRepository.GetOrders()
                    .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                    .Select(o => o.UserID)
                    .Distinct()
                    .CountAsync();

                // Top customers by order value
                var topCustomers = await _orderRepository.GetOrders()
                    .Where(o => o.Status == "Completed")
                    .GroupBy(o => o.UserID)
                    .Select(g => new
                    {
                        UserID = g.Key,
                        CustomerName = g.First().User.FullName,
                        Email = g.First().User.Email,
                        TotalOrders = g.Count(),
                        TotalSpent = g.Sum(o => o.TotalAmount)
                    })
                    .OrderByDescending(x => x.TotalSpent)
                    .Take(10)
                    .ToListAsync();

                return Ok(new
                {
                    Period = period,
                    NewUsers = newUsers,
                    TotalUsers = totalUsers,
                    ActiveUsers = activeUsers,
                    TopCustomers = topCustomers
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        #endregion

        #region Chat Management

        // Models for chat functionality would need to be created

        // GET: api/admin/chat/conversations
        [HttpGet("chat/conversations")]
        public async Task<IActionResult> GetAllConversations([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            // This would require a chat repository and model implementation
            // Returning a placeholder response
            return Ok(new
            {
                message = "Chat functionality requires additional implementation"
            });
        }

        // GET: api/admin/chat/conversations/5
        [HttpGet("chat/conversations/{userId}")]
        public async Task<IActionResult> GetConversationWithUser(int userId)
        {
            // This would require a chat repository and model implementation
            // Returning a placeholder response
            return Ok(new
            {
                userId = userId,
                message = "Conversation retrieval requires additional implementation"
            });
        }

        // POST: api/admin/chat/message
        [HttpPost("chat/message")]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageRequest message)
        {
            // This would require a chat repository and model implementation
            // Returning a placeholder response
            return Ok(new
            {
                message = "Message sent successfully (placeholder)"
            });
        }

        // PUT: api/admin/chat/read
        [HttpPut("chat/read")]
        public async Task<IActionResult> MarkMessagesAsRead([FromBody] MarkMessagesReadRequest request)
        {
            // This would require a chat repository and model implementation
            // Returning a placeholder response
            return NoContent();
        }
        // GET: api/admin/flash-sale
        [HttpGet("flash-sale")]
        public async Task<IActionResult> GetAllFlashSales()
        {
            try
            {
                var context = HttpContext.RequestServices.GetService(typeof(FriendwithBooksBackend.Data.DataContext)) as FriendwithBooksBackend.Data.DataContext;
                if (context == null)
                    return StatusCode(500, new { message = "Database context not found." });

                var flashSales = await context.FlashSales
                    .Include(fs => fs.Book)
                    .Select(fs => new
                    {
                        fs.FlashSaleID,
                        fs.BookID,
                        fs.DiscountPercent,
                        fs.StartTime,
                        fs.EndTime,
                        BookTitle = fs.Book.Title,
                        BookPrice = fs.Book.Price,
                        BookImgURL = fs.Book.ImgURL1
                    })
                    .OrderByDescending(fs => fs.StartTime)
                    .ToListAsync();

                return Ok(flashSales);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Có lỗi xảy ra khi tải danh sách Flash Sale", error = ex.Message });
            }
        }

        // GET: api/admin/flash-sale/book/{bookId}
        [HttpGet("flash-sale/book/{bookId}")]
        public async Task<IActionResult> GetFlashSalesByBookId(int bookId)
        {
            try
            {
                var context = HttpContext.RequestServices.GetService(typeof(FriendwithBooksBackend.Data.DataContext)) as FriendwithBooksBackend.Data.DataContext;
                if (context == null)
                    return StatusCode(500, new { message = "Database context not found." });

                var flashSales = await context.FlashSales
                    .Where(fs => fs.BookID == bookId)
                    .Select(fs => new
                    {
                        fs.FlashSaleID,
                        fs.BookID,
                        fs.DiscountPercent,
                        fs.StartTime,
                        fs.EndTime
                    })
                    .OrderByDescending(fs => fs.StartTime)
                    .ToListAsync();

                return Ok(flashSales);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Có lỗi xảy ra khi tải Flash Sale của sách", error = ex.Message });
            }
        }

        // POST: api/admin/flash-sale
        [HttpPost("flash-sale")]
        public async Task<IActionResult> CreateFlashSale([FromBody] CreateFlashSaleDto dto)
        {
            try
            {
                // Validate input
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => new { Field = x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage) })
                        .ToList();

                    return BadRequest(new { message = "Dữ liệu không hợp lệ", errors = errors });
                }

                var context = HttpContext.RequestServices.GetService(typeof(FriendwithBooksBackend.Data.DataContext)) as FriendwithBooksBackend.Data.DataContext;
                if (context == null)
                    return StatusCode(500, new { message = "Database context not found." });

                // Additional validation
                if (dto.BookID <= 0)
                {
                    return BadRequest(new { message = "ID sách không hợp lệ" });
                }

                // Check if start time is not in the past (allow some buffer for timezone differences)
                var now = DateTime.UtcNow.AddMinutes(-5); // 5 minute buffer
                if (dto.StartTime < now)
                {
                    return BadRequest(new { message = "Thời gian bắt đầu không thể trong quá khứ" });
                }

                // Check if book exists
                var bookExists = await context.Books.AnyAsync(b => b.BookID == dto.BookID);
                if (!bookExists)
                    return NotFound(new { message = "Không tìm thấy sách với ID này" });

                if (dto.StartTime >= dto.EndTime)
                    return BadRequest(new { message = "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc" });

                // Check for overlapping flash sales for the same book with improved overlap detection
                var hasOverlap = await context.FlashSales
                    .Where(fs => fs.BookID == dto.BookID)
                    .AnyAsync(fs =>
                        (dto.StartTime >= fs.StartTime && dto.StartTime < fs.EndTime) ||
                        (dto.EndTime > fs.StartTime && dto.EndTime <= fs.EndTime) ||
                        (dto.StartTime <= fs.StartTime && dto.EndTime >= fs.EndTime)
                    );

                if (hasOverlap)
                    return BadRequest(new { message = "Đã có Flash Sale khác trong khoảng thời gian này cho sách này" });

                // Create flash sale entity
                var flashSale = new FlashSale
                {
                    BookID = dto.BookID,
                    DiscountPercent = dto.DiscountPercent,
                    StartTime = dto.StartTime.ToUniversalTime(), // Ensure UTC
                    EndTime = dto.EndTime.ToUniversalTime()      // Ensure UTC
                };

                // Add to database
                context.FlashSales.Add(flashSale);
                var result = await context.SaveChangesAsync();

                // Clear cache
                _cache.Remove("FlashSaleData");

                // Return detailed response
                return Ok(new
                {
                    message = "Tạo Flash Sale thành công",
                    flashSaleId = flashSale.FlashSaleID,
                    data = new
                    {
                        flashSale.FlashSaleID,
                        flashSale.BookID,
                        flashSale.DiscountPercent,
                        flashSale.StartTime,
                        flashSale.EndTime
                    }
                });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi cơ sở dữ liệu khi tạo Flash Sale",
                    error = dbEx.InnerException?.Message ?? dbEx.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Có lỗi không mong muốn xảy ra khi tạo Flash Sale",
                    error = ex.Message
                });
            }
        }

        // PUT: api/admin/flash-sale/{id}
        [HttpPut("flash-sale/{id}")]
        public async Task<IActionResult> UpdateFlashSale(int id, [FromBody] UpdateFlashSaleDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var context = HttpContext.RequestServices.GetService(typeof(FriendwithBooksBackend.Data.DataContext)) as FriendwithBooksBackend.Data.DataContext;
                if (context == null)
                    return StatusCode(500, new { message = "Database context not found." });

                var flashSale = await context.FlashSales.FindAsync(id);
                if (flashSale == null)
                    return NotFound(new { message = "Không tìm thấy Flash Sale" });

                if (dto.StartTime >= dto.EndTime)
                    return BadRequest(new { message = "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc" });

                var hasOverlap = await context.FlashSales
                    .Where(fs => fs.BookID == flashSale.BookID && fs.FlashSaleID != id)
                    .AnyAsync(fs =>
                        (dto.StartTime >= fs.StartTime && dto.StartTime < fs.EndTime) ||
                        (dto.EndTime > fs.StartTime && dto.EndTime <= fs.EndTime) ||
                        (dto.StartTime <= fs.StartTime && dto.EndTime >= fs.EndTime)
                    );

                if (hasOverlap)
                    return BadRequest(new { message = "Đã có Flash Sale khác trong khoảng thời gian này cho sách này" });

                flashSale.DiscountPercent = dto.DiscountPercent;
                flashSale.StartTime = dto.StartTime;
                flashSale.EndTime = dto.EndTime;

                await context.SaveChangesAsync();

                return Ok(new { message = "Cập nhật Flash Sale thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Có lỗi xảy ra khi cập nhật Flash Sale", error = ex.Message });
            }
        }

        // DELETE: api/admin/flash-sale/{id}
        [HttpDelete("flash-sale/{id}")]
        public async Task<IActionResult> DeleteFlashSale(int id)
        {
            try
            {
                var context = HttpContext.RequestServices.GetService(typeof(FriendwithBooksBackend.Data.DataContext)) as FriendwithBooksBackend.Data.DataContext;
                if (context == null)
                    return StatusCode(500, new { message = "Database context not found." });

                var flashSale = await context.FlashSales.FindAsync(id);
                if (flashSale == null)
                    return NotFound(new { message = "Không tìm thấy Flash Sale" });

                context.FlashSales.Remove(flashSale);
                await context.SaveChangesAsync();

                return Ok(new { message = "Xóa Flash Sale thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Có lỗi xảy ra khi xóa Flash Sale", error = ex.Message });
            }
        }

        // GET: api/admin/flash-sale/active
        [HttpGet("flash-sale/active")]
        public async Task<IActionResult> GetActiveFlashSales()
        {
            try
            {
                var context = HttpContext.RequestServices.GetService(typeof(FriendwithBooksBackend.Data.DataContext)) as FriendwithBooksBackend.Data.DataContext;
                if (context == null)
                    return StatusCode(500, new { message = "Database context not found." });

                var now = DateTime.UtcNow;
                var activeFlashSales = await context.FlashSales
                    .Include(fs => fs.Book)
                    .Where(fs => fs.StartTime <= now && fs.EndTime >= now)
                    .Select(fs => new
                    {
                        fs.FlashSaleID,
                        fs.BookID,
                        fs.DiscountPercent,
                        fs.StartTime,
                        fs.EndTime,
                        BookTitle = fs.Book.Title,
                        BookPrice = fs.Book.Price,
                        BookImgURL = fs.Book.ImgURL1
                    })
                    .ToListAsync();

                return Ok(activeFlashSales);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Có lỗi xảy ra khi tải Flash Sale đang hoạt động", error = ex.Message });
            }
        }

        #endregion
    }

    // Helper classes for request bodies
    public class CreateBookRequest
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(500, ErrorMessage = "Title cannot exceed 500 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Author is required")]
        [StringLength(200, ErrorMessage = "Author cannot exceed 200 characters")]
        public string Author { get; set; }

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative")]
        public int Stock { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public int CategoryID { get; set; }

        [Range(0, 100, ErrorMessage = "Discount must be between 0 and 100")]
        public int Discount { get; set; }

        public string? ImgURL1 { get; set; }
        public string? ImgURL2 { get; set; }
        public string? ImgURL3 { get; set; }

        [StringLength(200, ErrorMessage = "Supplier cannot exceed 200 characters")]
        public string? Supplier { get; set; }

        [Range(1000, 9999, ErrorMessage = "Publish year must be a valid year")]
        public int PublishYear { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Page number cannot be negative")]
        public int PageNum { get; set; }

        [StringLength(50, ErrorMessage = "Language cannot exceed 50 characters")]
        public string? Language { get; set; }

        [StringLength(50, ErrorMessage = "Binding cannot exceed 50 characters")]
        public string? Binding { get; set; }

        [StringLength(20, ErrorMessage = "Age group cannot exceed 20 characters")]
        public string? AgeGroup { get; set; }
    }

    public class UpdateBookRequest
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(500, ErrorMessage = "Title cannot exceed 500 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Author is required")]
        [StringLength(200, ErrorMessage = "Author cannot exceed 200 characters")]
        public string Author { get; set; }

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative")]
        public int Stock { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public int CategoryID { get; set; }

        [Range(0, 100, ErrorMessage = "Discount must be between 0 and 100")]
        public int Discount { get; set; }

        public string? ImgURL1 { get; set; }
        public string? ImgURL2 { get; set; }
        public string? ImgURL3 { get; set; }

        [StringLength(200, ErrorMessage = "Supplier cannot exceed 200 characters")]
        public string? Supplier { get; set; }

        [Range(1000, 9999, ErrorMessage = "Publish year must be a valid year")]
        public int PublishYear { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Page number cannot be negative")]
        public int PageNum { get; set; }

        [StringLength(50, ErrorMessage = "Language cannot exceed 50 characters")]
        public string? Language { get; set; }

        [StringLength(50, ErrorMessage = "Binding cannot exceed 50 characters")]
        public string? Binding { get; set; }

        [StringLength(20, ErrorMessage = "Age group cannot exceed 20 characters")]
        public string? AgeGroup { get; set; }
    }

    public class OrderStatusUpdate
    {
        public string Status { get; set; }
    }

    public class UserRoleUpdate
    {
        public string Role { get; set; }
    }

    public class ChatMessageRequest
    {
        public int UserId { get; set; }
        public string Message { get; set; }
    }

    public class MarkMessagesReadRequest
    {
        public int UserId { get; set; }
        public List<int> MessageIds { get; set; }
    }

    // DTOs for Flash Sale
    public class CreateFlashSaleDto
    {
        [Required]
        public int BookID { get; set; }

        [Required]
        [Range(1, 90, ErrorMessage = "Phần trăm giảm giá phải từ 1% đến 90%")]
        public int DiscountPercent { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }
    }

    public class UpdateFlashSaleDto
    {
        [Required]
        [Range(1, 90, ErrorMessage = "Phần trăm giảm giá phải từ 1% đến 90%")]
        public int DiscountPercent { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }
    }
}
