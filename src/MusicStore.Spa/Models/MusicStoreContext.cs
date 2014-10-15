using System;
using System.Linq;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Framework.OptionsModel;

namespace MusicStore.Models
{
    public class MusicStoreContext : DbContext
    {
        public MusicStoreContext(IServiceProvider serviceProvider, IOptions<MusicStoreDbContextOptions> optionsAccessor)
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
            builder.Entity<Album>().ToTable("Albums");
            builder.Entity<Artist>().ToTable("Artists");
            builder.Entity<Order>().ToTable("Orders");
            builder.Entity<Genre>().ToTable("Genres");
            builder.Entity<CartItem>().ToTable("CartItems");
            builder.Entity<OrderDetail>().ToTable("OrderDetails");

            builder.Entity<Album>().Key(a => a.AlbumId);
            builder.Entity<Artist>().Key(a => a.ArtistId);

            builder.Entity<Order>(
                b =>
                {
                    b.Key(o => o.OrderId);
                    b.Property(o => o.OrderId)
                        .ColumnName("[Order]");
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
        }
    }

    public class MusicStoreDbContextOptions : DbContextOptions
    {

    }
}