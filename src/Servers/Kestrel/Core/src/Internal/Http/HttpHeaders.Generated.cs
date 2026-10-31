// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

#nullable enable

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    internal enum KnownHeaderType
    {
        Unknown,
        Accept,
        AcceptCharset,
        AcceptEncoding,
        AcceptLanguage,
        AcceptRanges,
        AccessControlAllowCredentials,
        AccessControlAllowHeaders,
        AccessControlAllowMethods,
        AccessControlAllowOrigin,
        AccessControlExposeHeaders,
        AccessControlMaxAge,
        AccessControlRequestHeaders,
        AccessControlRequestMethod,
        Age,
        Allow,
        AltSvc,
        AltUsed,
        Authority,
        Authorization,
        Baggage,
        CacheControl,
        Connection,
        ContentEncoding,
        ContentLanguage,
        ContentLength,
        ContentLocation,
        ContentMD5,
        ContentRange,
        ContentType,
        Cookie,
        CorrelationContext,
        Date,
        ETag,
        Expect,
        Expires,
        From,
        GrpcAcceptEncoding,
        GrpcEncoding,
        GrpcMessage,
        GrpcStatus,
        GrpcTimeout,
        Host,
        IfMatch,
        IfModifiedSince,
        IfNoneMatch,
        IfRange,
        IfUnmodifiedSince,
        KeepAlive,
        LastModified,
        Location,
        MaxForwards,
        Method,
        Origin,
        Path,
        Pragma,
        Protocol,
        ProxyAuthenticate,
        ProxyAuthorization,
        ProxyConnection,
        Range,
        Referer,
        RequestId,
        RetryAfter,
        Scheme,
        Server,
        SetCookie,
        TE,
        TraceParent,
        TraceState,
        Trailer,
        TransferEncoding,
        Translate,
        Upgrade,
        UpgradeInsecureRequests,
        UserAgent,
        Vary,
        Via,
        Warning,
        WWWAuthenticate,
    }

    internal static class HttpHeadersCompression
    {
        internal static (int index, bool matchedValue) MatchKnownHeaderQPack(KnownHeaderType knownHeader, string value)
        {
            switch (knownHeader)
            {
                case KnownHeaderType.Age:
                    switch (value)
                    {
                        case "0":
                            return (2, true);
                        default:
                            return (2, false);
                    }
                case KnownHeaderType.ContentLength:
                    switch (value)
                    {
                        case "0":
                            return (4, true);
                        default:
                            return (4, false);
                    }
                case KnownHeaderType.Date:
                    return (6, false);
                case KnownHeaderType.ETag:
                    return (7, false);
                case KnownHeaderType.LastModified:
                    return (10, false);
                case KnownHeaderType.Location:
                    return (12, false);
                case KnownHeaderType.SetCookie:
                    return (14, false);
                case KnownHeaderType.AcceptRanges:
                    switch (value)
                    {
                        case "bytes":
                            return (32, true);
                        default:
                            return (32, false);
                    }
                case KnownHeaderType.AccessControlAllowHeaders:
                    switch (value)
                    {
                        case "cache-control":
                            return (33, true);
                        case "content-type":
                            return (34, true);
                        case "*":
                            return (75, true);
                        default:
                            return (33, false);
                    }
                case KnownHeaderType.AccessControlAllowOrigin:
                    switch (value)
                    {
                        case "*":
                            return (35, true);
                        default:
                            return (35, false);
                    }
                case KnownHeaderType.CacheControl:
                    switch (value)
                    {
                        case "max-age=0":
                            return (36, true);
                        case "max-age=2592000":
                            return (37, true);
                        case "max-age=604800":
                            return (38, true);
                        case "no-cache":
                            return (39, true);
                        case "no-store":
                            return (40, true);
                        case "public, max-age=31536000":
                            return (41, true);
                        default:
                            return (36, false);
                    }
                case KnownHeaderType.ContentEncoding:
                    switch (value)
                    {
                        case "br":
                            return (42, true);
                        case "gzip":
                            return (43, true);
                        default:
                            return (42, false);
                    }
                case KnownHeaderType.ContentType:
                    switch (value)
                    {
                        case "application/dns-message":
                            return (44, true);
                        case "application/javascript":
                            return (45, true);
                        case "application/json":
                            return (46, true);
                        case "application/x-www-form-urlencoded":
                            return (47, true);
                        case "image/gif":
                            return (48, true);
                        case "image/jpeg":
                            return (49, true);
                        case "image/png":
                            return (50, true);
                        case "text/css":
                            return (51, true);
                        case "text/html; charset=utf-8":
                            return (52, true);
                        case "text/plain":
                            return (53, true);
                        case "text/plain;charset=utf-8":
                            return (54, true);
                        default:
                            return (44, false);
                    }
                case KnownHeaderType.Vary:
                    switch (value)
                    {
                        case "accept-encoding":
                            return (59, true);
                        case "origin":
                            return (60, true);
                        default:
                            return (59, false);
                    }
                case KnownHeaderType.AccessControlAllowCredentials:
                    switch (value)
                    {
                        case "FALSE":
                            return (73, true);
                        case "TRUE":
                            return (74, true);
                        default:
                            return (73, false);
                    }
                case KnownHeaderType.AccessControlAllowMethods:
                    switch (value)
                    {
                        case "get":
                            return (76, true);
                        case "get, post, options":
                            return (77, true);
                        case "options":
                            return (78, true);
                        default:
                            return (76, false);
                    }
                case KnownHeaderType.AccessControlExposeHeaders:
                    switch (value)
                    {
                        case "content-length":
                            return (79, true);
                        default:
                            return (79, false);
                    }
                case KnownHeaderType.AltSvc:
                    switch (value)
                    {
                        case "clear":
                            return (83, true);
                        default:
                            return (83, false);
                    }
                case KnownHeaderType.Server:
                    return (92, false);
                
                default:
                    return (-1, false);
            }
        }
    }

    internal partial class HttpHeaders
    {
        private readonly static HashSet<string> _internedHeaderNames = new HashSet<string>(91, StringComparer.OrdinalIgnoreCase)
        {
            HeaderNames.Accept,
            HeaderNames.AcceptCharset,
            HeaderNames.AcceptEncoding,
            HeaderNames.AcceptLanguage,
            HeaderNames.AcceptRanges,
            HeaderNames.AccessControlAllowCredentials,
            HeaderNames.AccessControlAllowHeaders,
            HeaderNames.AccessControlAllowMethods,
            HeaderNames.AccessControlAllowOrigin,
            HeaderNames.AccessControlExposeHeaders,
            HeaderNames.AccessControlMaxAge,
            HeaderNames.AccessControlRequestHeaders,
            HeaderNames.AccessControlRequestMethod,
            HeaderNames.Age,
            HeaderNames.Allow,
            HeaderNames.AltSvc,
            HeaderNames.Authorization,
            HeaderNames.Baggage,
            HeaderNames.CacheControl,
            HeaderNames.Connection,
            HeaderNames.ContentDisposition,
            HeaderNames.ContentEncoding,
            HeaderNames.ContentLanguage,
            HeaderNames.ContentLength,
            HeaderNames.ContentLocation,
            HeaderNames.ContentMD5,
            HeaderNames.ContentRange,
            HeaderNames.ContentSecurityPolicy,
            HeaderNames.ContentSecurityPolicyReportOnly,
            HeaderNames.ContentType,
            HeaderNames.CorrelationContext,
            HeaderNames.Cookie,
            HeaderNames.Date,
            HeaderNames.DNT,
            HeaderNames.ETag,
            HeaderNames.Expires,
            HeaderNames.Expect,
            HeaderNames.From,
            HeaderNames.GrpcAcceptEncoding,
            HeaderNames.GrpcEncoding,
            HeaderNames.GrpcMessage,
            HeaderNames.GrpcStatus,
            HeaderNames.GrpcTimeout,
            HeaderNames.Host,
            HeaderNames.KeepAlive,
            HeaderNames.IfMatch,
            HeaderNames.IfModifiedSince,
            HeaderNames.IfNoneMatch,
            HeaderNames.IfRange,
            HeaderNames.IfUnmodifiedSince,
            HeaderNames.LastModified,
            HeaderNames.Link,
            HeaderNames.Location,
            HeaderNames.MaxForwards,
            HeaderNames.Origin,
            HeaderNames.Pragma,
            HeaderNames.ProxyAuthenticate,
            HeaderNames.ProxyAuthorization,
            HeaderNames.ProxyConnection,
            HeaderNames.Range,
            HeaderNames.Referer,
            HeaderNames.RetryAfter,
            HeaderNames.RequestId,
            HeaderNames.SecWebSocketAccept,
            HeaderNames.SecWebSocketKey,
            HeaderNames.SecWebSocketProtocol,
            HeaderNames.SecWebSocketVersion,
            HeaderNames.SecWebSocketExtensions,
            HeaderNames.Server,
            HeaderNames.SetCookie,
            HeaderNames.StrictTransportSecurity,
            HeaderNames.TE,
            HeaderNames.Trailer,
            HeaderNames.TransferEncoding,
            HeaderNames.Translate,
            HeaderNames.TraceParent,
            HeaderNames.TraceState,
            HeaderNames.Upgrade,
            HeaderNames.UpgradeInsecureRequests,
            HeaderNames.UserAgent,
            HeaderNames.Vary,
            HeaderNames.Via,
            HeaderNames.Warning,
            HeaderNames.WebSocketSubProtocols,
            HeaderNames.WWWAuthenticate,
            HeaderNames.XContentTypeOptions,
            HeaderNames.XFrameOptions,
            HeaderNames.XPoweredBy,
            HeaderNames.XRequestedWith,
            HeaderNames.XUACompatible,
            HeaderNames.XXSSProtection,
        };
    }

    internal partial class HttpRequestHeaders : IHeaderDictionary
    {
        private HeaderReferences _headers;

        public bool HasConnection => (_bits & 0x2L) != 0;
        public bool HasCookie => (_bits & 0x80000L) != 0;
        public bool HasTransferEncoding => (_bits & 0x80000000000L) != 0;

        public int HostCount => _headers._Host.Count;

        public override StringValues HeaderConnection
        {
            get
            {
                if ((_bits & 0x2L) != 0)
                {
                    return _headers._Connection;
                }
                return StringValues.Empty;
            }
            set
            {
                if (!StringValues.IsNullOrEmpty(value))
                {
                    _bits |= 0x2L;
                    _headers._Connection = value; 
                }
                else
                {
                    _bits &= ~0x2L;
                    _headers._Connection = default; 
                }
            }
        }
        public StringValues HeaderHost
        {
            get
            {
                if ((_bits & 0x4L) != 0)
                {
                    return _headers._Host;
                }
                return StringValues.Empty;
            }
            set
            {
                if (!StringValues.IsNullOrEmpty(value))
                {
                    _bits |= 0x4L;
                    _headers._Host = value; 
                }
                else
                {
                    _bits &= ~0x4L;
                    _headers._Host = default; 
                }
            }
        }
        public StringValues HeaderAuthority
        {
            get
            {
                if ((_bits & 0x10L) != 0)
                {
                    return _headers._Authority;
                }
                return StringValues.Empty;
            }
            set
            {
                if (!StringValues.IsNullOrEmpty(value))
                {
                    _bits |= 0x10L;
                    _headers._Authority = value; 
                }
                else
                {
                    _bits &= ~0x10L;
                    _headers._Authority = default; 
                }
            }
        }
        public StringValues HeaderMethod
        {
            get
            {
                if ((_bits & 0x20L) != 0)
                {
                    return _headers._Method;
                }
                return StringValues.Empty;
            }
            set
            {
                if (!StringValues.IsNullOrEmpty(value))
                {
                    _bits |= 0x20L;
                    _headers._Method = value; 
                }
                else
                {
                    _bits &= ~0x20L;
                    _headers._Method = default; 
                }
            }
        }
        public StringValues HeaderPath
        {
            get
            {
                if ((_bits & 0x40L) != 0)
                {
                    return _headers._Path;
                }
                return StringValues.Empty;
            }
            set
            {
                if (!StringValues.IsNullOrEmpty(value))
                {
                    _bits |= 0x40L;
                    _headers._Path = value; 
                }
                else
                {
                    _bits &= ~0x40L;
                    _headers._Path = default; 
                }
            }
        }
        public StringValues HeaderProtocol
        {
            get
            {
                if ((_bits & 0x80L) != 0)
                {
                    return _headers._Protocol;
                }
                return StringValues.Empty;
            }
            set
            {
                if (!StringValues.IsNullOrEmpty(value))
                {
                    _bits |= 0x80L;
                    _headers._Protocol = value; 
                }
                else
                {
                    _bits &= ~0x80L;
                    _headers._Protocol = default; 
                }
            }
        }
        public StringValues HeaderScheme
        {
            get
            {
                if ((_bits & 0x100L) != 0)
                {
                    return _headers._Scheme;
                }
                return StringValues.Empty;
            }
            set
            {
                if (!StringValues.IsNullOrEmpty(value))
                {
                    _bits |= 0x100L;
                    _headers._Scheme = value; 
                }
                else
                {
                    _bits &= ~0x100L;
                    _headers._Scheme = default; 
                }
            }
        }
        public StringValues HeaderTransferEncoding
        {
            get
            {
                if ((_bits & 0x80000000000L) != 0)
                {
                    return _headers._TransferEncoding;
                }
                return StringValues.Empty;
            }
            set
            {
                if (!StringValues.IsNullOrEmpty(value))
                {
                    _bits |= 0x80000000000L;
                    _headers._TransferEncoding = value; 
                }
                else
                {
                    _bits &= ~0x80000000000L;
                    _headers._TransferEncoding = default; 
                }
            }
        }
        public StringValues HeaderContentLength
        {
            get
            {
                if (_contentLength.HasValue)
                {
                    return new StringValues(HeaderUtilities.FormatNonNegativeInt64(_contentLength.Value));
                }
                return StringValues.Empty;
            }
            set
            {
                _contentLength = ParseContentLength(value.ToString());
            }
        }
        
        StringValues IHeaderDictionary.Accept
        {
            get
            {
                var value = _headers._Accept;
                if ((_bits & 0x1L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x1L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._Accept = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._Accept = default;
                }
            }
        }
        StringValues IHeaderDictionary.Connection
        {
            get
            {
                var value = _headers._Connection;
                if ((_bits & 0x2L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x2L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._Connection = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._Connection = default;
                }
            }
        }
        StringValues IHeaderDictionary.Host
        {
            get
            {
                var value = _headers._Host;
                if ((_bits & 0x4L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x4L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._Host = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._Host = default;
                }
            }
        }
        StringValues IHeaderDictionary.UserAgent
        {
            get
            {
                var value = _headers._UserAgent;
                if ((_bits & 0x8L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x8L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._UserAgent = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._UserAgent = default;
                }
            }
        }
        StringValues IHeaderDictionary.AcceptCharset
        {
            get
            {
                var value = _headers._AcceptCharset;
                if ((_bits & 0x200L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x200L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._AcceptCharset = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._AcceptCharset = default;
                }
            }
        }
        StringValues IHeaderDictionary.AcceptEncoding
        {
            get
            {
                var value = _headers._AcceptEncoding;
                if ((_bits & 0x400L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x400L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._AcceptEncoding = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._AcceptEncoding = default;
                }
            }
        }
        StringValues IHeaderDictionary.AcceptLanguage
        {
            get
            {
                var value = _headers._AcceptLanguage;
                if ((_bits & 0x800L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x800L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._AcceptLanguage = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._AcceptLanguage = default;
                }
            }
        }
        StringValues IHeaderDictionary.AccessControlRequestHeaders
        {
            get
            {
                var value = _headers._AccessControlRequestHeaders;
                if ((_bits & 0x1000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x1000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._AccessControlRequestHeaders = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._AccessControlRequestHeaders = default;
                }
            }
        }
        StringValues IHeaderDictionary.AccessControlRequestMethod
        {
            get
            {
                var value = _headers._AccessControlRequestMethod;
                if ((_bits & 0x2000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x2000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._AccessControlRequestMethod = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._AccessControlRequestMethod = default;
                }
            }
        }
        StringValues IHeaderDictionary.Authorization
        {
            get
            {
                var value = _headers._Authorization;
                if ((_bits & 0x8000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x8000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._Authorization = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._Authorization = default;
                }
            }
        }
        StringValues IHeaderDictionary.Baggage
        {
            get
            {
                var value = _headers._Baggage;
                if ((_bits & 0x10000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x10000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._Baggage = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._Baggage = default;
                }
            }
        }
        StringValues IHeaderDictionary.CacheControl
        {
            get
            {
                var value = _headers._CacheControl;
                if ((_bits & 0x20000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x20000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._CacheControl = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._CacheControl = default;
                }
            }
        }
        StringValues IHeaderDictionary.ContentType
        {
            get
            {
                var value = _headers._ContentType;
                if ((_bits & 0x40000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x40000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._ContentType = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._ContentType = default;
                }
            }
        }
        StringValues IHeaderDictionary.Cookie
        {
            get
            {
                var value = _headers._Cookie;
                if ((_bits & 0x80000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x80000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._Cookie = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._Cookie = default;
                }
            }
        }
        StringValues IHeaderDictionary.CorrelationContext
        {
            get
            {
                var value = _headers._CorrelationContext;
                if ((_bits & 0x100000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x100000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._CorrelationContext = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._CorrelationContext = default;
                }
            }
        }
        StringValues IHeaderDictionary.Date
        {
            get
            {
                var value = _headers._Date;
                if ((_bits & 0x200000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x200000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._Date = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._Date = default;
                }
            }
        }
        StringValues IHeaderDictionary.Expect
        {
            get
            {
                var value = _headers._Expect;
                if ((_bits & 0x400000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x400000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._Expect = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._Expect = default;
                }
            }
        }
        StringValues IHeaderDictionary.From
        {
            get
            {
                var value = _headers._From;
                if ((_bits & 0x800000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x800000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._From = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._From = default;
                }
            }
        }
        StringValues IHeaderDictionary.GrpcAcceptEncoding
        {
            get
            {
                var value = _headers._GrpcAcceptEncoding;
                if ((_bits & 0x1000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x1000000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._GrpcAcceptEncoding = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._GrpcAcceptEncoding = default;
                }
            }
        }
        StringValues IHeaderDictionary.GrpcEncoding
        {
            get
            {
                var value = _headers._GrpcEncoding;
                if ((_bits & 0x2000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x2000000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._GrpcEncoding = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._GrpcEncoding = default;
                }
            }
        }
        StringValues IHeaderDictionary.GrpcTimeout
        {
            get
            {
                var value = _headers._GrpcTimeout;
                if ((_bits & 0x4000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x4000000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._GrpcTimeout = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._GrpcTimeout = default;
                }
            }
        }
        StringValues IHeaderDictionary.IfMatch
        {
            get
            {
                var value = _headers._IfMatch;
                if ((_bits & 0x8000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x8000000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._IfMatch = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._IfMatch = default;
                }
            }
        }
        StringValues IHeaderDictionary.IfModifiedSince
        {
            get
            {
                var value = _headers._IfModifiedSince;
                if ((_bits & 0x10000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x10000000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._IfModifiedSince = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._IfModifiedSince = default;
                }
            }
        }
        StringValues IHeaderDictionary.IfNoneMatch
        {
            get
            {
                var value = _headers._IfNoneMatch;
                if ((_bits & 0x20000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x20000000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._IfNoneMatch = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._IfNoneMatch = default;
                }
            }
        }
        StringValues IHeaderDictionary.IfRange
        {
            get
            {
                var value = _headers._IfRange;
                if ((_bits & 0x40000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x40000000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._IfRange = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._IfRange = default;
                }
            }
        }
        StringValues IHeaderDictionary.IfUnmodifiedSince
        {
            get
            {
                var value = _headers._IfUnmodifiedSince;
                if ((_bits & 0x80000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x80000000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._IfUnmodifiedSince = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._IfUnmodifiedSince = default;
                }
            }
        }
        StringValues IHeaderDictionary.KeepAlive
        {
            get
            {
                var value = _headers._KeepAlive;
                if ((_bits & 0x100000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x100000000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._KeepAlive = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._KeepAlive = default;
                }
            }
        }
        StringValues IHeaderDictionary.MaxForwards
        {
            get
            {
                var value = _headers._MaxForwards;
                if ((_bits & 0x200000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x200000000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._MaxForwards = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._MaxForwards = default;
                }
            }
        }
        StringValues IHeaderDictionary.Origin
        {
            get
            {
                var value = _headers._Origin;
                if ((_bits & 0x400000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x400000000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._Origin = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._Origin = default;
                }
            }
        }
        StringValues IHeaderDictionary.Pragma
        {
            get
            {
                var value = _headers._Pragma;
                if ((_bits & 0x800000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x800000000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._Pragma = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._Pragma = default;
                }
            }
        }
        StringValues IHeaderDictionary.ProxyAuthorization
        {
            get
            {
                var value = _headers._ProxyAuthorization;
                if ((_bits & 0x1000000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x1000000000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._ProxyAuthorization = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._ProxyAuthorization = default;
                }
            }
        }
        StringValues IHeaderDictionary.Range
        {
            get
            {
                var value = _headers._Range;
                if ((_bits & 0x2000000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x2000000000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._Range = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._Range = default;
                }
            }
        }
        StringValues IHeaderDictionary.Referer
        {
            get
            {
                var value = _headers._Referer;
                if ((_bits & 0x4000000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x4000000000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._Referer = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._Referer = default;
                }
            }
        }
        StringValues IHeaderDictionary.RequestId
        {
            get
            {
                var value = _headers._RequestId;
                if ((_bits & 0x8000000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x8000000000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._RequestId = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._RequestId = default;
                }
            }
        }
        StringValues IHeaderDictionary.TE
        {
            get
            {
                var value = _headers._TE;
                if ((_bits & 0x10000000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x10000000000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._TE = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._TE = default;
                }
            }
        }
        StringValues IHeaderDictionary.TraceParent
        {
            get
            {
                var value = _headers._TraceParent;
                if ((_bits & 0x20000000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x20000000000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._TraceParent = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._TraceParent = default;
                }
            }
        }
        StringValues IHeaderDictionary.TraceState
        {
            get
            {
                var value = _headers._TraceState;
                if ((_bits & 0x40000000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x40000000000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._TraceState = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._TraceState = default;
                }
            }
        }
        StringValues IHeaderDictionary.TransferEncoding
        {
            get
            {
                var value = _headers._TransferEncoding;
                if ((_bits & 0x80000000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x80000000000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._TransferEncoding = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._TransferEncoding = default;
                }
            }
        }
        StringValues IHeaderDictionary.Translate
        {
            get
            {
                var value = _headers._Translate;
                if ((_bits & 0x100000000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x100000000000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._Translate = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._Translate = default;
                }
            }
        }
        StringValues IHeaderDictionary.Upgrade
        {
            get
            {
                var value = _headers._Upgrade;
                if ((_bits & 0x200000000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x200000000000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._Upgrade = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._Upgrade = default;
                }
            }
        }
        StringValues IHeaderDictionary.UpgradeInsecureRequests
        {
            get
            {
                var value = _headers._UpgradeInsecureRequests;
                if ((_bits & 0x400000000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x400000000000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._UpgradeInsecureRequests = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._UpgradeInsecureRequests = default;
                }
            }
        }
        StringValues IHeaderDictionary.Via
        {
            get
            {
                var value = _headers._Via;
                if ((_bits & 0x800000000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x800000000000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._Via = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._Via = default;
                }
            }
        }
        StringValues IHeaderDictionary.Warning
        {
            get
            {
                var value = _headers._Warning;
                if ((_bits & 0x1000000000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x1000000000000L;
                if (value.Count > 0)
                {
                    _bits |= flag;
                    _headers._Warning = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._Warning = default;
                }
            }
        }
        
        StringValues IHeaderDictionary.AcceptRanges
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.AcceptRanges, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.AcceptRanges, value);
            }
        }
        StringValues IHeaderDictionary.AccessControlAllowCredentials
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.AccessControlAllowCredentials, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.AccessControlAllowCredentials, value);
            }
        }
        StringValues IHeaderDictionary.AccessControlAllowHeaders
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.AccessControlAllowHeaders, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.AccessControlAllowHeaders, value);
            }
        }
        StringValues IHeaderDictionary.AccessControlAllowMethods
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.AccessControlAllowMethods, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.AccessControlAllowMethods, value);
            }
        }
        StringValues IHeaderDictionary.AccessControlAllowOrigin
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.AccessControlAllowOrigin, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.AccessControlAllowOrigin, value);
            }
        }
        StringValues IHeaderDictionary.AccessControlExposeHeaders
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.AccessControlExposeHeaders, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.AccessControlExposeHeaders, value);
            }
        }
        StringValues IHeaderDictionary.AccessControlMaxAge
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.AccessControlMaxAge, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.AccessControlMaxAge, value);
            }
        }
        StringValues IHeaderDictionary.Age
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Age, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.Age, value);
            }
        }
        StringValues IHeaderDictionary.Allow
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Allow, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.Allow, value);
            }
        }
        StringValues IHeaderDictionary.AltSvc
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.AltSvc, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.AltSvc, value);
            }
        }
        StringValues IHeaderDictionary.ContentDisposition
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.ContentDisposition, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.ContentDisposition, value);
            }
        }
        StringValues IHeaderDictionary.ContentEncoding
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.ContentEncoding, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.ContentEncoding, value);
            }
        }
        StringValues IHeaderDictionary.ContentLanguage
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.ContentLanguage, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.ContentLanguage, value);
            }
        }
        StringValues IHeaderDictionary.ContentLocation
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.ContentLocation, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.ContentLocation, value);
            }
        }
        StringValues IHeaderDictionary.ContentMD5
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.ContentMD5, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.ContentMD5, value);
            }
        }
        StringValues IHeaderDictionary.ContentRange
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.ContentRange, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.ContentRange, value);
            }
        }
        StringValues IHeaderDictionary.ContentSecurityPolicy
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.ContentSecurityPolicy, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.ContentSecurityPolicy, value);
            }
        }
        StringValues IHeaderDictionary.ContentSecurityPolicyReportOnly
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.ContentSecurityPolicyReportOnly, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.ContentSecurityPolicyReportOnly, value);
            }
        }
        StringValues IHeaderDictionary.ETag
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.ETag, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.ETag, value);
            }
        }
        StringValues IHeaderDictionary.Expires
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Expires, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.Expires, value);
            }
        }
        StringValues IHeaderDictionary.GrpcMessage
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.GrpcMessage, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.GrpcMessage, value);
            }
        }
        StringValues IHeaderDictionary.GrpcStatus
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.GrpcStatus, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.GrpcStatus, value);
            }
        }
        StringValues IHeaderDictionary.LastModified
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.LastModified, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.LastModified, value);
            }
        }
        StringValues IHeaderDictionary.Link
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Link, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.Link, value);
            }
        }
        StringValues IHeaderDictionary.Location
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Location, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.Location, value);
            }
        }
        StringValues IHeaderDictionary.ProxyAuthenticate
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.ProxyAuthenticate, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.ProxyAuthenticate, value);
            }
        }
        StringValues IHeaderDictionary.ProxyConnection
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.ProxyConnection, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.ProxyConnection, value);
            }
        }
        StringValues IHeaderDictionary.RetryAfter
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.RetryAfter, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.RetryAfter, value);
            }
        }
        StringValues IHeaderDictionary.SecWebSocketAccept
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.SecWebSocketAccept, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.SecWebSocketAccept, value);
            }
        }
        StringValues IHeaderDictionary.SecWebSocketKey
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.SecWebSocketKey, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.SecWebSocketKey, value);
            }
        }
        StringValues IHeaderDictionary.SecWebSocketProtocol
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.SecWebSocketProtocol, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.SecWebSocketProtocol, value);
            }
        }
        StringValues IHeaderDictionary.SecWebSocketVersion
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.SecWebSocketVersion, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.SecWebSocketVersion, value);
            }
        }
        StringValues IHeaderDictionary.SecWebSocketExtensions
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.SecWebSocketExtensions, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.SecWebSocketExtensions, value);
            }
        }
        StringValues IHeaderDictionary.Server
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Server, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.Server, value);
            }
        }
        StringValues IHeaderDictionary.SetCookie
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.SetCookie, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.SetCookie, value);
            }
        }
        StringValues IHeaderDictionary.StrictTransportSecurity
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.StrictTransportSecurity, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.StrictTransportSecurity, value);
            }
        }
        StringValues IHeaderDictionary.Trailer
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Trailer, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.Trailer, value);
            }
        }
        StringValues IHeaderDictionary.Vary
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Vary, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.Vary, value);
            }
        }
        StringValues IHeaderDictionary.WebSocketSubProtocols
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.WebSocketSubProtocols, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.WebSocketSubProtocols, value);
            }
        }
        StringValues IHeaderDictionary.WWWAuthenticate
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.WWWAuthenticate, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.WWWAuthenticate, value);
            }
        }
        StringValues IHeaderDictionary.XContentTypeOptions
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.XContentTypeOptions, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.XContentTypeOptions, value);
            }
        }
        StringValues IHeaderDictionary.XFrameOptions
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.XFrameOptions, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.XFrameOptions, value);
            }
        }
        StringValues IHeaderDictionary.XPoweredBy
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.XPoweredBy, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.XPoweredBy, value);
            }
        }
        StringValues IHeaderDictionary.XRequestedWith
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.XRequestedWith, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.XRequestedWith, value);
            }
        }
        StringValues IHeaderDictionary.XUACompatible
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.XUACompatible, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.XUACompatible, value);
            }
        }
        StringValues IHeaderDictionary.XXSSProtection
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.XXSSProtection, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                SetValueUnknown(HeaderNames.XXSSProtection, value);
            }
        }

        protected override int GetCountFast()
        {
            return (_contentLength.HasValue ? 1 : 0 ) + BitOperations.PopCount((ulong)_bits) + (MaybeUnknown?.Count ?? 0);
        }

        protected override bool TryGetValueFast(string key, out StringValues value)
        {
            value = default;
            switch (key.Length)
            {
                case 2:
                {
                    if (ReferenceEquals(HeaderNames.TE, key))
                    {
                        if ((_bits & 0x10000000000L) != 0)
                        {
                            value = _headers._TE;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.TE.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000000000L) != 0)
                        {
                            value = _headers._TE;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 3:
                {
                    if (ReferenceEquals(HeaderNames.Via, key))
                    {
                        if ((_bits & 0x800000000000L) != 0)
                        {
                            value = _headers._Via;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.Via.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800000000000L) != 0)
                        {
                            value = _headers._Via;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 4:
                {
                    if (ReferenceEquals(HeaderNames.Host, key))
                    {
                        if ((_bits & 0x4L) != 0)
                        {
                            value = _headers._Host;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Date, key))
                    {
                        if ((_bits & 0x200000L) != 0)
                        {
                            value = _headers._Date;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.From, key))
                    {
                        if ((_bits & 0x800000L) != 0)
                        {
                            value = _headers._From;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.Host.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4L) != 0)
                        {
                            value = _headers._Host;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Date.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200000L) != 0)
                        {
                            value = _headers._Date;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.From.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800000L) != 0)
                        {
                            value = _headers._From;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 5:
                {
                    if (ReferenceEquals(InternalHeaderNames.Path, key))
                    {
                        if ((_bits & 0x40L) != 0)
                        {
                            value = _headers._Path;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Range, key))
                    {
                        if ((_bits & 0x2000000000L) != 0)
                        {
                            value = _headers._Range;
                            return true;
                        }
                        return false;
                    }

                    if (InternalHeaderNames.Path.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40L) != 0)
                        {
                            value = _headers._Path;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Range.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000000000L) != 0)
                        {
                            value = _headers._Range;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 6:
                {
                    if (ReferenceEquals(HeaderNames.Accept, key))
                    {
                        if ((_bits & 0x1L) != 0)
                        {
                            value = _headers._Accept;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Cookie, key))
                    {
                        if ((_bits & 0x80000L) != 0)
                        {
                            value = _headers._Cookie;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Expect, key))
                    {
                        if ((_bits & 0x400000L) != 0)
                        {
                            value = _headers._Expect;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Origin, key))
                    {
                        if ((_bits & 0x400000000L) != 0)
                        {
                            value = _headers._Origin;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Pragma, key))
                    {
                        if ((_bits & 0x800000000L) != 0)
                        {
                            value = _headers._Pragma;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.Accept.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1L) != 0)
                        {
                            value = _headers._Accept;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Cookie.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80000L) != 0)
                        {
                            value = _headers._Cookie;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Expect.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400000L) != 0)
                        {
                            value = _headers._Expect;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Origin.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400000000L) != 0)
                        {
                            value = _headers._Origin;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Pragma.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800000000L) != 0)
                        {
                            value = _headers._Pragma;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 7:
                {
                    if (ReferenceEquals(InternalHeaderNames.Method, key))
                    {
                        if ((_bits & 0x20L) != 0)
                        {
                            value = _headers._Method;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(InternalHeaderNames.Scheme, key))
                    {
                        if ((_bits & 0x100L) != 0)
                        {
                            value = _headers._Scheme;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Baggage, key))
                    {
                        if ((_bits & 0x10000L) != 0)
                        {
                            value = _headers._Baggage;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Referer, key))
                    {
                        if ((_bits & 0x4000000000L) != 0)
                        {
                            value = _headers._Referer;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Upgrade, key))
                    {
                        if ((_bits & 0x200000000000L) != 0)
                        {
                            value = _headers._Upgrade;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Warning, key))
                    {
                        if ((_bits & 0x1000000000000L) != 0)
                        {
                            value = _headers._Warning;
                            return true;
                        }
                        return false;
                    }

                    if (InternalHeaderNames.Method.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20L) != 0)
                        {
                            value = _headers._Method;
                            return true;
                        }
                        return false;
                    }
                    if (InternalHeaderNames.Scheme.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100L) != 0)
                        {
                            value = _headers._Scheme;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Baggage.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000L) != 0)
                        {
                            value = _headers._Baggage;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Referer.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000000000L) != 0)
                        {
                            value = _headers._Referer;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Upgrade.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200000000000L) != 0)
                        {
                            value = _headers._Upgrade;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Warning.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000000000000L) != 0)
                        {
                            value = _headers._Warning;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 8:
                {
                    if (ReferenceEquals(InternalHeaderNames.AltUsed, key))
                    {
                        if ((_bits & 0x4000L) != 0)
                        {
                            value = _headers._AltUsed;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.IfMatch, key))
                    {
                        if ((_bits & 0x8000000L) != 0)
                        {
                            value = _headers._IfMatch;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.IfRange, key))
                    {
                        if ((_bits & 0x40000000L) != 0)
                        {
                            value = _headers._IfRange;
                            return true;
                        }
                        return false;
                    }

                    if (InternalHeaderNames.AltUsed.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000L) != 0)
                        {
                            value = _headers._AltUsed;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.IfMatch.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000000L) != 0)
                        {
                            value = _headers._IfMatch;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.IfRange.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40000000L) != 0)
                        {
                            value = _headers._IfRange;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 9:
                {
                    if (ReferenceEquals(InternalHeaderNames.Protocol, key))
                    {
                        if ((_bits & 0x80L) != 0)
                        {
                            value = _headers._Protocol;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Translate, key))
                    {
                        if ((_bits & 0x100000000000L) != 0)
                        {
                            value = _headers._Translate;
                            return true;
                        }
                        return false;
                    }

                    if (InternalHeaderNames.Protocol.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80L) != 0)
                        {
                            value = _headers._Protocol;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Translate.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100000000000L) != 0)
                        {
                            value = _headers._Translate;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 10:
                {
                    if (ReferenceEquals(HeaderNames.Connection, key))
                    {
                        if ((_bits & 0x2L) != 0)
                        {
                            value = _headers._Connection;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.UserAgent, key))
                    {
                        if ((_bits & 0x8L) != 0)
                        {
                            value = _headers._UserAgent;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(InternalHeaderNames.Authority, key))
                    {
                        if ((_bits & 0x10L) != 0)
                        {
                            value = _headers._Authority;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.KeepAlive, key))
                    {
                        if ((_bits & 0x100000000L) != 0)
                        {
                            value = _headers._KeepAlive;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.RequestId, key))
                    {
                        if ((_bits & 0x8000000000L) != 0)
                        {
                            value = _headers._RequestId;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.TraceState, key))
                    {
                        if ((_bits & 0x40000000000L) != 0)
                        {
                            value = _headers._TraceState;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.Connection.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2L) != 0)
                        {
                            value = _headers._Connection;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.UserAgent.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8L) != 0)
                        {
                            value = _headers._UserAgent;
                            return true;
                        }
                        return false;
                    }
                    if (InternalHeaderNames.Authority.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10L) != 0)
                        {
                            value = _headers._Authority;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.KeepAlive.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100000000L) != 0)
                        {
                            value = _headers._KeepAlive;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.RequestId.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000000000L) != 0)
                        {
                            value = _headers._RequestId;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.TraceState.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40000000000L) != 0)
                        {
                            value = _headers._TraceState;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 11:
                {
                    if (ReferenceEquals(HeaderNames.TraceParent, key))
                    {
                        if ((_bits & 0x20000000000L) != 0)
                        {
                            value = _headers._TraceParent;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.TraceParent.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20000000000L) != 0)
                        {
                            value = _headers._TraceParent;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 12:
                {
                    if (ReferenceEquals(HeaderNames.ContentType, key))
                    {
                        if ((_bits & 0x40000L) != 0)
                        {
                            value = _headers._ContentType;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.GrpcTimeout, key))
                    {
                        if ((_bits & 0x4000000L) != 0)
                        {
                            value = _headers._GrpcTimeout;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.MaxForwards, key))
                    {
                        if ((_bits & 0x200000000L) != 0)
                        {
                            value = _headers._MaxForwards;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.ContentType.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40000L) != 0)
                        {
                            value = _headers._ContentType;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.GrpcTimeout.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000000L) != 0)
                        {
                            value = _headers._GrpcTimeout;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.MaxForwards.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200000000L) != 0)
                        {
                            value = _headers._MaxForwards;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 13:
                {
                    if (ReferenceEquals(HeaderNames.Authorization, key))
                    {
                        if ((_bits & 0x8000L) != 0)
                        {
                            value = _headers._Authorization;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.CacheControl, key))
                    {
                        if ((_bits & 0x20000L) != 0)
                        {
                            value = _headers._CacheControl;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.GrpcEncoding, key))
                    {
                        if ((_bits & 0x2000000L) != 0)
                        {
                            value = _headers._GrpcEncoding;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.IfNoneMatch, key))
                    {
                        if ((_bits & 0x20000000L) != 0)
                        {
                            value = _headers._IfNoneMatch;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.Authorization.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000L) != 0)
                        {
                            value = _headers._Authorization;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.CacheControl.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20000L) != 0)
                        {
                            value = _headers._CacheControl;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.GrpcEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000000L) != 0)
                        {
                            value = _headers._GrpcEncoding;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.IfNoneMatch.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20000000L) != 0)
                        {
                            value = _headers._IfNoneMatch;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 14:
                {
                    if (ReferenceEquals(HeaderNames.AcceptCharset, key))
                    {
                        if ((_bits & 0x200L) != 0)
                        {
                            value = _headers._AcceptCharset;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentLength, key))
                    {
                        if (_contentLength.HasValue)
                        {
                            value = HeaderUtilities.FormatNonNegativeInt64(_contentLength.Value);
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.AcceptCharset.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200L) != 0)
                        {
                            value = _headers._AcceptCharset;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentLength.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if (_contentLength.HasValue)
                        {
                            value = HeaderUtilities.FormatNonNegativeInt64(_contentLength.Value);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 15:
                {
                    if (ReferenceEquals(HeaderNames.AcceptEncoding, key))
                    {
                        if ((_bits & 0x400L) != 0)
                        {
                            value = _headers._AcceptEncoding;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.AcceptLanguage, key))
                    {
                        if ((_bits & 0x800L) != 0)
                        {
                            value = _headers._AcceptLanguage;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.AcceptEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400L) != 0)
                        {
                            value = _headers._AcceptEncoding;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.AcceptLanguage.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800L) != 0)
                        {
                            value = _headers._AcceptLanguage;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 17:
                {
                    if (ReferenceEquals(HeaderNames.IfModifiedSince, key))
                    {
                        if ((_bits & 0x10000000L) != 0)
                        {
                            value = _headers._IfModifiedSince;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.TransferEncoding, key))
                    {
                        if ((_bits & 0x80000000000L) != 0)
                        {
                            value = _headers._TransferEncoding;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.IfModifiedSince.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000000L) != 0)
                        {
                            value = _headers._IfModifiedSince;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.TransferEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80000000000L) != 0)
                        {
                            value = _headers._TransferEncoding;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 19:
                {
                    if (ReferenceEquals(HeaderNames.CorrelationContext, key))
                    {
                        if ((_bits & 0x100000L) != 0)
                        {
                            value = _headers._CorrelationContext;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.IfUnmodifiedSince, key))
                    {
                        if ((_bits & 0x80000000L) != 0)
                        {
                            value = _headers._IfUnmodifiedSince;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ProxyAuthorization, key))
                    {
                        if ((_bits & 0x1000000000L) != 0)
                        {
                            value = _headers._ProxyAuthorization;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.CorrelationContext.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100000L) != 0)
                        {
                            value = _headers._CorrelationContext;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.IfUnmodifiedSince.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80000000L) != 0)
                        {
                            value = _headers._IfUnmodifiedSince;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ProxyAuthorization.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000000000L) != 0)
                        {
                            value = _headers._ProxyAuthorization;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 20:
                {
                    if (ReferenceEquals(HeaderNames.GrpcAcceptEncoding, key))
                    {
                        if ((_bits & 0x1000000L) != 0)
                        {
                            value = _headers._GrpcAcceptEncoding;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.GrpcAcceptEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000000L) != 0)
                        {
                            value = _headers._GrpcAcceptEncoding;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 25:
                {
                    if (ReferenceEquals(HeaderNames.UpgradeInsecureRequests, key))
                    {
                        if ((_bits & 0x400000000000L) != 0)
                        {
                            value = _headers._UpgradeInsecureRequests;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.UpgradeInsecureRequests.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400000000000L) != 0)
                        {
                            value = _headers._UpgradeInsecureRequests;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 29:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlRequestMethod, key))
                    {
                        if ((_bits & 0x2000L) != 0)
                        {
                            value = _headers._AccessControlRequestMethod;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.AccessControlRequestMethod.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000L) != 0)
                        {
                            value = _headers._AccessControlRequestMethod;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 30:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlRequestHeaders, key))
                    {
                        if ((_bits & 0x1000L) != 0)
                        {
                            value = _headers._AccessControlRequestHeaders;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.AccessControlRequestHeaders.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000L) != 0)
                        {
                            value = _headers._AccessControlRequestHeaders;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
            }

            return TryGetUnknown(key, ref value);
        }

        protected override void SetValueFast(string key, StringValues value)
        {
            switch (key.Length)
            {
                case 2:
                {
                    if (ReferenceEquals(HeaderNames.TE, key))
                    {
                        _bits |= 0x10000000000L;
                        _headers._TE = value;
                        return;
                    }

                    if (HeaderNames.TE.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x10000000000L;
                        _headers._TE = value;
                        return;
                    }
                    break;
                }
                case 3:
                {
                    if (ReferenceEquals(HeaderNames.Via, key))
                    {
                        _bits |= 0x800000000000L;
                        _headers._Via = value;
                        return;
                    }

                    if (HeaderNames.Via.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x800000000000L;
                        _headers._Via = value;
                        return;
                    }
                    break;
                }
                case 4:
                {
                    if (ReferenceEquals(HeaderNames.Host, key))
                    {
                        _bits |= 0x4L;
                        _headers._Host = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Date, key))
                    {
                        _bits |= 0x200000L;
                        _headers._Date = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.From, key))
                    {
                        _bits |= 0x800000L;
                        _headers._From = value;
                        return;
                    }

                    if (HeaderNames.Host.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x4L;
                        _headers._Host = value;
                        return;
                    }
                    if (HeaderNames.Date.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x200000L;
                        _headers._Date = value;
                        return;
                    }
                    if (HeaderNames.From.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x800000L;
                        _headers._From = value;
                        return;
                    }
                    break;
                }
                case 5:
                {
                    if (ReferenceEquals(InternalHeaderNames.Path, key))
                    {
                        _bits |= 0x40L;
                        _headers._Path = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Range, key))
                    {
                        _bits |= 0x2000000000L;
                        _headers._Range = value;
                        return;
                    }

                    if (InternalHeaderNames.Path.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x40L;
                        _headers._Path = value;
                        return;
                    }
                    if (HeaderNames.Range.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x2000000000L;
                        _headers._Range = value;
                        return;
                    }
                    break;
                }
                case 6:
                {
                    if (ReferenceEquals(HeaderNames.Accept, key))
                    {
                        _bits |= 0x1L;
                        _headers._Accept = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Cookie, key))
                    {
                        _bits |= 0x80000L;
                        _headers._Cookie = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Expect, key))
                    {
                        _bits |= 0x400000L;
                        _headers._Expect = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Origin, key))
                    {
                        _bits |= 0x400000000L;
                        _headers._Origin = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Pragma, key))
                    {
                        _bits |= 0x800000000L;
                        _headers._Pragma = value;
                        return;
                    }

                    if (HeaderNames.Accept.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x1L;
                        _headers._Accept = value;
                        return;
                    }
                    if (HeaderNames.Cookie.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x80000L;
                        _headers._Cookie = value;
                        return;
                    }
                    if (HeaderNames.Expect.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x400000L;
                        _headers._Expect = value;
                        return;
                    }
                    if (HeaderNames.Origin.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x400000000L;
                        _headers._Origin = value;
                        return;
                    }
                    if (HeaderNames.Pragma.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x800000000L;
                        _headers._Pragma = value;
                        return;
                    }
                    break;
                }
                case 7:
                {
                    if (ReferenceEquals(InternalHeaderNames.Method, key))
                    {
                        _bits |= 0x20L;
                        _headers._Method = value;
                        return;
                    }
                    if (ReferenceEquals(InternalHeaderNames.Scheme, key))
                    {
                        _bits |= 0x100L;
                        _headers._Scheme = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Baggage, key))
                    {
                        _bits |= 0x10000L;
                        _headers._Baggage = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Referer, key))
                    {
                        _bits |= 0x4000000000L;
                        _headers._Referer = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Upgrade, key))
                    {
                        _bits |= 0x200000000000L;
                        _headers._Upgrade = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Warning, key))
                    {
                        _bits |= 0x1000000000000L;
                        _headers._Warning = value;
                        return;
                    }

                    if (InternalHeaderNames.Method.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x20L;
                        _headers._Method = value;
                        return;
                    }
                    if (InternalHeaderNames.Scheme.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x100L;
                        _headers._Scheme = value;
                        return;
                    }
                    if (HeaderNames.Baggage.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x10000L;
                        _headers._Baggage = value;
                        return;
                    }
                    if (HeaderNames.Referer.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x4000000000L;
                        _headers._Referer = value;
                        return;
                    }
                    if (HeaderNames.Upgrade.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x200000000000L;
                        _headers._Upgrade = value;
                        return;
                    }
                    if (HeaderNames.Warning.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x1000000000000L;
                        _headers._Warning = value;
                        return;
                    }
                    break;
                }
                case 8:
                {
                    if (ReferenceEquals(InternalHeaderNames.AltUsed, key))
                    {
                        _bits |= 0x4000L;
                        _headers._AltUsed = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.IfMatch, key))
                    {
                        _bits |= 0x8000000L;
                        _headers._IfMatch = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.IfRange, key))
                    {
                        _bits |= 0x40000000L;
                        _headers._IfRange = value;
                        return;
                    }

                    if (InternalHeaderNames.AltUsed.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x4000L;
                        _headers._AltUsed = value;
                        return;
                    }
                    if (HeaderNames.IfMatch.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x8000000L;
                        _headers._IfMatch = value;
                        return;
                    }
                    if (HeaderNames.IfRange.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x40000000L;
                        _headers._IfRange = value;
                        return;
                    }
                    break;
                }
                case 9:
                {
                    if (ReferenceEquals(InternalHeaderNames.Protocol, key))
                    {
                        _bits |= 0x80L;
                        _headers._Protocol = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Translate, key))
                    {
                        _bits |= 0x100000000000L;
                        _headers._Translate = value;
                        return;
                    }

                    if (InternalHeaderNames.Protocol.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x80L;
                        _headers._Protocol = value;
                        return;
                    }
                    if (HeaderNames.Translate.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x100000000000L;
                        _headers._Translate = value;
                        return;
                    }
                    break;
                }
                case 10:
                {
                    if (ReferenceEquals(HeaderNames.Connection, key))
                    {
                        _bits |= 0x2L;
                        _headers._Connection = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.UserAgent, key))
                    {
                        _bits |= 0x8L;
                        _headers._UserAgent = value;
                        return;
                    }
                    if (ReferenceEquals(InternalHeaderNames.Authority, key))
                    {
                        _bits |= 0x10L;
                        _headers._Authority = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.KeepAlive, key))
                    {
                        _bits |= 0x100000000L;
                        _headers._KeepAlive = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.RequestId, key))
                    {
                        _bits |= 0x8000000000L;
                        _headers._RequestId = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.TraceState, key))
                    {
                        _bits |= 0x40000000000L;
                        _headers._TraceState = value;
                        return;
                    }

                    if (HeaderNames.Connection.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x2L;
                        _headers._Connection = value;
                        return;
                    }
                    if (HeaderNames.UserAgent.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x8L;
                        _headers._UserAgent = value;
                        return;
                    }
                    if (InternalHeaderNames.Authority.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x10L;
                        _headers._Authority = value;
                        return;
                    }
                    if (HeaderNames.KeepAlive.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x100000000L;
                        _headers._KeepAlive = value;
                        return;
                    }
                    if (HeaderNames.RequestId.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x8000000000L;
                        _headers._RequestId = value;
                        return;
                    }
                    if (HeaderNames.TraceState.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x40000000000L;
                        _headers._TraceState = value;
                        return;
                    }
                    break;
                }
                case 11:
                {
                    if (ReferenceEquals(HeaderNames.TraceParent, key))
                    {
                        _bits |= 0x20000000000L;
                        _headers._TraceParent = value;
                        return;
                    }

                    if (HeaderNames.TraceParent.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x20000000000L;
                        _headers._TraceParent = value;
                        return;
                    }
                    break;
                }
                case 12:
                {
                    if (ReferenceEquals(HeaderNames.ContentType, key))
                    {
                        _bits |= 0x40000L;
                        _headers._ContentType = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.GrpcTimeout, key))
                    {
                        _bits |= 0x4000000L;
                        _headers._GrpcTimeout = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.MaxForwards, key))
                    {
                        _bits |= 0x200000000L;
                        _headers._MaxForwards = value;
                        return;
                    }

                    if (HeaderNames.ContentType.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x40000L;
                        _headers._ContentType = value;
                        return;
                    }
                    if (HeaderNames.GrpcTimeout.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x4000000L;
                        _headers._GrpcTimeout = value;
                        return;
                    }
                    if (HeaderNames.MaxForwards.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x200000000L;
                        _headers._MaxForwards = value;
                        return;
                    }
                    break;
                }
                case 13:
                {
                    if (ReferenceEquals(HeaderNames.Authorization, key))
                    {
                        _bits |= 0x8000L;
                        _headers._Authorization = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.CacheControl, key))
                    {
                        _bits |= 0x20000L;
                        _headers._CacheControl = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.GrpcEncoding, key))
                    {
                        _bits |= 0x2000000L;
                        _headers._GrpcEncoding = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.IfNoneMatch, key))
                    {
                        _bits |= 0x20000000L;
                        _headers._IfNoneMatch = value;
                        return;
                    }

                    if (HeaderNames.Authorization.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x8000L;
                        _headers._Authorization = value;
                        return;
                    }
                    if (HeaderNames.CacheControl.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x20000L;
                        _headers._CacheControl = value;
                        return;
                    }
                    if (HeaderNames.GrpcEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x2000000L;
                        _headers._GrpcEncoding = value;
                        return;
                    }
                    if (HeaderNames.IfNoneMatch.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x20000000L;
                        _headers._IfNoneMatch = value;
                        return;
                    }
                    break;
                }
                case 14:
                {
                    if (ReferenceEquals(HeaderNames.AcceptCharset, key))
                    {
                        _bits |= 0x200L;
                        _headers._AcceptCharset = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.ContentLength, key))
                    {
                        _contentLength = ParseContentLength(value.ToString());
                        return;
                    }

                    if (HeaderNames.AcceptCharset.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x200L;
                        _headers._AcceptCharset = value;
                        return;
                    }
                    if (HeaderNames.ContentLength.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _contentLength = ParseContentLength(value.ToString());
                        return;
                    }
                    break;
                }
                case 15:
                {
                    if (ReferenceEquals(HeaderNames.AcceptEncoding, key))
                    {
                        _bits |= 0x400L;
                        _headers._AcceptEncoding = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.AcceptLanguage, key))
                    {
                        _bits |= 0x800L;
                        _headers._AcceptLanguage = value;
                        return;
                    }

                    if (HeaderNames.AcceptEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x400L;
                        _headers._AcceptEncoding = value;
                        return;
                    }
                    if (HeaderNames.AcceptLanguage.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x800L;
                        _headers._AcceptLanguage = value;
                        return;
                    }
                    break;
                }
                case 17:
                {
                    if (ReferenceEquals(HeaderNames.IfModifiedSince, key))
                    {
                        _bits |= 0x10000000L;
                        _headers._IfModifiedSince = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.TransferEncoding, key))
                    {
                        _bits |= 0x80000000000L;
                        _headers._TransferEncoding = value;
                        return;
                    }

                    if (HeaderNames.IfModifiedSince.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x10000000L;
                        _headers._IfModifiedSince = value;
                        return;
                    }
                    if (HeaderNames.TransferEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x80000000000L;
                        _headers._TransferEncoding = value;
                        return;
                    }
                    break;
                }
                case 19:
                {
                    if (ReferenceEquals(HeaderNames.CorrelationContext, key))
                    {
                        _bits |= 0x100000L;
                        _headers._CorrelationContext = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.IfUnmodifiedSince, key))
                    {
                        _bits |= 0x80000000L;
                        _headers._IfUnmodifiedSince = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.ProxyAuthorization, key))
                    {
                        _bits |= 0x1000000000L;
                        _headers._ProxyAuthorization = value;
                        return;
                    }

                    if (HeaderNames.CorrelationContext.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x100000L;
                        _headers._CorrelationContext = value;
                        return;
                    }
                    if (HeaderNames.IfUnmodifiedSince.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x80000000L;
                        _headers._IfUnmodifiedSince = value;
                        return;
                    }
                    if (HeaderNames.ProxyAuthorization.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x1000000000L;
                        _headers._ProxyAuthorization = value;
                        return;
                    }
                    break;
                }
                case 20:
                {
                    if (ReferenceEquals(HeaderNames.GrpcAcceptEncoding, key))
                    {
                        _bits |= 0x1000000L;
                        _headers._GrpcAcceptEncoding = value;
                        return;
                    }

                    if (HeaderNames.GrpcAcceptEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x1000000L;
                        _headers._GrpcAcceptEncoding = value;
                        return;
                    }
                    break;
                }
                case 25:
                {
                    if (ReferenceEquals(HeaderNames.UpgradeInsecureRequests, key))
                    {
                        _bits |= 0x400000000000L;
                        _headers._UpgradeInsecureRequests = value;
                        return;
                    }

                    if (HeaderNames.UpgradeInsecureRequests.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x400000000000L;
                        _headers._UpgradeInsecureRequests = value;
                        return;
                    }
                    break;
                }
                case 29:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlRequestMethod, key))
                    {
                        _bits |= 0x2000L;
                        _headers._AccessControlRequestMethod = value;
                        return;
                    }

                    if (HeaderNames.AccessControlRequestMethod.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x2000L;
                        _headers._AccessControlRequestMethod = value;
                        return;
                    }
                    break;
                }
                case 30:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlRequestHeaders, key))
                    {
                        _bits |= 0x1000L;
                        _headers._AccessControlRequestHeaders = value;
                        return;
                    }

                    if (HeaderNames.AccessControlRequestHeaders.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x1000L;
                        _headers._AccessControlRequestHeaders = value;
                        return;
                    }
                    break;
                }
            }

            SetValueUnknown(key, value);
        }

        protected override bool AddValueFast(string key, StringValues value)
        {
            switch (key.Length)
            {
                case 2:
                {
                    if (ReferenceEquals(HeaderNames.TE, key))
                    {
                        if ((_bits & 0x10000000000L) == 0)
                        {
                            _bits |= 0x10000000000L;
                            _headers._TE = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.TE.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000000000L) == 0)
                        {
                            _bits |= 0x10000000000L;
                            _headers._TE = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 3:
                {
                    if (ReferenceEquals(HeaderNames.Via, key))
                    {
                        if ((_bits & 0x800000000000L) == 0)
                        {
                            _bits |= 0x800000000000L;
                            _headers._Via = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Via.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800000000000L) == 0)
                        {
                            _bits |= 0x800000000000L;
                            _headers._Via = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 4:
                {
                    if (ReferenceEquals(HeaderNames.Host, key))
                    {
                        if ((_bits & 0x4L) == 0)
                        {
                            _bits |= 0x4L;
                            _headers._Host = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Date, key))
                    {
                        if ((_bits & 0x200000L) == 0)
                        {
                            _bits |= 0x200000L;
                            _headers._Date = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.From, key))
                    {
                        if ((_bits & 0x800000L) == 0)
                        {
                            _bits |= 0x800000L;
                            _headers._From = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Host.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4L) == 0)
                        {
                            _bits |= 0x4L;
                            _headers._Host = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Date.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200000L) == 0)
                        {
                            _bits |= 0x200000L;
                            _headers._Date = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.From.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800000L) == 0)
                        {
                            _bits |= 0x800000L;
                            _headers._From = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 5:
                {
                    if (ReferenceEquals(InternalHeaderNames.Path, key))
                    {
                        if ((_bits & 0x40L) == 0)
                        {
                            _bits |= 0x40L;
                            _headers._Path = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Range, key))
                    {
                        if ((_bits & 0x2000000000L) == 0)
                        {
                            _bits |= 0x2000000000L;
                            _headers._Range = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (InternalHeaderNames.Path.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40L) == 0)
                        {
                            _bits |= 0x40L;
                            _headers._Path = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Range.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000000000L) == 0)
                        {
                            _bits |= 0x2000000000L;
                            _headers._Range = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 6:
                {
                    if (ReferenceEquals(HeaderNames.Accept, key))
                    {
                        if ((_bits & 0x1L) == 0)
                        {
                            _bits |= 0x1L;
                            _headers._Accept = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Cookie, key))
                    {
                        if ((_bits & 0x80000L) == 0)
                        {
                            _bits |= 0x80000L;
                            _headers._Cookie = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Expect, key))
                    {
                        if ((_bits & 0x400000L) == 0)
                        {
                            _bits |= 0x400000L;
                            _headers._Expect = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Origin, key))
                    {
                        if ((_bits & 0x400000000L) == 0)
                        {
                            _bits |= 0x400000000L;
                            _headers._Origin = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Pragma, key))
                    {
                        if ((_bits & 0x800000000L) == 0)
                        {
                            _bits |= 0x800000000L;
                            _headers._Pragma = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Accept.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1L) == 0)
                        {
                            _bits |= 0x1L;
                            _headers._Accept = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Cookie.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80000L) == 0)
                        {
                            _bits |= 0x80000L;
                            _headers._Cookie = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Expect.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400000L) == 0)
                        {
                            _bits |= 0x400000L;
                            _headers._Expect = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Origin.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400000000L) == 0)
                        {
                            _bits |= 0x400000000L;
                            _headers._Origin = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Pragma.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800000000L) == 0)
                        {
                            _bits |= 0x800000000L;
                            _headers._Pragma = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 7:
                {
                    if (ReferenceEquals(InternalHeaderNames.Method, key))
                    {
                        if ((_bits & 0x20L) == 0)
                        {
                            _bits |= 0x20L;
                            _headers._Method = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(InternalHeaderNames.Scheme, key))
                    {
                        if ((_bits & 0x100L) == 0)
                        {
                            _bits |= 0x100L;
                            _headers._Scheme = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Baggage, key))
                    {
                        if ((_bits & 0x10000L) == 0)
                        {
                            _bits |= 0x10000L;
                            _headers._Baggage = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Referer, key))
                    {
                        if ((_bits & 0x4000000000L) == 0)
                        {
                            _bits |= 0x4000000000L;
                            _headers._Referer = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Upgrade, key))
                    {
                        if ((_bits & 0x200000000000L) == 0)
                        {
                            _bits |= 0x200000000000L;
                            _headers._Upgrade = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Warning, key))
                    {
                        if ((_bits & 0x1000000000000L) == 0)
                        {
                            _bits |= 0x1000000000000L;
                            _headers._Warning = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (InternalHeaderNames.Method.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20L) == 0)
                        {
                            _bits |= 0x20L;
                            _headers._Method = value;
                            return true;
                        }
                        return false;
                    }
                    if (InternalHeaderNames.Scheme.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100L) == 0)
                        {
                            _bits |= 0x100L;
                            _headers._Scheme = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Baggage.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000L) == 0)
                        {
                            _bits |= 0x10000L;
                            _headers._Baggage = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Referer.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000000000L) == 0)
                        {
                            _bits |= 0x4000000000L;
                            _headers._Referer = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Upgrade.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200000000000L) == 0)
                        {
                            _bits |= 0x200000000000L;
                            _headers._Upgrade = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Warning.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000000000000L) == 0)
                        {
                            _bits |= 0x1000000000000L;
                            _headers._Warning = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 8:
                {
                    if (ReferenceEquals(InternalHeaderNames.AltUsed, key))
                    {
                        if ((_bits & 0x4000L) == 0)
                        {
                            _bits |= 0x4000L;
                            _headers._AltUsed = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.IfMatch, key))
                    {
                        if ((_bits & 0x8000000L) == 0)
                        {
                            _bits |= 0x8000000L;
                            _headers._IfMatch = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.IfRange, key))
                    {
                        if ((_bits & 0x40000000L) == 0)
                        {
                            _bits |= 0x40000000L;
                            _headers._IfRange = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (InternalHeaderNames.AltUsed.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000L) == 0)
                        {
                            _bits |= 0x4000L;
                            _headers._AltUsed = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.IfMatch.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000000L) == 0)
                        {
                            _bits |= 0x8000000L;
                            _headers._IfMatch = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.IfRange.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40000000L) == 0)
                        {
                            _bits |= 0x40000000L;
                            _headers._IfRange = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 9:
                {
                    if (ReferenceEquals(InternalHeaderNames.Protocol, key))
                    {
                        if ((_bits & 0x80L) == 0)
                        {
                            _bits |= 0x80L;
                            _headers._Protocol = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Translate, key))
                    {
                        if ((_bits & 0x100000000000L) == 0)
                        {
                            _bits |= 0x100000000000L;
                            _headers._Translate = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (InternalHeaderNames.Protocol.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80L) == 0)
                        {
                            _bits |= 0x80L;
                            _headers._Protocol = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Translate.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100000000000L) == 0)
                        {
                            _bits |= 0x100000000000L;
                            _headers._Translate = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 10:
                {
                    if (ReferenceEquals(HeaderNames.Connection, key))
                    {
                        if ((_bits & 0x2L) == 0)
                        {
                            _bits |= 0x2L;
                            _headers._Connection = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.UserAgent, key))
                    {
                        if ((_bits & 0x8L) == 0)
                        {
                            _bits |= 0x8L;
                            _headers._UserAgent = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(InternalHeaderNames.Authority, key))
                    {
                        if ((_bits & 0x10L) == 0)
                        {
                            _bits |= 0x10L;
                            _headers._Authority = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.KeepAlive, key))
                    {
                        if ((_bits & 0x100000000L) == 0)
                        {
                            _bits |= 0x100000000L;
                            _headers._KeepAlive = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.RequestId, key))
                    {
                        if ((_bits & 0x8000000000L) == 0)
                        {
                            _bits |= 0x8000000000L;
                            _headers._RequestId = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.TraceState, key))
                    {
                        if ((_bits & 0x40000000000L) == 0)
                        {
                            _bits |= 0x40000000000L;
                            _headers._TraceState = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Connection.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2L) == 0)
                        {
                            _bits |= 0x2L;
                            _headers._Connection = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.UserAgent.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8L) == 0)
                        {
                            _bits |= 0x8L;
                            _headers._UserAgent = value;
                            return true;
                        }
                        return false;
                    }
                    if (InternalHeaderNames.Authority.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10L) == 0)
                        {
                            _bits |= 0x10L;
                            _headers._Authority = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.KeepAlive.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100000000L) == 0)
                        {
                            _bits |= 0x100000000L;
                            _headers._KeepAlive = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.RequestId.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000000000L) == 0)
                        {
                            _bits |= 0x8000000000L;
                            _headers._RequestId = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.TraceState.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40000000000L) == 0)
                        {
                            _bits |= 0x40000000000L;
                            _headers._TraceState = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 11:
                {
                    if (ReferenceEquals(HeaderNames.TraceParent, key))
                    {
                        if ((_bits & 0x20000000000L) == 0)
                        {
                            _bits |= 0x20000000000L;
                            _headers._TraceParent = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.TraceParent.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20000000000L) == 0)
                        {
                            _bits |= 0x20000000000L;
                            _headers._TraceParent = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 12:
                {
                    if (ReferenceEquals(HeaderNames.ContentType, key))
                    {
                        if ((_bits & 0x40000L) == 0)
                        {
                            _bits |= 0x40000L;
                            _headers._ContentType = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.GrpcTimeout, key))
                    {
                        if ((_bits & 0x4000000L) == 0)
                        {
                            _bits |= 0x4000000L;
                            _headers._GrpcTimeout = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.MaxForwards, key))
                    {
                        if ((_bits & 0x200000000L) == 0)
                        {
                            _bits |= 0x200000000L;
                            _headers._MaxForwards = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.ContentType.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40000L) == 0)
                        {
                            _bits |= 0x40000L;
                            _headers._ContentType = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.GrpcTimeout.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000000L) == 0)
                        {
                            _bits |= 0x4000000L;
                            _headers._GrpcTimeout = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.MaxForwards.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200000000L) == 0)
                        {
                            _bits |= 0x200000000L;
                            _headers._MaxForwards = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 13:
                {
                    if (ReferenceEquals(HeaderNames.Authorization, key))
                    {
                        if ((_bits & 0x8000L) == 0)
                        {
                            _bits |= 0x8000L;
                            _headers._Authorization = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.CacheControl, key))
                    {
                        if ((_bits & 0x20000L) == 0)
                        {
                            _bits |= 0x20000L;
                            _headers._CacheControl = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.GrpcEncoding, key))
                    {
                        if ((_bits & 0x2000000L) == 0)
                        {
                            _bits |= 0x2000000L;
                            _headers._GrpcEncoding = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.IfNoneMatch, key))
                    {
                        if ((_bits & 0x20000000L) == 0)
                        {
                            _bits |= 0x20000000L;
                            _headers._IfNoneMatch = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Authorization.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000L) == 0)
                        {
                            _bits |= 0x8000L;
                            _headers._Authorization = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.CacheControl.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20000L) == 0)
                        {
                            _bits |= 0x20000L;
                            _headers._CacheControl = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.GrpcEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000000L) == 0)
                        {
                            _bits |= 0x2000000L;
                            _headers._GrpcEncoding = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.IfNoneMatch.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20000000L) == 0)
                        {
                            _bits |= 0x20000000L;
                            _headers._IfNoneMatch = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 14:
                {
                    if (ReferenceEquals(HeaderNames.AcceptCharset, key))
                    {
                        if ((_bits & 0x200L) == 0)
                        {
                            _bits |= 0x200L;
                            _headers._AcceptCharset = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentLength, key))
                    {
                        if (!_contentLength.HasValue)
                        {
                            _contentLength = ParseContentLength(value.ToString());
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AcceptCharset.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200L) == 0)
                        {
                            _bits |= 0x200L;
                            _headers._AcceptCharset = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentLength.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!_contentLength.HasValue)
                        {
                            _contentLength = ParseContentLength(value.ToString());
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 15:
                {
                    if (ReferenceEquals(HeaderNames.AcceptEncoding, key))
                    {
                        if ((_bits & 0x400L) == 0)
                        {
                            _bits |= 0x400L;
                            _headers._AcceptEncoding = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.AcceptLanguage, key))
                    {
                        if ((_bits & 0x800L) == 0)
                        {
                            _bits |= 0x800L;
                            _headers._AcceptLanguage = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AcceptEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400L) == 0)
                        {
                            _bits |= 0x400L;
                            _headers._AcceptEncoding = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.AcceptLanguage.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800L) == 0)
                        {
                            _bits |= 0x800L;
                            _headers._AcceptLanguage = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 17:
                {
                    if (ReferenceEquals(HeaderNames.IfModifiedSince, key))
                    {
                        if ((_bits & 0x10000000L) == 0)
                        {
                            _bits |= 0x10000000L;
                            _headers._IfModifiedSince = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.TransferEncoding, key))
                    {
                        if ((_bits & 0x80000000000L) == 0)
                        {
                            _bits |= 0x80000000000L;
                            _headers._TransferEncoding = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.IfModifiedSince.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000000L) == 0)
                        {
                            _bits |= 0x10000000L;
                            _headers._IfModifiedSince = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.TransferEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80000000000L) == 0)
                        {
                            _bits |= 0x80000000000L;
                            _headers._TransferEncoding = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 19:
                {
                    if (ReferenceEquals(HeaderNames.CorrelationContext, key))
                    {
                        if ((_bits & 0x100000L) == 0)
                        {
                            _bits |= 0x100000L;
                            _headers._CorrelationContext = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.IfUnmodifiedSince, key))
                    {
                        if ((_bits & 0x80000000L) == 0)
                        {
                            _bits |= 0x80000000L;
                            _headers._IfUnmodifiedSince = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ProxyAuthorization, key))
                    {
                        if ((_bits & 0x1000000000L) == 0)
                        {
                            _bits |= 0x1000000000L;
                            _headers._ProxyAuthorization = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.CorrelationContext.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100000L) == 0)
                        {
                            _bits |= 0x100000L;
                            _headers._CorrelationContext = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.IfUnmodifiedSince.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80000000L) == 0)
                        {
                            _bits |= 0x80000000L;
                            _headers._IfUnmodifiedSince = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ProxyAuthorization.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000000000L) == 0)
                        {
                            _bits |= 0x1000000000L;
                            _headers._ProxyAuthorization = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 20:
                {
                    if (ReferenceEquals(HeaderNames.GrpcAcceptEncoding, key))
                    {
                        if ((_bits & 0x1000000L) == 0)
                        {
                            _bits |= 0x1000000L;
                            _headers._GrpcAcceptEncoding = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.GrpcAcceptEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000000L) == 0)
                        {
                            _bits |= 0x1000000L;
                            _headers._GrpcAcceptEncoding = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 25:
                {
                    if (ReferenceEquals(HeaderNames.UpgradeInsecureRequests, key))
                    {
                        if ((_bits & 0x400000000000L) == 0)
                        {
                            _bits |= 0x400000000000L;
                            _headers._UpgradeInsecureRequests = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.UpgradeInsecureRequests.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400000000000L) == 0)
                        {
                            _bits |= 0x400000000000L;
                            _headers._UpgradeInsecureRequests = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 29:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlRequestMethod, key))
                    {
                        if ((_bits & 0x2000L) == 0)
                        {
                            _bits |= 0x2000L;
                            _headers._AccessControlRequestMethod = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AccessControlRequestMethod.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000L) == 0)
                        {
                            _bits |= 0x2000L;
                            _headers._AccessControlRequestMethod = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 30:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlRequestHeaders, key))
                    {
                        if ((_bits & 0x1000L) == 0)
                        {
                            _bits |= 0x1000L;
                            _headers._AccessControlRequestHeaders = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AccessControlRequestHeaders.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000L) == 0)
                        {
                            _bits |= 0x1000L;
                            _headers._AccessControlRequestHeaders = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
            }

            return AddValueUnknown(key, value);
        }

        protected override bool RemoveFast(string key)
        {
            switch (key.Length)
            {
                case 2:
                {
                    if (ReferenceEquals(HeaderNames.TE, key))
                    {
                        if ((_bits & 0x10000000000L) != 0)
                        {
                            _bits &= ~0x10000000000L;
                            _headers._TE = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.TE.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000000000L) != 0)
                        {
                            _bits &= ~0x10000000000L;
                            _headers._TE = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 3:
                {
                    if (ReferenceEquals(HeaderNames.Via, key))
                    {
                        if ((_bits & 0x800000000000L) != 0)
                        {
                            _bits &= ~0x800000000000L;
                            _headers._Via = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Via.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800000000000L) != 0)
                        {
                            _bits &= ~0x800000000000L;
                            _headers._Via = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 4:
                {
                    if (ReferenceEquals(HeaderNames.Host, key))
                    {
                        if ((_bits & 0x4L) != 0)
                        {
                            _bits &= ~0x4L;
                            _headers._Host = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Date, key))
                    {
                        if ((_bits & 0x200000L) != 0)
                        {
                            _bits &= ~0x200000L;
                            _headers._Date = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.From, key))
                    {
                        if ((_bits & 0x800000L) != 0)
                        {
                            _bits &= ~0x800000L;
                            _headers._From = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Host.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4L) != 0)
                        {
                            _bits &= ~0x4L;
                            _headers._Host = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Date.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200000L) != 0)
                        {
                            _bits &= ~0x200000L;
                            _headers._Date = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.From.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800000L) != 0)
                        {
                            _bits &= ~0x800000L;
                            _headers._From = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 5:
                {
                    if (ReferenceEquals(InternalHeaderNames.Path, key))
                    {
                        if ((_bits & 0x40L) != 0)
                        {
                            _bits &= ~0x40L;
                            _headers._Path = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Range, key))
                    {
                        if ((_bits & 0x2000000000L) != 0)
                        {
                            _bits &= ~0x2000000000L;
                            _headers._Range = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (InternalHeaderNames.Path.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40L) != 0)
                        {
                            _bits &= ~0x40L;
                            _headers._Path = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Range.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000000000L) != 0)
                        {
                            _bits &= ~0x2000000000L;
                            _headers._Range = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 6:
                {
                    if (ReferenceEquals(HeaderNames.Accept, key))
                    {
                        if ((_bits & 0x1L) != 0)
                        {
                            _bits &= ~0x1L;
                            _headers._Accept = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Cookie, key))
                    {
                        if ((_bits & 0x80000L) != 0)
                        {
                            _bits &= ~0x80000L;
                            _headers._Cookie = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Expect, key))
                    {
                        if ((_bits & 0x400000L) != 0)
                        {
                            _bits &= ~0x400000L;
                            _headers._Expect = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Origin, key))
                    {
                        if ((_bits & 0x400000000L) != 0)
                        {
                            _bits &= ~0x400000000L;
                            _headers._Origin = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Pragma, key))
                    {
                        if ((_bits & 0x800000000L) != 0)
                        {
                            _bits &= ~0x800000000L;
                            _headers._Pragma = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Accept.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1L) != 0)
                        {
                            _bits &= ~0x1L;
                            _headers._Accept = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Cookie.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80000L) != 0)
                        {
                            _bits &= ~0x80000L;
                            _headers._Cookie = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Expect.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400000L) != 0)
                        {
                            _bits &= ~0x400000L;
                            _headers._Expect = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Origin.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400000000L) != 0)
                        {
                            _bits &= ~0x400000000L;
                            _headers._Origin = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Pragma.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800000000L) != 0)
                        {
                            _bits &= ~0x800000000L;
                            _headers._Pragma = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 7:
                {
                    if (ReferenceEquals(InternalHeaderNames.Method, key))
                    {
                        if ((_bits & 0x20L) != 0)
                        {
                            _bits &= ~0x20L;
                            _headers._Method = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(InternalHeaderNames.Scheme, key))
                    {
                        if ((_bits & 0x100L) != 0)
                        {
                            _bits &= ~0x100L;
                            _headers._Scheme = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Baggage, key))
                    {
                        if ((_bits & 0x10000L) != 0)
                        {
                            _bits &= ~0x10000L;
                            _headers._Baggage = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Referer, key))
                    {
                        if ((_bits & 0x4000000000L) != 0)
                        {
                            _bits &= ~0x4000000000L;
                            _headers._Referer = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Upgrade, key))
                    {
                        if ((_bits & 0x200000000000L) != 0)
                        {
                            _bits &= ~0x200000000000L;
                            _headers._Upgrade = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Warning, key))
                    {
                        if ((_bits & 0x1000000000000L) != 0)
                        {
                            _bits &= ~0x1000000000000L;
                            _headers._Warning = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (InternalHeaderNames.Method.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20L) != 0)
                        {
                            _bits &= ~0x20L;
                            _headers._Method = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (InternalHeaderNames.Scheme.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100L) != 0)
                        {
                            _bits &= ~0x100L;
                            _headers._Scheme = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Baggage.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000L) != 0)
                        {
                            _bits &= ~0x10000L;
                            _headers._Baggage = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Referer.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000000000L) != 0)
                        {
                            _bits &= ~0x4000000000L;
                            _headers._Referer = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Upgrade.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200000000000L) != 0)
                        {
                            _bits &= ~0x200000000000L;
                            _headers._Upgrade = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Warning.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000000000000L) != 0)
                        {
                            _bits &= ~0x1000000000000L;
                            _headers._Warning = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 8:
                {
                    if (ReferenceEquals(InternalHeaderNames.AltUsed, key))
                    {
                        if ((_bits & 0x4000L) != 0)
                        {
                            _bits &= ~0x4000L;
                            _headers._AltUsed = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.IfMatch, key))
                    {
                        if ((_bits & 0x8000000L) != 0)
                        {
                            _bits &= ~0x8000000L;
                            _headers._IfMatch = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.IfRange, key))
                    {
                        if ((_bits & 0x40000000L) != 0)
                        {
                            _bits &= ~0x40000000L;
                            _headers._IfRange = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (InternalHeaderNames.AltUsed.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000L) != 0)
                        {
                            _bits &= ~0x4000L;
                            _headers._AltUsed = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.IfMatch.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000000L) != 0)
                        {
                            _bits &= ~0x8000000L;
                            _headers._IfMatch = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.IfRange.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40000000L) != 0)
                        {
                            _bits &= ~0x40000000L;
                            _headers._IfRange = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 9:
                {
                    if (ReferenceEquals(InternalHeaderNames.Protocol, key))
                    {
                        if ((_bits & 0x80L) != 0)
                        {
                            _bits &= ~0x80L;
                            _headers._Protocol = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Translate, key))
                    {
                        if ((_bits & 0x100000000000L) != 0)
                        {
                            _bits &= ~0x100000000000L;
                            _headers._Translate = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (InternalHeaderNames.Protocol.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80L) != 0)
                        {
                            _bits &= ~0x80L;
                            _headers._Protocol = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Translate.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100000000000L) != 0)
                        {
                            _bits &= ~0x100000000000L;
                            _headers._Translate = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 10:
                {
                    if (ReferenceEquals(HeaderNames.Connection, key))
                    {
                        if ((_bits & 0x2L) != 0)
                        {
                            _bits &= ~0x2L;
                            _headers._Connection = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.UserAgent, key))
                    {
                        if ((_bits & 0x8L) != 0)
                        {
                            _bits &= ~0x8L;
                            _headers._UserAgent = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(InternalHeaderNames.Authority, key))
                    {
                        if ((_bits & 0x10L) != 0)
                        {
                            _bits &= ~0x10L;
                            _headers._Authority = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.KeepAlive, key))
                    {
                        if ((_bits & 0x100000000L) != 0)
                        {
                            _bits &= ~0x100000000L;
                            _headers._KeepAlive = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.RequestId, key))
                    {
                        if ((_bits & 0x8000000000L) != 0)
                        {
                            _bits &= ~0x8000000000L;
                            _headers._RequestId = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.TraceState, key))
                    {
                        if ((_bits & 0x40000000000L) != 0)
                        {
                            _bits &= ~0x40000000000L;
                            _headers._TraceState = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Connection.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2L) != 0)
                        {
                            _bits &= ~0x2L;
                            _headers._Connection = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.UserAgent.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8L) != 0)
                        {
                            _bits &= ~0x8L;
                            _headers._UserAgent = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (InternalHeaderNames.Authority.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10L) != 0)
                        {
                            _bits &= ~0x10L;
                            _headers._Authority = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.KeepAlive.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100000000L) != 0)
                        {
                            _bits &= ~0x100000000L;
                            _headers._KeepAlive = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.RequestId.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000000000L) != 0)
                        {
                            _bits &= ~0x8000000000L;
                            _headers._RequestId = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.TraceState.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40000000000L) != 0)
                        {
                            _bits &= ~0x40000000000L;
                            _headers._TraceState = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 11:
                {
                    if (ReferenceEquals(HeaderNames.TraceParent, key))
                    {
                        if ((_bits & 0x20000000000L) != 0)
                        {
                            _bits &= ~0x20000000000L;
                            _headers._TraceParent = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.TraceParent.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20000000000L) != 0)
                        {
                            _bits &= ~0x20000000000L;
                            _headers._TraceParent = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 12:
                {
                    if (ReferenceEquals(HeaderNames.ContentType, key))
                    {
                        if ((_bits & 0x40000L) != 0)
                        {
                            _bits &= ~0x40000L;
                            _headers._ContentType = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.GrpcTimeout, key))
                    {
                        if ((_bits & 0x4000000L) != 0)
                        {
                            _bits &= ~0x4000000L;
                            _headers._GrpcTimeout = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.MaxForwards, key))
                    {
                        if ((_bits & 0x200000000L) != 0)
                        {
                            _bits &= ~0x200000000L;
                            _headers._MaxForwards = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.ContentType.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40000L) != 0)
                        {
                            _bits &= ~0x40000L;
                            _headers._ContentType = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.GrpcTimeout.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000000L) != 0)
                        {
                            _bits &= ~0x4000000L;
                            _headers._GrpcTimeout = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.MaxForwards.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200000000L) != 0)
                        {
                            _bits &= ~0x200000000L;
                            _headers._MaxForwards = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 13:
                {
                    if (ReferenceEquals(HeaderNames.Authorization, key))
                    {
                        if ((_bits & 0x8000L) != 0)
                        {
                            _bits &= ~0x8000L;
                            _headers._Authorization = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.CacheControl, key))
                    {
                        if ((_bits & 0x20000L) != 0)
                        {
                            _bits &= ~0x20000L;
                            _headers._CacheControl = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.GrpcEncoding, key))
                    {
                        if ((_bits & 0x2000000L) != 0)
                        {
                            _bits &= ~0x2000000L;
                            _headers._GrpcEncoding = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.IfNoneMatch, key))
                    {
                        if ((_bits & 0x20000000L) != 0)
                        {
                            _bits &= ~0x20000000L;
                            _headers._IfNoneMatch = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Authorization.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000L) != 0)
                        {
                            _bits &= ~0x8000L;
                            _headers._Authorization = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.CacheControl.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20000L) != 0)
                        {
                            _bits &= ~0x20000L;
                            _headers._CacheControl = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.GrpcEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000000L) != 0)
                        {
                            _bits &= ~0x2000000L;
                            _headers._GrpcEncoding = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.IfNoneMatch.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20000000L) != 0)
                        {
                            _bits &= ~0x20000000L;
                            _headers._IfNoneMatch = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 14:
                {
                    if (ReferenceEquals(HeaderNames.AcceptCharset, key))
                    {
                        if ((_bits & 0x200L) != 0)
                        {
                            _bits &= ~0x200L;
                            _headers._AcceptCharset = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentLength, key))
                    {
                        if (_contentLength.HasValue)
                        {
                            _contentLength = null;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AcceptCharset.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200L) != 0)
                        {
                            _bits &= ~0x200L;
                            _headers._AcceptCharset = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentLength.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if (_contentLength.HasValue)
                        {
                            _contentLength = null;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 15:
                {
                    if (ReferenceEquals(HeaderNames.AcceptEncoding, key))
                    {
                        if ((_bits & 0x400L) != 0)
                        {
                            _bits &= ~0x400L;
                            _headers._AcceptEncoding = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.AcceptLanguage, key))
                    {
                        if ((_bits & 0x800L) != 0)
                        {
                            _bits &= ~0x800L;
                            _headers._AcceptLanguage = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AcceptEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400L) != 0)
                        {
                            _bits &= ~0x400L;
                            _headers._AcceptEncoding = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.AcceptLanguage.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800L) != 0)
                        {
                            _bits &= ~0x800L;
                            _headers._AcceptLanguage = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 17:
                {
                    if (ReferenceEquals(HeaderNames.IfModifiedSince, key))
                    {
                        if ((_bits & 0x10000000L) != 0)
                        {
                            _bits &= ~0x10000000L;
                            _headers._IfModifiedSince = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.TransferEncoding, key))
                    {
                        if ((_bits & 0x80000000000L) != 0)
                        {
                            _bits &= ~0x80000000000L;
                            _headers._TransferEncoding = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.IfModifiedSince.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000000L) != 0)
                        {
                            _bits &= ~0x10000000L;
                            _headers._IfModifiedSince = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.TransferEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80000000000L) != 0)
                        {
                            _bits &= ~0x80000000000L;
                            _headers._TransferEncoding = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 19:
                {
                    if (ReferenceEquals(HeaderNames.CorrelationContext, key))
                    {
                        if ((_bits & 0x100000L) != 0)
                        {
                            _bits &= ~0x100000L;
                            _headers._CorrelationContext = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.IfUnmodifiedSince, key))
                    {
                        if ((_bits & 0x80000000L) != 0)
                        {
                            _bits &= ~0x80000000L;
                            _headers._IfUnmodifiedSince = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ProxyAuthorization, key))
                    {
                        if ((_bits & 0x1000000000L) != 0)
                        {
                            _bits &= ~0x1000000000L;
                            _headers._ProxyAuthorization = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.CorrelationContext.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100000L) != 0)
                        {
                            _bits &= ~0x100000L;
                            _headers._CorrelationContext = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.IfUnmodifiedSince.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80000000L) != 0)
                        {
                            _bits &= ~0x80000000L;
                            _headers._IfUnmodifiedSince = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ProxyAuthorization.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000000000L) != 0)
                        {
                            _bits &= ~0x1000000000L;
                            _headers._ProxyAuthorization = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 20:
                {
                    if (ReferenceEquals(HeaderNames.GrpcAcceptEncoding, key))
                    {
                        if ((_bits & 0x1000000L) != 0)
                        {
                            _bits &= ~0x1000000L;
                            _headers._GrpcAcceptEncoding = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.GrpcAcceptEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000000L) != 0)
                        {
                            _bits &= ~0x1000000L;
                            _headers._GrpcAcceptEncoding = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 25:
                {
                    if (ReferenceEquals(HeaderNames.UpgradeInsecureRequests, key))
                    {
                        if ((_bits & 0x400000000000L) != 0)
                        {
                            _bits &= ~0x400000000000L;
                            _headers._UpgradeInsecureRequests = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.UpgradeInsecureRequests.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400000000000L) != 0)
                        {
                            _bits &= ~0x400000000000L;
                            _headers._UpgradeInsecureRequests = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 29:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlRequestMethod, key))
                    {
                        if ((_bits & 0x2000L) != 0)
                        {
                            _bits &= ~0x2000L;
                            _headers._AccessControlRequestMethod = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AccessControlRequestMethod.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000L) != 0)
                        {
                            _bits &= ~0x2000L;
                            _headers._AccessControlRequestMethod = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 30:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlRequestHeaders, key))
                    {
                        if ((_bits & 0x1000L) != 0)
                        {
                            _bits &= ~0x1000L;
                            _headers._AccessControlRequestHeaders = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AccessControlRequestHeaders.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000L) != 0)
                        {
                            _bits &= ~0x1000L;
                            _headers._AccessControlRequestHeaders = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
            }

            return RemoveUnknown(key);
        }
        private void Clear(long bitsToClear)
        {
            var tempBits = bitsToClear;
            
            if ((tempBits & 0x1L) != 0)
            {
                _headers._Accept = default;
                if((tempBits & ~0x1L) == 0)
                {
                    return;
                }
                tempBits &= ~0x1L;
            }
            
            if ((tempBits & 0x2L) != 0)
            {
                _headers._Connection = default;
                if((tempBits & ~0x2L) == 0)
                {
                    return;
                }
                tempBits &= ~0x2L;
            }
            
            if ((tempBits & 0x4L) != 0)
            {
                _headers._Host = default;
                if((tempBits & ~0x4L) == 0)
                {
                    return;
                }
                tempBits &= ~0x4L;
            }
            
            if ((tempBits & 0x8L) != 0)
            {
                _headers._UserAgent = default;
                if((tempBits & ~0x8L) == 0)
                {
                    return;
                }
                tempBits &= ~0x8L;
            }
            
            if ((tempBits & 0x10L) != 0)
            {
                _headers._Authority = default;
                if((tempBits & ~0x10L) == 0)
                {
                    return;
                }
                tempBits &= ~0x10L;
            }
            
            if ((tempBits & 0x20L) != 0)
            {
                _headers._Method = default;
                if((tempBits & ~0x20L) == 0)
                {
                    return;
                }
                tempBits &= ~0x20L;
            }
            
            if ((tempBits & 0x40L) != 0)
            {
                _headers._Path = default;
                if((tempBits & ~0x40L) == 0)
                {
                    return;
                }
                tempBits &= ~0x40L;
            }
            
            if ((tempBits & 0x80L) != 0)
            {
                _headers._Protocol = default;
                if((tempBits & ~0x80L) == 0)
                {
                    return;
                }
                tempBits &= ~0x80L;
            }
            
            if ((tempBits & 0x100L) != 0)
            {
                _headers._Scheme = default;
                if((tempBits & ~0x100L) == 0)
                {
                    return;
                }
                tempBits &= ~0x100L;
            }
            
            if ((tempBits & 0x200L) != 0)
            {
                _headers._AcceptCharset = default;
                if((tempBits & ~0x200L) == 0)
                {
                    return;
                }
                tempBits &= ~0x200L;
            }
            
            if ((tempBits & 0x400L) != 0)
            {
                _headers._AcceptEncoding = default;
                if((tempBits & ~0x400L) == 0)
                {
                    return;
                }
                tempBits &= ~0x400L;
            }
            
            if ((tempBits & 0x800L) != 0)
            {
                _headers._AcceptLanguage = default;
                if((tempBits & ~0x800L) == 0)
                {
                    return;
                }
                tempBits &= ~0x800L;
            }
            
            if ((tempBits & 0x1000L) != 0)
            {
                _headers._AccessControlRequestHeaders = default;
                if((tempBits & ~0x1000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x1000L;
            }
            
            if ((tempBits & 0x2000L) != 0)
            {
                _headers._AccessControlRequestMethod = default;
                if((tempBits & ~0x2000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x2000L;
            }
            
            if ((tempBits & 0x4000L) != 0)
            {
                _headers._AltUsed = default;
                if((tempBits & ~0x4000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x4000L;
            }
            
            if ((tempBits & 0x8000L) != 0)
            {
                _headers._Authorization = default;
                if((tempBits & ~0x8000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x8000L;
            }
            
            if ((tempBits & 0x10000L) != 0)
            {
                _headers._Baggage = default;
                if((tempBits & ~0x10000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x10000L;
            }
            
            if ((tempBits & 0x20000L) != 0)
            {
                _headers._CacheControl = default;
                if((tempBits & ~0x20000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x20000L;
            }
            
            if ((tempBits & 0x40000L) != 0)
            {
                _headers._ContentType = default;
                if((tempBits & ~0x40000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x40000L;
            }
            
            if ((tempBits & 0x80000L) != 0)
            {
                _headers._Cookie = default;
                if((tempBits & ~0x80000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x80000L;
            }
            
            if ((tempBits & 0x100000L) != 0)
            {
                _headers._CorrelationContext = default;
                if((tempBits & ~0x100000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x100000L;
            }
            
            if ((tempBits & 0x200000L) != 0)
            {
                _headers._Date = default;
                if((tempBits & ~0x200000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x200000L;
            }
            
            if ((tempBits & 0x400000L) != 0)
            {
                _headers._Expect = default;
                if((tempBits & ~0x400000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x400000L;
            }
            
            if ((tempBits & 0x800000L) != 0)
            {
                _headers._From = default;
                if((tempBits & ~0x800000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x800000L;
            }
            
            if ((tempBits & 0x1000000L) != 0)
            {
                _headers._GrpcAcceptEncoding = default;
                if((tempBits & ~0x1000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x1000000L;
            }
            
            if ((tempBits & 0x2000000L) != 0)
            {
                _headers._GrpcEncoding = default;
                if((tempBits & ~0x2000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x2000000L;
            }
            
            if ((tempBits & 0x4000000L) != 0)
            {
                _headers._GrpcTimeout = default;
                if((tempBits & ~0x4000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x4000000L;
            }
            
            if ((tempBits & 0x8000000L) != 0)
            {
                _headers._IfMatch = default;
                if((tempBits & ~0x8000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x8000000L;
            }
            
            if ((tempBits & 0x10000000L) != 0)
            {
                _headers._IfModifiedSince = default;
                if((tempBits & ~0x10000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x10000000L;
            }
            
            if ((tempBits & 0x20000000L) != 0)
            {
                _headers._IfNoneMatch = default;
                if((tempBits & ~0x20000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x20000000L;
            }
            
            if ((tempBits & 0x40000000L) != 0)
            {
                _headers._IfRange = default;
                if((tempBits & ~0x40000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x40000000L;
            }
            
            if ((tempBits & 0x80000000L) != 0)
            {
                _headers._IfUnmodifiedSince = default;
                if((tempBits & ~0x80000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x80000000L;
            }
            
            if ((tempBits & 0x100000000L) != 0)
            {
                _headers._KeepAlive = default;
                if((tempBits & ~0x100000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x100000000L;
            }
            
            if ((tempBits & 0x200000000L) != 0)
            {
                _headers._MaxForwards = default;
                if((tempBits & ~0x200000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x200000000L;
            }
            
            if ((tempBits & 0x400000000L) != 0)
            {
                _headers._Origin = default;
                if((tempBits & ~0x400000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x400000000L;
            }
            
            if ((tempBits & 0x800000000L) != 0)
            {
                _headers._Pragma = default;
                if((tempBits & ~0x800000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x800000000L;
            }
            
            if ((tempBits & 0x1000000000L) != 0)
            {
                _headers._ProxyAuthorization = default;
                if((tempBits & ~0x1000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x1000000000L;
            }
            
            if ((tempBits & 0x2000000000L) != 0)
            {
                _headers._Range = default;
                if((tempBits & ~0x2000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x2000000000L;
            }
            
            if ((tempBits & 0x4000000000L) != 0)
            {
                _headers._Referer = default;
                if((tempBits & ~0x4000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x4000000000L;
            }
            
            if ((tempBits & 0x8000000000L) != 0)
            {
                _headers._RequestId = default;
                if((tempBits & ~0x8000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x8000000000L;
            }
            
            if ((tempBits & 0x10000000000L) != 0)
            {
                _headers._TE = default;
                if((tempBits & ~0x10000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x10000000000L;
            }
            
            if ((tempBits & 0x20000000000L) != 0)
            {
                _headers._TraceParent = default;
                if((tempBits & ~0x20000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x20000000000L;
            }
            
            if ((tempBits & 0x40000000000L) != 0)
            {
                _headers._TraceState = default;
                if((tempBits & ~0x40000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x40000000000L;
            }
            
            if ((tempBits & 0x80000000000L) != 0)
            {
                _headers._TransferEncoding = default;
                if((tempBits & ~0x80000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x80000000000L;
            }
            
            if ((tempBits & 0x100000000000L) != 0)
            {
                _headers._Translate = default;
                if((tempBits & ~0x100000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x100000000000L;
            }
            
            if ((tempBits & 0x200000000000L) != 0)
            {
                _headers._Upgrade = default;
                if((tempBits & ~0x200000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x200000000000L;
            }
            
            if ((tempBits & 0x400000000000L) != 0)
            {
                _headers._UpgradeInsecureRequests = default;
                if((tempBits & ~0x400000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x400000000000L;
            }
            
            if ((tempBits & 0x800000000000L) != 0)
            {
                _headers._Via = default;
                if((tempBits & ~0x800000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x800000000000L;
            }
            
            if ((tempBits & 0x1000000000000L) != 0)
            {
                _headers._Warning = default;
                if((tempBits & ~0x1000000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x1000000000000L;
            }
            
        }

        protected override bool CopyToFast(KeyValuePair<string, StringValues>[] array, int arrayIndex)
        {
            if (arrayIndex < 0)
            {
                return false;
            }
            
                if ((_bits & 0x1L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Accept, _headers._Accept);
                    ++arrayIndex;
                }
                if ((_bits & 0x2L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Connection, _headers._Connection);
                    ++arrayIndex;
                }
                if ((_bits & 0x4L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Host, _headers._Host);
                    ++arrayIndex;
                }
                if ((_bits & 0x8L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.UserAgent, _headers._UserAgent);
                    ++arrayIndex;
                }
                if ((_bits & 0x10L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(InternalHeaderNames.Authority, _headers._Authority);
                    ++arrayIndex;
                }
                if ((_bits & 0x20L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(InternalHeaderNames.Method, _headers._Method);
                    ++arrayIndex;
                }
                if ((_bits & 0x40L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(InternalHeaderNames.Path, _headers._Path);
                    ++arrayIndex;
                }
                if ((_bits & 0x80L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(InternalHeaderNames.Protocol, _headers._Protocol);
                    ++arrayIndex;
                }
                if ((_bits & 0x100L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(InternalHeaderNames.Scheme, _headers._Scheme);
                    ++arrayIndex;
                }
                if ((_bits & 0x200L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.AcceptCharset, _headers._AcceptCharset);
                    ++arrayIndex;
                }
                if ((_bits & 0x400L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.AcceptEncoding, _headers._AcceptEncoding);
                    ++arrayIndex;
                }
                if ((_bits & 0x800L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.AcceptLanguage, _headers._AcceptLanguage);
                    ++arrayIndex;
                }
                if ((_bits & 0x1000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlRequestHeaders, _headers._AccessControlRequestHeaders);
                    ++arrayIndex;
                }
                if ((_bits & 0x2000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlRequestMethod, _headers._AccessControlRequestMethod);
                    ++arrayIndex;
                }
                if ((_bits & 0x4000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(InternalHeaderNames.AltUsed, _headers._AltUsed);
                    ++arrayIndex;
                }
                if ((_bits & 0x8000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Authorization, _headers._Authorization);
                    ++arrayIndex;
                }
                if ((_bits & 0x10000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Baggage, _headers._Baggage);
                    ++arrayIndex;
                }
                if ((_bits & 0x20000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.CacheControl, _headers._CacheControl);
                    ++arrayIndex;
                }
                if ((_bits & 0x40000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ContentType, _headers._ContentType);
                    ++arrayIndex;
                }
                if ((_bits & 0x80000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Cookie, _headers._Cookie);
                    ++arrayIndex;
                }
                if ((_bits & 0x100000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.CorrelationContext, _headers._CorrelationContext);
                    ++arrayIndex;
                }
                if ((_bits & 0x200000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Date, _headers._Date);
                    ++arrayIndex;
                }
                if ((_bits & 0x400000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Expect, _headers._Expect);
                    ++arrayIndex;
                }
                if ((_bits & 0x800000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.From, _headers._From);
                    ++arrayIndex;
                }
                if ((_bits & 0x1000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.GrpcAcceptEncoding, _headers._GrpcAcceptEncoding);
                    ++arrayIndex;
                }
                if ((_bits & 0x2000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.GrpcEncoding, _headers._GrpcEncoding);
                    ++arrayIndex;
                }
                if ((_bits & 0x4000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.GrpcTimeout, _headers._GrpcTimeout);
                    ++arrayIndex;
                }
                if ((_bits & 0x8000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.IfMatch, _headers._IfMatch);
                    ++arrayIndex;
                }
                if ((_bits & 0x10000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.IfModifiedSince, _headers._IfModifiedSince);
                    ++arrayIndex;
                }
                if ((_bits & 0x20000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.IfNoneMatch, _headers._IfNoneMatch);
                    ++arrayIndex;
                }
                if ((_bits & 0x40000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.IfRange, _headers._IfRange);
                    ++arrayIndex;
                }
                if ((_bits & 0x80000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.IfUnmodifiedSince, _headers._IfUnmodifiedSince);
                    ++arrayIndex;
                }
                if ((_bits & 0x100000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.KeepAlive, _headers._KeepAlive);
                    ++arrayIndex;
                }
                if ((_bits & 0x200000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.MaxForwards, _headers._MaxForwards);
                    ++arrayIndex;
                }
                if ((_bits & 0x400000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Origin, _headers._Origin);
                    ++arrayIndex;
                }
                if ((_bits & 0x800000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Pragma, _headers._Pragma);
                    ++arrayIndex;
                }
                if ((_bits & 0x1000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ProxyAuthorization, _headers._ProxyAuthorization);
                    ++arrayIndex;
                }
                if ((_bits & 0x2000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Range, _headers._Range);
                    ++arrayIndex;
                }
                if ((_bits & 0x4000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Referer, _headers._Referer);
                    ++arrayIndex;
                }
                if ((_bits & 0x8000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.RequestId, _headers._RequestId);
                    ++arrayIndex;
                }
                if ((_bits & 0x10000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.TE, _headers._TE);
                    ++arrayIndex;
                }
                if ((_bits & 0x20000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.TraceParent, _headers._TraceParent);
                    ++arrayIndex;
                }
                if ((_bits & 0x40000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.TraceState, _headers._TraceState);
                    ++arrayIndex;
                }
                if ((_bits & 0x80000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.TransferEncoding, _headers._TransferEncoding);
                    ++arrayIndex;
                }
                if ((_bits & 0x100000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Translate, _headers._Translate);
                    ++arrayIndex;
                }
                if ((_bits & 0x200000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Upgrade, _headers._Upgrade);
                    ++arrayIndex;
                }
                if ((_bits & 0x400000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.UpgradeInsecureRequests, _headers._UpgradeInsecureRequests);
                    ++arrayIndex;
                }
                if ((_bits & 0x800000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Via, _headers._Via);
                    ++arrayIndex;
                }
                if ((_bits & 0x1000000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Warning, _headers._Warning);
                    ++arrayIndex;
                }
                if (_contentLength.HasValue)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ContentLength, HeaderUtilities.FormatNonNegativeInt64(_contentLength.Value));
                    ++arrayIndex;
                }
            ((ICollection<KeyValuePair<string, StringValues>>?)MaybeUnknown)?.CopyTo(array, arrayIndex);

            return true;
        }
        
        internal void ClearPseudoRequestHeaders()
        {
            _pseudoBits = _bits & 496;
            _bits &= ~496;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ushort ReadUnalignedLittleEndian_ushort(ref byte source)
        {
            ushort result = Unsafe.ReadUnaligned<ushort>(ref source);
            if (!BitConverter.IsLittleEndian)
            {
                result = BinaryPrimitives.ReverseEndianness(result);
            }
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint ReadUnalignedLittleEndian_uint(ref byte source)
        {
            uint result = Unsafe.ReadUnaligned<uint>(ref source);
            if (!BitConverter.IsLittleEndian)
            {
                result = BinaryPrimitives.ReverseEndianness(result);
            }
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong ReadUnalignedLittleEndian_ulong(ref byte source)
        {
            ulong result = Unsafe.ReadUnaligned<ulong>(ref source);
            if (!BitConverter.IsLittleEndian)
            {
                result = BinaryPrimitives.ReverseEndianness(result);
            }
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Append(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value, bool checkForNewlineChars)
        {
            ref byte nameStart = ref MemoryMarshal.GetReference(name);
            var nameStr = string.Empty;
            ref StringValues values = ref Unsafe.NullRef<StringValues>();
            var flag = 0L;

            // Does the name match any "known" headers
            switch (name.Length)
            {
                case 2:
                    if (((ReadUnalignedLittleEndian_ushort(ref nameStart) & 0xdfdfu) == 0x4554u))
                    {
                        flag = 0x10000000000L;
                        values = ref _headers._TE;
                        nameStr = HeaderNames.TE;
                    }
                    break;
                case 3:
                    if (((ReadUnalignedLittleEndian_ushort(ref nameStart) & 0xdfdfu) == 0x4956u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)2) & 0xdfu) == 0x41u))
                    {
                        flag = 0x800000000000L;
                        values = ref _headers._Via;
                        nameStr = HeaderNames.Via;
                    }
                    break;
                case 4:
                    var firstTerm4 = (ReadUnalignedLittleEndian_uint(ref nameStart) & 0xdfdfdfdfu);
                    if ((firstTerm4 == 0x54534f48u))
                    {
                        flag = 0x4L;
                        values = ref _headers._Host;
                        nameStr = HeaderNames.Host;
                    }
                    else if ((firstTerm4 == 0x45544144u))
                    {
                        flag = 0x200000L;
                        values = ref _headers._Date;
                        nameStr = HeaderNames.Date;
                    }
                    else if ((firstTerm4 == 0x4d4f5246u))
                    {
                        flag = 0x800000L;
                        values = ref _headers._From;
                        nameStr = HeaderNames.From;
                    }
                    break;
                case 5:
                    if (((ReadUnalignedLittleEndian_uint(ref nameStart) & 0xdfdfdfffu) == 0x5441503au) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)4) & 0xdfu) == 0x48u))
                    {
                        flag = 0x40L;
                        values = ref _headers._Path;
                        nameStr = InternalHeaderNames.Path;
                    }
                    else if (((ReadUnalignedLittleEndian_uint(ref nameStart) & 0xdfdfdfdfu) == 0x474e4152u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)4) & 0xdfu) == 0x45u))
                    {
                        flag = 0x2000000000L;
                        values = ref _headers._Range;
                        nameStr = HeaderNames.Range;
                    }
                    break;
                case 6:
                    var firstTerm6 = (ReadUnalignedLittleEndian_uint(ref nameStart) & 0xdfdfdfdfu);
                    if ((firstTerm6 == 0x45434341u) && ((ReadUnalignedLittleEndian_ushort(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x5450u))
                    {
                        flag = 0x1L;
                        values = ref _headers._Accept;
                        nameStr = HeaderNames.Accept;
                    }
                    else if ((firstTerm6 == 0x4b4f4f43u) && ((ReadUnalignedLittleEndian_ushort(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x4549u))
                    {
                        flag = 0x80000L;
                        values = ref _headers._Cookie;
                        nameStr = HeaderNames.Cookie;
                    }
                    else if ((firstTerm6 == 0x45505845u) && ((ReadUnalignedLittleEndian_ushort(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x5443u))
                    {
                        flag = 0x400000L;
                        values = ref _headers._Expect;
                        nameStr = HeaderNames.Expect;
                    }
                    else if ((firstTerm6 == 0x4749524fu) && ((ReadUnalignedLittleEndian_ushort(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x4e49u))
                    {
                        flag = 0x400000000L;
                        values = ref _headers._Origin;
                        nameStr = HeaderNames.Origin;
                    }
                    else if ((firstTerm6 == 0x47415250u) && ((ReadUnalignedLittleEndian_ushort(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x414du))
                    {
                        flag = 0x800000000L;
                        values = ref _headers._Pragma;
                        nameStr = HeaderNames.Pragma;
                    }
                    break;
                case 7:
                    if (((ReadUnalignedLittleEndian_uint(ref nameStart) & 0xdfdfdfffu) == 0x54454d3au) && ((ReadUnalignedLittleEndian_ushort(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x4f48u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)6) & 0xdfu) == 0x44u))
                    {
                        flag = 0x20L;
                        values = ref _headers._Method;
                        nameStr = InternalHeaderNames.Method;
                    }
                    else if (((ReadUnalignedLittleEndian_uint(ref nameStart) & 0xdfdfdfffu) == 0x4843533au) && ((ReadUnalignedLittleEndian_ushort(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x4d45u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)6) & 0xdfu) == 0x45u))
                    {
                        flag = 0x100L;
                        values = ref _headers._Scheme;
                        nameStr = InternalHeaderNames.Scheme;
                    }
                    else if (((ReadUnalignedLittleEndian_uint(ref nameStart) & 0xdfdfdfdfu) == 0x47474142u) && ((ReadUnalignedLittleEndian_ushort(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x4741u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)6) & 0xdfu) == 0x45u))
                    {
                        flag = 0x10000L;
                        values = ref _headers._Baggage;
                        nameStr = HeaderNames.Baggage;
                    }
                    else if (((ReadUnalignedLittleEndian_uint(ref nameStart) & 0xdfdfdfdfu) == 0x45464552u) && ((ReadUnalignedLittleEndian_ushort(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x4552u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)6) & 0xdfu) == 0x52u))
                    {
                        flag = 0x4000000000L;
                        values = ref _headers._Referer;
                        nameStr = HeaderNames.Referer;
                    }
                    else if (((ReadUnalignedLittleEndian_uint(ref nameStart) & 0xdfdfdfdfu) == 0x52475055u) && ((ReadUnalignedLittleEndian_ushort(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x4441u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)6) & 0xdfu) == 0x45u))
                    {
                        flag = 0x200000000000L;
                        values = ref _headers._Upgrade;
                        nameStr = HeaderNames.Upgrade;
                    }
                    else if (((ReadUnalignedLittleEndian_uint(ref nameStart) & 0xdfdfdfdfu) == 0x4e524157u) && ((ReadUnalignedLittleEndian_ushort(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x4e49u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)6) & 0xdfu) == 0x47u))
                    {
                        flag = 0x1000000000000L;
                        values = ref _headers._Warning;
                        nameStr = HeaderNames.Warning;
                    }
                    break;
                case 8:
                    if (((ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xdfdfdfdfffdfdfdfuL) == 0x444553552d544c41uL))
                    {
                        flag = 0x4000L;
                        values = ref _headers._AltUsed;
                        nameStr = InternalHeaderNames.AltUsed;
                    }
                    else if (((ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xdfdfdfdfdfffdfdfuL) == 0x484354414d2d4649uL))
                    {
                        flag = 0x8000000L;
                        values = ref _headers._IfMatch;
                        nameStr = HeaderNames.IfMatch;
                    }
                    else if (((ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xdfdfdfdfdfffdfdfuL) == 0x45474e41522d4649uL))
                    {
                        flag = 0x40000000L;
                        values = ref _headers._IfRange;
                        nameStr = HeaderNames.IfRange;
                    }
                    break;
                case 9:
                    if (((ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xdfdfdfdfdfdfdfffuL) == 0x4f434f544f52503auL) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)8) & 0xdfu) == 0x4cu))
                    {
                        flag = 0x80L;
                        values = ref _headers._Protocol;
                        nameStr = InternalHeaderNames.Protocol;
                    }
                    else if (((ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xdfdfdfdfdfdfdfdfuL) == 0x54414c534e415254uL) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)8) & 0xdfu) == 0x45u))
                    {
                        flag = 0x100000000000L;
                        values = ref _headers._Translate;
                        nameStr = HeaderNames.Translate;
                    }
                    break;
                case 10:
                    if (((ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xdfdfdfdfdfdfdfdfuL) == 0x495443454e4e4f43uL) && ((ReadUnalignedLittleEndian_ushort(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(4 * sizeof(ushort)))) & 0xdfdfu) == 0x4e4fu))
                    {
                        flag = 0x2L;
                        values = ref _headers._Connection;
                        nameStr = HeaderNames.Connection;
                    }
                    else if (((ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xdfdfdfffdfdfdfdfuL) == 0x4547412d52455355uL) && ((ReadUnalignedLittleEndian_ushort(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(4 * sizeof(ushort)))) & 0xdfdfu) == 0x544eu))
                    {
                        flag = 0x8L;
                        values = ref _headers._UserAgent;
                        nameStr = HeaderNames.UserAgent;
                    }
                    else if (((ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xdfdfdfdfdfdfdfffuL) == 0x49524f485455413auL) && ((ReadUnalignedLittleEndian_ushort(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(4 * sizeof(ushort)))) & 0xdfdfu) == 0x5954u))
                    {
                        flag = 0x10L;
                        values = ref _headers._Authority;
                        nameStr = InternalHeaderNames.Authority;
                    }
                    else if (((ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xdfdfdfffdfdfdfdfuL) == 0x494c412d5045454buL) && ((ReadUnalignedLittleEndian_ushort(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(4 * sizeof(ushort)))) & 0xdfdfu) == 0x4556u))
                    {
                        flag = 0x100000000L;
                        values = ref _headers._KeepAlive;
                        nameStr = HeaderNames.KeepAlive;
                    }
                    else if (((ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xffdfdfdfdfdfdfdfuL) == 0x2d54534555514552uL) && ((ReadUnalignedLittleEndian_ushort(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(4 * sizeof(ushort)))) & 0xdfdfu) == 0x4449u))
                    {
                        flag = 0x8000000000L;
                        values = ref _headers._RequestId;
                        nameStr = HeaderNames.RequestId;
                    }
                    else if (((ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xdfdfdfdfdfdfdfdfuL) == 0x4154534543415254uL) && ((ReadUnalignedLittleEndian_ushort(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(4 * sizeof(ushort)))) & 0xdfdfu) == 0x4554u))
                    {
                        flag = 0x40000000000L;
                        values = ref _headers._TraceState;
                        nameStr = HeaderNames.TraceState;
                    }
                    break;
                case 11:
                    if (((ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xdfdfdfdfdfdfdfdfuL) == 0x5241504543415254uL) && ((ReadUnalignedLittleEndian_ushort(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(4 * sizeof(ushort)))) & 0xdfdfu) == 0x4e45u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)10) & 0xdfu) == 0x54u))
                    {
                        flag = 0x20000000000L;
                        values = ref _headers._TraceParent;
                        nameStr = HeaderNames.TraceParent;
                    }
                    break;
                case 12:
                    if (((ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xffdfdfdfdfdfdfdfuL) == 0x2d544e45544e4f43uL) && ((ReadUnalignedLittleEndian_uint(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x45505954u))
                    {
                        flag = 0x40000L;
                        values = ref _headers._ContentType;
                        nameStr = HeaderNames.ContentType;
                    }
                    else if (((ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xdfdfdfffdfdfdfdfuL) == 0x4d49542d43505247uL) && ((ReadUnalignedLittleEndian_uint(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x54554f45u))
                    {
                        flag = 0x4000000L;
                        values = ref _headers._GrpcTimeout;
                        nameStr = HeaderNames.GrpcTimeout;
                    }
                    else if (((ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xdfdfdfdfffdfdfdfuL) == 0x57524f462d58414duL) && ((ReadUnalignedLittleEndian_uint(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x53445241u))
                    {
                        flag = 0x200000000L;
                        values = ref _headers._MaxForwards;
                        nameStr = HeaderNames.MaxForwards;
                    }
                    break;
                case 13:
                    if (((ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xdfdfdfdfdfdfdfdfuL) == 0x5a49524f48545541uL) && ((ReadUnalignedLittleEndian_uint(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x4f495441u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)12) & 0xdfu) == 0x4eu))
                    {
                        flag = 0x8000L;
                        values = ref _headers._Authorization;
                        nameStr = HeaderNames.Authorization;
                    }
                    else if (((ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xdfdfffdfdfdfdfdfuL) == 0x4f432d4548434143uL) && ((ReadUnalignedLittleEndian_uint(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x4f52544eu) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)12) & 0xdfu) == 0x4cu))
                    {
                        flag = 0x20000L;
                        values = ref _headers._CacheControl;
                        nameStr = HeaderNames.CacheControl;
                    }
                    else if (((ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xdfdfdfffdfdfdfdfuL) == 0x434e452d43505247uL) && ((ReadUnalignedLittleEndian_uint(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x4e49444fu) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)12) & 0xdfu) == 0x47u))
                    {
                        flag = 0x2000000L;
                        values = ref _headers._GrpcEncoding;
                        nameStr = HeaderNames.GrpcEncoding;
                    }
                    else if (((ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xffdfdfdfdfffdfdfuL) == 0x2d454e4f4e2d4649uL) && ((ReadUnalignedLittleEndian_uint(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x4354414du) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)12) & 0xdfu) == 0x48u))
                    {
                        flag = 0x20000000L;
                        values = ref _headers._IfNoneMatch;
                        nameStr = HeaderNames.IfNoneMatch;
                    }
                    break;
                case 14:
                    if (((ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xdfffdfdfdfdfdfdfuL) == 0x432d545045434341uL) && ((ReadUnalignedLittleEndian_uint(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x53524148u) && ((ReadUnalignedLittleEndian_ushort(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(6 * sizeof(ushort)))) & 0xdfdfu) == 0x5445u))
                    {
                        flag = 0x200L;
                        values = ref _headers._AcceptCharset;
                        nameStr = HeaderNames.AcceptCharset;
                    }
                    else if (((ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xffdfdfdfdfdfdfdfuL) == 0x2d544e45544e4f43uL) && ((ReadUnalignedLittleEndian_uint(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x474e454cu) && ((ReadUnalignedLittleEndian_ushort(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(6 * sizeof(ushort)))) & 0xdfdfu) == 0x4854u))
                    {
                        var customEncoding = ReferenceEquals(EncodingSelector, KestrelServerOptions.DefaultHeaderEncodingSelector)
                           ? null : EncodingSelector(HeaderNames.ContentLength);
                        if (customEncoding == null)
                        {
                            AppendContentLength(value);
                        }
                        else
                        {
                            AppendContentLengthCustomEncoding(value, customEncoding);
                        }
                        return;
                    }
                    break;
                case 15:
                    var firstTerm15 = (ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xdfffdfdfdfdfdfdfuL);
                    if ((firstTerm15 == 0x452d545045434341uL) && ((ReadUnalignedLittleEndian_uint(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x444f434eu) && ((ReadUnalignedLittleEndian_ushort(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(6 * sizeof(ushort)))) & 0xdfdfu) == 0x4e49u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)14) & 0xdfu) == 0x47u))
                    {
                        flag = 0x400L;
                        values = ref _headers._AcceptEncoding;
                        nameStr = HeaderNames.AcceptEncoding;
                    }
                    else if ((firstTerm15 == 0x4c2d545045434341uL) && ((ReadUnalignedLittleEndian_uint(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x55474e41u) && ((ReadUnalignedLittleEndian_ushort(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(6 * sizeof(ushort)))) & 0xdfdfu) == 0x4741u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)14) & 0xdfu) == 0x45u))
                    {
                        flag = 0x800L;
                        values = ref _headers._AcceptLanguage;
                        nameStr = HeaderNames.AcceptLanguage;
                    }
                    break;
                case 17:
                    if (((ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xdfdfdfdfdfffdfdfuL) == 0x4649444f4d2d4649uL) && ((ReadUnalignedLittleEndian_ulong(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)sizeof(ulong))) & 0xdfdfdfdfffdfdfdfuL) == 0x434e49532d444549uL) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)16) & 0xdfu) == 0x45u))
                    {
                        flag = 0x10000000L;
                        values = ref _headers._IfModifiedSince;
                        nameStr = HeaderNames.IfModifiedSince;
                    }
                    else if (((ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xdfdfdfdfdfdfdfdfuL) == 0x524546534e415254uL) && ((ReadUnalignedLittleEndian_ulong(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)sizeof(ulong))) & 0xdfdfdfdfdfdfdfffuL) == 0x4e49444f434e452duL) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)16) & 0xdfu) == 0x47u))
                    {
                        flag = 0x80000000000L;
                        values = ref _headers._TransferEncoding;
                        nameStr = HeaderNames.TransferEncoding;
                    }
                    break;
                case 19:
                    if (((ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xdfdfdfdfdfdfdfdfuL) == 0x54414c4552524f43uL) && ((ReadUnalignedLittleEndian_ulong(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)sizeof(ulong))) & 0xdfdfdfdfffdfdfdfuL) == 0x544e4f432d4e4f49uL) && ((ReadUnalignedLittleEndian_ushort(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(8 * sizeof(ushort)))) & 0xdfdfu) == 0x5845u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)18) & 0xdfu) == 0x54u))
                    {
                        flag = 0x100000L;
                        values = ref _headers._CorrelationContext;
                        nameStr = HeaderNames.CorrelationContext;
                    }
                    else if (((ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xdfdfdfdfdfffdfdfuL) == 0x444f4d4e552d4649uL) && ((ReadUnalignedLittleEndian_ulong(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)sizeof(ulong))) & 0xdfdfffdfdfdfdfdfuL) == 0x49532d4445494649uL) && ((ReadUnalignedLittleEndian_ushort(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(8 * sizeof(ushort)))) & 0xdfdfu) == 0x434eu) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)18) & 0xdfu) == 0x45u))
                    {
                        flag = 0x80000000L;
                        values = ref _headers._IfUnmodifiedSince;
                        nameStr = HeaderNames.IfUnmodifiedSince;
                    }
                    else if (((ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xdfdfffdfdfdfdfdfuL) == 0x55412d59584f5250uL) && ((ReadUnalignedLittleEndian_ulong(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)sizeof(ulong))) & 0xdfdfdfdfdfdfdfdfuL) == 0x54415a49524f4854uL) && ((ReadUnalignedLittleEndian_ushort(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(8 * sizeof(ushort)))) & 0xdfdfu) == 0x4f49u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)18) & 0xdfu) == 0x4eu))
                    {
                        flag = 0x1000000000L;
                        values = ref _headers._ProxyAuthorization;
                        nameStr = HeaderNames.ProxyAuthorization;
                    }
                    break;
                case 20:
                    if (((ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xdfdfdfffdfdfdfdfuL) == 0x4343412d43505247uL) && ((ReadUnalignedLittleEndian_ulong(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)sizeof(ulong))) & 0xdfdfdfdfffdfdfdfuL) == 0x4f434e452d545045uL) && ((ReadUnalignedLittleEndian_uint(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(4 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x474e4944u))
                    {
                        flag = 0x1000000L;
                        values = ref _headers._GrpcAcceptEncoding;
                        nameStr = HeaderNames.GrpcAcceptEncoding;
                    }
                    break;
                case 25:
                    if (((ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xffdfdfdfdfdfdfdfuL) == 0x2d45444152475055uL) && ((ReadUnalignedLittleEndian_ulong(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)sizeof(ulong))) & 0xdfdfdfdfdfdfdfdfuL) == 0x4552554345534e49uL) && ((ReadUnalignedLittleEndian_ulong(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ulong)))) & 0xdfdfdfdfdfdfdfffuL) == 0x545345555145522duL) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)24) & 0xdfu) == 0x53u))
                    {
                        flag = 0x400000000000L;
                        values = ref _headers._UpgradeInsecureRequests;
                        nameStr = HeaderNames.UpgradeInsecureRequests;
                    }
                    break;
                case 29:
                    if (((ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xdfffdfdfdfdfdfdfuL) == 0x432d535345434341uL) && ((ReadUnalignedLittleEndian_ulong(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)sizeof(ulong))) & 0xdfffdfdfdfdfdfdfuL) == 0x522d4c4f52544e4fuL) && ((ReadUnalignedLittleEndian_ulong(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ulong)))) & 0xdfffdfdfdfdfdfdfuL) == 0x4d2d545345555145uL) && ((ReadUnalignedLittleEndian_uint(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(6 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x4f485445u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)28) & 0xdfu) == 0x44u))
                    {
                        flag = 0x2000L;
                        values = ref _headers._AccessControlRequestMethod;
                        nameStr = HeaderNames.AccessControlRequestMethod;
                    }
                    break;
                case 30:
                    if (((ReadUnalignedLittleEndian_ulong(ref nameStart) & 0xdfffdfdfdfdfdfdfuL) == 0x432d535345434341uL) && ((ReadUnalignedLittleEndian_ulong(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)sizeof(ulong))) & 0xdfffdfdfdfdfdfdfuL) == 0x522d4c4f52544e4fuL) && ((ReadUnalignedLittleEndian_ulong(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ulong)))) & 0xdfffdfdfdfdfdfdfuL) == 0x482d545345555145uL) && ((ReadUnalignedLittleEndian_uint(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(6 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x45444145u) && ((ReadUnalignedLittleEndian_ushort(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(14 * sizeof(ushort)))) & 0xdfdfu) == 0x5352u))
                    {
                        flag = 0x1000L;
                        values = ref _headers._AccessControlRequestHeaders;
                        nameStr = HeaderNames.AccessControlRequestHeaders;
                    }
                    break;
            }

            if (flag != 0)
            {
                // Matched a known header
                if ((_previousBits & flag) != 0)
                {
                    // Had a previous string for this header, mark it as used so we don't clear it OnHeadersComplete or consider it if we get a second header
                    _previousBits ^= flag;

                    // We will only reuse this header if there was only one previous header
                    if (values.Count == 1)
                    {
                        var previousValue = values.ToString();
                        // Check lengths are the same, then if the bytes were converted to an ascii string if they would be the same.
                        // We do not consider Utf8 headers for reuse.
                        if (previousValue.Length == value.Length &&
                            StringUtilities.BytesOrdinalEqualsStringAndAscii(previousValue, value))
                        {
                            // The previous string matches what the bytes would convert to, so we will just use that one.
                            _bits |= flag;
                            return;
                        }
                    }
                }

                // We didn't have a previous matching header value, or have already added a header, so get the string for this value.
                var valueStr = value.GetRequestHeaderString(nameStr, EncodingSelector, checkForNewlineChars);
                if ((_bits & flag) == 0)
                {
                    // We didn't already have a header set, so add a new one.
                    _bits |= flag;
                    values = new StringValues(valueStr);
                }
                else
                {
                    // We already had a header set, so concatenate the new one.
                    values = AppendValue(values, valueStr);
                }
            }
            else
            {
                // The header was not one of the "known" headers.
                // Convert value to string first, because passing two spans causes 8 bytes stack zeroing in
                // this method with rep stosd, which is slower than necessary.
                nameStr = name.GetHeaderName();
                var valueStr = value.GetRequestHeaderString(nameStr, EncodingSelector, checkForNewlineChars);
                AppendUnknownHeaders(nameStr, valueStr);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public bool TryHPackAppend(int index, ReadOnlySpan<byte> value, bool checkForNewlineChars)
        {
            ref StringValues values = ref Unsafe.NullRef<StringValues>();
            var nameStr = string.Empty;
            var flag = 0L;

            // Does the HPack static index match any "known" headers
            switch (index)
            {
                case 1:
                    flag = 0x10L;
                    values = ref _headers._Authority;
                    nameStr = InternalHeaderNames.Authority;
                    break;
                case 2:
                case 3:
                    flag = 0x20L;
                    values = ref _headers._Method;
                    nameStr = InternalHeaderNames.Method;
                    break;
                case 4:
                case 5:
                    flag = 0x40L;
                    values = ref _headers._Path;
                    nameStr = InternalHeaderNames.Path;
                    break;
                case 6:
                case 7:
                    flag = 0x100L;
                    values = ref _headers._Scheme;
                    nameStr = InternalHeaderNames.Scheme;
                    break;
                case 15:
                    flag = 0x200L;
                    values = ref _headers._AcceptCharset;
                    nameStr = HeaderNames.AcceptCharset;
                    break;
                case 16:
                    flag = 0x400L;
                    values = ref _headers._AcceptEncoding;
                    nameStr = HeaderNames.AcceptEncoding;
                    break;
                case 17:
                    flag = 0x800L;
                    values = ref _headers._AcceptLanguage;
                    nameStr = HeaderNames.AcceptLanguage;
                    break;
                case 19:
                    flag = 0x1L;
                    values = ref _headers._Accept;
                    nameStr = HeaderNames.Accept;
                    break;
                case 23:
                    flag = 0x8000L;
                    values = ref _headers._Authorization;
                    nameStr = HeaderNames.Authorization;
                    break;
                case 24:
                    flag = 0x20000L;
                    values = ref _headers._CacheControl;
                    nameStr = HeaderNames.CacheControl;
                    break;
                case 28:
                    var customEncoding = ReferenceEquals(EncodingSelector, KestrelServerOptions.DefaultHeaderEncodingSelector)
                        ? null : EncodingSelector(HeaderNames.ContentLength);
                    if (customEncoding == null)
                    {
                        AppendContentLength(value);
                    }
                    else
                    {
                        AppendContentLengthCustomEncoding(value, customEncoding);
                    }
                    return true;
                case 31:
                    flag = 0x40000L;
                    values = ref _headers._ContentType;
                    nameStr = HeaderNames.ContentType;
                    break;
                case 32:
                    flag = 0x80000L;
                    values = ref _headers._Cookie;
                    nameStr = HeaderNames.Cookie;
                    break;
                case 33:
                    flag = 0x200000L;
                    values = ref _headers._Date;
                    nameStr = HeaderNames.Date;
                    break;
                case 35:
                    flag = 0x400000L;
                    values = ref _headers._Expect;
                    nameStr = HeaderNames.Expect;
                    break;
                case 37:
                    flag = 0x800000L;
                    values = ref _headers._From;
                    nameStr = HeaderNames.From;
                    break;
                case 38:
                    flag = 0x4L;
                    values = ref _headers._Host;
                    nameStr = HeaderNames.Host;
                    break;
                case 39:
                    flag = 0x8000000L;
                    values = ref _headers._IfMatch;
                    nameStr = HeaderNames.IfMatch;
                    break;
                case 40:
                    flag = 0x10000000L;
                    values = ref _headers._IfModifiedSince;
                    nameStr = HeaderNames.IfModifiedSince;
                    break;
                case 41:
                    flag = 0x20000000L;
                    values = ref _headers._IfNoneMatch;
                    nameStr = HeaderNames.IfNoneMatch;
                    break;
                case 42:
                    flag = 0x40000000L;
                    values = ref _headers._IfRange;
                    nameStr = HeaderNames.IfRange;
                    break;
                case 43:
                    flag = 0x80000000L;
                    values = ref _headers._IfUnmodifiedSince;
                    nameStr = HeaderNames.IfUnmodifiedSince;
                    break;
                case 47:
                    flag = 0x200000000L;
                    values = ref _headers._MaxForwards;
                    nameStr = HeaderNames.MaxForwards;
                    break;
                case 49:
                    flag = 0x1000000000L;
                    values = ref _headers._ProxyAuthorization;
                    nameStr = HeaderNames.ProxyAuthorization;
                    break;
                case 50:
                    flag = 0x2000000000L;
                    values = ref _headers._Range;
                    nameStr = HeaderNames.Range;
                    break;
                case 51:
                    flag = 0x4000000000L;
                    values = ref _headers._Referer;
                    nameStr = HeaderNames.Referer;
                    break;
                case 57:
                    flag = 0x80000000000L;
                    values = ref _headers._TransferEncoding;
                    nameStr = HeaderNames.TransferEncoding;
                    break;
                case 58:
                    flag = 0x8L;
                    values = ref _headers._UserAgent;
                    nameStr = HeaderNames.UserAgent;
                    break;
                case 60:
                    flag = 0x800000000000L;
                    values = ref _headers._Via;
                    nameStr = HeaderNames.Via;
                    break;
            }

            if (flag != 0)
            {
                // Matched a known header
                if ((_previousBits & flag) != 0)
                {
                    // Had a previous string for this header, mark it as used so we don't clear it OnHeadersComplete or consider it if we get a second header
                    _previousBits ^= flag;

                    // We will only reuse this header if there was only one previous header
                    if (values.Count == 1)
                    {
                        var previousValue = values.ToString();
                        // Check lengths are the same, then if the bytes were converted to an ascii string if they would be the same.
                        // We do not consider Utf8 headers for reuse.
                        if (previousValue.Length == value.Length &&
                            StringUtilities.BytesOrdinalEqualsStringAndAscii(previousValue, value))
                        {
                            // The previous string matches what the bytes would convert to, so we will just use that one.
                            _bits |= flag;
                            return true;
                        }
                    }
                }

                // We didn't have a previous matching header value, or have already added a header, so get the string for this value.
                var valueStr = value.GetRequestHeaderString(nameStr, EncodingSelector, checkForNewlineChars);
                if ((_bits & flag) == 0)
                {
                    // We didn't already have a header set, so add a new one.
                    _bits |= flag;
                    values = new StringValues(valueStr);
                }
                else
                {
                    // We already had a header set, so concatenate the new one.
                    values = AppendValue(values, valueStr);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public bool TryQPackAppend(int index, ReadOnlySpan<byte> value, bool checkForNewlineChars)
        {
            ref StringValues values = ref Unsafe.NullRef<StringValues>();
            var nameStr = string.Empty;
            var flag = 0L;

            // Does the QPack static index match any "known" headers
            switch (index)
            {
                case 0:
                    flag = 0x10L;
                    values = ref _headers._Authority;
                    nameStr = InternalHeaderNames.Authority;
                    break;
                case 1:
                    flag = 0x40L;
                    values = ref _headers._Path;
                    nameStr = InternalHeaderNames.Path;
                    break;
                case 4:
                    var customEncoding = ReferenceEquals(EncodingSelector, KestrelServerOptions.DefaultHeaderEncodingSelector)
                        ? null : EncodingSelector(HeaderNames.ContentLength);
                    if (customEncoding == null)
                    {
                        AppendContentLength(value);
                    }
                    else
                    {
                        AppendContentLengthCustomEncoding(value, customEncoding);
                    }
                    return true;
                case 5:
                    flag = 0x80000L;
                    values = ref _headers._Cookie;
                    nameStr = HeaderNames.Cookie;
                    break;
                case 6:
                    flag = 0x200000L;
                    values = ref _headers._Date;
                    nameStr = HeaderNames.Date;
                    break;
                case 8:
                    flag = 0x10000000L;
                    values = ref _headers._IfModifiedSince;
                    nameStr = HeaderNames.IfModifiedSince;
                    break;
                case 9:
                    flag = 0x20000000L;
                    values = ref _headers._IfNoneMatch;
                    nameStr = HeaderNames.IfNoneMatch;
                    break;
                case 13:
                    flag = 0x4000000000L;
                    values = ref _headers._Referer;
                    nameStr = HeaderNames.Referer;
                    break;
                case 15:
                case 16:
                case 17:
                case 18:
                case 19:
                case 20:
                case 21:
                    flag = 0x20L;
                    values = ref _headers._Method;
                    nameStr = InternalHeaderNames.Method;
                    break;
                case 22:
                case 23:
                    flag = 0x100L;
                    values = ref _headers._Scheme;
                    nameStr = InternalHeaderNames.Scheme;
                    break;
                case 29:
                case 30:
                    flag = 0x1L;
                    values = ref _headers._Accept;
                    nameStr = HeaderNames.Accept;
                    break;
                case 31:
                    flag = 0x400L;
                    values = ref _headers._AcceptEncoding;
                    nameStr = HeaderNames.AcceptEncoding;
                    break;
                case 36:
                case 37:
                case 38:
                case 39:
                case 40:
                case 41:
                    flag = 0x20000L;
                    values = ref _headers._CacheControl;
                    nameStr = HeaderNames.CacheControl;
                    break;
                case 44:
                case 45:
                case 46:
                case 47:
                case 48:
                case 49:
                case 50:
                case 51:
                case 52:
                case 53:
                case 54:
                    flag = 0x40000L;
                    values = ref _headers._ContentType;
                    nameStr = HeaderNames.ContentType;
                    break;
                case 55:
                    flag = 0x2000000000L;
                    values = ref _headers._Range;
                    nameStr = HeaderNames.Range;
                    break;
                case 72:
                    flag = 0x800L;
                    values = ref _headers._AcceptLanguage;
                    nameStr = HeaderNames.AcceptLanguage;
                    break;
                case 80:
                    flag = 0x1000L;
                    values = ref _headers._AccessControlRequestHeaders;
                    nameStr = HeaderNames.AccessControlRequestHeaders;
                    break;
                case 81:
                case 82:
                    flag = 0x2000L;
                    values = ref _headers._AccessControlRequestMethod;
                    nameStr = HeaderNames.AccessControlRequestMethod;
                    break;
                case 84:
                    flag = 0x8000L;
                    values = ref _headers._Authorization;
                    nameStr = HeaderNames.Authorization;
                    break;
                case 89:
                    flag = 0x40000000L;
                    values = ref _headers._IfRange;
                    nameStr = HeaderNames.IfRange;
                    break;
                case 90:
                    flag = 0x400000000L;
                    values = ref _headers._Origin;
                    nameStr = HeaderNames.Origin;
                    break;
                case 94:
                    flag = 0x400000000000L;
                    values = ref _headers._UpgradeInsecureRequests;
                    nameStr = HeaderNames.UpgradeInsecureRequests;
                    break;
                case 95:
                    flag = 0x8L;
                    values = ref _headers._UserAgent;
                    nameStr = HeaderNames.UserAgent;
                    break;
            }

            if (flag != 0)
            {
                // Matched a known header
                if ((_previousBits & flag) != 0)
                {
                    // Had a previous string for this header, mark it as used so we don't clear it OnHeadersComplete or consider it if we get a second header
                    _previousBits ^= flag;

                    // We will only reuse this header if there was only one previous header
                    if (values.Count == 1)
                    {
                        var previousValue = values.ToString();
                        // Check lengths are the same, then if the bytes were converted to an ascii string if they would be the same.
                        // We do not consider Utf8 headers for reuse.
                        if (previousValue.Length == value.Length &&
                            StringUtilities.BytesOrdinalEqualsStringAndAscii(previousValue, value))
                        {
                            // The previous string matches what the bytes would convert to, so we will just use that one.
                            _bits |= flag;
                            return true;
                        }
                    }
                }

                // We didn't have a previous matching header value, or have already added a header, so get the string for this value.
                var valueStr = value.GetRequestHeaderString(nameStr, EncodingSelector, checkForNewlineChars);
                if ((_bits & flag) == 0)
                {
                    // We didn't already have a header set, so add a new one.
                    _bits |= flag;
                    values = new StringValues(valueStr);
                }
                else
                {
                    // We already had a header set, so concatenate the new one.
                    values = AppendValue(values, valueStr);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private struct HeaderReferences
        {
            public StringValues _Accept;
            public StringValues _Connection;
            public StringValues _Host;
            public StringValues _UserAgent;
            public StringValues _Authority;
            public StringValues _Method;
            public StringValues _Path;
            public StringValues _Protocol;
            public StringValues _Scheme;
            public StringValues _AcceptCharset;
            public StringValues _AcceptEncoding;
            public StringValues _AcceptLanguage;
            public StringValues _AccessControlRequestHeaders;
            public StringValues _AccessControlRequestMethod;
            public StringValues _AltUsed;
            public StringValues _Authorization;
            public StringValues _Baggage;
            public StringValues _CacheControl;
            public StringValues _ContentType;
            public StringValues _Cookie;
            public StringValues _CorrelationContext;
            public StringValues _Date;
            public StringValues _Expect;
            public StringValues _From;
            public StringValues _GrpcAcceptEncoding;
            public StringValues _GrpcEncoding;
            public StringValues _GrpcTimeout;
            public StringValues _IfMatch;
            public StringValues _IfModifiedSince;
            public StringValues _IfNoneMatch;
            public StringValues _IfRange;
            public StringValues _IfUnmodifiedSince;
            public StringValues _KeepAlive;
            public StringValues _MaxForwards;
            public StringValues _Origin;
            public StringValues _Pragma;
            public StringValues _ProxyAuthorization;
            public StringValues _Range;
            public StringValues _Referer;
            public StringValues _RequestId;
            public StringValues _TE;
            public StringValues _TraceParent;
            public StringValues _TraceState;
            public StringValues _TransferEncoding;
            public StringValues _Translate;
            public StringValues _Upgrade;
            public StringValues _UpgradeInsecureRequests;
            public StringValues _Via;
            public StringValues _Warning;
            
        }

        public partial struct Enumerator
        {
            // Compiled to Jump table
            public bool MoveNext()
            {
                switch (_next)
                {
                    case 0: // Header: "Accept"
                        Debug.Assert((_currentBits & 0x1L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Accept, _collection._headers._Accept);
                        _currentBits ^= 0x1L;
                        break;
                    case 1: // Header: "Connection"
                        Debug.Assert((_currentBits & 0x2L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Connection, _collection._headers._Connection);
                        _currentBits ^= 0x2L;
                        break;
                    case 2: // Header: "Host"
                        Debug.Assert((_currentBits & 0x4L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Host, _collection._headers._Host);
                        _currentBits ^= 0x4L;
                        break;
                    case 3: // Header: "User-Agent"
                        Debug.Assert((_currentBits & 0x8L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.UserAgent, _collection._headers._UserAgent);
                        _currentBits ^= 0x8L;
                        break;
                    case 4: // Header: ":authority"
                        Debug.Assert((_currentBits & 0x10L) != 0);
                        _current = new KeyValuePair<string, StringValues>(InternalHeaderNames.Authority, _collection._headers._Authority);
                        _currentBits ^= 0x10L;
                        break;
                    case 5: // Header: ":method"
                        Debug.Assert((_currentBits & 0x20L) != 0);
                        _current = new KeyValuePair<string, StringValues>(InternalHeaderNames.Method, _collection._headers._Method);
                        _currentBits ^= 0x20L;
                        break;
                    case 6: // Header: ":path"
                        Debug.Assert((_currentBits & 0x40L) != 0);
                        _current = new KeyValuePair<string, StringValues>(InternalHeaderNames.Path, _collection._headers._Path);
                        _currentBits ^= 0x40L;
                        break;
                    case 7: // Header: ":protocol"
                        Debug.Assert((_currentBits & 0x80L) != 0);
                        _current = new KeyValuePair<string, StringValues>(InternalHeaderNames.Protocol, _collection._headers._Protocol);
                        _currentBits ^= 0x80L;
                        break;
                    case 8: // Header: ":scheme"
                        Debug.Assert((_currentBits & 0x100L) != 0);
                        _current = new KeyValuePair<string, StringValues>(InternalHeaderNames.Scheme, _collection._headers._Scheme);
                        _currentBits ^= 0x100L;
                        break;
                    case 9: // Header: "Accept-Charset"
                        Debug.Assert((_currentBits & 0x200L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.AcceptCharset, _collection._headers._AcceptCharset);
                        _currentBits ^= 0x200L;
                        break;
                    case 10: // Header: "Accept-Encoding"
                        Debug.Assert((_currentBits & 0x400L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.AcceptEncoding, _collection._headers._AcceptEncoding);
                        _currentBits ^= 0x400L;
                        break;
                    case 11: // Header: "Accept-Language"
                        Debug.Assert((_currentBits & 0x800L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.AcceptLanguage, _collection._headers._AcceptLanguage);
                        _currentBits ^= 0x800L;
                        break;
                    case 12: // Header: "Access-Control-Request-Headers"
                        Debug.Assert((_currentBits & 0x1000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlRequestHeaders, _collection._headers._AccessControlRequestHeaders);
                        _currentBits ^= 0x1000L;
                        break;
                    case 13: // Header: "Access-Control-Request-Method"
                        Debug.Assert((_currentBits & 0x2000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlRequestMethod, _collection._headers._AccessControlRequestMethod);
                        _currentBits ^= 0x2000L;
                        break;
                    case 14: // Header: "Alt-Used"
                        Debug.Assert((_currentBits & 0x4000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(InternalHeaderNames.AltUsed, _collection._headers._AltUsed);
                        _currentBits ^= 0x4000L;
                        break;
                    case 15: // Header: "Authorization"
                        Debug.Assert((_currentBits & 0x8000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Authorization, _collection._headers._Authorization);
                        _currentBits ^= 0x8000L;
                        break;
                    case 16: // Header: "baggage"
                        Debug.Assert((_currentBits & 0x10000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Baggage, _collection._headers._Baggage);
                        _currentBits ^= 0x10000L;
                        break;
                    case 17: // Header: "Cache-Control"
                        Debug.Assert((_currentBits & 0x20000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.CacheControl, _collection._headers._CacheControl);
                        _currentBits ^= 0x20000L;
                        break;
                    case 18: // Header: "Content-Type"
                        Debug.Assert((_currentBits & 0x40000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ContentType, _collection._headers._ContentType);
                        _currentBits ^= 0x40000L;
                        break;
                    case 19: // Header: "Cookie"
                        Debug.Assert((_currentBits & 0x80000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Cookie, _collection._headers._Cookie);
                        _currentBits ^= 0x80000L;
                        break;
                    case 20: // Header: "Correlation-Context"
                        Debug.Assert((_currentBits & 0x100000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.CorrelationContext, _collection._headers._CorrelationContext);
                        _currentBits ^= 0x100000L;
                        break;
                    case 21: // Header: "Date"
                        Debug.Assert((_currentBits & 0x200000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Date, _collection._headers._Date);
                        _currentBits ^= 0x200000L;
                        break;
                    case 22: // Header: "Expect"
                        Debug.Assert((_currentBits & 0x400000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Expect, _collection._headers._Expect);
                        _currentBits ^= 0x400000L;
                        break;
                    case 23: // Header: "From"
                        Debug.Assert((_currentBits & 0x800000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.From, _collection._headers._From);
                        _currentBits ^= 0x800000L;
                        break;
                    case 24: // Header: "Grpc-Accept-Encoding"
                        Debug.Assert((_currentBits & 0x1000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.GrpcAcceptEncoding, _collection._headers._GrpcAcceptEncoding);
                        _currentBits ^= 0x1000000L;
                        break;
                    case 25: // Header: "Grpc-Encoding"
                        Debug.Assert((_currentBits & 0x2000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.GrpcEncoding, _collection._headers._GrpcEncoding);
                        _currentBits ^= 0x2000000L;
                        break;
                    case 26: // Header: "Grpc-Timeout"
                        Debug.Assert((_currentBits & 0x4000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.GrpcTimeout, _collection._headers._GrpcTimeout);
                        _currentBits ^= 0x4000000L;
                        break;
                    case 27: // Header: "If-Match"
                        Debug.Assert((_currentBits & 0x8000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.IfMatch, _collection._headers._IfMatch);
                        _currentBits ^= 0x8000000L;
                        break;
                    case 28: // Header: "If-Modified-Since"
                        Debug.Assert((_currentBits & 0x10000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.IfModifiedSince, _collection._headers._IfModifiedSince);
                        _currentBits ^= 0x10000000L;
                        break;
                    case 29: // Header: "If-None-Match"
                        Debug.Assert((_currentBits & 0x20000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.IfNoneMatch, _collection._headers._IfNoneMatch);
                        _currentBits ^= 0x20000000L;
                        break;
                    case 30: // Header: "If-Range"
                        Debug.Assert((_currentBits & 0x40000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.IfRange, _collection._headers._IfRange);
                        _currentBits ^= 0x40000000L;
                        break;
                    case 31: // Header: "If-Unmodified-Since"
                        Debug.Assert((_currentBits & 0x80000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.IfUnmodifiedSince, _collection._headers._IfUnmodifiedSince);
                        _currentBits ^= 0x80000000L;
                        break;
                    case 32: // Header: "Keep-Alive"
                        Debug.Assert((_currentBits & 0x100000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.KeepAlive, _collection._headers._KeepAlive);
                        _currentBits ^= 0x100000000L;
                        break;
                    case 33: // Header: "Max-Forwards"
                        Debug.Assert((_currentBits & 0x200000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.MaxForwards, _collection._headers._MaxForwards);
                        _currentBits ^= 0x200000000L;
                        break;
                    case 34: // Header: "Origin"
                        Debug.Assert((_currentBits & 0x400000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Origin, _collection._headers._Origin);
                        _currentBits ^= 0x400000000L;
                        break;
                    case 35: // Header: "Pragma"
                        Debug.Assert((_currentBits & 0x800000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Pragma, _collection._headers._Pragma);
                        _currentBits ^= 0x800000000L;
                        break;
                    case 36: // Header: "Proxy-Authorization"
                        Debug.Assert((_currentBits & 0x1000000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ProxyAuthorization, _collection._headers._ProxyAuthorization);
                        _currentBits ^= 0x1000000000L;
                        break;
                    case 37: // Header: "Range"
                        Debug.Assert((_currentBits & 0x2000000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Range, _collection._headers._Range);
                        _currentBits ^= 0x2000000000L;
                        break;
                    case 38: // Header: "Referer"
                        Debug.Assert((_currentBits & 0x4000000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Referer, _collection._headers._Referer);
                        _currentBits ^= 0x4000000000L;
                        break;
                    case 39: // Header: "Request-Id"
                        Debug.Assert((_currentBits & 0x8000000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.RequestId, _collection._headers._RequestId);
                        _currentBits ^= 0x8000000000L;
                        break;
                    case 40: // Header: "TE"
                        Debug.Assert((_currentBits & 0x10000000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.TE, _collection._headers._TE);
                        _currentBits ^= 0x10000000000L;
                        break;
                    case 41: // Header: "traceparent"
                        Debug.Assert((_currentBits & 0x20000000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.TraceParent, _collection._headers._TraceParent);
                        _currentBits ^= 0x20000000000L;
                        break;
                    case 42: // Header: "tracestate"
                        Debug.Assert((_currentBits & 0x40000000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.TraceState, _collection._headers._TraceState);
                        _currentBits ^= 0x40000000000L;
                        break;
                    case 43: // Header: "Transfer-Encoding"
                        Debug.Assert((_currentBits & 0x80000000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.TransferEncoding, _collection._headers._TransferEncoding);
                        _currentBits ^= 0x80000000000L;
                        break;
                    case 44: // Header: "Translate"
                        Debug.Assert((_currentBits & 0x100000000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Translate, _collection._headers._Translate);
                        _currentBits ^= 0x100000000000L;
                        break;
                    case 45: // Header: "Upgrade"
                        Debug.Assert((_currentBits & 0x200000000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Upgrade, _collection._headers._Upgrade);
                        _currentBits ^= 0x200000000000L;
                        break;
                    case 46: // Header: "Upgrade-Insecure-Requests"
                        Debug.Assert((_currentBits & 0x400000000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.UpgradeInsecureRequests, _collection._headers._UpgradeInsecureRequests);
                        _currentBits ^= 0x400000000000L;
                        break;
                    case 47: // Header: "Via"
                        Debug.Assert((_currentBits & 0x800000000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Via, _collection._headers._Via);
                        _currentBits ^= 0x800000000000L;
                        break;
                    case 48: // Header: "Warning"
                        Debug.Assert((_currentBits & 0x1000000000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Warning, _collection._headers._Warning);
                        _currentBits ^= 0x1000000000000L;
                        break;
                    case 49: // Header: "Content-Length"
                        Debug.Assert(_currentBits == 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ContentLength, HeaderUtilities.FormatNonNegativeInt64(_collection._contentLength.GetValueOrDefault()));
                        _next = -1;
                        return true;
                    default:
                        if (!_hasUnknown || !_unknownEnumerator.MoveNext())
                        {
                            _current = default(KeyValuePair<string, StringValues>);
                            return false;
                        }
                        _current = _unknownEnumerator.Current;
                        return true;
                }

                if (_currentBits != 0)
                {
                    _next = BitOperations.TrailingZeroCount(_currentBits);
                    return true;
                }
                else
                {
                    _next = _collection._contentLength.HasValue ? 49 : -1;
                    return true;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static int GetNext(long bits, bool hasContentLength)
            {
                return bits != 0
                    ? BitOperations.TrailingZeroCount(bits)
                    : hasContentLength
                        ? 49
                        : -1;
            }
        }
    }

    internal partial class HttpResponseHeaders : IHeaderDictionary
    {
        private static ReadOnlySpan<byte> HeaderBytes => [13,10,67,111,110,110,101,99,116,105,111,110,58,32,13,10,67,111,110,116,101,110,116,45,84,121,112,101,58,32,13,10,68,97,116,101,58,32,13,10,83,101,114,118,101,114,58,32,13,10,65,99,99,101,112,116,45,82,97,110,103,101,115,58,32,13,10,65,99,99,101,115,115,45,67,111,110,116,114,111,108,45,65,108,108,111,119,45,67,114,101,100,101,110,116,105,97,108,115,58,32,13,10,65,99,99,101,115,115,45,67,111,110,116,114,111,108,45,65,108,108,111,119,45,72,101,97,100,101,114,115,58,32,13,10,65,99,99,101,115,115,45,67,111,110,116,114,111,108,45,65,108,108,111,119,45,77,101,116,104,111,100,115,58,32,13,10,65,99,99,101,115,115,45,67,111,110,116,114,111,108,45,65,108,108,111,119,45,79,114,105,103,105,110,58,32,13,10,65,99,99,101,115,115,45,67,111,110,116,114,111,108,45,69,120,112,111,115,101,45,72,101,97,100,101,114,115,58,32,13,10,65,99,99,101,115,115,45,67,111,110,116,114,111,108,45,77,97,120,45,65,103,101,58,32,13,10,65,103,101,58,32,13,10,65,108,108,111,119,58,32,13,10,65,108,116,45,83,118,99,58,32,13,10,67,97,99,104,101,45,67,111,110,116,114,111,108,58,32,13,10,67,111,110,116,101,110,116,45,69,110,99,111,100,105,110,103,58,32,13,10,67,111,110,116,101,110,116,45,76,97,110,103,117,97,103,101,58,32,13,10,67,111,110,116,101,110,116,45,76,111,99,97,116,105,111,110,58,32,13,10,67,111,110,116,101,110,116,45,77,68,53,58,32,13,10,67,111,110,116,101,110,116,45,82,97,110,103,101,58,32,13,10,69,84,97,103,58,32,13,10,69,120,112,105,114,101,115,58,32,13,10,71,114,112,99,45,69,110,99,111,100,105,110,103,58,32,13,10,75,101,101,112,45,65,108,105,118,101,58,32,13,10,76,97,115,116,45,77,111,100,105,102,105,101,100,58,32,13,10,76,111,99,97,116,105,111,110,58,32,13,10,80,114,97,103,109,97,58,32,13,10,80,114,111,120,121,45,65,117,116,104,101,110,116,105,99,97,116,101,58,32,13,10,80,114,111,120,121,45,67,111,110,110,101,99,116,105,111,110,58,32,13,10,82,101,116,114,121,45,65,102,116,101,114,58,32,13,10,83,101,116,45,67,111,111,107,105,101,58,32,13,10,84,114,97,105,108,101,114,58,32,13,10,84,114,97,110,115,102,101,114,45,69,110,99,111,100,105,110,103,58,32,13,10,85,112,103,114,97,100,101,58,32,13,10,86,97,114,121,58,32,13,10,86,105,97,58,32,13,10,87,97,114,110,105,110,103,58,32,13,10,87,87,87,45,65,117,116,104,101,110,116,105,99,97,116,101,58,32,13,10,67,111,110,116,101,110,116,45,76,101,110,103,116,104,58,32,];
        private HeaderReferences _headers;

        public bool HasConnection => (_bits & 0x1L) != 0;
        public bool HasDate => (_bits & 0x4L) != 0;
        public bool HasServer => (_bits & 0x8L) != 0;
        public bool HasAltSvc => (_bits & 0x2000L) != 0;
        public bool HasTransferEncoding => (_bits & 0x100000000L) != 0;


        public override StringValues HeaderConnection
        {
            get
            {
                if ((_bits & 0x1L) != 0)
                {
                    return _headers._Connection;
                }
                return StringValues.Empty;
            }
            set
            {
                if (!StringValues.IsNullOrEmpty(value))
                {
                    _bits |= 0x1L;
                    _headers._Connection = value; 
                }
                else
                {
                    _bits &= ~0x1L;
                    _headers._Connection = default; 
                }
                _headers._rawConnection = null;
            }
        }
        public StringValues HeaderAllow
        {
            get
            {
                if ((_bits & 0x1000L) != 0)
                {
                    return _headers._Allow;
                }
                return StringValues.Empty;
            }
            set
            {
                if (!StringValues.IsNullOrEmpty(value))
                {
                    _bits |= 0x1000L;
                    _headers._Allow = value; 
                }
                else
                {
                    _bits &= ~0x1000L;
                    _headers._Allow = default; 
                }
            }
        }
        public StringValues HeaderAltSvc
        {
            get
            {
                if ((_bits & 0x2000L) != 0)
                {
                    return _headers._AltSvc;
                }
                return StringValues.Empty;
            }
            set
            {
                if (!StringValues.IsNullOrEmpty(value))
                {
                    _bits |= 0x2000L;
                    _headers._AltSvc = value; 
                }
                else
                {
                    _bits &= ~0x2000L;
                    _headers._AltSvc = default; 
                }
                _headers._rawAltSvc = null;
            }
        }
        public StringValues HeaderTransferEncoding
        {
            get
            {
                if ((_bits & 0x100000000L) != 0)
                {
                    return _headers._TransferEncoding;
                }
                return StringValues.Empty;
            }
            set
            {
                if (!StringValues.IsNullOrEmpty(value))
                {
                    _bits |= 0x100000000L;
                    _headers._TransferEncoding = value; 
                }
                else
                {
                    _bits &= ~0x100000000L;
                    _headers._TransferEncoding = default; 
                }
                _headers._rawTransferEncoding = null;
            }
        }
        public StringValues HeaderContentLength
        {
            get
            {
                if (_contentLength.HasValue)
                {
                    return new StringValues(HeaderUtilities.FormatNonNegativeInt64(_contentLength.Value));
                }
                return StringValues.Empty;
            }
            set
            {
                _contentLength = ParseContentLength(value.ToString());
            }
        }
        
        StringValues IHeaderDictionary.Connection
        {
            get
            {
                var value = _headers._Connection;
                if ((_bits & 0x1L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x1L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.Connection, value, EncodingSelector);
                    _bits |= flag;
                    _headers._Connection = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._Connection = default;
                }
                    _headers._rawConnection = null;
            }
        }
        StringValues IHeaderDictionary.ContentType
        {
            get
            {
                var value = _headers._ContentType;
                if ((_bits & 0x2L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x2L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.ContentType, value, EncodingSelector);
                    _bits |= flag;
                    _headers._ContentType = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._ContentType = default;
                }
            }
        }
        StringValues IHeaderDictionary.Date
        {
            get
            {
                var value = _headers._Date;
                if ((_bits & 0x4L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x4L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.Date, value, EncodingSelector);
                    _bits |= flag;
                    _headers._Date = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._Date = default;
                }
                    _headers._rawDate = null;
            }
        }
        StringValues IHeaderDictionary.Server
        {
            get
            {
                var value = _headers._Server;
                if ((_bits & 0x8L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x8L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.Server, value, EncodingSelector);
                    _bits |= flag;
                    _headers._Server = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._Server = default;
                }
                    _headers._rawServer = null;
            }
        }
        StringValues IHeaderDictionary.AcceptRanges
        {
            get
            {
                var value = _headers._AcceptRanges;
                if ((_bits & 0x10L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x10L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.AcceptRanges, value, EncodingSelector);
                    _bits |= flag;
                    _headers._AcceptRanges = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._AcceptRanges = default;
                }
            }
        }
        StringValues IHeaderDictionary.AccessControlAllowCredentials
        {
            get
            {
                var value = _headers._AccessControlAllowCredentials;
                if ((_bits & 0x20L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x20L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.AccessControlAllowCredentials, value, EncodingSelector);
                    _bits |= flag;
                    _headers._AccessControlAllowCredentials = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._AccessControlAllowCredentials = default;
                }
            }
        }
        StringValues IHeaderDictionary.AccessControlAllowHeaders
        {
            get
            {
                var value = _headers._AccessControlAllowHeaders;
                if ((_bits & 0x40L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x40L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.AccessControlAllowHeaders, value, EncodingSelector);
                    _bits |= flag;
                    _headers._AccessControlAllowHeaders = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._AccessControlAllowHeaders = default;
                }
            }
        }
        StringValues IHeaderDictionary.AccessControlAllowMethods
        {
            get
            {
                var value = _headers._AccessControlAllowMethods;
                if ((_bits & 0x80L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x80L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.AccessControlAllowMethods, value, EncodingSelector);
                    _bits |= flag;
                    _headers._AccessControlAllowMethods = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._AccessControlAllowMethods = default;
                }
            }
        }
        StringValues IHeaderDictionary.AccessControlAllowOrigin
        {
            get
            {
                var value = _headers._AccessControlAllowOrigin;
                if ((_bits & 0x100L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x100L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.AccessControlAllowOrigin, value, EncodingSelector);
                    _bits |= flag;
                    _headers._AccessControlAllowOrigin = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._AccessControlAllowOrigin = default;
                }
            }
        }
        StringValues IHeaderDictionary.AccessControlExposeHeaders
        {
            get
            {
                var value = _headers._AccessControlExposeHeaders;
                if ((_bits & 0x200L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x200L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.AccessControlExposeHeaders, value, EncodingSelector);
                    _bits |= flag;
                    _headers._AccessControlExposeHeaders = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._AccessControlExposeHeaders = default;
                }
            }
        }
        StringValues IHeaderDictionary.AccessControlMaxAge
        {
            get
            {
                var value = _headers._AccessControlMaxAge;
                if ((_bits & 0x400L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x400L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.AccessControlMaxAge, value, EncodingSelector);
                    _bits |= flag;
                    _headers._AccessControlMaxAge = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._AccessControlMaxAge = default;
                }
            }
        }
        StringValues IHeaderDictionary.Age
        {
            get
            {
                var value = _headers._Age;
                if ((_bits & 0x800L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x800L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.Age, value, EncodingSelector);
                    _bits |= flag;
                    _headers._Age = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._Age = default;
                }
            }
        }
        StringValues IHeaderDictionary.Allow
        {
            get
            {
                var value = _headers._Allow;
                if ((_bits & 0x1000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x1000L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.Allow, value, EncodingSelector);
                    _bits |= flag;
                    _headers._Allow = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._Allow = default;
                }
            }
        }
        StringValues IHeaderDictionary.AltSvc
        {
            get
            {
                var value = _headers._AltSvc;
                if ((_bits & 0x2000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x2000L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.AltSvc, value, EncodingSelector);
                    _bits |= flag;
                    _headers._AltSvc = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._AltSvc = default;
                }
                    _headers._rawAltSvc = null;
            }
        }
        StringValues IHeaderDictionary.CacheControl
        {
            get
            {
                var value = _headers._CacheControl;
                if ((_bits & 0x4000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x4000L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.CacheControl, value, EncodingSelector);
                    _bits |= flag;
                    _headers._CacheControl = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._CacheControl = default;
                }
            }
        }
        StringValues IHeaderDictionary.ContentEncoding
        {
            get
            {
                var value = _headers._ContentEncoding;
                if ((_bits & 0x8000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x8000L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.ContentEncoding, value, EncodingSelector);
                    _bits |= flag;
                    _headers._ContentEncoding = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._ContentEncoding = default;
                }
            }
        }
        StringValues IHeaderDictionary.ContentLanguage
        {
            get
            {
                var value = _headers._ContentLanguage;
                if ((_bits & 0x10000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x10000L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.ContentLanguage, value, EncodingSelector);
                    _bits |= flag;
                    _headers._ContentLanguage = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._ContentLanguage = default;
                }
            }
        }
        StringValues IHeaderDictionary.ContentLocation
        {
            get
            {
                var value = _headers._ContentLocation;
                if ((_bits & 0x20000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x20000L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.ContentLocation, value, EncodingSelector);
                    _bits |= flag;
                    _headers._ContentLocation = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._ContentLocation = default;
                }
            }
        }
        StringValues IHeaderDictionary.ContentMD5
        {
            get
            {
                var value = _headers._ContentMD5;
                if ((_bits & 0x40000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x40000L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.ContentMD5, value, EncodingSelector);
                    _bits |= flag;
                    _headers._ContentMD5 = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._ContentMD5 = default;
                }
            }
        }
        StringValues IHeaderDictionary.ContentRange
        {
            get
            {
                var value = _headers._ContentRange;
                if ((_bits & 0x80000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x80000L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.ContentRange, value, EncodingSelector);
                    _bits |= flag;
                    _headers._ContentRange = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._ContentRange = default;
                }
            }
        }
        StringValues IHeaderDictionary.ETag
        {
            get
            {
                var value = _headers._ETag;
                if ((_bits & 0x100000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x100000L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.ETag, value, EncodingSelector);
                    _bits |= flag;
                    _headers._ETag = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._ETag = default;
                }
            }
        }
        StringValues IHeaderDictionary.Expires
        {
            get
            {
                var value = _headers._Expires;
                if ((_bits & 0x200000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x200000L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.Expires, value, EncodingSelector);
                    _bits |= flag;
                    _headers._Expires = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._Expires = default;
                }
            }
        }
        StringValues IHeaderDictionary.GrpcEncoding
        {
            get
            {
                var value = _headers._GrpcEncoding;
                if ((_bits & 0x400000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x400000L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.GrpcEncoding, value, EncodingSelector);
                    _bits |= flag;
                    _headers._GrpcEncoding = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._GrpcEncoding = default;
                }
            }
        }
        StringValues IHeaderDictionary.KeepAlive
        {
            get
            {
                var value = _headers._KeepAlive;
                if ((_bits & 0x800000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x800000L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.KeepAlive, value, EncodingSelector);
                    _bits |= flag;
                    _headers._KeepAlive = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._KeepAlive = default;
                }
            }
        }
        StringValues IHeaderDictionary.LastModified
        {
            get
            {
                var value = _headers._LastModified;
                if ((_bits & 0x1000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x1000000L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.LastModified, value, EncodingSelector);
                    _bits |= flag;
                    _headers._LastModified = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._LastModified = default;
                }
            }
        }
        StringValues IHeaderDictionary.Location
        {
            get
            {
                var value = _headers._Location;
                if ((_bits & 0x2000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x2000000L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.Location, value, EncodingSelector);
                    _bits |= flag;
                    _headers._Location = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._Location = default;
                }
            }
        }
        StringValues IHeaderDictionary.Pragma
        {
            get
            {
                var value = _headers._Pragma;
                if ((_bits & 0x4000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x4000000L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.Pragma, value, EncodingSelector);
                    _bits |= flag;
                    _headers._Pragma = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._Pragma = default;
                }
            }
        }
        StringValues IHeaderDictionary.ProxyAuthenticate
        {
            get
            {
                var value = _headers._ProxyAuthenticate;
                if ((_bits & 0x8000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x8000000L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.ProxyAuthenticate, value, EncodingSelector);
                    _bits |= flag;
                    _headers._ProxyAuthenticate = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._ProxyAuthenticate = default;
                }
            }
        }
        StringValues IHeaderDictionary.ProxyConnection
        {
            get
            {
                var value = _headers._ProxyConnection;
                if ((_bits & 0x10000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x10000000L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.ProxyConnection, value, EncodingSelector);
                    _bits |= flag;
                    _headers._ProxyConnection = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._ProxyConnection = default;
                }
            }
        }
        StringValues IHeaderDictionary.RetryAfter
        {
            get
            {
                var value = _headers._RetryAfter;
                if ((_bits & 0x20000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x20000000L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.RetryAfter, value, EncodingSelector);
                    _bits |= flag;
                    _headers._RetryAfter = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._RetryAfter = default;
                }
            }
        }
        StringValues IHeaderDictionary.SetCookie
        {
            get
            {
                var value = _headers._SetCookie;
                if ((_bits & 0x40000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x40000000L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.SetCookie, value, EncodingSelector);
                    _bits |= flag;
                    _headers._SetCookie = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._SetCookie = default;
                }
            }
        }
        StringValues IHeaderDictionary.Trailer
        {
            get
            {
                var value = _headers._Trailer;
                if ((_bits & 0x80000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x80000000L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.Trailer, value, EncodingSelector);
                    _bits |= flag;
                    _headers._Trailer = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._Trailer = default;
                }
            }
        }
        StringValues IHeaderDictionary.TransferEncoding
        {
            get
            {
                var value = _headers._TransferEncoding;
                if ((_bits & 0x100000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x100000000L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.TransferEncoding, value, EncodingSelector);
                    _bits |= flag;
                    _headers._TransferEncoding = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._TransferEncoding = default;
                }
                    _headers._rawTransferEncoding = null;
            }
        }
        StringValues IHeaderDictionary.Upgrade
        {
            get
            {
                var value = _headers._Upgrade;
                if ((_bits & 0x200000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x200000000L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.Upgrade, value, EncodingSelector);
                    _bits |= flag;
                    _headers._Upgrade = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._Upgrade = default;
                }
            }
        }
        StringValues IHeaderDictionary.Vary
        {
            get
            {
                var value = _headers._Vary;
                if ((_bits & 0x400000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x400000000L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.Vary, value, EncodingSelector);
                    _bits |= flag;
                    _headers._Vary = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._Vary = default;
                }
            }
        }
        StringValues IHeaderDictionary.Via
        {
            get
            {
                var value = _headers._Via;
                if ((_bits & 0x800000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x800000000L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.Via, value, EncodingSelector);
                    _bits |= flag;
                    _headers._Via = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._Via = default;
                }
            }
        }
        StringValues IHeaderDictionary.Warning
        {
            get
            {
                var value = _headers._Warning;
                if ((_bits & 0x1000000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x1000000000L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.Warning, value, EncodingSelector);
                    _bits |= flag;
                    _headers._Warning = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._Warning = default;
                }
            }
        }
        StringValues IHeaderDictionary.WWWAuthenticate
        {
            get
            {
                var value = _headers._WWWAuthenticate;
                if ((_bits & 0x2000000000L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x2000000000L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.WWWAuthenticate, value, EncodingSelector);
                    _bits |= flag;
                    _headers._WWWAuthenticate = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._WWWAuthenticate = default;
                }
            }
        }
        
        StringValues IHeaderDictionary.Accept
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Accept, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Accept, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Accept, value);
            }
        }
        StringValues IHeaderDictionary.AcceptCharset
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.AcceptCharset, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.AcceptCharset, value, EncodingSelector);
                SetValueUnknown(HeaderNames.AcceptCharset, value);
            }
        }
        StringValues IHeaderDictionary.AcceptEncoding
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.AcceptEncoding, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.AcceptEncoding, value, EncodingSelector);
                SetValueUnknown(HeaderNames.AcceptEncoding, value);
            }
        }
        StringValues IHeaderDictionary.AcceptLanguage
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.AcceptLanguage, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.AcceptLanguage, value, EncodingSelector);
                SetValueUnknown(HeaderNames.AcceptLanguage, value);
            }
        }
        StringValues IHeaderDictionary.AccessControlRequestHeaders
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.AccessControlRequestHeaders, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.AccessControlRequestHeaders, value, EncodingSelector);
                SetValueUnknown(HeaderNames.AccessControlRequestHeaders, value);
            }
        }
        StringValues IHeaderDictionary.AccessControlRequestMethod
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.AccessControlRequestMethod, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.AccessControlRequestMethod, value, EncodingSelector);
                SetValueUnknown(HeaderNames.AccessControlRequestMethod, value);
            }
        }
        StringValues IHeaderDictionary.Authorization
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Authorization, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Authorization, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Authorization, value);
            }
        }
        StringValues IHeaderDictionary.Baggage
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Baggage, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Baggage, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Baggage, value);
            }
        }
        StringValues IHeaderDictionary.ContentDisposition
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.ContentDisposition, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.ContentDisposition, value, EncodingSelector);
                SetValueUnknown(HeaderNames.ContentDisposition, value);
            }
        }
        StringValues IHeaderDictionary.ContentSecurityPolicy
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.ContentSecurityPolicy, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.ContentSecurityPolicy, value, EncodingSelector);
                SetValueUnknown(HeaderNames.ContentSecurityPolicy, value);
            }
        }
        StringValues IHeaderDictionary.ContentSecurityPolicyReportOnly
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.ContentSecurityPolicyReportOnly, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.ContentSecurityPolicyReportOnly, value, EncodingSelector);
                SetValueUnknown(HeaderNames.ContentSecurityPolicyReportOnly, value);
            }
        }
        StringValues IHeaderDictionary.CorrelationContext
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.CorrelationContext, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.CorrelationContext, value, EncodingSelector);
                SetValueUnknown(HeaderNames.CorrelationContext, value);
            }
        }
        StringValues IHeaderDictionary.Cookie
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Cookie, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Cookie, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Cookie, value);
            }
        }
        StringValues IHeaderDictionary.Expect
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Expect, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Expect, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Expect, value);
            }
        }
        StringValues IHeaderDictionary.From
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.From, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.From, value, EncodingSelector);
                SetValueUnknown(HeaderNames.From, value);
            }
        }
        StringValues IHeaderDictionary.GrpcAcceptEncoding
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.GrpcAcceptEncoding, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.GrpcAcceptEncoding, value, EncodingSelector);
                SetValueUnknown(HeaderNames.GrpcAcceptEncoding, value);
            }
        }
        StringValues IHeaderDictionary.GrpcMessage
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.GrpcMessage, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.GrpcMessage, value, EncodingSelector);
                SetValueUnknown(HeaderNames.GrpcMessage, value);
            }
        }
        StringValues IHeaderDictionary.GrpcStatus
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.GrpcStatus, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.GrpcStatus, value, EncodingSelector);
                SetValueUnknown(HeaderNames.GrpcStatus, value);
            }
        }
        StringValues IHeaderDictionary.GrpcTimeout
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.GrpcTimeout, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.GrpcTimeout, value, EncodingSelector);
                SetValueUnknown(HeaderNames.GrpcTimeout, value);
            }
        }
        StringValues IHeaderDictionary.Host
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Host, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Host, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Host, value);
            }
        }
        StringValues IHeaderDictionary.IfMatch
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.IfMatch, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.IfMatch, value, EncodingSelector);
                SetValueUnknown(HeaderNames.IfMatch, value);
            }
        }
        StringValues IHeaderDictionary.IfModifiedSince
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.IfModifiedSince, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.IfModifiedSince, value, EncodingSelector);
                SetValueUnknown(HeaderNames.IfModifiedSince, value);
            }
        }
        StringValues IHeaderDictionary.IfNoneMatch
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.IfNoneMatch, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.IfNoneMatch, value, EncodingSelector);
                SetValueUnknown(HeaderNames.IfNoneMatch, value);
            }
        }
        StringValues IHeaderDictionary.IfRange
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.IfRange, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.IfRange, value, EncodingSelector);
                SetValueUnknown(HeaderNames.IfRange, value);
            }
        }
        StringValues IHeaderDictionary.IfUnmodifiedSince
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.IfUnmodifiedSince, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.IfUnmodifiedSince, value, EncodingSelector);
                SetValueUnknown(HeaderNames.IfUnmodifiedSince, value);
            }
        }
        StringValues IHeaderDictionary.Link
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Link, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Link, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Link, value);
            }
        }
        StringValues IHeaderDictionary.MaxForwards
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.MaxForwards, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.MaxForwards, value, EncodingSelector);
                SetValueUnknown(HeaderNames.MaxForwards, value);
            }
        }
        StringValues IHeaderDictionary.Origin
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Origin, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Origin, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Origin, value);
            }
        }
        StringValues IHeaderDictionary.ProxyAuthorization
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.ProxyAuthorization, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.ProxyAuthorization, value, EncodingSelector);
                SetValueUnknown(HeaderNames.ProxyAuthorization, value);
            }
        }
        StringValues IHeaderDictionary.Range
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Range, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Range, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Range, value);
            }
        }
        StringValues IHeaderDictionary.Referer
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Referer, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Referer, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Referer, value);
            }
        }
        StringValues IHeaderDictionary.RequestId
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.RequestId, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.RequestId, value, EncodingSelector);
                SetValueUnknown(HeaderNames.RequestId, value);
            }
        }
        StringValues IHeaderDictionary.SecWebSocketAccept
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.SecWebSocketAccept, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.SecWebSocketAccept, value, EncodingSelector);
                SetValueUnknown(HeaderNames.SecWebSocketAccept, value);
            }
        }
        StringValues IHeaderDictionary.SecWebSocketKey
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.SecWebSocketKey, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.SecWebSocketKey, value, EncodingSelector);
                SetValueUnknown(HeaderNames.SecWebSocketKey, value);
            }
        }
        StringValues IHeaderDictionary.SecWebSocketProtocol
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.SecWebSocketProtocol, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.SecWebSocketProtocol, value, EncodingSelector);
                SetValueUnknown(HeaderNames.SecWebSocketProtocol, value);
            }
        }
        StringValues IHeaderDictionary.SecWebSocketVersion
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.SecWebSocketVersion, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.SecWebSocketVersion, value, EncodingSelector);
                SetValueUnknown(HeaderNames.SecWebSocketVersion, value);
            }
        }
        StringValues IHeaderDictionary.SecWebSocketExtensions
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.SecWebSocketExtensions, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.SecWebSocketExtensions, value, EncodingSelector);
                SetValueUnknown(HeaderNames.SecWebSocketExtensions, value);
            }
        }
        StringValues IHeaderDictionary.StrictTransportSecurity
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.StrictTransportSecurity, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.StrictTransportSecurity, value, EncodingSelector);
                SetValueUnknown(HeaderNames.StrictTransportSecurity, value);
            }
        }
        StringValues IHeaderDictionary.TE
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.TE, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.TE, value, EncodingSelector);
                SetValueUnknown(HeaderNames.TE, value);
            }
        }
        StringValues IHeaderDictionary.Translate
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Translate, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Translate, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Translate, value);
            }
        }
        StringValues IHeaderDictionary.TraceParent
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.TraceParent, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.TraceParent, value, EncodingSelector);
                SetValueUnknown(HeaderNames.TraceParent, value);
            }
        }
        StringValues IHeaderDictionary.TraceState
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.TraceState, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.TraceState, value, EncodingSelector);
                SetValueUnknown(HeaderNames.TraceState, value);
            }
        }
        StringValues IHeaderDictionary.UpgradeInsecureRequests
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.UpgradeInsecureRequests, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.UpgradeInsecureRequests, value, EncodingSelector);
                SetValueUnknown(HeaderNames.UpgradeInsecureRequests, value);
            }
        }
        StringValues IHeaderDictionary.UserAgent
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.UserAgent, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.UserAgent, value, EncodingSelector);
                SetValueUnknown(HeaderNames.UserAgent, value);
            }
        }
        StringValues IHeaderDictionary.WebSocketSubProtocols
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.WebSocketSubProtocols, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.WebSocketSubProtocols, value, EncodingSelector);
                SetValueUnknown(HeaderNames.WebSocketSubProtocols, value);
            }
        }
        StringValues IHeaderDictionary.XContentTypeOptions
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.XContentTypeOptions, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.XContentTypeOptions, value, EncodingSelector);
                SetValueUnknown(HeaderNames.XContentTypeOptions, value);
            }
        }
        StringValues IHeaderDictionary.XFrameOptions
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.XFrameOptions, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.XFrameOptions, value, EncodingSelector);
                SetValueUnknown(HeaderNames.XFrameOptions, value);
            }
        }
        StringValues IHeaderDictionary.XPoweredBy
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.XPoweredBy, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.XPoweredBy, value, EncodingSelector);
                SetValueUnknown(HeaderNames.XPoweredBy, value);
            }
        }
        StringValues IHeaderDictionary.XRequestedWith
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.XRequestedWith, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.XRequestedWith, value, EncodingSelector);
                SetValueUnknown(HeaderNames.XRequestedWith, value);
            }
        }
        StringValues IHeaderDictionary.XUACompatible
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.XUACompatible, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.XUACompatible, value, EncodingSelector);
                SetValueUnknown(HeaderNames.XUACompatible, value);
            }
        }
        StringValues IHeaderDictionary.XXSSProtection
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.XXSSProtection, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.XXSSProtection, value, EncodingSelector);
                SetValueUnknown(HeaderNames.XXSSProtection, value);
            }
        }

        public void SetRawConnection(StringValues value, byte[] raw)
        {
            _bits |= 0x1L;
            _headers._Connection = value;
            _headers._rawConnection = raw;
        }
        public void SetRawDate(StringValues value, byte[] raw)
        {
            _bits |= 0x4L;
            _headers._Date = value;
            _headers._rawDate = raw;
        }
        public void SetRawServer(StringValues value, byte[] raw)
        {
            _bits |= 0x8L;
            _headers._Server = value;
            _headers._rawServer = raw;
        }
        public void SetRawAltSvc(StringValues value, byte[] raw)
        {
            _bits |= 0x2000L;
            _headers._AltSvc = value;
            _headers._rawAltSvc = raw;
        }
        public void SetRawTransferEncoding(StringValues value, byte[] raw)
        {
            _bits |= 0x100000000L;
            _headers._TransferEncoding = value;
            _headers._rawTransferEncoding = raw;
        }
        protected override int GetCountFast()
        {
            return (_contentLength.HasValue ? 1 : 0 ) + BitOperations.PopCount((ulong)_bits) + (MaybeUnknown?.Count ?? 0);
        }

        protected override bool TryGetValueFast(string key, out StringValues value)
        {
            value = default;
            switch (key.Length)
            {
                case 3:
                {
                    if (ReferenceEquals(HeaderNames.Age, key))
                    {
                        if ((_bits & 0x800L) != 0)
                        {
                            value = _headers._Age;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Via, key))
                    {
                        if ((_bits & 0x800000000L) != 0)
                        {
                            value = _headers._Via;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.Age.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800L) != 0)
                        {
                            value = _headers._Age;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Via.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800000000L) != 0)
                        {
                            value = _headers._Via;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 4:
                {
                    if (ReferenceEquals(HeaderNames.Date, key))
                    {
                        if ((_bits & 0x4L) != 0)
                        {
                            value = _headers._Date;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ETag, key))
                    {
                        if ((_bits & 0x100000L) != 0)
                        {
                            value = _headers._ETag;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Vary, key))
                    {
                        if ((_bits & 0x400000000L) != 0)
                        {
                            value = _headers._Vary;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.Date.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4L) != 0)
                        {
                            value = _headers._Date;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ETag.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100000L) != 0)
                        {
                            value = _headers._ETag;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Vary.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400000000L) != 0)
                        {
                            value = _headers._Vary;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 5:
                {
                    if (ReferenceEquals(HeaderNames.Allow, key))
                    {
                        if ((_bits & 0x1000L) != 0)
                        {
                            value = _headers._Allow;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.Allow.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000L) != 0)
                        {
                            value = _headers._Allow;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 6:
                {
                    if (ReferenceEquals(HeaderNames.Server, key))
                    {
                        if ((_bits & 0x8L) != 0)
                        {
                            value = _headers._Server;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Pragma, key))
                    {
                        if ((_bits & 0x4000000L) != 0)
                        {
                            value = _headers._Pragma;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.Server.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8L) != 0)
                        {
                            value = _headers._Server;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Pragma.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000000L) != 0)
                        {
                            value = _headers._Pragma;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 7:
                {
                    if (ReferenceEquals(HeaderNames.AltSvc, key))
                    {
                        if ((_bits & 0x2000L) != 0)
                        {
                            value = _headers._AltSvc;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Expires, key))
                    {
                        if ((_bits & 0x200000L) != 0)
                        {
                            value = _headers._Expires;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Trailer, key))
                    {
                        if ((_bits & 0x80000000L) != 0)
                        {
                            value = _headers._Trailer;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Upgrade, key))
                    {
                        if ((_bits & 0x200000000L) != 0)
                        {
                            value = _headers._Upgrade;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Warning, key))
                    {
                        if ((_bits & 0x1000000000L) != 0)
                        {
                            value = _headers._Warning;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.AltSvc.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000L) != 0)
                        {
                            value = _headers._AltSvc;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Expires.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200000L) != 0)
                        {
                            value = _headers._Expires;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Trailer.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80000000L) != 0)
                        {
                            value = _headers._Trailer;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Upgrade.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200000000L) != 0)
                        {
                            value = _headers._Upgrade;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Warning.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000000000L) != 0)
                        {
                            value = _headers._Warning;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 8:
                {
                    if (ReferenceEquals(HeaderNames.Location, key))
                    {
                        if ((_bits & 0x2000000L) != 0)
                        {
                            value = _headers._Location;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.Location.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000000L) != 0)
                        {
                            value = _headers._Location;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 10:
                {
                    if (ReferenceEquals(HeaderNames.Connection, key))
                    {
                        if ((_bits & 0x1L) != 0)
                        {
                            value = _headers._Connection;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.KeepAlive, key))
                    {
                        if ((_bits & 0x800000L) != 0)
                        {
                            value = _headers._KeepAlive;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.SetCookie, key))
                    {
                        if ((_bits & 0x40000000L) != 0)
                        {
                            value = _headers._SetCookie;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.Connection.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1L) != 0)
                        {
                            value = _headers._Connection;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.KeepAlive.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800000L) != 0)
                        {
                            value = _headers._KeepAlive;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.SetCookie.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40000000L) != 0)
                        {
                            value = _headers._SetCookie;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 11:
                {
                    if (ReferenceEquals(HeaderNames.ContentMD5, key))
                    {
                        if ((_bits & 0x40000L) != 0)
                        {
                            value = _headers._ContentMD5;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.RetryAfter, key))
                    {
                        if ((_bits & 0x20000000L) != 0)
                        {
                            value = _headers._RetryAfter;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.ContentMD5.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40000L) != 0)
                        {
                            value = _headers._ContentMD5;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.RetryAfter.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20000000L) != 0)
                        {
                            value = _headers._RetryAfter;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 12:
                {
                    if (ReferenceEquals(HeaderNames.ContentType, key))
                    {
                        if ((_bits & 0x2L) != 0)
                        {
                            value = _headers._ContentType;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.ContentType.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2L) != 0)
                        {
                            value = _headers._ContentType;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 13:
                {
                    if (ReferenceEquals(HeaderNames.AcceptRanges, key))
                    {
                        if ((_bits & 0x10L) != 0)
                        {
                            value = _headers._AcceptRanges;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.CacheControl, key))
                    {
                        if ((_bits & 0x4000L) != 0)
                        {
                            value = _headers._CacheControl;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentRange, key))
                    {
                        if ((_bits & 0x80000L) != 0)
                        {
                            value = _headers._ContentRange;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.GrpcEncoding, key))
                    {
                        if ((_bits & 0x400000L) != 0)
                        {
                            value = _headers._GrpcEncoding;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.LastModified, key))
                    {
                        if ((_bits & 0x1000000L) != 0)
                        {
                            value = _headers._LastModified;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.AcceptRanges.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10L) != 0)
                        {
                            value = _headers._AcceptRanges;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.CacheControl.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000L) != 0)
                        {
                            value = _headers._CacheControl;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentRange.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80000L) != 0)
                        {
                            value = _headers._ContentRange;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.GrpcEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400000L) != 0)
                        {
                            value = _headers._GrpcEncoding;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.LastModified.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000000L) != 0)
                        {
                            value = _headers._LastModified;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 14:
                {
                    if (ReferenceEquals(HeaderNames.ContentLength, key))
                    {
                        if (_contentLength.HasValue)
                        {
                            value = HeaderUtilities.FormatNonNegativeInt64(_contentLength.Value);
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.ContentLength.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if (_contentLength.HasValue)
                        {
                            value = HeaderUtilities.FormatNonNegativeInt64(_contentLength.Value);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 16:
                {
                    if (ReferenceEquals(HeaderNames.ContentEncoding, key))
                    {
                        if ((_bits & 0x8000L) != 0)
                        {
                            value = _headers._ContentEncoding;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentLanguage, key))
                    {
                        if ((_bits & 0x10000L) != 0)
                        {
                            value = _headers._ContentLanguage;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentLocation, key))
                    {
                        if ((_bits & 0x20000L) != 0)
                        {
                            value = _headers._ContentLocation;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ProxyConnection, key))
                    {
                        if ((_bits & 0x10000000L) != 0)
                        {
                            value = _headers._ProxyConnection;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.WWWAuthenticate, key))
                    {
                        if ((_bits & 0x2000000000L) != 0)
                        {
                            value = _headers._WWWAuthenticate;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.ContentEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000L) != 0)
                        {
                            value = _headers._ContentEncoding;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentLanguage.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000L) != 0)
                        {
                            value = _headers._ContentLanguage;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentLocation.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20000L) != 0)
                        {
                            value = _headers._ContentLocation;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ProxyConnection.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000000L) != 0)
                        {
                            value = _headers._ProxyConnection;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.WWWAuthenticate.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000000000L) != 0)
                        {
                            value = _headers._WWWAuthenticate;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 17:
                {
                    if (ReferenceEquals(HeaderNames.TransferEncoding, key))
                    {
                        if ((_bits & 0x100000000L) != 0)
                        {
                            value = _headers._TransferEncoding;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.TransferEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100000000L) != 0)
                        {
                            value = _headers._TransferEncoding;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 18:
                {
                    if (ReferenceEquals(HeaderNames.ProxyAuthenticate, key))
                    {
                        if ((_bits & 0x8000000L) != 0)
                        {
                            value = _headers._ProxyAuthenticate;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.ProxyAuthenticate.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000000L) != 0)
                        {
                            value = _headers._ProxyAuthenticate;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 22:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlMaxAge, key))
                    {
                        if ((_bits & 0x400L) != 0)
                        {
                            value = _headers._AccessControlMaxAge;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.AccessControlMaxAge.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400L) != 0)
                        {
                            value = _headers._AccessControlMaxAge;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 27:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlAllowOrigin, key))
                    {
                        if ((_bits & 0x100L) != 0)
                        {
                            value = _headers._AccessControlAllowOrigin;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.AccessControlAllowOrigin.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100L) != 0)
                        {
                            value = _headers._AccessControlAllowOrigin;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 28:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlAllowHeaders, key))
                    {
                        if ((_bits & 0x40L) != 0)
                        {
                            value = _headers._AccessControlAllowHeaders;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.AccessControlAllowMethods, key))
                    {
                        if ((_bits & 0x80L) != 0)
                        {
                            value = _headers._AccessControlAllowMethods;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.AccessControlAllowHeaders.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40L) != 0)
                        {
                            value = _headers._AccessControlAllowHeaders;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.AccessControlAllowMethods.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80L) != 0)
                        {
                            value = _headers._AccessControlAllowMethods;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 29:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlExposeHeaders, key))
                    {
                        if ((_bits & 0x200L) != 0)
                        {
                            value = _headers._AccessControlExposeHeaders;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.AccessControlExposeHeaders.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200L) != 0)
                        {
                            value = _headers._AccessControlExposeHeaders;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 32:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlAllowCredentials, key))
                    {
                        if ((_bits & 0x20L) != 0)
                        {
                            value = _headers._AccessControlAllowCredentials;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.AccessControlAllowCredentials.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20L) != 0)
                        {
                            value = _headers._AccessControlAllowCredentials;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
            }

            return TryGetUnknown(key, ref value);
        }

        protected override void SetValueFast(string key, StringValues value)
        {
            ValidateHeaderValueCharacters(key, value, EncodingSelector);
            switch (key.Length)
            {
                case 3:
                {
                    if (ReferenceEquals(HeaderNames.Age, key))
                    {
                        _bits |= 0x800L;
                        _headers._Age = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Via, key))
                    {
                        _bits |= 0x800000000L;
                        _headers._Via = value;
                        return;
                    }

                    if (HeaderNames.Age.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x800L;
                        _headers._Age = value;
                        return;
                    }
                    if (HeaderNames.Via.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x800000000L;
                        _headers._Via = value;
                        return;
                    }
                    break;
                }
                case 4:
                {
                    if (ReferenceEquals(HeaderNames.Date, key))
                    {
                        _bits |= 0x4L;
                        _headers._Date = value;
                        _headers._rawDate = null;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.ETag, key))
                    {
                        _bits |= 0x100000L;
                        _headers._ETag = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Vary, key))
                    {
                        _bits |= 0x400000000L;
                        _headers._Vary = value;
                        return;
                    }

                    if (HeaderNames.Date.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x4L;
                        _headers._Date = value;
                        _headers._rawDate = null;
                        return;
                    }
                    if (HeaderNames.ETag.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x100000L;
                        _headers._ETag = value;
                        return;
                    }
                    if (HeaderNames.Vary.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x400000000L;
                        _headers._Vary = value;
                        return;
                    }
                    break;
                }
                case 5:
                {
                    if (ReferenceEquals(HeaderNames.Allow, key))
                    {
                        _bits |= 0x1000L;
                        _headers._Allow = value;
                        return;
                    }

                    if (HeaderNames.Allow.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x1000L;
                        _headers._Allow = value;
                        return;
                    }
                    break;
                }
                case 6:
                {
                    if (ReferenceEquals(HeaderNames.Server, key))
                    {
                        _bits |= 0x8L;
                        _headers._Server = value;
                        _headers._rawServer = null;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Pragma, key))
                    {
                        _bits |= 0x4000000L;
                        _headers._Pragma = value;
                        return;
                    }

                    if (HeaderNames.Server.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x8L;
                        _headers._Server = value;
                        _headers._rawServer = null;
                        return;
                    }
                    if (HeaderNames.Pragma.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x4000000L;
                        _headers._Pragma = value;
                        return;
                    }
                    break;
                }
                case 7:
                {
                    if (ReferenceEquals(HeaderNames.AltSvc, key))
                    {
                        _bits |= 0x2000L;
                        _headers._AltSvc = value;
                        _headers._rawAltSvc = null;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Expires, key))
                    {
                        _bits |= 0x200000L;
                        _headers._Expires = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Trailer, key))
                    {
                        _bits |= 0x80000000L;
                        _headers._Trailer = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Upgrade, key))
                    {
                        _bits |= 0x200000000L;
                        _headers._Upgrade = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Warning, key))
                    {
                        _bits |= 0x1000000000L;
                        _headers._Warning = value;
                        return;
                    }

                    if (HeaderNames.AltSvc.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x2000L;
                        _headers._AltSvc = value;
                        _headers._rawAltSvc = null;
                        return;
                    }
                    if (HeaderNames.Expires.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x200000L;
                        _headers._Expires = value;
                        return;
                    }
                    if (HeaderNames.Trailer.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x80000000L;
                        _headers._Trailer = value;
                        return;
                    }
                    if (HeaderNames.Upgrade.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x200000000L;
                        _headers._Upgrade = value;
                        return;
                    }
                    if (HeaderNames.Warning.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x1000000000L;
                        _headers._Warning = value;
                        return;
                    }
                    break;
                }
                case 8:
                {
                    if (ReferenceEquals(HeaderNames.Location, key))
                    {
                        _bits |= 0x2000000L;
                        _headers._Location = value;
                        return;
                    }

                    if (HeaderNames.Location.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x2000000L;
                        _headers._Location = value;
                        return;
                    }
                    break;
                }
                case 10:
                {
                    if (ReferenceEquals(HeaderNames.Connection, key))
                    {
                        _bits |= 0x1L;
                        _headers._Connection = value;
                        _headers._rawConnection = null;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.KeepAlive, key))
                    {
                        _bits |= 0x800000L;
                        _headers._KeepAlive = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.SetCookie, key))
                    {
                        _bits |= 0x40000000L;
                        _headers._SetCookie = value;
                        return;
                    }

                    if (HeaderNames.Connection.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x1L;
                        _headers._Connection = value;
                        _headers._rawConnection = null;
                        return;
                    }
                    if (HeaderNames.KeepAlive.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x800000L;
                        _headers._KeepAlive = value;
                        return;
                    }
                    if (HeaderNames.SetCookie.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x40000000L;
                        _headers._SetCookie = value;
                        return;
                    }
                    break;
                }
                case 11:
                {
                    if (ReferenceEquals(HeaderNames.ContentMD5, key))
                    {
                        _bits |= 0x40000L;
                        _headers._ContentMD5 = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.RetryAfter, key))
                    {
                        _bits |= 0x20000000L;
                        _headers._RetryAfter = value;
                        return;
                    }

                    if (HeaderNames.ContentMD5.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x40000L;
                        _headers._ContentMD5 = value;
                        return;
                    }
                    if (HeaderNames.RetryAfter.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x20000000L;
                        _headers._RetryAfter = value;
                        return;
                    }
                    break;
                }
                case 12:
                {
                    if (ReferenceEquals(HeaderNames.ContentType, key))
                    {
                        _bits |= 0x2L;
                        _headers._ContentType = value;
                        return;
                    }

                    if (HeaderNames.ContentType.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x2L;
                        _headers._ContentType = value;
                        return;
                    }
                    break;
                }
                case 13:
                {
                    if (ReferenceEquals(HeaderNames.AcceptRanges, key))
                    {
                        _bits |= 0x10L;
                        _headers._AcceptRanges = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.CacheControl, key))
                    {
                        _bits |= 0x4000L;
                        _headers._CacheControl = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.ContentRange, key))
                    {
                        _bits |= 0x80000L;
                        _headers._ContentRange = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.GrpcEncoding, key))
                    {
                        _bits |= 0x400000L;
                        _headers._GrpcEncoding = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.LastModified, key))
                    {
                        _bits |= 0x1000000L;
                        _headers._LastModified = value;
                        return;
                    }

                    if (HeaderNames.AcceptRanges.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x10L;
                        _headers._AcceptRanges = value;
                        return;
                    }
                    if (HeaderNames.CacheControl.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x4000L;
                        _headers._CacheControl = value;
                        return;
                    }
                    if (HeaderNames.ContentRange.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x80000L;
                        _headers._ContentRange = value;
                        return;
                    }
                    if (HeaderNames.GrpcEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x400000L;
                        _headers._GrpcEncoding = value;
                        return;
                    }
                    if (HeaderNames.LastModified.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x1000000L;
                        _headers._LastModified = value;
                        return;
                    }
                    break;
                }
                case 14:
                {
                    if (ReferenceEquals(HeaderNames.ContentLength, key))
                    {
                        _contentLength = ParseContentLength(value.ToString());
                        return;
                    }

                    if (HeaderNames.ContentLength.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _contentLength = ParseContentLength(value.ToString());
                        return;
                    }
                    break;
                }
                case 16:
                {
                    if (ReferenceEquals(HeaderNames.ContentEncoding, key))
                    {
                        _bits |= 0x8000L;
                        _headers._ContentEncoding = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.ContentLanguage, key))
                    {
                        _bits |= 0x10000L;
                        _headers._ContentLanguage = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.ContentLocation, key))
                    {
                        _bits |= 0x20000L;
                        _headers._ContentLocation = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.ProxyConnection, key))
                    {
                        _bits |= 0x10000000L;
                        _headers._ProxyConnection = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.WWWAuthenticate, key))
                    {
                        _bits |= 0x2000000000L;
                        _headers._WWWAuthenticate = value;
                        return;
                    }

                    if (HeaderNames.ContentEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x8000L;
                        _headers._ContentEncoding = value;
                        return;
                    }
                    if (HeaderNames.ContentLanguage.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x10000L;
                        _headers._ContentLanguage = value;
                        return;
                    }
                    if (HeaderNames.ContentLocation.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x20000L;
                        _headers._ContentLocation = value;
                        return;
                    }
                    if (HeaderNames.ProxyConnection.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x10000000L;
                        _headers._ProxyConnection = value;
                        return;
                    }
                    if (HeaderNames.WWWAuthenticate.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x2000000000L;
                        _headers._WWWAuthenticate = value;
                        return;
                    }
                    break;
                }
                case 17:
                {
                    if (ReferenceEquals(HeaderNames.TransferEncoding, key))
                    {
                        _bits |= 0x100000000L;
                        _headers._TransferEncoding = value;
                        _headers._rawTransferEncoding = null;
                        return;
                    }

                    if (HeaderNames.TransferEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x100000000L;
                        _headers._TransferEncoding = value;
                        _headers._rawTransferEncoding = null;
                        return;
                    }
                    break;
                }
                case 18:
                {
                    if (ReferenceEquals(HeaderNames.ProxyAuthenticate, key))
                    {
                        _bits |= 0x8000000L;
                        _headers._ProxyAuthenticate = value;
                        return;
                    }

                    if (HeaderNames.ProxyAuthenticate.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x8000000L;
                        _headers._ProxyAuthenticate = value;
                        return;
                    }
                    break;
                }
                case 22:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlMaxAge, key))
                    {
                        _bits |= 0x400L;
                        _headers._AccessControlMaxAge = value;
                        return;
                    }

                    if (HeaderNames.AccessControlMaxAge.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x400L;
                        _headers._AccessControlMaxAge = value;
                        return;
                    }
                    break;
                }
                case 27:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlAllowOrigin, key))
                    {
                        _bits |= 0x100L;
                        _headers._AccessControlAllowOrigin = value;
                        return;
                    }

                    if (HeaderNames.AccessControlAllowOrigin.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x100L;
                        _headers._AccessControlAllowOrigin = value;
                        return;
                    }
                    break;
                }
                case 28:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlAllowHeaders, key))
                    {
                        _bits |= 0x40L;
                        _headers._AccessControlAllowHeaders = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.AccessControlAllowMethods, key))
                    {
                        _bits |= 0x80L;
                        _headers._AccessControlAllowMethods = value;
                        return;
                    }

                    if (HeaderNames.AccessControlAllowHeaders.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x40L;
                        _headers._AccessControlAllowHeaders = value;
                        return;
                    }
                    if (HeaderNames.AccessControlAllowMethods.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x80L;
                        _headers._AccessControlAllowMethods = value;
                        return;
                    }
                    break;
                }
                case 29:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlExposeHeaders, key))
                    {
                        _bits |= 0x200L;
                        _headers._AccessControlExposeHeaders = value;
                        return;
                    }

                    if (HeaderNames.AccessControlExposeHeaders.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x200L;
                        _headers._AccessControlExposeHeaders = value;
                        return;
                    }
                    break;
                }
                case 32:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlAllowCredentials, key))
                    {
                        _bits |= 0x20L;
                        _headers._AccessControlAllowCredentials = value;
                        return;
                    }

                    if (HeaderNames.AccessControlAllowCredentials.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x20L;
                        _headers._AccessControlAllowCredentials = value;
                        return;
                    }
                    break;
                }
            }

            SetValueUnknown(key, value);
        }

        protected override bool AddValueFast(string key, StringValues value)
        {
            ValidateHeaderValueCharacters(key, value, EncodingSelector);
            switch (key.Length)
            {
                case 3:
                {
                    if (ReferenceEquals(HeaderNames.Age, key))
                    {
                        if ((_bits & 0x800L) == 0)
                        {
                            _bits |= 0x800L;
                            _headers._Age = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Via, key))
                    {
                        if ((_bits & 0x800000000L) == 0)
                        {
                            _bits |= 0x800000000L;
                            _headers._Via = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Age.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800L) == 0)
                        {
                            _bits |= 0x800L;
                            _headers._Age = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Via.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800000000L) == 0)
                        {
                            _bits |= 0x800000000L;
                            _headers._Via = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 4:
                {
                    if (ReferenceEquals(HeaderNames.Date, key))
                    {
                        if ((_bits & 0x4L) == 0)
                        {
                            _bits |= 0x4L;
                            _headers._Date = value;
                            _headers._rawDate = null;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ETag, key))
                    {
                        if ((_bits & 0x100000L) == 0)
                        {
                            _bits |= 0x100000L;
                            _headers._ETag = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Vary, key))
                    {
                        if ((_bits & 0x400000000L) == 0)
                        {
                            _bits |= 0x400000000L;
                            _headers._Vary = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Date.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4L) == 0)
                        {
                            _bits |= 0x4L;
                            _headers._Date = value;
                            _headers._rawDate = null;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ETag.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100000L) == 0)
                        {
                            _bits |= 0x100000L;
                            _headers._ETag = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Vary.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400000000L) == 0)
                        {
                            _bits |= 0x400000000L;
                            _headers._Vary = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 5:
                {
                    if (ReferenceEquals(HeaderNames.Allow, key))
                    {
                        if ((_bits & 0x1000L) == 0)
                        {
                            _bits |= 0x1000L;
                            _headers._Allow = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Allow.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000L) == 0)
                        {
                            _bits |= 0x1000L;
                            _headers._Allow = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 6:
                {
                    if (ReferenceEquals(HeaderNames.Server, key))
                    {
                        if ((_bits & 0x8L) == 0)
                        {
                            _bits |= 0x8L;
                            _headers._Server = value;
                            _headers._rawServer = null;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Pragma, key))
                    {
                        if ((_bits & 0x4000000L) == 0)
                        {
                            _bits |= 0x4000000L;
                            _headers._Pragma = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Server.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8L) == 0)
                        {
                            _bits |= 0x8L;
                            _headers._Server = value;
                            _headers._rawServer = null;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Pragma.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000000L) == 0)
                        {
                            _bits |= 0x4000000L;
                            _headers._Pragma = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 7:
                {
                    if (ReferenceEquals(HeaderNames.AltSvc, key))
                    {
                        if ((_bits & 0x2000L) == 0)
                        {
                            _bits |= 0x2000L;
                            _headers._AltSvc = value;
                            _headers._rawAltSvc = null;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Expires, key))
                    {
                        if ((_bits & 0x200000L) == 0)
                        {
                            _bits |= 0x200000L;
                            _headers._Expires = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Trailer, key))
                    {
                        if ((_bits & 0x80000000L) == 0)
                        {
                            _bits |= 0x80000000L;
                            _headers._Trailer = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Upgrade, key))
                    {
                        if ((_bits & 0x200000000L) == 0)
                        {
                            _bits |= 0x200000000L;
                            _headers._Upgrade = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Warning, key))
                    {
                        if ((_bits & 0x1000000000L) == 0)
                        {
                            _bits |= 0x1000000000L;
                            _headers._Warning = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AltSvc.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000L) == 0)
                        {
                            _bits |= 0x2000L;
                            _headers._AltSvc = value;
                            _headers._rawAltSvc = null;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Expires.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200000L) == 0)
                        {
                            _bits |= 0x200000L;
                            _headers._Expires = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Trailer.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80000000L) == 0)
                        {
                            _bits |= 0x80000000L;
                            _headers._Trailer = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Upgrade.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200000000L) == 0)
                        {
                            _bits |= 0x200000000L;
                            _headers._Upgrade = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Warning.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000000000L) == 0)
                        {
                            _bits |= 0x1000000000L;
                            _headers._Warning = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 8:
                {
                    if (ReferenceEquals(HeaderNames.Location, key))
                    {
                        if ((_bits & 0x2000000L) == 0)
                        {
                            _bits |= 0x2000000L;
                            _headers._Location = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Location.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000000L) == 0)
                        {
                            _bits |= 0x2000000L;
                            _headers._Location = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 10:
                {
                    if (ReferenceEquals(HeaderNames.Connection, key))
                    {
                        if ((_bits & 0x1L) == 0)
                        {
                            _bits |= 0x1L;
                            _headers._Connection = value;
                            _headers._rawConnection = null;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.KeepAlive, key))
                    {
                        if ((_bits & 0x800000L) == 0)
                        {
                            _bits |= 0x800000L;
                            _headers._KeepAlive = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.SetCookie, key))
                    {
                        if ((_bits & 0x40000000L) == 0)
                        {
                            _bits |= 0x40000000L;
                            _headers._SetCookie = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Connection.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1L) == 0)
                        {
                            _bits |= 0x1L;
                            _headers._Connection = value;
                            _headers._rawConnection = null;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.KeepAlive.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800000L) == 0)
                        {
                            _bits |= 0x800000L;
                            _headers._KeepAlive = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.SetCookie.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40000000L) == 0)
                        {
                            _bits |= 0x40000000L;
                            _headers._SetCookie = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 11:
                {
                    if (ReferenceEquals(HeaderNames.ContentMD5, key))
                    {
                        if ((_bits & 0x40000L) == 0)
                        {
                            _bits |= 0x40000L;
                            _headers._ContentMD5 = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.RetryAfter, key))
                    {
                        if ((_bits & 0x20000000L) == 0)
                        {
                            _bits |= 0x20000000L;
                            _headers._RetryAfter = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.ContentMD5.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40000L) == 0)
                        {
                            _bits |= 0x40000L;
                            _headers._ContentMD5 = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.RetryAfter.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20000000L) == 0)
                        {
                            _bits |= 0x20000000L;
                            _headers._RetryAfter = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 12:
                {
                    if (ReferenceEquals(HeaderNames.ContentType, key))
                    {
                        if ((_bits & 0x2L) == 0)
                        {
                            _bits |= 0x2L;
                            _headers._ContentType = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.ContentType.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2L) == 0)
                        {
                            _bits |= 0x2L;
                            _headers._ContentType = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 13:
                {
                    if (ReferenceEquals(HeaderNames.AcceptRanges, key))
                    {
                        if ((_bits & 0x10L) == 0)
                        {
                            _bits |= 0x10L;
                            _headers._AcceptRanges = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.CacheControl, key))
                    {
                        if ((_bits & 0x4000L) == 0)
                        {
                            _bits |= 0x4000L;
                            _headers._CacheControl = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentRange, key))
                    {
                        if ((_bits & 0x80000L) == 0)
                        {
                            _bits |= 0x80000L;
                            _headers._ContentRange = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.GrpcEncoding, key))
                    {
                        if ((_bits & 0x400000L) == 0)
                        {
                            _bits |= 0x400000L;
                            _headers._GrpcEncoding = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.LastModified, key))
                    {
                        if ((_bits & 0x1000000L) == 0)
                        {
                            _bits |= 0x1000000L;
                            _headers._LastModified = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AcceptRanges.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10L) == 0)
                        {
                            _bits |= 0x10L;
                            _headers._AcceptRanges = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.CacheControl.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000L) == 0)
                        {
                            _bits |= 0x4000L;
                            _headers._CacheControl = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentRange.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80000L) == 0)
                        {
                            _bits |= 0x80000L;
                            _headers._ContentRange = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.GrpcEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400000L) == 0)
                        {
                            _bits |= 0x400000L;
                            _headers._GrpcEncoding = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.LastModified.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000000L) == 0)
                        {
                            _bits |= 0x1000000L;
                            _headers._LastModified = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 14:
                {
                    if (ReferenceEquals(HeaderNames.ContentLength, key))
                    {
                        if (!_contentLength.HasValue)
                        {
                            _contentLength = ParseContentLength(value.ToString());
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.ContentLength.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!_contentLength.HasValue)
                        {
                            _contentLength = ParseContentLength(value.ToString());
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 16:
                {
                    if (ReferenceEquals(HeaderNames.ContentEncoding, key))
                    {
                        if ((_bits & 0x8000L) == 0)
                        {
                            _bits |= 0x8000L;
                            _headers._ContentEncoding = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentLanguage, key))
                    {
                        if ((_bits & 0x10000L) == 0)
                        {
                            _bits |= 0x10000L;
                            _headers._ContentLanguage = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentLocation, key))
                    {
                        if ((_bits & 0x20000L) == 0)
                        {
                            _bits |= 0x20000L;
                            _headers._ContentLocation = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ProxyConnection, key))
                    {
                        if ((_bits & 0x10000000L) == 0)
                        {
                            _bits |= 0x10000000L;
                            _headers._ProxyConnection = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.WWWAuthenticate, key))
                    {
                        if ((_bits & 0x2000000000L) == 0)
                        {
                            _bits |= 0x2000000000L;
                            _headers._WWWAuthenticate = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.ContentEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000L) == 0)
                        {
                            _bits |= 0x8000L;
                            _headers._ContentEncoding = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentLanguage.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000L) == 0)
                        {
                            _bits |= 0x10000L;
                            _headers._ContentLanguage = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentLocation.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20000L) == 0)
                        {
                            _bits |= 0x20000L;
                            _headers._ContentLocation = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ProxyConnection.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000000L) == 0)
                        {
                            _bits |= 0x10000000L;
                            _headers._ProxyConnection = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.WWWAuthenticate.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000000000L) == 0)
                        {
                            _bits |= 0x2000000000L;
                            _headers._WWWAuthenticate = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 17:
                {
                    if (ReferenceEquals(HeaderNames.TransferEncoding, key))
                    {
                        if ((_bits & 0x100000000L) == 0)
                        {
                            _bits |= 0x100000000L;
                            _headers._TransferEncoding = value;
                            _headers._rawTransferEncoding = null;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.TransferEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100000000L) == 0)
                        {
                            _bits |= 0x100000000L;
                            _headers._TransferEncoding = value;
                            _headers._rawTransferEncoding = null;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 18:
                {
                    if (ReferenceEquals(HeaderNames.ProxyAuthenticate, key))
                    {
                        if ((_bits & 0x8000000L) == 0)
                        {
                            _bits |= 0x8000000L;
                            _headers._ProxyAuthenticate = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.ProxyAuthenticate.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000000L) == 0)
                        {
                            _bits |= 0x8000000L;
                            _headers._ProxyAuthenticate = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 22:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlMaxAge, key))
                    {
                        if ((_bits & 0x400L) == 0)
                        {
                            _bits |= 0x400L;
                            _headers._AccessControlMaxAge = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AccessControlMaxAge.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400L) == 0)
                        {
                            _bits |= 0x400L;
                            _headers._AccessControlMaxAge = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 27:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlAllowOrigin, key))
                    {
                        if ((_bits & 0x100L) == 0)
                        {
                            _bits |= 0x100L;
                            _headers._AccessControlAllowOrigin = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AccessControlAllowOrigin.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100L) == 0)
                        {
                            _bits |= 0x100L;
                            _headers._AccessControlAllowOrigin = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 28:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlAllowHeaders, key))
                    {
                        if ((_bits & 0x40L) == 0)
                        {
                            _bits |= 0x40L;
                            _headers._AccessControlAllowHeaders = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.AccessControlAllowMethods, key))
                    {
                        if ((_bits & 0x80L) == 0)
                        {
                            _bits |= 0x80L;
                            _headers._AccessControlAllowMethods = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AccessControlAllowHeaders.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40L) == 0)
                        {
                            _bits |= 0x40L;
                            _headers._AccessControlAllowHeaders = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.AccessControlAllowMethods.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80L) == 0)
                        {
                            _bits |= 0x80L;
                            _headers._AccessControlAllowMethods = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 29:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlExposeHeaders, key))
                    {
                        if ((_bits & 0x200L) == 0)
                        {
                            _bits |= 0x200L;
                            _headers._AccessControlExposeHeaders = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AccessControlExposeHeaders.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200L) == 0)
                        {
                            _bits |= 0x200L;
                            _headers._AccessControlExposeHeaders = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 32:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlAllowCredentials, key))
                    {
                        if ((_bits & 0x20L) == 0)
                        {
                            _bits |= 0x20L;
                            _headers._AccessControlAllowCredentials = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AccessControlAllowCredentials.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20L) == 0)
                        {
                            _bits |= 0x20L;
                            _headers._AccessControlAllowCredentials = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
            }

            return AddValueUnknown(key, value);
        }

        protected override bool RemoveFast(string key)
        {
            switch (key.Length)
            {
                case 3:
                {
                    if (ReferenceEquals(HeaderNames.Age, key))
                    {
                        if ((_bits & 0x800L) != 0)
                        {
                            _bits &= ~0x800L;
                            _headers._Age = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Via, key))
                    {
                        if ((_bits & 0x800000000L) != 0)
                        {
                            _bits &= ~0x800000000L;
                            _headers._Via = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Age.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800L) != 0)
                        {
                            _bits &= ~0x800L;
                            _headers._Age = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Via.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800000000L) != 0)
                        {
                            _bits &= ~0x800000000L;
                            _headers._Via = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 4:
                {
                    if (ReferenceEquals(HeaderNames.Date, key))
                    {
                        if ((_bits & 0x4L) != 0)
                        {
                            _bits &= ~0x4L;
                            _headers._Date = default(StringValues);
                            _headers._rawDate = null;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ETag, key))
                    {
                        if ((_bits & 0x100000L) != 0)
                        {
                            _bits &= ~0x100000L;
                            _headers._ETag = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Vary, key))
                    {
                        if ((_bits & 0x400000000L) != 0)
                        {
                            _bits &= ~0x400000000L;
                            _headers._Vary = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Date.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4L) != 0)
                        {
                            _bits &= ~0x4L;
                            _headers._Date = default(StringValues);
                            _headers._rawDate = null;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ETag.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100000L) != 0)
                        {
                            _bits &= ~0x100000L;
                            _headers._ETag = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Vary.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400000000L) != 0)
                        {
                            _bits &= ~0x400000000L;
                            _headers._Vary = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 5:
                {
                    if (ReferenceEquals(HeaderNames.Allow, key))
                    {
                        if ((_bits & 0x1000L) != 0)
                        {
                            _bits &= ~0x1000L;
                            _headers._Allow = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Allow.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000L) != 0)
                        {
                            _bits &= ~0x1000L;
                            _headers._Allow = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 6:
                {
                    if (ReferenceEquals(HeaderNames.Server, key))
                    {
                        if ((_bits & 0x8L) != 0)
                        {
                            _bits &= ~0x8L;
                            _headers._Server = default(StringValues);
                            _headers._rawServer = null;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Pragma, key))
                    {
                        if ((_bits & 0x4000000L) != 0)
                        {
                            _bits &= ~0x4000000L;
                            _headers._Pragma = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Server.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8L) != 0)
                        {
                            _bits &= ~0x8L;
                            _headers._Server = default(StringValues);
                            _headers._rawServer = null;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Pragma.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000000L) != 0)
                        {
                            _bits &= ~0x4000000L;
                            _headers._Pragma = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 7:
                {
                    if (ReferenceEquals(HeaderNames.AltSvc, key))
                    {
                        if ((_bits & 0x2000L) != 0)
                        {
                            _bits &= ~0x2000L;
                            _headers._AltSvc = default(StringValues);
                            _headers._rawAltSvc = null;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Expires, key))
                    {
                        if ((_bits & 0x200000L) != 0)
                        {
                            _bits &= ~0x200000L;
                            _headers._Expires = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Trailer, key))
                    {
                        if ((_bits & 0x80000000L) != 0)
                        {
                            _bits &= ~0x80000000L;
                            _headers._Trailer = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Upgrade, key))
                    {
                        if ((_bits & 0x200000000L) != 0)
                        {
                            _bits &= ~0x200000000L;
                            _headers._Upgrade = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Warning, key))
                    {
                        if ((_bits & 0x1000000000L) != 0)
                        {
                            _bits &= ~0x1000000000L;
                            _headers._Warning = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AltSvc.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000L) != 0)
                        {
                            _bits &= ~0x2000L;
                            _headers._AltSvc = default(StringValues);
                            _headers._rawAltSvc = null;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Expires.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200000L) != 0)
                        {
                            _bits &= ~0x200000L;
                            _headers._Expires = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Trailer.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80000000L) != 0)
                        {
                            _bits &= ~0x80000000L;
                            _headers._Trailer = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Upgrade.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200000000L) != 0)
                        {
                            _bits &= ~0x200000000L;
                            _headers._Upgrade = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Warning.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000000000L) != 0)
                        {
                            _bits &= ~0x1000000000L;
                            _headers._Warning = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 8:
                {
                    if (ReferenceEquals(HeaderNames.Location, key))
                    {
                        if ((_bits & 0x2000000L) != 0)
                        {
                            _bits &= ~0x2000000L;
                            _headers._Location = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Location.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000000L) != 0)
                        {
                            _bits &= ~0x2000000L;
                            _headers._Location = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 10:
                {
                    if (ReferenceEquals(HeaderNames.Connection, key))
                    {
                        if ((_bits & 0x1L) != 0)
                        {
                            _bits &= ~0x1L;
                            _headers._Connection = default(StringValues);
                            _headers._rawConnection = null;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.KeepAlive, key))
                    {
                        if ((_bits & 0x800000L) != 0)
                        {
                            _bits &= ~0x800000L;
                            _headers._KeepAlive = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.SetCookie, key))
                    {
                        if ((_bits & 0x40000000L) != 0)
                        {
                            _bits &= ~0x40000000L;
                            _headers._SetCookie = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Connection.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1L) != 0)
                        {
                            _bits &= ~0x1L;
                            _headers._Connection = default(StringValues);
                            _headers._rawConnection = null;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.KeepAlive.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800000L) != 0)
                        {
                            _bits &= ~0x800000L;
                            _headers._KeepAlive = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.SetCookie.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40000000L) != 0)
                        {
                            _bits &= ~0x40000000L;
                            _headers._SetCookie = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 11:
                {
                    if (ReferenceEquals(HeaderNames.ContentMD5, key))
                    {
                        if ((_bits & 0x40000L) != 0)
                        {
                            _bits &= ~0x40000L;
                            _headers._ContentMD5 = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.RetryAfter, key))
                    {
                        if ((_bits & 0x20000000L) != 0)
                        {
                            _bits &= ~0x20000000L;
                            _headers._RetryAfter = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.ContentMD5.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40000L) != 0)
                        {
                            _bits &= ~0x40000L;
                            _headers._ContentMD5 = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.RetryAfter.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20000000L) != 0)
                        {
                            _bits &= ~0x20000000L;
                            _headers._RetryAfter = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 12:
                {
                    if (ReferenceEquals(HeaderNames.ContentType, key))
                    {
                        if ((_bits & 0x2L) != 0)
                        {
                            _bits &= ~0x2L;
                            _headers._ContentType = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.ContentType.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2L) != 0)
                        {
                            _bits &= ~0x2L;
                            _headers._ContentType = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 13:
                {
                    if (ReferenceEquals(HeaderNames.AcceptRanges, key))
                    {
                        if ((_bits & 0x10L) != 0)
                        {
                            _bits &= ~0x10L;
                            _headers._AcceptRanges = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.CacheControl, key))
                    {
                        if ((_bits & 0x4000L) != 0)
                        {
                            _bits &= ~0x4000L;
                            _headers._CacheControl = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentRange, key))
                    {
                        if ((_bits & 0x80000L) != 0)
                        {
                            _bits &= ~0x80000L;
                            _headers._ContentRange = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.GrpcEncoding, key))
                    {
                        if ((_bits & 0x400000L) != 0)
                        {
                            _bits &= ~0x400000L;
                            _headers._GrpcEncoding = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.LastModified, key))
                    {
                        if ((_bits & 0x1000000L) != 0)
                        {
                            _bits &= ~0x1000000L;
                            _headers._LastModified = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AcceptRanges.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10L) != 0)
                        {
                            _bits &= ~0x10L;
                            _headers._AcceptRanges = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.CacheControl.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000L) != 0)
                        {
                            _bits &= ~0x4000L;
                            _headers._CacheControl = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentRange.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80000L) != 0)
                        {
                            _bits &= ~0x80000L;
                            _headers._ContentRange = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.GrpcEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400000L) != 0)
                        {
                            _bits &= ~0x400000L;
                            _headers._GrpcEncoding = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.LastModified.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000000L) != 0)
                        {
                            _bits &= ~0x1000000L;
                            _headers._LastModified = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 14:
                {
                    if (ReferenceEquals(HeaderNames.ContentLength, key))
                    {
                        if (_contentLength.HasValue)
                        {
                            _contentLength = null;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.ContentLength.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if (_contentLength.HasValue)
                        {
                            _contentLength = null;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 16:
                {
                    if (ReferenceEquals(HeaderNames.ContentEncoding, key))
                    {
                        if ((_bits & 0x8000L) != 0)
                        {
                            _bits &= ~0x8000L;
                            _headers._ContentEncoding = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentLanguage, key))
                    {
                        if ((_bits & 0x10000L) != 0)
                        {
                            _bits &= ~0x10000L;
                            _headers._ContentLanguage = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentLocation, key))
                    {
                        if ((_bits & 0x20000L) != 0)
                        {
                            _bits &= ~0x20000L;
                            _headers._ContentLocation = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ProxyConnection, key))
                    {
                        if ((_bits & 0x10000000L) != 0)
                        {
                            _bits &= ~0x10000000L;
                            _headers._ProxyConnection = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.WWWAuthenticate, key))
                    {
                        if ((_bits & 0x2000000000L) != 0)
                        {
                            _bits &= ~0x2000000000L;
                            _headers._WWWAuthenticate = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.ContentEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000L) != 0)
                        {
                            _bits &= ~0x8000L;
                            _headers._ContentEncoding = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentLanguage.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000L) != 0)
                        {
                            _bits &= ~0x10000L;
                            _headers._ContentLanguage = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentLocation.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20000L) != 0)
                        {
                            _bits &= ~0x20000L;
                            _headers._ContentLocation = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ProxyConnection.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000000L) != 0)
                        {
                            _bits &= ~0x10000000L;
                            _headers._ProxyConnection = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.WWWAuthenticate.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000000000L) != 0)
                        {
                            _bits &= ~0x2000000000L;
                            _headers._WWWAuthenticate = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 17:
                {
                    if (ReferenceEquals(HeaderNames.TransferEncoding, key))
                    {
                        if ((_bits & 0x100000000L) != 0)
                        {
                            _bits &= ~0x100000000L;
                            _headers._TransferEncoding = default(StringValues);
                            _headers._rawTransferEncoding = null;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.TransferEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100000000L) != 0)
                        {
                            _bits &= ~0x100000000L;
                            _headers._TransferEncoding = default(StringValues);
                            _headers._rawTransferEncoding = null;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 18:
                {
                    if (ReferenceEquals(HeaderNames.ProxyAuthenticate, key))
                    {
                        if ((_bits & 0x8000000L) != 0)
                        {
                            _bits &= ~0x8000000L;
                            _headers._ProxyAuthenticate = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.ProxyAuthenticate.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000000L) != 0)
                        {
                            _bits &= ~0x8000000L;
                            _headers._ProxyAuthenticate = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 22:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlMaxAge, key))
                    {
                        if ((_bits & 0x400L) != 0)
                        {
                            _bits &= ~0x400L;
                            _headers._AccessControlMaxAge = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AccessControlMaxAge.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400L) != 0)
                        {
                            _bits &= ~0x400L;
                            _headers._AccessControlMaxAge = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 27:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlAllowOrigin, key))
                    {
                        if ((_bits & 0x100L) != 0)
                        {
                            _bits &= ~0x100L;
                            _headers._AccessControlAllowOrigin = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AccessControlAllowOrigin.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100L) != 0)
                        {
                            _bits &= ~0x100L;
                            _headers._AccessControlAllowOrigin = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 28:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlAllowHeaders, key))
                    {
                        if ((_bits & 0x40L) != 0)
                        {
                            _bits &= ~0x40L;
                            _headers._AccessControlAllowHeaders = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.AccessControlAllowMethods, key))
                    {
                        if ((_bits & 0x80L) != 0)
                        {
                            _bits &= ~0x80L;
                            _headers._AccessControlAllowMethods = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AccessControlAllowHeaders.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40L) != 0)
                        {
                            _bits &= ~0x40L;
                            _headers._AccessControlAllowHeaders = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.AccessControlAllowMethods.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80L) != 0)
                        {
                            _bits &= ~0x80L;
                            _headers._AccessControlAllowMethods = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 29:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlExposeHeaders, key))
                    {
                        if ((_bits & 0x200L) != 0)
                        {
                            _bits &= ~0x200L;
                            _headers._AccessControlExposeHeaders = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AccessControlExposeHeaders.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200L) != 0)
                        {
                            _bits &= ~0x200L;
                            _headers._AccessControlExposeHeaders = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 32:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlAllowCredentials, key))
                    {
                        if ((_bits & 0x20L) != 0)
                        {
                            _bits &= ~0x20L;
                            _headers._AccessControlAllowCredentials = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AccessControlAllowCredentials.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20L) != 0)
                        {
                            _bits &= ~0x20L;
                            _headers._AccessControlAllowCredentials = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
            }

            return RemoveUnknown(key);
        }
        protected override void ClearFast()
        {
            MaybeUnknown?.Clear();
            _contentLength = null;
            var tempBits = _bits;
            _bits = 0;
            if(BitOperations.PopCount((ulong)tempBits) > 12)
            {
                _headers = default(HeaderReferences);
                return;
            }
            
            if ((tempBits & 0x1L) != 0)
            {
                _headers._Connection = default;
                if((tempBits & ~0x1L) == 0)
                {
                    return;
                }
                tempBits &= ~0x1L;
            }
            
            if ((tempBits & 0x2L) != 0)
            {
                _headers._ContentType = default;
                if((tempBits & ~0x2L) == 0)
                {
                    return;
                }
                tempBits &= ~0x2L;
            }
            
            if ((tempBits & 0x4L) != 0)
            {
                _headers._Date = default;
                if((tempBits & ~0x4L) == 0)
                {
                    return;
                }
                tempBits &= ~0x4L;
            }
            
            if ((tempBits & 0x8L) != 0)
            {
                _headers._Server = default;
                if((tempBits & ~0x8L) == 0)
                {
                    return;
                }
                tempBits &= ~0x8L;
            }
            
            if ((tempBits & 0x10L) != 0)
            {
                _headers._AcceptRanges = default;
                if((tempBits & ~0x10L) == 0)
                {
                    return;
                }
                tempBits &= ~0x10L;
            }
            
            if ((tempBits & 0x20L) != 0)
            {
                _headers._AccessControlAllowCredentials = default;
                if((tempBits & ~0x20L) == 0)
                {
                    return;
                }
                tempBits &= ~0x20L;
            }
            
            if ((tempBits & 0x40L) != 0)
            {
                _headers._AccessControlAllowHeaders = default;
                if((tempBits & ~0x40L) == 0)
                {
                    return;
                }
                tempBits &= ~0x40L;
            }
            
            if ((tempBits & 0x80L) != 0)
            {
                _headers._AccessControlAllowMethods = default;
                if((tempBits & ~0x80L) == 0)
                {
                    return;
                }
                tempBits &= ~0x80L;
            }
            
            if ((tempBits & 0x100L) != 0)
            {
                _headers._AccessControlAllowOrigin = default;
                if((tempBits & ~0x100L) == 0)
                {
                    return;
                }
                tempBits &= ~0x100L;
            }
            
            if ((tempBits & 0x200L) != 0)
            {
                _headers._AccessControlExposeHeaders = default;
                if((tempBits & ~0x200L) == 0)
                {
                    return;
                }
                tempBits &= ~0x200L;
            }
            
            if ((tempBits & 0x400L) != 0)
            {
                _headers._AccessControlMaxAge = default;
                if((tempBits & ~0x400L) == 0)
                {
                    return;
                }
                tempBits &= ~0x400L;
            }
            
            if ((tempBits & 0x800L) != 0)
            {
                _headers._Age = default;
                if((tempBits & ~0x800L) == 0)
                {
                    return;
                }
                tempBits &= ~0x800L;
            }
            
            if ((tempBits & 0x1000L) != 0)
            {
                _headers._Allow = default;
                if((tempBits & ~0x1000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x1000L;
            }
            
            if ((tempBits & 0x2000L) != 0)
            {
                _headers._AltSvc = default;
                if((tempBits & ~0x2000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x2000L;
            }
            
            if ((tempBits & 0x4000L) != 0)
            {
                _headers._CacheControl = default;
                if((tempBits & ~0x4000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x4000L;
            }
            
            if ((tempBits & 0x8000L) != 0)
            {
                _headers._ContentEncoding = default;
                if((tempBits & ~0x8000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x8000L;
            }
            
            if ((tempBits & 0x10000L) != 0)
            {
                _headers._ContentLanguage = default;
                if((tempBits & ~0x10000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x10000L;
            }
            
            if ((tempBits & 0x20000L) != 0)
            {
                _headers._ContentLocation = default;
                if((tempBits & ~0x20000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x20000L;
            }
            
            if ((tempBits & 0x40000L) != 0)
            {
                _headers._ContentMD5 = default;
                if((tempBits & ~0x40000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x40000L;
            }
            
            if ((tempBits & 0x80000L) != 0)
            {
                _headers._ContentRange = default;
                if((tempBits & ~0x80000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x80000L;
            }
            
            if ((tempBits & 0x100000L) != 0)
            {
                _headers._ETag = default;
                if((tempBits & ~0x100000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x100000L;
            }
            
            if ((tempBits & 0x200000L) != 0)
            {
                _headers._Expires = default;
                if((tempBits & ~0x200000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x200000L;
            }
            
            if ((tempBits & 0x400000L) != 0)
            {
                _headers._GrpcEncoding = default;
                if((tempBits & ~0x400000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x400000L;
            }
            
            if ((tempBits & 0x800000L) != 0)
            {
                _headers._KeepAlive = default;
                if((tempBits & ~0x800000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x800000L;
            }
            
            if ((tempBits & 0x1000000L) != 0)
            {
                _headers._LastModified = default;
                if((tempBits & ~0x1000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x1000000L;
            }
            
            if ((tempBits & 0x2000000L) != 0)
            {
                _headers._Location = default;
                if((tempBits & ~0x2000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x2000000L;
            }
            
            if ((tempBits & 0x4000000L) != 0)
            {
                _headers._Pragma = default;
                if((tempBits & ~0x4000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x4000000L;
            }
            
            if ((tempBits & 0x8000000L) != 0)
            {
                _headers._ProxyAuthenticate = default;
                if((tempBits & ~0x8000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x8000000L;
            }
            
            if ((tempBits & 0x10000000L) != 0)
            {
                _headers._ProxyConnection = default;
                if((tempBits & ~0x10000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x10000000L;
            }
            
            if ((tempBits & 0x20000000L) != 0)
            {
                _headers._RetryAfter = default;
                if((tempBits & ~0x20000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x20000000L;
            }
            
            if ((tempBits & 0x40000000L) != 0)
            {
                _headers._SetCookie = default;
                if((tempBits & ~0x40000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x40000000L;
            }
            
            if ((tempBits & 0x80000000L) != 0)
            {
                _headers._Trailer = default;
                if((tempBits & ~0x80000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x80000000L;
            }
            
            if ((tempBits & 0x100000000L) != 0)
            {
                _headers._TransferEncoding = default;
                if((tempBits & ~0x100000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x100000000L;
            }
            
            if ((tempBits & 0x200000000L) != 0)
            {
                _headers._Upgrade = default;
                if((tempBits & ~0x200000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x200000000L;
            }
            
            if ((tempBits & 0x400000000L) != 0)
            {
                _headers._Vary = default;
                if((tempBits & ~0x400000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x400000000L;
            }
            
            if ((tempBits & 0x800000000L) != 0)
            {
                _headers._Via = default;
                if((tempBits & ~0x800000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x800000000L;
            }
            
            if ((tempBits & 0x1000000000L) != 0)
            {
                _headers._Warning = default;
                if((tempBits & ~0x1000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x1000000000L;
            }
            
            if ((tempBits & 0x2000000000L) != 0)
            {
                _headers._WWWAuthenticate = default;
                if((tempBits & ~0x2000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x2000000000L;
            }
            
        }

        protected override bool CopyToFast(KeyValuePair<string, StringValues>[] array, int arrayIndex)
        {
            if (arrayIndex < 0)
            {
                return false;
            }
            
                if ((_bits & 0x1L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Connection, _headers._Connection);
                    ++arrayIndex;
                }
                if ((_bits & 0x2L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ContentType, _headers._ContentType);
                    ++arrayIndex;
                }
                if ((_bits & 0x4L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Date, _headers._Date);
                    ++arrayIndex;
                }
                if ((_bits & 0x8L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Server, _headers._Server);
                    ++arrayIndex;
                }
                if ((_bits & 0x10L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.AcceptRanges, _headers._AcceptRanges);
                    ++arrayIndex;
                }
                if ((_bits & 0x20L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlAllowCredentials, _headers._AccessControlAllowCredentials);
                    ++arrayIndex;
                }
                if ((_bits & 0x40L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlAllowHeaders, _headers._AccessControlAllowHeaders);
                    ++arrayIndex;
                }
                if ((_bits & 0x80L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlAllowMethods, _headers._AccessControlAllowMethods);
                    ++arrayIndex;
                }
                if ((_bits & 0x100L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlAllowOrigin, _headers._AccessControlAllowOrigin);
                    ++arrayIndex;
                }
                if ((_bits & 0x200L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlExposeHeaders, _headers._AccessControlExposeHeaders);
                    ++arrayIndex;
                }
                if ((_bits & 0x400L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlMaxAge, _headers._AccessControlMaxAge);
                    ++arrayIndex;
                }
                if ((_bits & 0x800L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Age, _headers._Age);
                    ++arrayIndex;
                }
                if ((_bits & 0x1000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Allow, _headers._Allow);
                    ++arrayIndex;
                }
                if ((_bits & 0x2000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.AltSvc, _headers._AltSvc);
                    ++arrayIndex;
                }
                if ((_bits & 0x4000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.CacheControl, _headers._CacheControl);
                    ++arrayIndex;
                }
                if ((_bits & 0x8000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ContentEncoding, _headers._ContentEncoding);
                    ++arrayIndex;
                }
                if ((_bits & 0x10000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ContentLanguage, _headers._ContentLanguage);
                    ++arrayIndex;
                }
                if ((_bits & 0x20000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ContentLocation, _headers._ContentLocation);
                    ++arrayIndex;
                }
                if ((_bits & 0x40000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ContentMD5, _headers._ContentMD5);
                    ++arrayIndex;
                }
                if ((_bits & 0x80000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ContentRange, _headers._ContentRange);
                    ++arrayIndex;
                }
                if ((_bits & 0x100000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ETag, _headers._ETag);
                    ++arrayIndex;
                }
                if ((_bits & 0x200000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Expires, _headers._Expires);
                    ++arrayIndex;
                }
                if ((_bits & 0x400000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.GrpcEncoding, _headers._GrpcEncoding);
                    ++arrayIndex;
                }
                if ((_bits & 0x800000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.KeepAlive, _headers._KeepAlive);
                    ++arrayIndex;
                }
                if ((_bits & 0x1000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.LastModified, _headers._LastModified);
                    ++arrayIndex;
                }
                if ((_bits & 0x2000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Location, _headers._Location);
                    ++arrayIndex;
                }
                if ((_bits & 0x4000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Pragma, _headers._Pragma);
                    ++arrayIndex;
                }
                if ((_bits & 0x8000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ProxyAuthenticate, _headers._ProxyAuthenticate);
                    ++arrayIndex;
                }
                if ((_bits & 0x10000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ProxyConnection, _headers._ProxyConnection);
                    ++arrayIndex;
                }
                if ((_bits & 0x20000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.RetryAfter, _headers._RetryAfter);
                    ++arrayIndex;
                }
                if ((_bits & 0x40000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.SetCookie, _headers._SetCookie);
                    ++arrayIndex;
                }
                if ((_bits & 0x80000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Trailer, _headers._Trailer);
                    ++arrayIndex;
                }
                if ((_bits & 0x100000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.TransferEncoding, _headers._TransferEncoding);
                    ++arrayIndex;
                }
                if ((_bits & 0x200000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Upgrade, _headers._Upgrade);
                    ++arrayIndex;
                }
                if ((_bits & 0x400000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Vary, _headers._Vary);
                    ++arrayIndex;
                }
                if ((_bits & 0x800000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Via, _headers._Via);
                    ++arrayIndex;
                }
                if ((_bits & 0x1000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Warning, _headers._Warning);
                    ++arrayIndex;
                }
                if ((_bits & 0x2000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.WWWAuthenticate, _headers._WWWAuthenticate);
                    ++arrayIndex;
                }
                if (_contentLength.HasValue)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ContentLength, HeaderUtilities.FormatNonNegativeInt64(_contentLength.Value));
                    ++arrayIndex;
                }
            ((ICollection<KeyValuePair<string, StringValues>>?)MaybeUnknown)?.CopyTo(array, arrayIndex);

            return true;
        }
        
        internal bool HasInvalidH2H3Headers => (_bits & 13161725953) != 0;
        internal void ClearInvalidH2H3Headers()
        {
            _bits &= ~13161725953;
        }
        internal void CopyToFast(ref BufferWriter<PipeWriter> output)
        {
            var tempBits = (ulong)_bits;
            // Set exact next
            var next = BitOperations.TrailingZeroCount(tempBits);

            // Output Content-Length now as it isn't contained in the bit flags.
            if (_contentLength.HasValue)
            {
                output.Write(HeaderBytes.Slice(640, 18));
                output.WriteNumeric((ulong)ContentLength.GetValueOrDefault());
            }
            if (tempBits == 0)
            {
                return;
            }

            ref readonly StringValues values = ref Unsafe.NullRef<StringValues>();
            do
            {
                int keyStart;
                int keyLength;
                var headerName = string.Empty;
                switch (next)
                {
                    case 0: // Header: "Connection"
                        Debug.Assert((tempBits & 0x1L) != 0);
                        if (_headers._rawConnection != null)
                        {
                            // Clear and set next as not using common output.
                            tempBits ^= 0x1L;
                            next = BitOperations.TrailingZeroCount(tempBits);
                            output.Write(_headers._rawConnection);
                            continue; // Jump to next, already output header
                        }
                        else
                        {
                            values = ref _headers._Connection;
                            keyStart = 0;
                            keyLength = 14;
                            headerName = HeaderNames.Connection;
                        }
                        break; // OutputHeader

                    case 1: // Header: "Content-Type"
                        Debug.Assert((tempBits & 0x2L) != 0);
                        values = ref _headers._ContentType;
                        keyStart = 14;
                        keyLength = 16;
                        break; // OutputHeader

                    case 2: // Header: "Date"
                        Debug.Assert((tempBits & 0x4L) != 0);
                        if (_headers._rawDate != null)
                        {
                            // Clear and set next as not using common output.
                            tempBits ^= 0x4L;
                            next = BitOperations.TrailingZeroCount(tempBits);
                            output.Write(_headers._rawDate);
                            continue; // Jump to next, already output header
                        }
                        else
                        {
                            values = ref _headers._Date;
                            keyStart = 30;
                            keyLength = 8;
                            headerName = HeaderNames.Date;
                        }
                        break; // OutputHeader

                    case 3: // Header: "Server"
                        Debug.Assert((tempBits & 0x8L) != 0);
                        if (_headers._rawServer != null)
                        {
                            // Clear and set next as not using common output.
                            tempBits ^= 0x8L;
                            next = BitOperations.TrailingZeroCount(tempBits);
                            output.Write(_headers._rawServer);
                            continue; // Jump to next, already output header
                        }
                        else
                        {
                            values = ref _headers._Server;
                            keyStart = 38;
                            keyLength = 10;
                            headerName = HeaderNames.Server;
                        }
                        break; // OutputHeader

                    case 4: // Header: "Accept-Ranges"
                        Debug.Assert((tempBits & 0x10L) != 0);
                        values = ref _headers._AcceptRanges;
                        keyStart = 48;
                        keyLength = 17;
                        break; // OutputHeader

                    case 5: // Header: "Access-Control-Allow-Credentials"
                        Debug.Assert((tempBits & 0x20L) != 0);
                        values = ref _headers._AccessControlAllowCredentials;
                        keyStart = 65;
                        keyLength = 36;
                        break; // OutputHeader

                    case 6: // Header: "Access-Control-Allow-Headers"
                        Debug.Assert((tempBits & 0x40L) != 0);
                        values = ref _headers._AccessControlAllowHeaders;
                        keyStart = 101;
                        keyLength = 32;
                        break; // OutputHeader

                    case 7: // Header: "Access-Control-Allow-Methods"
                        Debug.Assert((tempBits & 0x80L) != 0);
                        values = ref _headers._AccessControlAllowMethods;
                        keyStart = 133;
                        keyLength = 32;
                        break; // OutputHeader

                    case 8: // Header: "Access-Control-Allow-Origin"
                        Debug.Assert((tempBits & 0x100L) != 0);
                        values = ref _headers._AccessControlAllowOrigin;
                        keyStart = 165;
                        keyLength = 31;
                        break; // OutputHeader

                    case 9: // Header: "Access-Control-Expose-Headers"
                        Debug.Assert((tempBits & 0x200L) != 0);
                        values = ref _headers._AccessControlExposeHeaders;
                        keyStart = 196;
                        keyLength = 33;
                        break; // OutputHeader

                    case 10: // Header: "Access-Control-Max-Age"
                        Debug.Assert((tempBits & 0x400L) != 0);
                        values = ref _headers._AccessControlMaxAge;
                        keyStart = 229;
                        keyLength = 26;
                        break; // OutputHeader

                    case 11: // Header: "Age"
                        Debug.Assert((tempBits & 0x800L) != 0);
                        values = ref _headers._Age;
                        keyStart = 255;
                        keyLength = 7;
                        break; // OutputHeader

                    case 12: // Header: "Allow"
                        Debug.Assert((tempBits & 0x1000L) != 0);
                        values = ref _headers._Allow;
                        keyStart = 262;
                        keyLength = 9;
                        break; // OutputHeader

                    case 13: // Header: "Alt-Svc"
                        Debug.Assert((tempBits & 0x2000L) != 0);
                        if (_headers._rawAltSvc != null)
                        {
                            // Clear and set next as not using common output.
                            tempBits ^= 0x2000L;
                            next = BitOperations.TrailingZeroCount(tempBits);
                            output.Write(_headers._rawAltSvc);
                            continue; // Jump to next, already output header
                        }
                        else
                        {
                            values = ref _headers._AltSvc;
                            keyStart = 271;
                            keyLength = 11;
                            headerName = HeaderNames.AltSvc;
                        }
                        break; // OutputHeader

                    case 14: // Header: "Cache-Control"
                        Debug.Assert((tempBits & 0x4000L) != 0);
                        values = ref _headers._CacheControl;
                        keyStart = 282;
                        keyLength = 17;
                        break; // OutputHeader

                    case 15: // Header: "Content-Encoding"
                        Debug.Assert((tempBits & 0x8000L) != 0);
                        values = ref _headers._ContentEncoding;
                        keyStart = 299;
                        keyLength = 20;
                        break; // OutputHeader

                    case 16: // Header: "Content-Language"
                        Debug.Assert((tempBits & 0x10000L) != 0);
                        values = ref _headers._ContentLanguage;
                        keyStart = 319;
                        keyLength = 20;
                        break; // OutputHeader

                    case 17: // Header: "Content-Location"
                        Debug.Assert((tempBits & 0x20000L) != 0);
                        values = ref _headers._ContentLocation;
                        keyStart = 339;
                        keyLength = 20;
                        break; // OutputHeader

                    case 18: // Header: "Content-MD5"
                        Debug.Assert((tempBits & 0x40000L) != 0);
                        values = ref _headers._ContentMD5;
                        keyStart = 359;
                        keyLength = 15;
                        break; // OutputHeader

                    case 19: // Header: "Content-Range"
                        Debug.Assert((tempBits & 0x80000L) != 0);
                        values = ref _headers._ContentRange;
                        keyStart = 374;
                        keyLength = 17;
                        break; // OutputHeader

                    case 20: // Header: "ETag"
                        Debug.Assert((tempBits & 0x100000L) != 0);
                        values = ref _headers._ETag;
                        keyStart = 391;
                        keyLength = 8;
                        break; // OutputHeader

                    case 21: // Header: "Expires"
                        Debug.Assert((tempBits & 0x200000L) != 0);
                        values = ref _headers._Expires;
                        keyStart = 399;
                        keyLength = 11;
                        break; // OutputHeader

                    case 22: // Header: "Grpc-Encoding"
                        Debug.Assert((tempBits & 0x400000L) != 0);
                        values = ref _headers._GrpcEncoding;
                        keyStart = 410;
                        keyLength = 17;
                        break; // OutputHeader

                    case 23: // Header: "Keep-Alive"
                        Debug.Assert((tempBits & 0x800000L) != 0);
                        values = ref _headers._KeepAlive;
                        keyStart = 427;
                        keyLength = 14;
                        break; // OutputHeader

                    case 24: // Header: "Last-Modified"
                        Debug.Assert((tempBits & 0x1000000L) != 0);
                        values = ref _headers._LastModified;
                        keyStart = 441;
                        keyLength = 17;
                        break; // OutputHeader

                    case 25: // Header: "Location"
                        Debug.Assert((tempBits & 0x2000000L) != 0);
                        values = ref _headers._Location;
                        keyStart = 458;
                        keyLength = 12;
                        break; // OutputHeader

                    case 26: // Header: "Pragma"
                        Debug.Assert((tempBits & 0x4000000L) != 0);
                        values = ref _headers._Pragma;
                        keyStart = 470;
                        keyLength = 10;
                        break; // OutputHeader

                    case 27: // Header: "Proxy-Authenticate"
                        Debug.Assert((tempBits & 0x8000000L) != 0);
                        values = ref _headers._ProxyAuthenticate;
                        keyStart = 480;
                        keyLength = 22;
                        break; // OutputHeader

                    case 28: // Header: "Proxy-Connection"
                        Debug.Assert((tempBits & 0x10000000L) != 0);
                        values = ref _headers._ProxyConnection;
                        keyStart = 502;
                        keyLength = 20;
                        break; // OutputHeader

                    case 29: // Header: "Retry-After"
                        Debug.Assert((tempBits & 0x20000000L) != 0);
                        values = ref _headers._RetryAfter;
                        keyStart = 522;
                        keyLength = 15;
                        break; // OutputHeader

                    case 30: // Header: "Set-Cookie"
                        Debug.Assert((tempBits & 0x40000000L) != 0);
                        values = ref _headers._SetCookie;
                        keyStart = 537;
                        keyLength = 14;
                        break; // OutputHeader

                    case 31: // Header: "Trailer"
                        Debug.Assert((tempBits & 0x80000000L) != 0);
                        values = ref _headers._Trailer;
                        keyStart = 551;
                        keyLength = 11;
                        break; // OutputHeader

                    case 32: // Header: "Transfer-Encoding"
                        Debug.Assert((tempBits & 0x100000000L) != 0);
                        if (_headers._rawTransferEncoding != null)
                        {
                            // Clear and set next as not using common output.
                            tempBits ^= 0x100000000L;
                            next = BitOperations.TrailingZeroCount(tempBits);
                            output.Write(_headers._rawTransferEncoding);
                            continue; // Jump to next, already output header
                        }
                        else
                        {
                            values = ref _headers._TransferEncoding;
                            keyStart = 562;
                            keyLength = 21;
                            headerName = HeaderNames.TransferEncoding;
                        }
                        break; // OutputHeader

                    case 33: // Header: "Upgrade"
                        Debug.Assert((tempBits & 0x200000000L) != 0);
                        values = ref _headers._Upgrade;
                        keyStart = 583;
                        keyLength = 11;
                        break; // OutputHeader

                    case 34: // Header: "Vary"
                        Debug.Assert((tempBits & 0x400000000L) != 0);
                        values = ref _headers._Vary;
                        keyStart = 594;
                        keyLength = 8;
                        break; // OutputHeader

                    case 35: // Header: "Via"
                        Debug.Assert((tempBits & 0x800000000L) != 0);
                        values = ref _headers._Via;
                        keyStart = 602;
                        keyLength = 7;
                        break; // OutputHeader

                    case 36: // Header: "Warning"
                        Debug.Assert((tempBits & 0x1000000000L) != 0);
                        values = ref _headers._Warning;
                        keyStart = 609;
                        keyLength = 11;
                        break; // OutputHeader

                    case 37: // Header: "WWW-Authenticate"
                        Debug.Assert((tempBits & 0x2000000000L) != 0);
                        values = ref _headers._WWWAuthenticate;
                        keyStart = 620;
                        keyLength = 20;
                        break; // OutputHeader

                    default:
                        ThrowInvalidHeaderBits();
                        return;
                }

                // OutputHeader
                {
                    // Clear bit
                    tempBits ^= (1UL << next);
                    var encoding = ReferenceEquals(EncodingSelector, KestrelServerOptions.DefaultHeaderEncodingSelector)
                        ? null : EncodingSelector(headerName);
                    var valueCount = values.Count;
                    Debug.Assert(valueCount > 0);

                    var headerKey = HeaderBytes.Slice(keyStart, keyLength);
                    for (var i = 0; i < valueCount; i++)
                    {
                        var value = values[i];
                        if (value != null)
                        {
                            output.Write(headerKey);
                            if (encoding is null)
                            {
                                output.WriteAscii(value);
                            }
                            else
                            {
                                output.WriteEncoded(value, encoding);
                            }
                        }
                    }
                    // Set exact next
                    next = BitOperations.TrailingZeroCount(tempBits);
                }
            } while (tempBits != 0);
        }

        private struct HeaderReferences
        {
            public StringValues _Connection;
            public StringValues _ContentType;
            public StringValues _Date;
            public StringValues _Server;
            public StringValues _AcceptRanges;
            public StringValues _AccessControlAllowCredentials;
            public StringValues _AccessControlAllowHeaders;
            public StringValues _AccessControlAllowMethods;
            public StringValues _AccessControlAllowOrigin;
            public StringValues _AccessControlExposeHeaders;
            public StringValues _AccessControlMaxAge;
            public StringValues _Age;
            public StringValues _Allow;
            public StringValues _AltSvc;
            public StringValues _CacheControl;
            public StringValues _ContentEncoding;
            public StringValues _ContentLanguage;
            public StringValues _ContentLocation;
            public StringValues _ContentMD5;
            public StringValues _ContentRange;
            public StringValues _ETag;
            public StringValues _Expires;
            public StringValues _GrpcEncoding;
            public StringValues _KeepAlive;
            public StringValues _LastModified;
            public StringValues _Location;
            public StringValues _Pragma;
            public StringValues _ProxyAuthenticate;
            public StringValues _ProxyConnection;
            public StringValues _RetryAfter;
            public StringValues _SetCookie;
            public StringValues _Trailer;
            public StringValues _TransferEncoding;
            public StringValues _Upgrade;
            public StringValues _Vary;
            public StringValues _Via;
            public StringValues _Warning;
            public StringValues _WWWAuthenticate;
            
            public byte[]? _rawConnection;
            public byte[]? _rawDate;
            public byte[]? _rawServer;
            public byte[]? _rawAltSvc;
            public byte[]? _rawTransferEncoding;
        }

        public partial struct Enumerator
        {
            // Compiled to Jump table
            public bool MoveNext()
            {
                switch (_next)
                {
                    case 0: // Header: "Connection"
                        Debug.Assert((_currentBits & 0x1L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Connection, _collection._headers._Connection);
                        _currentKnownType = KnownHeaderType.Connection;
                        _currentBits ^= 0x1L;
                        break;
                    case 1: // Header: "Content-Type"
                        Debug.Assert((_currentBits & 0x2L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ContentType, _collection._headers._ContentType);
                        _currentKnownType = KnownHeaderType.ContentType;
                        _currentBits ^= 0x2L;
                        break;
                    case 2: // Header: "Date"
                        Debug.Assert((_currentBits & 0x4L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Date, _collection._headers._Date);
                        _currentKnownType = KnownHeaderType.Date;
                        _currentBits ^= 0x4L;
                        break;
                    case 3: // Header: "Server"
                        Debug.Assert((_currentBits & 0x8L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Server, _collection._headers._Server);
                        _currentKnownType = KnownHeaderType.Server;
                        _currentBits ^= 0x8L;
                        break;
                    case 4: // Header: "Accept-Ranges"
                        Debug.Assert((_currentBits & 0x10L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.AcceptRanges, _collection._headers._AcceptRanges);
                        _currentKnownType = KnownHeaderType.AcceptRanges;
                        _currentBits ^= 0x10L;
                        break;
                    case 5: // Header: "Access-Control-Allow-Credentials"
                        Debug.Assert((_currentBits & 0x20L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlAllowCredentials, _collection._headers._AccessControlAllowCredentials);
                        _currentKnownType = KnownHeaderType.AccessControlAllowCredentials;
                        _currentBits ^= 0x20L;
                        break;
                    case 6: // Header: "Access-Control-Allow-Headers"
                        Debug.Assert((_currentBits & 0x40L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlAllowHeaders, _collection._headers._AccessControlAllowHeaders);
                        _currentKnownType = KnownHeaderType.AccessControlAllowHeaders;
                        _currentBits ^= 0x40L;
                        break;
                    case 7: // Header: "Access-Control-Allow-Methods"
                        Debug.Assert((_currentBits & 0x80L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlAllowMethods, _collection._headers._AccessControlAllowMethods);
                        _currentKnownType = KnownHeaderType.AccessControlAllowMethods;
                        _currentBits ^= 0x80L;
                        break;
                    case 8: // Header: "Access-Control-Allow-Origin"
                        Debug.Assert((_currentBits & 0x100L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlAllowOrigin, _collection._headers._AccessControlAllowOrigin);
                        _currentKnownType = KnownHeaderType.AccessControlAllowOrigin;
                        _currentBits ^= 0x100L;
                        break;
                    case 9: // Header: "Access-Control-Expose-Headers"
                        Debug.Assert((_currentBits & 0x200L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlExposeHeaders, _collection._headers._AccessControlExposeHeaders);
                        _currentKnownType = KnownHeaderType.AccessControlExposeHeaders;
                        _currentBits ^= 0x200L;
                        break;
                    case 10: // Header: "Access-Control-Max-Age"
                        Debug.Assert((_currentBits & 0x400L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlMaxAge, _collection._headers._AccessControlMaxAge);
                        _currentKnownType = KnownHeaderType.AccessControlMaxAge;
                        _currentBits ^= 0x400L;
                        break;
                    case 11: // Header: "Age"
                        Debug.Assert((_currentBits & 0x800L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Age, _collection._headers._Age);
                        _currentKnownType = KnownHeaderType.Age;
                        _currentBits ^= 0x800L;
                        break;
                    case 12: // Header: "Allow"
                        Debug.Assert((_currentBits & 0x1000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Allow, _collection._headers._Allow);
                        _currentKnownType = KnownHeaderType.Allow;
                        _currentBits ^= 0x1000L;
                        break;
                    case 13: // Header: "Alt-Svc"
                        Debug.Assert((_currentBits & 0x2000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.AltSvc, _collection._headers._AltSvc);
                        _currentKnownType = KnownHeaderType.AltSvc;
                        _currentBits ^= 0x2000L;
                        break;
                    case 14: // Header: "Cache-Control"
                        Debug.Assert((_currentBits & 0x4000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.CacheControl, _collection._headers._CacheControl);
                        _currentKnownType = KnownHeaderType.CacheControl;
                        _currentBits ^= 0x4000L;
                        break;
                    case 15: // Header: "Content-Encoding"
                        Debug.Assert((_currentBits & 0x8000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ContentEncoding, _collection._headers._ContentEncoding);
                        _currentKnownType = KnownHeaderType.ContentEncoding;
                        _currentBits ^= 0x8000L;
                        break;
                    case 16: // Header: "Content-Language"
                        Debug.Assert((_currentBits & 0x10000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ContentLanguage, _collection._headers._ContentLanguage);
                        _currentKnownType = KnownHeaderType.ContentLanguage;
                        _currentBits ^= 0x10000L;
                        break;
                    case 17: // Header: "Content-Location"
                        Debug.Assert((_currentBits & 0x20000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ContentLocation, _collection._headers._ContentLocation);
                        _currentKnownType = KnownHeaderType.ContentLocation;
                        _currentBits ^= 0x20000L;
                        break;
                    case 18: // Header: "Content-MD5"
                        Debug.Assert((_currentBits & 0x40000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ContentMD5, _collection._headers._ContentMD5);
                        _currentKnownType = KnownHeaderType.ContentMD5;
                        _currentBits ^= 0x40000L;
                        break;
                    case 19: // Header: "Content-Range"
                        Debug.Assert((_currentBits & 0x80000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ContentRange, _collection._headers._ContentRange);
                        _currentKnownType = KnownHeaderType.ContentRange;
                        _currentBits ^= 0x80000L;
                        break;
                    case 20: // Header: "ETag"
                        Debug.Assert((_currentBits & 0x100000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ETag, _collection._headers._ETag);
                        _currentKnownType = KnownHeaderType.ETag;
                        _currentBits ^= 0x100000L;
                        break;
                    case 21: // Header: "Expires"
                        Debug.Assert((_currentBits & 0x200000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Expires, _collection._headers._Expires);
                        _currentKnownType = KnownHeaderType.Expires;
                        _currentBits ^= 0x200000L;
                        break;
                    case 22: // Header: "Grpc-Encoding"
                        Debug.Assert((_currentBits & 0x400000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.GrpcEncoding, _collection._headers._GrpcEncoding);
                        _currentKnownType = KnownHeaderType.GrpcEncoding;
                        _currentBits ^= 0x400000L;
                        break;
                    case 23: // Header: "Keep-Alive"
                        Debug.Assert((_currentBits & 0x800000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.KeepAlive, _collection._headers._KeepAlive);
                        _currentKnownType = KnownHeaderType.KeepAlive;
                        _currentBits ^= 0x800000L;
                        break;
                    case 24: // Header: "Last-Modified"
                        Debug.Assert((_currentBits & 0x1000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.LastModified, _collection._headers._LastModified);
                        _currentKnownType = KnownHeaderType.LastModified;
                        _currentBits ^= 0x1000000L;
                        break;
                    case 25: // Header: "Location"
                        Debug.Assert((_currentBits & 0x2000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Location, _collection._headers._Location);
                        _currentKnownType = KnownHeaderType.Location;
                        _currentBits ^= 0x2000000L;
                        break;
                    case 26: // Header: "Pragma"
                        Debug.Assert((_currentBits & 0x4000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Pragma, _collection._headers._Pragma);
                        _currentKnownType = KnownHeaderType.Pragma;
                        _currentBits ^= 0x4000000L;
                        break;
                    case 27: // Header: "Proxy-Authenticate"
                        Debug.Assert((_currentBits & 0x8000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ProxyAuthenticate, _collection._headers._ProxyAuthenticate);
                        _currentKnownType = KnownHeaderType.ProxyAuthenticate;
                        _currentBits ^= 0x8000000L;
                        break;
                    case 28: // Header: "Proxy-Connection"
                        Debug.Assert((_currentBits & 0x10000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ProxyConnection, _collection._headers._ProxyConnection);
                        _currentKnownType = KnownHeaderType.ProxyConnection;
                        _currentBits ^= 0x10000000L;
                        break;
                    case 29: // Header: "Retry-After"
                        Debug.Assert((_currentBits & 0x20000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.RetryAfter, _collection._headers._RetryAfter);
                        _currentKnownType = KnownHeaderType.RetryAfter;
                        _currentBits ^= 0x20000000L;
                        break;
                    case 30: // Header: "Set-Cookie"
                        Debug.Assert((_currentBits & 0x40000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.SetCookie, _collection._headers._SetCookie);
                        _currentKnownType = KnownHeaderType.SetCookie;
                        _currentBits ^= 0x40000000L;
                        break;
                    case 31: // Header: "Trailer"
                        Debug.Assert((_currentBits & 0x80000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Trailer, _collection._headers._Trailer);
                        _currentKnownType = KnownHeaderType.Trailer;
                        _currentBits ^= 0x80000000L;
                        break;
                    case 32: // Header: "Transfer-Encoding"
                        Debug.Assert((_currentBits & 0x100000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.TransferEncoding, _collection._headers._TransferEncoding);
                        _currentKnownType = KnownHeaderType.TransferEncoding;
                        _currentBits ^= 0x100000000L;
                        break;
                    case 33: // Header: "Upgrade"
                        Debug.Assert((_currentBits & 0x200000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Upgrade, _collection._headers._Upgrade);
                        _currentKnownType = KnownHeaderType.Upgrade;
                        _currentBits ^= 0x200000000L;
                        break;
                    case 34: // Header: "Vary"
                        Debug.Assert((_currentBits & 0x400000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Vary, _collection._headers._Vary);
                        _currentKnownType = KnownHeaderType.Vary;
                        _currentBits ^= 0x400000000L;
                        break;
                    case 35: // Header: "Via"
                        Debug.Assert((_currentBits & 0x800000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Via, _collection._headers._Via);
                        _currentKnownType = KnownHeaderType.Via;
                        _currentBits ^= 0x800000000L;
                        break;
                    case 36: // Header: "Warning"
                        Debug.Assert((_currentBits & 0x1000000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Warning, _collection._headers._Warning);
                        _currentKnownType = KnownHeaderType.Warning;
                        _currentBits ^= 0x1000000000L;
                        break;
                    case 37: // Header: "WWW-Authenticate"
                        Debug.Assert((_currentBits & 0x2000000000L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.WWWAuthenticate, _collection._headers._WWWAuthenticate);
                        _currentKnownType = KnownHeaderType.WWWAuthenticate;
                        _currentBits ^= 0x2000000000L;
                        break;
                    case 38: // Header: "Content-Length"
                        Debug.Assert(_currentBits == 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ContentLength, HeaderUtilities.FormatNonNegativeInt64(_collection._contentLength.GetValueOrDefault()));
                        _currentKnownType = KnownHeaderType.ContentLength;
                        _next = -1;
                        return true;
                    default:
                        if (!_hasUnknown || !_unknownEnumerator.MoveNext())
                        {
                            _current = default(KeyValuePair<string, StringValues>);
                            _currentKnownType = default;
                            return false;
                        }
                        _current = _unknownEnumerator.Current;
                        _currentKnownType = KnownHeaderType.Unknown;
                        return true;
                }

                if (_currentBits != 0)
                {
                    _next = BitOperations.TrailingZeroCount(_currentBits);
                    return true;
                }
                else
                {
                    _next = _collection._contentLength.HasValue ? 38 : -1;
                    return true;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static int GetNext(long bits, bool hasContentLength)
            {
                return bits != 0
                    ? BitOperations.TrailingZeroCount(bits)
                    : hasContentLength
                        ? 38
                        : -1;
            }
        }
    }

    internal partial class HttpResponseTrailers : IHeaderDictionary
    {
        private static ReadOnlySpan<byte> HeaderBytes => [13,10,69,84,97,103,58,32,13,10,71,114,112,99,45,77,101,115,115,97,103,101,58,32,13,10,71,114,112,99,45,83,116,97,116,117,115,58,32,];
        private HeaderReferences _headers;



        
        StringValues IHeaderDictionary.ETag
        {
            get
            {
                var value = _headers._ETag;
                if ((_bits & 0x1L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x1L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.ETag, value, EncodingSelector);
                    _bits |= flag;
                    _headers._ETag = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._ETag = default;
                }
            }
        }
        StringValues IHeaderDictionary.GrpcMessage
        {
            get
            {
                var value = _headers._GrpcMessage;
                if ((_bits & 0x2L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x2L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.GrpcMessage, value, EncodingSelector);
                    _bits |= flag;
                    _headers._GrpcMessage = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._GrpcMessage = default;
                }
            }
        }
        StringValues IHeaderDictionary.GrpcStatus
        {
            get
            {
                var value = _headers._GrpcStatus;
                if ((_bits & 0x4L) != 0)
                {
                    return value;
                }
                return StringValues.Empty;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }

                var flag = 0x4L;
                if (value.Count > 0)
                {
                    ValidateHeaderValueCharacters(HeaderNames.GrpcStatus, value, EncodingSelector);
                    _bits |= flag;
                    _headers._GrpcStatus = value;
                }
                else
                {
                    _bits &= ~flag;
                    _headers._GrpcStatus = default;
                }
            }
        }
        
        StringValues IHeaderDictionary.Accept
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Accept, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Accept, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Accept, value);
            }
        }
        StringValues IHeaderDictionary.AcceptCharset
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.AcceptCharset, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.AcceptCharset, value, EncodingSelector);
                SetValueUnknown(HeaderNames.AcceptCharset, value);
            }
        }
        StringValues IHeaderDictionary.AcceptEncoding
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.AcceptEncoding, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.AcceptEncoding, value, EncodingSelector);
                SetValueUnknown(HeaderNames.AcceptEncoding, value);
            }
        }
        StringValues IHeaderDictionary.AcceptLanguage
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.AcceptLanguage, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.AcceptLanguage, value, EncodingSelector);
                SetValueUnknown(HeaderNames.AcceptLanguage, value);
            }
        }
        StringValues IHeaderDictionary.AcceptRanges
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.AcceptRanges, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.AcceptRanges, value, EncodingSelector);
                SetValueUnknown(HeaderNames.AcceptRanges, value);
            }
        }
        StringValues IHeaderDictionary.AccessControlAllowCredentials
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.AccessControlAllowCredentials, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.AccessControlAllowCredentials, value, EncodingSelector);
                SetValueUnknown(HeaderNames.AccessControlAllowCredentials, value);
            }
        }
        StringValues IHeaderDictionary.AccessControlAllowHeaders
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.AccessControlAllowHeaders, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.AccessControlAllowHeaders, value, EncodingSelector);
                SetValueUnknown(HeaderNames.AccessControlAllowHeaders, value);
            }
        }
        StringValues IHeaderDictionary.AccessControlAllowMethods
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.AccessControlAllowMethods, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.AccessControlAllowMethods, value, EncodingSelector);
                SetValueUnknown(HeaderNames.AccessControlAllowMethods, value);
            }
        }
        StringValues IHeaderDictionary.AccessControlAllowOrigin
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.AccessControlAllowOrigin, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.AccessControlAllowOrigin, value, EncodingSelector);
                SetValueUnknown(HeaderNames.AccessControlAllowOrigin, value);
            }
        }
        StringValues IHeaderDictionary.AccessControlExposeHeaders
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.AccessControlExposeHeaders, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.AccessControlExposeHeaders, value, EncodingSelector);
                SetValueUnknown(HeaderNames.AccessControlExposeHeaders, value);
            }
        }
        StringValues IHeaderDictionary.AccessControlMaxAge
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.AccessControlMaxAge, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.AccessControlMaxAge, value, EncodingSelector);
                SetValueUnknown(HeaderNames.AccessControlMaxAge, value);
            }
        }
        StringValues IHeaderDictionary.AccessControlRequestHeaders
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.AccessControlRequestHeaders, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.AccessControlRequestHeaders, value, EncodingSelector);
                SetValueUnknown(HeaderNames.AccessControlRequestHeaders, value);
            }
        }
        StringValues IHeaderDictionary.AccessControlRequestMethod
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.AccessControlRequestMethod, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.AccessControlRequestMethod, value, EncodingSelector);
                SetValueUnknown(HeaderNames.AccessControlRequestMethod, value);
            }
        }
        StringValues IHeaderDictionary.Age
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Age, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Age, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Age, value);
            }
        }
        StringValues IHeaderDictionary.Allow
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Allow, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Allow, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Allow, value);
            }
        }
        StringValues IHeaderDictionary.AltSvc
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.AltSvc, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.AltSvc, value, EncodingSelector);
                SetValueUnknown(HeaderNames.AltSvc, value);
            }
        }
        StringValues IHeaderDictionary.Authorization
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Authorization, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Authorization, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Authorization, value);
            }
        }
        StringValues IHeaderDictionary.Baggage
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Baggage, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Baggage, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Baggage, value);
            }
        }
        StringValues IHeaderDictionary.CacheControl
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.CacheControl, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.CacheControl, value, EncodingSelector);
                SetValueUnknown(HeaderNames.CacheControl, value);
            }
        }
        StringValues IHeaderDictionary.Connection
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Connection, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Connection, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Connection, value);
            }
        }
        StringValues IHeaderDictionary.ContentDisposition
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.ContentDisposition, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.ContentDisposition, value, EncodingSelector);
                SetValueUnknown(HeaderNames.ContentDisposition, value);
            }
        }
        StringValues IHeaderDictionary.ContentEncoding
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.ContentEncoding, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.ContentEncoding, value, EncodingSelector);
                SetValueUnknown(HeaderNames.ContentEncoding, value);
            }
        }
        StringValues IHeaderDictionary.ContentLanguage
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.ContentLanguage, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.ContentLanguage, value, EncodingSelector);
                SetValueUnknown(HeaderNames.ContentLanguage, value);
            }
        }
        StringValues IHeaderDictionary.ContentLocation
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.ContentLocation, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.ContentLocation, value, EncodingSelector);
                SetValueUnknown(HeaderNames.ContentLocation, value);
            }
        }
        StringValues IHeaderDictionary.ContentMD5
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.ContentMD5, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.ContentMD5, value, EncodingSelector);
                SetValueUnknown(HeaderNames.ContentMD5, value);
            }
        }
        StringValues IHeaderDictionary.ContentRange
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.ContentRange, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.ContentRange, value, EncodingSelector);
                SetValueUnknown(HeaderNames.ContentRange, value);
            }
        }
        StringValues IHeaderDictionary.ContentSecurityPolicy
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.ContentSecurityPolicy, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.ContentSecurityPolicy, value, EncodingSelector);
                SetValueUnknown(HeaderNames.ContentSecurityPolicy, value);
            }
        }
        StringValues IHeaderDictionary.ContentSecurityPolicyReportOnly
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.ContentSecurityPolicyReportOnly, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.ContentSecurityPolicyReportOnly, value, EncodingSelector);
                SetValueUnknown(HeaderNames.ContentSecurityPolicyReportOnly, value);
            }
        }
        StringValues IHeaderDictionary.ContentType
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.ContentType, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.ContentType, value, EncodingSelector);
                SetValueUnknown(HeaderNames.ContentType, value);
            }
        }
        StringValues IHeaderDictionary.CorrelationContext
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.CorrelationContext, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.CorrelationContext, value, EncodingSelector);
                SetValueUnknown(HeaderNames.CorrelationContext, value);
            }
        }
        StringValues IHeaderDictionary.Cookie
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Cookie, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Cookie, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Cookie, value);
            }
        }
        StringValues IHeaderDictionary.Date
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Date, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Date, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Date, value);
            }
        }
        StringValues IHeaderDictionary.Expires
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Expires, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Expires, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Expires, value);
            }
        }
        StringValues IHeaderDictionary.Expect
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Expect, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Expect, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Expect, value);
            }
        }
        StringValues IHeaderDictionary.From
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.From, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.From, value, EncodingSelector);
                SetValueUnknown(HeaderNames.From, value);
            }
        }
        StringValues IHeaderDictionary.GrpcAcceptEncoding
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.GrpcAcceptEncoding, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.GrpcAcceptEncoding, value, EncodingSelector);
                SetValueUnknown(HeaderNames.GrpcAcceptEncoding, value);
            }
        }
        StringValues IHeaderDictionary.GrpcEncoding
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.GrpcEncoding, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.GrpcEncoding, value, EncodingSelector);
                SetValueUnknown(HeaderNames.GrpcEncoding, value);
            }
        }
        StringValues IHeaderDictionary.GrpcTimeout
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.GrpcTimeout, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.GrpcTimeout, value, EncodingSelector);
                SetValueUnknown(HeaderNames.GrpcTimeout, value);
            }
        }
        StringValues IHeaderDictionary.Host
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Host, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Host, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Host, value);
            }
        }
        StringValues IHeaderDictionary.KeepAlive
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.KeepAlive, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.KeepAlive, value, EncodingSelector);
                SetValueUnknown(HeaderNames.KeepAlive, value);
            }
        }
        StringValues IHeaderDictionary.IfMatch
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.IfMatch, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.IfMatch, value, EncodingSelector);
                SetValueUnknown(HeaderNames.IfMatch, value);
            }
        }
        StringValues IHeaderDictionary.IfModifiedSince
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.IfModifiedSince, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.IfModifiedSince, value, EncodingSelector);
                SetValueUnknown(HeaderNames.IfModifiedSince, value);
            }
        }
        StringValues IHeaderDictionary.IfNoneMatch
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.IfNoneMatch, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.IfNoneMatch, value, EncodingSelector);
                SetValueUnknown(HeaderNames.IfNoneMatch, value);
            }
        }
        StringValues IHeaderDictionary.IfRange
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.IfRange, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.IfRange, value, EncodingSelector);
                SetValueUnknown(HeaderNames.IfRange, value);
            }
        }
        StringValues IHeaderDictionary.IfUnmodifiedSince
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.IfUnmodifiedSince, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.IfUnmodifiedSince, value, EncodingSelector);
                SetValueUnknown(HeaderNames.IfUnmodifiedSince, value);
            }
        }
        StringValues IHeaderDictionary.LastModified
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.LastModified, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.LastModified, value, EncodingSelector);
                SetValueUnknown(HeaderNames.LastModified, value);
            }
        }
        StringValues IHeaderDictionary.Link
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Link, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Link, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Link, value);
            }
        }
        StringValues IHeaderDictionary.Location
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Location, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Location, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Location, value);
            }
        }
        StringValues IHeaderDictionary.MaxForwards
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.MaxForwards, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.MaxForwards, value, EncodingSelector);
                SetValueUnknown(HeaderNames.MaxForwards, value);
            }
        }
        StringValues IHeaderDictionary.Origin
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Origin, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Origin, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Origin, value);
            }
        }
        StringValues IHeaderDictionary.Pragma
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Pragma, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Pragma, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Pragma, value);
            }
        }
        StringValues IHeaderDictionary.ProxyAuthenticate
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.ProxyAuthenticate, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.ProxyAuthenticate, value, EncodingSelector);
                SetValueUnknown(HeaderNames.ProxyAuthenticate, value);
            }
        }
        StringValues IHeaderDictionary.ProxyAuthorization
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.ProxyAuthorization, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.ProxyAuthorization, value, EncodingSelector);
                SetValueUnknown(HeaderNames.ProxyAuthorization, value);
            }
        }
        StringValues IHeaderDictionary.ProxyConnection
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.ProxyConnection, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.ProxyConnection, value, EncodingSelector);
                SetValueUnknown(HeaderNames.ProxyConnection, value);
            }
        }
        StringValues IHeaderDictionary.Range
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Range, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Range, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Range, value);
            }
        }
        StringValues IHeaderDictionary.Referer
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Referer, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Referer, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Referer, value);
            }
        }
        StringValues IHeaderDictionary.RetryAfter
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.RetryAfter, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.RetryAfter, value, EncodingSelector);
                SetValueUnknown(HeaderNames.RetryAfter, value);
            }
        }
        StringValues IHeaderDictionary.RequestId
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.RequestId, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.RequestId, value, EncodingSelector);
                SetValueUnknown(HeaderNames.RequestId, value);
            }
        }
        StringValues IHeaderDictionary.SecWebSocketAccept
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.SecWebSocketAccept, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.SecWebSocketAccept, value, EncodingSelector);
                SetValueUnknown(HeaderNames.SecWebSocketAccept, value);
            }
        }
        StringValues IHeaderDictionary.SecWebSocketKey
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.SecWebSocketKey, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.SecWebSocketKey, value, EncodingSelector);
                SetValueUnknown(HeaderNames.SecWebSocketKey, value);
            }
        }
        StringValues IHeaderDictionary.SecWebSocketProtocol
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.SecWebSocketProtocol, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.SecWebSocketProtocol, value, EncodingSelector);
                SetValueUnknown(HeaderNames.SecWebSocketProtocol, value);
            }
        }
        StringValues IHeaderDictionary.SecWebSocketVersion
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.SecWebSocketVersion, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.SecWebSocketVersion, value, EncodingSelector);
                SetValueUnknown(HeaderNames.SecWebSocketVersion, value);
            }
        }
        StringValues IHeaderDictionary.SecWebSocketExtensions
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.SecWebSocketExtensions, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.SecWebSocketExtensions, value, EncodingSelector);
                SetValueUnknown(HeaderNames.SecWebSocketExtensions, value);
            }
        }
        StringValues IHeaderDictionary.Server
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Server, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Server, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Server, value);
            }
        }
        StringValues IHeaderDictionary.SetCookie
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.SetCookie, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.SetCookie, value, EncodingSelector);
                SetValueUnknown(HeaderNames.SetCookie, value);
            }
        }
        StringValues IHeaderDictionary.StrictTransportSecurity
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.StrictTransportSecurity, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.StrictTransportSecurity, value, EncodingSelector);
                SetValueUnknown(HeaderNames.StrictTransportSecurity, value);
            }
        }
        StringValues IHeaderDictionary.TE
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.TE, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.TE, value, EncodingSelector);
                SetValueUnknown(HeaderNames.TE, value);
            }
        }
        StringValues IHeaderDictionary.Trailer
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Trailer, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Trailer, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Trailer, value);
            }
        }
        StringValues IHeaderDictionary.TransferEncoding
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.TransferEncoding, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.TransferEncoding, value, EncodingSelector);
                SetValueUnknown(HeaderNames.TransferEncoding, value);
            }
        }
        StringValues IHeaderDictionary.Translate
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Translate, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Translate, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Translate, value);
            }
        }
        StringValues IHeaderDictionary.TraceParent
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.TraceParent, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.TraceParent, value, EncodingSelector);
                SetValueUnknown(HeaderNames.TraceParent, value);
            }
        }
        StringValues IHeaderDictionary.TraceState
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.TraceState, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.TraceState, value, EncodingSelector);
                SetValueUnknown(HeaderNames.TraceState, value);
            }
        }
        StringValues IHeaderDictionary.Upgrade
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Upgrade, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Upgrade, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Upgrade, value);
            }
        }
        StringValues IHeaderDictionary.UpgradeInsecureRequests
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.UpgradeInsecureRequests, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.UpgradeInsecureRequests, value, EncodingSelector);
                SetValueUnknown(HeaderNames.UpgradeInsecureRequests, value);
            }
        }
        StringValues IHeaderDictionary.UserAgent
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.UserAgent, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.UserAgent, value, EncodingSelector);
                SetValueUnknown(HeaderNames.UserAgent, value);
            }
        }
        StringValues IHeaderDictionary.Vary
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Vary, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Vary, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Vary, value);
            }
        }
        StringValues IHeaderDictionary.Via
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Via, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Via, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Via, value);
            }
        }
        StringValues IHeaderDictionary.Warning
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.Warning, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.Warning, value, EncodingSelector);
                SetValueUnknown(HeaderNames.Warning, value);
            }
        }
        StringValues IHeaderDictionary.WebSocketSubProtocols
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.WebSocketSubProtocols, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.WebSocketSubProtocols, value, EncodingSelector);
                SetValueUnknown(HeaderNames.WebSocketSubProtocols, value);
            }
        }
        StringValues IHeaderDictionary.WWWAuthenticate
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.WWWAuthenticate, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.WWWAuthenticate, value, EncodingSelector);
                SetValueUnknown(HeaderNames.WWWAuthenticate, value);
            }
        }
        StringValues IHeaderDictionary.XContentTypeOptions
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.XContentTypeOptions, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.XContentTypeOptions, value, EncodingSelector);
                SetValueUnknown(HeaderNames.XContentTypeOptions, value);
            }
        }
        StringValues IHeaderDictionary.XFrameOptions
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.XFrameOptions, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.XFrameOptions, value, EncodingSelector);
                SetValueUnknown(HeaderNames.XFrameOptions, value);
            }
        }
        StringValues IHeaderDictionary.XPoweredBy
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.XPoweredBy, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.XPoweredBy, value, EncodingSelector);
                SetValueUnknown(HeaderNames.XPoweredBy, value);
            }
        }
        StringValues IHeaderDictionary.XRequestedWith
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.XRequestedWith, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.XRequestedWith, value, EncodingSelector);
                SetValueUnknown(HeaderNames.XRequestedWith, value);
            }
        }
        StringValues IHeaderDictionary.XUACompatible
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.XUACompatible, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.XUACompatible, value, EncodingSelector);
                SetValueUnknown(HeaderNames.XUACompatible, value);
            }
        }
        StringValues IHeaderDictionary.XXSSProtection
        {
            get
            {
                StringValues value = default;
                if (!TryGetUnknown(HeaderNames.XXSSProtection, ref value))
                {
                    value = StringValues.Empty;
                }
                return value;
            }
            set
            {
                if (_isReadOnly) { ThrowHeadersReadOnlyException(); }
                ValidateHeaderValueCharacters(HeaderNames.XXSSProtection, value, EncodingSelector);
                SetValueUnknown(HeaderNames.XXSSProtection, value);
            }
        }

        protected override int GetCountFast()
        {
            return (_contentLength.HasValue ? 1 : 0 ) + BitOperations.PopCount((ulong)_bits) + (MaybeUnknown?.Count ?? 0);
        }

        protected override bool TryGetValueFast(string key, out StringValues value)
        {
            value = default;
            switch (key.Length)
            {
                case 4:
                {
                    if (ReferenceEquals(HeaderNames.ETag, key))
                    {
                        if ((_bits & 0x1L) != 0)
                        {
                            value = _headers._ETag;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.ETag.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1L) != 0)
                        {
                            value = _headers._ETag;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 11:
                {
                    if (ReferenceEquals(HeaderNames.GrpcStatus, key))
                    {
                        if ((_bits & 0x4L) != 0)
                        {
                            value = _headers._GrpcStatus;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.GrpcStatus.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4L) != 0)
                        {
                            value = _headers._GrpcStatus;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 12:
                {
                    if (ReferenceEquals(HeaderNames.GrpcMessage, key))
                    {
                        if ((_bits & 0x2L) != 0)
                        {
                            value = _headers._GrpcMessage;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.GrpcMessage.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2L) != 0)
                        {
                            value = _headers._GrpcMessage;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
            }

            return TryGetUnknown(key, ref value);
        }

        protected override void SetValueFast(string key, StringValues value)
        {
            ValidateHeaderValueCharacters(key, value, EncodingSelector);
            switch (key.Length)
            {
                case 4:
                {
                    if (ReferenceEquals(HeaderNames.ETag, key))
                    {
                        _bits |= 0x1L;
                        _headers._ETag = value;
                        return;
                    }

                    if (HeaderNames.ETag.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x1L;
                        _headers._ETag = value;
                        return;
                    }
                    break;
                }
                case 11:
                {
                    if (ReferenceEquals(HeaderNames.GrpcStatus, key))
                    {
                        _bits |= 0x4L;
                        _headers._GrpcStatus = value;
                        return;
                    }

                    if (HeaderNames.GrpcStatus.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x4L;
                        _headers._GrpcStatus = value;
                        return;
                    }
                    break;
                }
                case 12:
                {
                    if (ReferenceEquals(HeaderNames.GrpcMessage, key))
                    {
                        _bits |= 0x2L;
                        _headers._GrpcMessage = value;
                        return;
                    }

                    if (HeaderNames.GrpcMessage.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x2L;
                        _headers._GrpcMessage = value;
                        return;
                    }
                    break;
                }
            }

            SetValueUnknown(key, value);
        }

        protected override bool AddValueFast(string key, StringValues value)
        {
            ValidateHeaderValueCharacters(key, value, EncodingSelector);
            switch (key.Length)
            {
                case 4:
                {
                    if (ReferenceEquals(HeaderNames.ETag, key))
                    {
                        if ((_bits & 0x1L) == 0)
                        {
                            _bits |= 0x1L;
                            _headers._ETag = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.ETag.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1L) == 0)
                        {
                            _bits |= 0x1L;
                            _headers._ETag = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 11:
                {
                    if (ReferenceEquals(HeaderNames.GrpcStatus, key))
                    {
                        if ((_bits & 0x4L) == 0)
                        {
                            _bits |= 0x4L;
                            _headers._GrpcStatus = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.GrpcStatus.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4L) == 0)
                        {
                            _bits |= 0x4L;
                            _headers._GrpcStatus = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 12:
                {
                    if (ReferenceEquals(HeaderNames.GrpcMessage, key))
                    {
                        if ((_bits & 0x2L) == 0)
                        {
                            _bits |= 0x2L;
                            _headers._GrpcMessage = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.GrpcMessage.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2L) == 0)
                        {
                            _bits |= 0x2L;
                            _headers._GrpcMessage = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
            }

            return AddValueUnknown(key, value);
        }

        protected override bool RemoveFast(string key)
        {
            switch (key.Length)
            {
                case 4:
                {
                    if (ReferenceEquals(HeaderNames.ETag, key))
                    {
                        if ((_bits & 0x1L) != 0)
                        {
                            _bits &= ~0x1L;
                            _headers._ETag = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.ETag.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1L) != 0)
                        {
                            _bits &= ~0x1L;
                            _headers._ETag = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 11:
                {
                    if (ReferenceEquals(HeaderNames.GrpcStatus, key))
                    {
                        if ((_bits & 0x4L) != 0)
                        {
                            _bits &= ~0x4L;
                            _headers._GrpcStatus = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.GrpcStatus.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4L) != 0)
                        {
                            _bits &= ~0x4L;
                            _headers._GrpcStatus = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 12:
                {
                    if (ReferenceEquals(HeaderNames.GrpcMessage, key))
                    {
                        if ((_bits & 0x2L) != 0)
                        {
                            _bits &= ~0x2L;
                            _headers._GrpcMessage = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.GrpcMessage.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2L) != 0)
                        {
                            _bits &= ~0x2L;
                            _headers._GrpcMessage = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
            }

            return RemoveUnknown(key);
        }
        protected override void ClearFast()
        {
            MaybeUnknown?.Clear();
            _contentLength = null;
            var tempBits = _bits;
            _bits = 0;
            if(BitOperations.PopCount((ulong)tempBits) > 12)
            {
                _headers = default(HeaderReferences);
                return;
            }
            
            if ((tempBits & 0x1L) != 0)
            {
                _headers._ETag = default;
                if((tempBits & ~0x1L) == 0)
                {
                    return;
                }
                tempBits &= ~0x1L;
            }
            
            if ((tempBits & 0x2L) != 0)
            {
                _headers._GrpcMessage = default;
                if((tempBits & ~0x2L) == 0)
                {
                    return;
                }
                tempBits &= ~0x2L;
            }
            
            if ((tempBits & 0x4L) != 0)
            {
                _headers._GrpcStatus = default;
                if((tempBits & ~0x4L) == 0)
                {
                    return;
                }
                tempBits &= ~0x4L;
            }
            
        }

        protected override bool CopyToFast(KeyValuePair<string, StringValues>[] array, int arrayIndex)
        {
            if (arrayIndex < 0)
            {
                return false;
            }
            
                if ((_bits & 0x1L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ETag, _headers._ETag);
                    ++arrayIndex;
                }
                if ((_bits & 0x2L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.GrpcMessage, _headers._GrpcMessage);
                    ++arrayIndex;
                }
                if ((_bits & 0x4L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.GrpcStatus, _headers._GrpcStatus);
                    ++arrayIndex;
                }
                if (_contentLength.HasValue)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ContentLength, HeaderUtilities.FormatNonNegativeInt64(_contentLength.Value));
                    ++arrayIndex;
                }
            ((ICollection<KeyValuePair<string, StringValues>>?)MaybeUnknown)?.CopyTo(array, arrayIndex);

            return true;
        }
        

        private struct HeaderReferences
        {
            public StringValues _ETag;
            public StringValues _GrpcMessage;
            public StringValues _GrpcStatus;
            
        }

        public partial struct Enumerator
        {
            // Compiled to Jump table
            public bool MoveNext()
            {
                switch (_next)
                {
                    case 0: // Header: "ETag"
                        Debug.Assert((_currentBits & 0x1L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ETag, _collection._headers._ETag);
                        _currentKnownType = KnownHeaderType.ETag;
                        _currentBits ^= 0x1L;
                        break;
                    case 1: // Header: "Grpc-Message"
                        Debug.Assert((_currentBits & 0x2L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.GrpcMessage, _collection._headers._GrpcMessage);
                        _currentKnownType = KnownHeaderType.GrpcMessage;
                        _currentBits ^= 0x2L;
                        break;
                    case 2: // Header: "Grpc-Status"
                        Debug.Assert((_currentBits & 0x4L) != 0);
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.GrpcStatus, _collection._headers._GrpcStatus);
                        _currentKnownType = KnownHeaderType.GrpcStatus;
                        _currentBits ^= 0x4L;
                        break;
                    
                    default:
                        if (!_hasUnknown || !_unknownEnumerator.MoveNext())
                        {
                            _current = default(KeyValuePair<string, StringValues>);
                            _currentKnownType = default;
                            return false;
                        }
                        _current = _unknownEnumerator.Current;
                        _currentKnownType = KnownHeaderType.Unknown;
                        return true;
                }

                if (_currentBits != 0)
                {
                    _next = BitOperations.TrailingZeroCount(_currentBits);
                    return true;
                }
                else
                {
                    _next = -1;
                    return true;
                }
            }
        }
    }
}