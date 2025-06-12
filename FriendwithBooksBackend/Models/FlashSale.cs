using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FriendwithBooksBackend.Models
{
        public class FlashSale
        {
            [Key]
            public int FlashSaleID { get; set; }

            [ForeignKey("Book")]
            public int BookID { get; set; }

            public int DiscountPercent { get; set; }

            public DateTime StartTime { get; set; }

            public DateTime EndTime { get; set; }

            public Book? Book { get; set; }
        }
}

