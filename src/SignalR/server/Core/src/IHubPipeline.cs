using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR
{
    public interface IHubPipeline
    {
        Task<object> InvokeHubMethod(Hub hub, HubInvocationContext invocationContext, Func<HubInvocationContext, Task<object>> next);

        public bool OnBeforeIncoming(HubInvocationContext invocationContext);
        public void OnIncomingError(Exception ex, HubInvocationContext invocationContext);

        public object OnAfterIncoming(object result, HubInvocationContext invocationContext);
    }
}
