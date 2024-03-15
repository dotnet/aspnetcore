// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.RazorViews;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.StackTrace.Sources;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Diagnostics;

/// <summary>
/// Captures synchronous and asynchronous exceptions from the pipeline and generates error responses.
/// </summary>
internal class DeveloperExceptionPageMiddlewareImpl
{
    private readonly RequestDelegate _next;
    private readonly DeveloperExceptionPageOptions _options;
    private readonly ILogger _logger;
    private readonly IFileProvider _fileProvider;
    private readonly DiagnosticSource _diagnosticSource;
    private readonly DiagnosticsMetrics _metrics;
    private readonly ExceptionDetailsProvider _exceptionDetailsProvider;
    private readonly Func<ErrorContext, Task> _exceptionHandler;
    private static readonly MediaTypeHeaderValue _textHtmlMediaType = new MediaTypeHeaderValue("text/html");
    private readonly ExtensionsExceptionJsonContext _serializationContext;
    private readonly IProblemDetailsService? _problemDetailsService;

    public DeveloperExceptionPageMiddlewareImpl(
        RequestDelegate next,
        IOptions<DeveloperExceptionPageOptions> options,
        ILoggerFactory loggerFactory,
        IWebHostEnvironment hostingEnvironment,
        DiagnosticSource diagnosticSource,
        IEnumerable<IDeveloperPageExceptionFilter> filters,
        IMeterFactory meterFactory,
        IOptions<JsonOptions>? jsonOptions = null,
        IProblemDetailsService? problemDetailsService = null)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(filters);

