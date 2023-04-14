// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.HtmlRendering.Infrastructure;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Routing;
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
/// </summary>
internal sealed partial class EndpointHtmlRenderer : StaticHtmlRenderer, IComponentPrerenderer
{
    private readonly IServiceProvider _services;
    private Task? _servicesInitializedTask;

    // The underlying Renderer always tracks the pending tasks representing *full* quiescence, i.e.,
    // when everything (regardless of streaming SSR) is fully complete. In this subclass we also track
    // the subset of those that are from the non-streaming subtrees, since we want the response to
    // wait for the non-streaming tasks (these ones), then start streaming until full quiescence.
    private readonly List<Task> _nonStreamingPendingTasks = new();

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

    protected override ComponentState CreateComponentState(int componentId, IComponent component, ComponentState? parentComponentState)
        => new EndpointComponentState(this, componentId, component, parentComponentState);

    protected override void AddPendingTask(ComponentState? componentState, Task task)
    {
        var streamRendering = componentState is null
            ? false
            : ((EndpointComponentState)componentState).StreamRendering;

        if (!streamRendering)
        {
            _nonStreamingPendingTasks.Add(task);
        }

        // We still need to determine full quiescence, so always let the base renderer track this task too
        base.AddPendingTask(componentState, task);
    }

    // For tests only
    internal List<Task> NonStreamingPendingTasks => _nonStreamingPendingTasks;

    protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
    {
        if (_streamingUpdatesWriter is { } writer)
        {
            SendBatchAsStreamingUpdate(renderBatch, writer);
            return FlushThenComplete(writer, base.UpdateDisplayAsync(renderBatch));
        }
        else
        {
            return base.UpdateDisplayAsync(renderBatch);
        }

        // Workaround for methods with "in" parameters not being allowed to be async
        // We resolve the "result" first and then combine it with the FlushAsync task here
        static async Task FlushThenComplete(TextWriter writerToFlush, Task completion)
        {
            await writerToFlush.FlushAsync();
            await completion;
        }
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
