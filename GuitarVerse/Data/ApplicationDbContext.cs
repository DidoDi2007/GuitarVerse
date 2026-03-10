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
        public DbSet<Product> Products { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<PasswordReset> PasswordResets { get; set; } 
    }
}
