// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR
{
    public interface IHubFilter
    {
        ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next);

        Task OnConnectedAsync(HubInvocationContext context, Func<HubInvocationContext, Task> next) => next(context);
        Task OnDisconnectedAsync(HubInvocationContext context, Exception exception, Func<HubInvocationContext, Exception, Task> next) => next(context, exception);
    }
}
