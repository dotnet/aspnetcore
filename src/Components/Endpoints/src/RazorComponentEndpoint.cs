// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal static class RazorComponentEndpoint
{
    public static RequestDelegate CreateRouteDelegate(Type componentType)
    {
        return httpContext =>
            RenderComponentToResponse(httpContext, RenderMode.Static, componentType, componentParameters: null);
    }

    internal static Task RenderComponentToResponse(
        HttpContext httpContext,
        RenderMode renderMode,
        Type componentType,
        IReadOnlyDictionary<string, object?>? componentParameters)
    {
        var endpointHtmlRenderer = httpContext.RequestServices.GetRequiredService<EndpointHtmlRenderer>();
        return endpointHtmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // We could pool these dictionary instances if we wanted, and possibly even the ParameterView
            // backing buffers could come from a pool like they do during rendering.
            var hostParameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                { nameof(RazorComponentEndpointHost.RenderMode), renderMode },
                { nameof(RazorComponentEndpointHost.ComponentType), componentType },
                { nameof(RazorComponentEndpointHost.ComponentParameters), componentParameters },
            });

            await using var writer = CreateResponseWriter(httpContext.Response.Body);

            try
            {
                // Note that we always use Static rendering mode for the top-level output from a RazorComponentResult,
                // because you never want to serialize the invocation of RazorComponentResultHost. Instead, that host
                // component takes care of switching into your desired render mode when it produces its own output.
                var htmlContent = (EndpointHtmlRenderer.PrerenderedComponentHtmlContent)(await endpointHtmlRenderer.PrerenderComponentAsync(
                    httpContext,
                    typeof(RazorComponentEndpointHost),
                    RenderMode.Static,
                    hostParameters,
                    waitForQuiescence: false));

                // Importantly, we must not yield this thread (which holds exclusive access to the renderer sync context)
                // in between the first call to htmlContent.WriteTo and the point where we start listening for subsequent
                // streaming SSR batches. Otherwise some other code might dispatch to the renderer sync context and cause
                // a batch that would get missed.
                htmlContent.WriteTo(writer, HtmlEncoder.Default); // Don't use WriteToAsync, as per the comment above

                if (!htmlContent.QuiescenceTask.IsCompleted)
                {
                    endpointHtmlRenderer.OnContentUpdated(htmlComponents =>
                        EmitStreamingRenderingUpdate(htmlComponents, writer));

                    await writer.FlushAsync(); // Make sure the initial HTML was sent
                    await htmlContent.QuiescenceTask;
                }
            }
            catch (NavigationException navigationException)
            {
                HandleNavigation(httpContext, writer, navigationException.Location);
            }
            catch (Exception ex) when (httpContext.Response.HasStarted)
            {
                // We already started the response so we have no choice but to return a 200 with HTML and will
                // have to communicate the error information within that (and leave the client-side JS to display
                // it). But we also rethrow because we want the regular pipeline to treat this like any other
                // unhandled exception.
                HandleException(httpContext, writer, ex);
                throw;
            }

            // Invoke FlushAsync to ensure any buffered content is asynchronously written to the underlying
            // response asynchronously. In the absence of this line, the buffer gets synchronously written to the
            // response as part of the Dispose which has a perf impact.
            await writer.FlushAsync();
        });
    }

    private static void EmitStreamingRenderingUpdate(IEnumerable<HtmlComponent> htmlComponents, TextWriter writer)
    {
        foreach (var entry in htmlComponents)
        {
            // This relies on the component producing well-formed markup (i.e., it can't have a closing
            // </template> at the top level without a preceding matching <template>). Alternatively we
            // could look at using a custom TextWriter that does some extra encoding of all the content
            // as it is being written out.
            writer.Write($"<template blazor-component-id=\"{entry.ComponentId}\">");
            entry.WriteHtmlTo(writer);
            writer.Write("</template>");
        }
    }

    private static void HandleException(HttpContext httpContext, TextWriter writer, Exception exception)
    {
        var env = httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var options = httpContext.RequestServices.GetRequiredService<IOptions<RazorComponentsEndpointsOptions>>();
        var showDetailedErrors = env.IsDevelopment() || options.Value.DetailedErrors;
        var message = showDetailedErrors
            ? exception.ToString()
            : "There was an unhandled exception on the current request. For more details turn on detailed exceptions by setting 'DetailedErrors: true' in 'appSettings.Development.json'";

        writer.Write("<template blazor-type=\"exception\">");
        writer.Write(message);
        writer.Write("</template>");
    }

    private static void HandleNavigation(HttpContext httpContext, TextWriter writer, string destinationUrl)
    {
        var response = httpContext.Response;
        if (response.HasStarted)
        {
            writer.Write("<template blazor-type=\"redirection\">");
            writer.Write(destinationUrl);
            writer.Write("</template>");
        }
        else
        {
            response.Redirect(destinationUrl);
        }
    }

    private static TextWriter CreateResponseWriter(Stream bodyStream)
    {
        // Matches MVC's MemoryPoolHttpResponseStreamWriterFactory.DefaultBufferSize
        const int DefaultBufferSize = 16 * 1024;
        return new HttpResponseStreamWriter(bodyStream, Encoding.UTF8, DefaultBufferSize, ArrayPool<byte>.Shared, ArrayPool<char>.Shared);
    }
}
