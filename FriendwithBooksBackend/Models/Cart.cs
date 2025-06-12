using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace FriendwithBooksBackend.Models


{
    public class Cart
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CartID { get; set; }
        public int UserID { get; set; }
        public int BookID { get; set; }
        public int Quantity { get; set; }
        public DateTime? CreateDate { get; set; }

        public User? User { get; set; }
        public Book? Book { get; set; }
    }
}
