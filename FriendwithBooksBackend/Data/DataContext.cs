using Microsoft.EntityFrameworkCore;
using FriendwithBooksBackend.Models;

namespace FriendwithBooksBackend.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }
        public DbSet<User> Users { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<FlashSale> FlashSales { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Composite keys
            // modelBuilder.Entity<Cart>().HasKey(c => new { c.CartID, c.UserID, c.BookID });
            modelBuilder.Entity<OrderDetail>().HasKey(od => new { od.OrderDetailID, od.OrderID, od.BookID });
            modelBuilder.Entity<Review>().HasKey(r => new { r.ReviewID, r.BookID, r.UserID });

            // Chuyển tất cả tên bảng và cột sang lowercase
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                // Đổi tên bảng
                entity.SetTableName(entity.GetTableName()!.ToLower());

                // Đổi tên cột
                foreach (var property in entity.GetProperties())
                {
                    property.SetColumnName(property.Name.ToLower());
                }
            }

            // Cấu hình OrderID là cột tự động tăng (Identity Column)
            modelBuilder.Entity<Order>()
                .Property(o => o.OrderID)
                .ValueGeneratedOnAdd();

            // Cấu hình OrderDetailID là cột tự động tăng
            modelBuilder.Entity<OrderDetail>()
                .Property(od => od.OrderDetailID)
                .ValueGeneratedOnAdd();

            // Cấu hình TransactionID là cột tự động tăng
            modelBuilder.Entity<Transaction>()
                .Property(t => t.TransactionID)
                .ValueGeneratedOnAdd();

            // Foreign keys
            modelBuilder.Entity<Book>()
                .HasOne(b => b.Category)
                .WithMany(c => c.Books)
                .HasForeignKey(b => b.CategoryID);

            modelBuilder.Entity<Cart>()
                .HasOne(c => c.User)
                .WithMany(u => u.Carts)
                .HasForeignKey(c => c.UserID);

            modelBuilder.Entity<Cart>()
                .HasOne(c => c.Book)
                .WithMany(b => b.Carts)
                .HasForeignKey(c => c.BookID);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserID);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.PaymentMethod)
                .WithMany(pm => pm.Orders)
                .HasForeignKey(o => o.PaymentMethodID);

            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderID);

            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Book)
                .WithMany(b => b.OrderDetails)
                .HasForeignKey(od => od.BookID);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserID);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Book)
                .WithMany(b => b.Reviews)
                .HasForeignKey(r => r.BookID);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Order)
                .WithOne(o => o.Transaction)
                .HasForeignKey<Transaction>(t => t.OrderID);
            modelBuilder.Entity<FlashSale>()
                .HasOne(fs => fs.Book)
                .WithMany(b => b.FlashSales)
                .HasForeignKey(fs => fs.BookID);
        }
    }
}
