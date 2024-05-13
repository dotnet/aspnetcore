// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Endpoints.Rendering;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal partial class RazorComponentEndpointInvoker : IRazorComponentEndpointInvoker
{
    private readonly EndpointHtmlRenderer _renderer;
    private readonly ILogger<RazorComponentEndpointInvoker> _logger;

    public RazorComponentEndpointInvoker(EndpointHtmlRenderer renderer, ILogger<RazorComponentEndpointInvoker> logger)
    {
        _renderer = renderer;
        _logger = logger;
    }

    public Task Render(HttpContext context)
    {
        return _renderer.Dispatcher.InvokeAsync(() => RenderComponentCore(context));
    }

    private async Task RenderComponentCore(HttpContext context)
    {
        context.Response.ContentType = RazorComponentResultExecutor.DefaultContentType;
        var isErrorHandler = context.Features.Get<IExceptionHandlerFeature>() is not null;
        if (isErrorHandler)
        {
            Log.InteractivityDisabledForErrorHandling(_logger);
        }
        _renderer.InitializeStreamingRenderingFraming(context, isErrorHandler);
        EndpointHtmlRenderer.MarkAsAllowingEnhancedNavigation(context);

        var endpoint = context.GetEndpoint() ?? throw new InvalidOperationException($"An endpoint must be set on the '{nameof(HttpContext)}'.");

        var rootComponent = endpoint.Metadata.GetRequiredMetadata<RootComponentMetadata>().Type;
        var pageComponent = endpoint.Metadata.GetRequiredMetadata<ComponentTypeMetadata>().Type;

        Log.BeginRenderRootComponent(_logger, rootComponent.Name, pageComponent.Name);

        // Metadata controls whether we require antiforgery protection for this endpoint or we should skip it.
        // The default for razor component endpoints is to require the metadata, but it can be overriden by
        // the developer.
        var antiforgeryMetadata = endpoint.Metadata.GetMetadata<IAntiforgeryMetadata>();
        var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
        var result = await ValidateRequestAsync(context, antiforgeryMetadata?.RequiresValidation == true ? antiforgery : null);
        if (!result.IsValid)
        {
            // If the request is not valid we've already set the response to a 400 or similar
            // and we can just exit early.
            return;
        }

        context.Response.OnStarting(() =>
        {
            // Generate the antiforgery tokens before we start streaming the response, as it needs
            // to set the cookie header.
            antiforgery!.GetAndStoreTokens(context);
            return Task.CompletedTask;
        });

        await EndpointHtmlRenderer.InitializeStandardComponentServicesAsync(
            context,
            componentType: pageComponent,
            handler: result.HandlerName,
            form: result.HandlerName != null && context.Request.HasFormContentType ? await context.Request.ReadFormAsync() : null);

        // Matches MVC's MemoryPoolHttpResponseStreamWriterFactory.DefaultBufferSize
        var defaultBufferSize = 16 * 1024;
        await using var writer = new HttpResponseStreamWriter(context.Response.Body, Encoding.UTF8, defaultBufferSize, ArrayPool<byte>.Shared, ArrayPool<char>.Shared);
        using var bufferWriter = new BufferedTextWriter(writer);

        // Note that we always use Static rendering mode for the top-level output from a RazorComponentResult,
        // because you never want to serialize the invocation of RazorComponentResultHost. Instead, that host
        // component takes care of switching into your desired render mode when it produces its own output.
        var htmlContent = await _renderer.RenderEndpointComponent(
            context,
            rootComponent,
            ParameterView.Empty,
            waitForQuiescence: result.IsPost || isErrorHandler);

        Task quiesceTask;
        if (!result.IsPost)
        {
            quiesceTask = htmlContent.QuiescenceTask;
        }
        else
        {
            try
            {
                var isBadRequest = false;
                quiesceTask = _renderer.DispatchSubmitEventAsync(result.HandlerName, out isBadRequest);
                if (isBadRequest)
                {
                    return;
                }

                await _renderer.WaitForNonStreamingPendingTasks();
            }
            catch (NavigationException ex)
            {
                await EndpointHtmlRenderer.HandleNavigationException(context, ex);
                quiesceTask = Task.CompletedTask;
            }
        }

        if (!quiesceTask.IsCompleted)
        {
            // An incomplete QuiescenceTask indicates there may be streaming rendering updates.
            // Disable all response buffering and compression on IIS like SignalR's ServerSentEventsServerTransport does.
            var bufferingFeature = context.Features.GetRequiredFeature<IHttpResponseBodyFeature>();
            bufferingFeature.DisableBuffering();

            context.Response.Headers.ContentEncoding = "identity";
        }

        // Importantly, we must not yield this thread (which holds exclusive access to the renderer sync context)
        // in between the first call to htmlContent.WriteTo and the point where we start listening for subsequent
        // streaming SSR batches (inside SendStreamingUpdatesAsync). Otherwise some other code might dispatch to the
        // renderer sync context and cause a batch that would get missed.
        htmlContent.WriteTo(bufferWriter, HtmlEncoder.Default); // Don't use WriteToAsync, as per the comment above

        if (!quiesceTask.IsCompletedSuccessfully)
        {
            await _renderer.SendStreamingUpdatesAsync(context, quiesceTask, bufferWriter);
        }
        else
        {
            _renderer.EmitInitializersIfNecessary(context, bufferWriter);
        }

        // Emit comment containing state.
        if (!isErrorHandler)
        {
            var componentStateHtmlContent = await _renderer.PrerenderPersistedStateAsync(context);
            componentStateHtmlContent.WriteTo(bufferWriter, HtmlEncoder.Default);
        }

        // Invoke FlushAsync to ensure any buffered content is asynchronously written to the underlying
        // response asynchronously. In the absence of this line, the buffer gets synchronously written to the
        // response as part of the Dispose which has a perf impact.
        await bufferWriter.FlushAsync();
    }

    private async Task<RequestValidationState> ValidateRequestAsync(HttpContext context, IAntiforgery? antiforgery)
    {
        var processPost = HttpMethods.IsPost(context.Request.Method) &&
            // Disable POST functionality during exception handling.
            // The exception handler middleware will not update the request method, and we don't
            // want to run the form handling logic against the error page.
            context.Features.Get<IExceptionHandlerFeature>() == null;

        if (processPost)
        {
            if (context.Request.ContentType is not null && MediaTypeHeaderValue.TryParse(context.Request.ContentType, out var type))
            {
                // We can't use request.HasFormContentType here because it will throw. We should revisit that.
                if (!type.MediaType.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) &&
                    !type.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    if (EndpointHtmlRenderer.ShouldShowDetailedErrors(context))
                    {
                        await context.Response.WriteAsync("The request has an incorrect Content-type.");
                    }
                    return RequestValidationState.InvalidPostRequest;
                }
            }

            // Respect the token validation done by the middleware _if_ it has been set, otherwise
            // run the validation here.
            var valid = false;
            if (context.Features.Get<IAntiforgeryValidationFeature>() is { } antiForgeryValidationFeature)
            {
                if (!antiForgeryValidationFeature.IsValid)
                {
                    Log.MiddlewareAntiforgeryValidationFailed(_logger);
                }
                else
                {
                    valid = true;
                    Log.MiddlewareAntiforgeryValidationSucceeded(_logger);
                }
            }
            else
            {
                if (antiforgery == null)
                {
                    valid = true;
                    Log.EndpointAntiforgeryValidationDisabled(_logger);
                }
                else
                {
                    valid = await antiforgery.IsRequestValidAsync(context);
                    if (valid)
                    {
                        Log.EndpointAntiforgeryValidationSucceeded(_logger);
                    }
                    else
                    {
                        Log.EndpointAntiforgeryValidationFailed(_logger);
                    }
                }
            }

            if (!valid)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;

                if (EndpointHtmlRenderer.ShouldShowDetailedErrors(context))
                {
                    await context.Response.WriteAsync("A valid antiforgery token was not provided with the request. Add an antiforgery token, or disable antiforgery validation for this endpoint.");
                }
                return RequestValidationState.InvalidPostRequest;
            }

            // Read the form asynchronously to ensure Request.Form has been populated.
            await context.Request.ReadFormAsync();

            var handler = GetFormHandler(context, out var isBadRequest);
            return new(valid && !isBadRequest, processPost, handler);
        }

        return RequestValidationState.ValidNonPostRequest;
    }

    private static string? GetFormHandler(HttpContext context, out bool isBadRequest)
    {
        isBadRequest = false;
        if (context.Request.Form.TryGetValue("_handler", out var value))
        {
            if (value.Count != 1)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                isBadRequest = true;
            }
            else
            {
                return value[0]!;
            }
        }
        return null;
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
            return $"IsValid = {IsValid}, IsPost = {IsPost}, HandlerName = {HandlerName}";
        }
    }

    public static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Begin render root component '{componentType}' with page '{pageType}'.", EventName = nameof(BeginRenderRootComponent))]
        public static partial void BeginRenderRootComponent(ILogger<RazorComponentEndpointInvoker> logger, string componentType, string pageType);

        [LoggerMessage(2, LogLevel.Debug, "The antiforgery middleware already failed to validate the current token.", EventName = nameof(MiddlewareAntiforgeryValidationFailed))]
        public static partial void MiddlewareAntiforgeryValidationFailed(ILogger<RazorComponentEndpointInvoker> logger);

        [LoggerMessage(3, LogLevel.Debug, "The antiforgery middleware already succeeded to validate the current token.", EventName = nameof(MiddlewareAntiforgeryValidationSucceeded))]
        public static partial void MiddlewareAntiforgeryValidationSucceeded(ILogger<RazorComponentEndpointInvoker> logger);

        [LoggerMessage(4, LogLevel.Debug, "The endpoint disabled antiforgery token validation.", EventName = nameof(EndpointAntiforgeryValidationDisabled))]
        public static partial void EndpointAntiforgeryValidationDisabled(ILogger<RazorComponentEndpointInvoker> logger);

        [LoggerMessage(5, LogLevel.Information, "Antiforgery token validation failed for the current request.", EventName = nameof(EndpointAntiforgeryValidationFailed))]
        public static partial void EndpointAntiforgeryValidationFailed(ILogger<RazorComponentEndpointInvoker> logger);

        [LoggerMessage(6, LogLevel.Debug, "Antiforgery token validation succeeded for the current request.", EventName = nameof(EndpointAntiforgeryValidationSucceeded))]
        public static partial void EndpointAntiforgeryValidationSucceeded(ILogger<RazorComponentEndpointInvoker> logger);

        [LoggerMessage(7, LogLevel.Debug, "Error handling in progress. Interactive components are not enabled.", EventName = nameof(InteractivityDisabledForErrorHandling))]
        public static partial void InteractivityDisabledForErrorHandling(ILogger<RazorComponentEndpointInvoker> logger);
    }
}
