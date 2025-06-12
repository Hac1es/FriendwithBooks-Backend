using FriendwithBooksBackend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FriendwithBooksBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Yêu cầu xác thực cho tất cả các hành động trong controller này
    public class ProfileController : ControllerBase
    {
        private readonly DataContext _context;

        public ProfileController(DataContext context)
        {
            _context = context;
        }

        // GET: api/Profile
        // Lấy thông tin profile của người dùng hiện tại
        [HttpGet] // Endpoint này sẽ là /api/Profile
        public async Task<IActionResult> GetProfile()
        {
            var userIdClaim = User.FindFirst("userId");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Không xác thực được người dùng hoặc ID người dùng không hợp lệ." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy người dùng." });
            }

            // Trả về một anonymous object (có thể thay bằng DTO nếu cần cấu trúc phức tạp hơn)
            // Đảm bảo tên trường trùng khớp với frontend mong đợi (camelCase)
            return Ok(new
            {
                fullName = user.FullName,
                email = user.Email,
                phoneNumber = user.Phone,
                address = user.Address,
                avatar = user.Avatar,
                role = user.Role,
                registrationDate = user.RegistrationDate,
                // Thêm các trường khác nếu có trong model User của bạn và muốn hiển thị
                // Ví dụ: city = user.City, province = user.Province
            });
        }
    }
} 