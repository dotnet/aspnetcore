// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    internal class HubFilterFactory : IHubFilter
    {
        private readonly Type _hubFilterType;

        public HubFilterFactory(Type hubFilterType)
        {
            _hubFilterType = hubFilterType;
        }

        public ValueTask<HubResult> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<HubResult>> next)
        {
            return GetFilter(invocationContext.ServiceProvider, _hubFilterType).InvokeMethodAsync(invocationContext, next);
        }

        public Task OnConnectedAsync(HubInvocationContext context, Func<HubInvocationContext, Task> next)
        {
            return GetFilter(context.ServiceProvider, _hubFilterType).OnConnectedAsync(context, next);
        }

        public Task OnDisconnectedAsync(HubInvocationContext context, Exception exception, Func<HubInvocationContext, Exception, Task> next)
        {
            return GetFilter(context.ServiceProvider, _hubFilterType).OnDisconnectedAsync(context, exception, next);
        }

        private static IHubFilter GetFilter(IServiceProvider serviceProvider, Type filterType)
        {
            var filter = (IHubFilter)serviceProvider.GetService(filterType);
            if (filter == null)
            {
                filter = (IHubFilter)ActivatorUtilities.CreateInstance(serviceProvider, filterType);
            }

            return filter;
        }
    }
}
