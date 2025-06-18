using FriendwithBooksBackend.Interfaces;
using FriendwithBooksBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;

namespace FriendwithBooksBackend.Controllers
{
    [Route("api/admin")]
    [ApiController]
    // In a real application, add authentication middleware and authorize attribute
    [Authorize(Roles = "admin")]
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
                // Get DataContext to perform the actual deletion
                var context = HttpContext.RequestServices.GetService(typeof(FriendwithBooksBackend.Data.DataContext)) as FriendwithBooksBackend.Data.DataContext;
                if (context == null)
                    return StatusCode(500, new { message = "Database context not found." });

                // Check if book exists in orders
                bool hasOrders = await context.OrderDetails.AnyAsync(od => od.BookID == id);
                if (hasOrders)
                    return BadRequest(new { message = "Cannot delete book because it exists in completed orders" });

                // Remove related flash sales first to maintain referential integrity
                var flashSales = await context.FlashSales.Where(fs => fs.BookID == id).ToListAsync();
                if (flashSales.Any())
                {
                    context.FlashSales.RemoveRange(flashSales);
                }

                // Remove from carts
                var cartItems = await context.Carts.Where(c => c.BookID == id).ToListAsync();
                if (cartItems.Any())
                {
                    context.Carts.RemoveRange(cartItems);
                }

                // Remove reviews
                var reviews = await context.Reviews.Where(r => r.BookID == id).ToListAsync();
                if (reviews.Any())
                {
                    context.Reviews.RemoveRange(reviews);
                }

