// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Moq;

namespace Microsoft.AspNetCore.Identity.Test;

public class RoleManagerTest
{
    [Fact]
    public async Task CreateCallsStore()
    {
        // Setup
        var normalizer = MockHelpers.MockLookupNormalizer();
        var store = new Mock<IRoleStore<PocoRole>>();
        var role = new PocoRole { Name = "Foo" };
        store.Setup(s => s.CreateAsync(role, CancellationToken.None)).ReturnsAsync(IdentityResult.Success).Verifiable();
        store.Setup(s => s.GetRoleNameAsync(role, CancellationToken.None)).Returns(Task.FromResult(role.Name)).Verifiable();
        store.Setup(s => s.SetNormalizedRoleNameAsync(role, normalizer.NormalizeName(role.Name), CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
        var roleManager = MockHelpers.TestRoleManager(store.Object);

        // Act
        var result = await roleManager.CreateAsync(role);

        // Assert
        Assert.True(result.Succeeded);
        store.VerifyAll();
    }

    [Fact]
    public async Task UpdateCallsStore()
    {
        // Setup
        var normalizer = MockHelpers.MockLookupNormalizer();
        var store = new Mock<IRoleStore<PocoRole>>();
        var role = new PocoRole { Name = "Foo" };
        store.Setup(s => s.UpdateAsync(role, CancellationToken.None)).ReturnsAsync(IdentityResult.Success).Verifiable();
        store.Setup(s => s.GetRoleNameAsync(role, CancellationToken.None)).Returns(Task.FromResult(role.Name)).Verifiable();
        store.Setup(s => s.SetNormalizedRoleNameAsync(role, normalizer.NormalizeName(role.Name), CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
        var roleManager = MockHelpers.TestRoleManager(store.Object);

        // Act
        var result = await roleManager.UpdateAsync(role);

        // Assert
        Assert.True(result.Succeeded);
        store.VerifyAll();
    }

    [Fact]
    public void RolesQueryableFailWhenStoreNotImplemented()
    {
        var manager = CreateRoleManager(new NoopRoleStore());
        Assert.False(manager.SupportsQueryableRoles);
        Assert.Throws<NotSupportedException>(() => manager.Roles.Count());
    }

    [Fact]
    public async Task FindByNameCallsStoreWithNormalizedName()
    {
        // Setup
        var normalizer = MockHelpers.MockLookupNormalizer();
        var store = new Mock<IRoleStore<PocoRole>>();
        var role = new PocoRole { Name = "Foo" };
        store.Setup(s => s.FindByNameAsync(normalizer.NormalizeName("Foo"), CancellationToken.None)).Returns(Task.FromResult(role)).Verifiable();
        var manager = MockHelpers.TestRoleManager(store.Object);

        // Act
        var result = await manager.FindByNameAsync(role.Name);

        // Assert
        Assert.Equal(role, result);
        store.VerifyAll();
    }

    [Fact]
    public async Task CanFindByNameCallsStoreWithoutNormalizedName()
    {
        // Setup
        var store = new Mock<IRoleStore<PocoRole>>();
        var role = new PocoRole { Name = "Foo" };
        store.Setup(s => s.FindByNameAsync(role.Name, CancellationToken.None)).Returns(Task.FromResult(role)).Verifiable();
        var manager = MockHelpers.TestRoleManager(store.Object);
        manager.KeyNormalizer = null;

        // Act
        var result = await manager.FindByNameAsync(role.Name);

        // Assert
        Assert.Equal(role, result);
        store.VerifyAll();
    }

    [Fact]
    public async Task RoleExistsCallsStoreWithNormalizedName()
    {
        // Setup
        var normalizer = MockHelpers.MockLookupNormalizer();
        var store = new Mock<IRoleStore<PocoRole>>();
        var role = new PocoRole { Name = "Foo" };
        store.Setup(s => s.FindByNameAsync(normalizer.NormalizeName("Foo"), CancellationToken.None)).Returns(Task.FromResult(role)).Verifiable();
        var manager = MockHelpers.TestRoleManager(store.Object);

        // Act
        var result = await manager.RoleExistsAsync(role.Name);

        // Assert
        Assert.True(result);
        store.VerifyAll();
    }

    [Fact]
    public void DisposeAfterDisposeDoesNotThrow()
    {
        var manager = CreateRoleManager(new NoopRoleStore());
        manager.Dispose();
        manager.Dispose();
    }

    [Fact]
    public async Task RoleManagerPublicNullChecks()
    {
        Assert.Throws<ArgumentNullException>("store",
            () => new RoleManager<PocoRole>(null, null, null, null, null));
        var manager = CreateRoleManager(new NotImplementedStore());
        await Assert.ThrowsAsync<ArgumentNullException>("role", async () => await manager.CreateAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("role", async () => await manager.UpdateAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("role", async () => await manager.DeleteAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("roleName", async () => await manager.FindByNameAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>("roleName", async () => await manager.RoleExistsAsync(null));
    }

    [Fact]
    public async Task RoleStoreMethodsThrowWhenDisposed()
    {
        var manager = CreateRoleManager(new NoopRoleStore());
        manager.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.FindByIdAsync(null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.FindByNameAsync(null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.RoleExistsAsync(null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.CreateAsync(null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.UpdateAsync(null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.DeleteAsync(null));
    }

    private static RoleManager<PocoRole> CreateRoleManager(IRoleStore<PocoRole> roleStore)
    {
        return MockHelpers.TestRoleManager(roleStore);
    }

    private class NotImplementedStore : IRoleStore<PocoRole>
    {
        public Task<IdentityResult> CreateAsync(PocoRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<IdentityResult> UpdateAsync(PocoRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<IdentityResult> DeleteAsync(PocoRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<string> GetRoleIdAsync(PocoRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<string> GetRoleNameAsync(PocoRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task SetRoleNameAsync(PocoRole role, string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<PocoRole> FindByIdAsync(string roleId, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<PocoRole> FindByNameAsync(string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Task<string> GetNormalizedRoleNameAsync(PocoRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task SetNormalizedRoleNameAsync(PocoRole role, string normalizedName, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}
