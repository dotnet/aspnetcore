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
            builder.Entity<Album>().ToTable("Albums");
            builder.Entity<Artist>().ToTable("Artists");
            builder.Entity<Order>().ToTable("Orders");
            builder.Entity<Genre>().ToTable("Genres");
            builder.Entity<CartItem>().ToTable("CartItems");
            builder.Entity<OrderDetail>().ToTable("OrderDetails");

            base.OnModelCreating(builder);
        }
    }
}