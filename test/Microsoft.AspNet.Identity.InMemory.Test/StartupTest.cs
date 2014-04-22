using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.DependencyInjection.Fallback;
using Microsoft.AspNet.PipelineCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Identity.InMemory.Test
{
    public class StartupTest
    {

        [Fact]
        public async Task EnsureStartupUsageWorks()
        {
            IBuilder builder = new Builder(new ServiceCollection().BuildServiceProvider());

            builder.UseServices(services => services.AddIdentity<ApplicationUser>(s =>
            {
                s.UseUserStore(() => new InMemoryUserStore<ApplicationUser>());
                s.UseUserManager<ApplicationUserManager>();
                s.UseRoleStore(() => new InMemoryRoleStore<IdentityRole>());
                s.UseRoleManager<ApplicationRoleManager>();
            }));

            var userStore = builder.ApplicationServices.GetService<IUserStore<ApplicationUser>>();
            var roleStore = builder.ApplicationServices.GetService<IRoleStore<IdentityRole>>();
            var userManager = builder.ApplicationServices.GetService<ApplicationUserManager>();
            var roleManager = builder.ApplicationServices.GetService<ApplicationRoleManager>();

            Assert.NotNull(userStore);
            Assert.NotNull(userManager);
            Assert.NotNull(roleStore);
            Assert.NotNull(roleManager);

            await CreateAdminUser(builder.ApplicationServices);
        }

        private static async Task CreateAdminUser(IServiceProvider serviceProvider)
        {
            const string userName = "admin";
            const string roleName = "Admins";
            const string password = "1qaz@WSX";
            var userManager = serviceProvider.GetService<ApplicationUserManager>();
            var roleManager = serviceProvider.GetService<ApplicationRoleManager>();

            var user = new ApplicationUser { UserName = userName };
            IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user, password));
            IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(new IdentityRole { Name = roleName }));
            IdentityResultAssert.IsSuccess(await userManager.AddToRoleAsync(user, roleName));
        }


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
}