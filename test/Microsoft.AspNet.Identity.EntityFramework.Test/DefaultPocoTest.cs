// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Identity.Test;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Services;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
using Xunit;

namespace Microsoft.AspNet.Identity.EntityFramework.Test
{
    [TestCaseOrderer("Microsoft.AspNet.Identity.Test.PriorityOrderer", "Microsoft.AspNet.Identity.EntityFramework.Test")]
    public class DefaultPocoTest
    {
        private readonly string ConnectionString = @"Server=(localdb)\v11.0;Database=DefaultSchemaTest" + DateTime.Now.Month + "-" + DateTime.Now.Day + "-" + DateTime.Now.Year + ";Trusted_Connection=True;";
        public IdentityDbContext CreateContext(bool ensureCreated = false)
        {
            var services = UserStoreTest.ConfigureDbServices(ConnectionString);
            var serviceProvider = services.BuildServiceProvider();
            var db = new IdentityDbContext(serviceProvider, 
                serviceProvider.GetService<IOptions<DbContextOptions>>().Options);
            if (ensureCreated)
            {
                db.Database.EnsureCreated();
            }
            return db;
        }

        public void DropDb()
        {
            var db = CreateContext();
            db.Database.EnsureDeleted();
        }

        [TestPriority(-1000)]
        [Fact]
        public void DropDatabaseStart()
        {
            DropDb();
        }

        [Fact]
        public async Task EnsureStartupUsageWorks()
        {
            var context = CreateContext(true);
            var builder = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());

            builder.UseServices(services =>
            {
                UserStoreTest.ConfigureDbServices(ConnectionString, services);
                services.AddIdentitySqlServer();
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

        [TestPriority(10000)]
        [Fact]
        public void DropDatabaseDone()
        {
            DropDb();
        }
    }
}