// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.HttpLogging;

/// <summary>
/// Middleware that logs HTTP requests and HTTP responses.
/// </summary>
internal sealed class HttpLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;
    private readonly IHttpLoggingInterceptor[] _interceptors;
    private readonly IOptionsMonitor<HttpLoggingOptions> _options;
    private const string Redacted = "[Redacted]";

    public HttpLoggingMiddleware(RequestDelegate next, IOptionsMonitor<HttpLoggingOptions> options, ILogger<HttpLoggingMiddleware> logger,
        IEnumerable<IHttpLoggingInterceptor> interceptors)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(interceptors);

        _next = next;
        _options = options;
        _logger = logger;
        _interceptors = interceptors.ToArray();
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

        // TODO: Cache this
        var logContext = new HttpLoggingContext(context)
        {
            LoggingFields = loggingFields,
            RequestBodyLogLimit = options.RequestBodyLogLimit,
            ResponseBodyLogLimit = options.ResponseBodyLogLimit,
        };

        if (loggingAttribute?.IsRequestBodyLogLimitSet is true)
        {
            logContext.RequestBodyLogLimit = loggingAttribute.RequestBodyLogLimit;
        }
        if (loggingAttribute?.IsResponseBodyLogLimitSet is true)
        {
            logContext.ResponseBodyLogLimit = loggingAttribute.ResponseBodyLogLimit;
        }

        for (var i = 0; i < _interceptors.Length; i++)
        {
            _interceptors[i].OnRequest(logContext);
        }

        loggingFields = logContext.LoggingFields;

        if (logContext.IsAnyEnabled(HttpLoggingFields.Request))
        {
            var request = context.Request;

            if (loggingFields.HasFlag(HttpLoggingFields.RequestProtocol))
            {
                logContext.Add(nameof(request.Protocol), request.Protocol);
            }

            if (loggingFields.HasFlag(HttpLoggingFields.RequestMethod))
            {
                logContext.Add(nameof(request.Method), request.Method);
            }

            if (loggingFields.HasFlag(HttpLoggingFields.RequestScheme))
            {
                logContext.Add(nameof(request.Scheme), request.Scheme);
            }

            if (loggingFields.HasFlag(HttpLoggingFields.RequestPath))
            {
                logContext.Add(nameof(request.PathBase), request.PathBase);
                logContext.Add(nameof(request.Path), request.Path);
            }

            if (loggingFields.HasFlag(HttpLoggingFields.RequestQuery))
            {
                logContext.Add(nameof(request.QueryString), request.QueryString.Value);
            }

            if (loggingFields.HasFlag(HttpLoggingFields.RequestHeaders))
            {
                FilterHeaders(logContext, request.Headers, options._internalRequestHeaders);
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
                        encoding);
                    request.Body = requestBufferingStream;
                }
                else
                {
                    _logger.UnrecognizedMediaType("request");
                }
            }

            var httpRequestLog = new HttpRequestLog(logContext.Parameters);

            _logger.RequestLog(httpRequestLog);

            logContext.Parameters.Clear();
        }

        ResponseBufferingStream? responseBufferingStream = null;
        IHttpResponseBodyFeature? originalBodyFeature = null;

        UpgradeFeatureLoggingDecorator? loggableUpgradeFeature = null;
        IHttpUpgradeFeature? originalUpgradeFeature = null;

        try
        {
            var response = context.Response;

            if (loggingFields.HasFlag(HttpLoggingFields.ResponseStatusCode) || loggingFields.HasFlag(HttpLoggingFields.ResponseHeaders))
            {
                originalUpgradeFeature = context.Features.Get<IHttpUpgradeFeature>();

                if (originalUpgradeFeature != null && originalUpgradeFeature.IsUpgradableRequest)
                {
                    loggableUpgradeFeature = new UpgradeFeatureLoggingDecorator(originalUpgradeFeature,
                        logContext, options._internalResponseHeaders, _interceptors, _logger);

                    context.Features.Set<IHttpUpgradeFeature>(loggableUpgradeFeature);
                }
            }

            if (loggingFields.HasFlag(HttpLoggingFields.ResponseBody))
            {
                originalBodyFeature = context.Features.Get<IHttpResponseBodyFeature>()!;

                // TODO pool these.
                responseBufferingStream = new ResponseBufferingStream(originalBodyFeature,
                    _logger, logContext, options, _interceptors);
                response.Body = responseBufferingStream;
                context.Features.Set<IHttpResponseBodyFeature>(responseBufferingStream);
            }

            await _next(context);

            // If the middleware pipeline didn't read until 0 was returned from ReadAsync,
            // make sure we log the request body.
            requestBufferingStream?.LogRequestBody();

            if (ResponseHeadersNotYetWritten(responseBufferingStream, loggableUpgradeFeature))
            {
                // No body, not an upgradable request or request not upgraded, write headers here.
                LogResponseHeaders(logContext, options._internalResponseHeaders, _interceptors, _logger);
            }

            responseBufferingStream?.LogResponseBody();
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

    public static void LogResponseHeaders(HttpLoggingContext logContext, HashSet<string> allowedResponseHeaders, IHttpLoggingInterceptor[] interceptors, ILogger logger)
    {
        for (var i = 0; i < interceptors.Length; i++)
        {
            interceptors[i].OnResponse(logContext);
        }

        var loggingFields = logContext.LoggingFields;
        var response = logContext.HttpContext.Response;

        if (loggingFields.HasFlag(HttpLoggingFields.ResponseStatusCode))
        {
            logContext.Add(nameof(response.StatusCode), response.StatusCode);
        }

        if (loggingFields.HasFlag(HttpLoggingFields.ResponseHeaders))
        {
            FilterHeaders(logContext, response.Headers, allowedResponseHeaders);
        }

        if (logContext.Parameters.Count > 0)
        {
            var httpResponseLog = new HttpResponseLog(logContext.Parameters);

            logger.ResponseLog(httpResponseLog);
        }
    }

    internal static void FilterHeaders(HttpLoggingContext logContext,
        IHeaderDictionary headers,
        HashSet<string> allowedHeaders)
    {
        foreach (var (key, value) in headers)
        {
            if (!allowedHeaders.Contains(key))
            {
                // Key is not among the "only listed" headers.
                logContext.Add(key, Redacted);
                continue;
            }
            logContext.Add(key, value.ToString());
        }
    }
}
