// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.HttpLogging;

/// <summary>
/// Middleware that logs HTTP requests and HTTP responses.
/// </summary>
internal sealed class HttpLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;
    private readonly ObjectPool<HttpLoggingInterceptorContext> _contextPool;
    private readonly TimeProvider _timeProvider;
    private readonly IHttpLoggingInterceptor[] _interceptors;
    private readonly IOptionsMonitor<HttpLoggingOptions> _options;
    private const string Redacted = "[Redacted]";

    public HttpLoggingMiddleware(RequestDelegate next, IOptionsMonitor<HttpLoggingOptions> options, ILogger<HttpLoggingMiddleware> logger,
        IEnumerable<IHttpLoggingInterceptor> interceptors, ObjectPool<HttpLoggingInterceptorContext> contextPool, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(interceptors);
        ArgumentNullException.ThrowIfNull(contextPool);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _next = next;
        _options = options;
        _logger = logger;
        _interceptors = interceptors.ToArray();
        _contextPool = contextPool;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Invokes the <see cref="HttpLoggingMiddleware" />.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>HttpResponseLog.cs
    public Task Invoke(HttpContext context)
    {
        if (!_logger.IsEnabled(LogLevel.Information))
        {
            // Logger isn't enabled.
            return _next(context);
        }

        var options = _options.CurrentValue;
        var loggingAttribute = context.GetEndpoint()?.Metadata.GetMetadata<HttpLoggingAttribute>();
        var loggingFields = loggingAttribute?.LoggingFields ?? options.LoggingFields;

        if (_interceptors.Length == 0 && loggingFields == HttpLoggingFields.None)
        {
            // Logging is disabled for this endpoint and there are no interceptors to turn it on.
            return _next(context);
        }

        return InvokeInternal(context, options, loggingAttribute, loggingFields);
    }

    private async Task InvokeInternal(HttpContext context, HttpLoggingOptions options,
        HttpLoggingAttribute? loggingAttribute, HttpLoggingFields loggingFields)
    {
        RequestBufferingStream? requestBufferingStream = null;
        Stream? originalBody = null;

        var logContext = _contextPool.Get();
        logContext.HttpContext = context;
        logContext.LoggingFields = loggingFields;
        logContext.RequestBodyLogLimit = options.RequestBodyLogLimit;
        logContext.ResponseBodyLogLimit = options.ResponseBodyLogLimit;
        logContext.StartTimestamp = _timeProvider.GetTimestamp();
        logContext.TimeProvider = _timeProvider;

        if (loggingAttribute?.IsRequestBodyLogLimitSet is true)
        {
            logContext.RequestBodyLogLimit = loggingAttribute.RequestBodyLogLimit;
        }
        if (loggingAttribute?.IsResponseBodyLogLimitSet is true)
        {
            logContext.ResponseBodyLogLimit = loggingAttribute.ResponseBodyLogLimit;
        }

        try
        {
            for (var i = 0; i < _interceptors.Length; i++)
            {
                await _interceptors[i].OnRequestAsync(logContext);
            }
        }
        catch (Exception)
        {
            logContext.Reset();
            _contextPool.Return(logContext);
            throw;
        }

        loggingFields = logContext.LoggingFields;

        var request = context.Request;
        if (logContext.IsAnyEnabled(HttpLoggingFields.RequestPropertiesAndHeaders | HttpLoggingFields.RequestQuery))
        {
            if (loggingFields.HasFlag(HttpLoggingFields.RequestProtocol))
            {
                logContext.AddParameter(nameof(request.Protocol), request.Protocol);
            }

            if (loggingFields.HasFlag(HttpLoggingFields.RequestMethod))
            {
                logContext.AddParameter(nameof(request.Method), request.Method);
            }

            if (loggingFields.HasFlag(HttpLoggingFields.RequestScheme))
            {
                logContext.AddParameter(nameof(request.Scheme), request.Scheme);
            }

            if (loggingFields.HasFlag(HttpLoggingFields.RequestPath))
            {
                logContext.AddParameter(nameof(request.PathBase), request.PathBase);
                logContext.AddParameter(nameof(request.Path), request.Path);
            }

            if (loggingFields.HasFlag(HttpLoggingFields.RequestQuery))
            {
                logContext.AddParameter(nameof(request.QueryString), request.QueryString.Value);
            }

            if (loggingFields.HasFlag(HttpLoggingFields.RequestHeaders))
            {
                FilterHeaders(logContext, request.Headers, options._internalRequestHeaders);
            }

            if (logContext.InternalParameters.Count > 0 && !options.CombineLogs)
            {
                var httpRequestLog = new HttpLog(logContext.InternalParameters, "Request");

                _logger.RequestLog(httpRequestLog);

                logContext.InternalParameters = new();
            }
        }

        if (loggingFields.HasFlag(HttpLoggingFields.RequestBody))
        {
            if (request.ContentType is null)
            {
                _logger.NoMediaType("request");
            }
            else if (MediaTypeHelpers.TryGetEncodingForMediaType(request.ContentType,
                options.MediaTypeOptions.MediaTypeStates,
                out var encoding))
            {
                originalBody = request.Body;
                requestBufferingStream = new RequestBufferingStream(
                    request.Body,
                    logContext.RequestBodyLogLimit,
                    _logger,
                    encoding,
                    !options.CombineLogs);
                request.Body = requestBufferingStream;
            }
            else
            {
                _logger.UnrecognizedMediaType("request");
            }
        }

        ResponseBufferingStream? responseBufferingStream = null;
        IHttpResponseBodyFeature? originalBodyFeature = null;

        UpgradeFeatureLoggingDecorator? loggableUpgradeFeature = null;
        IHttpUpgradeFeature? originalUpgradeFeature = null;

        try
        {
            var response = context.Response;

            if (logContext.IsAnyEnabled(HttpLoggingFields.ResponsePropertiesAndHeaders))
            {
                originalUpgradeFeature = context.Features.Get<IHttpUpgradeFeature>();

                if (originalUpgradeFeature != null && originalUpgradeFeature.IsUpgradableRequest)
                {
                    loggableUpgradeFeature = new UpgradeFeatureLoggingDecorator(originalUpgradeFeature,
                        logContext, options, _interceptors, _logger);

                    context.Features.Set<IHttpUpgradeFeature>(loggableUpgradeFeature);
                }
            }

            // Hook the response body when there are interceptors in case they want to conditionally log the body.
            if (loggingFields.HasFlag(HttpLoggingFields.ResponseBody) || _interceptors.Length > 0)
            {
                originalBodyFeature = context.Features.Get<IHttpResponseBodyFeature>()!;

                // TODO pool these.
                responseBufferingStream = new ResponseBufferingStream(originalBodyFeature,
                    _logger, logContext, options, _interceptors);
                response.Body = responseBufferingStream;
                context.Features.Set<IHttpResponseBodyFeature>(responseBufferingStream);
            }

            try
            {
                await _next(context);
            }
            finally
            {
                if (options.CombineLogs)
                {
                    if (ResponseHeadersNotYetWritten(responseBufferingStream, loggableUpgradeFeature))
                    {
                        // No body, not an upgradable request or request not upgraded, write headers here.
                        await LogResponseHeadersAsync(logContext, options, _interceptors, _logger);
                    }

                    // Now that the interceptors have run, add the request & response body logs (if they're still enabled).
                    requestBufferingStream?.LogRequestBody(logContext);
                    responseBufferingStream?.LogResponseBody(logContext);

                    if (logContext.IsAnyEnabled(HttpLoggingFields.Duration))
                    {
                        logContext.AddParameter(nameof(HttpLoggingFields.Duration), logContext.GetDuration());
                    }

                    if (logContext.InternalParameters.Count > 0)
                    {
                        var log = new HttpLog(logContext.InternalParameters, "Request and Response");
                        _logger.RequestResponseLog(log);
                    }
                }
                else
                {
                    // If the middleware pipeline didn't read until 0 was returned from ReadAsync,
                    // make sure we log the request body.
                    requestBufferingStream?.LogRequestBody();

                    if (ResponseHeadersNotYetWritten(responseBufferingStream, loggableUpgradeFeature))
                    {
                        // No body, not an upgradable request or request not upgraded, write headers here.
                        await LogResponseHeadersAsync(logContext, options, _interceptors, _logger);
                    }
                    else
                    {
                        // There will only be a response body if the headers were already written.
                        responseBufferingStream?.LogResponseBody();
                    }

                    if (logContext.IsAnyEnabled(HttpLoggingFields.Duration))
                    {
                        _logger.Duration(logContext.GetDuration());
                    }
                }
            }
        }
        finally
        {
            responseBufferingStream?.Dispose();

            if (originalBodyFeature != null)
            {
                context.Features.Set(originalBodyFeature);
            }

            requestBufferingStream?.Dispose();

            if (originalBody != null)
            {
                context.Request.Body = originalBody;
            }

            if (loggableUpgradeFeature != null)
            {
                context.Features.Set(originalUpgradeFeature);
            }

            logContext.Reset();
            _contextPool.Return(logContext);
        }
    }

    private static bool ResponseHeadersNotYetWritten(ResponseBufferingStream? responseBufferingStream, UpgradeFeatureLoggingDecorator? upgradeFeatureLogging)
    {
        return BodyNotYetWritten(responseBufferingStream) && NotUpgradeableRequestOrRequestNotUpgraded(upgradeFeatureLogging);
    }

    private static bool BodyNotYetWritten(ResponseBufferingStream? responseBufferingStream)
    {
        return responseBufferingStream == null || responseBufferingStream.HeadersWritten == false;
    }

    private static bool NotUpgradeableRequestOrRequestNotUpgraded(UpgradeFeatureLoggingDecorator? upgradeFeatureLogging)
    {
        return upgradeFeatureLogging == null || !upgradeFeatureLogging.IsUpgraded;
    }

    // Called from the response body stream sync Write and Flush APIs. These are disabled by the server by default, so we're not as worried about the sync-over-async code needed here.
    public static void LogResponseHeadersSync(HttpLoggingInterceptorContext logContext, HttpLoggingOptions options, IHttpLoggingInterceptor[] interceptors, ILogger logger)
    {
        for (var i = 0; i < interceptors.Length; i++)
        {
            interceptors[i].OnResponseAsync(logContext).AsTask().GetAwaiter().GetResult();
        }

        LogResponseHeadersCore(logContext, options, logger);
    }

    public static async ValueTask LogResponseHeadersAsync(HttpLoggingInterceptorContext logContext, HttpLoggingOptions options, IHttpLoggingInterceptor[] interceptors, ILogger logger)
    {
        for (var i = 0; i < interceptors.Length; i++)
        {
            await interceptors[i].OnResponseAsync(logContext);
        }

        LogResponseHeadersCore(logContext, options, logger);
    }

    private static void LogResponseHeadersCore(HttpLoggingInterceptorContext logContext, HttpLoggingOptions options, ILogger logger)
    {
        var loggingFields = logContext.LoggingFields;
        var response = logContext.HttpContext.Response;

        if (loggingFields.HasFlag(HttpLoggingFields.ResponseStatusCode))
        {
            logContext.AddParameter(nameof(response.StatusCode), response.StatusCode);
        }

        if (loggingFields.HasFlag(HttpLoggingFields.ResponseHeaders))
        {
            FilterHeaders(logContext, response.Headers, options._internalResponseHeaders);
        }

        if (logContext.InternalParameters.Count > 0 && !options.CombineLogs)
        {
            var httpResponseLog = new HttpLog(logContext.InternalParameters, "Response");
            logger.ResponseLog(httpResponseLog);
        }
    }

    internal static void FilterHeaders(HttpLoggingInterceptorContext logContext,
        IHeaderDictionary headers,
        HashSet<string> allowedHeaders)
    {
        foreach (var (key, value) in headers)
        {
            if (!allowedHeaders.Contains(key))
            {
                // Key is not among the "only listed" headers.
                logContext.AddParameter(key, Redacted);
                continue;
            }
            logContext.AddParameter(key, value.ToString());
        }
    }
}
