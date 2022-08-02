// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

public partial interface IHeaderDictionary
{
    /// <summary>Gets or sets the <c>Accept</c> HTTP header.</summary>
    StringValues Accept { get => this[HeaderNames.Accept]; set => this[HeaderNames.Accept] = value; }

    /// <summary>Gets or sets the <c>Accept-Charset</c> HTTP header.</summary>
    StringValues AcceptCharset { get => this[HeaderNames.AcceptCharset]; set => this[HeaderNames.AcceptCharset] = value; }

    /// <summary>Gets or sets the <c>Accept-Encoding</c> HTTP header.</summary>
    StringValues AcceptEncoding { get => this[HeaderNames.AcceptEncoding]; set => this[HeaderNames.AcceptEncoding] = value; }

    /// <summary>Gets or sets the <c>Accept-Language</c> HTTP header.</summary>
    StringValues AcceptLanguage { get => this[HeaderNames.AcceptLanguage]; set => this[HeaderNames.AcceptLanguage] = value; }

    /// <summary>Gets or sets the <c>Accept-Ranges</c> HTTP header.</summary>
    StringValues AcceptRanges { get => this[HeaderNames.AcceptRanges]; set => this[HeaderNames.AcceptRanges] = value; }

    /// <summary>Gets or sets the <c>Access-Control-Allow-Credentials</c> HTTP header.</summary>
    StringValues AccessControlAllowCredentials { get => this[HeaderNames.AccessControlAllowCredentials]; set => this[HeaderNames.AccessControlAllowCredentials] = value; }

    /// <summary>Gets or sets the <c>Access-Control-Allow-Headers</c> HTTP header.</summary>
    StringValues AccessControlAllowHeaders { get => this[HeaderNames.AccessControlAllowHeaders]; set => this[HeaderNames.AccessControlAllowHeaders] = value; }

    /// <summary>Gets or sets the <c>Access-Control-Allow-Methods</c> HTTP header.</summary>
    StringValues AccessControlAllowMethods { get => this[HeaderNames.AccessControlAllowMethods]; set => this[HeaderNames.AccessControlAllowMethods] = value; }

    /// <summary>Gets or sets the <c>Access-Control-Allow-Origin</c> HTTP header.</summary>
    StringValues AccessControlAllowOrigin { get => this[HeaderNames.AccessControlAllowOrigin]; set => this[HeaderNames.AccessControlAllowOrigin] = value; }

    /// <summary>Gets or sets the <c>Access-Control-Expose-Headers</c> HTTP header.</summary>
    StringValues AccessControlExposeHeaders { get => this[HeaderNames.AccessControlExposeHeaders]; set => this[HeaderNames.AccessControlExposeHeaders] = value; }

    /// <summary>Gets or sets the <c>Access-Control-Max-Age</c> HTTP header.</summary>
    StringValues AccessControlMaxAge { get => this[HeaderNames.AccessControlMaxAge]; set => this[HeaderNames.AccessControlMaxAge] = value; }

    /// <summary>Gets or sets the <c>Access-Control-Request-Headers</c> HTTP header.</summary>
    StringValues AccessControlRequestHeaders { get => this[HeaderNames.AccessControlRequestHeaders]; set => this[HeaderNames.AccessControlRequestHeaders] = value; }

    /// <summary>Gets or sets the <c>Access-Control-Request-Method</c> HTTP header.</summary>
    StringValues AccessControlRequestMethod { get => this[HeaderNames.AccessControlRequestMethod]; set => this[HeaderNames.AccessControlRequestMethod] = value; }

    /// <summary>Gets or sets the <c>Age</c> HTTP header.</summary>
    StringValues Age { get => this[HeaderNames.Age]; set => this[HeaderNames.Age] = value; }

    /// <summary>Gets or sets the <c>Allow</c> HTTP header.</summary>
    StringValues Allow { get => this[HeaderNames.Allow]; set => this[HeaderNames.Allow] = value; }

    /// <summary>Gets or sets the <c>Alt-Svc</c> HTTP header.</summary>
    StringValues AltSvc { get => this[HeaderNames.AltSvc]; set => this[HeaderNames.AltSvc] = value; }

