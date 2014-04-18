using System;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.InMemory;
using Microsoft.Data.SqlServer;

namespace MusicStore.Models
{
    public class MusicStoreContext : EntityContext
    {
        public MusicStoreContext(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public EntitySet<Album> Albums { get; set; }
        public EntitySet<Artist> Artists { get; set; }
        public EntitySet<Order> Orders { get; set; }
        public EntitySet<Genre> Genres { get; set; }
        public EntitySet<CartItem> CartItems { get; set; }
        public EntitySet<OrderDetail> OrderDetails { get; set; }

        protected override void OnConfiguring(EntityConfigurationBuilder builder)
        {
#if NET45
            builder.SqlServerConnectionString(@"Server=(localdb)\v11.0;Database=MusicStore;Trusted_Connection=True;");
#else
            builder.UseInMemoryStore(persist: true);
#endif
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Album>().Key(a => a.AlbumId);
            builder.Entity<Artist>().Key(a => a.ArtistId);
            builder.Entity<Order>().Key(o => o.OrderId).StorageName("[Order]");
            builder.Entity<Genre>().Key(g => g.GenreId);
            builder.Entity<CartItem>().Key(c => c.CartItemId);
            builder.Entity<OrderDetail>().Key(o => o.OrderDetailId);
        }
    }
}