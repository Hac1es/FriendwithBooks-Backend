using FriendwithBooksBackend.Interfaces;
using FriendwithBooksBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
                    b.AvgRating
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
        public async Task<IActionResult> CreateProduct([FromBody] Book book)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Implementation depends on actual repository methods
                // This is a placeholder for the actual implementation
                // await _bookRepository.AddBook(book);
                
                // Clear relevant cache entries
                _cache.Remove("BestSellerData");
                
                return CreatedAtAction(nameof(GetProductById), new { id = book.BookID }, book);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        // PUT: api/admin/products/
        [HttpPut("products/{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] Book book)
        {
            if (id != book.BookID)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Implementation depends on actual repository methods
                // This is a placeholder for the actual implementation
                // await _bookRepository.UpdateBook(book);
                
                // Clear relevant cache entries
                _cache.Remove("BestSellerData");
                if (book.Discount > 0)
                    _cache.Remove("FlashSaleData");
                
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        // DELETE: api/admin/products/
        [HttpDelete("products/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var book = await _bookRepository.GetBooks()
                .Where(b => b.BookID == id)
                .FirstOrDefaultAsync();

            if (book == null)
            {
                return NotFound(new { message = "Book not found" });
            }

            try
            {
                // Implementation depends on actual repository methods
                // This is a placeholder for the actual implementation
                // await _bookRepository.DeleteBook(id);
                
                // Clear relevant cache entries
                _cache.Remove("BestSellerData");
                _cache.Remove("FlashSaleData");
                
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }
        
        // POST: api/admin/products/flash-sale
        [HttpPost("products/flash-sale")]
        public async Task<IActionResult> AddFlashSale([FromBody] FlashSale flashSale)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Implementation depends on actual repository methods
                // This is a placeholder for the actual implementation
                // await _bookRepository.AddFlashSale(flashSale);
                
                // Clear cache
                _cache.Remove("FlashSaleData");
                
                return Ok(new { message = "Flash sale created successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }
        
        // DELETE: api/admin/products/flash-sale/
        [HttpDelete("products/flash-sale/{id}")]
        public async Task<IActionResult> RemoveFlashSale(int id)
        {
            try
            {
                // Implementation depends on actual repository methods
                // This is a placeholder for the actual implementation
                // await _bookRepository.RemoveFlashSale(id);
                
                // Clear cache
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

        // GET: api/admin/orders/5
        [HttpGet("orders/{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var order = await _orderRepository.GetOrders()
                .Where(o => o.OrderID == id)
                .Select(o => new
                {
                    o.OrderID,
                    o.UserID,
                    Customer = new { o.User.FullName, o.User.Email, o.User.Phone },
                    o.OrderDate,
                    o.TotalAmount,
                    o.Status,
                    PaymentMethod = o.PaymentMethod.MethodName,
                    Items = o.OrderDetails.Select(od => new
                    {
                        od.BookID,
                        BookTitle = od.Book.Title,
                        od.Quantity,
                        od.UnitPrice,
                        Subtotal = od.Quantity * od.UnitPrice
                    })
                })
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            return Ok(order);
        }

        // PUT: api/admin/orders/5
        [HttpPut("orders/{id}")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] OrderStatusUpdate update)
        {
            var order = await _orderRepository.GetOrders()
                .Where(o => o.OrderID == id)
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            try
            {
                order.Status = update.Status;
                // Implementation depends on actual repository methods
                // This is a placeholder for the actual implementation
                // await _orderRepository.UpdateOrder(order);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
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
        
        #endregion
    }

    // Helper classes for request bodies
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
}