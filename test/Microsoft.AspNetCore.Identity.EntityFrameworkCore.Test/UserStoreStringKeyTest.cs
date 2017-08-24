// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test
{
    public class StringUser : IdentityUser
    {
        public StringUser()
        {
            Id = Guid.NewGuid().ToString();
            UserName = Id;
        }
    }

    public class StringRole : IdentityRole<string>
    {
        public StringRole()
        {
            Id = Guid.NewGuid().ToString();
            Name = Id;
        }
    }

    public class UserStoreStringKeyTest : SqlStoreTestBase<StringUser, StringRole, string>
    {
        public UserStoreStringKeyTest(ScratchDatabaseFixture fixture)
            : base(fixture)
        { }

        [Fact]
        public void AddEntityFrameworkStoresCanInferKey()
        {
            var services = new ServiceCollection();
            // This used to throw
            var builder = services.AddIdentity<StringUser, StringRole>().AddEntityFrameworkStores<TestDbContext>();
        }

        [Fact]
        public void AddEntityFrameworkStoresCanInferKeyWithGenericBase()
        {
            var services = new ServiceCollection();
            // This used to throw
            var builder = services.AddIdentity<IdentityUser<string>, IdentityRole<string>>().AddEntityFrameworkStores<TestDbContext>();
        }

    }
}