using System;
using Microsoft.AspNet.DependencyInjection;

namespace Microsoft.AspNet.Identity.Test
{
    public class ApplicationUserManager : UserManager<ApplicationUser>
    {
        public ApplicationUserManager(IServiceProvider services, IUserStore<ApplicationUser> store, IOptionsAccessor<IdentityOptions> options) : base(services, store, options) { }
    }

    public class ApplicationRoleManager : RoleManager<IdentityRole>
    {
        public ApplicationRoleManager(IServiceProvider services, IRoleStore<IdentityRole> store) : base(services, store) { }
    }

    public class ApplicationUser : IdentityUser
    {
    }
}