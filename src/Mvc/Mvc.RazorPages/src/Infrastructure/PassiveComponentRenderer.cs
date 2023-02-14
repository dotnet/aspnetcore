// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
internal class PassiveComponentRenderer
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IViewBufferScope _viewBufferScope;
    private readonly IHttpResponseStreamWriterFactory _writerFactory;
    private readonly HtmlEncoder _htmlEncoder;

    public PassiveComponentRenderer(
        ILoggerFactory loggerFactory,
        IViewBufferScope viewBufferScope,
        IHttpResponseStreamWriterFactory writerFactory,
        HtmlEncoder htmlEncoder)
    {
        _loggerFactory = loggerFactory;
        _viewBufferScope = viewBufferScope;
        _writerFactory = writerFactory;
        _htmlEncoder = htmlEncoder;
    }

    public async Task HandleRequest(HttpContext httpContext, Type componentType)
    {
        using var htmlRenderer = new HtmlRenderer(httpContext.RequestServices, _loggerFactory, _viewBufferScope);
        var staticComponentRenderer = new StaticComponentRenderer(htmlRenderer);

        var routeData = httpContext.GetRouteData();
        var rootComponentType = typeof(RouteView);
        var rootComponentParameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                { nameof(RouteView.RouteData), new Components.RouteData(componentType, routeData.Values!) },
            });

        var result = await staticComponentRenderer.PrerenderComponentAsync(
            rootComponentParameters,
            httpContext,
            rootComponentType);

        var viewBuffer = new ViewBuffer(_viewBufferScope, nameof(RazorComponentsEndpointRouteBuilderExtensions), ViewBuffer.ViewPageSize);
        viewBuffer.AppendHtml(result);

        using var writer = _writerFactory.CreateWriter(httpContext.Response.BodyWriter.AsStream(), Encoding.UTF8);
        await viewBuffer.WriteToAsync(writer, _htmlEncoder);
    }
}
