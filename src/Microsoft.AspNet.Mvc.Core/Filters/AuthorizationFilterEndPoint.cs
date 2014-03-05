using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Filters
{
    public class AuthorizationFilterEndPoint : IAuthorizationFilter
    {
        public bool EndPointCalled { get; private set; }

        public Task Invoke(AuthorizationFilterContext context, Func<Task> next)
        {
            EndPointCalled = true;

            return Task.FromResult(true);
        }
    }
}
