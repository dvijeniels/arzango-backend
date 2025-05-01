using ArzanGo.Models;
using Microsoft.EntityFrameworkCore;

namespace ArzanGo.Data
{
    public class AppDbContext:DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<About> About { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductPhoto> ProductPhotos { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<PaymentSettings> PaymentSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Для Order
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Address)
                .WithMany(a => a.Orders)
                .HasForeignKey(o => o.AddressId)
                .OnDelete(DeleteBehavior.Restrict); // Изменяем поведение удаления

            // Configure the relationship between Order and User (Customer)
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Users)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure the relationship between Order and User (Courier)
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Courier)
                .WithMany() // Assuming Courier doesn't have a navigation property back to Orders
                .HasForeignKey(o => o.CourierId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
