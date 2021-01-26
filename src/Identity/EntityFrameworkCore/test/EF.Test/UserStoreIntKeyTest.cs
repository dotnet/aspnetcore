// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test
{
    public class IntUser : IdentityUser<int>
    {
        public IntUser()
        {
            UserName = Guid.NewGuid().ToString();
        }
    }

    public class IntRole : IdentityRole<int>
    {
        public IntRole()
        {
            Name = Guid.NewGuid().ToString();
        }
    }

    public class UserStoreIntTest : SqlStoreTestBase<IntUser, IntRole, int>
    {
        public UserStoreIntTest(ScratchDatabaseFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void AddEntityFrameworkStoresCanInferKey()
        {
            var services = new ServiceCollection();
            services.AddLogging()
                .AddSingleton(new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().Options));
            // This used to throw
            var builder = services.AddIdentity<IntUser, IntRole>().AddEntityFrameworkStores<TestDbContext>();

            var sp = services.BuildServiceProvider();
            using (var csope = sp.CreateScope())
            {
                Assert.NotNull(sp.GetRequiredService<UserManager<IntUser>>());
                Assert.NotNull(sp.GetRequiredService<RoleManager<IntRole>>());
            }
        }

        [Fact]
        public void AddEntityFrameworkStoresCanInferKeyWithGenericBase()
        {
            var services = new ServiceCollection();
            services.AddLogging()
                .AddSingleton(new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().Options));
            // This used to throw
            var builder = services.AddIdentityCore<IdentityUser<int>>().AddRoles<IdentityRole<int>>().AddEntityFrameworkStores<TestDbContext>();

            var sp = services.BuildServiceProvider();
            using (var csope = sp.CreateScope())
            {
                Assert.NotNull(sp.GetRequiredService<UserManager<IdentityUser<int>>>());
                Assert.NotNull(sp.GetRequiredService<RoleManager<IdentityRole<int>>>());
            }
        }

    }
}