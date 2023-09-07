// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
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

    private SSRRenderModeBoundary? GetClosestRenderModeBoundary(int componentId)
    {
        var componentState = GetComponentState(componentId);
        do
        {
            if (componentState.Component is SSRRenderModeBoundary boundary)
            {
                return boundary;
            }

            componentState = componentState.ParentComponentState;
        }
        while (componentState is not null);

        return null;
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
            // Just wait for quiescence of the non-streaming subtrees
            await Task.WhenAll(_nonStreamingPendingTasks);
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
        else
        {
            // If this was an enhanced nav request, it will have been sent with "mode: 'no-cors'" which means:
            // - If the redirection target is on the same origin, the browser will follow it automatically
            //   and then disclose the final URL to JS code as response.url. Our JS-side code will update the
            //   URL in the history stack to match. Unfortunately the browser will strip off any hash part of
            //   the URL so developers will have to disable enhanced nav for links where scrolling to a target
            //   is important.
            // - If the redirection target is on a different origin, the browser will not disclose the URL,
            //   but instead will supply an opaqueredirect response. Our JS-side code will have to renavigate
            //   to the original (pre-redirection) URL without enhanced nav so the browser can follow the redirection.
            // Note that we could return the redirection URL to the browser explicitly here (e.g., as a header in
            // a 200 response) but doing so would disclose more info to JS than a regular 301/302, so we don't
            // want to do that.
            httpContext.Response.Redirect(navigationException.Location);
            return new ValueTask<PrerenderedComponentHtmlContent>(PrerenderedComponentHtmlContent.Empty);
        }
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
