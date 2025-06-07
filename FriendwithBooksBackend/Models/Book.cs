using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.ComponentModel.DataAnnotations.Schema;

namespace FriendwithBooksBackend.Models
{
    public class Book
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
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
        [Column("imgurl1")]
        public string? ImgURL1 { get; set; }
        [Column("imgurl2")]
        public string? ImgURL2 { get; set; }
        [Column("imgurl3")]
        public string? ImgURL3 { get; set; }
        public int AvgRating { get; set; }
        public int TotalRating { get; set; }
        public string? AgeGroup { get; set; } // all, 12, 18
        public int Discount { get; set; }
        public Category? Category { get; set; }
        public ICollection<Cart>? Carts { get; set; }
        public ICollection<OrderDetail>? OrderDetails { get; set; }
        public ICollection<Review>? Reviews { get; set; }
        public ICollection<FlashSale>? FlashSales { get; set; }
    }
}
