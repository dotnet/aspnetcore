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

        public async ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next)
        {
            var (filter, owned) = GetFilter(invocationContext.ServiceProvider);

            try
            {
                return await filter.InvokeMethodAsync(invocationContext, next);
            }
            finally
            {
                if (owned)
                {
                    await DisposeFilter(filter);
                }
            }
        }

        public async Task OnConnectedAsync(SomeHubContext context, Func<SomeHubContext, Task> next)
        {
            var (filter, owned) = GetFilter(context.ServiceProvider);

            try
            {
                await filter.OnConnectedAsync(context, next);
            }
            finally
            {
                if (owned)
                {
                    await DisposeFilter(filter);
                }
            }
        }

        public async Task OnDisconnectedAsync(SomeHubContext context, Exception exception, Func<SomeHubContext, Exception, Task> next)
        {
            var (filter, owned) = GetFilter(context.ServiceProvider);

            try
            {
                await filter.OnDisconnectedAsync(context, exception, next);
            }
            finally
            {
                if (owned)
                {
                    await DisposeFilter(filter);
                }
            }
        }

        private ValueTask DisposeFilter(IHubFilter filter)
        {
            if (filter is IAsyncDisposable asyncDispsable)
            {
                return asyncDispsable.DisposeAsync();
            }
            if (filter is IDisposable disposable)
            {
                disposable.Dispose();
            }
            return default;
        }

        private (IHubFilter, bool) GetFilter(IServiceProvider serviceProvider)
        {
            var owned = false;
            var filter = (IHubFilter)serviceProvider.GetService<T>();
            if (filter == null)
            {
                filter = (IHubFilter)_objectFactory.Invoke(serviceProvider, null);
                owned = true;
            }

            return (filter, owned);
        }
    }
}
