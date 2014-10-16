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
        public MusicStoreContext(IServiceProvider serviceProvider, IOptions<DbContextOptions<MusicStoreContext>> optionsAccessor)
            : base(serviceProvider, optionsAccessor.Options)
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
            // TODO: All this configuration needs to be done manually right now.
            //       We can remove this once EF supports the conventions again.
            builder.Entity<Album>().ForRelational().Table("Albums");
            builder.Entity<Artist>().ForRelational().Table("Artists");
            builder.Entity<Order>().ForRelational().Table("Orders");
            builder.Entity<Genre>().ForRelational().Table("Genres");
            builder.Entity<CartItem>().ForRelational().Table("CartItems");
            builder.Entity<OrderDetail>().ForRelational().Table("OrderDetails");

            builder.Entity<Album>().Key(a => a.AlbumId);
            builder.Entity<Artist>().Key(a => a.ArtistId);

            builder.Entity<Order>(b =>
            {
                b.Key(o => o.OrderId);
                b.Property(o => o.OrderId)
                    .ForRelational()
                    .Column("[Order]");
            });

            builder.Entity<Genre>().Key(g => g.GenreId);
            builder.Entity<CartItem>().Key(ci => ci.CartItemId);
            builder.Entity<OrderDetail>().Key(od => od.OrderDetailId);

            // TODO: Remove this when we start using auto generated values
            builder.Entity<Artist>().Property(a => a.ArtistId).GenerateValuesOnAdd(generateValues: false);
            builder.Entity<Album>().Property(a => a.ArtistId).GenerateValuesOnAdd(generateValues: false);
            builder.Entity<Genre>().Property(g => g.GenreId).GenerateValuesOnAdd(generateValues: false);

            builder.Entity<Album>(b =>
            {
                b.ForeignKey<Genre>(a => a.GenreId);
                b.ForeignKey<Artist>(a => a.ArtistId);
            });

            builder.Entity<OrderDetail>(b =>
            {
                b.ForeignKey<Album>(a => a.AlbumId);
                b.ForeignKey<Order>(a => a.OrderId);
            });

            var genre = builder.Model.GetEntityType(typeof(Genre));
            var album = builder.Model.GetEntityType(typeof(Album));
            var artist = builder.Model.GetEntityType(typeof(Artist));
            var orderDetail = builder.Model.GetEntityType(typeof(OrderDetail));
            genre.AddNavigation("Albums", album.ForeignKeys.Single(k => k.ReferencedEntityType == genre), pointsToPrincipal: false);
            album.AddNavigation("OrderDetails", orderDetail.ForeignKeys.Single(k => k.ReferencedEntityType == album), pointsToPrincipal: false);
            album.AddNavigation("Genre", album.ForeignKeys.Single(k => k.ReferencedEntityType == genre), pointsToPrincipal: true);
            album.AddNavigation("Artist", album.ForeignKeys.Single(k => k.ReferencedEntityType == artist), pointsToPrincipal: true);

            base.OnModelCreating(builder);
        }
    }
}