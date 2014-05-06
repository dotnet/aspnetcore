// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Xunit;

namespace Microsoft.AspNet.Identity.Test
{
    public class RoleManagerTest
    {
        [Fact]
        public void RolesQueryableFailWhenStoreNotImplemented()
        {
            var manager = new RoleManager<TestRole>(new ServiceCollection().BuildServiceProvider(), new NoopRoleStore());
            Assert.False(manager.SupportsQueryableRoles);
            Assert.Throws<NotSupportedException>(() => manager.Roles.Count());
        }

        [Fact]
        public void DisposeAfterDisposeDoesNotThrow()
        {
            var manager = new RoleManager<TestRole>(new ServiceCollection().BuildServiceProvider(), new NoopRoleStore());
            manager.Dispose();
            manager.Dispose();
        }

        [Fact]
        public async Task RoleManagerPublicNullChecks()
        {
            var provider = new ServiceCollection().BuildServiceProvider();
            Assert.Throws<ArgumentNullException>("store",
                () => new RoleManager<TestRole>(provider, null));
            Assert.Throws<ArgumentNullException>("services",
                () => new RoleManager<TestRole>(null, new NotImplementedStore()));
            var manager = new RoleManager<TestRole>(provider, new NotImplementedStore());
            await Assert.ThrowsAsync<ArgumentNullException>("role", async () => await manager.CreateAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("role", async () => await manager.UpdateAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("role", async () => await manager.DeleteAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("roleName", async () => await manager.FindByNameAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("roleName", async () => await manager.RoleExistsAsync(null));
        }

        [Fact]
        public async Task RoleStoreMethodsThrowWhenDisposed()
        {
            var manager = new RoleManager<TestRole>(new ServiceCollection().BuildServiceProvider(), new NoopRoleStore());
            manager.Dispose();
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.FindByIdAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.FindByNameAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.RoleExistsAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.CreateAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.UpdateAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.DeleteAsync(null));
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

            public Task<string> GetRoleIdAsync(TestRole role, CancellationToken cancellationToken = new CancellationToken())
            {
                throw new NotImplementedException();
            }

            public Task<string> GetRoleNameAsync(TestRole role, CancellationToken cancellationToken = new CancellationToken())
            {
                throw new NotImplementedException();
            }

            public Task SetRoleNameAsync(TestRole role, string roleName, CancellationToken cancellationToken = new CancellationToken())
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