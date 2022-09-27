// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test;

public class GuidUser : IdentityUser<Guid>
{
    public GuidUser()
    {
        Id = Guid.NewGuid();
        UserName = Id.ToString();
    }
}

public class GuidRole : IdentityRole<Guid>
{
    public GuidRole()
    {
        Id = Guid.NewGuid();
        Name = Id.ToString();
    }
}

public class UserStoreGuidTest : SqlStoreTestBase<GuidUser, GuidRole, Guid>
{
    public UserStoreGuidTest(ScratchDatabaseFixture fixture)
        : base(fixture)
    {
    }

    public class ApplicationUserStore : UserStore<GuidUser, GuidRole, TestDbContext, Guid>
    {
        public ApplicationUserStore(TestDbContext context) : base(context) { }
    }

    public class ApplicationRoleStore : RoleStore<GuidRole, TestDbContext, Guid>
    {
        public ApplicationRoleStore(TestDbContext context) : base(context) { }
    }

    protected override void AddUserStore(IServiceCollection services, object context = null)
    {
        services.AddSingleton<IUserStore<GuidUser>>(new ApplicationUserStore((TestDbContext)context));
    }

    protected override void AddRoleStore(IServiceCollection services, object context = null)
    {
        services.AddSingleton<IRoleStore<GuidRole>>(new ApplicationRoleStore((TestDbContext)context));
    }

    [Fact]
    public void AddEntityFrameworkStoresCanInferKey()
    {
        var services = new ServiceCollection();
        services.AddLogging()
            .AddSingleton(new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().Options));
        // This used to throw
        var builder = services.AddIdentity<GuidUser, GuidRole>().AddEntityFrameworkStores<TestDbContext>();

        var sp = services.BuildServiceProvider();
        using (var csope = sp.CreateScope())
        {
            Assert.NotNull(sp.GetRequiredService<UserManager<GuidUser>>());
            Assert.NotNull(sp.GetRequiredService<RoleManager<GuidRole>>());
        }
    }

    [Fact]
    public void AddEntityFrameworkStoresCanInferKeyWithGenericBase()
    {
        var services = new ServiceCollection();
        services.AddLogging()
            .AddSingleton(new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().Options));
        // This used to throw
        var builder = services.AddIdentityCore<IdentityUser<Guid>>().AddRoles<IdentityRole<Guid>>().AddEntityFrameworkStores<TestDbContext>();

        var sp = services.BuildServiceProvider();
        using (var csope = sp.CreateScope())
        {
            Assert.NotNull(sp.GetRequiredService<UserManager<IdentityUser<Guid>>>());
            Assert.NotNull(sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>());
        }
    }
}
