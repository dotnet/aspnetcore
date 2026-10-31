// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.HttpLogging;

/// <summary>
/// Flags used to control which parts of the
/// request and response are logged.
/// </summary>
[Flags]
public enum HttpLoggingFields : long
{
    /// <summary>
    /// No logging.
    /// </summary>
    None = 0x0,

    /// <summary>
    /// Flag for logging the HTTP Request Path, which includes both the <see cref="HttpRequest.Path"/>
    /// and <see cref="HttpRequest.PathBase"/>.
    /// <para>
    /// For example:
    /// Path: /index
    /// PathBase: /app
    /// </para>
    /// </summary>
    RequestPath = 0x1,

    /// <summary>
    /// Flag for logging the HTTP Request <see cref="HttpRequest.QueryString"/>.
    /// <para>
    /// For example:
    /// Query: ?index=1
    /// </para>
    /// RequestQuery contents can contain private information
    /// which may have regulatory concerns under GDPR
    /// and other laws. RequestQuery should not be logged
    /// unless logs are secure and access controlled
    /// and the privacy impact assessed.
    /// </summary>
    RequestQuery = 0x2,

    /// <summary>
    /// Flag for logging the HTTP Request <see cref="HttpRequest.Protocol"/>.
    /// <para>
    /// For example:
    /// Protocol: HTTP/1.1
    /// </para>
    /// </summary>
    RequestProtocol = 0x4,

    /// <summary>
    /// Flag for logging the HTTP Request <see cref="HttpRequest.Method"/>.
    /// <para>
    /// For example:
    /// Method: GET
    /// </para>
    /// </summary>
    RequestMethod = 0x8,

    /// <summary>
    /// Flag for logging the HTTP Request <see cref="HttpRequest.Scheme"/>.
    /// <para>
    /// For example:
    /// Scheme: https
    /// </para>
    /// </summary>
    RequestScheme = 0x10,

    /// <summary>
    /// Flag for logging the HTTP Response <see cref="HttpResponse.StatusCode"/>.
    /// <para>
    /// For example:
    /// StatusCode: 200
    /// </para>
    /// </summary>
    ResponseStatusCode = 0x20,

    /// <summary>
    /// Flag for logging the HTTP Request <see cref="HttpRequest.Headers"/>.
    /// Request Headers are logged as soon as the middleware is invoked.
    /// Headers are redacted by default with the character '[Redacted]' unless specified in
    /// the <see cref="HttpLoggingOptions.RequestHeaders"/>.
    /// <para>
    /// For example:
    /// Connection: keep-alive
    /// My-Custom-Request-Header: [Redacted]
    /// </para>
    /// </summary>
    RequestHeaders = 0x40,

    /// <summary>
    /// Flag for logging the HTTP Response <see cref="HttpResponse.Headers"/>.
    /// Response Headers are logged when the <see cref="HttpResponse.Body"/> is written to
    /// or when <see cref="IHttpResponseBodyFeature.StartAsync(System.Threading.CancellationToken)"/>
    /// is called.
    /// <para>
    /// Headers are redacted by default with the character '[Redacted]' unless specified in
    /// the <see cref="HttpLoggingOptions.ResponseHeaders"/>.
    /// </para>
    /// <para>
    /// For example:
    /// Content-Length: 16
    /// My-Custom-Response-Header: [Redacted]
    /// </para>
    /// </summary>
    ResponseHeaders = 0x80,

    /// <summary>
    /// Flag for logging the HTTP Request <see cref="IHttpRequestTrailersFeature.Trailers"/>.
    /// Request Trailers are currently not logged.
    /// </summary>
    RequestTrailers = 0x100,

    /// <summary>
    /// Flag for logging the HTTP Response <see cref="IHttpResponseTrailersFeature.Trailers"/>.
    /// Response Trailers are currently not logged.
    /// </summary>
    ResponseTrailers = 0x200,

