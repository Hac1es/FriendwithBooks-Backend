using FriendwithBooksBackend.Data;
using FriendwithBooksBackend.Models;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace FriendwithBooksBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(DataContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST: api/Auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest(new { message = "Email đã được đăng ký" });

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Password = HashPassword(dto.Password),
                Phone = dto.Phone,
                Address = dto.Address,
                RegistrationDate = DateTime.UtcNow,
                Role = "user",
                Avatar = "https://i.pinimg.com/736x/8f/1c/a2/8f1ca2029e2efceebd22fa05cca423d7.jpg"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            return Ok(new { token });
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return Unauthorized(new { message = "Email chưa được đăng ký" });
            if (!VerifyPassword(dto.Password, user.Password))
                return Unauthorized(new { message = "Mật khẩu không chính xác" });

            var token = GenerateJwtToken(user);
            return Ok(new { token });
        }

        // POST: api/Auth/googleLogin
        /*[HttpPost("googleLogin")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
        {
            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken);
            }
            catch
            {
                return Unauthorized(new { message = "Token Google không hợp lệ" });
            }

            // Kiểm tra user đã tồn tại chưa
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);
            if (user == null)
            {
                // Đăng ký user mới
                user = new User
                {
                    FullName = payload.Name ?? payload.Email,
                    Email = payload.Email,
                    Password = "", // Không cần mật khẩu cho user Google
                    Avatar = payload.Picture,
                    RegistrationDate = DateTime.UtcNow,
                    Role = "user"
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            var token = GenerateJwtToken(user);
            return Ok(new { token });
        }*/

        // PUT: api/Auth/updateProfile
        [Authorize]
        [HttpPut("updateProfile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userIdClaim = User.FindFirst("userId");
            if (userIdClaim == null)
                return Unauthorized(new { message = "Không xác thực được người dùng." });

            if (!int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized(new { message = "ID người dùng không hợp lệ." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng." });

            user.FullName = dto.FullName;
            user.Phone = dto.Phone;
            user.Address = dto.Address;
            user.Avatar = dto.Avatar;

            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            return Ok(new { token });
        }

        // PUT: api/Auth/forgotPassword
        [HttpPost("forgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return Unauthorized(new { message = "Email chưa được đăng ký" });

            user.Password = HashPassword(dto.Password);

            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            return Ok(new { token });
        }

        // Simple hash for demo (use a stronger method in production)
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private bool VerifyPassword(string input, string hash)
        {
            return HashPassword(input) == hash;
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"];
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim("userId", user.UserID.ToString()),
                new Claim("fullName", user.FullName.ToString()),
                new Claim("email", user.Email.ToString()),
                new Claim("phone", user.Phone.ToString()),
                new Claim("address", user.Address.ToString()),
                new Claim("avatar", user.Avatar.ToString()),
                new Claim("registrationDate", user.RegistrationDate?.ToString("o")),
                new Claim("role", user.Role.ToString()),
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: null,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(12),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // DTOs defined inside the controller
        public class RegisterDto
        {
            public string FullName { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
            public string? Phone { get; set; }
            public string? Address { get; set; }
        }

        public class LoginDto
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }

        public class GoogleLoginDto
        {
            public string IdToken { get; set; }
        }

        public class UpdateProfileDto
        {
            public string FullName { get; set; }
            public string? Phone { get; set; }
            public string? Address { get; set; }
            public string? Avatar { get; set; }    
        }

        public class ForgotPasswordDto
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }
    }
}