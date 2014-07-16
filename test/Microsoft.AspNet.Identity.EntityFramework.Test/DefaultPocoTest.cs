// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Identity.Test;
using Microsoft.Data.Entity;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Identity.EntityFramework.Test
{
    public class DefaultPocoTest
    {
        private const string ConnectionString = @"Server=(localdb)\v11.0;Database=DefaultPocoTest;Trusted_Connection=True;";
        public static IdentityDbContext CreateContext(bool delete = false)
        {
            var services = new ServiceCollection();
            services.AddEntityFramework().AddSqlServer();
            var serviceProvider = services.BuildServiceProvider();

            var db = new IdentityDbContext(serviceProvider, ConnectionString);
            if (delete)
            {
                db.Database.EnsureDeleted();
            }
            db.Database.EnsureCreated();
            return db;
        }

        public static void EnsureDatabase()
        {
            CreateContext();
        }

        [Fact]
        public async Task EnsureStartupUsageWorks()
        {
            var context = CreateContext(true);
            IBuilder builder = new Builder.Builder(new ServiceCollection().BuildServiceProvider());

            builder.UseServices(services =>
            {
                services.AddEntityFramework().AddSqlServer();
                services.AddIdentitySqlServer();
                services.SetupOptions<DbContextOptions>(options =>
                    options.UseSqlServer(ConnectionString));
                // todo: constructor resolution doesn't work well with IdentityDbContext since it has 4 constructors
                services.AddInstance(context);
            });

            var userStore = builder.ApplicationServices.GetService<IUserStore<IdentityUser>>();
            var userManager = builder.ApplicationServices.GetService<UserManager<IdentityUser>>();

            Assert.NotNull(userStore);
            Assert.NotNull(userManager);

            const string userName = "admin";
            const string password = "1qaz@WSX";
            var user = new IdentityUser { UserName = userName };
            IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user, password));
            IdentityResultAssert.IsSuccess(await userManager.DeleteAsync(user));
        }
    }
}