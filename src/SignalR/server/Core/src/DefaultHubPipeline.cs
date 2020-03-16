using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR
{
    public class DefaultHubPipeline : IHubPipeline
    {
        public Task<object> InvokeHubMethod(Hub hub, HubInvocationContext invocationContext, Func<HubInvocationContext, Task<object>> next)
        {
            return next(invocationContext);
        }

        public object OnAfterIncoming(object result, HubInvocationContext invocationContext)
        {
            return result;
        }

        public bool OnBeforeIncoming(HubInvocationContext invocationContext)
        {
            return true;
        }

        public void OnIncomingError(Exception ex, HubInvocationContext invocationContext)
        {
        }
    }
}
