// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Net.Http.Headers
{
    public static class HeaderNames
    {
        // Use readonly statics rather than constants so ReferenceEquals works
        public static readonly string Accept = "Accept";
        public static readonly string AcceptCharset = "Accept-Charset";
        public static readonly string AcceptEncoding = "Accept-Encoding";
        public static readonly string AcceptLanguage = "Accept-Language";
        public static readonly string AcceptRanges = "Accept-Ranges";
        public static readonly string AccessControlAllowCredentials = "Access-Control-Allow-Credentials";
        public static readonly string AccessControlAllowHeaders = "Access-Control-Allow-Headers";
        public static readonly string AccessControlAllowMethods = "Access-Control-Allow-Methods";
        public static readonly string AccessControlAllowOrigin = "Access-Control-Allow-Origin";
        public static readonly string AccessControlExposeHeaders = "Access-Control-Expose-Headers";
        public static readonly string AccessControlMaxAge = "Access-Control-Max-Age";
        public static readonly string AccessControlRequestHeaders = "Access-Control-Request-Headers";
        public static readonly string AccessControlRequestMethod = "Access-Control-Request-Method";
        public static readonly string Age = "Age";
        public static readonly string Allow = "Allow";
        public static readonly string AltSvc = "Alt-Svc";
        public static readonly string Authority = ":authority";
        public static readonly string Authorization = "Authorization";
        public static readonly string CacheControl = "Cache-Control";
        public static readonly string Connection = "Connection";
        public static readonly string ContentDisposition = "Content-Disposition";
        public static readonly string ContentEncoding = "Content-Encoding";
        public static readonly string ContentLanguage = "Content-Language";
        public static readonly string ContentLength = "Content-Length";
        public static readonly string ContentLocation = "Content-Location";
        public static readonly string ContentMD5 = "Content-MD5";
        public static readonly string ContentRange = "Content-Range";
        public static readonly string ContentSecurityPolicy = "Content-Security-Policy";
        public static readonly string ContentSecurityPolicyReportOnly = "Content-Security-Policy-Report-Only";
        public static readonly string ContentType = "Content-Type";
        public static readonly string CorrelationContext = "Correlation-Context";
        public static readonly string Cookie = "Cookie";
        public static readonly string Date = "Date";
        public static readonly string DNT = "DNT";
        public static readonly string ETag = "ETag";
        public static readonly string Expires = "Expires";
        public static readonly string Expect = "Expect";
        public static readonly string From = "From";
        public static readonly string Host = "Host";
        public static readonly string KeepAlive = "Keep-Alive";
        public static readonly string IfMatch = "If-Match";
        public static readonly string IfModifiedSince = "If-Modified-Since";
        public static readonly string IfNoneMatch = "If-None-Match";
        public static readonly string IfRange = "If-Range";
        public static readonly string IfUnmodifiedSince = "If-Unmodified-Since";
        public static readonly string LastModified = "Last-Modified";
        public static readonly string Location = "Location";
        public static readonly string MaxForwards = "Max-Forwards";
        public static readonly string Method = ":method";
        public static readonly string Origin = "Origin";
        public static readonly string Path = ":path";
        public static readonly string Pragma = "Pragma";
        public static readonly string ProxyAuthenticate = "Proxy-Authenticate";
        public static readonly string ProxyAuthorization = "Proxy-Authorization";
        public static readonly string Range = "Range";
        public static readonly string Referer = "Referer";
        public static readonly string RetryAfter = "Retry-After";
        public static readonly string RequestId = "Request-Id";
        public static readonly string Scheme = ":scheme";
        public static readonly string SecWebSocketAccept = "Sec-WebSocket-Accept";
        public static readonly string SecWebSocketKey = "Sec-WebSocket-Key";
        public static readonly string SecWebSocketProtocol = "Sec-WebSocket-Protocol";
        public static readonly string SecWebSocketVersion = "Sec-WebSocket-Version";
        public static readonly string Server = "Server";
        public static readonly string SetCookie = "Set-Cookie";
        public static readonly string Status = ":status";
        public static readonly string StrictTransportSecurity = "Strict-Transport-Security";
        public static readonly string TE = "TE";
        public static readonly string Trailer = "Trailer";
        public static readonly string TransferEncoding = "Transfer-Encoding";
        public static readonly string Translate = "Translate";
        public static readonly string TraceParent = "traceparent";
        public static readonly string TraceState = "tracestate";
        public static readonly string Upgrade = "Upgrade";
        public static readonly string UpgradeInsecureRequests = "Upgrade-Insecure-Requests";
        public static readonly string UserAgent = "User-Agent";
        public static readonly string Vary = "Vary";
        public static readonly string Via = "Via";
        public static readonly string Warning = "Warning";
        public static readonly string WebSocketSubProtocols = "Sec-WebSocket-Protocol";
        public static readonly string WWWAuthenticate = "WWW-Authenticate";
        public static readonly string XFrameOptions = "X-Frame-Options";
        public static readonly string XRequestedWith = "X-Requested-With";
    }
}
