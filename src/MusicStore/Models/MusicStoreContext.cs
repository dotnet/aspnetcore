using System;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.InMemory;
using Microsoft.Data.SqlServer;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.ConfigurationModel;

namespace MusicStore.Models
{
    public class MusicStoreContext : DbContext
    {
        private readonly IServiceProvider _serviceProvider;

        public MusicStoreContext(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public DbSet<Album> Albums { get; set; }
        public DbSet<Artist> Artists { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        protected override void OnConfiguring(EntityConfigurationBuilder builder)
        {
            var configuration = _serviceProvider.GetService<IConfiguration>();
#if NET45
            builder.SqlServerConnectionString(configuration.Get("Data:DefaultConnection:ConnectionString"));
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