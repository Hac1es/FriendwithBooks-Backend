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
        private const int MAX_CART_ITEMS = 20;
        private const int MAX_QUANTITY_PER_ITEM = 10;

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

            // Trả về DTO, KHÔNG trả về object Book gốc hoặc navigation property
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
                        Book = c.Book == null ? null : new
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
                    TotalAmount = totalAmount,
                    MaxItemsPerCart = MAX_CART_ITEMS,
                    MaxQuantityPerItem = MAX_QUANTITY_PER_ITEM
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

            if (book.Price <= 0)
                return BadRequest(new { success = false, message = "Giá sách không hợp lệ" });

            if (book.Stock < request.Quantity)
                return BadRequest(new { success = false, message = $"Chỉ còn {book.Stock} sản phẩm trong kho" });

            if (request.Quantity > MAX_QUANTITY_PER_ITEM)
                return BadRequest(new { success = false, message = $"Số lượng tối đa cho mỗi sản phẩm là {MAX_QUANTITY_PER_ITEM}" });

            // Kiểm tra số lượng item trong giỏ
            var currentCartItems = await _context.Carts
                .Where(c => c.UserID == userId.Value)
                .CountAsync();

            if (currentCartItems >= MAX_CART_ITEMS)
                return BadRequest(new { success = false, message = $"Giỏ hàng đã đạt tối đa {MAX_CART_ITEMS} sản phẩm" });

            // Kiểm tra đã có sản phẩm trong giỏ chưa
            var existingCartItem = await _context.Carts
                .FirstOrDefaultAsync(c => c.UserID == userId.Value && c.BookID == request.BookID);

            if (existingCartItem != null)
            {
                var newQuantity = existingCartItem.Quantity + request.Quantity;
                if (newQuantity > book.Stock)
                    return BadRequest(new { success = false, message = $"Tổng số lượng ({newQuantity}) vượt quá tồn kho ({book.Stock})" });

                if (newQuantity > MAX_QUANTITY_PER_ITEM)
                    return BadRequest(new { success = false, message = $"Tổng số lượng ({newQuantity}) vượt quá giới hạn ({MAX_QUANTITY_PER_ITEM})" });

                existingCartItem.Quantity = newQuantity;
                existingCartItem.CreateDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Trả về DTO thay vì entity gốc
                return Ok(new { success = true, message = "Đã cập nhật số lượng trong giỏ hàng", data = new {
                    existingCartItem.CartID,
                    existingCartItem.BookID,
                    existingCartItem.Quantity,
                    existingCartItem.CreateDate
                }});
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

                // Trả về DTO thay vì entity gốc
                return Ok(new { success = true, message = "Đã thêm vào giỏ hàng thành công", data = new {
                    cartItem.CartID,
                    cartItem.BookID,
                    cartItem.Quantity,
                    cartItem.CreateDate
                }});
            }
        }

        // PUT: api/Cart/{cartId}
        [HttpPut("{cartId}")]
        public async Task<ActionResult> UpdateCartItem(int cartId, [FromBody] CartRequest request)
        {
            if (request.Quantity <= 0)
                return BadRequest(new { success = false, message = "Số lượng phải lớn hơn 0" });

            if (request.Quantity > MAX_QUANTITY_PER_ITEM)
                return BadRequest(new { success = false, message = $"Số lượng tối đa cho mỗi sản phẩm là {MAX_QUANTITY_PER_ITEM}" });

            var cartItem = await _context.Carts.Include(c => c.Book).FirstOrDefaultAsync(c => c.CartID == cartId);
            if (cartItem == null)
                return NotFound(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng" });

            var userId = GetCurrentUserId();
            if (userId != cartItem.UserID)
                return Forbid();

            if (cartItem.Book != null && request.Quantity > cartItem.Book.Stock)
                return BadRequest(new { success = false, message = $"Chỉ còn {cartItem.Book.Stock} sản phẩm trong kho" });

            cartItem.Quantity = request.Quantity;
            cartItem.CreateDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Trả về DTO thay vì entity gốc
            return Ok(new { success = true, message = "Cập nhật số lượng thành công", data = new {
                cartItem.CartID,
                cartItem.BookID,
                cartItem.Quantity,
                cartItem.CreateDate
            }});
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

            // Đảm bảo Entity Framework theo dõi đối tượng trước khi xóa
            _context.Entry(cartItem).State = EntityState.Deleted;
            
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

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            Console.WriteLine($"CartController: GetCurrentUserId - userIdClaim: {userIdClaim}");
            if (int.TryParse(userIdClaim, out int userId))
            {
                Console.WriteLine($"CartController: GetCurrentUserId - Parsed userId: {userId}");
                return userId;
            }
            Console.WriteLine("CartController: GetCurrentUserId - Failed to parse userIdClaim.");
            return null;
        }
    }
}
