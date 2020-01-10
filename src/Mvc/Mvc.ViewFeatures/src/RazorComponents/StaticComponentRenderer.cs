// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    internal class StaticComponentRenderer
    {
        private readonly HtmlEncoder _encoder;
        private bool _initialized = false;

        public StaticComponentRenderer(HtmlEncoder encoder)
        {
            _encoder = encoder;
        }

        public async Task<IEnumerable<string>> PrerenderComponentAsync(
            ParameterView parameters,
            HttpContext httpContext,
            Type componentType)
        {
            InitializeStandardComponentServices(httpContext);
            var loggerFactory = (ILoggerFactory)httpContext.RequestServices.GetService(typeof (ILoggerFactory));
            using (var htmlRenderer = new HtmlRenderer(httpContext.RequestServices, loggerFactory, _encoder.Encode))
            {
                ComponentRenderedText result = default;
                try
                {
                    result = await htmlRenderer.Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync(
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
                    return Array.Empty<string>();
                }

                return result.Tokens;
            }
        }

        private void InitializeStandardComponentServices(HttpContext httpContext)
        {
            // This might not be the first component in the request we are rendering, so
            // we need to check if we already initialized the services in this request.
            if (!_initialized)
            {
                _initialized = true;

                var authenticationStateProvider = httpContext.RequestServices.GetService<AuthenticationStateProvider>() as IHostEnvironmentAuthenticationStateProvider;
                if (authenticationStateProvider != null)
                {
                    var authenticationState = new AuthenticationState(httpContext.User);
                    authenticationStateProvider.SetAuthenticationState(Task.FromResult(authenticationState));
                }

                var navigationManager = (IHostEnvironmentNavigationManager)httpContext.RequestServices.GetRequiredService<NavigationManager>();
                navigationManager?.Initialize(GetContextBaseUri(httpContext.Request), GetFullUri(httpContext.Request));
            }
        }

        private string GetFullUri(HttpRequest request)
        {
            return UriHelper.BuildAbsolute(
                request.Scheme,
                request.Host,
                request.PathBase,
                request.Path,
                request.QueryString);
        }

        private string GetContextBaseUri(HttpRequest request)
        {
            var result = UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase);

            // PathBase may be "/" or "/some/thing", but to be a well-formed base URI
            // it has to end with a trailing slash
            return result.EndsWith("/") ? result : result += "/";
        }
    }
}
