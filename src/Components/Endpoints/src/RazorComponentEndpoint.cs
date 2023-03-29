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
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal static class RazorComponentEndpoint
{
    public static RequestDelegate CreateRouteDelegate(Type componentType)
    {
        return httpContext =>
            RenderComponentToResponse(httpContext, RenderMode.Static, componentType, componentParameters: null, preventStreamingRendering: false);
    }

    internal static Task RenderComponentToResponse(
        HttpContext httpContext,
        RenderMode renderMode,
        Type componentType,
        IReadOnlyDictionary<string, object?>? componentParameters,
        bool preventStreamingRendering)
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

            // Note that we always use Static rendering mode for the top-level output from a RazorComponentResult,
            // because you never want to serialize the invocation of RazorComponentResultHost. Instead, that host
            // component takes care of switching into your desired render mode when it produces its own output.
            var htmlContent = (EndpointHtmlRenderer.PrerenderedComponentHtmlContent)(await endpointHtmlRenderer.PrerenderComponentAsync(
                httpContext,
                typeof(RazorComponentEndpointHost),
                RenderMode.Static,
                hostParameters,
                waitForQuiescence: preventStreamingRendering));

            // Importantly, we must not yield this thread (which holds exclusive access to the renderer sync context)
            // in between the first call to htmlContent.WriteTo and the point where we start listening for subsequent
            // streaming SSR batches. Otherwise some other code might dispatch to the renderer sync context and cause
            // a batch that would get missed.
            htmlContent.WriteTo(writer, HtmlEncoder.Default); // Don't use WriteToAsync, as per the comment above

            if (!htmlContent.QuiescenceTask.IsCompleted)
            {
                endpointHtmlRenderer.OnContentUpdated(htmlComponents =>
                    EmitStreamingRenderingUpdate(htmlComponents, writer));

                try
                {
                    await writer.FlushAsync(); // Make sure the initial HTML was sent
                    await htmlContent.QuiescenceTask;
                }
                catch (NavigationException navigationException)
                {
                    HandleNavigationAfterResponseStarted(writer, navigationException.Location);
                }
                catch (Exception ex)
                {
                    HandleExceptionAfterResponseStarted(httpContext, writer, ex);

                    // The rest of the pipeline can treat this as a regular unhandled exception
                    // TODO: Is this really right? I think we'll terminate the response in an invalid way.
                    throw;
                }
            }

            // Invoke FlushAsync to ensure any buffered content is asynchronously written to the underlying
            // response asynchronously. In the absence of this line, the buffer gets synchronously written to the
            // response as part of the Dispose which has a perf impact.
            await writer.FlushAsync();
        });
    }

    private static void EmitStreamingRenderingUpdate(IEnumerable<HtmlComponent> htmlComponents, TextWriter writer)
    {
        writer.Write("<blazor-ssr>");
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
        writer.Write("</blazor-ssr>");
    }

    private static void HandleExceptionAfterResponseStarted(HttpContext httpContext, TextWriter writer, Exception exception)
    {
        // We already started the response so we have no choice but to return a 200 with HTML and will
        // have to communicate the error information within that
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

    private static void HandleNavigationAfterResponseStarted(TextWriter writer, string destinationUrl)
    {
        writer.Write("<template blazor-type=\"redirection\">");
        writer.Write(destinationUrl);
        writer.Write("</template>");
    }

    private static TextWriter CreateResponseWriter(Stream bodyStream)
    {
        // Matches MVC's MemoryPoolHttpResponseStreamWriterFactory.DefaultBufferSize
        const int DefaultBufferSize = 16 * 1024;
        return new HttpResponseStreamWriter(bodyStream, Encoding.UTF8, DefaultBufferSize, ArrayPool<byte>.Shared, ArrayPool<char>.Shared);
    }
}
