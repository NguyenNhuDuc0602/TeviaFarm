using Microsoft.EntityFrameworkCore;
using TeviaFarm.Models;

namespace TeviaFarm.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Cart> Carts => Set<Cart>();
        public DbSet<CartItem> CartItems => Set<CartItem>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
        public DbSet<Post> Posts => Set<Post>();
        public DbSet<Course> Courses => Set<Course>();
        public DbSet<Lesson> Lessons => Set<Lesson>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Cart>()
                .HasMany(c => c.Items)
                .WithOne(i => i.Cart!)
                .HasForeignKey(i => i.CartId);

            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderDetails)
                .WithOne(d => d.Order!)
                .HasForeignKey(d => d.OrderId);

            modelBuilder.Entity<Course>()
                .HasMany(c => c.Lessons)
                .WithOne(l => l.Course!)
                .HasForeignKey(l => l.CourseId);
        }
    }
}

