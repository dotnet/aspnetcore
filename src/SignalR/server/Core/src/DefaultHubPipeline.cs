using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR
{
    public class DefaultHubPipeline : IHubPipeline
    {
        public ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next)
        {
            return next(invocationContext);
        }
    }
}