    /// <summary>Gets or sets the <c>Authorization</c> HTTP header.</summary>
    StringValues Authorization { get => this[HeaderNames.Authorization]; set => this[HeaderNames.Authorization] = value; }

    /// <summary>Gets or sets the <c>baggage</c> HTTP header.</summary>
    StringValues Baggage { get => this[HeaderNames.Baggage]; set => this[HeaderNames.Baggage] = value; }

    /// <summary>Gets or sets the <c>Cache-Control</c> HTTP header.</summary>
    StringValues CacheControl { get => this[HeaderNames.CacheControl]; set => this[HeaderNames.CacheControl] = value; }

    /// <summary>Gets or sets the <c>Connection</c> HTTP header.</summary>
    StringValues Connection { get => this[HeaderNames.Connection]; set => this[HeaderNames.Connection] = value; }

    /// <summary>Gets or sets the <c>Content-Disposition</c> HTTP header.</summary>
    StringValues ContentDisposition { get => this[HeaderNames.ContentDisposition]; set => this[HeaderNames.ContentDisposition] = value; }

    /// <summary>Gets or sets the <c>Content-Encoding</c> HTTP header.</summary>
    StringValues ContentEncoding { get => this[HeaderNames.ContentEncoding]; set => this[HeaderNames.ContentEncoding] = value; }

    /// <summary>Gets or sets the <c>Content-Language</c> HTTP header.</summary>
    StringValues ContentLanguage { get => this[HeaderNames.ContentLanguage]; set => this[HeaderNames.ContentLanguage] = value; }

    /// <summary>Gets or sets the <c>Content-Location</c> HTTP header.</summary>
    StringValues ContentLocation { get => this[HeaderNames.ContentLocation]; set => this[HeaderNames.ContentLocation] = value; }

    /// <summary>Gets or sets the <c>Content-MD5</c> HTTP header.</summary>
    StringValues ContentMD5 { get => this[HeaderNames.ContentMD5]; set => this[HeaderNames.ContentMD5] = value; }

    /// <summary>Gets or sets the <c>Content-Range</c> HTTP header.</summary>
    StringValues ContentRange { get => this[HeaderNames.ContentRange]; set => this[HeaderNames.ContentRange] = value; }

    /// <summary>Gets or sets the <c>Content-Security-Policy</c> HTTP header.</summary>
    StringValues ContentSecurityPolicy { get => this[HeaderNames.ContentSecurityPolicy]; set => this[HeaderNames.ContentSecurityPolicy] = value; }

    /// <summary>Gets or sets the <c>Content-Security-Policy-Report-Only</c> HTTP header.</summary>
    StringValues ContentSecurityPolicyReportOnly { get => this[HeaderNames.ContentSecurityPolicyReportOnly]; set => this[HeaderNames.ContentSecurityPolicyReportOnly] = value; }

    /// <summary>Gets or sets the <c>Content-Type</c> HTTP header.</summary>
    StringValues ContentType { get => this[HeaderNames.ContentType]; set => this[HeaderNames.ContentType] = value; }

    /// <summary>Gets or sets the <c>Correlation-Context</c> HTTP header.</summary>
    StringValues CorrelationContext { get => this[HeaderNames.CorrelationContext]; set => this[HeaderNames.CorrelationContext] = value; }

    /// <summary>Gets or sets the <c>Cookie</c> HTTP header.</summary>
    StringValues Cookie { get => this[HeaderNames.Cookie]; set => this[HeaderNames.Cookie] = value; }

    /// <summary>Gets or sets the <c>Date</c> HTTP header.</summary>
    StringValues Date { get => this[HeaderNames.Date]; set => this[HeaderNames.Date] = value; }

    /// <summary>Gets or sets the <c>ETag</c> HTTP header.</summary>
    StringValues ETag { get => this[HeaderNames.ETag]; set => this[HeaderNames.ETag] = value; }

    /// <summary>Gets or sets the <c>Expires</c> HTTP header.</summary>
    StringValues Expires { get => this[HeaderNames.Expires]; set => this[HeaderNames.Expires] = value; }

    /// <summary>Gets or sets the <c>Expect</c> HTTP header.</summary>
    StringValues Expect { get => this[HeaderNames.Expect]; set => this[HeaderNames.Expect] = value; }

