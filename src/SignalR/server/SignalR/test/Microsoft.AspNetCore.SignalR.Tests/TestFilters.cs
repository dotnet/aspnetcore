// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.SignalR.Tests;

public class VerifyMethodFilter : IHubFilter
{
    private readonly TcsService _service;
    public VerifyMethodFilter(TcsService tcsService)
    {
        _service = tcsService;
    }

    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        _service.StartedMethod.TrySetResult(null);
        await next(context);
        _service.EndMethod.TrySetResult(null);
    }

    public async ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next)
    {
        _service.StartedMethod.TrySetResult(null);
        var result = await next(invocationContext);
        _service.EndMethod.TrySetResult(null);

        return result;
    }

    public async Task OnDisconnectedAsync(HubLifetimeContext context, Exception exception, Func<HubLifetimeContext, Exception, Task> next)
    {
        _service.StartedMethod.TrySetResult(null);
        await next(context, exception);
        _service.EndMethod.TrySetResult(null);
    }
}

public class SyncPointFilter : IHubFilter
{
    private readonly SyncPoint[] _syncPoint;
    public SyncPointFilter(SyncPoint[] syncPoints)
    {
        Debug.Assert(syncPoints.Length == 3);
        _syncPoint = syncPoints;
    }

    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        await _syncPoint[0].WaitToContinue();
        await next(context);
    }

    public async ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next)
    {
        await _syncPoint[1].WaitToContinue();
        var result = await next(invocationContext);

        return result;
    }

    public async Task OnDisconnectedAsync(HubLifetimeContext context, Exception exception, Func<HubLifetimeContext, Exception, Task> next)
    {
        await _syncPoint[2].WaitToContinue();
        await next(context, exception);
    }
}

public class FilterCounter
{
    public int OnConnectedAsyncCount;
    public int InvokeMethodAsyncCount;
    public int OnDisconnectedAsyncCount;
}

public class CounterFilter : IHubFilter
{
    private readonly FilterCounter _counter;
    public CounterFilter(FilterCounter counter)
    {
        _counter = counter;
        _counter.OnConnectedAsyncCount = 0;
        _counter.InvokeMethodAsyncCount = 0;
        _counter.OnDisconnectedAsyncCount = 0;
    }

    public Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        _counter.OnConnectedAsyncCount++;
        return next(context);
    }

    public Task OnDisconnectedAsync(HubLifetimeContext context, Exception exception, Func<HubLifetimeContext, Exception, Task> next)
    {
        _counter.OnDisconnectedAsyncCount++;
        return next(context, exception);
    }

    public ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next)
    {
        _counter.InvokeMethodAsyncCount++;
        return next(invocationContext);
    }
}

public class NoExceptionFilter : IHubFilter
{
    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        try
        {
            await next(context);
        }
        catch { }
    }

    public async Task OnDisconnectedAsync(HubLifetimeContext context, Exception exception, Func<HubLifetimeContext, Exception, Task> next)
    {
        try
        {
            await next(context, exception);
        }
        catch { }
    }

    public async ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next)
    {
        try
        {
            return await next(invocationContext);
        }
        catch { }

        return null;
    }
}

public class SkipNextFilter : IHubFilter
{
    private readonly bool _skipOnConnected;
    private readonly bool _skipInvoke;
    private readonly bool _skipOnDisconnected;

    public SkipNextFilter(bool skipOnConnected = false, bool skipInvoke = false, bool skipOnDisconnected = false)
    {
        _skipOnConnected = skipOnConnected;
        _skipInvoke = skipInvoke;
        _skipOnDisconnected = skipOnDisconnected;
    }

    public Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        if (_skipOnConnected)
        {
            return Task.CompletedTask;
        }

        return next(context);
    }

    public Task OnDisconnectedAsync(HubLifetimeContext context, Exception exception, Func<HubLifetimeContext, Exception, Task> next)
    {
        if (_skipOnDisconnected)
        {
            return Task.CompletedTask;
        }

        return next(context, exception);
    }

    public ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next)
    {
        if (_skipInvoke)
        {
            return new ValueTask<object>();
        }

        return next(invocationContext);
    }
}

public class DisposableFilter : IHubFilter, IDisposable
{
    private readonly TcsService _tcsService;

    public DisposableFilter(TcsService tcsService)
    {
        _tcsService = tcsService;
    }

    public void Dispose()
    {
        _tcsService.StartedMethod.SetResult(null);
    }

    public ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next)
    {
        return next(invocationContext);
    }
}

public class AsyncDisposableFilter : IHubFilter, IAsyncDisposable
{
    private readonly TcsService _tcsService;

    public AsyncDisposableFilter(TcsService tcsService)
    {
        _tcsService = tcsService;
    }

    public ValueTask DisposeAsync()
    {
        _tcsService.StartedMethod.SetResult(null);
        return default;
    }

    public ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next)
    {
        return next(invocationContext);
    }
}

public class ChangeMethodFilter : IHubFilter
{
    public ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next)
    {
        var methodInfo = typeof(BaseHub).GetMethod(nameof(BaseHub.BaseMethod));
        var context = new HubInvocationContext(invocationContext.Context, invocationContext.ServiceProvider, invocationContext.Hub, methodInfo, invocationContext.HubMethodArguments);
        return next(context);
    }
}

public class EmptyFilter : IHubFilter
{
    // Purposefully not implementing any methods
}
