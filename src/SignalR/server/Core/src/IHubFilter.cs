using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR
{
    public interface IHubFilter
    {
        ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next);

        Task OnConnectedAsync(HubCallerContext context, Func<HubCallerContext, Task> next) => next(context);
        Task OnDisconnectedAsync(HubCallerContext context, Func<HubCallerContext, Task> next) => next(context);
    }
}