                // Finally remove the book
                context.Books.Remove(book);
                await context.SaveChangesAsync();

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
                existingBook.PublishYear = DateTime.SpecifyKind(new DateTime(request.PublishYear, 1, 1), DateTimeKind.Utc); existingBook.Language = request.Language ?? "Tiếng Việt";
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
        //POST /api/admin/products/bulk-delete
        [HttpPost("products/bulk-delete")]
        public async Task<IActionResult> BulkDeleteProducts([FromBody] List<int> productIds)
        {
            if (productIds == null || !productIds.Any())
                return BadRequest(new { message = "No product IDs provided." });

            try
            {
                var context = HttpContext.RequestServices.GetService(typeof(FriendwithBooksBackend.Data.DataContext)) as FriendwithBooksBackend.Data.DataContext;
                if (context == null)
                    return StatusCode(500, new { message = "Database context not found." });

                var books = await context.Books.Where(b => productIds.Contains(b.BookID)).ToListAsync();

                if (!books.Any())
                    return NotFound(new { message = "No matching books found." });

                // Check if any books exist in completed orders
                var blockedBookIds = await context.OrderDetails
                    .Where(od => productIds.Contains(od.BookID))
                    .Select(od => od.BookID)
                    .Distinct()
                    .ToListAsync();

                var deletableBooks = books.Where(b => !blockedBookIds.Contains(b.BookID)).ToList();
                var blockedBooks = books.Where(b => blockedBookIds.Contains(b.BookID)).ToList();

                if (deletableBooks.Any())
                {
                    // Remove related data
                    var bookIdsToDelete = deletableBooks.Select(b => b.BookID).ToList();

                    var flashSales = await context.FlashSales.Where(fs => bookIdsToDelete.Contains(fs.BookID)).ToListAsync();
                    context.FlashSales.RemoveRange(flashSales);

                    var cartItems = await context.Carts.Where(c => bookIdsToDelete.Contains(c.BookID)).ToListAsync();
                    context.Carts.RemoveRange(cartItems);

                    var reviews = await context.Reviews.Where(r => bookIdsToDelete.Contains(r.BookID)).ToListAsync();
                    context.Reviews.RemoveRange(reviews);

                    context.Books.RemoveRange(deletableBooks);
                    await context.SaveChangesAsync();

                    _cache.Remove("BestSellerData");
                    _cache.Remove("FlashSaleData");
                }

                return Ok(new
                {
                    message = $"Deleted {deletableBooks.Count} products.",
                    blocked = blockedBooks.Select(b => new { b.BookID, b.Title })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error.", error = ex.Message });
            }
        }



        #endregion
        #region Enhanced Order Management

        // GET: api/admin/orders/{id}
        [HttpGet("orders/{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            try
            {
                var context = HttpContext.RequestServices.GetService(typeof(FriendwithBooksBackend.Data.DataContext)) as FriendwithBooksBackend.Data.DataContext;
                if (context == null)
                    return StatusCode(500, new { message = "Database context not found." });

                var order = await context.Orders
                    .Include(o => o.User) // UserID, FullName, Email, Phone, Address
                    .Include(o => o.OrderDetails) // OrderDetailID, BookID, Quantity, UnitPrice
                        .ThenInclude(od => od.Book) // Title, Author, ImgURL1, Price
                    .Include(o => o.PaymentMethod) // PaymentMethodID, MethodName
                    .Where(o => o.OrderID == id)
                    .Select(o => new
                    {
                        // Đảm bảo tên thuộc tính JSON trả về khớp với OrderAdmin.vue
                        orderId = o.OrderID,
                        userId = o.UserID,
                        orderDate = o.OrderDate,
                        totalAmount = o.TotalAmount,
                        status = o.Status,
                        paymentMethodId = o.PaymentMethodID,
                        customer = o.User == null ? null : new
                        {
                            userId = o.User.UserID,
                            fullName = o.User.FullName, // Vue có thể dùng customerName từ danh sách, nhưng chi tiết thì nên là fullName
                            email = o.User.Email,
                            phone = o.User.Phone,
                            address = o.User.Address
                        },
                        paymentMethodName = o.PaymentMethod == null ? null : o.PaymentMethod.MethodName, // Đổi tên để rõ ràng hơn
                        orderDetails = o.OrderDetails.Select(od => new
                        {
                            orderDetailId = od.OrderDetailID,
                            bookId = od.BookID,
                            quantity = od.Quantity,
                            unitPrice = od.UnitPrice,
                            book = od.Book == null ? null : new
                            {
                                title = od.Book.Title,
                                author = od.Book.Author,
                                imgUrl1 = od.Book.ImgURL1,
                                price = od.Book.Price
                            }
                        })
                    })
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    return NotFound(new { message = "Order not found" });
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                // Ghi log chi tiết lỗi để debug
                Console.WriteLine(ex.ToString());
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}", details = ex.InnerException?.Message });
            }
        }

        // PUT: api/admin/orders/{id}
        [HttpPut("orders/{id}")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] UpdateOrderRequest request)
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

                var context = HttpContext.RequestServices.GetService(typeof(FriendwithBooksBackend.Data.DataContext)) as FriendwithBooksBackend.Data.DataContext;
                if (context == null)
                    return StatusCode(500, new { message = "Database context not found." });

                var order = await context.Orders
                    .Include(o => o.OrderDetails)
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.OrderID == id);

                if (order == null)
                    return NotFound(new { message = "Order not found" });

                var oldStatus = order.Status;
                order.Status = request.Status;

                if (request.TotalAmount.HasValue && request.TotalAmount >= 0)
                {
                    order.TotalAmount = request.TotalAmount.Value;
                }

                if (request.PaymentMethodID.HasValue)
                {
                    order.PaymentMethodID = request.PaymentMethodID.Value;
                }

                if (request.CustomerInfo != null && order.User != null)
                {
                    if (!string.IsNullOrEmpty(request.CustomerInfo.FullName))
                        order.User.FullName = request.CustomerInfo.FullName;

                    if (!string.IsNullOrEmpty(request.CustomerInfo.Email))
                        order.User.Email = request.CustomerInfo.Email;

                    if (!string.IsNullOrEmpty(request.CustomerInfo.Phone))
                        order.User.Phone = request.CustomerInfo.Phone;

                    if (!string.IsNullOrEmpty(request.CustomerInfo.Address))
                        order.User.Address = request.CustomerInfo.Address;
                }

