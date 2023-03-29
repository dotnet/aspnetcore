// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.HtmlRendering.Infrastructure;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// A <see cref="StaticHtmlRenderer"/> subclass that is used when prerendering on an endpoint
/// or for the component tag helper. It knows how to annotate the output with prerendering
/// markers so the content can later switch into interactive mode. It also deals with initializing
/// the standard component DI services once per request.
/// </summary>
internal sealed partial class EndpointHtmlRenderer : StaticHtmlRenderer, IComponentPrerenderer
{
    private readonly IServiceProvider _services;
    private Task? _servicesInitializedTask;

    public EndpointHtmlRenderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        : base(serviceProvider, loggerFactory)
    {
        _services = serviceProvider;
    }

    // Indicates the name for the form that we need to dispatch an incoming form post to.
    public string? NamedFormHandler { get; set; }

    public ulong? NamedFormHandlerId { get; set; }

    protected override void TrackEventName(string eventName, ulong id)
    {
        if (NamedFormHandler == null)
        {
            return;
        }

        if (string.Equals(NamedFormHandler, eventName, StringComparison.Ordinal))
        {
            NamedFormHandlerId = id;
        }
    }

    internal bool HasSubmitEvent()
    {
        return NamedFormHandlerId != null;
    }

    internal Task DispatchSubmitEventAsync()
    {
        if (!HasSubmitEvent())
        {
            throw new InvalidOperationException();
        }
        return DispatchEventAsync(NamedFormHandlerId!.Value, null, new EventArgs(), quiesce: true);
    }

    internal Task InitializeStandardComponentServicesAsync(HttpContext httpContext)
    {
        return _servicesInitializedTask ??= InitializeCore(httpContext);

        static async Task InitializeCore(HttpContext httpContext)
        {
            var navigationManager = (IHostEnvironmentNavigationManager)httpContext.RequestServices.GetRequiredService<NavigationManager>();
            navigationManager?.Initialize(GetContextBaseUri(httpContext.Request), GetFullUri(httpContext.Request));

            if (httpContext.RequestServices.GetService<AuthenticationStateProvider>() is IHostEnvironmentAuthenticationStateProvider authenticationStateProvider)
            {
                var authenticationState = new AuthenticationState(httpContext.User);
                authenticationStateProvider.SetAuthenticationState(Task.FromResult(authenticationState));
            }

            // It's important that this is initialized since a component might try to restore state during prerendering
            // (which will obviously not work, but should not fail)
            var componentApplicationLifetime = httpContext.RequestServices.GetRequiredService<ComponentStatePersistenceManager>();
            await componentApplicationLifetime.RestoreStateAsync(new PrerenderComponentApplicationStore());
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
