using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FriendwithBooksBackend.Data;
using FriendwithBooksBackend.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace FriendwithBooksBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly DataContext _context;

        public CartController(DataContext context) { _context = context; }

        // Model cho request thêm/cập nhật giỏ hàng
        public class CartRequest
        {
            [JsonPropertyName("bookID")]
            public int BookID { get; set; }
            [JsonPropertyName("quantity")]
            public int Quantity { get; set; }
        }

        // GET: api/Cart/my
        [HttpGet("my")]
        public async Task<ActionResult> GetMyCart()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { success = false, message = "Không thể xác định người dùng" });

            var cartItems = await _context.Carts
                .Where(c => c.UserID == userId.Value)
                .Include(c => c.Book)
                .ToListAsync();

            var totalItems = cartItems.Sum(c => c.Quantity);
            var totalAmount = cartItems.Sum(c =>
                (c.Book.Price * (100 - c.Book.Discount) / 100) * c.Quantity);

            return Ok(new
            {
                success = true,
                data = new
                {
                    UserID = userId.Value,
                    Items = cartItems.Select(c => new
                    {
                        c.CartID,
                        c.BookID,
                        c.Quantity,
                        c.CreateDate,
                        Book = new
                        {
                            c.Book.BookID,
                            c.Book.Title,
                            c.Book.Author,
                            c.Book.Price,
                            c.Book.Discount,
                            c.Book.Stock,
                            c.Book.ImgURL1,
                            DiscountedPrice = c.Book.Price * (100 - c.Book.Discount) / 100
                        }
                    }),
                    TotalItems = totalItems,
                    TotalAmount = totalAmount
                }
            });
        }

        // POST: api/Cart
       [HttpPost]
public async Task<ActionResult> AddToCart([FromBody] CartRequest request)
{
    if (request.BookID <= 0 || request.Quantity <= 0)
        return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });

    var userId = GetCurrentUserId();
    if (userId == null)
        return Unauthorized(new { success = false, message = "Không thể xác định người dùng" });

    var book = await _context.Books.FindAsync(request.BookID);
    if (book == null)
        return NotFound(new { success = false, message = "Không tìm thấy sách" });

    if (book.Stock < request.Quantity)
        return BadRequest(new { success = false, message = $"Chỉ còn {book.Stock} sản phẩm trong kho" });

    // Kiểm tra đã có sản phẩm trong giỏ chưa (đúng tên cột)
    var existingCartItem = await _context.Carts
        .FirstOrDefaultAsync(c => c.UserID == userId.Value && c.BookID == request.BookID);

    if (existingCartItem != null)
    {
        var newQuantity = existingCartItem.Quantity + request.Quantity;
        if (newQuantity > book.Stock)
            return BadRequest(new { success = false, message = $"Tổng số lượng ({newQuantity}) vượt quá tồn kho ({book.Stock})" });

        existingCartItem.Quantity = newQuantity;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Đã cập nhật số lượng trong giỏ hàng", data = existingCartItem });
    }
    else
    {
        var cartItem = new Cart
        {
            UserID = userId.Value,
            BookID = request.BookID,
            Quantity = request.Quantity,
            CreateDate = DateTime.UtcNow
        };

        _context.Carts.Add(cartItem);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Đã thêm vào giỏ hàng thành công", data = cartItem });
    }
}

        // PUT: api/Cart/{cartId}
        [HttpPut("{cartId}")]
        public async Task<ActionResult> UpdateCartItem(int cartId, [FromBody] CartRequest request)
        {
            if (request.Quantity <= 0)
                return BadRequest(new { success = false, message = "Số lượng phải lớn hơn 0" });

            var cartItem = await _context.Carts.Include(c => c.Book).FirstOrDefaultAsync(c => c.CartID == cartId);
            if (cartItem == null)
                return NotFound(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng" });

            var userId = GetCurrentUserId();
            if (userId != cartItem.UserID)
                return Forbid();

            if (cartItem.Book != null && request.Quantity > cartItem.Book.Stock)
                return BadRequest(new { success = false, message = $"Chỉ còn {cartItem.Book.Stock} sản phẩm trong kho" });

            cartItem.Quantity = request.Quantity;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Cập nhật số lượng thành công", data = cartItem });
        }

        // DELETE: api/Cart/{cartId}
        [HttpDelete("{cartId}")]
        public async Task<ActionResult> RemoveFromCart(int cartId)
        {
            var cartItem = await _context.Carts.FindAsync(cartId);
            if (cartItem == null)
                return NotFound(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng" });

            var userId = GetCurrentUserId();
            if (userId != cartItem.UserID)
                return Forbid();

            _context.Carts.Remove(cartItem);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã xóa sản phẩm khỏi giỏ hàng" });
        }

        // DELETE: api/Cart/clear
        [HttpDelete("clear")]
        public async Task<ActionResult> ClearMyCart()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { success = false, message = "Không thể xác định người dùng" });

            var cartItems = await _context.Carts.Where(c => c.UserID == userId.Value).ToListAsync();
            if (cartItems.Any())
            {
                _context.Carts.RemoveRange(cartItems);
                await _context.SaveChangesAsync();
            }

            return Ok(new { success = true, message = $"Đã xóa {cartItems.Count} sản phẩm khỏi giỏ hàng" });
        }

        // GET: api/Cart/count
        [HttpGet("count")]
        public async Task<ActionResult> GetMyCartItemCount()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { success = false, message = "Không thể xác định người dùng" });

            var count = await _context.Carts.Where(c => c.UserID == userId.Value).SumAsync(c => c.Quantity);
            return Ok(new { success = true, count = count });
        }

        // Lấy userId từ JWT token, hỗ trợ cả "sub" và "nameid"
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
                return userId;
            return null;
        }
    }
}
