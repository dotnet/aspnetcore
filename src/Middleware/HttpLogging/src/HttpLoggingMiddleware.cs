// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    private readonly IOptionsMonitor<HttpLoggingOptions> _options;
    private const int DefaultRequestFieldsMinusHeaders = 7;
    private const int DefaultResponseFieldsMinusHeaders = 2;
    private const string Redacted = "[Redacted]";

    /// <summary>
    /// Initializes <see cref="HttpLoggingMiddleware" />.
    /// </summary>
    /// <param name="next"></param>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    public HttpLoggingMiddleware(RequestDelegate next, IOptionsMonitor<HttpLoggingOptions> options, ILogger<HttpLoggingMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _next = next;
        _options = options;
        _logger = logger;
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

        return InvokeInternal(context);
    }

    private async Task InvokeInternal(HttpContext context)
    {
        var options = _options.CurrentValue;
        RequestBufferingStream? requestBufferingStream = null;
        Stream? originalBody = null;

        if ((HttpLoggingFields.Request & options.LoggingFields) != HttpLoggingFields.None)
        {
            var request = context.Request;
            var list = new List<KeyValuePair<string, object?>>(
                request.Headers.Count + DefaultRequestFieldsMinusHeaders);

            if (options.LoggingFields.HasFlag(HttpLoggingFields.RequestProtocol))
            {
                AddToList(list, nameof(request.Protocol), request.Protocol);
            }

            if (options.LoggingFields.HasFlag(HttpLoggingFields.RequestMethod))
            {
                AddToList(list, nameof(request.Method), request.Method);
            }

            if (options.LoggingFields.HasFlag(HttpLoggingFields.RequestScheme))
            {
                AddToList(list, nameof(request.Scheme), request.Scheme);
            }

            if (options.LoggingFields.HasFlag(HttpLoggingFields.RequestPath))
            {
                AddToList(list, nameof(request.PathBase), request.PathBase);
                AddToList(list, nameof(request.Path), request.Path);
            }

            if (options.LoggingFields.HasFlag(HttpLoggingFields.RequestQuery))
            {
                AddToList(list, nameof(request.QueryString), request.QueryString.Value);
            }

            if (options.LoggingFields.HasFlag(HttpLoggingFields.RequestHeaders))
            {
                FilterHeaders(list, request.Headers, options._internalRequestHeaders);
            }

            if (options.LoggingFields.HasFlag(HttpLoggingFields.RequestBody))
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
                        options.RequestBodyLogLimit,
                        _logger,
                        encoding);
                    request.Body = requestBufferingStream;
                }
                else
                {
                    _logger.UnrecognizedMediaType("request");
                }
            }

            var httpRequestLog = new HttpRequestLog(list);

            _logger.RequestLog(httpRequestLog);
        }

        ResponseBufferingStream? responseBufferingStream = null;
        IHttpResponseBodyFeature? originalBodyFeature = null;

        UpgradeFeatureLoggingDecorator? loggableUpgradeFeature = null;
        IHttpUpgradeFeature? originalUpgradeFeature = null;

        try
        {
            var response = context.Response;

            if (options.LoggingFields.HasFlag(HttpLoggingFields.ResponseStatusCode) || options.LoggingFields.HasFlag(HttpLoggingFields.ResponseHeaders))
            {
                originalUpgradeFeature = context.Features.Get<IHttpUpgradeFeature>();

                if (originalUpgradeFeature != null && originalUpgradeFeature.IsUpgradableRequest)
                {
                    loggableUpgradeFeature = new UpgradeFeatureLoggingDecorator(originalUpgradeFeature, response, options, _logger);

                    context.Features.Set<IHttpUpgradeFeature>(loggableUpgradeFeature);
                }
            }

            if (options.LoggingFields.HasFlag(HttpLoggingFields.ResponseBody))
            {
                originalBodyFeature = context.Features.Get<IHttpResponseBodyFeature>()!;

                // TODO pool these.
                responseBufferingStream = new ResponseBufferingStream(originalBodyFeature,
                    options.ResponseBodyLogLimit,
                    _logger,
                    context,
                    options.MediaTypeOptions.MediaTypeStates,
                    options);
                response.Body = responseBufferingStream;
                context.Features.Set<IHttpResponseBodyFeature>(responseBufferingStream);
            }

            await _next(context);

            if (requestBufferingStream?.HasLogged == false)
            {
                // If the middleware pipeline didn't read until 0 was returned from readasync,
                // make sure we log the request body.
                requestBufferingStream.LogRequestBody();
            }

            if (ResponseHeadersNotYetWritten(responseBufferingStream, loggableUpgradeFeature))
            {
                // No body, not an upgradable request or request not upgraded, write headers here.
                LogResponseHeaders(response, options, _logger);
            }

            if (responseBufferingStream != null)
            {
                var responseBody = responseBufferingStream.GetString(responseBufferingStream.Encoding);
                if (!string.IsNullOrEmpty(responseBody))
                {
                    _logger.ResponseBody(responseBody);
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
        }
    }

    private static bool ResponseHeadersNotYetWritten(ResponseBufferingStream? responseBufferingStream, UpgradeFeatureLoggingDecorator? upgradeFeatureLogging)
    {
        return BodyNotYetWritten(responseBufferingStream) && NotUpgradeableRequestOrRequestNotUpgraded(upgradeFeatureLogging);
    }

    private static bool BodyNotYetWritten(ResponseBufferingStream? responseBufferingStream)
    {
        return responseBufferingStream == null || responseBufferingStream.FirstWrite == false;
    }

    private static bool NotUpgradeableRequestOrRequestNotUpgraded(UpgradeFeatureLoggingDecorator? upgradeFeatureLogging)
    {
        return upgradeFeatureLogging == null || !upgradeFeatureLogging.IsUpgraded;
    }

    private static void AddToList(List<KeyValuePair<string, object?>> list, string key, string? value)
    {
        list.Add(new KeyValuePair<string, object?>(key, value));
    }

    public static void LogResponseHeaders(HttpResponse response, HttpLoggingOptions options, ILogger logger)
    {
        var list = new List<KeyValuePair<string, object?>>(
            response.Headers.Count + DefaultResponseFieldsMinusHeaders);

        if (options.LoggingFields.HasFlag(HttpLoggingFields.ResponseStatusCode))
        {
            list.Add(new KeyValuePair<string, object?>(nameof(response.StatusCode), response.StatusCode));
        }

        if (options.LoggingFields.HasFlag(HttpLoggingFields.ResponseHeaders))
        {
            FilterHeaders(list, response.Headers, options._internalResponseHeaders);
        }

        if (list.Count > 0)
        {
            var httpResponseLog = new HttpResponseLog(list);

            logger.ResponseLog(httpResponseLog);
        }
    }

    internal static void FilterHeaders(List<KeyValuePair<string, object?>> keyValues,
        IHeaderDictionary headers,
        HashSet<string> allowedHeaders)
    {
        foreach (var (key, value) in headers)
        {
            if (!allowedHeaders.Contains(key))
            {
                // Key is not among the "only listed" headers.
                keyValues.Add(new KeyValuePair<string, object?>(key, Redacted));
                continue;
            }
            keyValues.Add(new KeyValuePair<string, object?>(key, value.ToString()));
        }
    }
}
