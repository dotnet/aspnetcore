// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Net.Http.Headers;

/// <summary>
/// Defines constants for well-known HTTP headers.
/// </summary>
// MODIFICATION POLICY: This list is not intended to be exhaustive, it primarily contains values used by the framework itself.
// Please do not open PRs without first opening an issue to discuss a specific item.
public static class HeaderNames
{
    private const string PseudoHeaderNamesObsoletionMessage = "This is obsolete and will be removed in a future version. Header dictionaries do not contain this key.";

    // Use readonly statics rather than constants so ReferenceEquals works

    /// <summary>Gets the <c>Accept</c> HTTP header name.</summary>
    public static readonly string Accept = "Accept";

    /// <summary>Gets the <c>Accept-Charset</c> HTTP header name.</summary>
    public static readonly string AcceptCharset = "Accept-Charset";

    /// <summary>Gets the <c>Accept-Encoding</c> HTTP header name.</summary>
    public static readonly string AcceptEncoding = "Accept-Encoding";

    /// <summary>Gets the <c>Accept-Language</c> HTTP header name.</summary>
    public static readonly string AcceptLanguage = "Accept-Language";

    /// <summary>Gets the <c>Accept-Ranges</c> HTTP header name.</summary>
    public static readonly string AcceptRanges = "Accept-Ranges";

    /// <summary>Gets the <c>Access-Control-Allow-Credentials</c> HTTP header name.</summary>
    public static readonly string AccessControlAllowCredentials = "Access-Control-Allow-Credentials";

    /// <summary>Gets the <c>Access-Control-Allow-Headers</c> HTTP header name.</summary>
    public static readonly string AccessControlAllowHeaders = "Access-Control-Allow-Headers";

    /// <summary>Gets the <c>Access-Control-Allow-Methods</c> HTTP header name.</summary>
    public static readonly string AccessControlAllowMethods = "Access-Control-Allow-Methods";

    /// <summary>Gets the <c>Access-Control-Allow-Origin</c> HTTP header name.</summary>
    public static readonly string AccessControlAllowOrigin = "Access-Control-Allow-Origin";

    /// <summary>Gets the <c>Access-Control-Expose-Headers</c> HTTP header name.</summary>
    public static readonly string AccessControlExposeHeaders = "Access-Control-Expose-Headers";

    /// <summary>Gets the <c>Access-Control-Max-Age</c> HTTP header name.</summary>
    public static readonly string AccessControlMaxAge = "Access-Control-Max-Age";

    /// <summary>Gets the <c>Access-Control-Request-Headers</c> HTTP header name.</summary>
    public static readonly string AccessControlRequestHeaders = "Access-Control-Request-Headers";

    /// <summary>Gets the <c>Access-Control-Request-Method</c> HTTP header name.</summary>
    public static readonly string AccessControlRequestMethod = "Access-Control-Request-Method";

    /// <summary>Gets the <c>Age</c> HTTP header name.</summary>
    public static readonly string Age = "Age";

    /// <summary>Gets the <c>Allow</c> HTTP header name.</summary>
    public static readonly string Allow = "Allow";

    /// <summary>Gets the <c>Alt-Svc</c> HTTP header name.</summary>
    public static readonly string AltSvc = "Alt-Svc";

    /// <summary>Gets the <c>:authority</c> HTTP header name.</summary>
    [Obsolete(PseudoHeaderNamesObsoletionMessage, false)]
    public static readonly string Authority = ":authority";

    /// <summary>Gets the <c>Authorization</c> HTTP header name.</summary>
    public static readonly string Authorization = "Authorization";

    /// <summary>Gets the <c>baggage</c> HTTP header name.</summary>
    public static readonly string Baggage = "baggage";

    /// <summary>Gets the <c>Cache-Control</c> HTTP header name.</summary>
    public static readonly string CacheControl = "Cache-Control";

    /// <summary>Gets the <c>Connection</c> HTTP header name.</summary>
    public static readonly string Connection = "Connection";

    /// <summary>Gets the <c>Content-Disposition</c> HTTP header name.</summary>
    public static readonly string ContentDisposition = "Content-Disposition";

    /// <summary>Gets the <c>Content-Encoding</c> HTTP header name.</summary>
    public static readonly string ContentEncoding = "Content-Encoding";

    /// <summary>Gets the <c>Content-Language</c> HTTP header name.</summary>
    public static readonly string ContentLanguage = "Content-Language";

    /// <summary>Gets the <c>Content-Length</c> HTTP header name.</summary>
    public static readonly string ContentLength = "Content-Length";

    /// <summary>Gets the <c>Content-Location</c> HTTP header name.</summary>
    public static readonly string ContentLocation = "Content-Location";

    /// <summary>Gets the <c>Content-MD5</c> HTTP header name.</summary>
    public static readonly string ContentMD5 = "Content-MD5";

    /// <summary>Gets the <c>Content-Range</c> HTTP header name.</summary>
    public static readonly string ContentRange = "Content-Range";

    /// <summary>Gets the <c>Content-Security-Policy</c> HTTP header name.</summary>
    public static readonly string ContentSecurityPolicy = "Content-Security-Policy";

