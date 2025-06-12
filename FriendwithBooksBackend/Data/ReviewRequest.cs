namespace FriendwithBooksBackend.Data
{
    public class ReviewRequest
    {
        public int UserID { get; set; }
        public int BookID { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}
