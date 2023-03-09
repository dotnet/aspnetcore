// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// Executes a Razor Component.
/// </summary>
public class RazorComponentResultExecutor
{
    /// <summary>
    /// The default content-type header value for Razor Components, <c>text/html; charset=utf-8</c>.
    /// </summary>
    public static readonly string DefaultContentType = "text/html; charset=utf-8";

    /// <summary>
    /// Constructs an instance of <see cref="RazorComponentResultExecutor"/>.
    /// </summary>
    /// <param name="writerFactory">The <see cref="IHttpResponseStreamWriterFactory"/>.</param>
    public RazorComponentResultExecutor(
        IHttpResponseStreamWriterFactory writerFactory)
    {
        ArgumentNullException.ThrowIfNull(writerFactory);
        WriterFactory = writerFactory;
    }

    /// <summary>
    /// Gets the <see cref="IHttpResponseStreamWriterFactory"/>.
    /// </summary>
    protected IHttpResponseStreamWriterFactory WriterFactory { get; }

    /// <summary>
    /// Executes a Razor Component asynchronously.
    /// </summary>
    public virtual async Task ExecuteAsync(HttpContext httpContext, RazorComponentResult result)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var response = httpContext.Response;

        ResponseContentTypeHelper.ResolveContentTypeAndEncoding(
            result.ContentType,
            response.ContentType,
            (DefaultContentType, Encoding.UTF8),
            MediaType.GetEncoding,
            out var resolvedContentType,
            out var resolvedContentTypeEncoding);

        response.ContentType = resolvedContentType;

        if (result.StatusCode != null)
        {
            response.StatusCode = result.StatusCode.Value;
        }

        await using var writer = WriterFactory.CreateWriter(response.Body, resolvedContentTypeEncoding);
        await RenderComponentToResponse(httpContext, result, writer);

        // Perf: Invoke FlushAsync to ensure any buffered content is asynchronously written to the underlying
        // response asynchronously. In the absence of this line, the buffer gets synchronously written to the
        // response as part of the Dispose which has a perf impact.
        await writer.FlushAsync();
    }

    private static Task RenderComponentToResponse(HttpContext httpContext, RazorComponentResult result, TextWriter writer)
    {
        var componentPrerenderer = httpContext.RequestServices.GetRequiredService<ComponentPrerenderer>();
        return componentPrerenderer.Dispatcher.InvokeAsync(async () =>
        {
            // We could pool these dictionary instances if we wanted. We could even skip the dictionary
            // phase entirely if we added some new ParameterView.FromSingleValue(name, value) API.
            var hostParameters = ParameterView.FromDictionary(new Dictionary<string, object>
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
