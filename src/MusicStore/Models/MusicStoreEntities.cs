using System;
using System.Collections.Generic;

namespace MvcMusicStore.Models
{
    //public class MusicStoreEntities : DbContext
    //{
    //    public DbSet<Album>     Albums { get; set; }
    //    public DbSet<Genre>     Genres { get; set; }
    //    public DbSet<Artist>    Artists { get; set; }
    //    public DbSet<Cart>      Carts { get; set; }
    //    public DbSet<Order>     Orders { get; set; }
    //    public DbSet<OrderDetail> OrderDetails { get; set; }
    //}

    public class MusicStoreEntities : IDisposable
    {
        public List<Album> Albums { get; set; }
        public List<Genre> Genres { get; set; }
        public List<Artist> Artists { get; set; }
        public List<Cart> Carts { get; set; }
        public List<Order> Orders { get; set; }
        public List<OrderDetail> OrderDetails { get; set; }

        public void SaveChanges()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        internal object Entry(Album album)
        {
            throw new NotImplementedException();
        }
    }
}