    /// <summary>Gets the <c>Content-Security-Policy-Report-Only</c> HTTP header name.</summary>
    public static readonly string ContentSecurityPolicyReportOnly = "Content-Security-Policy-Report-Only";

    /// <summary>Gets the <c>Content-Type</c> HTTP header name.</summary>
    public static readonly string ContentType = "Content-Type";

    /// <summary>Gets the <c>Correlation-Context</c> HTTP header name.</summary>
    public static readonly string CorrelationContext = "Correlation-Context";

    /// <summary>Gets the <c>Cookie</c> HTTP header name.</summary>
    public static readonly string Cookie = "Cookie";

    /// <summary>Gets the <c>Date</c> HTTP header name.</summary>
    public static readonly string Date = "Date";

    /// <summary>Gets the <c>DNT</c> HTTP header name.</summary>
    public static readonly string DNT = "DNT";

    /// <summary>Gets the <c>ETag</c> HTTP header name.</summary>
    public static readonly string ETag = "ETag";

    /// <summary>Gets the <c>Expires</c> HTTP header name.</summary>
    public static readonly string Expires = "Expires";

    /// <summary>Gets the <c>Expect</c> HTTP header name.</summary>
    public static readonly string Expect = "Expect";

    /// <summary>Gets the <c>From</c> HTTP header name.</summary>
    public static readonly string From = "From";

    /// <summary>Gets the <c>Grpc-Accept-Encoding</c> HTTP header name.</summary>
    public static readonly string GrpcAcceptEncoding = "Grpc-Accept-Encoding";

    /// <summary>Gets the <c>Grpc-Encoding</c> HTTP header name.</summary>
    public static readonly string GrpcEncoding = "Grpc-Encoding";

    /// <summary>Gets the <c>Grpc-Message</c> HTTP header name.</summary>
    public static readonly string GrpcMessage = "Grpc-Message";

    /// <summary>Gets the <c>Grpc-Status</c> HTTP header name.</summary>
    public static readonly string GrpcStatus = "Grpc-Status";

    /// <summary>Gets the <c>Grpc-Timeout</c> HTTP header name.</summary>
    public static readonly string GrpcTimeout = "Grpc-Timeout";

    /// <summary>Gets the <c>Host</c> HTTP header name.</summary>
    public static readonly string Host = "Host";

    /// <summary>Gets the <c>Keep-Alive</c> HTTP header name.</summary>
    public static readonly string KeepAlive = "Keep-Alive";

    /// <summary>Gets the <c>If-Match</c> HTTP header name.</summary>
    public static readonly string IfMatch = "If-Match";

    /// <summary>Gets the <c>If-Modified-Since</c> HTTP header name.</summary>
    public static readonly string IfModifiedSince = "If-Modified-Since";

    /// <summary>Gets the <c>If-None-Match</c> HTTP header name.</summary>
    public static readonly string IfNoneMatch = "If-None-Match";

    /// <summary>Gets the <c>If-Range</c> HTTP header name.</summary>
    public static readonly string IfRange = "If-Range";

    /// <summary>Gets the <c>If-Unmodified-Since</c> HTTP header name.</summary>
    public static readonly string IfUnmodifiedSince = "If-Unmodified-Since";

    /// <summary>Gets the <c>Last-Modified</c> HTTP header name.</summary>
    public static readonly string LastModified = "Last-Modified";

    /// <summary>Gets the <c>Link</c> HTTP header name.</summary>
    public static readonly string Link = "Link";

    /// <summary>Gets the <c>Location</c> HTTP header name.</summary>
    public static readonly string Location = "Location";

    /// <summary>Gets the <c>Max-Forwards</c> HTTP header name.</summary>
    public static readonly string MaxForwards = "Max-Forwards";

    /// <summary>Gets the <c>:method</c> HTTP header name.</summary>
    [Obsolete(PseudoHeaderNamesObsoletionMessage, false)]
    public static readonly string Method = ":method";

    /// <summary>Gets the <c>Origin</c> HTTP header name.</summary>
    public static readonly string Origin = "Origin";

    /// <summary>Gets the <c>:path</c> HTTP header name.</summary>
    [Obsolete(PseudoHeaderNamesObsoletionMessage, false)]
    public static readonly string Path = ":path";

    /// <summary>Gets the <c>Pragma</c> HTTP header name.</summary>
    public static readonly string Pragma = "Pragma";

    /// <summary>Gets the <c>Proxy-Authenticate</c> HTTP header name.</summary>
    public static readonly string ProxyAuthenticate = "Proxy-Authenticate";

    /// <summary>Gets the <c>Proxy-Authorization</c> HTTP header name.</summary>
    public static readonly string ProxyAuthorization = "Proxy-Authorization";

    /// <summary>Gets the <c>Proxy-Connection</c> HTTP header name.</summary>
    public static readonly string ProxyConnection = "Proxy-Connection";

    /// <summary>Gets the <c>Range</c> HTTP header name.</summary>
    public static readonly string Range = "Range";

    /// <summary>Gets the <c>Referer</c> HTTP header name.</summary>
    public static readonly string Referer = "Referer";

