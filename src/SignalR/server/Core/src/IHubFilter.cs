// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR
{
    public interface IHubFilter
    {
        ValueTask<HubResult> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<HubResult>> next);

        Task OnConnectedAsync(HubCallerContext context, Func<HubCallerContext, Task> next) => next(context);
        Task OnDisconnectedAsync(HubCallerContext context, Func<HubCallerContext, Task> next) => next(context);
    }
}
