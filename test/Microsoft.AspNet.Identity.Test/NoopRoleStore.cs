using System.Collections.Generic;
using System.Security.Claims;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity.Test
{
        public class NoopRoleStore : IRoleStore<TestRole, string>
        {
            public Task Create(TestRole user)
            {
                return Task.FromResult(0);
            }

            public Task Update(TestRole user)
            {
                return Task.FromResult(0);
            }

            public Task<TestRole> FindById(string roleId)
            {
                return Task.FromResult<TestRole>(null);
            }

            public Task<TestRole> FindByName(string userName)
            {
                return Task.FromResult<TestRole>(null);
            }

            public void Dispose()
            {
            }

            public Task Delete(TestRole user)
            {
                return Task.FromResult(0);
            }
        }

}