    /// <summary>Gets or sets the <c>From</c> HTTP header.</summary>
    StringValues From { get => this[HeaderNames.From]; set => this[HeaderNames.From] = value; }

    /// <summary>Gets or sets the <c>Grpc-Accept-Encoding</c> HTTP header.</summary>
    StringValues GrpcAcceptEncoding { get => this[HeaderNames.GrpcAcceptEncoding]; set => this[HeaderNames.GrpcAcceptEncoding] = value; }

    /// <summary>Gets or sets the <c>Grpc-Encoding</c> HTTP header.</summary>
    StringValues GrpcEncoding { get => this[HeaderNames.GrpcEncoding]; set => this[HeaderNames.GrpcEncoding] = value; }

    /// <summary>Gets or sets the <c>Grpc-Message</c> HTTP header.</summary>
    StringValues GrpcMessage { get => this[HeaderNames.GrpcMessage]; set => this[HeaderNames.GrpcMessage] = value; }

    /// <summary>Gets or sets the <c>Grpc-Status</c> HTTP header.</summary>
    StringValues GrpcStatus { get => this[HeaderNames.GrpcStatus]; set => this[HeaderNames.GrpcStatus] = value; }

    /// <summary>Gets or sets the <c>Grpc-Timeout</c> HTTP header.</summary>
    StringValues GrpcTimeout { get => this[HeaderNames.GrpcTimeout]; set => this[HeaderNames.GrpcTimeout] = value; }

    /// <summary>Gets or sets the <c>Host</c> HTTP header.</summary>
    StringValues Host { get => this[HeaderNames.Host]; set => this[HeaderNames.Host] = value; }

    /// <summary>Gets or sets the <c>Keep-Alive</c> HTTP header.</summary>
    StringValues KeepAlive { get => this[HeaderNames.KeepAlive]; set => this[HeaderNames.KeepAlive] = value; }

    /// <summary>Gets or sets the <c>If-Match</c> HTTP header.</summary>
    StringValues IfMatch { get => this[HeaderNames.IfMatch]; set => this[HeaderNames.IfMatch] = value; }

    /// <summary>Gets or sets the <c>If-Modified-Since</c> HTTP header.</summary>
    StringValues IfModifiedSince { get => this[HeaderNames.IfModifiedSince]; set => this[HeaderNames.IfModifiedSince] = value; }

    /// <summary>Gets or sets the <c>If-None-Match</c> HTTP header.</summary>
    StringValues IfNoneMatch { get => this[HeaderNames.IfNoneMatch]; set => this[HeaderNames.IfNoneMatch] = value; }

    /// <summary>Gets or sets the <c>If-Range</c> HTTP header.</summary>
    StringValues IfRange { get => this[HeaderNames.IfRange]; set => this[HeaderNames.IfRange] = value; }

    /// <summary>Gets or sets the <c>If-Unmodified-Since</c> HTTP header.</summary>
    StringValues IfUnmodifiedSince { get => this[HeaderNames.IfUnmodifiedSince]; set => this[HeaderNames.IfUnmodifiedSince] = value; }

    /// <summary>Gets or sets the <c>Last-Modified</c> HTTP header.</summary>
    StringValues LastModified { get => this[HeaderNames.LastModified]; set => this[HeaderNames.LastModified] = value; }

    /// <summary>Gets or sets the <c>Link</c> HTTP header.</summary>
    StringValues Link { get => this[HeaderNames.Link]; set => this[HeaderNames.Link] = value; }

    /// <summary>Gets or sets the <c>Location</c> HTTP header.</summary>
    StringValues Location { get => this[HeaderNames.Location]; set => this[HeaderNames.Location] = value; }

    /// <summary>Gets or sets the <c>Max-Forwards</c> HTTP header.</summary>
    StringValues MaxForwards { get => this[HeaderNames.MaxForwards]; set => this[HeaderNames.MaxForwards] = value; }

    /// <summary>Gets or sets the <c>Origin</c> HTTP header.</summary>
    StringValues Origin { get => this[HeaderNames.Origin]; set => this[HeaderNames.Origin] = value; }

