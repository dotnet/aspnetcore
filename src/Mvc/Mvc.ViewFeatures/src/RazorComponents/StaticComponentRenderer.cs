// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.RazorComponents
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
            ParameterCollection parameters,
            HttpContext httpContext,
            Type componentType)
        {
            var dispatcher = Renderer.CreateDefaultDispatcher();

            InitializeUriHelper(httpContext);
            using (var htmlRenderer = new HtmlRenderer(httpContext.RequestServices, _encoder.Encode, dispatcher))
            {
                var result = await dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync(
                    componentType,
                    parameters));
                return result.Tokens;
            }
        }

        private void InitializeUriHelper(HttpContext httpContext)
        {
            // We don't know here if we are dealing with the default HttpUriHelper registered
            // by MVC or with the RemoteUriHelper registered by AddComponents.
            // This might not be the first component in the request we are rendering, so
            // we need to check if we already initialized the uri helper in this request.
            if (!_initialized)
            {
                _initialized = true;
                var helper = (UriHelperBase)httpContext.RequestServices.GetRequiredService<IUriHelper>();
                helper.InitializeState(GetFullUri(httpContext.Request), GetContextBaseUri(httpContext.Request));
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
