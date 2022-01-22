// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

internal class StaticComponentRenderer
{
    private Task _initialized;
    private readonly HtmlRenderer _renderer;
    private readonly object _lock = new();

    public StaticComponentRenderer(HtmlRenderer renderer)
    {
        _renderer = renderer;
    }

    public async ValueTask<IHtmlContent> PrerenderComponentAsync(
        ParameterView parameters,
        HttpContext httpContext,
        Type componentType)
    {
        await InitializeStandardComponentServicesAsync(httpContext);

        ComponentRenderedText result = default;
        try
        {
            result = await _renderer.Dispatcher.InvokeAsync(() => _renderer.RenderComponentAsync(
                componentType,
                parameters));
        }
        catch (NavigationException navigationException)
        {
            // Navigation was attempted during prerendering.
            if (httpContext.Response.HasStarted)
            {
                // We can't perform a redirect as the server already started sending the response.
                // This is considered an application error as the developer should buffer the response until
                // all components have rendered.
                throw new InvalidOperationException("A navigation command was attempted during prerendering after the server already started sending the response. " +
                    "Navigation commands can not be issued during server-side prerendering after the response from the server has started. Applications must buffer the" +
                    "response and avoid using features like FlushAsync() before all components on the page have been rendered to prevent failed navigation commands.", navigationException);
            }

            httpContext.Response.Redirect(navigationException.Location);
            return HtmlString.Empty;
        }

        return result.HtmlContent;
    }

    private Task InitializeStandardComponentServicesAsync(HttpContext httpContext)
    {
        // This might not be the first component in the request we are rendering, so
        // we need to check if we already initialized the services in this request.
        lock (_lock)
        {
            if (_initialized == null)
            {
                _initialized = InitializeCore(httpContext);
            }
        }

        return _initialized;

        static async Task InitializeCore(HttpContext httpContext)
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
