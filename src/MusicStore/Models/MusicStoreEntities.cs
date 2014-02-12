using Microsoft.Data.Entity;

namespace MvcMusicStore.Models
{
    public class MusicStoreEntities : EntityContext
    {
        public MusicStoreEntities()
            : base(null) // TODO: Fix after discussion of which patterns to use here
        {
        }

        public EntitySet<Album> Albums { get; set; }
        public EntitySet<Genre> Genres { get; set; }
        public EntitySet<Artist> Artists { get; set; }
        public EntitySet<Cart> Carts { get; set; }
        public EntitySet<Order> Orders { get; set; }
    }
}