using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Data.Entity.SqlServer;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;

namespace IdentitySample.Models
{
    public static class SampleData
    {
        public static async Task InitializeIdentityDatabaseAsync(IServiceProvider serviceProvider)
        {
            using (var db = serviceProvider.GetRequiredService<ApplicationDbContext>())
            {
                var sqlServerDatabase = db.Database as SqlServerDatabase;
                if (sqlServerDatabase != null)
                {
                    if (await sqlServerDatabase.EnsureCreatedAsync())
                    {
                        await CreateAdminUser(serviceProvider);
                    }
                }
                else
                {
                    await CreateAdminUser(serviceProvider);
                }
            }
        }

        /// <summary>
        /// Creates a store manager user who can manage the inventory.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        private static async Task CreateAdminUser(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<IOptions<IdentityDbContextOptions>>().Options;
            const string adminRole = "Administrator";

            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            if (!await roleManager.RoleExistsAsync(adminRole))
            {
                await roleManager.CreateAsync(new IdentityRole(adminRole));
            }

            var user = await userManager.FindByNameAsync(options.DefaultAdminUserName);
            if (user == null)
            {
                user = new ApplicationUser { UserName = options.DefaultAdminUserName };
                await userManager.CreateAsync(user, options.DefaultAdminPassword);
                await userManager.AddToRoleAsync(user, adminRole);
                await userManager.AddClaimAsync(user, new Claim("ManageStore", "Allowed"));
            }
        }
    }
}