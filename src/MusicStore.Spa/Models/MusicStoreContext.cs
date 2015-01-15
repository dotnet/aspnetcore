using System;
using System.Linq;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Framework.OptionsModel;

namespace MusicStore.Models
{
    public class ApplicationUser : IdentityUser { }

    public class MusicStoreContext : IdentityDbContext<ApplicationUser>
    {
        public MusicStoreContext()
        {

        }

        public DbSet<Album> Albums { get; set; }
        public DbSet<Artist> Artists { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Configure pluralization
            builder.Entity<Album>().ForRelational().Table("Albums");
            builder.Entity<Artist>().ForRelational().Table("Artists");
            builder.Entity<Order>().ForRelational().Table("Orders");
            builder.Entity<Genre>().ForRelational().Table("Genres");
            builder.Entity<CartItem>().ForRelational().Table("CartItems");
            builder.Entity<OrderDetail>().ForRelational().Table("OrderDetails");

            // TODO: Remove this when we start using auto generated values
            builder.Entity<Artist>().Property(a => a.ArtistId).GenerateValueOnAdd(generateValue: false);
            builder.Entity<Album>().Property(a => a.ArtistId).GenerateValueOnAdd(generateValue: false);
            builder.Entity<Genre>().Property(g => g.GenreId).GenerateValueOnAdd(generateValue: false);

            base.OnModelCreating(builder);
        }
    }
}