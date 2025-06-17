using System.ComponentModel.DataAnnotations.Schema;

namespace FriendwithBooksBackend.Models
{
    public class Category
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CategoryID { get; set; }
        public int? ParentID { get; set; }
        public string? CategoryName { get; set; }

        public ICollection<Book>? Books { get; set; }
    }
}
