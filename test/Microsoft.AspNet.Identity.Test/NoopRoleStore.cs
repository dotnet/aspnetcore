using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity.Test
{
    public class NoopRoleStore : IRoleStore<TestRole>
    {
        public Task CreateAsync(TestRole user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task UpdateAsync(TestRole user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task<string> GetRoleNameAsync(TestRole role, CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult<string>(null);
        }

        public Task SetRoleNameAsync(TestRole role, string roleName, CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(0);
        }

        public Task<TestRole> FindByIdAsync(string roleId, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult<TestRole>(null);
        }

        public Task<TestRole> FindByNameAsync(string userName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult<TestRole>(null);
        }

        public void Dispose()
        {
        }

        public Task DeleteAsync(TestRole user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task<string> GetRoleIdAsync(TestRole role, CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult<string>(null);
        }
    }
}