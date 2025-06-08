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

        public OrderController(DataContext context) { _context = context; }

        [HttpGet("my")]
        public async Task<ActionResult> GetMyOrders()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var orders = await _context.Orders
                .Where(o => o.UserID == userId)
                .Include(o => o.PaymentMethod)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Book)
                .Include(o => o.Transaction)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var result = orders.Select(o => new
            {
                o.OrderID,
                o.OrderDate,
                o.TotalAmount,
                o.Status,
                PaymentMethod = o.PaymentMethod?.MethodName,
                PaymentStatus = o.Transaction?.PaymentStatus,
                ItemCount = o.OrderDetails?.Sum(od => od.Quantity) ?? 0,
                OrderDetails = o.OrderDetails?.Select(od => new
                {
                    od.OrderDetailID,
                    od.BookID,
                    od.Quantity,
                    od.UnitPrice,
                    Book = new
                    {
                        od.Book?.Title,
                        od.Book?.Author,
                        od.Book?.ImgURL1
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

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (userId != order.UserID)
                return Forbid();

            var result = new
            {
                order.OrderID,
                order.OrderDate,
                order.TotalAmount,
                order.Status,
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
        public async Task<ActionResult> CreateOrder([FromBody] int paymentMethodId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (paymentMethodId <= 0)
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });

                var paymentMethod = await _context.PaymentMethods.FindAsync(paymentMethodId);
                if (paymentMethod == null)
                    return BadRequest(new { success = false, message = "Phương thức thanh toán không hợp lệ" });

                var cartItems = await _context.Carts
                    .Where(c => c.UserID == userId)
                    .Include(c => c.Book)
                    .ToListAsync();

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
                    PaymentMethodID = paymentMethodId,
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
                return StatusCode(500, new { success = false, message = "Lỗi tạo đơn hàng", error = ex.Message });
            }
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

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (userId != order.UserID)
                    return Forbid();

                if (order.Status != "Pending")
                    return BadRequest(new { success = false, message = $"Không thể hủy đơn hàng có trạng thái '{order.Status}'" });

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
                return StatusCode(500, new { success = false, message = "Lỗi server", error = ex.Message });
            }
        }
    }
}
