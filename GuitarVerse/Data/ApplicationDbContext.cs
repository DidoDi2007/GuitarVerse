using Microsoft.EntityFrameworkCore;
using GuitarVerse.Models;

namespace GuitarVerse.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Category> Categories { get; set; } // Ново
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Review> Reviews { get; set; }       // Ново
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<PasswordReset> PasswordResets { get; set; }
        public DbSet<PromoCode> PromoCodes { get; set; }
        public DbSet<SupportMessage> SupportMessages { get; set; }
    }
}