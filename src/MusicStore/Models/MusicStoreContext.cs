// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.InMemory;
using Microsoft.Data.SqlServer;

namespace MusicStore.Models
{
    public class MusicStoreContext : EntityContext
    {
        private static EntityConfiguration _config;

        public EntitySet<Album> Albums { get; set; }
        public EntitySet<Artist> Artists { get; set; }
        public EntitySet<Order> Orders { get; set; }
        public EntitySet<Genre> Genres { get; set; }
        public EntitySet<Cart> Carts { get; set; }
        public EntitySet<OrderDetail> OrderDetails { get; set; }

        public MusicStoreContext()
            : base(GetConfiguration())
        { }

        // TODO Not using OnModelCreating and OnConfiguring because model is not cached and that breaks InMemoryDataStore 
        //      because IEntityType is a different instance for the same type between context instances
        private static EntityConfiguration GetConfiguration()
        {
            if (_config == null)
            {
                var model = new Model();
                var modelBuilder = new ModelBuilder(model);
                modelBuilder.Entity<Album>().Key(a => a.AlbumId);
                modelBuilder.Entity<Artist>().Key(a => a.ArtistId);
                modelBuilder.Entity<Order>().Key(o => o.OrderId).StorageName("[Order]");
                modelBuilder.Entity<Genre>().Key(g => g.GenreId);
                modelBuilder.Entity<Cart>().Key(c => c.RecordId);
                modelBuilder.Entity<OrderDetail>().Key(o => o.OrderDetailId);
                new SimpleTemporaryConvention().Apply(model);

                var builder = new EntityConfigurationBuilder();

                // TODO [EF] Remove once SQL Client is available on K10
#if NET45
                builder.UseSqlServer(@"Server=(localdb)\v11.0;Database=MusicStore;Trusted_Connection=True;");
#else
                builder.UseDataStore(new InMemoryDataStore());
#endif
                builder.UseModel(model);

                _config = builder.BuildConfiguration();
            }

            return _config;
        }
    }
}