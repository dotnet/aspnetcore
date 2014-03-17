using System.Collections.Generic;
using System.Security.Claims;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Identity.Test
{
    public class RoleManagerTest
    {
        [Fact]
        public void ConstructorThrowsWithNullStore()
        {
            Assert.Throws<ArgumentNullException>("store", () => new RoleManager<TestRole, string>(null));
        }

        [Fact]
        public void RolesQueryableFailWhenStoreNotImplemented()
        {
            var manager = new RoleManager<TestRole, string>(new NoopRoleStore());
            Assert.False(manager.SupportsQueryableRoles);
            Assert.Throws<NotSupportedException>(() => manager.Roles.Count());
        }

        [Fact]
        public void DisposeAfterDisposeDoesNotThrow()
        {
            var manager = new RoleManager<TestRole, string>(new NoopRoleStore());
            manager.Dispose();
            manager.Dispose();
        }

        [Fact]
        public async Task RoleManagerPublicNullChecks()
        {
            Assert.Throws<ArgumentNullException>("store",
                () => new RoleManager<TestRole, string>(null));
            var manager = new RoleManager<TestRole, string>(new NotImplementedStore());
            await Assert.ThrowsAsync<ArgumentNullException>("role", async () => await manager.Create(null));
            await Assert.ThrowsAsync<ArgumentNullException>("role", async () => await manager.Update(null));
            await Assert.ThrowsAsync<ArgumentNullException>("role", async () => await manager.Delete(null));
            await Assert.ThrowsAsync<ArgumentNullException>("roleName", async () => await manager.FindByName(null));
        }

        private class NotImplementedStore : IRoleStore<TestRole, string>
        {

            public Task Create(TestRole role)
            {
                throw new NotImplementedException();
            }

            public Task Update(TestRole role)
            {
                throw new NotImplementedException();
            }

            public Task Delete(TestRole role)
            {
                throw new NotImplementedException();
            }

            public Task<TestRole> FindById(string roleId)
            {
                throw new NotImplementedException();
            }

            public Task<TestRole> FindByName(string roleName)
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