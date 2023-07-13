// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Antiforgery.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal class RazorComponentEndpointInvoker
{
    private readonly HttpContext _context;
    private readonly EndpointHtmlRenderer _renderer;
    private readonly Type _rootComponentType;
    private readonly Type _componentType;

    public RazorComponentEndpointInvoker(HttpContext context, Type rootComponentType, Type componentType)
    {
        _context = context;
        _renderer = _context.RequestServices.GetRequiredService<EndpointHtmlRenderer>();
        _rootComponentType = rootComponentType;
        _componentType = componentType;
    }

    public Task RenderComponent()
    {
        return _renderer.Dispatcher.InvokeAsync(RenderComponentCore);
    }

    private async Task RenderComponentCore()
    {
        _context.Response.ContentType = RazorComponentResultExecutor.DefaultContentType;
        _renderer.InitializeStreamingRenderingFraming(_context);

        // Metadata controls whether we require antiforgery protection for this endpoint or we should skip it.
        // The default for razor component endpoints is to require the metadata, but it can be overriden by
        // the developer.
        var antiforgeryMetadata = _context.GetEndpoint()!.Metadata.GetMetadata<IAntiforgeryMetadata>();
        var antiforgery = _context.RequestServices.GetRequiredService<IAntiforgery>();
        var (valid, isPost, handler) = await ValidateRequestAsync(antiforgeryMetadata?.Required == true ? antiforgery : null);
        if (!valid)
        {
            // If the request is not valid we've already set the response to a 400 or similar
            // and we can just exit early.
            return;
        }

        _context.Response.OnStarting(() =>
        {
            // Generate the antiforgery tokens before we start streaming the response, as it needs
            // to set the cookie header.
            antiforgery!.GetAndStoreTokens(_context);
            return Task.CompletedTask;
        });

        await EndpointHtmlRenderer.InitializeStandardComponentServicesAsync(
            _context,
            componentType: _componentType,
            handler: handler,
            form: handler != null && _context.Request.HasFormContentType ? await _context.Request.ReadFormAsync() : null);

        await using var writer = CreateResponseWriter(_context.Response.Body);

        // Note that we always use Static rendering mode for the top-level output from a RazorComponentResult,
        // because you never want to serialize the invocation of RazorComponentResultHost. Instead, that host
        // component takes care of switching into your desired render mode when it produces its own output.
        var htmlContent = await _renderer.RenderEndpointComponent(
            _context,
            _rootComponentType,
            ParameterView.Empty,
            waitForQuiescence: isPost);

        var quiesceTask = isPost ? _renderer.DispatchSubmitEventAsync(handler) : htmlContent.QuiescenceTask;

        if (isPost)
        {
            await Task.WhenAll(_renderer.NonStreamingPendingTasks);
        }

        // Importantly, we must not yield this thread (which holds exclusive access to the renderer sync context)
        // in between the first call to htmlContent.WriteTo and the point where we start listening for subsequent
        // streaming SSR batches (inside SendStreamingUpdatesAsync). Otherwise some other code might dispatch to the
        // renderer sync context and cause a batch that would get missed.
        htmlContent.WriteTo(writer, HtmlEncoder.Default); // Don't use WriteToAsync, as per the comment above

        if (!quiesceTask.IsCompleted)
        {
            await _renderer.SendStreamingUpdatesAsync(_context, quiesceTask, writer);
        }

        // Invoke FlushAsync to ensure any buffered content is asynchronously written to the underlying
        // response asynchronously. In the absence of this line, the buffer gets synchronously written to the
        // response as part of the Dispose which has a perf impact.
        await writer.FlushAsync();
    }

    private async Task<RequestValidationState> ValidateRequestAsync(IAntiforgery? antiforgery)
    {
        var isPost = HttpMethods.IsPost(_context.Request.Method);
        if (isPost)
        {
            var valid = antiforgery == null || await antiforgery.IsRequestValidAsync(_context);
            if (!valid)
            {
                _context.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
            var formValid = TrySetFormHandler(out var handler);
            return new(valid && formValid, isPost, handler);
        }

        return new(true, false, null);
    }

    private bool TrySetFormHandler([NotNullWhen(true)] out string? handler)
    {
        handler = "";
        if (_context.Request.Query.TryGetValue("handler", out var value))
        {
            if (value.Count != 1)
            {
                _context.Response.StatusCode = StatusCodes.Status400BadRequest;
                handler = null;
                return false;
            }
            else
            {
                handler = value[0]!;
            }
        }

        return true;
    }

    private static TextWriter CreateResponseWriter(Stream bodyStream)
    {
        // Matches MVC's MemoryPoolHttpResponseStreamWriterFactory.DefaultBufferSize
        const int DefaultBufferSize = 16 * 1024;
        return new HttpResponseStreamWriter(bodyStream, Encoding.UTF8, DefaultBufferSize, ArrayPool<byte>.Shared, ArrayPool<char>.Shared);
    }

    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    private readonly struct RequestValidationState(bool isValid, bool isPost, string? handlerName)
    {
        public bool IsValid => isValid;

        public bool IsPost => isPost;

        public string? HandlerName => handlerName;

        private string GetDebuggerDisplay()
        {
            return $"{nameof(RequestValidationState)}: {IsValid} {IsPost} {HandlerName}";
        }

        public void Deconstruct(out bool isValid, out bool isPost, out string? handlerName)
        {
            isValid = IsValid;
            isPost = IsPost;
            handlerName = HandlerName;
        }
    }
}
