// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Services;
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
                return await dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync(
                    componentType,
                    parameters));
            }
        }

        private void InitializeUriHelper(HttpContext httpContext)
        {
            // The UriHelper might have been already initialized by a previous render.
            if (!_initialized)
            {
                // This shouldn't be moved to the constructor as we want a request scoped service.
                var helper = (UriHelperBase)httpContext.RequestServices.GetRequiredService<IUriHelper>();
                helper.InitializeState(GetFullUri(httpContext.Request), GetContextBaseUri(httpContext.Request));
                _initialized = true;
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
            return UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase);
        }
    }
}
