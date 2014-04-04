using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Identity.Test
{
    public class RoleManagerTest
    {
        [Fact]
        public void ConstructorThrowsWithNullStore()
        {
            Assert.Throws<ArgumentNullException>("store", () => new RoleManager<TestRole>(null));
        }

        [Fact]
        public void RolesQueryableFailWhenStoreNotImplemented()
        {
            var manager = new RoleManager<TestRole>(new NoopRoleStore());
            Assert.False(manager.SupportsQueryableRoles);
            Assert.Throws<NotSupportedException>(() => manager.Roles.Count());
        }

        [Fact]
        public void DisposeAfterDisposeDoesNotThrow()
        {
            var manager = new RoleManager<TestRole>(new NoopRoleStore());
            manager.Dispose();
            manager.Dispose();
        }

        [Fact]
        public async Task RoleManagerPublicNullChecks()
        {
            Assert.Throws<ArgumentNullException>("store",
                () => new RoleManager<TestRole>(null));
            var manager = new RoleManager<TestRole>(new NotImplementedStore());
            await Assert.ThrowsAsync<ArgumentNullException>("role", async () => await manager.Create(null));
            await Assert.ThrowsAsync<ArgumentNullException>("role", async () => await manager.Update(null));
            await Assert.ThrowsAsync<ArgumentNullException>("role", async () => await manager.Delete(null));
            await Assert.ThrowsAsync<ArgumentNullException>("roleName", async () => await manager.FindByName(null));
            await Assert.ThrowsAsync<ArgumentNullException>("roleName", async () => await manager.RoleExists(null));
        }

        [Fact]
        public async Task RoleStoreMethodsThrowWhenDisposed()
        {
            var manager = new RoleManager<TestRole>(new NoopRoleStore());
            manager.Dispose();
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.FindById(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.FindByName(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.RoleExists(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.Create(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.Update(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.Delete(null));
        }

        private class NotImplementedStore : IRoleStore<TestRole>
        {
            public Task Create(TestRole role, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task Update(TestRole role, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task Delete(TestRole role, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetRoleId(TestRole role, CancellationToken cancellationToken = new CancellationToken())
            {
                throw new NotImplementedException();
            }

            public Task<string> GetRoleName(TestRole role, CancellationToken cancellationToken = new CancellationToken())
            {
                throw new NotImplementedException();
            }

            public Task<TestRole> FindById(string roleId, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<TestRole> FindByName(string roleName, CancellationToken cancellationToken = default(CancellationToken))
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