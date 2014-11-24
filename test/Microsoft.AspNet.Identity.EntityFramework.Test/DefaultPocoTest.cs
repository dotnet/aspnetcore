// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Identity.Test;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.AspNet.Security.DataProtection;
using Xunit;
using Microsoft.Framework.Runtime.Infrastructure;

namespace Microsoft.AspNet.Identity.EntityFramework.Test
{
    [TestCaseOrderer("Microsoft.AspNet.Identity.Test.PriorityOrderer", "Microsoft.AspNet.Identity.EntityFramework.Test")]
    public class DefaultPocoTest
    {
        private readonly string ConnectionString = @"Server=(localdb)\mssqllocaldb;Database=DefaultSchemaTest" + DateTime.Now.Month + "-" + DateTime.Now.Day + "-" + DateTime.Now.Year + ";Trusted_Connection=True;";
        public IdentityDbContext CreateContext(bool ensureCreated = false)
        {
            var db = DbUtil.Create(ConnectionString);
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
            var builder = new ApplicationBuilder(CallContextServiceLocator.Locator.ServiceProvider);

            builder.UseServices(services =>
            {
                DbUtil.ConfigureDbServices<IdentityDbContext>(ConnectionString, services);
                services.AddIdentity<IdentityUser, IdentityRole>().AddEntityFrameworkStores<IdentityDbContext>();
            });

            var userStore = builder.ApplicationServices.GetRequiredService<IUserStore<IdentityUser>>();
            var userManager = builder.ApplicationServices.GetRequiredService<UserManager<IdentityUser>>();

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