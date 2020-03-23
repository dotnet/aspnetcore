using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR
{
    public class DefaultHubPipeline : IHubPipeline
    {
        public ValueTask<object> InvokeHubMethod(Hub hub, HubInvocationContext invocationContext, Func<HubInvocationContext, Task<object>> next)
        {
            return new ValueTask<object>(next(invocationContext));
        }
    }
}
