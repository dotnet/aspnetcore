// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test;

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
        services.AddLogging()
            .AddSingleton(new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().Options));
        // This used to throw
        var builder = services.AddIdentity<StringUser, StringRole>().AddEntityFrameworkStores<TestDbContext>();

        var sp = services.BuildServiceProvider();
        using (var csope = sp.CreateScope())
        {
            Assert.NotNull(sp.GetRequiredService<UserManager<StringUser>>());
            Assert.NotNull(sp.GetRequiredService<RoleManager<StringRole>>());
        }
    }

    [Fact]
    public void AddEntityFrameworkStoresCanInferKeyWithGenericBase()
    {
        var services = new ServiceCollection();
        services.AddLogging()
            .AddSingleton(new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().Options));
        // This used to throw
        var builder = services.AddIdentityCore<IdentityUser<string>>().AddRoles<IdentityRole<string>>().AddEntityFrameworkStores<TestDbContext>();

        var sp = services.BuildServiceProvider();
        using (var csope = sp.CreateScope())
        {
            Assert.NotNull(sp.GetRequiredService<UserManager<IdentityUser<string>>>());
            Assert.NotNull(sp.GetRequiredService<RoleManager<IdentityRole<string>>>());
        }
    }
}
