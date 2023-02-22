// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Diagnostics;

/// <summary>
/// A middleware for handling exceptions in the application.
/// </summary>
internal class ExceptionHandlerMiddlewareImpl
{
    private const int DefaultStatusCode = StatusCodes.Status500InternalServerError;

    private readonly RequestDelegate _next;
    private readonly ExceptionHandlerOptions _options;
    private readonly ILogger _logger;
    private readonly Func<object, Task> _clearCacheHeadersDelegate;
    private readonly DiagnosticListener _diagnosticListener;
    private readonly IProblemDetailsService? _problemDetailsService;

    /// <summary>
    /// Creates a new <see cref="ExceptionHandlerMiddleware"/>
    /// </summary>
    /// <param name="next">The <see cref="RequestDelegate"/> representing the next middleware in the pipeline.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> used for logging.</param>
    /// <param name="options">The options for configuring the middleware.</param>
    /// <param name="diagnosticListener">The <see cref="DiagnosticListener"/> used for writing diagnostic messages.</param>
    /// <param name="problemDetailsService">The <see cref="IProblemDetailsService"/> used for writing <see cref="ProblemDetails"/> messages.</param>
    public ExceptionHandlerMiddlewareImpl(
        RequestDelegate next,
        ILoggerFactory loggerFactory,
        IOptions<ExceptionHandlerOptions> options,
        DiagnosticListener diagnosticListener,
        IProblemDetailsService? problemDetailsService = null)
    {
        _next = next;
        _options = options.Value;
        _logger = loggerFactory.CreateLogger<ExceptionHandlerMiddleware>();
        _clearCacheHeadersDelegate = ClearCacheHeaders;
        _diagnosticListener = diagnosticListener;
        _problemDetailsService = problemDetailsService;

        if (_options.ExceptionHandler == null)
        {
            if (_options.ExceptionHandlingPath == null)
            {
                if (problemDetailsService == null)
                {
                    throw new InvalidOperationException(Resources.ExceptionHandlerOptions_NotConfiguredCorrectly);
                }
            }
            else
            {
                _options.ExceptionHandler = _next;
            }
        }
    }

    /// <summary>
    /// Executes the middleware.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> for the current request.</param>
    public Task Invoke(HttpContext context)
    {
        ExceptionDispatchInfo edi;
        try
        {
            var task = _next(context);
            if (!task.IsCompletedSuccessfully)
            {
                return Awaited(this, context, task);
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            // Get the Exception, but don't continue processing in the catch block as its bad for stack usage.
            edi = ExceptionDispatchInfo.Capture(exception);
        }

        return HandleException(context, edi);

        static async Task Awaited(ExceptionHandlerMiddlewareImpl middleware, HttpContext context, Task task)
        {
            ExceptionDispatchInfo? edi = null;
            try
            {
                await task;
            }
            catch (Exception exception)
            {
                // Get the Exception, but don't continue processing in the catch block as its bad for stack usage.
                edi = ExceptionDispatchInfo.Capture(exception);
            }

            if (edi != null)
            {
                await middleware.HandleException(context, edi);
            }
        }
    }

    private async Task HandleException(HttpContext context, ExceptionDispatchInfo edi)
    {
        if ((edi.SourceException is OperationCanceledException || edi.SourceException is IOException) && context.RequestAborted.IsCancellationRequested)
        {
            _logger.RequestAbortedException();

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            }

            return;
        }

        _logger.UnhandledException(edi.SourceException);
        // We can't do anything if the response has already started, just abort.
        if (context.Response.HasStarted)
        {
            _logger.ResponseStartedErrorHandler();
            edi.Throw();
        }

        PathString originalPath = context.Request.Path;
        if (_options.ExceptionHandlingPath.HasValue)
        {
            context.Request.Path = _options.ExceptionHandlingPath;
        }
        try
        {
            var exceptionHandlerFeature = new ExceptionHandlerFeature()
            {
                Error = edi.SourceException,
                Path = originalPath.Value!,
                Endpoint = context.GetEndpoint(),
                RouteValues = context.Features.Get<IRouteValuesFeature>()?.RouteValues
            };

            ClearHttpContext(context);

            context.Features.Set<IExceptionHandlerFeature>(exceptionHandlerFeature);
            context.Features.Set<IExceptionHandlerPathFeature>(exceptionHandlerFeature);
            context.Response.StatusCode = DefaultStatusCode;
            context.Response.OnStarting(_clearCacheHeadersDelegate, context.Response);

            var problemDetailsWritten = false;
            if (_options.ExceptionHandler != null)
            {
                await _options.ExceptionHandler!(context);
            }
            else
            {
                problemDetailsWritten = await _problemDetailsService!.TryWriteAsync(new()
                {
                    HttpContext = context,
                    AdditionalMetadata = exceptionHandlerFeature.Endpoint?.Metadata,
                    ProblemDetails = { Status = DefaultStatusCode }
                });
            }

            // If the response has already started, assume exception handler was successful.
            if (context.Response.HasStarted || problemDetailsWritten || context.Response.StatusCode != StatusCodes.Status404NotFound || _options.AllowStatusCode404Response)
            {
                const string eventName = "Microsoft.AspNetCore.Diagnostics.HandledException";
                if (_diagnosticListener.IsEnabled() && _diagnosticListener.IsEnabled(eventName))
                {
                    WriteDiagnosticEvent(_diagnosticListener, eventName, new { httpContext = context, exception = edi.SourceException });
                }

                return;
            }

            edi = ExceptionDispatchInfo.Capture(new InvalidOperationException($"The exception handler configured on {nameof(ExceptionHandlerOptions)} produced a 404 status response. " +
                $"This {nameof(InvalidOperationException)} containing the original exception was thrown since this is often due to a misconfigured {nameof(ExceptionHandlerOptions.ExceptionHandlingPath)}. " +
                $"If the exception handler is expected to return 404 status responses then set {nameof(ExceptionHandlerOptions.AllowStatusCode404Response)} to true.", edi.SourceException));
        }
        catch (Exception ex2)
        {
            // Suppress secondary exceptions, re-throw the original.
            _logger.ErrorHandlerException(ex2);
        }
        finally
        {
            context.Request.Path = originalPath;
        }

        edi.Throw(); // Re-throw wrapped exception or the original if we couldn't handle it

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026",
            Justification = "The values being passed into Write have the commonly used properties being preserved with DynamicDependency.")]
        static void WriteDiagnosticEvent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(DiagnosticSource diagnosticSource, string name, TValue value)
            => diagnosticSource.Write(name, value);
    }

    private static void ClearHttpContext(HttpContext context)
    {
        context.Response.Clear();

        // An endpoint may have already been set. Since we're going to re-invoke the middleware pipeline we need to reset
        // the endpoint and route values to ensure things are re-calculated.
        context.SetEndpoint(endpoint: null);
        var routeValuesFeature = context.Features.Get<IRouteValuesFeature>();
        if (routeValuesFeature != null)
        {
            routeValuesFeature.RouteValues = null!;
        }
    }

    private static Task ClearCacheHeaders(object state)
    {
        var headers = ((HttpResponse)state).Headers;
        headers.CacheControl = "no-cache,no-store";
        headers.Pragma = "no-cache";
        headers.Expires = "-1";
        headers.ETag = default;
        return Task.CompletedTask;
    }
}
