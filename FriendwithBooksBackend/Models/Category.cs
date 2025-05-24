namespace FriendwithBooksBackend.Models
{
    public class Category
    {
        public int CategoryID { get; set; }
        public int? ParentID { get; set; }
        public string? CategoryName { get; set; }

        public ICollection<Book>? Books { get; set; }
    }
}
