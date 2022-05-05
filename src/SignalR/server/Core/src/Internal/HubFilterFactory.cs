// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR.Internal;

internal sealed class HubFilterFactory : IHubFilter
{
    private readonly ObjectFactory _objectFactory;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    private readonly Type _filterType;

    public HubFilterFactory([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type filterType)
    {
        _objectFactory = ActivatorUtilities.CreateFactory(filterType, Array.Empty<Type>());
        _filterType = filterType;
    }

    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
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

    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
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

    public async Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
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

    private static ValueTask DisposeFilter(IHubFilter filter)
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
        var filter = (IHubFilter?)serviceProvider.GetService(_filterType);
        if (filter == null)
        {
            filter = (IHubFilter)_objectFactory.Invoke(serviceProvider, Array.Empty<object>());
            owned = true;
        }

        return (filter, owned);
    }
}
