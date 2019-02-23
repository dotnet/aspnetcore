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
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.RazorComponents
{
    internal class StaticComponentRenderer
    {
        private readonly HtmlEncoder _encoder;

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

            // This shouldn't be moved to the constructor as we want a request scoped service.
            var helper = (HttpUriHelper)httpContext.RequestServices.GetRequiredService<IUriHelper>();
            helper.InitializeState(httpContext);
            using (var htmlRenderer = new HtmlRenderer(httpContext.RequestServices, _encoder.Encode, dispatcher))
            {
                return await dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync(
                    componentType,
                    parameters));
            }
        }
    }
}