        _next = next;
        _options = options.Value;
        _logger = loggerFactory.CreateLogger<DeveloperExceptionPageMiddleware>();
        _fileProvider = _options.FileProvider ?? hostingEnvironment.ContentRootFileProvider;
        _diagnosticSource = diagnosticSource;
        _metrics = new DiagnosticsMetrics(meterFactory);
        _exceptionDetailsProvider = new ExceptionDetailsProvider(_fileProvider, _logger, _options.SourceCodeLineCount);
        _exceptionHandler = DisplayException;
        _serializationContext = CreateSerializationContext(jsonOptions?.Value);
        _problemDetailsService = problemDetailsService;
        foreach (var filter in filters.Reverse())
        {
            var nextFilter = _exceptionHandler;
            _exceptionHandler = errorContext => filter.HandleExceptionAsync(errorContext, nextFilter);
        }
    }

    private static ExtensionsExceptionJsonContext CreateSerializationContext(JsonOptions? jsonOptions)
    {
        // Create context from configured options to get settings such as PropertyNamePolicy and DictionaryKeyPolicy.
        jsonOptions ??= new JsonOptions();
        return new ExtensionsExceptionJsonContext(new JsonSerializerOptions(jsonOptions.SerializerOptions));
    }

    private static void SetExceptionHandlerFeatures(ErrorContext errorContext)
    {
        var httpContext = errorContext.HttpContext;

        var exceptionHandlerFeature = new ExceptionHandlerFeature()
        {
            Error = errorContext.Exception,
            Path = httpContext.Request.Path.ToString(),
            Endpoint = httpContext.GetEndpoint(),
            RouteValues = httpContext.Features.Get<IRouteValuesFeature>()?.RouteValues
        };

        httpContext.Features.Set<IExceptionHandlerFeature>(exceptionHandlerFeature);
        httpContext.Features.Set<IExceptionHandlerPathFeature>(exceptionHandlerFeature);
    }

    /// <summary>
    /// Process an individual request.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var exceptionName = ex.GetType().FullName!;

            if ((ex is OperationCanceledException || ex is IOException) && context.RequestAborted.IsCancellationRequested)
            {
                _logger.RequestAbortedException();
                _metrics.RequestException(exceptionName, ExceptionResult.Aborted, handler: null);

                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
                }

                return;
            }

            DiagnosticsTelemetry.ReportUnhandledException(_logger, context, ex);

            if (context.Response.HasStarted)
            {
                _logger.ResponseStartedErrorPageMiddleware();
                _metrics.RequestException(exceptionName, ExceptionResult.Skipped, handler: null);
                throw;
            }

            try
            {
                context.Response.Clear();

                // Preserve the status code that would have been written by the server automatically when a BadHttpRequestException is thrown.
                if (ex is BadHttpRequestException badHttpRequestException)
                {
                    context.Response.StatusCode = badHttpRequestException.StatusCode;
                }
                else
                {
                    context.Response.StatusCode = 500;
                }

                await _exceptionHandler(new ErrorContext(context, ex));

                const string eventName = "Microsoft.AspNetCore.Diagnostics.UnhandledException";
                if (_diagnosticSource.IsEnabled(eventName))
                {
                    WriteDiagnosticEvent(_diagnosticSource, eventName, new { httpContext = context, exception = ex });
                }

                _metrics.RequestException(exceptionName, ExceptionResult.Unhandled, handler: null);
                return;
            }
            catch (Exception ex2)
            {
                // If there's a Exception while generating the error page, re-throw the original exception.
                _logger.DisplayErrorPageException(ex2);
            }

            _metrics.RequestException(exceptionName, ExceptionResult.Unhandled, handler: null);
            throw;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026",
            Justification = "The values being passed into Write have the commonly used properties being preserved with DynamicDependency.")]
        static void WriteDiagnosticEvent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(DiagnosticSource diagnosticSource, string name, TValue value)
            => diagnosticSource.Write(name, value);
    }

    // Assumes the response headers have not been sent.  If they have, still attempt to write to the body.
    private Task DisplayException(ErrorContext errorContext)
    {
        var httpContext = errorContext.HttpContext;
        var headers = httpContext.Request.GetTypedHeaders();
        var acceptHeader = headers.Accept;

        // If the client does not ask for HTML just format the exception as plain text
        if (acceptHeader == null || !acceptHeader.Any(h => h.IsSubsetOf(_textHtmlMediaType)))
        {
            return DisplayExceptionContent(errorContext);
        }

        if (errorContext.Exception is ICompilationException compilationException)
        {
            return DisplayCompilationException(httpContext, compilationException);
        }

        return DisplayRuntimeException(httpContext, errorContext.Exception);
    }

    private async Task DisplayExceptionContent(ErrorContext errorContext)
    {
        var httpContext = errorContext.HttpContext;

        if (_problemDetailsService is not null)
        {
            SetExceptionHandlerFeatures(errorContext);
        }

        if (_problemDetailsService == null || !await _problemDetailsService.TryWriteAsync(new()
            {
                HttpContext = httpContext,
                ProblemDetails = CreateProblemDetails(errorContext, httpContext), 
                Exception = errorContext.Exception 
            }))
        {
            httpContext.Response.ContentType = "text/plain; charset=utf-8";

            var sb = new StringBuilder();
            sb.AppendLine(errorContext.Exception.ToString());
            sb.AppendLine();
            sb.AppendLine("HEADERS");
            sb.AppendLine("=======");
            foreach (var pair in httpContext.Request.Headers)
            {
                sb.AppendLine(FormattableString.Invariant($"{pair.Key}: {pair.Value}"));
            }

            await httpContext.Response.WriteAsync(sb.ToString());
        }
    }

    private ProblemDetails CreateProblemDetails(ErrorContext errorContext, HttpContext httpContext)
    {
        var problemDetails = new ProblemDetails
        {
            Title = TypeNameHelper.GetTypeDisplayName(errorContext.Exception.GetType()),
            Detail = errorContext.Exception.Message,
            Status = httpContext.Response.StatusCode
        };

        // Problem details source gen serialization doesn't know about IHeaderDictionary or RouteValueDictionary.
        // Serialize payload to a JsonElement here. Problem details serialization can write JsonElement in extensions dictionary.
        problemDetails.Extensions["exception"] = JsonSerializer.SerializeToElement(new ExceptionExtensionData
        (
            details: errorContext.Exception.ToString(),
            headers: httpContext.Request.Headers,
            path: httpContext.Request.Path.ToString(),
            endpoint: httpContext.GetEndpoint()?.ToString(),
            routeValues: httpContext.Features.Get<IRouteValuesFeature>()?.RouteValues
        ), _serializationContext.ExceptionExtensionData);

        return problemDetails;
    }

    private Task DisplayCompilationException(
        HttpContext context,
        ICompilationException compilationException)
    {
        var model = new CompilationErrorPageModel(_options);

        var errorPage = new CompilationErrorPage(model);

        if (compilationException.CompilationFailures == null)
        {
            return errorPage.ExecuteAsync(context);
        }

        foreach (var compilationFailure in compilationException.CompilationFailures)
        {
            if (compilationFailure == null)
            {
                continue;
            }

            var stackFrames = new List<StackFrameSourceCodeInfo>();
            var exceptionDetails = new ExceptionDetails(compilationFailure.FailureSummary!, stackFrames);
            model.ErrorDetails.Add(exceptionDetails);
            model.CompiledContent.Add(compilationFailure.CompiledContent);

            if (compilationFailure.Messages == null)
            {
                continue;
            }

            var sourceLines = compilationFailure
                    .SourceFileContent?
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            foreach (var item in compilationFailure.Messages)
            {
                if (item == null)
                {
                    continue;
                }

                var frame = new StackFrameSourceCodeInfo
                {
                    File = compilationFailure.SourceFilePath,
                    Line = item.StartLine,
                    Function = string.Empty
                };

                if (sourceLines != null)
                {
                    _exceptionDetailsProvider.ReadFrameContent(frame, sourceLines, item.StartLine, item.EndLine);
                }

                frame.ErrorDetails = item.Message;

                stackFrames.Add(frame);
            }
        }

        return errorPage.ExecuteAsync(context);
    }

    private Task DisplayRuntimeException(HttpContext context, Exception ex)
    {
        var endpoint = context.GetEndpoint();

        EndpointModel? endpointModel = null;
        if (endpoint != null)
        {
            endpointModel = new EndpointModel
            {
                DisplayName = endpoint.DisplayName,
                Metadata = endpoint.Metadata
            };

            if (endpoint is RouteEndpoint routeEndpoint)
            {
                endpointModel.RoutePattern = routeEndpoint.RoutePattern.RawText;
                endpointModel.Order = routeEndpoint.Order;

                var httpMethods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods;
                if (httpMethods != null)
                {
                    endpointModel.HttpMethods = string.Join(", ", httpMethods);
                }
            }
        }

        var request = context.Request;
        var title = Resources.ErrorPageHtml_Title;

        if (ex is BadHttpRequestException badHttpRequestException)
        {
            var badRequestReasonPhrase = WebUtilities.ReasonPhrases.GetReasonPhrase(badHttpRequestException.StatusCode);

            if (!string.IsNullOrEmpty(badRequestReasonPhrase))
            {
                title = badRequestReasonPhrase;
            }
        }

        var model = new ErrorPageModel
        {
            Options = _options,
            ErrorDetails = _exceptionDetailsProvider.GetDetails(ex),
            Query = request.Query,
            Cookies = request.Cookies,
            Headers = request.Headers,
            RouteValues = request.RouteValues,
            Endpoint = endpointModel,
            Title = title,
        };

        var errorPage = new ErrorPage(model);
        return errorPage.ExecuteAsync(context);
    }
}

internal sealed class ExceptionExtensionData
{
    public ExceptionExtensionData(string details, IHeaderDictionary headers, string path, string? endpoint, RouteValueDictionary? routeValues)
    {
        Details = details;
        Headers = headers;
        Path = path;
        Endpoint = endpoint;
        RouteValues = routeValues;
    }

    public string Details { get; }
    public IHeaderDictionary Headers { get; }
    public string Path { get; }
    public string? Endpoint { get; }
    public RouteValueDictionary? RouteValues { get; }
}

[JsonSerializable(typeof(ExceptionExtensionData))]
internal sealed partial class ExtensionsExceptionJsonContext : JsonSerializerContext
{
}
