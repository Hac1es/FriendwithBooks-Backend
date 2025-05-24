namespace FriendwithBooksBackend.Models
{
    public class Cart
    {
        public int CartID { get; set; }
        public int UserID { get; set; }
        public int BookID { get; set; }
        public int Quantity { get; set; }
        public DateTime? CreateDate { get; set; }

        public User? User { get; set; }
        public Book? Book { get; set; }
    }
}
