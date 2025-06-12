namespace FriendwithBooksBackend.Models
{
    public class Transaction
    {
        public int TransactionID { get; set; }
        public int OrderID { get; set; }
        public string? PaymentStatus { get; set; }
        public DateTime PaymentDate { get; set; }

        public Order? Order { get; set; }
    }

}
