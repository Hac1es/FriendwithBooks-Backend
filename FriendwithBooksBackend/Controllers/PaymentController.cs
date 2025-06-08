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
    public class PaymentController : ControllerBase
    {
        private readonly DataContext _context;

        public PaymentController(DataContext context) { _context = context; }

        // GET: api/Payment/methods
        [HttpGet("methods")]
        public async Task<ActionResult> GetAllPaymentMethods()
        {
            var methods = await _context.PaymentMethods.ToListAsync();
            return Ok(new { success = true, data = methods });
        }

        // POST: api/Payment/method
        [HttpPost("method")]
        [Authorize]
        public async Task<ActionResult> CreatePaymentMethod([FromBody] PaymentMethod method)
        {
            // Chỉ cho phép 3 phương thức thanh toán: Tiền mặt, Momo, VNPAY
            var allowedMethods = new[] { "tiền mặt", "momo", "vnpay" };
            if (!allowedMethods.Contains(method.MethodName?.ToLower()))
                return BadRequest(new { success = false, message = "Chỉ hỗ trợ Tiền mặt, Momo và VNPAY" });

            _context.PaymentMethods.Add(method);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, data = method });
        }

        // POST: api/Payment/process
        [HttpPost("process")]
        [Authorize]
        public async Task<ActionResult> ProcessPayment([FromBody] int orderId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var order = await _context.Orders
                .Include(o => o.Transaction)
                .Include(o => o.PaymentMethod)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);

            if (order == null)
                return NotFound(new { success = false, message = "Không tìm thấy đơn hàng" });

            if (userId != order.UserID)
                return Forbid();

            if (order.Status != "Pending")
                return BadRequest(new { success = false, message = $"Đơn hàng có trạng thái '{order.Status}' không thể thanh toán" });

            var paymentMethod = await _context.PaymentMethods.FindAsync(order.PaymentMethodID);
            if (paymentMethod == null)
                return BadRequest(new { success = false, message = "Phương thức thanh toán không hợp lệ" });

            // Xử lý theo từng phương thức
            var methodName = paymentMethod.MethodName?.ToLower();
            return methodName switch
            {
                "tiền mặt" => HandleCashPayment(order),
                "momo" => HandleMomoPayment(order),
                "vnpay" => HandleVNPAYPayment(order),
                _ => BadRequest(new { success = false, message = "Phương thức thanh toán không được hỗ trợ" })
            };
        }

        private ActionResult HandleCashPayment(Order order)
        {
            order.Status = "Paid";
            order.Transaction.PaymentStatus = "Success";
            order.Transaction.PaymentDate = DateTime.UtcNow;
            _context.SaveChanges();
            return Ok(new { success = true, message = "Thanh toán tiền mặt thành công" });
        }

        private ActionResult HandleMomoPayment(Order order)
        {
            var paymentUrl = $"https://sandbox.momo.vn/payment?amount={order.TotalAmount}&orderId={order.OrderID}";
            return Ok(new
            {
                success = true,
                data = new
                {
                    paymentUrl,
                    qrCode = "base64_momo_qr_code_image"
                }
            });
        }

        private ActionResult HandleVNPAYPayment(Order order)
        {
            var paymentUrl = $"https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?amount={order.TotalAmount * 100}&orderInfo=Thanh%20toan%20don%20hang%20{order.OrderID}";
            return Ok(new
            {
                success = true,
                data = new
                {
                    paymentUrl,
                    qrCode = "base64_vnpay_qr_code_image"
                }
            });
        }
    }
}
