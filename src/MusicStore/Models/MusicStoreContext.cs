using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;

namespace MusicStore.Models
{
    public class ApplicationUser : IdentityUser { }

    public class MusicStoreContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Album> Albums { get; set; }
        public DbSet<Artist> Artists { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // TODO: Remove when explicit values insertion removed.
            builder.Entity<Artist>().Property(a => a.ArtistId).ValueGeneratedNever();
            builder.Entity<Genre>().Property(g => g.GenreId).ValueGeneratedNever();

            //Deleting an album fails with this relation
            builder.Entity<Album>().Ignore(a => a.OrderDetails);
            builder.Entity<OrderDetail>().Ignore(od => od.Album);

            base.OnModelCreating(builder);
        }
    }
}