// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.Extensions.Logging;
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

        private bool isDispatchingEvent;
        private Queue<IncomingEventInfo> deferredIncomingEvents = new Queue<IncomingEventInfo>();

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
        /// <returns>A <see cref="Task"/> that represents the asynchronous rendering of the added component.</returns>
        /// <remarks>
        /// Callers of this method may choose to ignore the returned <see cref="Task"/> if they do not
        /// want to await the rendering of the added component.
        /// </remarks>
        public Task AddComponentAsync<[DynamicallyAccessedMembers(Component)] TComponent>(string domElementSelector, ParameterView parameters) where TComponent : IComponent
            => AddComponentAsync(typeof(TComponent), domElementSelector, parameters);

        /// <summary>
        /// Associates the <see cref="IComponent"/> with the <see cref="WebAssemblyRenderer"/>,
        /// causing it to be displayed in the specified DOM element.
        /// </summary>
        /// <param name="componentType">The type of the component.</param>
        /// <param name="domElementSelector">A CSS selector that uniquely identifies a DOM element.</param>
        /// <param name="parameters">The list of root component parameters.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous rendering of the added component.</returns>
        /// <remarks>
        /// Callers of this method may choose to ignore the returned <see cref="Task"/> if they do not
        /// want to await the rendering of the added component.
        /// </remarks>
        public Task AddComponentAsync([DynamicallyAccessedMembers(Component)] Type componentType, string domElementSelector, ParameterView parameters)
        {
            var component = InstantiateComponent(componentType);
            var componentId = AssignRootComponentId(component);

            // The only reason we're calling this synchronously is so that, if it throws,
            // we get the exception back *before* attempting the first UpdateDisplayAsync
            // (otherwise the logged exception will come from UpdateDisplayAsync instead of here)
            // When implementing support for out-of-process runtimes, we'll need to call this
            // asynchronously and ensure we surface any exceptions correctly.

            DefaultWebAssemblyJSRuntime.Instance.Invoke<object>(
                "Blazor._internal.attachRootComponentToElement",
                domElementSelector,
                componentId,
                _webAssemblyRendererId);

            return RenderRootComponentAsync(componentId, parameters);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            RendererRegistry.TryRemove(_webAssemblyRendererId);
        }

        /// <inheritdoc />
        protected override Task UpdateDisplayAsync(in RenderBatch batch)
        {
            DefaultWebAssemblyJSRuntime.Instance.InvokeUnmarshalled<int, RenderBatch, object>(
                "Blazor._internal.renderBatch",
                _webAssemblyRendererId,
                batch);

            return Task.CompletedTask;
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

        /// <inheritdoc />
        public override Task DispatchEventAsync(ulong eventHandlerId, EventFieldInfo? eventFieldInfo, EventArgs eventArgs)
        {
            // Be sure we only run one event handler at once. Although they couldn't run
            // simultaneously anyway (there's only one thread), they could run nested on
            // the stack if somehow one event handler triggers another event synchronously.
            // We need event handlers not to overlap because (a) that's consistent with
            // server-side Blazor which uses a sync context, and (b) the rendering logic
            // relies completely on the idea that within a given scope it's only building
            // or processing one batch at a time.
            //
            // The only currently known case where this makes a difference is in the E2E
            // tests in ReorderingFocusComponent, where we hit what seems like a Chrome bug
            // where mutating the DOM cause an element's "change" to fire while its "input"
            // handler is still running (i.e., nested on the stack) -- this doesn't happen
            // in Firefox. Possibly a future version of Chrome may fix this, but even then,
            // it's conceivable that DOM mutation events could trigger this too.

            if (isDispatchingEvent)
            {
                var info = new IncomingEventInfo(eventHandlerId, eventFieldInfo, eventArgs);
                deferredIncomingEvents.Enqueue(info);
                return info.TaskCompletionSource.Task;
            }
            else
            {
                try
                {
                    isDispatchingEvent = true;
                    return base.DispatchEventAsync(eventHandlerId, eventFieldInfo, eventArgs);
                }
                finally
                {
                    isDispatchingEvent = false;

                    if (deferredIncomingEvents.Count > 0)
                    {
                        // Fire-and-forget because the task we return from this method should only reflect the
                        // completion of its own event dispatch, not that of any others that happen to be queued.
                        // Also, ProcessNextDeferredEventAsync deals with its own async errors.
                        _ = ProcessNextDeferredEventAsync();
                    }
                }
            }
        }

        private async Task ProcessNextDeferredEventAsync()
        {
            var info = deferredIncomingEvents.Dequeue();
            var taskCompletionSource = info.TaskCompletionSource;

            try
            {
                await DispatchEventAsync(info.EventHandlerId, info.EventFieldInfo, info.EventArgs);
                taskCompletionSource.SetResult();
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetException(ex);
            }
        }

        readonly struct IncomingEventInfo
        {
            public readonly ulong EventHandlerId;
            public readonly EventFieldInfo? EventFieldInfo;
            public readonly EventArgs EventArgs;
            public readonly TaskCompletionSource TaskCompletionSource;

            public IncomingEventInfo(ulong eventHandlerId, EventFieldInfo? eventFieldInfo, EventArgs eventArgs)
            {
                EventHandlerId = eventHandlerId;
                EventFieldInfo = eventFieldInfo;
                EventArgs = eventArgs;
                TaskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }

        private static class Log
        {
            private static readonly Action<ILogger, string, Exception> _unhandledExceptionRenderingComponent;

            private static class EventIds
            {
                public static readonly EventId UnhandledExceptionRenderingComponent = new EventId(100, "ExceptionRenderingComponent");
            }

            static Log()
            {
                _unhandledExceptionRenderingComponent = LoggerMessage.Define<string>(
                    LogLevel.Critical,
                    EventIds.UnhandledExceptionRenderingComponent,
                    "Unhandled exception rendering component: {Message}");
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
