using Microsoft.AspNet.Identity.InMemory;

namespace MusicStore.Models
{
    public class ApplicationUser : InMemoryUser
    {
    }

    //public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    //{
    //    public ApplicationDbContext()
    //        : base("DefaultConnection")
    //    {
    //    }
    //}
}