    /// <summary>Gets or sets the <c>Pragma</c> HTTP header.</summary>
    StringValues Pragma { get => this[HeaderNames.Pragma]; set => this[HeaderNames.Pragma] = value; }

    /// <summary>Gets or sets the <c>Proxy-Authenticate</c> HTTP header.</summary>
    StringValues ProxyAuthenticate { get => this[HeaderNames.ProxyAuthenticate]; set => this[HeaderNames.ProxyAuthenticate] = value; }

    /// <summary>Gets or sets the <c>Proxy-Authorization</c> HTTP header.</summary>
    StringValues ProxyAuthorization { get => this[HeaderNames.ProxyAuthorization]; set => this[HeaderNames.ProxyAuthorization] = value; }

    /// <summary>Gets or sets the <c>Proxy-Connection</c> HTTP header.</summary>
    StringValues ProxyConnection { get => this[HeaderNames.ProxyConnection]; set => this[HeaderNames.ProxyConnection] = value; }

    /// <summary>Gets or sets the <c>Range</c> HTTP header.</summary>
    StringValues Range { get => this[HeaderNames.Range]; set => this[HeaderNames.Range] = value; }

    /// <summary>Gets or sets the <c>Referer</c> HTTP header.</summary>
    StringValues Referer { get => this[HeaderNames.Referer]; set => this[HeaderNames.Referer] = value; }

    /// <summary>Gets or sets the <c>Retry-After</c> HTTP header.</summary>
    StringValues RetryAfter { get => this[HeaderNames.RetryAfter]; set => this[HeaderNames.RetryAfter] = value; }

    /// <summary>Gets or sets the <c>Request-Id</c> HTTP header.</summary>
    StringValues RequestId { get => this[HeaderNames.RequestId]; set => this[HeaderNames.RequestId] = value; }

    /// <summary>Gets or sets the <c>Sec-WebSocket-Accept</c> HTTP header.</summary>
    StringValues SecWebSocketAccept { get => this[HeaderNames.SecWebSocketAccept]; set => this[HeaderNames.SecWebSocketAccept] = value; }

    /// <summary>Gets or sets the <c>Sec-WebSocket-Key</c> HTTP header.</summary>
    StringValues SecWebSocketKey { get => this[HeaderNames.SecWebSocketKey]; set => this[HeaderNames.SecWebSocketKey] = value; }

    /// <summary>Gets or sets the <c>Sec-WebSocket-Protocol</c> HTTP header.</summary>
    StringValues SecWebSocketProtocol { get => this[HeaderNames.SecWebSocketProtocol]; set => this[HeaderNames.SecWebSocketProtocol] = value; }

    /// <summary>Gets or sets the <c>Sec-WebSocket-Version</c> HTTP header.</summary>
    StringValues SecWebSocketVersion { get => this[HeaderNames.SecWebSocketVersion]; set => this[HeaderNames.SecWebSocketVersion] = value; }

    /// <summary>Gets or sets the <c>Sec-WebSocket-Extensions</c> HTTP header.</summary>
    StringValues SecWebSocketExtensions { get => this[HeaderNames.SecWebSocketExtensions]; set => this[HeaderNames.SecWebSocketExtensions] = value; }

    /// <summary>Gets or sets the <c>Server</c> HTTP header.</summary>
    StringValues Server { get => this[HeaderNames.Server]; set => this[HeaderNames.Server] = value; }

    /// <summary>Gets or sets the <c>Set-Cookie</c> HTTP header.</summary>
    StringValues SetCookie { get => this[HeaderNames.SetCookie]; set => this[HeaderNames.SetCookie] = value; }

    /// <summary>Gets or sets the <c>Strict-Transport-Security</c> HTTP header.</summary>
    StringValues StrictTransportSecurity { get => this[HeaderNames.StrictTransportSecurity]; set => this[HeaderNames.StrictTransportSecurity] = value; }

    /// <summary>Gets or sets the <c>TE</c> HTTP header.</summary>
    StringValues TE { get => this[HeaderNames.TE]; set => this[HeaderNames.TE] = value; }

    /// <summary>Gets or sets the <c>Trailer</c> HTTP header.</summary>
    StringValues Trailer { get => this[HeaderNames.Trailer]; set => this[HeaderNames.Trailer] = value; }

