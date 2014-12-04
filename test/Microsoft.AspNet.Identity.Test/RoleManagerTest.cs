// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Identity.Test
{
    public class RoleManagerTest
    {
        [Fact]
        public void RolesQueryableFailWhenStoreNotImplemented()
        {
            var manager = CreateRoleManager(new NoopRoleStore());
            Assert.False(manager.SupportsQueryableRoles);
            Assert.Throws<NotSupportedException>(() => manager.Roles.Count());
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
                () => new RoleManager<TestRole>(null, null));
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

        private static RoleManager<TestRole> CreateRoleManager(IRoleStore<TestRole> roleStore)
        {
            var v = new List<IRoleValidator<TestRole>>();
            v.Add(new RoleValidator<TestRole>());
            return new RoleManager<TestRole>(roleStore, v);
        }

        private class NotImplementedStore : IRoleStore<TestRole>
        {
            public Task CreateAsync(TestRole role, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task UpdateAsync(TestRole role, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task DeleteAsync(TestRole role, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetRoleIdAsync(TestRole role, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetRoleNameAsync(TestRole role, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetRoleNameAsync(TestRole role, string roleName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<TestRole> FindByIdAsync(string roleId, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<TestRole> FindByNameAsync(string roleName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }
        }
    }
}