                if (request.OrderDetails != null) // Cho phép danh sách rỗng để xóa tất cả chi tiết
                {
                    context.OrderDetails.RemoveRange(order.OrderDetails);
                    // await context.SaveChangesAsync(); // Cân nhắc lưu thay đổi ở đây hoặc cuối cùng

                    if (request.OrderDetails.Any())
                    {
                        var newOrderDetails = new List<OrderDetail>();
                        foreach (var detailRequest in request.OrderDetails)
                        {
                            var book = await context.Books.FindAsync(detailRequest.BookID);
                            if (book == null)
                            {
                                return BadRequest(new { message = $"Book with ID {detailRequest.BookID} not found." });
                            }
                            var orderDetail = new OrderDetail
                            {
                                OrderID = order.OrderID,
                                BookID = detailRequest.BookID,
                                Quantity = detailRequest.Quantity,
                                UnitPrice = detailRequest.UnitPrice
                            };
                            newOrderDetails.Add(orderDetail);
                        }
                        order.OrderDetails = newOrderDetails;
                        // Tính toán lại tổng tiền nếu chi tiết đơn hàng thay đổi và không có TotalAmount được cung cấp tường minh
                        if (!request.TotalAmount.HasValue)
                        {
                            order.TotalAmount = newOrderDetails.Sum(od => od.Quantity * od.UnitPrice);
                        }
                    }
                    else
                    {
                        // Nếu OrderDetails là danh sách rỗng, đảm bảo totalAmount cũng được cập nhật nếu không được cung cấp
                        if (!request.TotalAmount.HasValue)
                        {
                            order.TotalAmount = 0;
                        }
                    }
                }


