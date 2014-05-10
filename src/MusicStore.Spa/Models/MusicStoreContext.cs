using System;
using System.Linq;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Framework.DependencyInjection;

namespace MusicStore.Models
{
    public class MusicStoreContext : DbContext
    {
        public MusicStoreContext(IServiceProvider serviceProvider, IOptionsAccessor<MusicStoreDbContextOptions> optionsAccessor)
            : base(serviceProvider, optionsAccessor.Options.BuildConfiguration())
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
            builder.Entity<Album>().ToTable("Albums");
            builder.Entity<Artist>().ToTable("Artists");
            builder.Entity<Order>().ToTable("Orders");
            builder.Entity<Genre>().ToTable("Genres");
            builder.Entity<CartItem>().ToTable("CartItems");
            builder.Entity<OrderDetail>().ToTable("OrderDetails");

            builder.Entity<Album>().Key(a => a.AlbumId);
            builder.Entity<Artist>().Key(a => a.ArtistId);
            builder.Entity<Order>().Key(o => o.OrderId).StorageName("[Order]");
            builder.Entity<Genre>().Key(g => g.GenreId);
            builder.Entity<CartItem>().Key(ci => ci.CartItemId);
            builder.Entity<OrderDetail>().Key(od => od.OrderDetailId);

            builder.Entity<Album>()
                .ForeignKeys(kb =>
                {
                    kb.ForeignKey<Genre>(a => a.GenreId);
                    kb.ForeignKey<Artist>(a => a.ArtistId);
                });
            builder.Entity<OrderDetail>()
                .ForeignKeys(kb =>
                {
                    kb.ForeignKey<Album>(a => a.AlbumId);
                    kb.ForeignKey<Order>(a => a.OrderId);
                });

            var genre = builder.Model.GetEntityType(typeof(Genre));
            var album = builder.Model.GetEntityType(typeof(Album));
            var artist = builder.Model.GetEntityType(typeof(Artist));
            var orderDetail = builder.Model.GetEntityType(typeof(OrderDetail));
            genre.AddNavigation(new Navigation(album.ForeignKeys.Single(k => k.ReferencedEntityType == genre), "Albums"));
            album.AddNavigation(new Navigation(orderDetail.ForeignKeys.Single(k => k.ReferencedEntityType == album), "OrderDetails"));
            album.AddNavigation(new Navigation(album.ForeignKeys.Single(k => k.ReferencedEntityType == genre), "Genre"));
            album.AddNavigation(new Navigation(album.ForeignKeys.Single(k => k.ReferencedEntityType == artist), "Artist"));
        }
    }

    public class MusicStoreDbContextOptions : DbContextOptions
    {

    }
}