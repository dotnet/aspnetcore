using System;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Security;

namespace MusicStore.Models
{
    public class ApplicationUserManager : UserManager<ApplicationUser>
    {
        public ApplicationUserManager(IServiceProvider services, IUserStore<ApplicationUser> store, IOptionsAccessor<IdentityOptions> optionsAccessor) : base(services, store, optionsAccessor) { }
    }

    public class ApplicationRoleManager : RoleManager<IdentityRole>
    {
        public ApplicationRoleManager(IServiceProvider services, IRoleStore<IdentityRole> store) : base(services, store) { }
    }

    public class ApplicationSignInManager : SignInManager<ApplicationUserManager, ApplicationUser>
    {
        public ApplicationSignInManager(ApplicationUserManager manager, IContextAccessor<HttpContext> contextAccessor) : base(manager, contextAccessor) { }
    }

    public class ApplicationUser : IdentityUser
    {
    }
}