// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.HttpLogging;

/// <summary>
/// Options for the <see cref="HttpLoggingMiddleware"/>.
/// </summary>
public sealed class HttpLoggingOptions
{
    /// <summary>
    /// Fields to log for the Request and Response. Defaults to logging request and response properties and headers.
    /// </summary>
    public HttpLoggingFields LoggingFields { get; set; } = HttpLoggingFields.RequestPropertiesAndHeaders | HttpLoggingFields.ResponsePropertiesAndHeaders;

    /// <summary>
    /// Request header values that are allowed to be logged.
    /// <para>
    /// If a request header is not present in the <see cref="RequestHeaders"/>,
    /// the header name will be logged with a redacted value.
    /// Request headers can contain authentication tokens,
    /// or private information which may have regulatory concerns
    /// under GDPR and other laws. Arbitrary request headers
    /// should not be logged unless logs are secure and
    /// access controlled and the privacy impact assessed.
    /// </para>
    /// </summary>
    public ISet<string> RequestHeaders => _internalRequestHeaders;

    internal HashSet<string> _internalRequestHeaders = new HashSet<string>(26, StringComparer.OrdinalIgnoreCase)
        {
            HeaderNames.Accept,
            HeaderNames.AcceptCharset,
            HeaderNames.AcceptEncoding,
            HeaderNames.AcceptLanguage,
            HeaderNames.Allow,
            HeaderNames.CacheControl,
            HeaderNames.Connection,
            HeaderNames.ContentEncoding,
            HeaderNames.ContentLength,
            HeaderNames.ContentType,
            HeaderNames.Date,
            HeaderNames.DNT,
            HeaderNames.Expect,
            HeaderNames.Host,
            HeaderNames.MaxForwards,
            HeaderNames.Range,
            HeaderNames.SecWebSocketExtensions,
            HeaderNames.SecWebSocketVersion,
            HeaderNames.TE,
            HeaderNames.Trailer,
            HeaderNames.TransferEncoding,
            HeaderNames.Upgrade,
            HeaderNames.UserAgent,
            HeaderNames.Warning,
            HeaderNames.XRequestedWith,
            HeaderNames.XUACompatible
        };

    /// <summary>
    /// Response header values that are allowed to be logged.
    /// <para>
    /// If a response header is not present in the <see cref="ResponseHeaders"/>,
    /// the header name will be logged with a redacted value.
    /// </para>
    /// </summary>
    public ISet<string> ResponseHeaders => _internalResponseHeaders;

    internal HashSet<string> _internalResponseHeaders = new HashSet<string>(19, StringComparer.OrdinalIgnoreCase)
        {
            HeaderNames.AcceptRanges,
            HeaderNames.Age,
            HeaderNames.Allow,
            HeaderNames.AltSvc,
            HeaderNames.Connection,
            HeaderNames.ContentDisposition,
            HeaderNames.ContentLanguage,
            HeaderNames.ContentLength,
            HeaderNames.ContentLocation,
            HeaderNames.ContentRange,
            HeaderNames.ContentType,
            HeaderNames.Date,
            HeaderNames.Expires,
            HeaderNames.LastModified,
            HeaderNames.Location,
            HeaderNames.Server,
            HeaderNames.TransferEncoding,
            HeaderNames.Upgrade,
            HeaderNames.XPoweredBy
        };

    /// <summary>
    /// Options for configuring encodings for a specific media type.
    /// <para>
    /// If the request or response do not match the supported media type,
    /// the response body will not be logged.
    /// </para>
    /// </summary>
    public MediaTypeOptions MediaTypeOptions { get; } = MediaTypeOptions.BuildDefaultMediaTypeOptions();

    /// <summary>
    /// Maximum request body size to log (in bytes). Defaults to 32 KB.
    /// </summary>
    public int RequestBodyLogLimit { get; set; } = 32 * 1024;

    /// <summary>
    /// Maximum response body size to log (in bytes). Defaults to 32 KB.
    /// </summary>
    public int ResponseBodyLogLimit { get; set; } = 32 * 1024;

    /// <summary>
    /// Gets or sets if the middleware will combine the request, request body, response, response body,
    /// and duration logs into a single log entry. The default is <see langword="false"/>.
    /// </summary>
    public bool CombineLogs { get; set; }
}
