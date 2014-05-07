using System.Data.Entity;

namespace MvcMusicStore.Models
{
    public class MusicStoreEntities : DbContext
    {
        public MusicStoreEntities()
            : base("name=MusicStoreEntities")
        {
            Configuration.ProxyCreationEnabled = false;
            Configuration.LazyLoadingEnabled = false;
        }

        public DbSet<Album>     Albums { get; set; }
        public DbSet<Genre>     Genres { get; set; }
        public DbSet<Artist>    Artists { get; set; }
        public DbSet<Cart>      Carts { get; set; }
        public DbSet<Order>     Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
    }
}