// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Identity.Test;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.InMemory.Test;

public class RoleStoreTest : IClassFixture<InMemoryDatabaseFixture>
{
    private readonly InMemoryDatabaseFixture _fixture;

    public RoleStoreTest(InMemoryDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CanCreateUsingAddRoleManager()
    {
        var manager = TestIdentityFactory.CreateRoleManager(_fixture.Connection);
        Assert.NotNull(manager);
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(new IdentityRole("arole")));
    }

    [Fact]
    public async Task CanCreateRoleWithSingletonManager()
    {
        var services = TestIdentityFactory.CreateTestServices();
        services.AddEntityFrameworkSqlite();
        services.AddSingleton(InMemoryContext.Create(_fixture.Connection));
        services.AddTransient<IRoleStore<IdentityRole>, RoleStore<IdentityRole, InMemoryContext>>();
        services.AddSingleton<RoleManager<IdentityRole>>();
        var provider = services.BuildServiceProvider();
        var manager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        Assert.NotNull(manager);
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(new IdentityRole("someRole")));
    }

    [Fact]
    public async Task RoleStoreMethodsThrowWhenDisposedTest()
    {
        var store = new RoleStore<IdentityRole>(InMemoryContext.Create(_fixture.Connection));
        store.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.FindByIdAsync(null));
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.FindByNameAsync(null));
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.GetRoleIdAsync(null));
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.GetRoleNameAsync(null));
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.SetRoleNameAsync(null, null));
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.CreateAsync(null));
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.UpdateAsync(null));
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.DeleteAsync(null));
    }

    [Fact]
    public async Task RoleStorePublicNullCheckTest()
    {
        Assert.Throws<ArgumentNullException>("context", () => new RoleStore<IdentityRole>(null));
        var store = new RoleStore<IdentityRole>(InMemoryContext.Create(_fixture.Connection));
        await Assert.ThrowsAsync<ArgumentNullException>("role", async () => await store.GetRoleIdAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("role", async () => await store.GetRoleNameAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("role", async () => await store.SetRoleNameAsync(null, null));
        await Assert.ThrowsAsync<ArgumentNullException>("role", async () => await store.CreateAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("role", async () => await store.UpdateAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("role", async () => await store.DeleteAsync(null));
    }

    [Fact]
    public async Task CanUpdateRoleName()
    {
        var manager = TestIdentityFactory.CreateRoleManager(_fixture.Connection);
        var role = new IdentityRole("UpdateRoleName");
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
        Assert.Null(await manager.FindByNameAsync("New"));
        IdentityResultAssert.IsSuccess(await manager.SetRoleNameAsync(role, "New"));
        IdentityResultAssert.IsSuccess(await manager.UpdateAsync(role));
        Assert.NotNull(await manager.FindByNameAsync("New"));
        Assert.Null(await manager.FindByNameAsync("UpdateAsync"));
    }
}