    /// <summary>Gets or sets the <c>Transfer-Encoding</c> HTTP header.</summary>
    StringValues TransferEncoding { get => this[HeaderNames.TransferEncoding]; set => this[HeaderNames.TransferEncoding] = value; }

    /// <summary>Gets or sets the <c>Translate</c> HTTP header.</summary>
    StringValues Translate { get => this[HeaderNames.Translate]; set => this[HeaderNames.Translate] = value; }

    /// <summary>Gets or sets the <c>traceparent</c> HTTP header.</summary>
    StringValues TraceParent { get => this[HeaderNames.TraceParent]; set => this[HeaderNames.TraceParent] = value; }

    /// <summary>Gets or sets the <c>tracestate</c> HTTP header.</summary>
    StringValues TraceState { get => this[HeaderNames.TraceState]; set => this[HeaderNames.TraceState] = value; }

    /// <summary>Gets or sets the <c>Upgrade</c> HTTP header.</summary>
    StringValues Upgrade { get => this[HeaderNames.Upgrade]; set => this[HeaderNames.Upgrade] = value; }

    /// <summary>Gets or sets the <c>Upgrade-Insecure-Requests</c> HTTP header.</summary>
    StringValues UpgradeInsecureRequests { get => this[HeaderNames.UpgradeInsecureRequests]; set => this[HeaderNames.UpgradeInsecureRequests] = value; }

    /// <summary>Gets or sets the <c>User-Agent</c> HTTP header.</summary>
    StringValues UserAgent { get => this[HeaderNames.UserAgent]; set => this[HeaderNames.UserAgent] = value; }

    /// <summary>Gets or sets the <c>Vary</c> HTTP header.</summary>
    StringValues Vary { get => this[HeaderNames.Vary]; set => this[HeaderNames.Vary] = value; }

    /// <summary>Gets or sets the <c>Via</c> HTTP header.</summary>
    StringValues Via { get => this[HeaderNames.Via]; set => this[HeaderNames.Via] = value; }

    /// <summary>Gets or sets the <c>Warning</c> HTTP header.</summary>
    StringValues Warning { get => this[HeaderNames.Warning]; set => this[HeaderNames.Warning] = value; }

    /// <summary>Gets or sets the <c>Sec-WebSocket-Protocol</c> HTTP header.</summary>
    StringValues WebSocketSubProtocols { get => this[HeaderNames.WebSocketSubProtocols]; set => this[HeaderNames.WebSocketSubProtocols] = value; }

    /// <summary>Gets or sets the <c>WWW-Authenticate</c> HTTP header.</summary>
    StringValues WWWAuthenticate { get => this[HeaderNames.WWWAuthenticate]; set => this[HeaderNames.WWWAuthenticate] = value; }

    /// <summary>Gets or sets the <c>X-Content-Type-Options</c> HTTP header.</summary>
    StringValues XContentTypeOptions { get => this[HeaderNames.XContentTypeOptions]; set => this[HeaderNames.XContentTypeOptions] = value; }

    /// <summary>Gets or sets the <c>X-Frame-Options</c> HTTP header.</summary>
    StringValues XFrameOptions { get => this[HeaderNames.XFrameOptions]; set => this[HeaderNames.XFrameOptions] = value; }

    /// <summary>Gets or sets the <c>X-Powered-By</c> HTTP header.</summary>
    StringValues XPoweredBy { get => this[HeaderNames.XPoweredBy]; set => this[HeaderNames.XPoweredBy] = value; }

    /// <summary>Gets or sets the <c>X-Requested-With</c> HTTP header.</summary>
    StringValues XRequestedWith { get => this[HeaderNames.XRequestedWith]; set => this[HeaderNames.XRequestedWith] = value; }

    /// <summary>Gets or sets the <c>X-UA-Compatible</c> HTTP header.</summary>
    StringValues XUACompatible { get => this[HeaderNames.XUACompatible]; set => this[HeaderNames.XUACompatible] = value; }

    /// <summary>Gets or sets the <c>X-XSS-Protection</c> HTTP header.</summary>
    StringValues XXSSProtection { get => this[HeaderNames.XXSSProtection]; set => this[HeaderNames.XXSSProtection] = value; }
}
