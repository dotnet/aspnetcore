using System;

namespace Microsoft.AspNetCore.SignalR
{
    public interface IHubPipeline
    {
        public bool OnBeforeIncoming(HubInvocationContext invocationContext);
        public void OnIncomingError(Exception ex, HubInvocationContext invocationContext);

        public object OnAfterIncoming(object result, HubInvocationContext invocationContext);
    }
}
