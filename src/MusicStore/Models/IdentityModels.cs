using System;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Entity;
using Microsoft.AspNet.Identity.Security;
using Microsoft.Data.Entity;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;

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

    public class ApplicationUser : User { }

    public class ApplicationDbContext : IdentitySqlContext<ApplicationUser> 
    {
        private readonly IConfiguration _configuration;

        public ApplicationDbContext(IServiceProvider serviceProvider, IConfiguration configuration)
            : base(serviceProvider)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptions builder)
        {
            builder.UseSqlServer(_configuration.Get("Data:IdentityConnection:ConnectionString"));
        }
    }
}