    /// <summary>
    /// Flag for logging the HTTP Request <see cref="HttpRequest.Body"/>.
    /// Logging the request body has performance implications, as it requires buffering
    /// the entire request body up to <see cref="HttpLoggingOptions.RequestBodyLogLimit"/>.
    /// </summary>
    RequestBody = 0x400,

    /// <summary>
    /// Flag for logging the HTTP Response <see cref="HttpResponse.Body"/>.
    /// Logging the response body has performance implications, as it requires buffering
    /// the entire response body up to <see cref="HttpLoggingOptions.ResponseBodyLogLimit"/>.
    /// </summary>
    ResponseBody = 0x800,

    /// <summary>
    /// Flag for logging how long it took to process the request and response in milliseconds.
    /// </summary>
    Duration = 0x1000,

    /// <summary>
    /// Flag for logging a collection of HTTP Request properties,
    /// including <see cref="RequestPath"/>, <see cref="RequestProtocol"/>,
    /// <see cref="RequestMethod"/>, and <see cref="RequestScheme"/>.
    /// </summary>
    /// <remarks>
    /// The HTTP Request <see cref="HttpRequest.QueryString"/> is not included with this flag as it may contain private information.
    /// If desired, it should be explicitly specified with <see cref="RequestQuery"/>.
    /// </remarks>
    RequestProperties = RequestPath | RequestProtocol | RequestMethod | RequestScheme,

    /// <summary>
    /// Flag for logging HTTP Request properties and headers.
    /// Includes <see cref="RequestProperties"/> and <see cref="RequestHeaders"/>
    /// </summary>
    /// <remarks>
    /// The HTTP Request <see cref="HttpRequest.QueryString"/> is not included with this flag as it may contain private information.
    /// If desired, it should be explicitly specified with <see cref="RequestQuery"/>.
    /// </remarks>
    RequestPropertiesAndHeaders = RequestProperties | RequestHeaders,

    /// <summary>
    /// Flag for logging HTTP Response properties and headers.
    /// Includes <see cref="ResponseStatusCode"/> and <see cref="ResponseHeaders"/>.
    /// </summary>
    ResponsePropertiesAndHeaders = ResponseStatusCode | ResponseHeaders,

    /// <summary>
    /// Flag for logging the entire HTTP Request.
    /// Includes <see cref="RequestPropertiesAndHeaders"/> and <see cref="RequestBody"/>.
    /// Logging the request body has performance implications, as it requires buffering
    /// the entire request body up to <see cref="HttpLoggingOptions.RequestBodyLogLimit"/>.
    /// </summary>
    /// <remarks>
    /// The HTTP Request <see cref="HttpRequest.QueryString"/> is not included with this flag as it may contain private information.
    /// If desired, it should be explicitly specified with <see cref="RequestQuery"/>.
    /// </remarks>
    Request = RequestPropertiesAndHeaders | RequestBody,

    /// <summary>
    /// Flag for logging the entire HTTP Response.
    /// Includes <see cref="ResponsePropertiesAndHeaders"/> and <see cref="ResponseBody"/>.
    /// Logging the response body has performance implications, as it requires buffering
    /// the entire response body up to <see cref="HttpLoggingOptions.ResponseBodyLogLimit"/>.
    /// </summary>
    Response = ResponsePropertiesAndHeaders | ResponseBody,

    /// <summary>
    /// Flag for logging both the HTTP Request and Response.
    /// Includes <see cref="Request"/>, <see cref="Response"/>, and <see cref="Duration"/>.
    /// Logging the request and response body has performance implications, as it requires buffering
    /// the entire request and response body up to the <see cref="HttpLoggingOptions.RequestBodyLogLimit"/>
    /// and <see cref="HttpLoggingOptions.ResponseBodyLogLimit"/>.
    /// </summary>
    /// <remarks>
    /// The HTTP Request <see cref="HttpRequest.QueryString"/> is not included with this flag as it may contain private information.
    /// If desired, it should be explicitly specified with <see cref="RequestQuery"/>.
    /// </remarks>
    All = Request | Response | Duration
}
