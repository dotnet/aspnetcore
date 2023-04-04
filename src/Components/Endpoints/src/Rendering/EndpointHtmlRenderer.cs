// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.HtmlRendering.Infrastructure;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// A <see cref="StaticHtmlRenderer"/> subclass which is also the implementation of the
/// <see cref="IComponentPrerenderer"/> DI service. This is the underlying mechanism shared by:
///
/// * Html.RenderComponentAsync (the earliest prerendering mechanism - a Razor HTML helper)
/// * ComponentTagHelper (the primary prerendering mechanism before .NET 8)
/// * RazorComponentResult and RazorComponentEndpoint (the primary prerendering mechanisms since .NET 8)
///
/// EndpointHtmlRenderer wraps the underlying <see cref="Web.HtmlRenderer"/> mechanism, annotating the
/// output with prerendering markers so the content can later switch into interactive mode when used with
/// blazor.*.js. It also deals with initializing the standard component DI services once per request.
///
/// Note that EndpointHtmlRenderer doesn't deal with streaming SSR since that's not applicable to Html.RenderComponentAsync
/// or ComponentTagHelper, because they don't control the entire response. Streaming SSR is a layer around this implemented
/// only inside RazorComponentResult/RazorComponentEndpoint.
/// </summary>
internal sealed partial class EndpointHtmlRenderer : StaticHtmlRenderer, IComponentPrerenderer
{
    private readonly IServiceProvider _services;
    private Task? _servicesInitializedTask;
    private Action<IEnumerable<HtmlComponentBase>>? _onContentUpdatedCallback;

    public EndpointHtmlRenderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        : base(serviceProvider, loggerFactory)
    {
        _services = serviceProvider;
    }

    private static async Task InitializeStandardComponentServicesAsync(HttpContext httpContext)
    {
        var navigationManager = (IHostEnvironmentNavigationManager)httpContext.RequestServices.GetRequiredService<NavigationManager>();
        navigationManager?.Initialize(GetContextBaseUri(httpContext.Request), GetFullUri(httpContext.Request));

        var authenticationStateProvider = httpContext.RequestServices.GetService<AuthenticationStateProvider>() as IHostEnvironmentAuthenticationStateProvider;
        if (authenticationStateProvider != null)
        {
            var authenticationState = new AuthenticationState(httpContext.User);
            authenticationStateProvider.SetAuthenticationState(Task.FromResult(authenticationState));
        }

        // It's important that this is initialized since a component might try to restore state during prerendering
        // (which will obviously not work, but should not fail)
        var componentApplicationLifetime = httpContext.RequestServices.GetRequiredService<ComponentStatePersistenceManager>();
        await componentApplicationLifetime.RestoreStateAsync(new PrerenderComponentApplicationStore());
    }

    public void OnContentUpdated(Action<IEnumerable<HtmlComponentBase>> callback)
    {
        if (_onContentUpdatedCallback is not null)
        {
            // The framework is the only user of this internal API, so it's OK to have an arbitrary limit like this
            throw new InvalidOperationException($"{nameof(OnContentUpdated)} can only be called once.");
        }

        _onContentUpdatedCallback = callback;
    }

    protected override ComponentState CreateComponentState(int componentId, IComponent component, ComponentState? parentComponentState)
        => new EndpointComponentState(this, componentId, component, parentComponentState);

    protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
    {
        var count = renderBatch.UpdatedComponents.Count;
        if (count > 0 && _onContentUpdatedCallback is not null)
        {
            // TODO: Deduplicate these. There's no reason ever to include the same component more than once in a single batch,
            // as we're rendering its whole final state, not the intermediate diffs.
            var htmlComponents = new List<HtmlComponentBase>(count);
            for (var i = 0; i < count; i++)
            {
                ref var diff = ref renderBatch.UpdatedComponents.Array[i];
                htmlComponents.Add(new HtmlComponentBase(this, diff.ComponentId));
            }

            _onContentUpdatedCallback(htmlComponents);
        }

        return base.UpdateDisplayAsync(renderBatch);
    }

    private static string GetFullUri(HttpRequest request)
    {
        return UriHelper.BuildAbsolute(
            request.Scheme,
            request.Host,
            request.PathBase,
            request.Path,
            request.QueryString);
    }

    private static string GetContextBaseUri(HttpRequest request)
    {
        var result = UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase);

        // PathBase may be "/" or "/some/thing", but to be a well-formed base URI
        // it has to end with a trailing slash
        return result.EndsWith('/') ? result : result += "/";
    }
}
