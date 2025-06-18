using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FriendwithBooksBackend.Data;
using FriendwithBooksBackend.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace FriendwithBooksBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly DataContext _context;
        private const decimal MIN_ORDER_AMOUNT = 10000; // 10,000 VND
        private const int ORDER_CANCELLATION_HOURS = 24; // 24 giờ

        public OrderController(DataContext context) { _context = context; }

        [HttpGet("my")]
        public async Task<ActionResult> GetMyOrders()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return BadRequest(new { success = false, message = "Không xác thực được người dùng hoặc ID người dùng không hợp lệ." });
            }
            var orders = await _context.Orders
                .Where(o => o.UserID == userId)
                .Include(o => o.PaymentMethod)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Book)
                .Include(o => o.Transaction)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var result = orders.Select(o => new
            {
                OrderID = o.OrderID,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                status = o.Status?.ToLower(),
                PaymentMethod = o.PaymentMethod?.MethodName,
                PaymentStatus = o.Transaction?.PaymentStatus,
                ItemCount = o.OrderDetails?.Sum(od => od.Quantity) ?? 0,
                CanCancel = CanCancelOrder(o),
                OrderDetails = o.OrderDetails?.Select(od => new
                {
                    OrderDetailID = od.OrderDetailID,
                    BookID = od.BookID,
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                    Book = new
                    {
                        Title = od.Book?.Title,
                        Author = od.Book?.Author,
                        ImgURL1 = od.Book?.ImgURL1
                    }
                })
            });

            return Ok(new { success = true, data = result });
        }

        [HttpGet("{orderId}")]
        public async Task<ActionResult> GetOrder(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.PaymentMethod)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Book)
                .Include(o => o.Transaction)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);

            if (order == null)
                return NotFound(new { success = false, message = "Không tìm thấy đơn hàng" });

            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { success = false, message = "Không xác thực được người dùng hoặc ID người dùng không hợp lệ." });
            }
            if (userId != order.UserID)
                return Forbid();

            var result = new
            {
                order.OrderID,
                order.OrderDate,
                order.TotalAmount,
                order.Status,
                CanCancel = CanCancelOrder(order),
                Customer = new
                {
                    order.User?.FullName,
                    order.User?.Email,
                    order.User?.Phone
                },
                PaymentMethod = order.PaymentMethod?.MethodName,
                Transaction = order.Transaction != null ? new
                {
                    order.Transaction.TransactionID,
                    order.Transaction.PaymentStatus,
                    order.Transaction.PaymentDate
                } : null,
                OrderDetails = order.OrderDetails?.Select(od => new
                {
                    od.OrderDetailID,
                    od.BookID,
                    od.Quantity,
                    od.UnitPrice,
                    Subtotal = od.Quantity * od.UnitPrice,
                    Book = new
                    {
                        od.Book?.BookID,
                        od.Book?.Title,
                        od.Book?.Author,
                        od.Book?.ImgURL1,
                        od.Book?.Price
                    }
                })
            };

            return Ok(new { success = true, data = result });
        }

        [HttpPost]
        public async Task<ActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () => {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var userIdClaim = User.FindFirst("userId")?.Value;
                    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    {
                        return Unauthorized(new { success = false, message = "Không xác thực được người dùng hoặc ID người dùng không hợp lệ." });
                    }

                    if (dto.PaymentMethodId <= 0)
                        return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });

                    var paymentMethod = await _context.PaymentMethods.FindAsync(dto.PaymentMethodId);
                    if (paymentMethod == null)
                        return BadRequest(new { success = false, message = "Phương thức thanh toán không hợp lệ" });

                    // Log the cart item retrieval query
                    var cartItemsQuery = _context.Carts
                        .Where(c => c.UserID == userId)
                        .Include(c => c.Book);

                    var cartItems = await cartItemsQuery.ToListAsync();

                    if (!cartItems.Any())
                        return BadRequest(new { success = false, message = "Giỏ hàng trống" });

                    foreach (var item in cartItems)
                    {
                        if (item.Book != null && item.Quantity > item.Book.Stock)
                            return BadRequest(new { success = false, message = $"Sách '{item.Book.Title}' không đủ số lượng trong kho (còn {item.Book.Stock})" });
                    }

                    var order = new Order
                    {
                        UserID = userId,
                        OrderDate = DateTime.UtcNow,
                        Status = "Pending",
                        PaymentMethodID = dto.PaymentMethodId,
                        TotalAmount = 0
                    };

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    decimal totalAmount = 0;
                    var orderDetails = new List<OrderDetail>();

                    foreach (var cartItem in cartItems)
                    {
                        if (cartItem.Book != null)
                        {
                            var discountedPrice = cartItem.Book.Price * (100 - cartItem.Book.Discount) / 100;

                            var orderDetail = new OrderDetail
                            {
                                OrderID = order.OrderID,
                                BookID = cartItem.BookID,
                                Quantity = cartItem.Quantity,
                                UnitPrice = discountedPrice
                            };

                            _context.OrderDetails.Add(orderDetail);
                            orderDetails.Add(orderDetail);
                            totalAmount += discountedPrice * cartItem.Quantity;

                            cartItem.Book.Stock -= cartItem.Quantity;
                        }
                    }

                    if (totalAmount < MIN_ORDER_AMOUNT)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new { success = false, message = $"Đơn hàng phải có giá trị tối thiểu {MIN_ORDER_AMOUNT:N0} VND" });
                    }

                    order.TotalAmount = totalAmount;

                    var transactionRecord = new Transaction
                    {
                        OrderID = order.OrderID,
                        PaymentStatus = "Pending",
                        PaymentDate = DateTime.UtcNow
                    };

                    _context.Transactions.Add(transactionRecord);
                    _context.Carts.RemoveRange(cartItems);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var result = new
                    {
                        order.OrderID,
                        order.OrderDate,
                        order.TotalAmount,
                        order.Status,
                        PaymentMethod = paymentMethod.MethodName,
                        TransactionID = transactionRecord.TransactionID,
                        ItemCount = orderDetails.Sum(od => od.Quantity)
                    };

                    return Ok(new { success = true, message = "Tạo đơn hàng thành công", data = result });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    var errorMessage = ex.Message;
                    if (ex.InnerException != null)
                    {
                        errorMessage = ex.InnerException.Message;
                    }
                    return StatusCode(500, new { success = false, message = "Lỗi tạo đơn hàng", error = errorMessage });
                }
            });
        }

        [HttpPut("{orderId}/cancel")]
        public async Task<ActionResult> CancelOrder(int orderId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails).ThenInclude(od => od.Book)
                    .Include(o => o.Transaction)
                    .FirstOrDefaultAsync(o => o.OrderID == orderId);

                if (order == null)
                    return NotFound(new { success = false, message = "Không tìm thấy đơn hàng" });

                var userIdClaim = User.FindFirst("userId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { success = false, message = "Không xác thực được người dùng hoặc ID người dùng không hợp lệ." });
                }

                if (userId != order.UserID)
                    return Forbid();

                if (!CanCancelOrder(order))
                    return BadRequest(new { success = false, message = "Không thể hủy đơn hàng này" });

                if (order.OrderDetails != null)
                {
                    foreach (var detail in order.OrderDetails)
                    {
                        if (detail.Book != null)
                            detail.Book.Stock += detail.Quantity;
                    }
                }

                order.Status = "Cancelled";
                if (order.Transaction != null)
                    order.Transaction.PaymentStatus = "Cancelled";

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, message = "Hủy đơn hàng thành công" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { success = false, message = "Lỗi hủy đơn hàng", error = ex.Message });
            }
        }

        private bool CanCancelOrder(Order order)
        {
            if (order.Status != "Pending")
                return false;

            var timeSinceOrder = DateTime.UtcNow - order.OrderDate;
            return timeSinceOrder.TotalHours <= ORDER_CANCELLATION_HOURS;
        }

        // DTOs (Data Transfer Objects) for CreateOrder
        public class CreateOrderDto
        {
            public int PaymentMethodId { get; set; }
        }

        // Define CartItemDto if it's not already defined elsewhere
        public class CartItemDto
        {
            public int BookId { get; set; }
            public int Quantity { get; set; }
        }
    }
}
