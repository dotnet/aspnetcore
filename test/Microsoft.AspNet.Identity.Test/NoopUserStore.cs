using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity.Test
{
        public class NoopUserStore : IUserStore<TestUser, string>
        {
            public Task Create(TestUser user)
            {
                return Task.FromResult(0);
            }

            public Task Update(TestUser user)
            {
                return Task.FromResult(0);
            }

            public Task<TestUser> FindById(string userId)
            {
                return Task.FromResult<TestUser>(null);
            }

            public Task<TestUser> FindByName(string userName)
            {
                return Task.FromResult<TestUser>(null);
            }

            public void Dispose()
            {
            }

            public Task Delete(TestUser user)
            {
                return Task.FromResult(0);
            }
        }

}
