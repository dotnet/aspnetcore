using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR
{
    public class DefaultHubFilter : IHubFilter
    {
        public ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next)
        {
            return next(invocationContext);
        }
    }
}
