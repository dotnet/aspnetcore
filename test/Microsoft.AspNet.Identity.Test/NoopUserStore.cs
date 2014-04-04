using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity.Test
{
    public class NoopUserStore : IUserStore<TestUser>
    {
        public Task<string> GetUserId(TestUser user, CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(user.Id);
        }

        public Task<string> GetUserName(TestUser user, CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(user.UserName);
        }

        public Task Create(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task Update(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task<TestUser> FindById(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult<TestUser>(null);
        }

        public Task<TestUser> FindByName(string userName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult<TestUser>(null);
        }

        public void Dispose()
        {
        }

        public Task Delete(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }
    }
}