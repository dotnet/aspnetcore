// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        var (valid, isPost, handler) = await ValidateRequestAsync(antiforgeryMetadata?.RequiresValidation == true ? antiforgery : null);
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

        Task quiesceTask;
        if (!isPost)
        {
            quiesceTask = htmlContent.QuiescenceTask;
        }
        else
        {
            try
            {
                var isBadRequest = false;
                quiesceTask = _renderer.DispatchSubmitEventAsync(handler, out isBadRequest);
                if (isBadRequest)
                {
                    return;
                }

                await Task.WhenAll(_renderer.NonStreamingPendingTasks);
            }
            catch (NavigationException ex)
            {
                await EndpointHtmlRenderer.HandleNavigationException(_context, ex);
                quiesceTask = Task.CompletedTask;
            }
        }

        // Importantly, we must not yield this thread (which holds exclusive access to the renderer sync context)
        // in between the first call to htmlContent.WriteTo and the point where we start listening for subsequent
        // streaming SSR batches (inside SendStreamingUpdatesAsync). Otherwise some other code might dispatch to the
        // renderer sync context and cause a batch that would get missed.
        htmlContent.WriteTo(writer, HtmlEncoder.Default); // Don't use WriteToAsync, as per the comment above

        if (!quiesceTask.IsCompletedSuccessfully)
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
            // Respect the token validation done by the middleware _if_ it has been set, otherwise
            // run the validation here.
            var valid = _context.Features.Get<IAntiforgeryValidationFeature>() is {} antiForgeryValidationFeature
                ? antiForgeryValidationFeature.IsValid
                : antiforgery == null || await antiforgery.IsRequestValidAsync(_context);
            if (!valid)
            {
                _context.Response.StatusCode = StatusCodes.Status400BadRequest;

                if (_context.RequestServices.GetService<IHostEnvironment>()?.IsDevelopment() == true)
                {
                    await _context.Response.WriteAsync("A valid antiforgery token was not provided with the request. Add an antiforgery token, or disable antiforgery validation for this endpoint.");
                }
                return RequestValidationState.InvalidPostRequest;
            }

            var handler = GetFormHandler(out var isBadRequest);
            return new(valid && !isBadRequest, isPost, handler);
        }

        return RequestValidationState.ValidNonPostRequest;
    }

    private string? GetFormHandler(out bool isBadRequest)
    {
        isBadRequest = false;
        if (_context.Request.Form.TryGetValue("_handler", out var value))
        {
            if (value.Count != 1)
            {
                _context.Response.StatusCode = StatusCodes.Status400BadRequest;
                isBadRequest = true;
            }
            else
            {
                return value[0]!;
            }
        }
        return null;
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
        public static readonly RequestValidationState ValidNonPostRequest = new(true, false, null);
        public static readonly RequestValidationState InvalidPostRequest = new(false, true, null);

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
