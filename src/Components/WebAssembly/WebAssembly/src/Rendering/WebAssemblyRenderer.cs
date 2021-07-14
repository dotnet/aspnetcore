// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.WebAssembly.Rendering
{
    /// <summary>
    /// Provides mechanisms for rendering <see cref="IComponent"/> instances in a
    /// web browser, dispatching events to them, and refreshing the UI as required.
    /// </summary>
    internal class WebAssemblyRenderer : Renderer
    {
        private readonly ILogger _logger;
        private readonly int _webAssemblyRendererId;

        /// <summary>
        /// Constructs an instance of <see cref="WebAssemblyRenderer"/>.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to use when initializing components.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public WebAssemblyRenderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
            : base(serviceProvider, loggerFactory)
        {
            // The WebAssembly renderer registers and unregisters itself with the static registry
            _webAssemblyRendererId = RendererRegistry.Add(this);
            _logger = loggerFactory.CreateLogger<WebAssemblyRenderer>();

            ElementReferenceContext = DefaultWebAssemblyJSRuntime.Instance.ElementReferenceContext;
        }

        public override Dispatcher Dispatcher => NullDispatcher.Instance;

        /// <summary>
        /// Attaches a new root component to the renderer,
        /// causing it to be displayed in the specified DOM element.
        /// </summary>
        /// <typeparam name="TComponent">The type of the component.</typeparam>
        /// <param name="domElementSelector">A CSS selector that uniquely identifies a DOM element.</param>
        /// <param name="parameters">The parameters for the component.</param>
        /// <param name="appendContent">
        /// If <c>true</c>, the child content of the root component will be appended to existing HTML content.
        /// This is useful when treating the HTML <c>&lt;head&gt;</c> as a root component.
        /// </param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous rendering of the added component.</returns>
        /// <remarks>
        /// Callers of this method may choose to ignore the returned <see cref="Task"/> if they do not
        /// want to await the rendering of the added component.
        /// </remarks>
        public Task AddComponentAsync<[DynamicallyAccessedMembers(Component)] TComponent>(string domElementSelector, ParameterView parameters, bool appendContent) where TComponent : IComponent
            => AddComponentAsync(typeof(TComponent), domElementSelector, parameters, appendContent);

        /// <summary>
        /// Associates the <see cref="IComponent"/> with the <see cref="WebAssemblyRenderer"/>,
        /// causing it to be displayed in the specified DOM element.
        /// </summary>
        /// <param name="componentType">The type of the component.</param>
        /// <param name="domElementSelector">A CSS selector that uniquely identifies a DOM element.</param>
        /// <param name="parameters">The list of root component parameters.</param>
        /// <param name="appendContent">
        /// If <c>true</c>, the child content of the root component will be appended to existing HTML content.
        /// This is useful when treating the HTML <c>&lt;head&gt;</c> as a root component.
        /// </param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous rendering of the added component.</returns>
        /// <remarks>
        /// Callers of this method may choose to ignore the returned <see cref="Task"/> if they do not
        /// want to await the rendering of the added component.
        /// </remarks>
        public Task AddComponentAsync([DynamicallyAccessedMembers(Component)] Type componentType, string domElementSelector, ParameterView parameters, bool appendContent)
        {
            var component = InstantiateComponent(componentType);
            var componentId = AssignRootComponentId(component);

            // The only reason we're calling this synchronously is so that, if it throws,
            // we get the exception back *before* attempting the first UpdateDisplayAsync
            // (otherwise the logged exception will come from UpdateDisplayAsync instead of here)
            // When implementing support for out-of-process runtimes, we'll need to call this
            // asynchronously and ensure we surface any exceptions correctly.

            DefaultWebAssemblyJSRuntime.Instance.InvokeVoid(
                "Blazor._internal.attachRootComponentToElement",
                domElementSelector,
                componentId,
                _webAssemblyRendererId,
                appendContent);

            return RenderRootComponentAsync(componentId, parameters);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            RendererRegistry.TryRemove(_webAssemblyRendererId);
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
        protected override Task UpdateDisplayAsync(in RenderBatch batch)
        {
            DefaultWebAssemblyJSRuntime.Instance.InvokeUnmarshalled<int, RenderBatch, object>(
                "Blazor._internal.renderBatch",
                _webAssemblyRendererId,
                batch);

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
                    Log.UnhandledExceptionRenderingComponent(_logger, innerException);
                }
            }
            else
            {
                Log.UnhandledExceptionRenderingComponent(_logger, exception);
            }
        }

        private static class Log
        {
            private static readonly Action<ILogger, string, Exception> _unhandledExceptionRenderingComponent = LoggerMessage.Define<string>(
                LogLevel.Critical,
                EventIds.UnhandledExceptionRenderingComponent,
                "Unhandled exception rendering component: {Message}");

            private static class EventIds
            {
                public static readonly EventId UnhandledExceptionRenderingComponent = new EventId(100, "ExceptionRenderingComponent");
            }

            public static void UnhandledExceptionRenderingComponent(ILogger logger, Exception exception)
            {
                _unhandledExceptionRenderingComponent(
                    logger,
                    exception.Message,
                    exception);
            }
        }
    }
}