                await context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Order updated successfully",
                    orderId = order.OrderID,
                    oldStatus = oldStatus,
                    newStatus = order.Status
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}", details = ex.InnerException?.Message });
            }
        }

        // PUT: api/admin/orders/{id}/status
        [HttpPut("orders/{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] OrderStatusUpdateRequest request)
        {
            try
            {
                if (!ModelState.IsValid) // Thêm kiểm tra ModelState
                {
                    return BadRequest(ModelState);
                }

                var context = HttpContext.RequestServices.GetService(typeof(FriendwithBooksBackend.Data.DataContext)) as FriendwithBooksBackend.Data.DataContext;
                if (context == null)
                    return StatusCode(500, new { message = "Database context not found." });

                var order = await context.Orders.FindAsync(id);
                if (order == null)
                    return NotFound(new { message = "Order not found" });

                var oldStatus = order.Status;
                order.Status = request.Status;

                await context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Order status updated successfully",
                    orderId = order.OrderID,
                    oldStatus = oldStatus,
                    newStatus = order.Status
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}", details = ex.InnerException?.Message });
            }
        }

        // DELETE: api/admin/orders/{id}
        [HttpDelete("orders/{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            try
            {
                var context = HttpContext.RequestServices.GetService(typeof(FriendwithBooksBackend.Data.DataContext)) as FriendwithBooksBackend.Data.DataContext;
                if (context == null)
                    return StatusCode(500, new { message = "Database context not found." });

                var order = await context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderID == id);

                if (order == null)
                    return NotFound(new { message = "Order not found" });

                // Kiểm tra trạng thái một cách chặt chẽ hơn (không phân biệt chữ hoa chữ thường và kiểm tra null)
                if (order.Status != null &&
                    !order.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase) &&
                    !order.Status.Equals("pending", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = "Chỉ có thể xóa đơn hàng ở trạng thái 'Chờ xử lý' (pending) hoặc 'Đã hủy' (cancelled)." });
                }

                if (order.OrderDetails != null && order.OrderDetails.Any())
                {
                    context.OrderDetails.RemoveRange(order.OrderDetails);
                }

                context.Orders.Remove(order);
                await context.SaveChangesAsync();

                return Ok(new { message = "Order deleted successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}", details = ex.InnerException?.Message });
            }
        }

        // GET: api/admin/orders/search
        [HttpGet("orders/search")]
        public async Task<IActionResult> SearchOrders([FromQuery] OrderSearchRequest request)
        {
            try
            {
                var context = HttpContext.RequestServices.GetService(typeof(FriendwithBooksBackend.Data.DataContext)) as FriendwithBooksBackend.Data.DataContext;
                if (context == null)
                    return StatusCode(500, new { message = "Database context not found." });

                var query = context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderDetails)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    var searchTermLower = request.SearchTerm.ToLower();
                    query = query.Where(o =>
                        o.OrderID.ToString().Contains(searchTermLower) ||
                        (o.User != null && o.User.FullName != null && o.User.FullName.ToLower().Contains(searchTermLower)) ||
                        (o.User != null && o.User.Email != null && o.User.Email.ToLower().Contains(searchTermLower)) ||
                        (o.User != null && o.User.Phone != null && o.User.Phone.Contains(searchTermLower))
                    );
                }

                if (!string.IsNullOrEmpty(request.Status) && !request.Status.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(o => o.Status != null && o.Status.ToLower() == request.Status.ToLower());
                }

                if (request.DateFrom.HasValue)
                {
                    query = query.Where(o => o.OrderDate >= request.DateFrom.Value.ToUniversalTime());
                }

                if (request.DateTo.HasValue)
                {
                    var dateTo = request.DateTo.Value.ToUniversalTime().Date.AddDays(1);
                    query = query.Where(o => o.OrderDate < dateTo);
                }

                if (request.MinAmount.HasValue)
                {
                    query = query.Where(o => o.TotalAmount >= request.MinAmount.Value);
                }

                if (request.MaxAmount.HasValue)
                {
                    query = query.Where(o => o.TotalAmount <= request.MaxAmount.Value);
                }

                request.Page = request.Page < 1 ? 1 : request.Page;
                request.PageSize = (request.PageSize < 1 || request.PageSize > 100) ? 20 : request.PageSize;


                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)request.PageSize);

                var orders = await query
                    .OrderByDescending(o => o.OrderDate)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(o => new
                    {
                        orderId = o.OrderID,
                        customerName = o.User == null ? "N/A" : o.User.FullName,
                        customerEmail = o.User == null ? "N/A" : o.User.Email,
                        orderDate = o.OrderDate,
                        totalAmount = o.TotalAmount,
                        paymentMethodId = o.PaymentMethodID,
                        status = o.Status,
                        userId = o.UserID,
                        itemCount = o.OrderDetails == null ? 0 : o.OrderDetails.Sum(od => od.Quantity)
                    })
                    .ToListAsync();

                return Ok(new
                {
                    currentPage = request.Page,
                    pageSize = request.PageSize,
                    totalItems,
                    totalPages,
                    items = orders
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}", details = ex.ToString() });
            }
        }

        // GET: api/admin/orders
        [HttpGet("orders")]
        public async Task<IActionResult> GetAllOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null)
        {
            try
            {
                page = page < 1 ? 1 : page; // Đảm bảo page và pageSize hợp lệ
                pageSize = (pageSize < 1 || pageSize > 100) ? 20 : pageSize;


                var context = HttpContext.RequestServices.GetService(typeof(FriendwithBooksBackend.Data.DataContext)) as FriendwithBooksBackend.Data.DataContext;
                if (context == null)
                    return StatusCode(500, new { message = "Database context not found." });

                var query = context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderDetails)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(o => o.Status != null && o.Status.ToLower() == status.ToLower());
                }

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var orders = await query
                    .OrderByDescending(o => o.OrderDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(o => new
                    {
                        orderId = o.OrderID,
                        customerName = o.User == null ? "N/A" : o.User.FullName,
                        customerEmail = o.User == null ? "N/A" : o.User.Email,
                        orderDate = o.OrderDate,
                        totalAmount = o.TotalAmount,
                        paymentMethodId = o.PaymentMethodID,
                        status = o.Status,
                        userId = o.UserID,
                        itemCount = o.OrderDetails == null ? 0 : o.OrderDetails.Sum(od => od.Quantity)
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
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}", details = ex.InnerException?.Message });
            }
        }

        // POST: api/admin/orders/bulk-update
        [HttpPost("orders/bulk-update")]
        public async Task<IActionResult> BulkUpdateOrderStatus([FromBody] BulkOrderStatusUpdateRequest request)
        {
            try
            {
                if (!ModelState.IsValid) // Thêm kiểm tra ModelState
                {
                    return BadRequest(ModelState);
                }
                // if (request.OrderIds == null || !request.OrderIds.Any()) // Đã được xử lý bởi [MinLength(1)] trong DTO
                //     return BadRequest(new { message = "Không có đơn hàng nào được chọn" });

                // if (string.IsNullOrWhiteSpace(request.Status)) // Đã được xử lý bởi [Required] trong DTO
                //     return BadRequest(new { message = "Trạng thái không được để trống" });


                var context = HttpContext.RequestServices.GetService(typeof(FriendwithBooksBackend.Data.DataContext)) as FriendwithBooksBackend.Data.DataContext;
                if (context == null)
                    return StatusCode(500, new { message = "Database context not found." });

                var ordersToUpdate = await context.Orders
                    .Where(o => request.OrderIds.Contains(o.OrderID))
                    .ToListAsync();

                if (!ordersToUpdate.Any())
                    return NotFound(new { message = "Không tìm thấy đơn hàng nào với các ID đã cho." });

                int updatedCount = 0;
                foreach (var order in ordersToUpdate)
                {
                    order.Status = request.Status;
                    updatedCount++;
                    // Ghi chú (request.Note) có thể được lưu vào một trường riêng nếu có trong model Order
                }

                await context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Đã cập nhật trạng thái của {updatedCount} đơn hàng thành '{request.Status}'.",
                    updatedCount = updatedCount
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}", details = ex.InnerException?.Message });
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

        // GET: api/admin/statistics/revenue
        [HttpGet("statistics/revenue")]
        public async Task<IActionResult> GetRevenueByTime([FromQuery] string? startTime, [FromQuery] string? endTime)
        {
            try
            {
                DateTime startDate = string.IsNullOrEmpty(startTime) ? DateTime.MinValue : DateTime.Parse(startTime).ToUniversalTime();
                DateTime endDate = string.IsNullOrEmpty(endTime) ? DateTime.MaxValue : DateTime.Parse(endTime).ToUniversalTime();
                var duration = endDate - startDate;

                DateTime prevStart = startDate - duration;
                DateTime prevEnd = startDate;

                var ordersQuery = _orderRepository.GetOrders().Where(o => o.Status == "delivered" && o.TotalAmount > 0);

                var currentOrders = await ordersQuery
                    .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                    .ToListAsync();

                var previousOrders = await ordersQuery
                    .Where(o => o.OrderDate >= prevStart && o.OrderDate < prevEnd)
                    .ToListAsync();

                decimal totalRevenueCurrent = currentOrders.Sum(o => o.TotalAmount);
                int orderCountCurrent = currentOrders.Count;
                decimal avgOrderValueCurrent = orderCountCurrent > 0 ? totalRevenueCurrent / orderCountCurrent : 0;

                decimal totalRevenuePrevious = previousOrders.Sum(o => o.TotalAmount);
                int orderCountPrevious = previousOrders.Count;
                decimal avgOrderValuePrevious = orderCountPrevious > 0 ? totalRevenuePrevious / orderCountPrevious : 0;

                decimal revenuePercent = totalRevenuePrevious > 0 ? ((totalRevenueCurrent - totalRevenuePrevious) / totalRevenuePrevious) * 100 : 0;
                decimal orderPercent = orderCountPrevious > 0 ? ((orderCountCurrent - orderCountPrevious) / orderCountPrevious) * 100 : 0;
                decimal avgOrderPercent = avgOrderValuePrevious > 0 ? ((avgOrderValueCurrent - avgOrderValuePrevious) / avgOrderValuePrevious) * 100 : 0;

                return Ok(new
                {
                    TotalRevenue = totalRevenueCurrent,
                    OrderCount = orderCountCurrent,
                    AverageOrderValue = avgOrderValueCurrent,
                    revenuePercent,
                    orderPercent,
                    avgOrderPercent,
                });
            }
            catch (FormatException)
            {
                return BadRequest(new { message = "Invalid date format. Use ISO 8601 format (e.g., 2023-10-01T00:00:00Z)." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        // GET: api/admin/statistics/chartpoints
        [HttpGet("statistics/chartpoints")]
        public async Task<IActionResult> GetChartPoints([FromQuery]string? period, [FromQuery] string? startTime, [FromQuery] string? endTime)
        {
            try
            {
                DateTime now = DateTime.UtcNow;
                DateTime? startDate = string.IsNullOrEmpty(startTime) ? null : DateTime.Parse(startTime).ToUniversalTime();
                DateTime? endDate = string.IsNullOrEmpty(endTime) ? null : DateTime.Parse(endTime).ToUniversalTime();

                int intervalCount;
                string intervalType; // "day", "month", etc.

                switch (period?.ToLower())
                {
                    case "week":
                        intervalCount = 7;
                        intervalType = "day";
                        startDate = now.AddDays(-6).Date;
                        endDate = now.Date;
                        break;
                    case "year":
                        intervalCount = 12;
                        intervalType = "month";
                        startDate = now.AddYears(-1).Date;
                        endDate = now.Date;
                        break;
                    case "month":
                    default:
                        intervalCount = 30;
                        intervalType = "day";
                        startDate = now.AddDays(-29).Date;
                        endDate = now.Date;
                        break;
                }

                // Nếu user chọn custom thời gian
                if (!string.IsNullOrEmpty(startTime) && !string.IsNullOrEmpty(endTime) && string.IsNullOrEmpty(period))
                {
                    TimeSpan totalRange = endDate.Value - startDate.Value;
                    intervalCount = totalRange.TotalDays > 7 ? 7 : (int)Math.Ceiling(totalRange.TotalDays) + 1;
                    intervalType = "custom";
                }

                if (intervalCount <= 0 || (endDate.Value - startDate.Value).TotalDays <= 0)
                {
                    return Ok(new List<object>());
                }
                double daysPerInterval = (endDate.Value - startDate.Value).TotalDays / intervalCount;

                var orders = await _orderRepository.GetOrders()
                    .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate && o.Status == "delivered")
                    .Select(o => new
                    {
                        o.OrderDate,
                        o.TotalAmount,
                    }).ToListAsync();

                var chartPoints = new List<object>();

                for (int i = 0; i < intervalCount; i++)
                {
                    DateTime intervalStart, intervalEnd;

                    if (intervalType == "month")
                    {
                        intervalStart = startDate.Value.AddMonths(i);
                        intervalEnd = intervalStart.AddMonths(1).AddTicks(-1);
                    }
                    else if (intervalType == "day" || intervalType == "custom")
                    {
                        intervalStart = startDate.Value.AddDays(i * daysPerInterval);
                        intervalEnd = startDate.Value.AddDays((i + 1) * daysPerInterval).AddTicks(-1);
                    }
                    else
                    {
                        intervalStart = startDate.Value;
                        intervalEnd = endDate.Value;
                    }

                    var intervalOrders = orders.Where(o => o.OrderDate >= intervalStart && o.OrderDate <= intervalEnd);
                    chartPoints.Add(new
                    {
                        Label = intervalType == "month" ? intervalStart.ToString("yyyy-MM") : intervalStart.ToString("yyyy-MM-dd"),
                        Revenue = intervalOrders.Sum(o => o.TotalAmount),
                    });
                }

                return Ok(chartPoints);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        // GET: api/admin/statistics/top5books
        [HttpGet("statistics/top5books")]
        public async Task<IActionResult> GetTop5Books([FromQuery] string? startTime, [FromQuery] string? endTime)
        {
            try
            {
                DateTime startDate = string.IsNullOrEmpty(startTime) ? DateTime.MinValue : DateTime.Parse(startTime).ToUniversalTime();
                DateTime endDate = string.IsNullOrEmpty(endTime) ? DateTime.MaxValue : DateTime.Parse(endTime).ToUniversalTime();
                var topBooks = await _orderRepository.GetOrders()
                    .Where(o => o.Status != "cancelled" && o.OrderDate >= startDate && o.OrderDate <= endDate)
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
                    .Take(5)
                    .ToListAsync();
                return Ok(topBooks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        // GET: api/admin/statistics/top5categories
        [HttpGet("statistics/top5categories")]
        public async Task<IActionResult> GetTop5Categories([FromQuery] string? startTime, [FromQuery] string? endTime)
        {
            try
            {
                DateTime startDate = string.IsNullOrEmpty(startTime) ? DateTime.MinValue : DateTime.Parse(startTime).ToUniversalTime();
                DateTime endDate = string.IsNullOrEmpty(endTime) ? DateTime.MaxValue : DateTime.Parse(endTime).ToUniversalTime();
                // 1. Tổng doanh thu tất cả các loại (không bị hủy)
                var totalRevenueAllCategories = await _orderRepository.GetOrders()
                    .Where(o => o.Status != "cancelled" && o.OrderDate >= startDate && o.OrderDate <= endDate)
                    .SelectMany(o => o.OrderDetails)
                    .SumAsync(od => od.Quantity * od.UnitPrice);

                // 2. Truy vấn Top 5 loại sách
                var topCategories = await _orderRepository.GetOrders()
                    .Where(o => o.Status != "cancelled" && o.OrderDate >= startDate && o.OrderDate <= endDate)
                    .SelectMany(o => o.OrderDetails)
                    .GroupBy(od => od.Book.CategoryID)
                    .Select(g => new
                    {
                        CategoryID = g.Key,
                        CategoryName = g.First().Book.Category.CategoryName,
                        TotalSold = g.Sum(od => od.Quantity),
                        TotalRevenue = g.Sum(od => od.Quantity * od.UnitPrice)
                    })
                    .OrderByDescending(x => x.TotalSold)
                    .Take(5)
                    .ToListAsync();

                // 3. Tính % doanh thu sau khi đã lấy dữ liệu
                var resultWithPercentage = topCategories.Select(c => new
                {
                    c.CategoryID,
                    c.CategoryName,
                    c.TotalSold,
                    c.TotalRevenue,
                    RevenuePercent = totalRevenueAllCategories == 0
                        ? 0
                        : Math.Round((double)(c.TotalRevenue / totalRevenueAllCategories) * 100, 2)
                });

                return Ok(resultWithPercentage);
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

        // GET: api/admin/statistics/latestorders
        [HttpGet("statistics/latestorders")]
        public async Task<IActionResult> GetLatestOrders()
        {
            try
            {
                var latestOrders = await _orderRepository.GetOrders()
                    .Include(o => o.User)
                    .Include(o => o.OrderDetails)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(10)
                    .Select(o => new
                    {
                        o.OrderID,
                        o.OrderDate,
                        o.TotalAmount,
                        o.Status,
                        CustomerName = o.User.FullName,
                        CustomerEmail = o.User.Email,
                        ItemCount = o.OrderDetails.Sum(od => od.Quantity),
                        OrderDetails = o.OrderDetails.Select(od => new
                        {
                            od.BookID,
                            BookTitle = od.Book.Title,
                            od.Quantity,
                            od.UnitPrice
                        })
                    })
                    .ToListAsync();

                return Ok(latestOrders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        // GET: api/admin/statistics/order-completion-rate
        [HttpGet("statistics/order-completion-rate")]
        public async Task<IActionResult> GetOrderCompletionRate([FromQuery] string? startTime, [FromQuery] string? endTime)
        {
            try
            {
                DateTime startDate = string.IsNullOrEmpty(startTime) ? DateTime.MinValue : DateTime.Parse(startTime).ToUniversalTime();
                DateTime endDate = string.IsNullOrEmpty(endTime) ? DateTime.MaxValue : DateTime.Parse(endTime).ToUniversalTime();

                var ordersInPeriod = await _orderRepository.GetOrders()
                    .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                    .ToListAsync();

                var totalOrders = ordersInPeriod.Count;
                var completedOrders = ordersInPeriod.Count(o => o.Status == "delivered" || o.Status == "Paid");

                double percent = totalOrders > 0 ? (double)completedOrders / totalOrders * 100 : 0;

                return Ok(new
                {
                    TotalOrders = totalOrders,
                    CompletedOrders = completedOrders,
                    CompletionRate = Math.Round(percent, 2),
                });
            }
            catch (FormatException)
            {
                return BadRequest(new { message = "Invalid date format. Use ISO 8601 format (e.g., 2023-10-01T00:00:00Z)." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        // GET: api/admin/statistics/top10customers
        [HttpGet("statistics/top10customers")]
        public async Task<IActionResult> GetTop10Customers([FromQuery] string? startTime, [FromQuery] string? endTime)
        {
            try
            {
                DateTime startDate = string.IsNullOrEmpty(startTime) ? DateTime.MinValue : DateTime.Parse(startTime).ToUniversalTime();
                DateTime endDate = string.IsNullOrEmpty(endTime) ? DateTime.MaxValue : DateTime.Parse(endTime).ToUniversalTime();

                var topCustomers = await _orderRepository.GetOrders()
                    .Where(o => o.Status == "delivered" && o.OrderDate >= startDate && o.OrderDate <= endDate)
                    .GroupBy(o => o.UserID)
                    .Select(g => new
                    {
                        UserID = g.Key,
                        CustomerName = g.First().User.FullName,
                        CustomerEmail = g.First().User.Email,
                        CustomerPhone = g.First().User.Phone,
                        TotalOrders = g.Count(),
                        TotalSpent = g.Sum(o => o.TotalAmount),
                        AverageOrderValue = g.Average(o => o.TotalAmount),
                        LastOrderDate = g.Max(o => o.OrderDate),
                        TotalItems = g.SelectMany(o => o.OrderDetails).Sum(od => od.Quantity)
                    })
                    .OrderByDescending(x => x.TotalSpent)
                    .Take(10)
                    .ToListAsync();

                return Ok(new
                {
                    TopCustomers = topCustomers,
                    Period = new
                    {
                        StartDate = startDate,
                        EndDate = endDate
                    }
                });
            }
            catch (FormatException)
            {
                return BadRequest(new { message = "Invalid date format. Use ISO 8601 format (e.g., 2023-10-01T00:00:00Z)." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        #endregion

        #region Flash Sale handler

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
        [Range(1, 99, ErrorMessage = "Phần trăm giảm giá phải từ 1% đến 99%")]
        public int DiscountPercent { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }
    }

    public class UpdateFlashSaleDto
    {
        [Required]
        [Range(1, 99, ErrorMessage = "Phần trăm giảm giá phải từ 1% đến 99%")]
        public int DiscountPercent { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }
    }
    // Helper classes for request bodies
    public class BestsellerPromotionRequest
    {
        public bool ApplyDiscount { get; set; }
        public int DiscountPercent { get; set; }
        public bool UpdateStock { get; set; }
        public int NewStock { get; set; }
        public bool UpdatePrice { get; set; }
        public decimal NewPrice { get; set; }
    }

    public class UpdateOrderRequest
    {
        [Required(ErrorMessage = "Status is required.")]
        public string Status { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Total amount cannot be negative.")]
        public decimal? TotalAmount { get; set; }
        public int? PaymentMethodID { get; set; } // Giả sử PaymentMethodID là int

        public CustomerUpdateInfo? CustomerInfo { get; set; }
        public List<OrderDetailUpdateRequest>? OrderDetails { get; set; }
    }

    public class CustomerUpdateInfo
    {
        public string? FullName { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string? Email { get; set; }

        // [Phone(ErrorMessage = "Invalid phone number format.")] // Thuộc tính Phone có thể cần một thư viện regex phức tạp hơn cho nhiều định dạng
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }

    public class OrderDetailUpdateRequest
    {
        [Required]
        public int BookID { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }

        [Required]
        [Range(0.00, double.MaxValue, ErrorMessage = "Unit price must be non-negative.")] // Cho phép giá 0.00
        public decimal UnitPrice { get; set; }
    }

    public class OrderStatusUpdateRequest // Dùng cho cập nhật trạng thái đơn giản
    {
        [Required(ErrorMessage = "Status is required.")]
        public string Status { get; set; }

        public string? Note { get; set; }
    }

    public class OrderSearchRequest
    {
        public string? SearchTerm { get; set; } = ""; // Cho phép null hoặc rỗng
        public string? Status { get; set; } = "all"; // Mặc định là "all"
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class BulkOrderStatusUpdateRequest
    {
        [Required(ErrorMessage = "Order IDs are required.")]
        [MinLength(1, ErrorMessage = "At least one Order ID must be provided.")]
        public List<int> OrderIds { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        public string Status { get; set; }

        public string? Note { get; set; }
    }
}
