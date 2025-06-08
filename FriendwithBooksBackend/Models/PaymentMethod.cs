namespace FriendwithBooksBackend.Models
{
    public class PaymentMethod
    {
        public int PaymentMethodID { get; set; }
        public string? MethodName { get; set; }


        public ICollection<Order>? Orders { get; set; }
      

    }

}
