// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
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
    /// <param name="diagnosticListener">The <see cref="System.Diagnostics.DiagnosticListener"/>.</param>
    public RazorComponentResultExecutor(
        IHttpResponseStreamWriterFactory writerFactory,
        DiagnosticListener diagnosticListener)
    {
        ArgumentNullException.ThrowIfNull(writerFactory);

        WriterFactory = writerFactory;
        DiagnosticListener = diagnosticListener;
    }

    /// <summary>
    /// Gets the <see cref="DiagnosticListener"/>.
    /// </summary>
    protected DiagnosticListener DiagnosticListener { get; }

    /// <summary>
    /// Gets the <see cref="IHttpResponseStreamWriterFactory"/>.
    /// </summary>
    protected IHttpResponseStreamWriterFactory WriterFactory { get; }

    /// <summary>
    /// Executes a Razor Component asynchronously.
    /// </summary>
    public virtual async Task ExecuteAsync(ActionContext actionContext, RazorComponentResult result)
    {
        ArgumentNullException.ThrowIfNull(actionContext);

        var response = actionContext.HttpContext.Response;

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

        var componentPrerenderer = actionContext.HttpContext.RequestServices.GetRequiredService<ComponentPrerenderer>();

        DiagnosticListener.BeforeRazorComponent(result.ComponentType, result.RenderMode, actionContext);
        await componentPrerenderer.Dispatcher.InvokeAsync(async () =>
        {
            var htmlContent = await componentPrerenderer.PrerenderComponentAsync(
                actionContext,
                result.ComponentType,
                result.RenderMode,
                result.Parameters);
            await htmlContent.WriteToAsync(writer);
        });

        DiagnosticListener.AfterRazorComponent(result.ComponentType, result.RenderMode, actionContext);

        // Perf: Invoke FlushAsync to ensure any buffered content is asynchronously written to the underlying
        // response asynchronously. In the absence of this line, the buffer gets synchronously written to the
        // response as part of the Dispose which has a perf impact.
        await writer.FlushAsync();
    }
}
