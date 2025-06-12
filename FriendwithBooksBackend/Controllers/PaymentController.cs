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
        private const int PAYMENT_TIMEOUT_MINUTES = 0;
        private const int MAX_PAYMENT_RETRIES = 3;

        public PaymentController(DataContext context) { _context = context; }

        // GET: api/Payment/methods
        [HttpGet("methods")]
        public async Task<ActionResult> GetAllPaymentMethods()
        {
            var methods = await _context.PaymentMethods.ToListAsync();
            
            var responseData = methods.Select(m => new
            {
                id = m.PaymentMethodID, // Dùng ID thực tế của phương thức
                name = m.MethodName, // Tên phương thức
                img = GetPaymentMethodImageUrl(m.MethodName) // Thêm trường imgUrl
            }).ToList();

            return Ok(new { success = true, data = responseData });
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

        // POST: api/Payment/process/{orderId}
        [HttpPost("process/{orderId}")]
        [Authorize]
        public async Task<ActionResult> ProcessPayment(int orderId)
        {
            var userIdClaim = User.FindFirst("userId")?.Value; // Lấy claim userId
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { success = false, message = "Không xác thực được người dùng hoặc ID người dùng không hợp lệ." });
            }

            var order = await _context.Orders
                .Include(o => o.Transaction)
                .Include(o => o.PaymentMethod)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);

            if (order == null)
                return NotFound(new { success = false, message = "Không tìm thấy đơn hàng" });

            Console.WriteLine($"Backend: Processing order {order.OrderID} with PaymentMethodID: {order.PaymentMethodID}");

            if (userId != order.UserID)
                return Forbid();

            if (order.Status != "Pending")
                return BadRequest(new { success = false, message = $"Đơn hàng có trạng thái '{order.Status}' không thể thanh toán" });

            var paymentMethod = await _context.PaymentMethods.FindAsync(order.PaymentMethodID);
            if (paymentMethod == null)
                return BadRequest(new { success = false, message = "Phương thức thanh toán không hợp lệ" });

            Console.WriteLine($"Backend: Retrieved PaymentMethod Name: {paymentMethod.MethodName}");

            // Kiểm tra thời gian thanh toán
            if (order.Transaction?.PaymentDate != null)
            {
                var timeSinceLastPayment = DateTime.UtcNow - order.Transaction.PaymentDate;
                if (timeSinceLastPayment.TotalMinutes < PAYMENT_TIMEOUT_MINUTES)
                    return BadRequest(new { success = false, message = $"Vui lòng đợi {PAYMENT_TIMEOUT_MINUTES} phút trước khi thử thanh toán lại" });
            }

            // Xử lý theo từng phương thức
            var methodName = paymentMethod.MethodName?.ToLower();
            return methodName switch
            {
                "cash" => await HandleCashPayment(order),
                "momo" => await HandleMomoPayment(order),
                "vnpay" => await HandleVNPAYPayment(order),
                _ => BadRequest(new { success = false, message = "Phương thức thanh toán không được hỗ trợ" })
            };
        }

        // POST: api/Payment/webhook/momo
        [HttpPost("webhook/momo")]
        [AllowAnonymous]
        public async Task<ActionResult> MomoWebhook([FromBody] MomoWebhookRequest request)
        {
            try
            {
                // Xác thực chữ ký webhook
                if (!ValidateMomoWebhook(request))
                    return BadRequest(new { success = false, message = "Chữ ký không hợp lệ" });

                var order = await _context.Orders
                    .Include(o => o.Transaction)
                    .FirstOrDefaultAsync(o => o.OrderID == request.OrderId);

                if (order == null)
                    return NotFound(new { success = false, message = "Không tìm thấy đơn hàng" });

                if (order.Transaction == null)
                    return BadRequest(new { success = false, message = "Không tìm thấy giao dịch" });

                order.Transaction.PaymentStatus = request.ResultCode == 0 ? "Success" : "Failed";
                order.Transaction.PaymentDate = DateTime.UtcNow;
                order.Status = request.ResultCode == 0 ? "Paid" : "PaymentFailed";

                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi xử lý webhook", error = ex.Message });
            }
        }

        // POST: api/Payment/webhook/vnpay
        [HttpPost("webhook/vnpay")]
        [AllowAnonymous]
        public async Task<ActionResult> VNPAYWebhook([FromBody] VNPAYWebhookRequest request)
        {
            try
            {
                // Xác thực chữ ký webhook
                if (!ValidateVNPAYWebhook(request))
                    return BadRequest(new { success = false, message = "Chữ ký không hợp lệ" });

                var order = await _context.Orders
                    .Include(o => o.Transaction)
                    .FirstOrDefaultAsync(o => o.OrderID == request.OrderId);

                if (order == null)
                    return NotFound(new { success = false, message = "Không tìm thấy đơn hàng" });

                if (order.Transaction == null)
                    return BadRequest(new { success = false, message = "Không tìm thấy giao dịch" });

                order.Transaction.PaymentStatus = request.ResponseCode == "00" ? "Success" : "Failed";
                order.Transaction.PaymentDate = DateTime.UtcNow;
                order.Status = request.ResponseCode == "00" ? "Paid" : "PaymentFailed";

                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi xử lý webhook", error = ex.Message });
            }
        }

        private async Task<ActionResult> HandleCashPayment(Order order)
        {
            order.Status = "Paid";
            if (order.Transaction == null)
            {
                order.Transaction = new Transaction
                {
                    OrderID = order.OrderID,
                    PaymentStatus = "Success",
                    PaymentDate = DateTime.UtcNow
                };
            }
            else
            {
                order.Transaction.PaymentStatus = "Success";
                order.Transaction.PaymentDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Thanh toán tiền mặt thành công" });
        }

        private async Task<ActionResult> HandleMomoPayment(Order order)
        {
            if (order.Transaction == null)
            {
                order.Transaction = new Transaction
                {
                    OrderID = order.OrderID,
                    PaymentStatus = "Pending",
                    PaymentDate = DateTime.UtcNow
                };
            }
            else
            {
                order.Transaction.PaymentStatus = "Pending";
                order.Transaction.PaymentDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            var paymentUrl = $"https://sandbox.momo.vn/payment?amount={order.TotalAmount}&orderId={order.OrderID}";
            return Ok(new
            {
                success = true,
                data = new
                {
                    paymentUrl,
                    qrCode = "base64_momo_qr_code_image",
                    timeoutMinutes = PAYMENT_TIMEOUT_MINUTES
                }
            });
        }

        private async Task<ActionResult> HandleVNPAYPayment(Order order)
        {
            if (order.Transaction == null)
            {
                order.Transaction = new Transaction
                {
                    OrderID = order.OrderID,
                    PaymentStatus = "Pending",
                    PaymentDate = DateTime.UtcNow
                };
            }
            else
            {
                order.Transaction.PaymentStatus = "Pending";
                order.Transaction.PaymentDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            var paymentUrl = $"https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?amount={order.TotalAmount * 100}&orderInfo=Thanh%20toan%20don%20hang%20{order.OrderID}";
            return Ok(new
            {
                success = true,
                data = new
                {
                    paymentUrl,
                    qrCode = "base64_vnpay_qr_code_image",
                    timeoutMinutes = PAYMENT_TIMEOUT_MINUTES
                }
            });
        }

        private bool ValidateMomoWebhook(MomoWebhookRequest request)
        {
            // TODO: Implement Momo webhook signature validation
            return true;
        }

        private bool ValidateVNPAYWebhook(VNPAYWebhookRequest request)
        {
            // TODO: Implement VNPAY webhook signature validation
            return true;
        }

        // Hàm giúp ánh xạ MethodName sang URL ảnh
        private string? GetPaymentMethodImageUrl(string? methodName)
        {
            return methodName?.ToLower() switch
            {
                "momo" => "https://static.mservice.io/img/logo-momo.png", // URL ảnh Momo
                "vnpay" => "/Vnpay.png", // URL ảnh VNPAY trong public folder
                "cash" => "/Picture1.png", // Đã sửa từ "tiền mặt" thành "cash" để khớp DB
                _ => null // Hoặc một ảnh mặc định khác nếu không khớp
            };
        }
    }

    public class MomoWebhookRequest
    {
        public int OrderId { get; set; }
        public int ResultCode { get; set; }
        public string Message { get; set; }
        public string Signature { get; set; }
    }

    public class VNPAYWebhookRequest
    {
        public int OrderId { get; set; }
        public string ResponseCode { get; set; }
        public string Message { get; set; }
        public string Signature { get; set; }
    }
}
