// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
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
    public virtual async Task ExecuteAsync(HttpContext httpContext, RazorComponentResult result)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var response = httpContext.Response;
        response.ContentType = result.ContentType ?? DefaultContentType;

        if (result.StatusCode != null)
        {
            response.StatusCode = result.StatusCode.Value;
        }

        await using var writer = CreateResponseWriter(response.Body);
        await RenderComponentToResponse(httpContext, result, writer);

        // Perf: Invoke FlushAsync to ensure any buffered content is asynchronously written to the underlying
        // response asynchronously. In the absence of this line, the buffer gets synchronously written to the
        // response as part of the Dispose which has a perf impact.
        await writer.FlushAsync();
    }

    private static TextWriter CreateResponseWriter(Stream bodyStream)
    {
        // Matches MVC's MemoryPoolHttpResponseStreamWriterFactory.DefaultBufferSize
        const int DefaultBufferSize = 16 * 1024;
        return new HttpResponseStreamWriter(bodyStream, Encoding.UTF8, DefaultBufferSize, ArrayPool<byte>.Shared, ArrayPool<char>.Shared);
    }

    private static Task RenderComponentToResponse(HttpContext httpContext, RazorComponentResult result, TextWriter writer)
    {
        var componentPrerenderer = httpContext.RequestServices.GetRequiredService<IComponentPrerenderer>();
        return componentPrerenderer.Dispatcher.InvokeAsync(async () =>
        {
            // We could pool these dictionary instances if we wanted. We could even skip the dictionary
            // phase entirely if we added some new ParameterView.FromSingleValue(name, value) API.
            var hostParameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                { nameof(RazorComponentResultHost.RazorComponentResult), result },
            });

            // Note that we always use Static rendering mode for the top-level output from a RazorComponentResult,
            // because you never want to serialize the invocation of RazorComponentResultHost. Instead, that host
            // component takes care of switching into your desired render mode when it produces its own output.
            var htmlContent = await componentPrerenderer.PrerenderComponentAsync(
                httpContext,
                typeof(RazorComponentResultHost),
                RenderMode.Static,
                hostParameters);
            await htmlContent.WriteToAsync(writer);
        });
    }
}
