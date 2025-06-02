using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace FriendwithBooksBackend.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; } // Hashed password
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Avatar { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public string? Role { get; set; }

        public ICollection<Cart>? Carts { get; set; }
        public ICollection<Order>? Orders { get; set; }
        public ICollection<Review>? Reviews { get; set; }
    }
}
