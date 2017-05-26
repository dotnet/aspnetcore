// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test
{
    public class IntUser : IdentityUser<int>
    {
        public IntUser() => UserName = Guid.NewGuid().ToString();
    }

    public class IntRole : IdentityRole<int>
    {
        public IntRole() => Name = Guid.NewGuid().ToString();
    }

    public class UserStoreIntTest : SqlStoreTestBase<IntUser, IntRole, int>
    {
        public UserStoreIntTest(ScratchDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public void AddEntityFrameworkStoresCanInferKey()
        {
            // This used to throw
            var builder = new ServiceCollection().AddIdentity<IntUser, IntRole>().AddEntityFrameworkStores<TestDbContext>();
        }
    }

    public class UserStoreIntV1Test : SqlStoreTestBaseV1<IntUser, IntRole, int>
    {
        public UserStoreIntV1Test(ScratchDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public void AddEntityFrameworkStoresCanInferKey()
        {
            // This used to throw
            var builder = new ServiceCollection().AddIdentity<IntUser, IntRole>().AddEntityFrameworkStores<TestDbContext>();
        }
    }

}