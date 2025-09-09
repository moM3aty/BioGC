using BioGC.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BioGC.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<RelaxationVideo> RelaxationVideos { get; set; }
        public DbSet<RelaxationAudio> RelaxationAudios { get; set; }
        public DbSet<ShippingZone> ShippingZones { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<RelaxationSubscription> RelaxationSubscriptions { get; set; }
        public DbSet<RelaxationPackage> RelaxationPackages { get; set; }
        public DbSet<UserRelaxationPackage> UserRelaxationPackages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Category self-referencing relationship for parent/sub categories
            builder.Entity<Category>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // User -> Notifications relationship
            builder.Entity<ApplicationUser>()
                .HasMany(u => u.Notifications)
                .WithOne(n => n.User)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserRelaxationPackage many-to-many join table setup
            builder.Entity<UserRelaxationPackage>()
               .HasKey(up => new { up.ApplicationUserId, up.RelaxationPackageId });

            builder.Entity<UserRelaxationPackage>()
                .HasOne(up => up.User)
                .WithMany(u => u.PurchasedRelaxationPackages)
                .HasForeignKey(up => up.ApplicationUserId);

            builder.Entity<UserRelaxationPackage>()
                .HasOne(up => up.Package)
                .WithMany(p => p.PurchasedByUsers)
                .HasForeignKey(up => up.RelaxationPackageId);

            // RelaxationPackage -> Category relationship
            builder.Entity<RelaxationPackage>()
                .HasOne(p => p.Category)
                .WithMany(c => c.RelaxationPackages) // Use the new navigation property
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); // Prevents deleting a category if packages are linked to it

            // RelaxationSubscription -> Order relationship
            builder.Entity<RelaxationSubscription>()
                .HasOne(s => s.Order)
                .WithMany() // Order model does not have a collection of subscriptions
                .HasForeignKey(s => s.OrderId)
                .OnDelete(DeleteBehavior.Restrict); // Prevents deleting an Order if a Subscription is linked to it
        }
    }
}
