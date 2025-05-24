using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace FriendwithBooksBackend.Models
{
    public class Book
    {
        public int BookID { get; set; }
        public string? Supplier { get; set; }
        public string? Title { get; set; }
        public string? Author { get; set; } 
        public int CategoryID { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string? Description { get; set; }
        public DateTime PublishYear { get; set; }
        public string? Language { get; set; }
        public string? PageNum { get; set; }
        public string? Binding { get; set; }
        public string? ImgURL { get; set; }
        public int AvgRating { get; set; }
        public int TotalRating { get; set; }
        public Category? Category { get; set; }
        public ICollection<Cart>? Carts { get; set; }
        public ICollection<OrderDetail>? OrderDetails { get; set; }
        public ICollection<Review>? Reviews { get; set; }
        public ICollection<FlashSale>? FlashSales { get; set; }
    }
}
