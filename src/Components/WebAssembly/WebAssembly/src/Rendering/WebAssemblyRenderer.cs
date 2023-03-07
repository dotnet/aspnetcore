// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web.Infrastructure;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.WebAssembly.Rendering;

/// <summary>
/// Provides mechanisms for rendering <see cref="IComponent"/> instances in a
/// web browser, dispatching events to them, and refreshing the UI as required.
/// </summary>
internal sealed partial class WebAssemblyRenderer : WebRenderer
{
    private readonly ILogger _logger;

    public WebAssemblyRenderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, JSComponentInterop jsComponentInterop)
        : base(serviceProvider, loggerFactory, DefaultWebAssemblyJSRuntime.Instance.ReadJsonSerializerOptions(), jsComponentInterop)
    {
        // The WebAssembly renderer registers and unregisters itself with the static registry
        RendererId = RendererRegistry.Add(this);
        _logger = loggerFactory.CreateLogger<WebAssemblyRenderer>();

        ElementReferenceContext = DefaultWebAssemblyJSRuntime.Instance.ElementReferenceContext;
    }

    public override Dispatcher Dispatcher => NullDispatcher.Instance;

    public Task AddComponentAsync([DynamicallyAccessedMembers(Component)] Type componentType, ParameterView parameters, string domElementSelector)
    {
        var componentId = AddRootComponent(componentType, domElementSelector);
        return RenderRootComponentAsync(componentId, parameters);
    }

    protected override void AttachRootComponentToBrowser(int componentId, string domElementSelector)
    {
        DefaultWebAssemblyJSRuntime.Instance.InvokeVoid(
            "Blazor._internal.attachRootComponentToElement",
            domElementSelector,
            componentId,
            RendererId);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        RendererRegistry.TryRemove(RendererId);
    }

    /// <inheritdoc />
    protected override void ProcessPendingRender()
    {
        // For historical reasons, Blazor WebAssembly doesn't enforce that you use InvokeAsync
        // to dispatch calls that originated from outside the system. Changing that now would be
        // too breaking, at least until we can make it a prerequisite for multithreading.
        // So, we don't have a way to guarantee that calls to here are already on our work queue.
        //
        // We do need rendering to happen on the work queue so that incoming events can be deferred
        // until we've finished this rendering process (and other similar cases where we want
        // execution order to be consistent with Blazor Server, which queues all JS->.NET calls).
        //
        // So, if we find that we're here and are not yet on the work queue, get onto it. Either
        // way, rendering must continue synchronously here and is not deferred until later.
        if (WebAssemblyCallQueue.IsInProgress)
        {
            base.ProcessPendingRender();
        }
        else
        {
            WebAssemblyCallQueue.Schedule(this, static @this => @this.CallBaseProcessPendingRender());
        }
    }

    private void CallBaseProcessPendingRender() => base.ProcessPendingRender();

    /// <inheritdoc />
    protected override unsafe Task UpdateDisplayAsync(in RenderBatch batch)
    {
        // This is a GC hazard - it would be ideal to pin 'batch' and all its contents to prevent
        // it from getting moved, or pause the GC for the duration of the 'RenderBatch()' call.
        // The key mitigation is that the JS-side code always processes renderbatches synchronously
        // and never calls back into .NET during that process, so GC cannot run (assuming it would
        // only run on the current thread).
        // As an early-warning system in case we accidentally introduce bugs and violate that rule,
        // or for edge cases where user code can be invoked during rendering (e.g., DOM mutation
        // observers) we further enforce it on the JS side using a notion of "locking the heap"
        // during rendering, which prevents any JS-to-.NET calls that go through Blazor APIs such
        // as DotNet.invokeMethod or event handlers.
        var batchCopy = batch;
        RenderBatch(RendererId, Unsafe.AsPointer(ref batchCopy));

        if (WebAssemblyCallQueue.HasUnstartedWork)
        {
            // Because further incoming calls from JS to .NET are already queued (e.g., event notifications),
            // we have to delay the renderbatch acknowledgement until it gets to the front of that queue.
            // This is for consistency with Blazor Server which queues all JS-to-.NET calls relative to each
            // other, and because various bits of cleanup logic rely on this ordering.
            var tcs = new TaskCompletionSource();
            WebAssemblyCallQueue.Schedule(tcs, static tcs => tcs.SetResult());
            return tcs.Task;
        }
        else
        {
            // Nothing else is pending, so we can treat the renderbatch as acknowledged synchronously.
            // This lets upstream code skip an expensive code path and avoids some allocations.
            return Task.CompletedTask;
        }
    }

    /// <inheritdoc />
    protected override void HandleException(Exception exception)
    {
        if (exception is AggregateException aggregateException)
        {
            foreach (var innerException in aggregateException.Flatten().InnerExceptions)
            {
                Log.UnhandledExceptionRenderingComponent(_logger, innerException.Message, innerException);
            }
        }
        else
        {
            Log.UnhandledExceptionRenderingComponent(_logger, exception.Message, exception);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(100, LogLevel.Critical, "Unhandled exception rendering component: {Message}", EventName = "ExceptionRenderingComponent")]
        public static partial void UnhandledExceptionRenderingComponent(ILogger logger, string message, Exception exception);
    }

    [JSImport("Blazor._internal.renderBatch", "blazor-internal")]
    private static unsafe partial void RenderBatch(int id, void* batch);
}
