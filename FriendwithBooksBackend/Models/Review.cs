using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FriendwithBooksBackend.Models
{
    public class Review
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ReviewID { get; set; }
        public int UserID { get; set; }
        public int BookID { get; set; }
        public int Rating { get; set; } // from 1 to 5 stars
        public string? Comment { get; set; }
        public DateTime ReviewDate { get; set; }

        public User? User { get; set; }
        public Book? Book { get; set; }
    }
}
