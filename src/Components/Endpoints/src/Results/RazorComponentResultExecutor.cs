// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// Executes a <see cref="RazorComponentResult"/>.
/// </summary>
public class RazorComponentResultExecutor
{
    /// <summary>
    /// The default content-type header value for Razor Components, <c>text/html; charset=utf-8</c>.
    /// </summary>
    public static readonly string DefaultContentType = "text/html; charset=utf-8";

    /// <summary>
    /// Executes a <see cref="RazorComponentResult"/> asynchronously.
    /// </summary>
    public virtual Task ExecuteAsync(HttpContext httpContext, RazorComponentResult result)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var response = httpContext.Response;
        response.ContentType = result.ContentType ?? DefaultContentType;

        if (result.StatusCode != null)
        {
            response.StatusCode = result.StatusCode.Value;
        }
        
        return RenderComponentToResponse(
            httpContext,
            result.ComponentType,
            result.Parameters,
            result.PreventStreamingRendering);
    }

    internal static Task RenderComponentToResponse(
        HttpContext httpContext,
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
                { nameof(RazorComponentEndpointHost.ComponentType), componentType },
                { nameof(RazorComponentEndpointHost.ComponentParameters), componentParameters },
            });

            await using var writer = CreateResponseWriter(httpContext.Response.Body);

            // Note that we don't set any interactive rendering mode for the top-level output from a RazorComponentResult,
            // because you never want to serialize the invocation of RazorComponentResultHost. Instead, that host
            // component takes care of switching into your desired render mode when it produces its own output.
            var htmlContent = (EndpointHtmlRenderer.PrerenderedComponentHtmlContent)(await endpointHtmlRenderer.PrerenderComponentAsync(
                httpContext,
                typeof(RazorComponentEndpointHost),
                null,
                hostParameters,
                waitForQuiescence: preventStreamingRendering));

            // Importantly, we must not yield this thread (which holds exclusive access to the renderer sync context)
            // in between the first call to htmlContent.WriteTo and the point where we start listening for subsequent
            // streaming SSR batches (inside SendStreamingUpdatesAsync). Otherwise some other code might dispatch to the
            // renderer sync context and cause a batch that would get missed.
            htmlContent.WriteTo(writer, HtmlEncoder.Default); // Don't use WriteToAsync, as per the comment above

            if (!htmlContent.QuiescenceTask.IsCompleted)
            {
                await endpointHtmlRenderer.SendStreamingUpdatesAsync(httpContext, htmlContent.QuiescenceTask, writer);
            }

            // Invoke FlushAsync to ensure any buffered content is asynchronously written to the underlying
            // response asynchronously. In the absence of this line, the buffer gets synchronously written to the
            // response as part of the Dispose which has a perf impact.
            await writer.FlushAsync();
        });
    }

    private static TextWriter CreateResponseWriter(Stream bodyStream)
    {
        // Matches MVC's MemoryPoolHttpResponseStreamWriterFactory.DefaultBufferSize
        const int DefaultBufferSize = 16 * 1024;
        return new HttpResponseStreamWriter(bodyStream, Encoding.UTF8, DefaultBufferSize, ArrayPool<byte>.Shared, ArrayPool<char>.Shared);
    }
}
