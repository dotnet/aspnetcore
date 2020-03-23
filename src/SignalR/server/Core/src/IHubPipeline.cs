using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR
{
    public interface IHubPipeline
    {
        ValueTask<object> InvokeHubMethod(Hub hub, HubInvocationContext invocationContext, Func<HubInvocationContext, Task<object>> next);

        Task OnConnectedAsync(HubCallerContext context, Func<Task> next) => next();
        Task OnDisconnectedAsync(HubCallerContext context, Func<Task> next) => next();
    }
}
