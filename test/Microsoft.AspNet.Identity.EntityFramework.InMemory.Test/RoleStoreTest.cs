// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity.Test;
using Microsoft.Framework.DependencyInjection;
using Xunit;

namespace Microsoft.AspNet.Identity.EntityFramework.InMemory.Test
{
    public class RoleStoreTest
    {
        [Fact]
        public async Task CanCreateUsingAddRoleManager()
        {
            var services = new ServiceCollection();
            services.AddEntityFramework().AddInMemoryStore();
            var store = new RoleStore<IdentityRole>(new InMemoryContext());
            services.AddInstance<IRoleStore<IdentityRole>>(store);
            services.AddIdentity<IdentityUser,IdentityRole>();
            var provider = services.BuildServiceProvider();
            var manager = provider.GetRequiredService<RoleManager<IdentityRole>>();
            Assert.NotNull(manager);
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(new IdentityRole("arole")));
        }
        [Fact]
        public async Task CanCreateRoleWithSingletonManager()
        {
            var services = new ServiceCollection();
            services.AddEntityFramework().AddInMemoryStore();
            services.AddTransient<InMemoryContext>();
            services.AddTransient<IRoleStore<IdentityRole>, RoleStore<IdentityRole, InMemoryContext>>();
            services.AddIdentity<IdentityUser, IdentityRole>();
            services.AddSingleton<RoleManager<IdentityRole>>();
            var provider = services.BuildServiceProvider();
            var manager = provider.GetRequiredService<RoleManager<IdentityRole>>();
            Assert.NotNull(manager);
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(new IdentityRole("someRole")));
        }

        [Fact]
        public async Task RoleStoreMethodsThrowWhenDisposedTest()
        {
            var store = new RoleStore<IdentityRole>(new InMemoryContext());
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
            var store = new RoleStore<IdentityRole>(new InMemoryContext());
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
            var manager = TestIdentityFactory.CreateRoleManager();
            var role = new IdentityRole("UpdateRoleName");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            Assert.Null(await manager.FindByNameAsync("New"));
            IdentityResultAssert.IsSuccess(await manager.SetRoleNameAsync(role, "New"));
            IdentityResultAssert.IsSuccess(await manager.UpdateAsync(role));
            Assert.NotNull(await manager.FindByNameAsync("New"));
            Assert.Null(await manager.FindByNameAsync("UpdateAsync"));
        }
    }
}
