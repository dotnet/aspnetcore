using System;
using Microsoft.AspNet.Identity;

namespace MusicStore.Models
{
    public class ApplicationUserManager : UserManager<ApplicationUser>
    {
        public ApplicationUserManager(IServiceProvider services) : base(services) { }
    }

    public class ApplicationRoleManager : RoleManager<IdentityRole>
    {
        public ApplicationRoleManager(IServiceProvider services) : base(services) { }
    }

    public class ApplicationUser : IdentityUser
    {
    }
}