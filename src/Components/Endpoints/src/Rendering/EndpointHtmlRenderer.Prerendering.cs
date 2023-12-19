// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal partial class EndpointHtmlRenderer
{
    private static readonly object ComponentSequenceKey = new object();

    protected override IComponent ResolveComponentForRenderMode([DynamicallyAccessedMembers(Component)] Type componentType, int? parentComponentId, IComponentActivator componentActivator, IComponentRenderMode renderMode)
    {
        if (_isHandlingErrors)
        {
            // Ignore the render mode boundary in error scenarios.
            return componentActivator.CreateInstance(componentType);
        }
        var closestRenderModeBoundary = parentComponentId.HasValue
            ? GetClosestRenderModeBoundary(parentComponentId.Value)
            : null;

        if (closestRenderModeBoundary is not null)
        {
            // We're already inside a subtree with a rendermode. Once it becomes interactive, the entire DOM subtree
            // will get replaced anyway. So there is no point emitting further rendermode boundaries.
            return componentActivator.CreateInstance(componentType);
        }
        else
        {
            // This component is the start of a subtree with a rendermode, so introduce a new rendermode boundary here
            return new SSRRenderModeBoundary(_httpContext, componentType, renderMode);
        }
    }

    protected override IComponentRenderMode? GetComponentRenderMode(IComponent component)
    {
        var componentState = GetComponentState(component);
        var ssrRenderBoundary = GetClosestRenderModeBoundary(componentState);

        if (ssrRenderBoundary is null)
        {
            return null;
        }

        return ssrRenderBoundary.RenderMode;
    }

    private SSRRenderModeBoundary? GetClosestRenderModeBoundary(int componentId)
    {
        var componentState = GetComponentState(componentId);
        return GetClosestRenderModeBoundary(componentState);
    }

    private static SSRRenderModeBoundary? GetClosestRenderModeBoundary(ComponentState componentState)
    {
        var currentComponentState = componentState;

        do
        {
            if (currentComponentState.Component is SSRRenderModeBoundary boundary)
            {
                return boundary;
            }

            currentComponentState = currentComponentState.ParentComponentState;
        }
        while (currentComponentState is not null);

        return null;
    }

    public static void MarkAsAllowingEnhancedNavigation(HttpContext context)
    {
        context.Response.Headers.Add("blazor-enhanced-nav", "allow");
    }

    public ValueTask<IHtmlAsyncContent> PrerenderComponentAsync(
        HttpContext httpContext,
        [DynamicallyAccessedMembers(Component)] Type componentType,
        IComponentRenderMode prerenderMode,
        ParameterView parameters)
        => PrerenderComponentAsync(httpContext, componentType, prerenderMode, parameters, waitForQuiescence: true);

    public async ValueTask<IHtmlAsyncContent> PrerenderComponentAsync(
        HttpContext httpContext,
        [DynamicallyAccessedMembers(Component)] Type componentType,
        IComponentRenderMode? prerenderMode,
        ParameterView parameters,
        bool waitForQuiescence)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(componentType);

        if (!typeof(IComponent).IsAssignableFrom(componentType))
        {
            throw new ArgumentException(Resources.FormatTypeMustDeriveFromType(componentType, typeof(IComponent)));
        }

        // Make sure we only initialize the services once, but on every call we wait for that process to complete
        // This does not have to be threadsafe since it's not valid to call this simultaneously from multiple threads.
        SetHttpContext(httpContext);
        _servicesInitializedTask ??= InitializeStandardComponentServicesAsync(httpContext);
        await _servicesInitializedTask;

        UpdateSaveStateRenderMode(httpContext, prerenderMode);

        try
        {
            var rootComponent = prerenderMode is null
                ? InstantiateComponent(componentType)
                : new SSRRenderModeBoundary(_httpContext, componentType, prerenderMode);
            var htmlRootComponent = await Dispatcher.InvokeAsync(() => BeginRenderingComponent(rootComponent, parameters));
            var result = new PrerenderedComponentHtmlContent(Dispatcher, htmlRootComponent);

            await WaitForResultReady(waitForQuiescence, result);

            return result;
        }
        catch (NavigationException navigationException)
        {
            return await HandleNavigationException(httpContext, navigationException);
        }
    }

    internal async ValueTask<PrerenderedComponentHtmlContent> RenderEndpointComponent(
        HttpContext httpContext,
        [DynamicallyAccessedMembers(Component)] Type rootComponentType,
        ParameterView parameters,
        bool waitForQuiescence)
    {
        SetHttpContext(httpContext);

        try
        {
            var component = BeginRenderingComponent(rootComponentType, parameters);
            var result = new PrerenderedComponentHtmlContent(Dispatcher, component);

            await WaitForResultReady(waitForQuiescence, result);

            return result;
        }
        catch (NavigationException navigationException)
        {
            return await HandleNavigationException(_httpContext, navigationException);
        }
    }

    private async Task WaitForResultReady(bool waitForQuiescence, PrerenderedComponentHtmlContent result)
    {
        if (waitForQuiescence)
        {
            // Full quiescence, i.e., all tasks completed regardless of streaming SSR
            await result.QuiescenceTask;
        }
        else if (_nonStreamingPendingTasks.Count > 0)
        {
            await WaitForNonStreamingPendingTasks();
        }
    }

    public Task WaitForNonStreamingPendingTasks()
    {
        return NonStreamingPendingTasksCompletion ??= Execute();

        async Task Execute()
        {
            while (_nonStreamingPendingTasks.Count > 0)
            {
                // Create a Task that represents the remaining ongoing work for the rendering process
                var pendingWork = Task.WhenAll(_nonStreamingPendingTasks);

                // Clear all pending work.
                _nonStreamingPendingTasks.Clear();

                // new work might be added before we check again as a result of waiting for all
                // the child components to finish executing SetParametersAsync
                await pendingWork;
            }
        }
    }

    public static ValueTask<PrerenderedComponentHtmlContent> HandleNavigationException(HttpContext httpContext, NavigationException navigationException)
    {
        if (httpContext.Response.HasStarted)
        {
            // If we're not doing streaming SSR, this has no choice but to be a fatal error because there's no way to
            // communicate the redirection to the browser.
            // If we are doing streaming SSR, this should not generally happen because if you navigate during the initial
            // synchronous render, the response would not yet have started, and if you do so during some later async
            // phase, we would already have exited this method since streaming SSR means not awaiting quiescence.
            throw new InvalidOperationException(
                "A navigation command was attempted during prerendering after the server already started sending the response. " +
                "Navigation commands can not be issued during server-side prerendering after the response from the server has started. Applications must buffer the" +
                "response and avoid using features like FlushAsync() before all components on the page have been rendered to prevent failed navigation commands.");
        }
        else if (IsPossibleExternalDestination(httpContext.Request, navigationException.Location)
            && IsProgressivelyEnhancedNavigation(httpContext.Request))
        {
            // For progressively-enhanced nav, we prefer to use opaque redirections for external URLs rather than
            // forcing the request to be retried, since that allows post-redirect-get to work, plus avoids a
            // duplicated request. The client can't rely on receiving this header, though, since non-Blazor endpoints
            // wouldn't return it.
            httpContext.Response.Headers.Add("blazor-enhanced-nav-redirect-location",
                OpaqueRedirection.CreateProtectedRedirectionUrl(httpContext, navigationException.Location));
            return new ValueTask<PrerenderedComponentHtmlContent>(PrerenderedComponentHtmlContent.Empty);
        }
        else
        {
            httpContext.Response.Redirect(navigationException.Location);
            return new ValueTask<PrerenderedComponentHtmlContent>(PrerenderedComponentHtmlContent.Empty);
        }
    }

    private static bool IsPossibleExternalDestination(HttpRequest request, string destinationUrl)
    {
        if (!Uri.TryCreate(destinationUrl, UriKind.Absolute, out var absoluteUri))
        {
            return false;
        }

        return absoluteUri.Scheme != request.Scheme
            || absoluteUri.Authority != request.Host.Value;
    }

    internal static ServerComponentInvocationSequence GetOrCreateInvocationId(HttpContext httpContext)
    {
        if (!httpContext.Items.TryGetValue(ComponentSequenceKey, out var result))
        {
            result = new ServerComponentInvocationSequence();
            httpContext.Items[ComponentSequenceKey] = result;
        }

        return (ServerComponentInvocationSequence)result!;
    }

    // An implementation of IHtmlContent that holds a reference to a component until we're ready to emit it as HTML to the response.
    // We don't construct the actual HTML until we receive the call to WriteTo.
    public class PrerenderedComponentHtmlContent : IHtmlAsyncContent
    {
        private readonly Dispatcher? _dispatcher;
        private readonly HtmlRootComponent? _htmlToEmitOrNull;

        public static PrerenderedComponentHtmlContent Empty { get; }
            = new PrerenderedComponentHtmlContent(null, default);

        public PrerenderedComponentHtmlContent(
            Dispatcher? dispatcher, // If null, we're only emitting the markers
            HtmlRootComponent? htmlToEmitOrNull)
        {
            _dispatcher = dispatcher;
            _htmlToEmitOrNull = htmlToEmitOrNull;
        }

        public async ValueTask WriteToAsync(TextWriter writer)
        {
            if (_dispatcher is null)
            {
                WriteTo(writer, HtmlEncoder.Default);
            }
            else
            {
                await _dispatcher.InvokeAsync(() => WriteTo(writer, HtmlEncoder.Default));
            }
        }

        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            if (_htmlToEmitOrNull is { } htmlToEmit)
            {
                htmlToEmit.WriteHtmlTo(writer);
            }
        }

        public Task QuiescenceTask =>
            _htmlToEmitOrNull.HasValue ? _htmlToEmitOrNull.Value.QuiescenceTask : Task.CompletedTask;
    }
}
