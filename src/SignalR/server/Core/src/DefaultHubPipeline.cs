using System;

namespace Microsoft.AspNetCore.SignalR
{
    public class DefaultHubPipeline : IHubPipeline
    {
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
