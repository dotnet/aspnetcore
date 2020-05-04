// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    internal class HubFilterFactory<T> : IHubFilter where T : IHubFilter
    {
        private readonly ObjectFactory _objectFactory;

        public HubFilterFactory()
        {
            _objectFactory = ActivatorUtilities.CreateFactory(typeof(T), Array.Empty<Type>());
        }

        public ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next)
        {
            return GetFilter(_objectFactory, invocationContext.ServiceProvider).InvokeMethodAsync(invocationContext, next);
        }

        public Task OnConnectedAsync(HubInvocationContext context, Func<HubInvocationContext, Task> next)
        {
            return GetFilter(_objectFactory, context.ServiceProvider).OnConnectedAsync(context, next);
        }

        public Task OnDisconnectedAsync(HubInvocationContext context, Exception exception, Func<HubInvocationContext, Exception, Task> next)
        {
            return GetFilter(_objectFactory, context.ServiceProvider).OnDisconnectedAsync(context, exception, next);
        }

        private static IHubFilter GetFilter(ObjectFactory objectFactory, IServiceProvider serviceProvider)
        {
            var filter = (IHubFilter)serviceProvider.GetService<T>();
            if (filter == null)
            {
                filter = (IHubFilter)objectFactory.Invoke(serviceProvider, null);
            }

            return filter;
        }
    }
}