    /// <summary>Gets the <c>Retry-After</c> HTTP header name.</summary>
    public static readonly string RetryAfter = "Retry-After";

    /// <summary>Gets the <c>Request-Id</c> HTTP header name.</summary>
    public static readonly string RequestId = "Request-Id";

    /// <summary>Gets the <c>:scheme</c> HTTP header name.</summary>
    [Obsolete(PseudoHeaderNamesObsoletionMessage, false)]
    public static readonly string Scheme = ":scheme";

    /// <summary>Gets the <c>Sec-WebSocket-Accept</c> HTTP header name.</summary>
    public static readonly string SecWebSocketAccept = "Sec-WebSocket-Accept";

    /// <summary>Gets the <c>Sec-WebSocket-Key</c> HTTP header name.</summary>
    public static readonly string SecWebSocketKey = "Sec-WebSocket-Key";

    /// <summary>Gets the <c>Sec-WebSocket-Protocol</c> HTTP header name.</summary>
    public static readonly string SecWebSocketProtocol = "Sec-WebSocket-Protocol";

    /// <summary>Gets the <c>Sec-WebSocket-Version</c> HTTP header name.</summary>
    public static readonly string SecWebSocketVersion = "Sec-WebSocket-Version";

    /// <summary>Gets the <c>Sec-WebSocket-Extensions</c> HTTP header name.</summary>
    public static readonly string SecWebSocketExtensions = "Sec-WebSocket-Extensions";

    /// <summary>Gets the <c>Server</c> HTTP header name.</summary>
    public static readonly string Server = "Server";

    /// <summary>Gets the <c>Set-Cookie</c> HTTP header name.</summary>
    public static readonly string SetCookie = "Set-Cookie";

    /// <summary>Gets the <c>:status</c> HTTP header name.</summary>
    [Obsolete(PseudoHeaderNamesObsoletionMessage, false)]
    public static readonly string Status = ":status";

    /// <summary>Gets the <c>Strict-Transport-Security</c> HTTP header name.</summary>
    public static readonly string StrictTransportSecurity = "Strict-Transport-Security";

    /// <summary>Gets the <c>TE</c> HTTP header name.</summary>
    public static readonly string TE = "TE";

    /// <summary>Gets the <c>Trailer</c> HTTP header name.</summary>
    public static readonly string Trailer = "Trailer";

    /// <summary>Gets the <c>Transfer-Encoding</c> HTTP header name.</summary>
    public static readonly string TransferEncoding = "Transfer-Encoding";

    /// <summary>Gets the <c>Translate</c> HTTP header name.</summary>
    public static readonly string Translate = "Translate";

    /// <summary>Gets the <c>traceparent</c> HTTP header name.</summary>
    public static readonly string TraceParent = "traceparent";

    /// <summary>Gets the <c>tracestate</c> HTTP header name.</summary>
    public static readonly string TraceState = "tracestate";

    /// <summary>Gets the <c>Upgrade</c> HTTP header name.</summary>
    public static readonly string Upgrade = "Upgrade";

    /// <summary>Gets the <c>Upgrade-Insecure-Requests</c> HTTP header name.</summary>
    public static readonly string UpgradeInsecureRequests = "Upgrade-Insecure-Requests";

    /// <summary>Gets the <c>User-Agent</c> HTTP header name.</summary>
    public static readonly string UserAgent = "User-Agent";

    /// <summary>Gets the <c>Vary</c> HTTP header name.</summary>
    public static readonly string Vary = "Vary";

    /// <summary>Gets the <c>Via</c> HTTP header name.</summary>
    public static readonly string Via = "Via";

    /// <summary>Gets the <c>Warning</c> HTTP header name.</summary>
    public static readonly string Warning = "Warning";

    /// <summary>Gets the <c>Sec-WebSocket-Protocol</c> HTTP header name.</summary>
    public static readonly string WebSocketSubProtocols = "Sec-WebSocket-Protocol";

    /// <summary>Gets the <c>WWW-Authenticate</c> HTTP header name.</summary>
    public static readonly string WWWAuthenticate = "WWW-Authenticate";

    /// <summary>Gets the <c>X-Content-Type-Options</c> HTTP header name.</summary>
    public static readonly string XContentTypeOptions = "X-Content-Type-Options";

    /// <summary>Gets the <c>X-Frame-Options</c> HTTP header name.</summary>
    public static readonly string XFrameOptions = "X-Frame-Options";

    /// <summary>Gets the <c>X-Powered-By</c> HTTP header name.</summary>
    public static readonly string XPoweredBy = "X-Powered-By";

    /// <summary>Gets the <c>X-Requested-With</c> HTTP header name.</summary>
    public static readonly string XRequestedWith = "X-Requested-With";

    /// <summary>Gets the <c>X-UA-Compatible</c> HTTP header name.</summary>
    public static readonly string XUACompatible = "X-UA-Compatible";

    /// <summary>Gets the <c>X-XSS-Protection</c> HTTP header name.</summary>
    public static readonly string XXSSProtection = "X-XSS-Protection";
}
