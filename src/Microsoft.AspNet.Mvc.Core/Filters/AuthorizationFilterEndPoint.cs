using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Filters
{
    // This one lives in the FilterDescriptors namespace, and only intended to be consumed by folks that rewrite the action invoker.
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
