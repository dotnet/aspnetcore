// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Buffers;
using System.IO.Pipelines;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

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
        Authority,
        Authorization,
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
        DNT,
        ETag,
        Expect,
        Expires,
        From,
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
        ProxyAuthenticate,
        ProxyAuthorization,
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

    internal partial class HttpRequestHeaders
    {
        private HeaderReferences _headers;

        public bool HasConnection => (_bits & 0x2L) != 0;
        public bool HasTransferEncoding => (_bits & 0x40L) != 0;

        public int HostCount => _headers._Host.Count;
        
        public StringValues HeaderCacheControl
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x1L) != 0)
                {
                    value = _headers._CacheControl;
                }
                return value;
            }
            set
            {
                _bits |= 0x1L;
                _headers._CacheControl = value; 
            }
        }
        public StringValues HeaderConnection
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x2L) != 0)
                {
                    value = _headers._Connection;
                }
                return value;
            }
            set
            {
                _bits |= 0x2L;
                _headers._Connection = value; 
            }
        }
        public StringValues HeaderDate
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x4L) != 0)
                {
                    value = _headers._Date;
                }
                return value;
            }
            set
            {
                _bits |= 0x4L;
                _headers._Date = value; 
            }
        }
        public StringValues HeaderKeepAlive
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x8L) != 0)
                {
                    value = _headers._KeepAlive;
                }
                return value;
            }
            set
            {
                _bits |= 0x8L;
                _headers._KeepAlive = value; 
            }
        }
        public StringValues HeaderPragma
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x10L) != 0)
                {
                    value = _headers._Pragma;
                }
                return value;
            }
            set
            {
                _bits |= 0x10L;
                _headers._Pragma = value; 
            }
        }
        public StringValues HeaderTrailer
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x20L) != 0)
                {
                    value = _headers._Trailer;
                }
                return value;
            }
            set
            {
                _bits |= 0x20L;
                _headers._Trailer = value; 
            }
        }
        public StringValues HeaderTransferEncoding
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x40L) != 0)
                {
                    value = _headers._TransferEncoding;
                }
                return value;
            }
            set
            {
                _bits |= 0x40L;
                _headers._TransferEncoding = value; 
            }
        }
        public StringValues HeaderUpgrade
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x80L) != 0)
                {
                    value = _headers._Upgrade;
                }
                return value;
            }
            set
            {
                _bits |= 0x80L;
                _headers._Upgrade = value; 
            }
        }
        public StringValues HeaderVia
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x100L) != 0)
                {
                    value = _headers._Via;
                }
                return value;
            }
            set
            {
                _bits |= 0x100L;
                _headers._Via = value; 
            }
        }
        public StringValues HeaderWarning
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x200L) != 0)
                {
                    value = _headers._Warning;
                }
                return value;
            }
            set
            {
                _bits |= 0x200L;
                _headers._Warning = value; 
            }
        }
        public StringValues HeaderAllow
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x400L) != 0)
                {
                    value = _headers._Allow;
                }
                return value;
            }
            set
            {
                _bits |= 0x400L;
                _headers._Allow = value; 
            }
        }
        public StringValues HeaderContentType
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x800L) != 0)
                {
                    value = _headers._ContentType;
                }
                return value;
            }
            set
            {
                _bits |= 0x800L;
                _headers._ContentType = value; 
            }
        }
        public StringValues HeaderContentEncoding
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x1000L) != 0)
                {
                    value = _headers._ContentEncoding;
                }
                return value;
            }
            set
            {
                _bits |= 0x1000L;
                _headers._ContentEncoding = value; 
            }
        }
        public StringValues HeaderContentLanguage
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x2000L) != 0)
                {
                    value = _headers._ContentLanguage;
                }
                return value;
            }
            set
            {
                _bits |= 0x2000L;
                _headers._ContentLanguage = value; 
            }
        }
        public StringValues HeaderContentLocation
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x4000L) != 0)
                {
                    value = _headers._ContentLocation;
                }
                return value;
            }
            set
            {
                _bits |= 0x4000L;
                _headers._ContentLocation = value; 
            }
        }
        public StringValues HeaderContentMD5
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x8000L) != 0)
                {
                    value = _headers._ContentMD5;
                }
                return value;
            }
            set
            {
                _bits |= 0x8000L;
                _headers._ContentMD5 = value; 
            }
        }
        public StringValues HeaderContentRange
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x10000L) != 0)
                {
                    value = _headers._ContentRange;
                }
                return value;
            }
            set
            {
                _bits |= 0x10000L;
                _headers._ContentRange = value; 
            }
        }
        public StringValues HeaderExpires
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x20000L) != 0)
                {
                    value = _headers._Expires;
                }
                return value;
            }
            set
            {
                _bits |= 0x20000L;
                _headers._Expires = value; 
            }
        }
        public StringValues HeaderLastModified
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x40000L) != 0)
                {
                    value = _headers._LastModified;
                }
                return value;
            }
            set
            {
                _bits |= 0x40000L;
                _headers._LastModified = value; 
            }
        }
        public StringValues HeaderAuthority
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x80000L) != 0)
                {
                    value = _headers._Authority;
                }
                return value;
            }
            set
            {
                _bits |= 0x80000L;
                _headers._Authority = value; 
            }
        }
        public StringValues HeaderMethod
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x100000L) != 0)
                {
                    value = _headers._Method;
                }
                return value;
            }
            set
            {
                _bits |= 0x100000L;
                _headers._Method = value; 
            }
        }
        public StringValues HeaderPath
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x200000L) != 0)
                {
                    value = _headers._Path;
                }
                return value;
            }
            set
            {
                _bits |= 0x200000L;
                _headers._Path = value; 
            }
        }
        public StringValues HeaderScheme
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x400000L) != 0)
                {
                    value = _headers._Scheme;
                }
                return value;
            }
            set
            {
                _bits |= 0x400000L;
                _headers._Scheme = value; 
            }
        }
        public StringValues HeaderAccept
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x800000L) != 0)
                {
                    value = _headers._Accept;
                }
                return value;
            }
            set
            {
                _bits |= 0x800000L;
                _headers._Accept = value; 
            }
        }
        public StringValues HeaderAcceptCharset
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x1000000L) != 0)
                {
                    value = _headers._AcceptCharset;
                }
                return value;
            }
            set
            {
                _bits |= 0x1000000L;
                _headers._AcceptCharset = value; 
            }
        }
        public StringValues HeaderAcceptEncoding
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x2000000L) != 0)
                {
                    value = _headers._AcceptEncoding;
                }
                return value;
            }
            set
            {
                _bits |= 0x2000000L;
                _headers._AcceptEncoding = value; 
            }
        }
        public StringValues HeaderAcceptLanguage
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x4000000L) != 0)
                {
                    value = _headers._AcceptLanguage;
                }
                return value;
            }
            set
            {
                _bits |= 0x4000000L;
                _headers._AcceptLanguage = value; 
            }
        }
        public StringValues HeaderAuthorization
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x8000000L) != 0)
                {
                    value = _headers._Authorization;
                }
                return value;
            }
            set
            {
                _bits |= 0x8000000L;
                _headers._Authorization = value; 
            }
        }
        public StringValues HeaderCookie
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x10000000L) != 0)
                {
                    value = _headers._Cookie;
                }
                return value;
            }
            set
            {
                _bits |= 0x10000000L;
                _headers._Cookie = value; 
            }
        }
        public StringValues HeaderExpect
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x20000000L) != 0)
                {
                    value = _headers._Expect;
                }
                return value;
            }
            set
            {
                _bits |= 0x20000000L;
                _headers._Expect = value; 
            }
        }
        public StringValues HeaderFrom
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x40000000L) != 0)
                {
                    value = _headers._From;
                }
                return value;
            }
            set
            {
                _bits |= 0x40000000L;
                _headers._From = value; 
            }
        }
        public StringValues HeaderHost
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x80000000L) != 0)
                {
                    value = _headers._Host;
                }
                return value;
            }
            set
            {
                _bits |= 0x80000000L;
                _headers._Host = value; 
            }
        }
        public StringValues HeaderIfMatch
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x100000000L) != 0)
                {
                    value = _headers._IfMatch;
                }
                return value;
            }
            set
            {
                _bits |= 0x100000000L;
                _headers._IfMatch = value; 
            }
        }
        public StringValues HeaderIfModifiedSince
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x200000000L) != 0)
                {
                    value = _headers._IfModifiedSince;
                }
                return value;
            }
            set
            {
                _bits |= 0x200000000L;
                _headers._IfModifiedSince = value; 
            }
        }
        public StringValues HeaderIfNoneMatch
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x400000000L) != 0)
                {
                    value = _headers._IfNoneMatch;
                }
                return value;
            }
            set
            {
                _bits |= 0x400000000L;
                _headers._IfNoneMatch = value; 
            }
        }
        public StringValues HeaderIfRange
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x800000000L) != 0)
                {
                    value = _headers._IfRange;
                }
                return value;
            }
            set
            {
                _bits |= 0x800000000L;
                _headers._IfRange = value; 
            }
        }
        public StringValues HeaderIfUnmodifiedSince
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x1000000000L) != 0)
                {
                    value = _headers._IfUnmodifiedSince;
                }
                return value;
            }
            set
            {
                _bits |= 0x1000000000L;
                _headers._IfUnmodifiedSince = value; 
            }
        }
        public StringValues HeaderMaxForwards
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x2000000000L) != 0)
                {
                    value = _headers._MaxForwards;
                }
                return value;
            }
            set
            {
                _bits |= 0x2000000000L;
                _headers._MaxForwards = value; 
            }
        }
        public StringValues HeaderProxyAuthorization
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x4000000000L) != 0)
                {
                    value = _headers._ProxyAuthorization;
                }
                return value;
            }
            set
            {
                _bits |= 0x4000000000L;
                _headers._ProxyAuthorization = value; 
            }
        }
        public StringValues HeaderReferer
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x8000000000L) != 0)
                {
                    value = _headers._Referer;
                }
                return value;
            }
            set
            {
                _bits |= 0x8000000000L;
                _headers._Referer = value; 
            }
        }
        public StringValues HeaderRange
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x10000000000L) != 0)
                {
                    value = _headers._Range;
                }
                return value;
            }
            set
            {
                _bits |= 0x10000000000L;
                _headers._Range = value; 
            }
        }
        public StringValues HeaderTE
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x20000000000L) != 0)
                {
                    value = _headers._TE;
                }
                return value;
            }
            set
            {
                _bits |= 0x20000000000L;
                _headers._TE = value; 
            }
        }
        public StringValues HeaderTranslate
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x40000000000L) != 0)
                {
                    value = _headers._Translate;
                }
                return value;
            }
            set
            {
                _bits |= 0x40000000000L;
                _headers._Translate = value; 
            }
        }
        public StringValues HeaderUserAgent
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x80000000000L) != 0)
                {
                    value = _headers._UserAgent;
                }
                return value;
            }
            set
            {
                _bits |= 0x80000000000L;
                _headers._UserAgent = value; 
            }
        }
        public StringValues HeaderDNT
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x100000000000L) != 0)
                {
                    value = _headers._DNT;
                }
                return value;
            }
            set
            {
                _bits |= 0x100000000000L;
                _headers._DNT = value; 
            }
        }
        public StringValues HeaderUpgradeInsecureRequests
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x200000000000L) != 0)
                {
                    value = _headers._UpgradeInsecureRequests;
                }
                return value;
            }
            set
            {
                _bits |= 0x200000000000L;
                _headers._UpgradeInsecureRequests = value; 
            }
        }
        public StringValues HeaderRequestId
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x400000000000L) != 0)
                {
                    value = _headers._RequestId;
                }
                return value;
            }
            set
            {
                _bits |= 0x400000000000L;
                _headers._RequestId = value; 
            }
        }
        public StringValues HeaderCorrelationContext
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x800000000000L) != 0)
                {
                    value = _headers._CorrelationContext;
                }
                return value;
            }
            set
            {
                _bits |= 0x800000000000L;
                _headers._CorrelationContext = value; 
            }
        }
        public StringValues HeaderTraceParent
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x1000000000000L) != 0)
                {
                    value = _headers._TraceParent;
                }
                return value;
            }
            set
            {
                _bits |= 0x1000000000000L;
                _headers._TraceParent = value; 
            }
        }
        public StringValues HeaderTraceState
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x2000000000000L) != 0)
                {
                    value = _headers._TraceState;
                }
                return value;
            }
            set
            {
                _bits |= 0x2000000000000L;
                _headers._TraceState = value; 
            }
        }
        public StringValues HeaderOrigin
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x4000000000000L) != 0)
                {
                    value = _headers._Origin;
                }
                return value;
            }
            set
            {
                _bits |= 0x4000000000000L;
                _headers._Origin = value; 
            }
        }
        public StringValues HeaderAccessControlRequestMethod
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x8000000000000L) != 0)
                {
                    value = _headers._AccessControlRequestMethod;
                }
                return value;
            }
            set
            {
                _bits |= 0x8000000000000L;
                _headers._AccessControlRequestMethod = value; 
            }
        }
        public StringValues HeaderAccessControlRequestHeaders
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x10000000000000L) != 0)
                {
                    value = _headers._AccessControlRequestHeaders;
                }
                return value;
            }
            set
            {
                _bits |= 0x10000000000000L;
                _headers._AccessControlRequestHeaders = value; 
            }
        }
        public StringValues HeaderContentLength
        {
            get
            {
                StringValues value = default;
                if (_contentLength.HasValue)
                {
                    value = new StringValues(HeaderUtilities.FormatNonNegativeInt64(_contentLength.Value));
                }
                return value;
            }
            set
            {
                _contentLength = ParseContentLength(value);
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
                        if ((_bits & 0x20000000000L) != 0)
                        {
                            value = _headers._TE;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.TE.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20000000000L) != 0)
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
                        if ((_bits & 0x100L) != 0)
                        {
                            value = _headers._Via;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.DNT, key))
                    {
                        if ((_bits & 0x100000000000L) != 0)
                        {
                            value = _headers._DNT;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.Via.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100L) != 0)
                        {
                            value = _headers._Via;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.DNT.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100000000000L) != 0)
                        {
                            value = _headers._DNT;
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
                        if ((_bits & 0x80000000L) != 0)
                        {
                            value = _headers._Host;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Date, key))
                    {
                        if ((_bits & 0x4L) != 0)
                        {
                            value = _headers._Date;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.From, key))
                    {
                        if ((_bits & 0x40000000L) != 0)
                        {
                            value = _headers._From;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.Host.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80000000L) != 0)
                        {
                            value = _headers._Host;
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
                    if (HeaderNames.From.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40000000L) != 0)
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
                    if (ReferenceEquals(HeaderNames.Path, key))
                    {
                        if ((_bits & 0x200000L) != 0)
                        {
                            value = _headers._Path;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Allow, key))
                    {
                        if ((_bits & 0x400L) != 0)
                        {
                            value = _headers._Allow;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Range, key))
                    {
                        if ((_bits & 0x10000000000L) != 0)
                        {
                            value = _headers._Range;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.Path.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200000L) != 0)
                        {
                            value = _headers._Path;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Allow.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400L) != 0)
                        {
                            value = _headers._Allow;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Range.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000000000L) != 0)
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
                        if ((_bits & 0x800000L) != 0)
                        {
                            value = _headers._Accept;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Pragma, key))
                    {
                        if ((_bits & 0x10L) != 0)
                        {
                            value = _headers._Pragma;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Cookie, key))
                    {
                        if ((_bits & 0x10000000L) != 0)
                        {
                            value = _headers._Cookie;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Expect, key))
                    {
                        if ((_bits & 0x20000000L) != 0)
                        {
                            value = _headers._Expect;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Origin, key))
                    {
                        if ((_bits & 0x4000000000000L) != 0)
                        {
                            value = _headers._Origin;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.Accept.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800000L) != 0)
                        {
                            value = _headers._Accept;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Pragma.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10L) != 0)
                        {
                            value = _headers._Pragma;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Cookie.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000000L) != 0)
                        {
                            value = _headers._Cookie;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Expect.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20000000L) != 0)
                        {
                            value = _headers._Expect;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Origin.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000000000000L) != 0)
                        {
                            value = _headers._Origin;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 7:
                {
                    if (ReferenceEquals(HeaderNames.Method, key))
                    {
                        if ((_bits & 0x100000L) != 0)
                        {
                            value = _headers._Method;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Scheme, key))
                    {
                        if ((_bits & 0x400000L) != 0)
                        {
                            value = _headers._Scheme;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Trailer, key))
                    {
                        if ((_bits & 0x20L) != 0)
                        {
                            value = _headers._Trailer;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Upgrade, key))
                    {
                        if ((_bits & 0x80L) != 0)
                        {
                            value = _headers._Upgrade;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Warning, key))
                    {
                        if ((_bits & 0x200L) != 0)
                        {
                            value = _headers._Warning;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Expires, key))
                    {
                        if ((_bits & 0x20000L) != 0)
                        {
                            value = _headers._Expires;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Referer, key))
                    {
                        if ((_bits & 0x8000000000L) != 0)
                        {
                            value = _headers._Referer;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.Method.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100000L) != 0)
                        {
                            value = _headers._Method;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Scheme.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400000L) != 0)
                        {
                            value = _headers._Scheme;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Trailer.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20L) != 0)
                        {
                            value = _headers._Trailer;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Upgrade.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80L) != 0)
                        {
                            value = _headers._Upgrade;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Warning.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200L) != 0)
                        {
                            value = _headers._Warning;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Expires.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20000L) != 0)
                        {
                            value = _headers._Expires;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Referer.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000000000L) != 0)
                        {
                            value = _headers._Referer;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 8:
                {
                    if (ReferenceEquals(HeaderNames.IfMatch, key))
                    {
                        if ((_bits & 0x100000000L) != 0)
                        {
                            value = _headers._IfMatch;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.IfRange, key))
                    {
                        if ((_bits & 0x800000000L) != 0)
                        {
                            value = _headers._IfRange;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.IfMatch.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100000000L) != 0)
                        {
                            value = _headers._IfMatch;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.IfRange.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800000000L) != 0)
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
                    if (ReferenceEquals(HeaderNames.Translate, key))
                    {
                        if ((_bits & 0x40000000000L) != 0)
                        {
                            value = _headers._Translate;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.Translate.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40000000000L) != 0)
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
                    if (ReferenceEquals(HeaderNames.Authority, key))
                    {
                        if ((_bits & 0x80000L) != 0)
                        {
                            value = _headers._Authority;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.UserAgent, key))
                    {
                        if ((_bits & 0x80000000000L) != 0)
                        {
                            value = _headers._UserAgent;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.KeepAlive, key))
                    {
                        if ((_bits & 0x8L) != 0)
                        {
                            value = _headers._KeepAlive;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.RequestId, key))
                    {
                        if ((_bits & 0x400000000000L) != 0)
                        {
                            value = _headers._RequestId;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.TraceState, key))
                    {
                        if ((_bits & 0x2000000000000L) != 0)
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
                    if (HeaderNames.Authority.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80000L) != 0)
                        {
                            value = _headers._Authority;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.UserAgent.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80000000000L) != 0)
                        {
                            value = _headers._UserAgent;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.KeepAlive.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8L) != 0)
                        {
                            value = _headers._KeepAlive;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.RequestId.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400000000000L) != 0)
                        {
                            value = _headers._RequestId;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.TraceState.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000000000000L) != 0)
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
                    if (ReferenceEquals(HeaderNames.ContentMD5, key))
                    {
                        if ((_bits & 0x8000L) != 0)
                        {
                            value = _headers._ContentMD5;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.TraceParent, key))
                    {
                        if ((_bits & 0x1000000000000L) != 0)
                        {
                            value = _headers._TraceParent;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.ContentMD5.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000L) != 0)
                        {
                            value = _headers._ContentMD5;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.TraceParent.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000000000000L) != 0)
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
                        if ((_bits & 0x800L) != 0)
                        {
                            value = _headers._ContentType;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.MaxForwards, key))
                    {
                        if ((_bits & 0x2000000000L) != 0)
                        {
                            value = _headers._MaxForwards;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.ContentType.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800L) != 0)
                        {
                            value = _headers._ContentType;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.MaxForwards.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000000000L) != 0)
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
                    if (ReferenceEquals(HeaderNames.CacheControl, key))
                    {
                        if ((_bits & 0x1L) != 0)
                        {
                            value = _headers._CacheControl;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentRange, key))
                    {
                        if ((_bits & 0x10000L) != 0)
                        {
                            value = _headers._ContentRange;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.LastModified, key))
                    {
                        if ((_bits & 0x40000L) != 0)
                        {
                            value = _headers._LastModified;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Authorization, key))
                    {
                        if ((_bits & 0x8000000L) != 0)
                        {
                            value = _headers._Authorization;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.IfNoneMatch, key))
                    {
                        if ((_bits & 0x400000000L) != 0)
                        {
                            value = _headers._IfNoneMatch;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.CacheControl.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1L) != 0)
                        {
                            value = _headers._CacheControl;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentRange.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000L) != 0)
                        {
                            value = _headers._ContentRange;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.LastModified.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40000L) != 0)
                        {
                            value = _headers._LastModified;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Authorization.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000000L) != 0)
                        {
                            value = _headers._Authorization;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.IfNoneMatch.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400000000L) != 0)
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
                        if ((_bits & 0x1000000L) != 0)
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
                        if ((_bits & 0x1000000L) != 0)
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
                        if ((_bits & 0x2000000L) != 0)
                        {
                            value = _headers._AcceptEncoding;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.AcceptLanguage, key))
                    {
                        if ((_bits & 0x4000000L) != 0)
                        {
                            value = _headers._AcceptLanguage;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.AcceptEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000000L) != 0)
                        {
                            value = _headers._AcceptEncoding;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.AcceptLanguage.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000000L) != 0)
                        {
                            value = _headers._AcceptLanguage;
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
                        if ((_bits & 0x1000L) != 0)
                        {
                            value = _headers._ContentEncoding;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentLanguage, key))
                    {
                        if ((_bits & 0x2000L) != 0)
                        {
                            value = _headers._ContentLanguage;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentLocation, key))
                    {
                        if ((_bits & 0x4000L) != 0)
                        {
                            value = _headers._ContentLocation;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.ContentEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000L) != 0)
                        {
                            value = _headers._ContentEncoding;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentLanguage.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000L) != 0)
                        {
                            value = _headers._ContentLanguage;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentLocation.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000L) != 0)
                        {
                            value = _headers._ContentLocation;
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
                        if ((_bits & 0x40L) != 0)
                        {
                            value = _headers._TransferEncoding;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.IfModifiedSince, key))
                    {
                        if ((_bits & 0x200000000L) != 0)
                        {
                            value = _headers._IfModifiedSince;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.TransferEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40L) != 0)
                        {
                            value = _headers._TransferEncoding;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.IfModifiedSince.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200000000L) != 0)
                        {
                            value = _headers._IfModifiedSince;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 19:
                {
                    if (ReferenceEquals(HeaderNames.IfUnmodifiedSince, key))
                    {
                        if ((_bits & 0x1000000000L) != 0)
                        {
                            value = _headers._IfUnmodifiedSince;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ProxyAuthorization, key))
                    {
                        if ((_bits & 0x4000000000L) != 0)
                        {
                            value = _headers._ProxyAuthorization;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.CorrelationContext, key))
                    {
                        if ((_bits & 0x800000000000L) != 0)
                        {
                            value = _headers._CorrelationContext;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.IfUnmodifiedSince.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000000000L) != 0)
                        {
                            value = _headers._IfUnmodifiedSince;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ProxyAuthorization.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000000000L) != 0)
                        {
                            value = _headers._ProxyAuthorization;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.CorrelationContext.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800000000000L) != 0)
                        {
                            value = _headers._CorrelationContext;
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
                        if ((_bits & 0x200000000000L) != 0)
                        {
                            value = _headers._UpgradeInsecureRequests;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.UpgradeInsecureRequests.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200000000000L) != 0)
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
                        if ((_bits & 0x8000000000000L) != 0)
                        {
                            value = _headers._AccessControlRequestMethod;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.AccessControlRequestMethod.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000000000000L) != 0)
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
                        if ((_bits & 0x10000000000000L) != 0)
                        {
                            value = _headers._AccessControlRequestHeaders;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.AccessControlRequestHeaders.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000000000000L) != 0)
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
                        _bits |= 0x20000000000L;
                        _headers._TE = value;
                        return;
                    }

                    if (HeaderNames.TE.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x20000000000L;
                        _headers._TE = value;
                        return;
                    }
                    break;
                }
                case 3:
                {
                    if (ReferenceEquals(HeaderNames.Via, key))
                    {
                        _bits |= 0x100L;
                        _headers._Via = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.DNT, key))
                    {
                        _bits |= 0x100000000000L;
                        _headers._DNT = value;
                        return;
                    }

                    if (HeaderNames.Via.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x100L;
                        _headers._Via = value;
                        return;
                    }
                    if (HeaderNames.DNT.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x100000000000L;
                        _headers._DNT = value;
                        return;
                    }
                    break;
                }
                case 4:
                {
                    if (ReferenceEquals(HeaderNames.Host, key))
                    {
                        _bits |= 0x80000000L;
                        _headers._Host = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Date, key))
                    {
                        _bits |= 0x4L;
                        _headers._Date = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.From, key))
                    {
                        _bits |= 0x40000000L;
                        _headers._From = value;
                        return;
                    }

                    if (HeaderNames.Host.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x80000000L;
                        _headers._Host = value;
                        return;
                    }
                    if (HeaderNames.Date.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x4L;
                        _headers._Date = value;
                        return;
                    }
                    if (HeaderNames.From.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x40000000L;
                        _headers._From = value;
                        return;
                    }
                    break;
                }
                case 5:
                {
                    if (ReferenceEquals(HeaderNames.Path, key))
                    {
                        _bits |= 0x200000L;
                        _headers._Path = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Allow, key))
                    {
                        _bits |= 0x400L;
                        _headers._Allow = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Range, key))
                    {
                        _bits |= 0x10000000000L;
                        _headers._Range = value;
                        return;
                    }

                    if (HeaderNames.Path.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x200000L;
                        _headers._Path = value;
                        return;
                    }
                    if (HeaderNames.Allow.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x400L;
                        _headers._Allow = value;
                        return;
                    }
                    if (HeaderNames.Range.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x10000000000L;
                        _headers._Range = value;
                        return;
                    }
                    break;
                }
                case 6:
                {
                    if (ReferenceEquals(HeaderNames.Accept, key))
                    {
                        _bits |= 0x800000L;
                        _headers._Accept = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Pragma, key))
                    {
                        _bits |= 0x10L;
                        _headers._Pragma = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Cookie, key))
                    {
                        _bits |= 0x10000000L;
                        _headers._Cookie = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Expect, key))
                    {
                        _bits |= 0x20000000L;
                        _headers._Expect = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Origin, key))
                    {
                        _bits |= 0x4000000000000L;
                        _headers._Origin = value;
                        return;
                    }

                    if (HeaderNames.Accept.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x800000L;
                        _headers._Accept = value;
                        return;
                    }
                    if (HeaderNames.Pragma.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x10L;
                        _headers._Pragma = value;
                        return;
                    }
                    if (HeaderNames.Cookie.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x10000000L;
                        _headers._Cookie = value;
                        return;
                    }
                    if (HeaderNames.Expect.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x20000000L;
                        _headers._Expect = value;
                        return;
                    }
                    if (HeaderNames.Origin.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x4000000000000L;
                        _headers._Origin = value;
                        return;
                    }
                    break;
                }
                case 7:
                {
                    if (ReferenceEquals(HeaderNames.Method, key))
                    {
                        _bits |= 0x100000L;
                        _headers._Method = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Scheme, key))
                    {
                        _bits |= 0x400000L;
                        _headers._Scheme = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Trailer, key))
                    {
                        _bits |= 0x20L;
                        _headers._Trailer = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Upgrade, key))
                    {
                        _bits |= 0x80L;
                        _headers._Upgrade = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Warning, key))
                    {
                        _bits |= 0x200L;
                        _headers._Warning = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Expires, key))
                    {
                        _bits |= 0x20000L;
                        _headers._Expires = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Referer, key))
                    {
                        _bits |= 0x8000000000L;
                        _headers._Referer = value;
                        return;
                    }

                    if (HeaderNames.Method.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x100000L;
                        _headers._Method = value;
                        return;
                    }
                    if (HeaderNames.Scheme.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x400000L;
                        _headers._Scheme = value;
                        return;
                    }
                    if (HeaderNames.Trailer.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x20L;
                        _headers._Trailer = value;
                        return;
                    }
                    if (HeaderNames.Upgrade.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x80L;
                        _headers._Upgrade = value;
                        return;
                    }
                    if (HeaderNames.Warning.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x200L;
                        _headers._Warning = value;
                        return;
                    }
                    if (HeaderNames.Expires.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x20000L;
                        _headers._Expires = value;
                        return;
                    }
                    if (HeaderNames.Referer.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x8000000000L;
                        _headers._Referer = value;
                        return;
                    }
                    break;
                }
                case 8:
                {
                    if (ReferenceEquals(HeaderNames.IfMatch, key))
                    {
                        _bits |= 0x100000000L;
                        _headers._IfMatch = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.IfRange, key))
                    {
                        _bits |= 0x800000000L;
                        _headers._IfRange = value;
                        return;
                    }

                    if (HeaderNames.IfMatch.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x100000000L;
                        _headers._IfMatch = value;
                        return;
                    }
                    if (HeaderNames.IfRange.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x800000000L;
                        _headers._IfRange = value;
                        return;
                    }
                    break;
                }
                case 9:
                {
                    if (ReferenceEquals(HeaderNames.Translate, key))
                    {
                        _bits |= 0x40000000000L;
                        _headers._Translate = value;
                        return;
                    }

                    if (HeaderNames.Translate.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x40000000000L;
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
                    if (ReferenceEquals(HeaderNames.Authority, key))
                    {
                        _bits |= 0x80000L;
                        _headers._Authority = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.UserAgent, key))
                    {
                        _bits |= 0x80000000000L;
                        _headers._UserAgent = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.KeepAlive, key))
                    {
                        _bits |= 0x8L;
                        _headers._KeepAlive = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.RequestId, key))
                    {
                        _bits |= 0x400000000000L;
                        _headers._RequestId = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.TraceState, key))
                    {
                        _bits |= 0x2000000000000L;
                        _headers._TraceState = value;
                        return;
                    }

                    if (HeaderNames.Connection.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x2L;
                        _headers._Connection = value;
                        return;
                    }
                    if (HeaderNames.Authority.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x80000L;
                        _headers._Authority = value;
                        return;
                    }
                    if (HeaderNames.UserAgent.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x80000000000L;
                        _headers._UserAgent = value;
                        return;
                    }
                    if (HeaderNames.KeepAlive.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x8L;
                        _headers._KeepAlive = value;
                        return;
                    }
                    if (HeaderNames.RequestId.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x400000000000L;
                        _headers._RequestId = value;
                        return;
                    }
                    if (HeaderNames.TraceState.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x2000000000000L;
                        _headers._TraceState = value;
                        return;
                    }
                    break;
                }
                case 11:
                {
                    if (ReferenceEquals(HeaderNames.ContentMD5, key))
                    {
                        _bits |= 0x8000L;
                        _headers._ContentMD5 = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.TraceParent, key))
                    {
                        _bits |= 0x1000000000000L;
                        _headers._TraceParent = value;
                        return;
                    }

                    if (HeaderNames.ContentMD5.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x8000L;
                        _headers._ContentMD5 = value;
                        return;
                    }
                    if (HeaderNames.TraceParent.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x1000000000000L;
                        _headers._TraceParent = value;
                        return;
                    }
                    break;
                }
                case 12:
                {
                    if (ReferenceEquals(HeaderNames.ContentType, key))
                    {
                        _bits |= 0x800L;
                        _headers._ContentType = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.MaxForwards, key))
                    {
                        _bits |= 0x2000000000L;
                        _headers._MaxForwards = value;
                        return;
                    }

                    if (HeaderNames.ContentType.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x800L;
                        _headers._ContentType = value;
                        return;
                    }
                    if (HeaderNames.MaxForwards.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x2000000000L;
                        _headers._MaxForwards = value;
                        return;
                    }
                    break;
                }
                case 13:
                {
                    if (ReferenceEquals(HeaderNames.CacheControl, key))
                    {
                        _bits |= 0x1L;
                        _headers._CacheControl = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.ContentRange, key))
                    {
                        _bits |= 0x10000L;
                        _headers._ContentRange = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.LastModified, key))
                    {
                        _bits |= 0x40000L;
                        _headers._LastModified = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Authorization, key))
                    {
                        _bits |= 0x8000000L;
                        _headers._Authorization = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.IfNoneMatch, key))
                    {
                        _bits |= 0x400000000L;
                        _headers._IfNoneMatch = value;
                        return;
                    }

                    if (HeaderNames.CacheControl.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x1L;
                        _headers._CacheControl = value;
                        return;
                    }
                    if (HeaderNames.ContentRange.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x10000L;
                        _headers._ContentRange = value;
                        return;
                    }
                    if (HeaderNames.LastModified.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x40000L;
                        _headers._LastModified = value;
                        return;
                    }
                    if (HeaderNames.Authorization.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x8000000L;
                        _headers._Authorization = value;
                        return;
                    }
                    if (HeaderNames.IfNoneMatch.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x400000000L;
                        _headers._IfNoneMatch = value;
                        return;
                    }
                    break;
                }
                case 14:
                {
                    if (ReferenceEquals(HeaderNames.AcceptCharset, key))
                    {
                        _bits |= 0x1000000L;
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
                        _bits |= 0x1000000L;
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
                        _bits |= 0x2000000L;
                        _headers._AcceptEncoding = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.AcceptLanguage, key))
                    {
                        _bits |= 0x4000000L;
                        _headers._AcceptLanguage = value;
                        return;
                    }

                    if (HeaderNames.AcceptEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x2000000L;
                        _headers._AcceptEncoding = value;
                        return;
                    }
                    if (HeaderNames.AcceptLanguage.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x4000000L;
                        _headers._AcceptLanguage = value;
                        return;
                    }
                    break;
                }
                case 16:
                {
                    if (ReferenceEquals(HeaderNames.ContentEncoding, key))
                    {
                        _bits |= 0x1000L;
                        _headers._ContentEncoding = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.ContentLanguage, key))
                    {
                        _bits |= 0x2000L;
                        _headers._ContentLanguage = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.ContentLocation, key))
                    {
                        _bits |= 0x4000L;
                        _headers._ContentLocation = value;
                        return;
                    }

                    if (HeaderNames.ContentEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x1000L;
                        _headers._ContentEncoding = value;
                        return;
                    }
                    if (HeaderNames.ContentLanguage.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x2000L;
                        _headers._ContentLanguage = value;
                        return;
                    }
                    if (HeaderNames.ContentLocation.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x4000L;
                        _headers._ContentLocation = value;
                        return;
                    }
                    break;
                }
                case 17:
                {
                    if (ReferenceEquals(HeaderNames.TransferEncoding, key))
                    {
                        _bits |= 0x40L;
                        _headers._TransferEncoding = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.IfModifiedSince, key))
                    {
                        _bits |= 0x200000000L;
                        _headers._IfModifiedSince = value;
                        return;
                    }

                    if (HeaderNames.TransferEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x40L;
                        _headers._TransferEncoding = value;
                        return;
                    }
                    if (HeaderNames.IfModifiedSince.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x200000000L;
                        _headers._IfModifiedSince = value;
                        return;
                    }
                    break;
                }
                case 19:
                {
                    if (ReferenceEquals(HeaderNames.IfUnmodifiedSince, key))
                    {
                        _bits |= 0x1000000000L;
                        _headers._IfUnmodifiedSince = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.ProxyAuthorization, key))
                    {
                        _bits |= 0x4000000000L;
                        _headers._ProxyAuthorization = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.CorrelationContext, key))
                    {
                        _bits |= 0x800000000000L;
                        _headers._CorrelationContext = value;
                        return;
                    }

                    if (HeaderNames.IfUnmodifiedSince.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x1000000000L;
                        _headers._IfUnmodifiedSince = value;
                        return;
                    }
                    if (HeaderNames.ProxyAuthorization.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x4000000000L;
                        _headers._ProxyAuthorization = value;
                        return;
                    }
                    if (HeaderNames.CorrelationContext.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x800000000000L;
                        _headers._CorrelationContext = value;
                        return;
                    }
                    break;
                }
                case 25:
                {
                    if (ReferenceEquals(HeaderNames.UpgradeInsecureRequests, key))
                    {
                        _bits |= 0x200000000000L;
                        _headers._UpgradeInsecureRequests = value;
                        return;
                    }

                    if (HeaderNames.UpgradeInsecureRequests.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x200000000000L;
                        _headers._UpgradeInsecureRequests = value;
                        return;
                    }
                    break;
                }
                case 29:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlRequestMethod, key))
                    {
                        _bits |= 0x8000000000000L;
                        _headers._AccessControlRequestMethod = value;
                        return;
                    }

                    if (HeaderNames.AccessControlRequestMethod.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x8000000000000L;
                        _headers._AccessControlRequestMethod = value;
                        return;
                    }
                    break;
                }
                case 30:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlRequestHeaders, key))
                    {
                        _bits |= 0x10000000000000L;
                        _headers._AccessControlRequestHeaders = value;
                        return;
                    }

                    if (HeaderNames.AccessControlRequestHeaders.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x10000000000000L;
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
                        if ((_bits & 0x20000000000L) == 0)
                        {
                            _bits |= 0x20000000000L;
                            _headers._TE = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.TE.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20000000000L) == 0)
                        {
                            _bits |= 0x20000000000L;
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
                        if ((_bits & 0x100L) == 0)
                        {
                            _bits |= 0x100L;
                            _headers._Via = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.DNT, key))
                    {
                        if ((_bits & 0x100000000000L) == 0)
                        {
                            _bits |= 0x100000000000L;
                            _headers._DNT = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Via.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100L) == 0)
                        {
                            _bits |= 0x100L;
                            _headers._Via = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.DNT.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100000000000L) == 0)
                        {
                            _bits |= 0x100000000000L;
                            _headers._DNT = value;
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
                        if ((_bits & 0x80000000L) == 0)
                        {
                            _bits |= 0x80000000L;
                            _headers._Host = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Date, key))
                    {
                        if ((_bits & 0x4L) == 0)
                        {
                            _bits |= 0x4L;
                            _headers._Date = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.From, key))
                    {
                        if ((_bits & 0x40000000L) == 0)
                        {
                            _bits |= 0x40000000L;
                            _headers._From = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Host.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80000000L) == 0)
                        {
                            _bits |= 0x80000000L;
                            _headers._Host = value;
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
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.From.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40000000L) == 0)
                        {
                            _bits |= 0x40000000L;
                            _headers._From = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 5:
                {
                    if (ReferenceEquals(HeaderNames.Path, key))
                    {
                        if ((_bits & 0x200000L) == 0)
                        {
                            _bits |= 0x200000L;
                            _headers._Path = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Allow, key))
                    {
                        if ((_bits & 0x400L) == 0)
                        {
                            _bits |= 0x400L;
                            _headers._Allow = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Range, key))
                    {
                        if ((_bits & 0x10000000000L) == 0)
                        {
                            _bits |= 0x10000000000L;
                            _headers._Range = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Path.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200000L) == 0)
                        {
                            _bits |= 0x200000L;
                            _headers._Path = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Allow.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400L) == 0)
                        {
                            _bits |= 0x400L;
                            _headers._Allow = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Range.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000000000L) == 0)
                        {
                            _bits |= 0x10000000000L;
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
                        if ((_bits & 0x800000L) == 0)
                        {
                            _bits |= 0x800000L;
                            _headers._Accept = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Pragma, key))
                    {
                        if ((_bits & 0x10L) == 0)
                        {
                            _bits |= 0x10L;
                            _headers._Pragma = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Cookie, key))
                    {
                        if ((_bits & 0x10000000L) == 0)
                        {
                            _bits |= 0x10000000L;
                            _headers._Cookie = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Expect, key))
                    {
                        if ((_bits & 0x20000000L) == 0)
                        {
                            _bits |= 0x20000000L;
                            _headers._Expect = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Origin, key))
                    {
                        if ((_bits & 0x4000000000000L) == 0)
                        {
                            _bits |= 0x4000000000000L;
                            _headers._Origin = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Accept.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800000L) == 0)
                        {
                            _bits |= 0x800000L;
                            _headers._Accept = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Pragma.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10L) == 0)
                        {
                            _bits |= 0x10L;
                            _headers._Pragma = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Cookie.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000000L) == 0)
                        {
                            _bits |= 0x10000000L;
                            _headers._Cookie = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Expect.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20000000L) == 0)
                        {
                            _bits |= 0x20000000L;
                            _headers._Expect = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Origin.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000000000000L) == 0)
                        {
                            _bits |= 0x4000000000000L;
                            _headers._Origin = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 7:
                {
                    if (ReferenceEquals(HeaderNames.Method, key))
                    {
                        if ((_bits & 0x100000L) == 0)
                        {
                            _bits |= 0x100000L;
                            _headers._Method = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Scheme, key))
                    {
                        if ((_bits & 0x400000L) == 0)
                        {
                            _bits |= 0x400000L;
                            _headers._Scheme = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Trailer, key))
                    {
                        if ((_bits & 0x20L) == 0)
                        {
                            _bits |= 0x20L;
                            _headers._Trailer = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Upgrade, key))
                    {
                        if ((_bits & 0x80L) == 0)
                        {
                            _bits |= 0x80L;
                            _headers._Upgrade = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Warning, key))
                    {
                        if ((_bits & 0x200L) == 0)
                        {
                            _bits |= 0x200L;
                            _headers._Warning = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Expires, key))
                    {
                        if ((_bits & 0x20000L) == 0)
                        {
                            _bits |= 0x20000L;
                            _headers._Expires = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Referer, key))
                    {
                        if ((_bits & 0x8000000000L) == 0)
                        {
                            _bits |= 0x8000000000L;
                            _headers._Referer = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Method.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100000L) == 0)
                        {
                            _bits |= 0x100000L;
                            _headers._Method = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Scheme.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400000L) == 0)
                        {
                            _bits |= 0x400000L;
                            _headers._Scheme = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Trailer.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20L) == 0)
                        {
                            _bits |= 0x20L;
                            _headers._Trailer = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Upgrade.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80L) == 0)
                        {
                            _bits |= 0x80L;
                            _headers._Upgrade = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Warning.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200L) == 0)
                        {
                            _bits |= 0x200L;
                            _headers._Warning = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Expires.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20000L) == 0)
                        {
                            _bits |= 0x20000L;
                            _headers._Expires = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Referer.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000000000L) == 0)
                        {
                            _bits |= 0x8000000000L;
                            _headers._Referer = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 8:
                {
                    if (ReferenceEquals(HeaderNames.IfMatch, key))
                    {
                        if ((_bits & 0x100000000L) == 0)
                        {
                            _bits |= 0x100000000L;
                            _headers._IfMatch = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.IfRange, key))
                    {
                        if ((_bits & 0x800000000L) == 0)
                        {
                            _bits |= 0x800000000L;
                            _headers._IfRange = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.IfMatch.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100000000L) == 0)
                        {
                            _bits |= 0x100000000L;
                            _headers._IfMatch = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.IfRange.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800000000L) == 0)
                        {
                            _bits |= 0x800000000L;
                            _headers._IfRange = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 9:
                {
                    if (ReferenceEquals(HeaderNames.Translate, key))
                    {
                        if ((_bits & 0x40000000000L) == 0)
                        {
                            _bits |= 0x40000000000L;
                            _headers._Translate = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Translate.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40000000000L) == 0)
                        {
                            _bits |= 0x40000000000L;
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
                    if (ReferenceEquals(HeaderNames.Authority, key))
                    {
                        if ((_bits & 0x80000L) == 0)
                        {
                            _bits |= 0x80000L;
                            _headers._Authority = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.UserAgent, key))
                    {
                        if ((_bits & 0x80000000000L) == 0)
                        {
                            _bits |= 0x80000000000L;
                            _headers._UserAgent = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.KeepAlive, key))
                    {
                        if ((_bits & 0x8L) == 0)
                        {
                            _bits |= 0x8L;
                            _headers._KeepAlive = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.RequestId, key))
                    {
                        if ((_bits & 0x400000000000L) == 0)
                        {
                            _bits |= 0x400000000000L;
                            _headers._RequestId = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.TraceState, key))
                    {
                        if ((_bits & 0x2000000000000L) == 0)
                        {
                            _bits |= 0x2000000000000L;
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
                    if (HeaderNames.Authority.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80000L) == 0)
                        {
                            _bits |= 0x80000L;
                            _headers._Authority = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.UserAgent.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80000000000L) == 0)
                        {
                            _bits |= 0x80000000000L;
                            _headers._UserAgent = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.KeepAlive.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8L) == 0)
                        {
                            _bits |= 0x8L;
                            _headers._KeepAlive = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.RequestId.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400000000000L) == 0)
                        {
                            _bits |= 0x400000000000L;
                            _headers._RequestId = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.TraceState.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000000000000L) == 0)
                        {
                            _bits |= 0x2000000000000L;
                            _headers._TraceState = value;
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
                        if ((_bits & 0x8000L) == 0)
                        {
                            _bits |= 0x8000L;
                            _headers._ContentMD5 = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.TraceParent, key))
                    {
                        if ((_bits & 0x1000000000000L) == 0)
                        {
                            _bits |= 0x1000000000000L;
                            _headers._TraceParent = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.ContentMD5.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000L) == 0)
                        {
                            _bits |= 0x8000L;
                            _headers._ContentMD5 = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.TraceParent.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000000000000L) == 0)
                        {
                            _bits |= 0x1000000000000L;
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
                        if ((_bits & 0x800L) == 0)
                        {
                            _bits |= 0x800L;
                            _headers._ContentType = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.MaxForwards, key))
                    {
                        if ((_bits & 0x2000000000L) == 0)
                        {
                            _bits |= 0x2000000000L;
                            _headers._MaxForwards = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.ContentType.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800L) == 0)
                        {
                            _bits |= 0x800L;
                            _headers._ContentType = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.MaxForwards.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000000000L) == 0)
                        {
                            _bits |= 0x2000000000L;
                            _headers._MaxForwards = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 13:
                {
                    if (ReferenceEquals(HeaderNames.CacheControl, key))
                    {
                        if ((_bits & 0x1L) == 0)
                        {
                            _bits |= 0x1L;
                            _headers._CacheControl = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentRange, key))
                    {
                        if ((_bits & 0x10000L) == 0)
                        {
                            _bits |= 0x10000L;
                            _headers._ContentRange = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.LastModified, key))
                    {
                        if ((_bits & 0x40000L) == 0)
                        {
                            _bits |= 0x40000L;
                            _headers._LastModified = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Authorization, key))
                    {
                        if ((_bits & 0x8000000L) == 0)
                        {
                            _bits |= 0x8000000L;
                            _headers._Authorization = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.IfNoneMatch, key))
                    {
                        if ((_bits & 0x400000000L) == 0)
                        {
                            _bits |= 0x400000000L;
                            _headers._IfNoneMatch = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.CacheControl.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1L) == 0)
                        {
                            _bits |= 0x1L;
                            _headers._CacheControl = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentRange.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000L) == 0)
                        {
                            _bits |= 0x10000L;
                            _headers._ContentRange = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.LastModified.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40000L) == 0)
                        {
                            _bits |= 0x40000L;
                            _headers._LastModified = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Authorization.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000000L) == 0)
                        {
                            _bits |= 0x8000000L;
                            _headers._Authorization = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.IfNoneMatch.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400000000L) == 0)
                        {
                            _bits |= 0x400000000L;
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
                        if ((_bits & 0x1000000L) == 0)
                        {
                            _bits |= 0x1000000L;
                            _headers._AcceptCharset = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentLength, key))
                    {
                        if (!_contentLength.HasValue)
                        {
                            _contentLength = ParseContentLength(value);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AcceptCharset.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000000L) == 0)
                        {
                            _bits |= 0x1000000L;
                            _headers._AcceptCharset = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentLength.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!_contentLength.HasValue)
                        {
                            _contentLength = ParseContentLength(value);
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
                        if ((_bits & 0x2000000L) == 0)
                        {
                            _bits |= 0x2000000L;
                            _headers._AcceptEncoding = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.AcceptLanguage, key))
                    {
                        if ((_bits & 0x4000000L) == 0)
                        {
                            _bits |= 0x4000000L;
                            _headers._AcceptLanguage = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AcceptEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000000L) == 0)
                        {
                            _bits |= 0x2000000L;
                            _headers._AcceptEncoding = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.AcceptLanguage.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000000L) == 0)
                        {
                            _bits |= 0x4000000L;
                            _headers._AcceptLanguage = value;
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
                        if ((_bits & 0x1000L) == 0)
                        {
                            _bits |= 0x1000L;
                            _headers._ContentEncoding = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentLanguage, key))
                    {
                        if ((_bits & 0x2000L) == 0)
                        {
                            _bits |= 0x2000L;
                            _headers._ContentLanguage = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentLocation, key))
                    {
                        if ((_bits & 0x4000L) == 0)
                        {
                            _bits |= 0x4000L;
                            _headers._ContentLocation = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.ContentEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000L) == 0)
                        {
                            _bits |= 0x1000L;
                            _headers._ContentEncoding = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentLanguage.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000L) == 0)
                        {
                            _bits |= 0x2000L;
                            _headers._ContentLanguage = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentLocation.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000L) == 0)
                        {
                            _bits |= 0x4000L;
                            _headers._ContentLocation = value;
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
                        if ((_bits & 0x40L) == 0)
                        {
                            _bits |= 0x40L;
                            _headers._TransferEncoding = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.IfModifiedSince, key))
                    {
                        if ((_bits & 0x200000000L) == 0)
                        {
                            _bits |= 0x200000000L;
                            _headers._IfModifiedSince = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.TransferEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40L) == 0)
                        {
                            _bits |= 0x40L;
                            _headers._TransferEncoding = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.IfModifiedSince.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200000000L) == 0)
                        {
                            _bits |= 0x200000000L;
                            _headers._IfModifiedSince = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 19:
                {
                    if (ReferenceEquals(HeaderNames.IfUnmodifiedSince, key))
                    {
                        if ((_bits & 0x1000000000L) == 0)
                        {
                            _bits |= 0x1000000000L;
                            _headers._IfUnmodifiedSince = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ProxyAuthorization, key))
                    {
                        if ((_bits & 0x4000000000L) == 0)
                        {
                            _bits |= 0x4000000000L;
                            _headers._ProxyAuthorization = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.CorrelationContext, key))
                    {
                        if ((_bits & 0x800000000000L) == 0)
                        {
                            _bits |= 0x800000000000L;
                            _headers._CorrelationContext = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.IfUnmodifiedSince.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000000000L) == 0)
                        {
                            _bits |= 0x1000000000L;
                            _headers._IfUnmodifiedSince = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ProxyAuthorization.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000000000L) == 0)
                        {
                            _bits |= 0x4000000000L;
                            _headers._ProxyAuthorization = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.CorrelationContext.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800000000000L) == 0)
                        {
                            _bits |= 0x800000000000L;
                            _headers._CorrelationContext = value;
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
                        if ((_bits & 0x200000000000L) == 0)
                        {
                            _bits |= 0x200000000000L;
                            _headers._UpgradeInsecureRequests = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.UpgradeInsecureRequests.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200000000000L) == 0)
                        {
                            _bits |= 0x200000000000L;
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
                        if ((_bits & 0x8000000000000L) == 0)
                        {
                            _bits |= 0x8000000000000L;
                            _headers._AccessControlRequestMethod = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AccessControlRequestMethod.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000000000000L) == 0)
                        {
                            _bits |= 0x8000000000000L;
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
                        if ((_bits & 0x10000000000000L) == 0)
                        {
                            _bits |= 0x10000000000000L;
                            _headers._AccessControlRequestHeaders = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AccessControlRequestHeaders.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000000000000L) == 0)
                        {
                            _bits |= 0x10000000000000L;
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
                        if ((_bits & 0x20000000000L) != 0)
                        {
                            _bits &= ~0x20000000000L;
                            _headers._TE = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.TE.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20000000000L) != 0)
                        {
                            _bits &= ~0x20000000000L;
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
                        if ((_bits & 0x100L) != 0)
                        {
                            _bits &= ~0x100L;
                            _headers._Via = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.DNT, key))
                    {
                        if ((_bits & 0x100000000000L) != 0)
                        {
                            _bits &= ~0x100000000000L;
                            _headers._DNT = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Via.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100L) != 0)
                        {
                            _bits &= ~0x100L;
                            _headers._Via = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.DNT.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100000000000L) != 0)
                        {
                            _bits &= ~0x100000000000L;
                            _headers._DNT = default(StringValues);
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
                        if ((_bits & 0x80000000L) != 0)
                        {
                            _bits &= ~0x80000000L;
                            _headers._Host = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Date, key))
                    {
                        if ((_bits & 0x4L) != 0)
                        {
                            _bits &= ~0x4L;
                            _headers._Date = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.From, key))
                    {
                        if ((_bits & 0x40000000L) != 0)
                        {
                            _bits &= ~0x40000000L;
                            _headers._From = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Host.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80000000L) != 0)
                        {
                            _bits &= ~0x80000000L;
                            _headers._Host = default(StringValues);
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
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.From.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40000000L) != 0)
                        {
                            _bits &= ~0x40000000L;
                            _headers._From = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 5:
                {
                    if (ReferenceEquals(HeaderNames.Path, key))
                    {
                        if ((_bits & 0x200000L) != 0)
                        {
                            _bits &= ~0x200000L;
                            _headers._Path = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Allow, key))
                    {
                        if ((_bits & 0x400L) != 0)
                        {
                            _bits &= ~0x400L;
                            _headers._Allow = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Range, key))
                    {
                        if ((_bits & 0x10000000000L) != 0)
                        {
                            _bits &= ~0x10000000000L;
                            _headers._Range = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Path.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200000L) != 0)
                        {
                            _bits &= ~0x200000L;
                            _headers._Path = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Allow.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400L) != 0)
                        {
                            _bits &= ~0x400L;
                            _headers._Allow = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Range.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000000000L) != 0)
                        {
                            _bits &= ~0x10000000000L;
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
                        if ((_bits & 0x800000L) != 0)
                        {
                            _bits &= ~0x800000L;
                            _headers._Accept = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Pragma, key))
                    {
                        if ((_bits & 0x10L) != 0)
                        {
                            _bits &= ~0x10L;
                            _headers._Pragma = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Cookie, key))
                    {
                        if ((_bits & 0x10000000L) != 0)
                        {
                            _bits &= ~0x10000000L;
                            _headers._Cookie = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Expect, key))
                    {
                        if ((_bits & 0x20000000L) != 0)
                        {
                            _bits &= ~0x20000000L;
                            _headers._Expect = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Origin, key))
                    {
                        if ((_bits & 0x4000000000000L) != 0)
                        {
                            _bits &= ~0x4000000000000L;
                            _headers._Origin = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Accept.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800000L) != 0)
                        {
                            _bits &= ~0x800000L;
                            _headers._Accept = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Pragma.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10L) != 0)
                        {
                            _bits &= ~0x10L;
                            _headers._Pragma = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Cookie.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000000L) != 0)
                        {
                            _bits &= ~0x10000000L;
                            _headers._Cookie = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Expect.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20000000L) != 0)
                        {
                            _bits &= ~0x20000000L;
                            _headers._Expect = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Origin.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000000000000L) != 0)
                        {
                            _bits &= ~0x4000000000000L;
                            _headers._Origin = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 7:
                {
                    if (ReferenceEquals(HeaderNames.Method, key))
                    {
                        if ((_bits & 0x100000L) != 0)
                        {
                            _bits &= ~0x100000L;
                            _headers._Method = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Scheme, key))
                    {
                        if ((_bits & 0x400000L) != 0)
                        {
                            _bits &= ~0x400000L;
                            _headers._Scheme = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Trailer, key))
                    {
                        if ((_bits & 0x20L) != 0)
                        {
                            _bits &= ~0x20L;
                            _headers._Trailer = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Upgrade, key))
                    {
                        if ((_bits & 0x80L) != 0)
                        {
                            _bits &= ~0x80L;
                            _headers._Upgrade = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Warning, key))
                    {
                        if ((_bits & 0x200L) != 0)
                        {
                            _bits &= ~0x200L;
                            _headers._Warning = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Expires, key))
                    {
                        if ((_bits & 0x20000L) != 0)
                        {
                            _bits &= ~0x20000L;
                            _headers._Expires = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Referer, key))
                    {
                        if ((_bits & 0x8000000000L) != 0)
                        {
                            _bits &= ~0x8000000000L;
                            _headers._Referer = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Method.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100000L) != 0)
                        {
                            _bits &= ~0x100000L;
                            _headers._Method = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Scheme.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400000L) != 0)
                        {
                            _bits &= ~0x400000L;
                            _headers._Scheme = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Trailer.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20L) != 0)
                        {
                            _bits &= ~0x20L;
                            _headers._Trailer = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Upgrade.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80L) != 0)
                        {
                            _bits &= ~0x80L;
                            _headers._Upgrade = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Warning.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200L) != 0)
                        {
                            _bits &= ~0x200L;
                            _headers._Warning = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Expires.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20000L) != 0)
                        {
                            _bits &= ~0x20000L;
                            _headers._Expires = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Referer.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000000000L) != 0)
                        {
                            _bits &= ~0x8000000000L;
                            _headers._Referer = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 8:
                {
                    if (ReferenceEquals(HeaderNames.IfMatch, key))
                    {
                        if ((_bits & 0x100000000L) != 0)
                        {
                            _bits &= ~0x100000000L;
                            _headers._IfMatch = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.IfRange, key))
                    {
                        if ((_bits & 0x800000000L) != 0)
                        {
                            _bits &= ~0x800000000L;
                            _headers._IfRange = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.IfMatch.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100000000L) != 0)
                        {
                            _bits &= ~0x100000000L;
                            _headers._IfMatch = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.IfRange.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800000000L) != 0)
                        {
                            _bits &= ~0x800000000L;
                            _headers._IfRange = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 9:
                {
                    if (ReferenceEquals(HeaderNames.Translate, key))
                    {
                        if ((_bits & 0x40000000000L) != 0)
                        {
                            _bits &= ~0x40000000000L;
                            _headers._Translate = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Translate.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40000000000L) != 0)
                        {
                            _bits &= ~0x40000000000L;
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
                    if (ReferenceEquals(HeaderNames.Authority, key))
                    {
                        if ((_bits & 0x80000L) != 0)
                        {
                            _bits &= ~0x80000L;
                            _headers._Authority = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.UserAgent, key))
                    {
                        if ((_bits & 0x80000000000L) != 0)
                        {
                            _bits &= ~0x80000000000L;
                            _headers._UserAgent = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.KeepAlive, key))
                    {
                        if ((_bits & 0x8L) != 0)
                        {
                            _bits &= ~0x8L;
                            _headers._KeepAlive = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.RequestId, key))
                    {
                        if ((_bits & 0x400000000000L) != 0)
                        {
                            _bits &= ~0x400000000000L;
                            _headers._RequestId = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.TraceState, key))
                    {
                        if ((_bits & 0x2000000000000L) != 0)
                        {
                            _bits &= ~0x2000000000000L;
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
                    if (HeaderNames.Authority.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80000L) != 0)
                        {
                            _bits &= ~0x80000L;
                            _headers._Authority = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.UserAgent.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80000000000L) != 0)
                        {
                            _bits &= ~0x80000000000L;
                            _headers._UserAgent = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.KeepAlive.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8L) != 0)
                        {
                            _bits &= ~0x8L;
                            _headers._KeepAlive = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.RequestId.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400000000000L) != 0)
                        {
                            _bits &= ~0x400000000000L;
                            _headers._RequestId = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.TraceState.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000000000000L) != 0)
                        {
                            _bits &= ~0x2000000000000L;
                            _headers._TraceState = default(StringValues);
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
                        if ((_bits & 0x8000L) != 0)
                        {
                            _bits &= ~0x8000L;
                            _headers._ContentMD5 = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.TraceParent, key))
                    {
                        if ((_bits & 0x1000000000000L) != 0)
                        {
                            _bits &= ~0x1000000000000L;
                            _headers._TraceParent = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.ContentMD5.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000L) != 0)
                        {
                            _bits &= ~0x8000L;
                            _headers._ContentMD5 = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.TraceParent.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000000000000L) != 0)
                        {
                            _bits &= ~0x1000000000000L;
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
                        if ((_bits & 0x800L) != 0)
                        {
                            _bits &= ~0x800L;
                            _headers._ContentType = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.MaxForwards, key))
                    {
                        if ((_bits & 0x2000000000L) != 0)
                        {
                            _bits &= ~0x2000000000L;
                            _headers._MaxForwards = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.ContentType.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800L) != 0)
                        {
                            _bits &= ~0x800L;
                            _headers._ContentType = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.MaxForwards.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000000000L) != 0)
                        {
                            _bits &= ~0x2000000000L;
                            _headers._MaxForwards = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 13:
                {
                    if (ReferenceEquals(HeaderNames.CacheControl, key))
                    {
                        if ((_bits & 0x1L) != 0)
                        {
                            _bits &= ~0x1L;
                            _headers._CacheControl = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentRange, key))
                    {
                        if ((_bits & 0x10000L) != 0)
                        {
                            _bits &= ~0x10000L;
                            _headers._ContentRange = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.LastModified, key))
                    {
                        if ((_bits & 0x40000L) != 0)
                        {
                            _bits &= ~0x40000L;
                            _headers._LastModified = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Authorization, key))
                    {
                        if ((_bits & 0x8000000L) != 0)
                        {
                            _bits &= ~0x8000000L;
                            _headers._Authorization = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.IfNoneMatch, key))
                    {
                        if ((_bits & 0x400000000L) != 0)
                        {
                            _bits &= ~0x400000000L;
                            _headers._IfNoneMatch = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.CacheControl.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1L) != 0)
                        {
                            _bits &= ~0x1L;
                            _headers._CacheControl = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentRange.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000L) != 0)
                        {
                            _bits &= ~0x10000L;
                            _headers._ContentRange = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.LastModified.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40000L) != 0)
                        {
                            _bits &= ~0x40000L;
                            _headers._LastModified = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Authorization.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000000L) != 0)
                        {
                            _bits &= ~0x8000000L;
                            _headers._Authorization = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.IfNoneMatch.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400000000L) != 0)
                        {
                            _bits &= ~0x400000000L;
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
                        if ((_bits & 0x1000000L) != 0)
                        {
                            _bits &= ~0x1000000L;
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
                        if ((_bits & 0x1000000L) != 0)
                        {
                            _bits &= ~0x1000000L;
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
                        if ((_bits & 0x2000000L) != 0)
                        {
                            _bits &= ~0x2000000L;
                            _headers._AcceptEncoding = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.AcceptLanguage, key))
                    {
                        if ((_bits & 0x4000000L) != 0)
                        {
                            _bits &= ~0x4000000L;
                            _headers._AcceptLanguage = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AcceptEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000000L) != 0)
                        {
                            _bits &= ~0x2000000L;
                            _headers._AcceptEncoding = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.AcceptLanguage.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000000L) != 0)
                        {
                            _bits &= ~0x4000000L;
                            _headers._AcceptLanguage = default(StringValues);
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
                        if ((_bits & 0x1000L) != 0)
                        {
                            _bits &= ~0x1000L;
                            _headers._ContentEncoding = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentLanguage, key))
                    {
                        if ((_bits & 0x2000L) != 0)
                        {
                            _bits &= ~0x2000L;
                            _headers._ContentLanguage = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentLocation, key))
                    {
                        if ((_bits & 0x4000L) != 0)
                        {
                            _bits &= ~0x4000L;
                            _headers._ContentLocation = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.ContentEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000L) != 0)
                        {
                            _bits &= ~0x1000L;
                            _headers._ContentEncoding = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentLanguage.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000L) != 0)
                        {
                            _bits &= ~0x2000L;
                            _headers._ContentLanguage = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentLocation.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000L) != 0)
                        {
                            _bits &= ~0x4000L;
                            _headers._ContentLocation = default(StringValues);
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
                        if ((_bits & 0x40L) != 0)
                        {
                            _bits &= ~0x40L;
                            _headers._TransferEncoding = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.IfModifiedSince, key))
                    {
                        if ((_bits & 0x200000000L) != 0)
                        {
                            _bits &= ~0x200000000L;
                            _headers._IfModifiedSince = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.TransferEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40L) != 0)
                        {
                            _bits &= ~0x40L;
                            _headers._TransferEncoding = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.IfModifiedSince.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200000000L) != 0)
                        {
                            _bits &= ~0x200000000L;
                            _headers._IfModifiedSince = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 19:
                {
                    if (ReferenceEquals(HeaderNames.IfUnmodifiedSince, key))
                    {
                        if ((_bits & 0x1000000000L) != 0)
                        {
                            _bits &= ~0x1000000000L;
                            _headers._IfUnmodifiedSince = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ProxyAuthorization, key))
                    {
                        if ((_bits & 0x4000000000L) != 0)
                        {
                            _bits &= ~0x4000000000L;
                            _headers._ProxyAuthorization = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.CorrelationContext, key))
                    {
                        if ((_bits & 0x800000000000L) != 0)
                        {
                            _bits &= ~0x800000000000L;
                            _headers._CorrelationContext = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.IfUnmodifiedSince.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000000000L) != 0)
                        {
                            _bits &= ~0x1000000000L;
                            _headers._IfUnmodifiedSince = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ProxyAuthorization.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000000000L) != 0)
                        {
                            _bits &= ~0x4000000000L;
                            _headers._ProxyAuthorization = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.CorrelationContext.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800000000000L) != 0)
                        {
                            _bits &= ~0x800000000000L;
                            _headers._CorrelationContext = default(StringValues);
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
                        if ((_bits & 0x200000000000L) != 0)
                        {
                            _bits &= ~0x200000000000L;
                            _headers._UpgradeInsecureRequests = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.UpgradeInsecureRequests.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200000000000L) != 0)
                        {
                            _bits &= ~0x200000000000L;
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
                        if ((_bits & 0x8000000000000L) != 0)
                        {
                            _bits &= ~0x8000000000000L;
                            _headers._AccessControlRequestMethod = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AccessControlRequestMethod.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000000000000L) != 0)
                        {
                            _bits &= ~0x8000000000000L;
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
                        if ((_bits & 0x10000000000000L) != 0)
                        {
                            _bits &= ~0x10000000000000L;
                            _headers._AccessControlRequestHeaders = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AccessControlRequestHeaders.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000000000000L) != 0)
                        {
                            _bits &= ~0x10000000000000L;
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
            
            if ((tempBits & 0x2L) != 0)
            {
                _headers._Connection = default;
                if((tempBits & ~0x2L) == 0)
                {
                    return;
                }
                tempBits &= ~0x2L;
            }
            
            if ((tempBits & 0x80000L) != 0)
            {
                _headers._Authority = default;
                if((tempBits & ~0x80000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x80000L;
            }
            
            if ((tempBits & 0x100000L) != 0)
            {
                _headers._Method = default;
                if((tempBits & ~0x100000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x100000L;
            }
            
            if ((tempBits & 0x200000L) != 0)
            {
                _headers._Path = default;
                if((tempBits & ~0x200000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x200000L;
            }
            
            if ((tempBits & 0x400000L) != 0)
            {
                _headers._Scheme = default;
                if((tempBits & ~0x400000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x400000L;
            }
            
            if ((tempBits & 0x800000L) != 0)
            {
                _headers._Accept = default;
                if((tempBits & ~0x800000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x800000L;
            }
            
            if ((tempBits & 0x80000000L) != 0)
            {
                _headers._Host = default;
                if((tempBits & ~0x80000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x80000000L;
            }
            
            if ((tempBits & 0x80000000000L) != 0)
            {
                _headers._UserAgent = default;
                if((tempBits & ~0x80000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x80000000000L;
            }
            
            if ((tempBits & 0x1L) != 0)
            {
                _headers._CacheControl = default;
                if((tempBits & ~0x1L) == 0)
                {
                    return;
                }
                tempBits &= ~0x1L;
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
                _headers._KeepAlive = default;
                if((tempBits & ~0x8L) == 0)
                {
                    return;
                }
                tempBits &= ~0x8L;
            }
            
            if ((tempBits & 0x10L) != 0)
            {
                _headers._Pragma = default;
                if((tempBits & ~0x10L) == 0)
                {
                    return;
                }
                tempBits &= ~0x10L;
            }
            
            if ((tempBits & 0x20L) != 0)
            {
                _headers._Trailer = default;
                if((tempBits & ~0x20L) == 0)
                {
                    return;
                }
                tempBits &= ~0x20L;
            }
            
            if ((tempBits & 0x40L) != 0)
            {
                _headers._TransferEncoding = default;
                if((tempBits & ~0x40L) == 0)
                {
                    return;
                }
                tempBits &= ~0x40L;
            }
            
            if ((tempBits & 0x80L) != 0)
            {
                _headers._Upgrade = default;
                if((tempBits & ~0x80L) == 0)
                {
                    return;
                }
                tempBits &= ~0x80L;
            }
            
            if ((tempBits & 0x100L) != 0)
            {
                _headers._Via = default;
                if((tempBits & ~0x100L) == 0)
                {
                    return;
                }
                tempBits &= ~0x100L;
            }
            
            if ((tempBits & 0x200L) != 0)
            {
                _headers._Warning = default;
                if((tempBits & ~0x200L) == 0)
                {
                    return;
                }
                tempBits &= ~0x200L;
            }
            
            if ((tempBits & 0x400L) != 0)
            {
                _headers._Allow = default;
                if((tempBits & ~0x400L) == 0)
                {
                    return;
                }
                tempBits &= ~0x400L;
            }
            
            if ((tempBits & 0x800L) != 0)
            {
                _headers._ContentType = default;
                if((tempBits & ~0x800L) == 0)
                {
                    return;
                }
                tempBits &= ~0x800L;
            }
            
            if ((tempBits & 0x1000L) != 0)
            {
                _headers._ContentEncoding = default;
                if((tempBits & ~0x1000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x1000L;
            }
            
            if ((tempBits & 0x2000L) != 0)
            {
                _headers._ContentLanguage = default;
                if((tempBits & ~0x2000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x2000L;
            }
            
            if ((tempBits & 0x4000L) != 0)
            {
                _headers._ContentLocation = default;
                if((tempBits & ~0x4000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x4000L;
            }
            
            if ((tempBits & 0x8000L) != 0)
            {
                _headers._ContentMD5 = default;
                if((tempBits & ~0x8000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x8000L;
            }
            
            if ((tempBits & 0x10000L) != 0)
            {
                _headers._ContentRange = default;
                if((tempBits & ~0x10000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x10000L;
            }
            
            if ((tempBits & 0x20000L) != 0)
            {
                _headers._Expires = default;
                if((tempBits & ~0x20000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x20000L;
            }
            
            if ((tempBits & 0x40000L) != 0)
            {
                _headers._LastModified = default;
                if((tempBits & ~0x40000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x40000L;
            }
            
            if ((tempBits & 0x1000000L) != 0)
            {
                _headers._AcceptCharset = default;
                if((tempBits & ~0x1000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x1000000L;
            }
            
            if ((tempBits & 0x2000000L) != 0)
            {
                _headers._AcceptEncoding = default;
                if((tempBits & ~0x2000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x2000000L;
            }
            
            if ((tempBits & 0x4000000L) != 0)
            {
                _headers._AcceptLanguage = default;
                if((tempBits & ~0x4000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x4000000L;
            }
            
            if ((tempBits & 0x8000000L) != 0)
            {
                _headers._Authorization = default;
                if((tempBits & ~0x8000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x8000000L;
            }
            
            if ((tempBits & 0x10000000L) != 0)
            {
                _headers._Cookie = default;
                if((tempBits & ~0x10000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x10000000L;
            }
            
            if ((tempBits & 0x20000000L) != 0)
            {
                _headers._Expect = default;
                if((tempBits & ~0x20000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x20000000L;
            }
            
            if ((tempBits & 0x40000000L) != 0)
            {
                _headers._From = default;
                if((tempBits & ~0x40000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x40000000L;
            }
            
            if ((tempBits & 0x100000000L) != 0)
            {
                _headers._IfMatch = default;
                if((tempBits & ~0x100000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x100000000L;
            }
            
            if ((tempBits & 0x200000000L) != 0)
            {
                _headers._IfModifiedSince = default;
                if((tempBits & ~0x200000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x200000000L;
            }
            
            if ((tempBits & 0x400000000L) != 0)
            {
                _headers._IfNoneMatch = default;
                if((tempBits & ~0x400000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x400000000L;
            }
            
            if ((tempBits & 0x800000000L) != 0)
            {
                _headers._IfRange = default;
                if((tempBits & ~0x800000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x800000000L;
            }
            
            if ((tempBits & 0x1000000000L) != 0)
            {
                _headers._IfUnmodifiedSince = default;
                if((tempBits & ~0x1000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x1000000000L;
            }
            
            if ((tempBits & 0x2000000000L) != 0)
            {
                _headers._MaxForwards = default;
                if((tempBits & ~0x2000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x2000000000L;
            }
            
            if ((tempBits & 0x4000000000L) != 0)
            {
                _headers._ProxyAuthorization = default;
                if((tempBits & ~0x4000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x4000000000L;
            }
            
            if ((tempBits & 0x8000000000L) != 0)
            {
                _headers._Referer = default;
                if((tempBits & ~0x8000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x8000000000L;
            }
            
            if ((tempBits & 0x10000000000L) != 0)
            {
                _headers._Range = default;
                if((tempBits & ~0x10000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x10000000000L;
            }
            
            if ((tempBits & 0x20000000000L) != 0)
            {
                _headers._TE = default;
                if((tempBits & ~0x20000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x20000000000L;
            }
            
            if ((tempBits & 0x40000000000L) != 0)
            {
                _headers._Translate = default;
                if((tempBits & ~0x40000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x40000000000L;
            }
            
            if ((tempBits & 0x100000000000L) != 0)
            {
                _headers._DNT = default;
                if((tempBits & ~0x100000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x100000000000L;
            }
            
            if ((tempBits & 0x200000000000L) != 0)
            {
                _headers._UpgradeInsecureRequests = default;
                if((tempBits & ~0x200000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x200000000000L;
            }
            
            if ((tempBits & 0x400000000000L) != 0)
            {
                _headers._RequestId = default;
                if((tempBits & ~0x400000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x400000000000L;
            }
            
            if ((tempBits & 0x800000000000L) != 0)
            {
                _headers._CorrelationContext = default;
                if((tempBits & ~0x800000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x800000000000L;
            }
            
            if ((tempBits & 0x1000000000000L) != 0)
            {
                _headers._TraceParent = default;
                if((tempBits & ~0x1000000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x1000000000000L;
            }
            
            if ((tempBits & 0x2000000000000L) != 0)
            {
                _headers._TraceState = default;
                if((tempBits & ~0x2000000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x2000000000000L;
            }
            
            if ((tempBits & 0x4000000000000L) != 0)
            {
                _headers._Origin = default;
                if((tempBits & ~0x4000000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x4000000000000L;
            }
            
            if ((tempBits & 0x8000000000000L) != 0)
            {
                _headers._AccessControlRequestMethod = default;
                if((tempBits & ~0x8000000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x8000000000000L;
            }
            
            if ((tempBits & 0x10000000000000L) != 0)
            {
                _headers._AccessControlRequestHeaders = default;
                if((tempBits & ~0x10000000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x10000000000000L;
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
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.CacheControl, _headers._CacheControl);
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
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Date, _headers._Date);
                    ++arrayIndex;
                }
                if ((_bits & 0x8L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.KeepAlive, _headers._KeepAlive);
                    ++arrayIndex;
                }
                if ((_bits & 0x10L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Pragma, _headers._Pragma);
                    ++arrayIndex;
                }
                if ((_bits & 0x20L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Trailer, _headers._Trailer);
                    ++arrayIndex;
                }
                if ((_bits & 0x40L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.TransferEncoding, _headers._TransferEncoding);
                    ++arrayIndex;
                }
                if ((_bits & 0x80L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Upgrade, _headers._Upgrade);
                    ++arrayIndex;
                }
                if ((_bits & 0x100L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Via, _headers._Via);
                    ++arrayIndex;
                }
                if ((_bits & 0x200L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Warning, _headers._Warning);
                    ++arrayIndex;
                }
                if ((_bits & 0x400L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Allow, _headers._Allow);
                    ++arrayIndex;
                }
                if ((_bits & 0x800L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ContentType, _headers._ContentType);
                    ++arrayIndex;
                }
                if ((_bits & 0x1000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ContentEncoding, _headers._ContentEncoding);
                    ++arrayIndex;
                }
                if ((_bits & 0x2000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ContentLanguage, _headers._ContentLanguage);
                    ++arrayIndex;
                }
                if ((_bits & 0x4000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ContentLocation, _headers._ContentLocation);
                    ++arrayIndex;
                }
                if ((_bits & 0x8000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ContentMD5, _headers._ContentMD5);
                    ++arrayIndex;
                }
                if ((_bits & 0x10000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ContentRange, _headers._ContentRange);
                    ++arrayIndex;
                }
                if ((_bits & 0x20000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Expires, _headers._Expires);
                    ++arrayIndex;
                }
                if ((_bits & 0x40000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.LastModified, _headers._LastModified);
                    ++arrayIndex;
                }
                if ((_bits & 0x80000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Authority, _headers._Authority);
                    ++arrayIndex;
                }
                if ((_bits & 0x100000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Method, _headers._Method);
                    ++arrayIndex;
                }
                if ((_bits & 0x200000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Path, _headers._Path);
                    ++arrayIndex;
                }
                if ((_bits & 0x400000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Scheme, _headers._Scheme);
                    ++arrayIndex;
                }
                if ((_bits & 0x800000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Accept, _headers._Accept);
                    ++arrayIndex;
                }
                if ((_bits & 0x1000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.AcceptCharset, _headers._AcceptCharset);
                    ++arrayIndex;
                }
                if ((_bits & 0x2000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.AcceptEncoding, _headers._AcceptEncoding);
                    ++arrayIndex;
                }
                if ((_bits & 0x4000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.AcceptLanguage, _headers._AcceptLanguage);
                    ++arrayIndex;
                }
                if ((_bits & 0x8000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Authorization, _headers._Authorization);
                    ++arrayIndex;
                }
                if ((_bits & 0x10000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Cookie, _headers._Cookie);
                    ++arrayIndex;
                }
                if ((_bits & 0x20000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Expect, _headers._Expect);
                    ++arrayIndex;
                }
                if ((_bits & 0x40000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.From, _headers._From);
                    ++arrayIndex;
                }
                if ((_bits & 0x80000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Host, _headers._Host);
                    ++arrayIndex;
                }
                if ((_bits & 0x100000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.IfMatch, _headers._IfMatch);
                    ++arrayIndex;
                }
                if ((_bits & 0x200000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.IfModifiedSince, _headers._IfModifiedSince);
                    ++arrayIndex;
                }
                if ((_bits & 0x400000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.IfNoneMatch, _headers._IfNoneMatch);
                    ++arrayIndex;
                }
                if ((_bits & 0x800000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.IfRange, _headers._IfRange);
                    ++arrayIndex;
                }
                if ((_bits & 0x1000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.IfUnmodifiedSince, _headers._IfUnmodifiedSince);
                    ++arrayIndex;
                }
                if ((_bits & 0x2000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.MaxForwards, _headers._MaxForwards);
                    ++arrayIndex;
                }
                if ((_bits & 0x4000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ProxyAuthorization, _headers._ProxyAuthorization);
                    ++arrayIndex;
                }
                if ((_bits & 0x8000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Referer, _headers._Referer);
                    ++arrayIndex;
                }
                if ((_bits & 0x10000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Range, _headers._Range);
                    ++arrayIndex;
                }
                if ((_bits & 0x20000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.TE, _headers._TE);
                    ++arrayIndex;
                }
                if ((_bits & 0x40000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Translate, _headers._Translate);
                    ++arrayIndex;
                }
                if ((_bits & 0x80000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.UserAgent, _headers._UserAgent);
                    ++arrayIndex;
                }
                if ((_bits & 0x100000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.DNT, _headers._DNT);
                    ++arrayIndex;
                }
                if ((_bits & 0x200000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.UpgradeInsecureRequests, _headers._UpgradeInsecureRequests);
                    ++arrayIndex;
                }
                if ((_bits & 0x400000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.RequestId, _headers._RequestId);
                    ++arrayIndex;
                }
                if ((_bits & 0x800000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.CorrelationContext, _headers._CorrelationContext);
                    ++arrayIndex;
                }
                if ((_bits & 0x1000000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.TraceParent, _headers._TraceParent);
                    ++arrayIndex;
                }
                if ((_bits & 0x2000000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.TraceState, _headers._TraceState);
                    ++arrayIndex;
                }
                if ((_bits & 0x4000000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Origin, _headers._Origin);
                    ++arrayIndex;
                }
                if ((_bits & 0x8000000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlRequestMethod, _headers._AccessControlRequestMethod);
                    ++arrayIndex;
                }
                if ((_bits & 0x10000000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlRequestHeaders, _headers._AccessControlRequestHeaders);
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
            ((ICollection<KeyValuePair<string, StringValues>>)MaybeUnknown)?.CopyTo(array, arrayIndex);

            return true;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public unsafe void Append(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
        {
            ref byte nameStart = ref MemoryMarshal.GetReference(name);
            var nameStr = string.Empty;
            ref StringValues values = ref Unsafe.AsRef<StringValues>(null);
            var flag = 0L;

            // Does the name matched any "known" headers
            switch (name.Length)
            {
                case 2:
                    if (((Unsafe.ReadUnaligned<ushort>(ref nameStart) & 0xdfdfu) == 0x4554u))
                    {
                        flag = 0x20000000000L;
                        values = ref _headers._TE;
                        nameStr = HeaderNames.TE;
                    }
                    break;
                case 3:
                    var firstTerm3 = (Unsafe.ReadUnaligned<ushort>(ref nameStart) & 0xdfdfu);
                    if ((firstTerm3 == 0x4e44u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)2) & 0xdfu) == 0x54u))
                    {
                        flag = 0x100000000000L;
                        values = ref _headers._DNT;
                        nameStr = HeaderNames.DNT;
                    }
                    else if ((firstTerm3 == 0x4956u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)2) & 0xdfu) == 0x41u))
                    {
                        flag = 0x100L;
                        values = ref _headers._Via;
                        nameStr = HeaderNames.Via;
                    }
                    break;
                case 4:
                    var firstTerm4 = (Unsafe.ReadUnaligned<uint>(ref nameStart) & 0xdfdfdfdfu);
                    if ((firstTerm4 == 0x54534f48u))
                    {
                        flag = 0x80000000L;
                        values = ref _headers._Host;
                        nameStr = HeaderNames.Host;
                    }
                    else if ((firstTerm4 == 0x45544144u))
                    {
                        flag = 0x4L;
                        values = ref _headers._Date;
                        nameStr = HeaderNames.Date;
                    }
                    else if ((firstTerm4 == 0x4d4f5246u))
                    {
                        flag = 0x40000000L;
                        values = ref _headers._From;
                        nameStr = HeaderNames.From;
                    }
                    break;
                case 5:
                    if (((Unsafe.ReadUnaligned<uint>(ref nameStart) & 0xdfdfdfffu) == 0x5441503au) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)4) & 0xdfu) == 0x48u))
                    {
                        flag = 0x200000L;
                        values = ref _headers._Path;
                        nameStr = HeaderNames.Path;
                    }
                    else if (((Unsafe.ReadUnaligned<uint>(ref nameStart) & 0xdfdfdfdfu) == 0x4f4c4c41u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)4) & 0xdfu) == 0x57u))
                    {
                        flag = 0x400L;
                        values = ref _headers._Allow;
                        nameStr = HeaderNames.Allow;
                    }
                    else if (((Unsafe.ReadUnaligned<uint>(ref nameStart) & 0xdfdfdfdfu) == 0x474e4152u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)4) & 0xdfu) == 0x45u))
                    {
                        flag = 0x10000000000L;
                        values = ref _headers._Range;
                        nameStr = HeaderNames.Range;
                    }
                    break;
                case 6:
                    var firstTerm6 = (Unsafe.ReadUnaligned<uint>(ref nameStart) & 0xdfdfdfdfu);
                    if ((firstTerm6 == 0x45434341u) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x5450u))
                    {
                        flag = 0x800000L;
                        values = ref _headers._Accept;
                        nameStr = HeaderNames.Accept;
                    }
                    else if ((firstTerm6 == 0x4b4f4f43u) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x4549u))
                    {
                        flag = 0x10000000L;
                        values = ref _headers._Cookie;
                        nameStr = HeaderNames.Cookie;
                    }
                    else if ((firstTerm6 == 0x45505845u) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x5443u))
                    {
                        flag = 0x20000000L;
                        values = ref _headers._Expect;
                        nameStr = HeaderNames.Expect;
                    }
                    else if ((firstTerm6 == 0x4749524fu) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x4e49u))
                    {
                        flag = 0x4000000000000L;
                        values = ref _headers._Origin;
                        nameStr = HeaderNames.Origin;
                    }
                    else if ((firstTerm6 == 0x47415250u) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x414du))
                    {
                        flag = 0x10L;
                        values = ref _headers._Pragma;
                        nameStr = HeaderNames.Pragma;
                    }
                    break;
                case 7:
                    if (((Unsafe.ReadUnaligned<uint>(ref nameStart) & 0xdfdfdfffu) == 0x54454d3au) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x4f48u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)6) & 0xdfu) == 0x44u))
                    {
                        flag = 0x100000L;
                        values = ref _headers._Method;
                        nameStr = HeaderNames.Method;
                    }
                    else if (((Unsafe.ReadUnaligned<uint>(ref nameStart) & 0xdfdfdfffu) == 0x4843533au) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x4d45u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)6) & 0xdfu) == 0x45u))
                    {
                        flag = 0x400000L;
                        values = ref _headers._Scheme;
                        nameStr = HeaderNames.Scheme;
                    }
                    else if (((Unsafe.ReadUnaligned<uint>(ref nameStart) & 0xdfdfdfdfu) == 0x49505845u) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x4552u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)6) & 0xdfu) == 0x53u))
                    {
                        flag = 0x20000L;
                        values = ref _headers._Expires;
                        nameStr = HeaderNames.Expires;
                    }
                    else if (((Unsafe.ReadUnaligned<uint>(ref nameStart) & 0xdfdfdfdfu) == 0x45464552u) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x4552u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)6) & 0xdfu) == 0x52u))
                    {
                        flag = 0x8000000000L;
                        values = ref _headers._Referer;
                        nameStr = HeaderNames.Referer;
                    }
                    else if (((Unsafe.ReadUnaligned<uint>(ref nameStart) & 0xdfdfdfdfu) == 0x49415254u) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x454cu) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)6) & 0xdfu) == 0x52u))
                    {
                        flag = 0x20L;
                        values = ref _headers._Trailer;
                        nameStr = HeaderNames.Trailer;
                    }
                    else if (((Unsafe.ReadUnaligned<uint>(ref nameStart) & 0xdfdfdfdfu) == 0x52475055u) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x4441u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)6) & 0xdfu) == 0x45u))
                    {
                        flag = 0x80L;
                        values = ref _headers._Upgrade;
                        nameStr = HeaderNames.Upgrade;
                    }
                    else if (((Unsafe.ReadUnaligned<uint>(ref nameStart) & 0xdfdfdfdfu) == 0x4e524157u) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x4e49u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)6) & 0xdfu) == 0x47u))
                    {
                        flag = 0x200L;
                        values = ref _headers._Warning;
                        nameStr = HeaderNames.Warning;
                    }
                    break;
                case 8:
                    var firstTerm8 = (Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfdfdfdfdfffdfdfuL);
                    if ((firstTerm8 == 0x484354414d2d4649uL))
                    {
                        flag = 0x100000000L;
                        values = ref _headers._IfMatch;
                        nameStr = HeaderNames.IfMatch;
                    }
                    else if ((firstTerm8 == 0x45474e41522d4649uL))
                    {
                        flag = 0x800000000L;
                        values = ref _headers._IfRange;
                        nameStr = HeaderNames.IfRange;
                    }
                    break;
                case 9:
                    if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfdfdfdfdfdfdfdfuL) == 0x54414c534e415254uL) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)8) & 0xdfu) == 0x45u))
                    {
                        flag = 0x40000000000L;
                        values = ref _headers._Translate;
                        nameStr = HeaderNames.Translate;
                    }
                    break;
                case 10:
                    if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfdfdfdfdfdfdfffuL) == 0x49524f485455413auL) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(4 * sizeof(ushort)))) & 0xdfdfu) == 0x5954u))
                    {
                        flag = 0x80000L;
                        values = ref _headers._Authority;
                        nameStr = HeaderNames.Authority;
                    }
                    else if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfdfdfdfdfdfdfdfuL) == 0x495443454e4e4f43uL) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(4 * sizeof(ushort)))) & 0xdfdfu) == 0x4e4fu))
                    {
                        flag = 0x2L;
                        values = ref _headers._Connection;
                        nameStr = HeaderNames.Connection;
                    }
                    else if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfdfdfffdfdfdfdfuL) == 0x4547412d52455355uL) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(4 * sizeof(ushort)))) & 0xdfdfu) == 0x544eu))
                    {
                        flag = 0x80000000000L;
                        values = ref _headers._UserAgent;
                        nameStr = HeaderNames.UserAgent;
                    }
                    else if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfdfdfffdfdfdfdfuL) == 0x494c412d5045454buL) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(4 * sizeof(ushort)))) & 0xdfdfu) == 0x4556u))
                    {
                        flag = 0x8L;
                        values = ref _headers._KeepAlive;
                        nameStr = HeaderNames.KeepAlive;
                    }
                    else if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xffdfdfdfdfdfdfdfuL) == 0x2d54534555514552uL) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(4 * sizeof(ushort)))) & 0xdfdfu) == 0x4449u))
                    {
                        flag = 0x400000000000L;
                        values = ref _headers._RequestId;
                        nameStr = HeaderNames.RequestId;
                    }
                    else if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfdfdfdfdfdfdfdfuL) == 0x4154534543415254uL) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(4 * sizeof(ushort)))) & 0xdfdfu) == 0x4554u))
                    {
                        flag = 0x2000000000000L;
                        values = ref _headers._TraceState;
                        nameStr = HeaderNames.TraceState;
                    }
                    break;
                case 11:
                    if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xffdfdfdfdfdfdfdfuL) == 0x2d544e45544e4f43uL) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(4 * sizeof(ushort)))) & 0xdfdfu) == 0x444du) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)10) & 0xffu) == 0x35u))
                    {
                        flag = 0x8000L;
                        values = ref _headers._ContentMD5;
                        nameStr = HeaderNames.ContentMD5;
                    }
                    else if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfdfdfdfdfdfdfdfuL) == 0x5241504543415254uL) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(4 * sizeof(ushort)))) & 0xdfdfu) == 0x4e45u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)10) & 0xdfu) == 0x54u))
                    {
                        flag = 0x1000000000000L;
                        values = ref _headers._TraceParent;
                        nameStr = HeaderNames.TraceParent;
                    }
                    break;
                case 12:
                    if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xffdfdfdfdfdfdfdfuL) == 0x2d544e45544e4f43uL) && ((Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x45505954u))
                    {
                        flag = 0x800L;
                        values = ref _headers._ContentType;
                        nameStr = HeaderNames.ContentType;
                    }
                    else if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfdfdfdfffdfdfdfuL) == 0x57524f462d58414duL) && ((Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x53445241u))
                    {
                        flag = 0x2000000000L;
                        values = ref _headers._MaxForwards;
                        nameStr = HeaderNames.MaxForwards;
                    }
                    break;
                case 13:
                    if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfdfdfdfdfdfdfdfuL) == 0x5a49524f48545541uL) && ((Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x4f495441u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)12) & 0xdfu) == 0x4eu))
                    {
                        flag = 0x8000000L;
                        values = ref _headers._Authorization;
                        nameStr = HeaderNames.Authorization;
                    }
                    else if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfdfffdfdfdfdfdfuL) == 0x4f432d4548434143uL) && ((Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x4f52544eu) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)12) & 0xdfu) == 0x4cu))
                    {
                        flag = 0x1L;
                        values = ref _headers._CacheControl;
                        nameStr = HeaderNames.CacheControl;
                    }
                    else if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xffdfdfdfdfdfdfdfuL) == 0x2d544e45544e4f43uL) && ((Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x474e4152u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)12) & 0xdfu) == 0x45u))
                    {
                        flag = 0x10000L;
                        values = ref _headers._ContentRange;
                        nameStr = HeaderNames.ContentRange;
                    }
                    else if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xffdfdfdfdfffdfdfuL) == 0x2d454e4f4e2d4649uL) && ((Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x4354414du) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)12) & 0xdfu) == 0x48u))
                    {
                        flag = 0x400000000L;
                        values = ref _headers._IfNoneMatch;
                        nameStr = HeaderNames.IfNoneMatch;
                    }
                    else if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfdfdfffdfdfdfdfuL) == 0x444f4d2d5453414cuL) && ((Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x45494649u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)12) & 0xdfu) == 0x44u))
                    {
                        flag = 0x40000L;
                        values = ref _headers._LastModified;
                        nameStr = HeaderNames.LastModified;
                    }
                    break;
                case 14:
                    if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfffdfdfdfdfdfdfuL) == 0x432d545045434341uL) && ((Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x53524148u) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(6 * sizeof(ushort)))) & 0xdfdfu) == 0x5445u))
                    {
                        flag = 0x1000000L;
                        values = ref _headers._AcceptCharset;
                        nameStr = HeaderNames.AcceptCharset;
                    }
                    else if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xffdfdfdfdfdfdfdfuL) == 0x2d544e45544e4f43uL) && ((Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x474e454cu) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(6 * sizeof(ushort)))) & 0xdfdfu) == 0x4854u))
                    {
                        if (ReferenceEquals(EncodingSelector, KestrelServerOptions.DefaultRequestHeaderEncodingSelector)
                            || ReferenceEquals(EncodingSelector, KestrelServerOptions.DefaultLatin1RequestHeaderEncodingSelector))
                        {
                            AppendContentLength(value);
                        }
                        else
                        {
                            AppendContentLengthCustomEncoding(value, EncodingSelector(HeaderNames.ContentLength));
                        }
                        return;
                    }
                    break;
                case 15:
                    var firstTerm15 = (Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfffdfdfdfdfdfdfuL);
                    if ((firstTerm15 == 0x452d545045434341uL) && ((Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x444f434eu) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(6 * sizeof(ushort)))) & 0xdfdfu) == 0x4e49u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)14) & 0xdfu) == 0x47u))
                    {
                        flag = 0x2000000L;
                        values = ref _headers._AcceptEncoding;
                        nameStr = HeaderNames.AcceptEncoding;
                    }
                    else if ((firstTerm15 == 0x4c2d545045434341uL) && ((Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x55474e41u) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(6 * sizeof(ushort)))) & 0xdfdfu) == 0x4741u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)14) & 0xdfu) == 0x45u))
                    {
                        flag = 0x4000000L;
                        values = ref _headers._AcceptLanguage;
                        nameStr = HeaderNames.AcceptLanguage;
                    }
                    break;
                case 16:
                    var firstTerm16 = (Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xffdfdfdfdfdfdfdfuL);
                    if ((firstTerm16 == 0x2d544e45544e4f43uL))
                    {
                        if (((Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)sizeof(ulong))) & 0xdfdfdfdfdfdfdfdfuL) == 0x474e49444f434e45uL))
                        {
                            flag = 0x1000L;
                            values = ref _headers._ContentEncoding;
                            nameStr = HeaderNames.ContentEncoding;
                        }
                        else if (((Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)sizeof(ulong))) & 0xdfdfdfdfdfdfdfdfuL) == 0x45474155474e414cuL))
                        {
                            flag = 0x2000L;
                            values = ref _headers._ContentLanguage;
                            nameStr = HeaderNames.ContentLanguage;
                        }
                        else if (((Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)sizeof(ulong))) & 0xdfdfdfdfdfdfdfdfuL) == 0x4e4f495441434f4cuL))
                        {
                            flag = 0x4000L;
                            values = ref _headers._ContentLocation;
                            nameStr = HeaderNames.ContentLocation;
                        }
                    }
                    break;
                case 17:
                    if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfdfdfdfdfffdfdfuL) == 0x4649444f4d2d4649uL) && ((Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)sizeof(ulong))) & 0xdfdfdfdfffdfdfdfuL) == 0x434e49532d444549uL) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)16) & 0xdfu) == 0x45u))
                    {
                        flag = 0x200000000L;
                        values = ref _headers._IfModifiedSince;
                        nameStr = HeaderNames.IfModifiedSince;
                    }
                    else if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfdfdfdfdfdfdfdfuL) == 0x524546534e415254uL) && ((Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)sizeof(ulong))) & 0xdfdfdfdfdfdfdfffuL) == 0x4e49444f434e452duL) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)16) & 0xdfu) == 0x47u))
                    {
                        flag = 0x40L;
                        values = ref _headers._TransferEncoding;
                        nameStr = HeaderNames.TransferEncoding;
                    }
                    break;
                case 19:
                    if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfdfdfdfdfdfdfdfuL) == 0x54414c4552524f43uL) && ((Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)sizeof(ulong))) & 0xdfdfdfdfffdfdfdfuL) == 0x544e4f432d4e4f49uL) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(8 * sizeof(ushort)))) & 0xdfdfu) == 0x5845u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)18) & 0xdfu) == 0x54u))
                    {
                        flag = 0x800000000000L;
                        values = ref _headers._CorrelationContext;
                        nameStr = HeaderNames.CorrelationContext;
                    }
                    else if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfdfdfdfdfffdfdfuL) == 0x444f4d4e552d4649uL) && ((Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)sizeof(ulong))) & 0xdfdfffdfdfdfdfdfuL) == 0x49532d4445494649uL) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(8 * sizeof(ushort)))) & 0xdfdfu) == 0x434eu) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)18) & 0xdfu) == 0x45u))
                    {
                        flag = 0x1000000000L;
                        values = ref _headers._IfUnmodifiedSince;
                        nameStr = HeaderNames.IfUnmodifiedSince;
                    }
                    else if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfdfffdfdfdfdfdfuL) == 0x55412d59584f5250uL) && ((Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)sizeof(ulong))) & 0xdfdfdfdfdfdfdfdfuL) == 0x54415a49524f4854uL) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(8 * sizeof(ushort)))) & 0xdfdfu) == 0x4f49u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)18) & 0xdfu) == 0x4eu))
                    {
                        flag = 0x4000000000L;
                        values = ref _headers._ProxyAuthorization;
                        nameStr = HeaderNames.ProxyAuthorization;
                    }
                    break;
                case 25:
                    if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xffdfdfdfdfdfdfdfuL) == 0x2d45444152475055uL) && ((Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)sizeof(ulong))) & 0xdfdfdfdfdfdfdfdfuL) == 0x4552554345534e49uL) && ((Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ulong)))) & 0xdfdfdfdfdfdfdfffuL) == 0x545345555145522duL) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)24) & 0xdfu) == 0x53u))
                    {
                        flag = 0x200000000000L;
                        values = ref _headers._UpgradeInsecureRequests;
                        nameStr = HeaderNames.UpgradeInsecureRequests;
                    }
                    break;
                case 29:
                    if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfffdfdfdfdfdfdfuL) == 0x432d535345434341uL) && ((Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)sizeof(ulong))) & 0xdfffdfdfdfdfdfdfuL) == 0x522d4c4f52544e4fuL) && ((Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ulong)))) & 0xdfffdfdfdfdfdfdfuL) == 0x4d2d545345555145uL) && ((Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(6 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x4f485445u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)28) & 0xdfu) == 0x44u))
                    {
                        flag = 0x8000000000000L;
                        values = ref _headers._AccessControlRequestMethod;
                        nameStr = HeaderNames.AccessControlRequestMethod;
                    }
                    break;
                case 30:
                    if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfffdfdfdfdfdfdfuL) == 0x432d535345434341uL) && ((Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)sizeof(ulong))) & 0xdfffdfdfdfdfdfdfuL) == 0x522d4c4f52544e4fuL) && ((Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ulong)))) & 0xdfffdfdfdfdfdfdfuL) == 0x482d545345555145uL) && ((Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(6 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x45444145u) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(14 * sizeof(ushort)))) & 0xdfdfu) == 0x5352u))
                    {
                        flag = 0x10000000000000L;
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
                var valueStr = value.GetRequestHeaderString(nameStr, EncodingSelector);
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
                var valueStr = value.GetRequestHeaderString(nameStr, EncodingSelector);
                AppendUnknownHeaders(nameStr, valueStr);
            }
        }

        private struct HeaderReferences
        {
            public StringValues _CacheControl;
            public StringValues _Connection;
            public StringValues _Date;
            public StringValues _KeepAlive;
            public StringValues _Pragma;
            public StringValues _Trailer;
            public StringValues _TransferEncoding;
            public StringValues _Upgrade;
            public StringValues _Via;
            public StringValues _Warning;
            public StringValues _Allow;
            public StringValues _ContentType;
            public StringValues _ContentEncoding;
            public StringValues _ContentLanguage;
            public StringValues _ContentLocation;
            public StringValues _ContentMD5;
            public StringValues _ContentRange;
            public StringValues _Expires;
            public StringValues _LastModified;
            public StringValues _Authority;
            public StringValues _Method;
            public StringValues _Path;
            public StringValues _Scheme;
            public StringValues _Accept;
            public StringValues _AcceptCharset;
            public StringValues _AcceptEncoding;
            public StringValues _AcceptLanguage;
            public StringValues _Authorization;
            public StringValues _Cookie;
            public StringValues _Expect;
            public StringValues _From;
            public StringValues _Host;
            public StringValues _IfMatch;
            public StringValues _IfModifiedSince;
            public StringValues _IfNoneMatch;
            public StringValues _IfRange;
            public StringValues _IfUnmodifiedSince;
            public StringValues _MaxForwards;
            public StringValues _ProxyAuthorization;
            public StringValues _Referer;
            public StringValues _Range;
            public StringValues _TE;
            public StringValues _Translate;
            public StringValues _UserAgent;
            public StringValues _DNT;
            public StringValues _UpgradeInsecureRequests;
            public StringValues _RequestId;
            public StringValues _CorrelationContext;
            public StringValues _TraceParent;
            public StringValues _TraceState;
            public StringValues _Origin;
            public StringValues _AccessControlRequestMethod;
            public StringValues _AccessControlRequestHeaders;
            
        }

        public partial struct Enumerator
        {
            // Compiled to Jump table
            public bool MoveNext()
            {
                switch (_next)
                {
                    case 0:
                        goto HeaderCacheControl;
                    case 1:
                        goto HeaderConnection;
                    case 2:
                        goto HeaderDate;
                    case 3:
                        goto HeaderKeepAlive;
                    case 4:
                        goto HeaderPragma;
                    case 5:
                        goto HeaderTrailer;
                    case 6:
                        goto HeaderTransferEncoding;
                    case 7:
                        goto HeaderUpgrade;
                    case 8:
                        goto HeaderVia;
                    case 9:
                        goto HeaderWarning;
                    case 10:
                        goto HeaderAllow;
                    case 11:
                        goto HeaderContentType;
                    case 12:
                        goto HeaderContentEncoding;
                    case 13:
                        goto HeaderContentLanguage;
                    case 14:
                        goto HeaderContentLocation;
                    case 15:
                        goto HeaderContentMD5;
                    case 16:
                        goto HeaderContentRange;
                    case 17:
                        goto HeaderExpires;
                    case 18:
                        goto HeaderLastModified;
                    case 19:
                        goto HeaderAuthority;
                    case 20:
                        goto HeaderMethod;
                    case 21:
                        goto HeaderPath;
                    case 22:
                        goto HeaderScheme;
                    case 23:
                        goto HeaderAccept;
                    case 24:
                        goto HeaderAcceptCharset;
                    case 25:
                        goto HeaderAcceptEncoding;
                    case 26:
                        goto HeaderAcceptLanguage;
                    case 27:
                        goto HeaderAuthorization;
                    case 28:
                        goto HeaderCookie;
                    case 29:
                        goto HeaderExpect;
                    case 30:
                        goto HeaderFrom;
                    case 31:
                        goto HeaderHost;
                    case 32:
                        goto HeaderIfMatch;
                    case 33:
                        goto HeaderIfModifiedSince;
                    case 34:
                        goto HeaderIfNoneMatch;
                    case 35:
                        goto HeaderIfRange;
                    case 36:
                        goto HeaderIfUnmodifiedSince;
                    case 37:
                        goto HeaderMaxForwards;
                    case 38:
                        goto HeaderProxyAuthorization;
                    case 39:
                        goto HeaderReferer;
                    case 40:
                        goto HeaderRange;
                    case 41:
                        goto HeaderTE;
                    case 42:
                        goto HeaderTranslate;
                    case 43:
                        goto HeaderUserAgent;
                    case 44:
                        goto HeaderDNT;
                    case 45:
                        goto HeaderUpgradeInsecureRequests;
                    case 46:
                        goto HeaderRequestId;
                    case 47:
                        goto HeaderCorrelationContext;
                    case 48:
                        goto HeaderTraceParent;
                    case 49:
                        goto HeaderTraceState;
                    case 50:
                        goto HeaderOrigin;
                    case 51:
                        goto HeaderAccessControlRequestMethod;
                    case 52:
                        goto HeaderAccessControlRequestHeaders;
                    case 53:
                        goto HeaderContentLength;
                    default:
                        goto ExtraHeaders;
                }
                
                HeaderCacheControl: // case 0
                    if ((_bits & 0x1L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.CacheControl, _collection._headers._CacheControl);
                        _currentKnownType = KnownHeaderType.CacheControl;
                        _next = 1;
                        return true;
                    }
                HeaderConnection: // case 1
                    if ((_bits & 0x2L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Connection, _collection._headers._Connection);
                        _currentKnownType = KnownHeaderType.Connection;
                        _next = 2;
                        return true;
                    }
                HeaderDate: // case 2
                    if ((_bits & 0x4L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Date, _collection._headers._Date);
                        _currentKnownType = KnownHeaderType.Date;
                        _next = 3;
                        return true;
                    }
                HeaderKeepAlive: // case 3
                    if ((_bits & 0x8L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.KeepAlive, _collection._headers._KeepAlive);
                        _currentKnownType = KnownHeaderType.KeepAlive;
                        _next = 4;
                        return true;
                    }
                HeaderPragma: // case 4
                    if ((_bits & 0x10L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Pragma, _collection._headers._Pragma);
                        _currentKnownType = KnownHeaderType.Pragma;
                        _next = 5;
                        return true;
                    }
                HeaderTrailer: // case 5
                    if ((_bits & 0x20L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Trailer, _collection._headers._Trailer);
                        _currentKnownType = KnownHeaderType.Trailer;
                        _next = 6;
                        return true;
                    }
                HeaderTransferEncoding: // case 6
                    if ((_bits & 0x40L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.TransferEncoding, _collection._headers._TransferEncoding);
                        _currentKnownType = KnownHeaderType.TransferEncoding;
                        _next = 7;
                        return true;
                    }
                HeaderUpgrade: // case 7
                    if ((_bits & 0x80L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Upgrade, _collection._headers._Upgrade);
                        _currentKnownType = KnownHeaderType.Upgrade;
                        _next = 8;
                        return true;
                    }
                HeaderVia: // case 8
                    if ((_bits & 0x100L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Via, _collection._headers._Via);
                        _currentKnownType = KnownHeaderType.Via;
                        _next = 9;
                        return true;
                    }
                HeaderWarning: // case 9
                    if ((_bits & 0x200L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Warning, _collection._headers._Warning);
                        _currentKnownType = KnownHeaderType.Warning;
                        _next = 10;
                        return true;
                    }
                HeaderAllow: // case 10
                    if ((_bits & 0x400L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Allow, _collection._headers._Allow);
                        _currentKnownType = KnownHeaderType.Allow;
                        _next = 11;
                        return true;
                    }
                HeaderContentType: // case 11
                    if ((_bits & 0x800L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ContentType, _collection._headers._ContentType);
                        _currentKnownType = KnownHeaderType.ContentType;
                        _next = 12;
                        return true;
                    }
                HeaderContentEncoding: // case 12
                    if ((_bits & 0x1000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ContentEncoding, _collection._headers._ContentEncoding);
                        _currentKnownType = KnownHeaderType.ContentEncoding;
                        _next = 13;
                        return true;
                    }
                HeaderContentLanguage: // case 13
                    if ((_bits & 0x2000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ContentLanguage, _collection._headers._ContentLanguage);
                        _currentKnownType = KnownHeaderType.ContentLanguage;
                        _next = 14;
                        return true;
                    }
                HeaderContentLocation: // case 14
                    if ((_bits & 0x4000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ContentLocation, _collection._headers._ContentLocation);
                        _currentKnownType = KnownHeaderType.ContentLocation;
                        _next = 15;
                        return true;
                    }
                HeaderContentMD5: // case 15
                    if ((_bits & 0x8000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ContentMD5, _collection._headers._ContentMD5);
                        _currentKnownType = KnownHeaderType.ContentMD5;
                        _next = 16;
                        return true;
                    }
                HeaderContentRange: // case 16
                    if ((_bits & 0x10000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ContentRange, _collection._headers._ContentRange);
                        _currentKnownType = KnownHeaderType.ContentRange;
                        _next = 17;
                        return true;
                    }
                HeaderExpires: // case 17
                    if ((_bits & 0x20000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Expires, _collection._headers._Expires);
                        _currentKnownType = KnownHeaderType.Expires;
                        _next = 18;
                        return true;
                    }
                HeaderLastModified: // case 18
                    if ((_bits & 0x40000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.LastModified, _collection._headers._LastModified);
                        _currentKnownType = KnownHeaderType.LastModified;
                        _next = 19;
                        return true;
                    }
                HeaderAuthority: // case 19
                    if ((_bits & 0x80000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Authority, _collection._headers._Authority);
                        _currentKnownType = KnownHeaderType.Authority;
                        _next = 20;
                        return true;
                    }
                HeaderMethod: // case 20
                    if ((_bits & 0x100000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Method, _collection._headers._Method);
                        _currentKnownType = KnownHeaderType.Method;
                        _next = 21;
                        return true;
                    }
                HeaderPath: // case 21
                    if ((_bits & 0x200000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Path, _collection._headers._Path);
                        _currentKnownType = KnownHeaderType.Path;
                        _next = 22;
                        return true;
                    }
                HeaderScheme: // case 22
                    if ((_bits & 0x400000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Scheme, _collection._headers._Scheme);
                        _currentKnownType = KnownHeaderType.Scheme;
                        _next = 23;
                        return true;
                    }
                HeaderAccept: // case 23
                    if ((_bits & 0x800000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Accept, _collection._headers._Accept);
                        _currentKnownType = KnownHeaderType.Accept;
                        _next = 24;
                        return true;
                    }
                HeaderAcceptCharset: // case 24
                    if ((_bits & 0x1000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.AcceptCharset, _collection._headers._AcceptCharset);
                        _currentKnownType = KnownHeaderType.AcceptCharset;
                        _next = 25;
                        return true;
                    }
                HeaderAcceptEncoding: // case 25
                    if ((_bits & 0x2000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.AcceptEncoding, _collection._headers._AcceptEncoding);
                        _currentKnownType = KnownHeaderType.AcceptEncoding;
                        _next = 26;
                        return true;
                    }
                HeaderAcceptLanguage: // case 26
                    if ((_bits & 0x4000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.AcceptLanguage, _collection._headers._AcceptLanguage);
                        _currentKnownType = KnownHeaderType.AcceptLanguage;
                        _next = 27;
                        return true;
                    }
                HeaderAuthorization: // case 27
                    if ((_bits & 0x8000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Authorization, _collection._headers._Authorization);
                        _currentKnownType = KnownHeaderType.Authorization;
                        _next = 28;
                        return true;
                    }
                HeaderCookie: // case 28
                    if ((_bits & 0x10000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Cookie, _collection._headers._Cookie);
                        _currentKnownType = KnownHeaderType.Cookie;
                        _next = 29;
                        return true;
                    }
                HeaderExpect: // case 29
                    if ((_bits & 0x20000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Expect, _collection._headers._Expect);
                        _currentKnownType = KnownHeaderType.Expect;
                        _next = 30;
                        return true;
                    }
                HeaderFrom: // case 30
                    if ((_bits & 0x40000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.From, _collection._headers._From);
                        _currentKnownType = KnownHeaderType.From;
                        _next = 31;
                        return true;
                    }
                HeaderHost: // case 31
                    if ((_bits & 0x80000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Host, _collection._headers._Host);
                        _currentKnownType = KnownHeaderType.Host;
                        _next = 32;
                        return true;
                    }
                HeaderIfMatch: // case 32
                    if ((_bits & 0x100000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.IfMatch, _collection._headers._IfMatch);
                        _currentKnownType = KnownHeaderType.IfMatch;
                        _next = 33;
                        return true;
                    }
                HeaderIfModifiedSince: // case 33
                    if ((_bits & 0x200000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.IfModifiedSince, _collection._headers._IfModifiedSince);
                        _currentKnownType = KnownHeaderType.IfModifiedSince;
                        _next = 34;
                        return true;
                    }
                HeaderIfNoneMatch: // case 34
                    if ((_bits & 0x400000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.IfNoneMatch, _collection._headers._IfNoneMatch);
                        _currentKnownType = KnownHeaderType.IfNoneMatch;
                        _next = 35;
                        return true;
                    }
                HeaderIfRange: // case 35
                    if ((_bits & 0x800000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.IfRange, _collection._headers._IfRange);
                        _currentKnownType = KnownHeaderType.IfRange;
                        _next = 36;
                        return true;
                    }
                HeaderIfUnmodifiedSince: // case 36
                    if ((_bits & 0x1000000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.IfUnmodifiedSince, _collection._headers._IfUnmodifiedSince);
                        _currentKnownType = KnownHeaderType.IfUnmodifiedSince;
                        _next = 37;
                        return true;
                    }
                HeaderMaxForwards: // case 37
                    if ((_bits & 0x2000000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.MaxForwards, _collection._headers._MaxForwards);
                        _currentKnownType = KnownHeaderType.MaxForwards;
                        _next = 38;
                        return true;
                    }
                HeaderProxyAuthorization: // case 38
                    if ((_bits & 0x4000000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ProxyAuthorization, _collection._headers._ProxyAuthorization);
                        _currentKnownType = KnownHeaderType.ProxyAuthorization;
                        _next = 39;
                        return true;
                    }
                HeaderReferer: // case 39
                    if ((_bits & 0x8000000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Referer, _collection._headers._Referer);
                        _currentKnownType = KnownHeaderType.Referer;
                        _next = 40;
                        return true;
                    }
                HeaderRange: // case 40
                    if ((_bits & 0x10000000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Range, _collection._headers._Range);
                        _currentKnownType = KnownHeaderType.Range;
                        _next = 41;
                        return true;
                    }
                HeaderTE: // case 41
                    if ((_bits & 0x20000000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.TE, _collection._headers._TE);
                        _currentKnownType = KnownHeaderType.TE;
                        _next = 42;
                        return true;
                    }
                HeaderTranslate: // case 42
                    if ((_bits & 0x40000000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Translate, _collection._headers._Translate);
                        _currentKnownType = KnownHeaderType.Translate;
                        _next = 43;
                        return true;
                    }
                HeaderUserAgent: // case 43
                    if ((_bits & 0x80000000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.UserAgent, _collection._headers._UserAgent);
                        _currentKnownType = KnownHeaderType.UserAgent;
                        _next = 44;
                        return true;
                    }
                HeaderDNT: // case 44
                    if ((_bits & 0x100000000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.DNT, _collection._headers._DNT);
                        _currentKnownType = KnownHeaderType.DNT;
                        _next = 45;
                        return true;
                    }
                HeaderUpgradeInsecureRequests: // case 45
                    if ((_bits & 0x200000000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.UpgradeInsecureRequests, _collection._headers._UpgradeInsecureRequests);
                        _currentKnownType = KnownHeaderType.UpgradeInsecureRequests;
                        _next = 46;
                        return true;
                    }
                HeaderRequestId: // case 46
                    if ((_bits & 0x400000000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.RequestId, _collection._headers._RequestId);
                        _currentKnownType = KnownHeaderType.RequestId;
                        _next = 47;
                        return true;
                    }
                HeaderCorrelationContext: // case 47
                    if ((_bits & 0x800000000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.CorrelationContext, _collection._headers._CorrelationContext);
                        _currentKnownType = KnownHeaderType.CorrelationContext;
                        _next = 48;
                        return true;
                    }
                HeaderTraceParent: // case 48
                    if ((_bits & 0x1000000000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.TraceParent, _collection._headers._TraceParent);
                        _currentKnownType = KnownHeaderType.TraceParent;
                        _next = 49;
                        return true;
                    }
                HeaderTraceState: // case 49
                    if ((_bits & 0x2000000000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.TraceState, _collection._headers._TraceState);
                        _currentKnownType = KnownHeaderType.TraceState;
                        _next = 50;
                        return true;
                    }
                HeaderOrigin: // case 50
                    if ((_bits & 0x4000000000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Origin, _collection._headers._Origin);
                        _currentKnownType = KnownHeaderType.Origin;
                        _next = 51;
                        return true;
                    }
                HeaderAccessControlRequestMethod: // case 51
                    if ((_bits & 0x8000000000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlRequestMethod, _collection._headers._AccessControlRequestMethod);
                        _currentKnownType = KnownHeaderType.AccessControlRequestMethod;
                        _next = 52;
                        return true;
                    }
                HeaderAccessControlRequestHeaders: // case 52
                    if ((_bits & 0x10000000000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlRequestHeaders, _collection._headers._AccessControlRequestHeaders);
                        _currentKnownType = KnownHeaderType.AccessControlRequestHeaders;
                        _next = 53;
                        return true;
                    }
                HeaderContentLength: // case 53
                    if (_collection._contentLength.HasValue)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ContentLength, HeaderUtilities.FormatNonNegativeInt64(_collection._contentLength.Value));
                        _currentKnownType = KnownHeaderType.ContentLength;
                        _next = 54;
                        return true;
                    }
                ExtraHeaders:
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
        }
    }

    internal partial class HttpResponseHeaders
    {
        private static ReadOnlySpan<byte> HeaderBytes => new byte[]
        {
            13,10,67,97,99,104,101,45,67,111,110,116,114,111,108,58,32,13,10,67,111,110,110,101,99,116,105,111,110,58,32,13,10,68,97,116,101,58,32,13,10,75,101,101,112,45,65,108,105,118,101,58,32,13,10,80,114,97,103,109,97,58,32,13,10,84,114,97,105,108,101,114,58,32,13,10,84,114,97,110,115,102,101,114,45,69,110,99,111,100,105,110,103,58,32,13,10,85,112,103,114,97,100,101,58,32,13,10,86,105,97,58,32,13,10,87,97,114,110,105,110,103,58,32,13,10,65,108,108,111,119,58,32,13,10,67,111,110,116,101,110,116,45,84,121,112,101,58,32,13,10,67,111,110,116,101,110,116,45,69,110,99,111,100,105,110,103,58,32,13,10,67,111,110,116,101,110,116,45,76,97,110,103,117,97,103,101,58,32,13,10,67,111,110,116,101,110,116,45,76,111,99,97,116,105,111,110,58,32,13,10,67,111,110,116,101,110,116,45,77,68,53,58,32,13,10,67,111,110,116,101,110,116,45,82,97,110,103,101,58,32,13,10,69,120,112,105,114,101,115,58,32,13,10,76,97,115,116,45,77,111,100,105,102,105,101,100,58,32,13,10,65,99,99,101,112,116,45,82,97,110,103,101,115,58,32,13,10,65,103,101,58,32,13,10,65,108,116,45,83,118,99,58,32,13,10,69,84,97,103,58,32,13,10,76,111,99,97,116,105,111,110,58,32,13,10,80,114,111,120,121,45,65,117,116,104,101,110,116,105,99,97,116,101,58,32,13,10,82,101,116,114,121,45,65,102,116,101,114,58,32,13,10,83,101,114,118,101,114,58,32,13,10,83,101,116,45,67,111,111,107,105,101,58,32,13,10,86,97,114,121,58,32,13,10,87,87,87,45,65,117,116,104,101,110,116,105,99,97,116,101,58,32,13,10,65,99,99,101,115,115,45,67,111,110,116,114,111,108,45,65,108,108,111,119,45,67,114,101,100,101,110,116,105,97,108,115,58,32,13,10,65,99,99,101,115,115,45,67,111,110,116,114,111,108,45,65,108,108,111,119,45,72,101,97,100,101,114,115,58,32,13,10,65,99,99,101,115,115,45,67,111,110,116,114,111,108,45,65,108,108,111,119,45,77,101,116,104,111,100,115,58,32,13,10,65,99,99,101,115,115,45,67,111,110,116,114,111,108,45,65,108,108,111,119,45,79,114,105,103,105,110,58,32,13,10,65,99,99,101,115,115,45,67,111,110,116,114,111,108,45,69,120,112,111,115,101,45,72,101,97,100,101,114,115,58,32,13,10,65,99,99,101,115,115,45,67,111,110,116,114,111,108,45,77,97,120,45,65,103,101,58,32,13,10,67,111,110,116,101,110,116,45,76,101,110,103,116,104,58,32,
        };
        private HeaderReferences _headers;

        public bool HasConnection => (_bits & 0x2L) != 0;
        public bool HasDate => (_bits & 0x4L) != 0;
        public bool HasTransferEncoding => (_bits & 0x40L) != 0;
        public bool HasServer => (_bits & 0x4000000L) != 0;

        
        public StringValues HeaderCacheControl
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x1L) != 0)
                {
                    value = _headers._CacheControl;
                }
                return value;
            }
            set
            {
                _bits |= 0x1L;
                _headers._CacheControl = value; 
            }
        }
        public StringValues HeaderConnection
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x2L) != 0)
                {
                    value = _headers._Connection;
                }
                return value;
            }
            set
            {
                _bits |= 0x2L;
                _headers._Connection = value; 
                _headers._rawConnection = null;
            }
        }
        public StringValues HeaderDate
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x4L) != 0)
                {
                    value = _headers._Date;
                }
                return value;
            }
            set
            {
                _bits |= 0x4L;
                _headers._Date = value; 
                _headers._rawDate = null;
            }
        }
        public StringValues HeaderKeepAlive
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x8L) != 0)
                {
                    value = _headers._KeepAlive;
                }
                return value;
            }
            set
            {
                _bits |= 0x8L;
                _headers._KeepAlive = value; 
            }
        }
        public StringValues HeaderPragma
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x10L) != 0)
                {
                    value = _headers._Pragma;
                }
                return value;
            }
            set
            {
                _bits |= 0x10L;
                _headers._Pragma = value; 
            }
        }
        public StringValues HeaderTrailer
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x20L) != 0)
                {
                    value = _headers._Trailer;
                }
                return value;
            }
            set
            {
                _bits |= 0x20L;
                _headers._Trailer = value; 
            }
        }
        public StringValues HeaderTransferEncoding
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x40L) != 0)
                {
                    value = _headers._TransferEncoding;
                }
                return value;
            }
            set
            {
                _bits |= 0x40L;
                _headers._TransferEncoding = value; 
                _headers._rawTransferEncoding = null;
            }
        }
        public StringValues HeaderUpgrade
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x80L) != 0)
                {
                    value = _headers._Upgrade;
                }
                return value;
            }
            set
            {
                _bits |= 0x80L;
                _headers._Upgrade = value; 
            }
        }
        public StringValues HeaderVia
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x100L) != 0)
                {
                    value = _headers._Via;
                }
                return value;
            }
            set
            {
                _bits |= 0x100L;
                _headers._Via = value; 
            }
        }
        public StringValues HeaderWarning
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x200L) != 0)
                {
                    value = _headers._Warning;
                }
                return value;
            }
            set
            {
                _bits |= 0x200L;
                _headers._Warning = value; 
            }
        }
        public StringValues HeaderAllow
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x400L) != 0)
                {
                    value = _headers._Allow;
                }
                return value;
            }
            set
            {
                _bits |= 0x400L;
                _headers._Allow = value; 
            }
        }
        public StringValues HeaderContentType
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x800L) != 0)
                {
                    value = _headers._ContentType;
                }
                return value;
            }
            set
            {
                _bits |= 0x800L;
                _headers._ContentType = value; 
            }
        }
        public StringValues HeaderContentEncoding
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x1000L) != 0)
                {
                    value = _headers._ContentEncoding;
                }
                return value;
            }
            set
            {
                _bits |= 0x1000L;
                _headers._ContentEncoding = value; 
            }
        }
        public StringValues HeaderContentLanguage
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x2000L) != 0)
                {
                    value = _headers._ContentLanguage;
                }
                return value;
            }
            set
            {
                _bits |= 0x2000L;
                _headers._ContentLanguage = value; 
            }
        }
        public StringValues HeaderContentLocation
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x4000L) != 0)
                {
                    value = _headers._ContentLocation;
                }
                return value;
            }
            set
            {
                _bits |= 0x4000L;
                _headers._ContentLocation = value; 
            }
        }
        public StringValues HeaderContentMD5
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x8000L) != 0)
                {
                    value = _headers._ContentMD5;
                }
                return value;
            }
            set
            {
                _bits |= 0x8000L;
                _headers._ContentMD5 = value; 
            }
        }
        public StringValues HeaderContentRange
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x10000L) != 0)
                {
                    value = _headers._ContentRange;
                }
                return value;
            }
            set
            {
                _bits |= 0x10000L;
                _headers._ContentRange = value; 
            }
        }
        public StringValues HeaderExpires
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x20000L) != 0)
                {
                    value = _headers._Expires;
                }
                return value;
            }
            set
            {
                _bits |= 0x20000L;
                _headers._Expires = value; 
            }
        }
        public StringValues HeaderLastModified
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x40000L) != 0)
                {
                    value = _headers._LastModified;
                }
                return value;
            }
            set
            {
                _bits |= 0x40000L;
                _headers._LastModified = value; 
            }
        }
        public StringValues HeaderAcceptRanges
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x80000L) != 0)
                {
                    value = _headers._AcceptRanges;
                }
                return value;
            }
            set
            {
                _bits |= 0x80000L;
                _headers._AcceptRanges = value; 
            }
        }
        public StringValues HeaderAge
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x100000L) != 0)
                {
                    value = _headers._Age;
                }
                return value;
            }
            set
            {
                _bits |= 0x100000L;
                _headers._Age = value; 
            }
        }
        public StringValues HeaderAltSvc
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x200000L) != 0)
                {
                    value = _headers._AltSvc;
                }
                return value;
            }
            set
            {
                _bits |= 0x200000L;
                _headers._AltSvc = value; 
            }
        }
        public StringValues HeaderETag
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x400000L) != 0)
                {
                    value = _headers._ETag;
                }
                return value;
            }
            set
            {
                _bits |= 0x400000L;
                _headers._ETag = value; 
            }
        }
        public StringValues HeaderLocation
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x800000L) != 0)
                {
                    value = _headers._Location;
                }
                return value;
            }
            set
            {
                _bits |= 0x800000L;
                _headers._Location = value; 
            }
        }
        public StringValues HeaderProxyAuthenticate
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x1000000L) != 0)
                {
                    value = _headers._ProxyAuthenticate;
                }
                return value;
            }
            set
            {
                _bits |= 0x1000000L;
                _headers._ProxyAuthenticate = value; 
            }
        }
        public StringValues HeaderRetryAfter
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x2000000L) != 0)
                {
                    value = _headers._RetryAfter;
                }
                return value;
            }
            set
            {
                _bits |= 0x2000000L;
                _headers._RetryAfter = value; 
            }
        }
        public StringValues HeaderServer
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x4000000L) != 0)
                {
                    value = _headers._Server;
                }
                return value;
            }
            set
            {
                _bits |= 0x4000000L;
                _headers._Server = value; 
                _headers._rawServer = null;
            }
        }
        public StringValues HeaderSetCookie
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x8000000L) != 0)
                {
                    value = _headers._SetCookie;
                }
                return value;
            }
            set
            {
                _bits |= 0x8000000L;
                _headers._SetCookie = value; 
            }
        }
        public StringValues HeaderVary
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x10000000L) != 0)
                {
                    value = _headers._Vary;
                }
                return value;
            }
            set
            {
                _bits |= 0x10000000L;
                _headers._Vary = value; 
            }
        }
        public StringValues HeaderWWWAuthenticate
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x20000000L) != 0)
                {
                    value = _headers._WWWAuthenticate;
                }
                return value;
            }
            set
            {
                _bits |= 0x20000000L;
                _headers._WWWAuthenticate = value; 
            }
        }
        public StringValues HeaderAccessControlAllowCredentials
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x40000000L) != 0)
                {
                    value = _headers._AccessControlAllowCredentials;
                }
                return value;
            }
            set
            {
                _bits |= 0x40000000L;
                _headers._AccessControlAllowCredentials = value; 
            }
        }
        public StringValues HeaderAccessControlAllowHeaders
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x80000000L) != 0)
                {
                    value = _headers._AccessControlAllowHeaders;
                }
                return value;
            }
            set
            {
                _bits |= 0x80000000L;
                _headers._AccessControlAllowHeaders = value; 
            }
        }
        public StringValues HeaderAccessControlAllowMethods
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x100000000L) != 0)
                {
                    value = _headers._AccessControlAllowMethods;
                }
                return value;
            }
            set
            {
                _bits |= 0x100000000L;
                _headers._AccessControlAllowMethods = value; 
            }
        }
        public StringValues HeaderAccessControlAllowOrigin
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x200000000L) != 0)
                {
                    value = _headers._AccessControlAllowOrigin;
                }
                return value;
            }
            set
            {
                _bits |= 0x200000000L;
                _headers._AccessControlAllowOrigin = value; 
            }
        }
        public StringValues HeaderAccessControlExposeHeaders
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x400000000L) != 0)
                {
                    value = _headers._AccessControlExposeHeaders;
                }
                return value;
            }
            set
            {
                _bits |= 0x400000000L;
                _headers._AccessControlExposeHeaders = value; 
            }
        }
        public StringValues HeaderAccessControlMaxAge
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x800000000L) != 0)
                {
                    value = _headers._AccessControlMaxAge;
                }
                return value;
            }
            set
            {
                _bits |= 0x800000000L;
                _headers._AccessControlMaxAge = value; 
            }
        }
        public StringValues HeaderContentLength
        {
            get
            {
                StringValues value = default;
                if (_contentLength.HasValue)
                {
                    value = new StringValues(HeaderUtilities.FormatNonNegativeInt64(_contentLength.Value));
                }
                return value;
            }
            set
            {
                _contentLength = ParseContentLength(value);
            }
        }

        public void SetRawConnection(StringValues value, byte[] raw)
        {
            _bits |= 0x2L;
            _headers._Connection = value;
            _headers._rawConnection = raw;
        }
        public void SetRawDate(StringValues value, byte[] raw)
        {
            _bits |= 0x4L;
            _headers._Date = value;
            _headers._rawDate = raw;
        }
        public void SetRawTransferEncoding(StringValues value, byte[] raw)
        {
            _bits |= 0x40L;
            _headers._TransferEncoding = value;
            _headers._rawTransferEncoding = raw;
        }
        public void SetRawServer(StringValues value, byte[] raw)
        {
            _bits |= 0x4000000L;
            _headers._Server = value;
            _headers._rawServer = raw;
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
                    if (ReferenceEquals(HeaderNames.Via, key))
                    {
                        if ((_bits & 0x100L) != 0)
                        {
                            value = _headers._Via;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Age, key))
                    {
                        if ((_bits & 0x100000L) != 0)
                        {
                            value = _headers._Age;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.Via.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100L) != 0)
                        {
                            value = _headers._Via;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Age.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100000L) != 0)
                        {
                            value = _headers._Age;
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
                        if ((_bits & 0x400000L) != 0)
                        {
                            value = _headers._ETag;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Vary, key))
                    {
                        if ((_bits & 0x10000000L) != 0)
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
                        if ((_bits & 0x400000L) != 0)
                        {
                            value = _headers._ETag;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Vary.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000000L) != 0)
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
                        if ((_bits & 0x400L) != 0)
                        {
                            value = _headers._Allow;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.Allow.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400L) != 0)
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
                        if ((_bits & 0x4000000L) != 0)
                        {
                            value = _headers._Server;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Pragma, key))
                    {
                        if ((_bits & 0x10L) != 0)
                        {
                            value = _headers._Pragma;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.Server.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000000L) != 0)
                        {
                            value = _headers._Server;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Pragma.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10L) != 0)
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
                    if (ReferenceEquals(HeaderNames.Trailer, key))
                    {
                        if ((_bits & 0x20L) != 0)
                        {
                            value = _headers._Trailer;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Upgrade, key))
                    {
                        if ((_bits & 0x80L) != 0)
                        {
                            value = _headers._Upgrade;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Warning, key))
                    {
                        if ((_bits & 0x200L) != 0)
                        {
                            value = _headers._Warning;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Expires, key))
                    {
                        if ((_bits & 0x20000L) != 0)
                        {
                            value = _headers._Expires;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.AltSvc, key))
                    {
                        if ((_bits & 0x200000L) != 0)
                        {
                            value = _headers._AltSvc;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.Trailer.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20L) != 0)
                        {
                            value = _headers._Trailer;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Upgrade.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80L) != 0)
                        {
                            value = _headers._Upgrade;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Warning.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200L) != 0)
                        {
                            value = _headers._Warning;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Expires.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20000L) != 0)
                        {
                            value = _headers._Expires;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.AltSvc.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200000L) != 0)
                        {
                            value = _headers._AltSvc;
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
                        if ((_bits & 0x800000L) != 0)
                        {
                            value = _headers._Location;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.Location.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800000L) != 0)
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
                        if ((_bits & 0x2L) != 0)
                        {
                            value = _headers._Connection;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.KeepAlive, key))
                    {
                        if ((_bits & 0x8L) != 0)
                        {
                            value = _headers._KeepAlive;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.SetCookie, key))
                    {
                        if ((_bits & 0x8000000L) != 0)
                        {
                            value = _headers._SetCookie;
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
                    if (HeaderNames.KeepAlive.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8L) != 0)
                        {
                            value = _headers._KeepAlive;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.SetCookie.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000000L) != 0)
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
                        if ((_bits & 0x8000L) != 0)
                        {
                            value = _headers._ContentMD5;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.RetryAfter, key))
                    {
                        if ((_bits & 0x2000000L) != 0)
                        {
                            value = _headers._RetryAfter;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.ContentMD5.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000L) != 0)
                        {
                            value = _headers._ContentMD5;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.RetryAfter.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000000L) != 0)
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
                        if ((_bits & 0x800L) != 0)
                        {
                            value = _headers._ContentType;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.ContentType.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800L) != 0)
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
                    if (ReferenceEquals(HeaderNames.CacheControl, key))
                    {
                        if ((_bits & 0x1L) != 0)
                        {
                            value = _headers._CacheControl;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentRange, key))
                    {
                        if ((_bits & 0x10000L) != 0)
                        {
                            value = _headers._ContentRange;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.LastModified, key))
                    {
                        if ((_bits & 0x40000L) != 0)
                        {
                            value = _headers._LastModified;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.AcceptRanges, key))
                    {
                        if ((_bits & 0x80000L) != 0)
                        {
                            value = _headers._AcceptRanges;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.CacheControl.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1L) != 0)
                        {
                            value = _headers._CacheControl;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentRange.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000L) != 0)
                        {
                            value = _headers._ContentRange;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.LastModified.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40000L) != 0)
                        {
                            value = _headers._LastModified;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.AcceptRanges.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80000L) != 0)
                        {
                            value = _headers._AcceptRanges;
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
                        if ((_bits & 0x1000L) != 0)
                        {
                            value = _headers._ContentEncoding;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentLanguage, key))
                    {
                        if ((_bits & 0x2000L) != 0)
                        {
                            value = _headers._ContentLanguage;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentLocation, key))
                    {
                        if ((_bits & 0x4000L) != 0)
                        {
                            value = _headers._ContentLocation;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.WWWAuthenticate, key))
                    {
                        if ((_bits & 0x20000000L) != 0)
                        {
                            value = _headers._WWWAuthenticate;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.ContentEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000L) != 0)
                        {
                            value = _headers._ContentEncoding;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentLanguage.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000L) != 0)
                        {
                            value = _headers._ContentLanguage;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentLocation.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000L) != 0)
                        {
                            value = _headers._ContentLocation;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.WWWAuthenticate.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20000000L) != 0)
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
                        if ((_bits & 0x40L) != 0)
                        {
                            value = _headers._TransferEncoding;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.TransferEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40L) != 0)
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
                        if ((_bits & 0x1000000L) != 0)
                        {
                            value = _headers._ProxyAuthenticate;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.ProxyAuthenticate.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000000L) != 0)
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
                        if ((_bits & 0x800000000L) != 0)
                        {
                            value = _headers._AccessControlMaxAge;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.AccessControlMaxAge.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800000000L) != 0)
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
                        if ((_bits & 0x200000000L) != 0)
                        {
                            value = _headers._AccessControlAllowOrigin;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.AccessControlAllowOrigin.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200000000L) != 0)
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
                        if ((_bits & 0x80000000L) != 0)
                        {
                            value = _headers._AccessControlAllowHeaders;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.AccessControlAllowMethods, key))
                    {
                        if ((_bits & 0x100000000L) != 0)
                        {
                            value = _headers._AccessControlAllowMethods;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.AccessControlAllowHeaders.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80000000L) != 0)
                        {
                            value = _headers._AccessControlAllowHeaders;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.AccessControlAllowMethods.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100000000L) != 0)
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
                        if ((_bits & 0x400000000L) != 0)
                        {
                            value = _headers._AccessControlExposeHeaders;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.AccessControlExposeHeaders.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400000000L) != 0)
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
                        if ((_bits & 0x40000000L) != 0)
                        {
                            value = _headers._AccessControlAllowCredentials;
                            return true;
                        }
                        return false;
                    }

                    if (HeaderNames.AccessControlAllowCredentials.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40000000L) != 0)
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
            ValidateHeaderValueCharacters(value);
            switch (key.Length)
            {
                case 3:
                {
                    if (ReferenceEquals(HeaderNames.Via, key))
                    {
                        _bits |= 0x100L;
                        _headers._Via = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Age, key))
                    {
                        _bits |= 0x100000L;
                        _headers._Age = value;
                        return;
                    }

                    if (HeaderNames.Via.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x100L;
                        _headers._Via = value;
                        return;
                    }
                    if (HeaderNames.Age.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x100000L;
                        _headers._Age = value;
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
                        _bits |= 0x400000L;
                        _headers._ETag = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Vary, key))
                    {
                        _bits |= 0x10000000L;
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
                        _bits |= 0x400000L;
                        _headers._ETag = value;
                        return;
                    }
                    if (HeaderNames.Vary.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x10000000L;
                        _headers._Vary = value;
                        return;
                    }
                    break;
                }
                case 5:
                {
                    if (ReferenceEquals(HeaderNames.Allow, key))
                    {
                        _bits |= 0x400L;
                        _headers._Allow = value;
                        return;
                    }

                    if (HeaderNames.Allow.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x400L;
                        _headers._Allow = value;
                        return;
                    }
                    break;
                }
                case 6:
                {
                    if (ReferenceEquals(HeaderNames.Server, key))
                    {
                        _bits |= 0x4000000L;
                        _headers._Server = value;
                        _headers._rawServer = null;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Pragma, key))
                    {
                        _bits |= 0x10L;
                        _headers._Pragma = value;
                        return;
                    }

                    if (HeaderNames.Server.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x4000000L;
                        _headers._Server = value;
                        _headers._rawServer = null;
                        return;
                    }
                    if (HeaderNames.Pragma.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x10L;
                        _headers._Pragma = value;
                        return;
                    }
                    break;
                }
                case 7:
                {
                    if (ReferenceEquals(HeaderNames.Trailer, key))
                    {
                        _bits |= 0x20L;
                        _headers._Trailer = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Upgrade, key))
                    {
                        _bits |= 0x80L;
                        _headers._Upgrade = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Warning, key))
                    {
                        _bits |= 0x200L;
                        _headers._Warning = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.Expires, key))
                    {
                        _bits |= 0x20000L;
                        _headers._Expires = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.AltSvc, key))
                    {
                        _bits |= 0x200000L;
                        _headers._AltSvc = value;
                        return;
                    }

                    if (HeaderNames.Trailer.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x20L;
                        _headers._Trailer = value;
                        return;
                    }
                    if (HeaderNames.Upgrade.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x80L;
                        _headers._Upgrade = value;
                        return;
                    }
                    if (HeaderNames.Warning.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x200L;
                        _headers._Warning = value;
                        return;
                    }
                    if (HeaderNames.Expires.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x20000L;
                        _headers._Expires = value;
                        return;
                    }
                    if (HeaderNames.AltSvc.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x200000L;
                        _headers._AltSvc = value;
                        return;
                    }
                    break;
                }
                case 8:
                {
                    if (ReferenceEquals(HeaderNames.Location, key))
                    {
                        _bits |= 0x800000L;
                        _headers._Location = value;
                        return;
                    }

                    if (HeaderNames.Location.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x800000L;
                        _headers._Location = value;
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
                        _headers._rawConnection = null;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.KeepAlive, key))
                    {
                        _bits |= 0x8L;
                        _headers._KeepAlive = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.SetCookie, key))
                    {
                        _bits |= 0x8000000L;
                        _headers._SetCookie = value;
                        return;
                    }

                    if (HeaderNames.Connection.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x2L;
                        _headers._Connection = value;
                        _headers._rawConnection = null;
                        return;
                    }
                    if (HeaderNames.KeepAlive.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x8L;
                        _headers._KeepAlive = value;
                        return;
                    }
                    if (HeaderNames.SetCookie.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x8000000L;
                        _headers._SetCookie = value;
                        return;
                    }
                    break;
                }
                case 11:
                {
                    if (ReferenceEquals(HeaderNames.ContentMD5, key))
                    {
                        _bits |= 0x8000L;
                        _headers._ContentMD5 = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.RetryAfter, key))
                    {
                        _bits |= 0x2000000L;
                        _headers._RetryAfter = value;
                        return;
                    }

                    if (HeaderNames.ContentMD5.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x8000L;
                        _headers._ContentMD5 = value;
                        return;
                    }
                    if (HeaderNames.RetryAfter.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x2000000L;
                        _headers._RetryAfter = value;
                        return;
                    }
                    break;
                }
                case 12:
                {
                    if (ReferenceEquals(HeaderNames.ContentType, key))
                    {
                        _bits |= 0x800L;
                        _headers._ContentType = value;
                        return;
                    }

                    if (HeaderNames.ContentType.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x800L;
                        _headers._ContentType = value;
                        return;
                    }
                    break;
                }
                case 13:
                {
                    if (ReferenceEquals(HeaderNames.CacheControl, key))
                    {
                        _bits |= 0x1L;
                        _headers._CacheControl = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.ContentRange, key))
                    {
                        _bits |= 0x10000L;
                        _headers._ContentRange = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.LastModified, key))
                    {
                        _bits |= 0x40000L;
                        _headers._LastModified = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.AcceptRanges, key))
                    {
                        _bits |= 0x80000L;
                        _headers._AcceptRanges = value;
                        return;
                    }

                    if (HeaderNames.CacheControl.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x1L;
                        _headers._CacheControl = value;
                        return;
                    }
                    if (HeaderNames.ContentRange.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x10000L;
                        _headers._ContentRange = value;
                        return;
                    }
                    if (HeaderNames.LastModified.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x40000L;
                        _headers._LastModified = value;
                        return;
                    }
                    if (HeaderNames.AcceptRanges.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x80000L;
                        _headers._AcceptRanges = value;
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
                        _bits |= 0x1000L;
                        _headers._ContentEncoding = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.ContentLanguage, key))
                    {
                        _bits |= 0x2000L;
                        _headers._ContentLanguage = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.ContentLocation, key))
                    {
                        _bits |= 0x4000L;
                        _headers._ContentLocation = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.WWWAuthenticate, key))
                    {
                        _bits |= 0x20000000L;
                        _headers._WWWAuthenticate = value;
                        return;
                    }

                    if (HeaderNames.ContentEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x1000L;
                        _headers._ContentEncoding = value;
                        return;
                    }
                    if (HeaderNames.ContentLanguage.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x2000L;
                        _headers._ContentLanguage = value;
                        return;
                    }
                    if (HeaderNames.ContentLocation.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x4000L;
                        _headers._ContentLocation = value;
                        return;
                    }
                    if (HeaderNames.WWWAuthenticate.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x20000000L;
                        _headers._WWWAuthenticate = value;
                        return;
                    }
                    break;
                }
                case 17:
                {
                    if (ReferenceEquals(HeaderNames.TransferEncoding, key))
                    {
                        _bits |= 0x40L;
                        _headers._TransferEncoding = value;
                        _headers._rawTransferEncoding = null;
                        return;
                    }

                    if (HeaderNames.TransferEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x40L;
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
                        _bits |= 0x1000000L;
                        _headers._ProxyAuthenticate = value;
                        return;
                    }

                    if (HeaderNames.ProxyAuthenticate.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x1000000L;
                        _headers._ProxyAuthenticate = value;
                        return;
                    }
                    break;
                }
                case 22:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlMaxAge, key))
                    {
                        _bits |= 0x800000000L;
                        _headers._AccessControlMaxAge = value;
                        return;
                    }

                    if (HeaderNames.AccessControlMaxAge.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x800000000L;
                        _headers._AccessControlMaxAge = value;
                        return;
                    }
                    break;
                }
                case 27:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlAllowOrigin, key))
                    {
                        _bits |= 0x200000000L;
                        _headers._AccessControlAllowOrigin = value;
                        return;
                    }

                    if (HeaderNames.AccessControlAllowOrigin.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x200000000L;
                        _headers._AccessControlAllowOrigin = value;
                        return;
                    }
                    break;
                }
                case 28:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlAllowHeaders, key))
                    {
                        _bits |= 0x80000000L;
                        _headers._AccessControlAllowHeaders = value;
                        return;
                    }
                    if (ReferenceEquals(HeaderNames.AccessControlAllowMethods, key))
                    {
                        _bits |= 0x100000000L;
                        _headers._AccessControlAllowMethods = value;
                        return;
                    }

                    if (HeaderNames.AccessControlAllowHeaders.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x80000000L;
                        _headers._AccessControlAllowHeaders = value;
                        return;
                    }
                    if (HeaderNames.AccessControlAllowMethods.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x100000000L;
                        _headers._AccessControlAllowMethods = value;
                        return;
                    }
                    break;
                }
                case 29:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlExposeHeaders, key))
                    {
                        _bits |= 0x400000000L;
                        _headers._AccessControlExposeHeaders = value;
                        return;
                    }

                    if (HeaderNames.AccessControlExposeHeaders.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x400000000L;
                        _headers._AccessControlExposeHeaders = value;
                        return;
                    }
                    break;
                }
                case 32:
                {
                    if (ReferenceEquals(HeaderNames.AccessControlAllowCredentials, key))
                    {
                        _bits |= 0x40000000L;
                        _headers._AccessControlAllowCredentials = value;
                        return;
                    }

                    if (HeaderNames.AccessControlAllowCredentials.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _bits |= 0x40000000L;
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
            ValidateHeaderValueCharacters(value);
            switch (key.Length)
            {
                case 3:
                {
                    if (ReferenceEquals(HeaderNames.Via, key))
                    {
                        if ((_bits & 0x100L) == 0)
                        {
                            _bits |= 0x100L;
                            _headers._Via = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Age, key))
                    {
                        if ((_bits & 0x100000L) == 0)
                        {
                            _bits |= 0x100000L;
                            _headers._Age = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Via.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100L) == 0)
                        {
                            _bits |= 0x100L;
                            _headers._Via = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Age.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100000L) == 0)
                        {
                            _bits |= 0x100000L;
                            _headers._Age = value;
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
                        if ((_bits & 0x400000L) == 0)
                        {
                            _bits |= 0x400000L;
                            _headers._ETag = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Vary, key))
                    {
                        if ((_bits & 0x10000000L) == 0)
                        {
                            _bits |= 0x10000000L;
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
                        if ((_bits & 0x400000L) == 0)
                        {
                            _bits |= 0x400000L;
                            _headers._ETag = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Vary.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000000L) == 0)
                        {
                            _bits |= 0x10000000L;
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
                        if ((_bits & 0x400L) == 0)
                        {
                            _bits |= 0x400L;
                            _headers._Allow = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Allow.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400L) == 0)
                        {
                            _bits |= 0x400L;
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
                        if ((_bits & 0x4000000L) == 0)
                        {
                            _bits |= 0x4000000L;
                            _headers._Server = value;
                            _headers._rawServer = null;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Pragma, key))
                    {
                        if ((_bits & 0x10L) == 0)
                        {
                            _bits |= 0x10L;
                            _headers._Pragma = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Server.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000000L) == 0)
                        {
                            _bits |= 0x4000000L;
                            _headers._Server = value;
                            _headers._rawServer = null;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Pragma.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10L) == 0)
                        {
                            _bits |= 0x10L;
                            _headers._Pragma = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 7:
                {
                    if (ReferenceEquals(HeaderNames.Trailer, key))
                    {
                        if ((_bits & 0x20L) == 0)
                        {
                            _bits |= 0x20L;
                            _headers._Trailer = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Upgrade, key))
                    {
                        if ((_bits & 0x80L) == 0)
                        {
                            _bits |= 0x80L;
                            _headers._Upgrade = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Warning, key))
                    {
                        if ((_bits & 0x200L) == 0)
                        {
                            _bits |= 0x200L;
                            _headers._Warning = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Expires, key))
                    {
                        if ((_bits & 0x20000L) == 0)
                        {
                            _bits |= 0x20000L;
                            _headers._Expires = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.AltSvc, key))
                    {
                        if ((_bits & 0x200000L) == 0)
                        {
                            _bits |= 0x200000L;
                            _headers._AltSvc = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Trailer.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20L) == 0)
                        {
                            _bits |= 0x20L;
                            _headers._Trailer = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Upgrade.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80L) == 0)
                        {
                            _bits |= 0x80L;
                            _headers._Upgrade = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Warning.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200L) == 0)
                        {
                            _bits |= 0x200L;
                            _headers._Warning = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Expires.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20000L) == 0)
                        {
                            _bits |= 0x20000L;
                            _headers._Expires = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.AltSvc.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200000L) == 0)
                        {
                            _bits |= 0x200000L;
                            _headers._AltSvc = value;
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
                        if ((_bits & 0x800000L) == 0)
                        {
                            _bits |= 0x800000L;
                            _headers._Location = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Location.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800000L) == 0)
                        {
                            _bits |= 0x800000L;
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
                        if ((_bits & 0x2L) == 0)
                        {
                            _bits |= 0x2L;
                            _headers._Connection = value;
                            _headers._rawConnection = null;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.KeepAlive, key))
                    {
                        if ((_bits & 0x8L) == 0)
                        {
                            _bits |= 0x8L;
                            _headers._KeepAlive = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.SetCookie, key))
                    {
                        if ((_bits & 0x8000000L) == 0)
                        {
                            _bits |= 0x8000000L;
                            _headers._SetCookie = value;
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
                            _headers._rawConnection = null;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.KeepAlive.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8L) == 0)
                        {
                            _bits |= 0x8L;
                            _headers._KeepAlive = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.SetCookie.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000000L) == 0)
                        {
                            _bits |= 0x8000000L;
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
                        if ((_bits & 0x8000L) == 0)
                        {
                            _bits |= 0x8000L;
                            _headers._ContentMD5 = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.RetryAfter, key))
                    {
                        if ((_bits & 0x2000000L) == 0)
                        {
                            _bits |= 0x2000000L;
                            _headers._RetryAfter = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.ContentMD5.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000L) == 0)
                        {
                            _bits |= 0x8000L;
                            _headers._ContentMD5 = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.RetryAfter.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000000L) == 0)
                        {
                            _bits |= 0x2000000L;
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
                        if ((_bits & 0x800L) == 0)
                        {
                            _bits |= 0x800L;
                            _headers._ContentType = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.ContentType.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800L) == 0)
                        {
                            _bits |= 0x800L;
                            _headers._ContentType = value;
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 13:
                {
                    if (ReferenceEquals(HeaderNames.CacheControl, key))
                    {
                        if ((_bits & 0x1L) == 0)
                        {
                            _bits |= 0x1L;
                            _headers._CacheControl = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentRange, key))
                    {
                        if ((_bits & 0x10000L) == 0)
                        {
                            _bits |= 0x10000L;
                            _headers._ContentRange = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.LastModified, key))
                    {
                        if ((_bits & 0x40000L) == 0)
                        {
                            _bits |= 0x40000L;
                            _headers._LastModified = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.AcceptRanges, key))
                    {
                        if ((_bits & 0x80000L) == 0)
                        {
                            _bits |= 0x80000L;
                            _headers._AcceptRanges = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.CacheControl.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1L) == 0)
                        {
                            _bits |= 0x1L;
                            _headers._CacheControl = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentRange.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000L) == 0)
                        {
                            _bits |= 0x10000L;
                            _headers._ContentRange = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.LastModified.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40000L) == 0)
                        {
                            _bits |= 0x40000L;
                            _headers._LastModified = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.AcceptRanges.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80000L) == 0)
                        {
                            _bits |= 0x80000L;
                            _headers._AcceptRanges = value;
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
                            _contentLength = ParseContentLength(value);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.ContentLength.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!_contentLength.HasValue)
                        {
                            _contentLength = ParseContentLength(value);
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
                        if ((_bits & 0x1000L) == 0)
                        {
                            _bits |= 0x1000L;
                            _headers._ContentEncoding = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentLanguage, key))
                    {
                        if ((_bits & 0x2000L) == 0)
                        {
                            _bits |= 0x2000L;
                            _headers._ContentLanguage = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentLocation, key))
                    {
                        if ((_bits & 0x4000L) == 0)
                        {
                            _bits |= 0x4000L;
                            _headers._ContentLocation = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.WWWAuthenticate, key))
                    {
                        if ((_bits & 0x20000000L) == 0)
                        {
                            _bits |= 0x20000000L;
                            _headers._WWWAuthenticate = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.ContentEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000L) == 0)
                        {
                            _bits |= 0x1000L;
                            _headers._ContentEncoding = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentLanguage.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000L) == 0)
                        {
                            _bits |= 0x2000L;
                            _headers._ContentLanguage = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentLocation.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000L) == 0)
                        {
                            _bits |= 0x4000L;
                            _headers._ContentLocation = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.WWWAuthenticate.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20000000L) == 0)
                        {
                            _bits |= 0x20000000L;
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
                        if ((_bits & 0x40L) == 0)
                        {
                            _bits |= 0x40L;
                            _headers._TransferEncoding = value;
                            _headers._rawTransferEncoding = null;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.TransferEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40L) == 0)
                        {
                            _bits |= 0x40L;
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
                        if ((_bits & 0x1000000L) == 0)
                        {
                            _bits |= 0x1000000L;
                            _headers._ProxyAuthenticate = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.ProxyAuthenticate.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000000L) == 0)
                        {
                            _bits |= 0x1000000L;
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
                        if ((_bits & 0x800000000L) == 0)
                        {
                            _bits |= 0x800000000L;
                            _headers._AccessControlMaxAge = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AccessControlMaxAge.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800000000L) == 0)
                        {
                            _bits |= 0x800000000L;
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
                        if ((_bits & 0x200000000L) == 0)
                        {
                            _bits |= 0x200000000L;
                            _headers._AccessControlAllowOrigin = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AccessControlAllowOrigin.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200000000L) == 0)
                        {
                            _bits |= 0x200000000L;
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
                        if ((_bits & 0x80000000L) == 0)
                        {
                            _bits |= 0x80000000L;
                            _headers._AccessControlAllowHeaders = value;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.AccessControlAllowMethods, key))
                    {
                        if ((_bits & 0x100000000L) == 0)
                        {
                            _bits |= 0x100000000L;
                            _headers._AccessControlAllowMethods = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AccessControlAllowHeaders.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80000000L) == 0)
                        {
                            _bits |= 0x80000000L;
                            _headers._AccessControlAllowHeaders = value;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.AccessControlAllowMethods.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100000000L) == 0)
                        {
                            _bits |= 0x100000000L;
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
                        if ((_bits & 0x400000000L) == 0)
                        {
                            _bits |= 0x400000000L;
                            _headers._AccessControlExposeHeaders = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AccessControlExposeHeaders.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400000000L) == 0)
                        {
                            _bits |= 0x400000000L;
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
                        if ((_bits & 0x40000000L) == 0)
                        {
                            _bits |= 0x40000000L;
                            _headers._AccessControlAllowCredentials = value;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AccessControlAllowCredentials.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40000000L) == 0)
                        {
                            _bits |= 0x40000000L;
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
                    if (ReferenceEquals(HeaderNames.Via, key))
                    {
                        if ((_bits & 0x100L) != 0)
                        {
                            _bits &= ~0x100L;
                            _headers._Via = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Age, key))
                    {
                        if ((_bits & 0x100000L) != 0)
                        {
                            _bits &= ~0x100000L;
                            _headers._Age = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Via.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100L) != 0)
                        {
                            _bits &= ~0x100L;
                            _headers._Via = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Age.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100000L) != 0)
                        {
                            _bits &= ~0x100000L;
                            _headers._Age = default(StringValues);
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
                        if ((_bits & 0x400000L) != 0)
                        {
                            _bits &= ~0x400000L;
                            _headers._ETag = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Vary, key))
                    {
                        if ((_bits & 0x10000000L) != 0)
                        {
                            _bits &= ~0x10000000L;
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
                        if ((_bits & 0x400000L) != 0)
                        {
                            _bits &= ~0x400000L;
                            _headers._ETag = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Vary.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000000L) != 0)
                        {
                            _bits &= ~0x10000000L;
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
                        if ((_bits & 0x400L) != 0)
                        {
                            _bits &= ~0x400L;
                            _headers._Allow = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Allow.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400L) != 0)
                        {
                            _bits &= ~0x400L;
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
                        if ((_bits & 0x4000000L) != 0)
                        {
                            _bits &= ~0x4000000L;
                            _headers._Server = default(StringValues);
                            _headers._rawServer = null;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Pragma, key))
                    {
                        if ((_bits & 0x10L) != 0)
                        {
                            _bits &= ~0x10L;
                            _headers._Pragma = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Server.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000000L) != 0)
                        {
                            _bits &= ~0x4000000L;
                            _headers._Server = default(StringValues);
                            _headers._rawServer = null;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Pragma.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10L) != 0)
                        {
                            _bits &= ~0x10L;
                            _headers._Pragma = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 7:
                {
                    if (ReferenceEquals(HeaderNames.Trailer, key))
                    {
                        if ((_bits & 0x20L) != 0)
                        {
                            _bits &= ~0x20L;
                            _headers._Trailer = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Upgrade, key))
                    {
                        if ((_bits & 0x80L) != 0)
                        {
                            _bits &= ~0x80L;
                            _headers._Upgrade = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Warning, key))
                    {
                        if ((_bits & 0x200L) != 0)
                        {
                            _bits &= ~0x200L;
                            _headers._Warning = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.Expires, key))
                    {
                        if ((_bits & 0x20000L) != 0)
                        {
                            _bits &= ~0x20000L;
                            _headers._Expires = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.AltSvc, key))
                    {
                        if ((_bits & 0x200000L) != 0)
                        {
                            _bits &= ~0x200000L;
                            _headers._AltSvc = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Trailer.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20L) != 0)
                        {
                            _bits &= ~0x20L;
                            _headers._Trailer = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Upgrade.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80L) != 0)
                        {
                            _bits &= ~0x80L;
                            _headers._Upgrade = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Warning.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200L) != 0)
                        {
                            _bits &= ~0x200L;
                            _headers._Warning = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.Expires.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20000L) != 0)
                        {
                            _bits &= ~0x20000L;
                            _headers._Expires = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.AltSvc.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200000L) != 0)
                        {
                            _bits &= ~0x200000L;
                            _headers._AltSvc = default(StringValues);
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
                        if ((_bits & 0x800000L) != 0)
                        {
                            _bits &= ~0x800000L;
                            _headers._Location = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.Location.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800000L) != 0)
                        {
                            _bits &= ~0x800000L;
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
                        if ((_bits & 0x2L) != 0)
                        {
                            _bits &= ~0x2L;
                            _headers._Connection = default(StringValues);
                            _headers._rawConnection = null;
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.KeepAlive, key))
                    {
                        if ((_bits & 0x8L) != 0)
                        {
                            _bits &= ~0x8L;
                            _headers._KeepAlive = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.SetCookie, key))
                    {
                        if ((_bits & 0x8000000L) != 0)
                        {
                            _bits &= ~0x8000000L;
                            _headers._SetCookie = default(StringValues);
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
                            _headers._rawConnection = null;
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.KeepAlive.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8L) != 0)
                        {
                            _bits &= ~0x8L;
                            _headers._KeepAlive = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.SetCookie.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000000L) != 0)
                        {
                            _bits &= ~0x8000000L;
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
                        if ((_bits & 0x8000L) != 0)
                        {
                            _bits &= ~0x8000L;
                            _headers._ContentMD5 = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.RetryAfter, key))
                    {
                        if ((_bits & 0x2000000L) != 0)
                        {
                            _bits &= ~0x2000000L;
                            _headers._RetryAfter = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.ContentMD5.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x8000L) != 0)
                        {
                            _bits &= ~0x8000L;
                            _headers._ContentMD5 = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.RetryAfter.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000000L) != 0)
                        {
                            _bits &= ~0x2000000L;
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
                        if ((_bits & 0x800L) != 0)
                        {
                            _bits &= ~0x800L;
                            _headers._ContentType = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.ContentType.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800L) != 0)
                        {
                            _bits &= ~0x800L;
                            _headers._ContentType = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    break;
                }
                case 13:
                {
                    if (ReferenceEquals(HeaderNames.CacheControl, key))
                    {
                        if ((_bits & 0x1L) != 0)
                        {
                            _bits &= ~0x1L;
                            _headers._CacheControl = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentRange, key))
                    {
                        if ((_bits & 0x10000L) != 0)
                        {
                            _bits &= ~0x10000L;
                            _headers._ContentRange = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.LastModified, key))
                    {
                        if ((_bits & 0x40000L) != 0)
                        {
                            _bits &= ~0x40000L;
                            _headers._LastModified = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.AcceptRanges, key))
                    {
                        if ((_bits & 0x80000L) != 0)
                        {
                            _bits &= ~0x80000L;
                            _headers._AcceptRanges = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.CacheControl.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1L) != 0)
                        {
                            _bits &= ~0x1L;
                            _headers._CacheControl = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentRange.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x10000L) != 0)
                        {
                            _bits &= ~0x10000L;
                            _headers._ContentRange = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.LastModified.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40000L) != 0)
                        {
                            _bits &= ~0x40000L;
                            _headers._LastModified = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.AcceptRanges.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80000L) != 0)
                        {
                            _bits &= ~0x80000L;
                            _headers._AcceptRanges = default(StringValues);
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
                        if ((_bits & 0x1000L) != 0)
                        {
                            _bits &= ~0x1000L;
                            _headers._ContentEncoding = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentLanguage, key))
                    {
                        if ((_bits & 0x2000L) != 0)
                        {
                            _bits &= ~0x2000L;
                            _headers._ContentLanguage = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.ContentLocation, key))
                    {
                        if ((_bits & 0x4000L) != 0)
                        {
                            _bits &= ~0x4000L;
                            _headers._ContentLocation = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.WWWAuthenticate, key))
                    {
                        if ((_bits & 0x20000000L) != 0)
                        {
                            _bits &= ~0x20000000L;
                            _headers._WWWAuthenticate = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.ContentEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000L) != 0)
                        {
                            _bits &= ~0x1000L;
                            _headers._ContentEncoding = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentLanguage.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x2000L) != 0)
                        {
                            _bits &= ~0x2000L;
                            _headers._ContentLanguage = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.ContentLocation.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x4000L) != 0)
                        {
                            _bits &= ~0x4000L;
                            _headers._ContentLocation = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.WWWAuthenticate.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x20000000L) != 0)
                        {
                            _bits &= ~0x20000000L;
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
                        if ((_bits & 0x40L) != 0)
                        {
                            _bits &= ~0x40L;
                            _headers._TransferEncoding = default(StringValues);
                            _headers._rawTransferEncoding = null;
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.TransferEncoding.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40L) != 0)
                        {
                            _bits &= ~0x40L;
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
                        if ((_bits & 0x1000000L) != 0)
                        {
                            _bits &= ~0x1000000L;
                            _headers._ProxyAuthenticate = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.ProxyAuthenticate.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x1000000L) != 0)
                        {
                            _bits &= ~0x1000000L;
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
                        if ((_bits & 0x800000000L) != 0)
                        {
                            _bits &= ~0x800000000L;
                            _headers._AccessControlMaxAge = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AccessControlMaxAge.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x800000000L) != 0)
                        {
                            _bits &= ~0x800000000L;
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
                        if ((_bits & 0x200000000L) != 0)
                        {
                            _bits &= ~0x200000000L;
                            _headers._AccessControlAllowOrigin = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AccessControlAllowOrigin.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x200000000L) != 0)
                        {
                            _bits &= ~0x200000000L;
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
                        if ((_bits & 0x80000000L) != 0)
                        {
                            _bits &= ~0x80000000L;
                            _headers._AccessControlAllowHeaders = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (ReferenceEquals(HeaderNames.AccessControlAllowMethods, key))
                    {
                        if ((_bits & 0x100000000L) != 0)
                        {
                            _bits &= ~0x100000000L;
                            _headers._AccessControlAllowMethods = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AccessControlAllowHeaders.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x80000000L) != 0)
                        {
                            _bits &= ~0x80000000L;
                            _headers._AccessControlAllowHeaders = default(StringValues);
                            return true;
                        }
                        return false;
                    }
                    if (HeaderNames.AccessControlAllowMethods.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x100000000L) != 0)
                        {
                            _bits &= ~0x100000000L;
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
                        if ((_bits & 0x400000000L) != 0)
                        {
                            _bits &= ~0x400000000L;
                            _headers._AccessControlExposeHeaders = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AccessControlExposeHeaders.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x400000000L) != 0)
                        {
                            _bits &= ~0x400000000L;
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
                        if ((_bits & 0x40000000L) != 0)
                        {
                            _bits &= ~0x40000000L;
                            _headers._AccessControlAllowCredentials = default(StringValues);
                            return true;
                        }
                        return false;
                    }
    
                    if (HeaderNames.AccessControlAllowCredentials.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((_bits & 0x40000000L) != 0)
                        {
                            _bits &= ~0x40000000L;
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
                _headers._Date = default;
                if((tempBits & ~0x4L) == 0)
                {
                    return;
                }
                tempBits &= ~0x4L;
            }
            
            if ((tempBits & 0x800L) != 0)
            {
                _headers._ContentType = default;
                if((tempBits & ~0x800L) == 0)
                {
                    return;
                }
                tempBits &= ~0x800L;
            }
            
            if ((tempBits & 0x4000000L) != 0)
            {
                _headers._Server = default;
                if((tempBits & ~0x4000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x4000000L;
            }
            
            if ((tempBits & 0x1L) != 0)
            {
                _headers._CacheControl = default;
                if((tempBits & ~0x1L) == 0)
                {
                    return;
                }
                tempBits &= ~0x1L;
            }
            
            if ((tempBits & 0x8L) != 0)
            {
                _headers._KeepAlive = default;
                if((tempBits & ~0x8L) == 0)
                {
                    return;
                }
                tempBits &= ~0x8L;
            }
            
            if ((tempBits & 0x10L) != 0)
            {
                _headers._Pragma = default;
                if((tempBits & ~0x10L) == 0)
                {
                    return;
                }
                tempBits &= ~0x10L;
            }
            
            if ((tempBits & 0x20L) != 0)
            {
                _headers._Trailer = default;
                if((tempBits & ~0x20L) == 0)
                {
                    return;
                }
                tempBits &= ~0x20L;
            }
            
            if ((tempBits & 0x40L) != 0)
            {
                _headers._TransferEncoding = default;
                if((tempBits & ~0x40L) == 0)
                {
                    return;
                }
                tempBits &= ~0x40L;
            }
            
            if ((tempBits & 0x80L) != 0)
            {
                _headers._Upgrade = default;
                if((tempBits & ~0x80L) == 0)
                {
                    return;
                }
                tempBits &= ~0x80L;
            }
            
            if ((tempBits & 0x100L) != 0)
            {
                _headers._Via = default;
                if((tempBits & ~0x100L) == 0)
                {
                    return;
                }
                tempBits &= ~0x100L;
            }
            
            if ((tempBits & 0x200L) != 0)
            {
                _headers._Warning = default;
                if((tempBits & ~0x200L) == 0)
                {
                    return;
                }
                tempBits &= ~0x200L;
            }
            
            if ((tempBits & 0x400L) != 0)
            {
                _headers._Allow = default;
                if((tempBits & ~0x400L) == 0)
                {
                    return;
                }
                tempBits &= ~0x400L;
            }
            
            if ((tempBits & 0x1000L) != 0)
            {
                _headers._ContentEncoding = default;
                if((tempBits & ~0x1000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x1000L;
            }
            
            if ((tempBits & 0x2000L) != 0)
            {
                _headers._ContentLanguage = default;
                if((tempBits & ~0x2000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x2000L;
            }
            
            if ((tempBits & 0x4000L) != 0)
            {
                _headers._ContentLocation = default;
                if((tempBits & ~0x4000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x4000L;
            }
            
            if ((tempBits & 0x8000L) != 0)
            {
                _headers._ContentMD5 = default;
                if((tempBits & ~0x8000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x8000L;
            }
            
            if ((tempBits & 0x10000L) != 0)
            {
                _headers._ContentRange = default;
                if((tempBits & ~0x10000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x10000L;
            }
            
            if ((tempBits & 0x20000L) != 0)
            {
                _headers._Expires = default;
                if((tempBits & ~0x20000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x20000L;
            }
            
            if ((tempBits & 0x40000L) != 0)
            {
                _headers._LastModified = default;
                if((tempBits & ~0x40000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x40000L;
            }
            
            if ((tempBits & 0x80000L) != 0)
            {
                _headers._AcceptRanges = default;
                if((tempBits & ~0x80000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x80000L;
            }
            
            if ((tempBits & 0x100000L) != 0)
            {
                _headers._Age = default;
                if((tempBits & ~0x100000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x100000L;
            }
            
            if ((tempBits & 0x200000L) != 0)
            {
                _headers._AltSvc = default;
                if((tempBits & ~0x200000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x200000L;
            }
            
            if ((tempBits & 0x400000L) != 0)
            {
                _headers._ETag = default;
                if((tempBits & ~0x400000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x400000L;
            }
            
            if ((tempBits & 0x800000L) != 0)
            {
                _headers._Location = default;
                if((tempBits & ~0x800000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x800000L;
            }
            
            if ((tempBits & 0x1000000L) != 0)
            {
                _headers._ProxyAuthenticate = default;
                if((tempBits & ~0x1000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x1000000L;
            }
            
            if ((tempBits & 0x2000000L) != 0)
            {
                _headers._RetryAfter = default;
                if((tempBits & ~0x2000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x2000000L;
            }
            
            if ((tempBits & 0x8000000L) != 0)
            {
                _headers._SetCookie = default;
                if((tempBits & ~0x8000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x8000000L;
            }
            
            if ((tempBits & 0x10000000L) != 0)
            {
                _headers._Vary = default;
                if((tempBits & ~0x10000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x10000000L;
            }
            
            if ((tempBits & 0x20000000L) != 0)
            {
                _headers._WWWAuthenticate = default;
                if((tempBits & ~0x20000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x20000000L;
            }
            
            if ((tempBits & 0x40000000L) != 0)
            {
                _headers._AccessControlAllowCredentials = default;
                if((tempBits & ~0x40000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x40000000L;
            }
            
            if ((tempBits & 0x80000000L) != 0)
            {
                _headers._AccessControlAllowHeaders = default;
                if((tempBits & ~0x80000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x80000000L;
            }
            
            if ((tempBits & 0x100000000L) != 0)
            {
                _headers._AccessControlAllowMethods = default;
                if((tempBits & ~0x100000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x100000000L;
            }
            
            if ((tempBits & 0x200000000L) != 0)
            {
                _headers._AccessControlAllowOrigin = default;
                if((tempBits & ~0x200000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x200000000L;
            }
            
            if ((tempBits & 0x400000000L) != 0)
            {
                _headers._AccessControlExposeHeaders = default;
                if((tempBits & ~0x400000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x400000000L;
            }
            
            if ((tempBits & 0x800000000L) != 0)
            {
                _headers._AccessControlMaxAge = default;
                if((tempBits & ~0x800000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x800000000L;
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
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.CacheControl, _headers._CacheControl);
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
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Date, _headers._Date);
                    ++arrayIndex;
                }
                if ((_bits & 0x8L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.KeepAlive, _headers._KeepAlive);
                    ++arrayIndex;
                }
                if ((_bits & 0x10L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Pragma, _headers._Pragma);
                    ++arrayIndex;
                }
                if ((_bits & 0x20L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Trailer, _headers._Trailer);
                    ++arrayIndex;
                }
                if ((_bits & 0x40L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.TransferEncoding, _headers._TransferEncoding);
                    ++arrayIndex;
                }
                if ((_bits & 0x80L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Upgrade, _headers._Upgrade);
                    ++arrayIndex;
                }
                if ((_bits & 0x100L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Via, _headers._Via);
                    ++arrayIndex;
                }
                if ((_bits & 0x200L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Warning, _headers._Warning);
                    ++arrayIndex;
                }
                if ((_bits & 0x400L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Allow, _headers._Allow);
                    ++arrayIndex;
                }
                if ((_bits & 0x800L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ContentType, _headers._ContentType);
                    ++arrayIndex;
                }
                if ((_bits & 0x1000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ContentEncoding, _headers._ContentEncoding);
                    ++arrayIndex;
                }
                if ((_bits & 0x2000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ContentLanguage, _headers._ContentLanguage);
                    ++arrayIndex;
                }
                if ((_bits & 0x4000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ContentLocation, _headers._ContentLocation);
                    ++arrayIndex;
                }
                if ((_bits & 0x8000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ContentMD5, _headers._ContentMD5);
                    ++arrayIndex;
                }
                if ((_bits & 0x10000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ContentRange, _headers._ContentRange);
                    ++arrayIndex;
                }
                if ((_bits & 0x20000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Expires, _headers._Expires);
                    ++arrayIndex;
                }
                if ((_bits & 0x40000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.LastModified, _headers._LastModified);
                    ++arrayIndex;
                }
                if ((_bits & 0x80000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.AcceptRanges, _headers._AcceptRanges);
                    ++arrayIndex;
                }
                if ((_bits & 0x100000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Age, _headers._Age);
                    ++arrayIndex;
                }
                if ((_bits & 0x200000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.AltSvc, _headers._AltSvc);
                    ++arrayIndex;
                }
                if ((_bits & 0x400000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ETag, _headers._ETag);
                    ++arrayIndex;
                }
                if ((_bits & 0x800000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Location, _headers._Location);
                    ++arrayIndex;
                }
                if ((_bits & 0x1000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ProxyAuthenticate, _headers._ProxyAuthenticate);
                    ++arrayIndex;
                }
                if ((_bits & 0x2000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.RetryAfter, _headers._RetryAfter);
                    ++arrayIndex;
                }
                if ((_bits & 0x4000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Server, _headers._Server);
                    ++arrayIndex;
                }
                if ((_bits & 0x8000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.SetCookie, _headers._SetCookie);
                    ++arrayIndex;
                }
                if ((_bits & 0x10000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.Vary, _headers._Vary);
                    ++arrayIndex;
                }
                if ((_bits & 0x20000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.WWWAuthenticate, _headers._WWWAuthenticate);
                    ++arrayIndex;
                }
                if ((_bits & 0x40000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlAllowCredentials, _headers._AccessControlAllowCredentials);
                    ++arrayIndex;
                }
                if ((_bits & 0x80000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlAllowHeaders, _headers._AccessControlAllowHeaders);
                    ++arrayIndex;
                }
                if ((_bits & 0x100000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlAllowMethods, _headers._AccessControlAllowMethods);
                    ++arrayIndex;
                }
                if ((_bits & 0x200000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlAllowOrigin, _headers._AccessControlAllowOrigin);
                    ++arrayIndex;
                }
                if ((_bits & 0x400000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlExposeHeaders, _headers._AccessControlExposeHeaders);
                    ++arrayIndex;
                }
                if ((_bits & 0x800000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlMaxAge, _headers._AccessControlMaxAge);
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
            ((ICollection<KeyValuePair<string, StringValues>>)MaybeUnknown)?.CopyTo(array, arrayIndex);

            return true;
        }
        
        internal unsafe void CopyToFast(ref BufferWriter<PipeWriter> output)
        {
            var tempBits = (ulong)_bits | (_contentLength.HasValue ? 0x8000000000000000L : 0);
            var next = 0;
            var keyStart = 0;
            var keyLength = 0;
            ref readonly StringValues values = ref Unsafe.AsRef<StringValues>(null);

            do
            {
                switch (next)
                {
                    case 0: // Header: "Connection"
                        if ((tempBits & 0x2L) != 0)
                        {
                            tempBits ^= 0x2L;
                            if (_headers._rawConnection != null)
                            {
                                output.Write(_headers._rawConnection);
                            }
                            else
                            {
                                values = ref _headers._Connection;
                                keyStart = 17;
                                keyLength = 14;
                                next = 1;
                                break; // OutputHeader
                            }
                        }
                        goto case 1;
                    case 1: // Header: "Date"
                        if ((tempBits & 0x4L) != 0)
                        {
                            tempBits ^= 0x4L;
                            if (_headers._rawDate != null)
                            {
                                output.Write(_headers._rawDate);
                            }
                            else
                            {
                                values = ref _headers._Date;
                                keyStart = 31;
                                keyLength = 8;
                                next = 2;
                                break; // OutputHeader
                            }
                        }
                        goto case 2;
                    case 2: // Header: "Content-Type"
                        if ((tempBits & 0x800L) != 0)
                        {
                            tempBits ^= 0x800L;
                            values = ref _headers._ContentType;
                            keyStart = 133;
                            keyLength = 16;
                            next = 3;
                            break; // OutputHeader
                        }
                        goto case 3;
                    case 3: // Header: "Server"
                        if ((tempBits & 0x4000000L) != 0)
                        {
                            tempBits ^= 0x4000000L;
                            if (_headers._rawServer != null)
                            {
                                output.Write(_headers._rawServer);
                            }
                            else
                            {
                                values = ref _headers._Server;
                                keyStart = 361;
                                keyLength = 10;
                                next = 4;
                                break; // OutputHeader
                            }
                        }
                        goto case 4;
                    case 4: // Header: "Content-Length"
                        if ((tempBits & 0x8000000000000000L) != 0)
                        {
                            tempBits ^= 0x8000000000000000L;
                            output.Write(HeaderBytes.Slice(603, 18));
                            output.WriteNumeric((ulong)ContentLength.Value);
                            if (tempBits == 0)
                            {
                                return;
                            }
                        }
                        goto case 5;
                    case 5: // Header: "Cache-Control"
                        if ((tempBits & 0x1L) != 0)
                        {
                            tempBits ^= 0x1L;
                            values = ref _headers._CacheControl;
                            keyStart = 0;
                            keyLength = 17;
                            next = 6;
                            break; // OutputHeader
                        }
                        goto case 6;
                    case 6: // Header: "Keep-Alive"
                        if ((tempBits & 0x8L) != 0)
                        {
                            tempBits ^= 0x8L;
                            values = ref _headers._KeepAlive;
                            keyStart = 39;
                            keyLength = 14;
                            next = 7;
                            break; // OutputHeader
                        }
                        goto case 7;
                    case 7: // Header: "Pragma"
                        if ((tempBits & 0x10L) != 0)
                        {
                            tempBits ^= 0x10L;
                            values = ref _headers._Pragma;
                            keyStart = 53;
                            keyLength = 10;
                            next = 8;
                            break; // OutputHeader
                        }
                        goto case 8;
                    case 8: // Header: "Trailer"
                        if ((tempBits & 0x20L) != 0)
                        {
                            tempBits ^= 0x20L;
                            values = ref _headers._Trailer;
                            keyStart = 63;
                            keyLength = 11;
                            next = 9;
                            break; // OutputHeader
                        }
                        goto case 9;
                    case 9: // Header: "Transfer-Encoding"
                        if ((tempBits & 0x40L) != 0)
                        {
                            tempBits ^= 0x40L;
                            if (_headers._rawTransferEncoding != null)
                            {
                                output.Write(_headers._rawTransferEncoding);
                            }
                            else
                            {
                                values = ref _headers._TransferEncoding;
                                keyStart = 74;
                                keyLength = 21;
                                next = 10;
                                break; // OutputHeader
                            }
                        }
                        goto case 10;
                    case 10: // Header: "Upgrade"
                        if ((tempBits & 0x80L) != 0)
                        {
                            tempBits ^= 0x80L;
                            values = ref _headers._Upgrade;
                            keyStart = 95;
                            keyLength = 11;
                            next = 11;
                            break; // OutputHeader
                        }
                        goto case 11;
                    case 11: // Header: "Via"
                        if ((tempBits & 0x100L) != 0)
                        {
                            tempBits ^= 0x100L;
                            values = ref _headers._Via;
                            keyStart = 106;
                            keyLength = 7;
                            next = 12;
                            break; // OutputHeader
                        }
                        goto case 12;
                    case 12: // Header: "Warning"
                        if ((tempBits & 0x200L) != 0)
                        {
                            tempBits ^= 0x200L;
                            values = ref _headers._Warning;
                            keyStart = 113;
                            keyLength = 11;
                            next = 13;
                            break; // OutputHeader
                        }
                        goto case 13;
                    case 13: // Header: "Allow"
                        if ((tempBits & 0x400L) != 0)
                        {
                            tempBits ^= 0x400L;
                            values = ref _headers._Allow;
                            keyStart = 124;
                            keyLength = 9;
                            next = 14;
                            break; // OutputHeader
                        }
                        goto case 14;
                    case 14: // Header: "Content-Encoding"
                        if ((tempBits & 0x1000L) != 0)
                        {
                            tempBits ^= 0x1000L;
                            values = ref _headers._ContentEncoding;
                            keyStart = 149;
                            keyLength = 20;
                            next = 15;
                            break; // OutputHeader
                        }
                        goto case 15;
                    case 15: // Header: "Content-Language"
                        if ((tempBits & 0x2000L) != 0)
                        {
                            tempBits ^= 0x2000L;
                            values = ref _headers._ContentLanguage;
                            keyStart = 169;
                            keyLength = 20;
                            next = 16;
                            break; // OutputHeader
                        }
                        goto case 16;
                    case 16: // Header: "Content-Location"
                        if ((tempBits & 0x4000L) != 0)
                        {
                            tempBits ^= 0x4000L;
                            values = ref _headers._ContentLocation;
                            keyStart = 189;
                            keyLength = 20;
                            next = 17;
                            break; // OutputHeader
                        }
                        goto case 17;
                    case 17: // Header: "Content-MD5"
                        if ((tempBits & 0x8000L) != 0)
                        {
                            tempBits ^= 0x8000L;
                            values = ref _headers._ContentMD5;
                            keyStart = 209;
                            keyLength = 15;
                            next = 18;
                            break; // OutputHeader
                        }
                        goto case 18;
                    case 18: // Header: "Content-Range"
                        if ((tempBits & 0x10000L) != 0)
                        {
                            tempBits ^= 0x10000L;
                            values = ref _headers._ContentRange;
                            keyStart = 224;
                            keyLength = 17;
                            next = 19;
                            break; // OutputHeader
                        }
                        goto case 19;
                    case 19: // Header: "Expires"
                        if ((tempBits & 0x20000L) != 0)
                        {
                            tempBits ^= 0x20000L;
                            values = ref _headers._Expires;
                            keyStart = 241;
                            keyLength = 11;
                            next = 20;
                            break; // OutputHeader
                        }
                        goto case 20;
                    case 20: // Header: "Last-Modified"
                        if ((tempBits & 0x40000L) != 0)
                        {
                            tempBits ^= 0x40000L;
                            values = ref _headers._LastModified;
                            keyStart = 252;
                            keyLength = 17;
                            next = 21;
                            break; // OutputHeader
                        }
                        goto case 21;
                    case 21: // Header: "Accept-Ranges"
                        if ((tempBits & 0x80000L) != 0)
                        {
                            tempBits ^= 0x80000L;
                            values = ref _headers._AcceptRanges;
                            keyStart = 269;
                            keyLength = 17;
                            next = 22;
                            break; // OutputHeader
                        }
                        goto case 22;
                    case 22: // Header: "Age"
                        if ((tempBits & 0x100000L) != 0)
                        {
                            tempBits ^= 0x100000L;
                            values = ref _headers._Age;
                            keyStart = 286;
                            keyLength = 7;
                            next = 23;
                            break; // OutputHeader
                        }
                        goto case 23;
                    case 23: // Header: "Alt-Svc"
                        if ((tempBits & 0x200000L) != 0)
                        {
                            tempBits ^= 0x200000L;
                            values = ref _headers._AltSvc;
                            keyStart = 293;
                            keyLength = 11;
                            next = 24;
                            break; // OutputHeader
                        }
                        goto case 24;
                    case 24: // Header: "ETag"
                        if ((tempBits & 0x400000L) != 0)
                        {
                            tempBits ^= 0x400000L;
                            values = ref _headers._ETag;
                            keyStart = 304;
                            keyLength = 8;
                            next = 25;
                            break; // OutputHeader
                        }
                        goto case 25;
                    case 25: // Header: "Location"
                        if ((tempBits & 0x800000L) != 0)
                        {
                            tempBits ^= 0x800000L;
                            values = ref _headers._Location;
                            keyStart = 312;
                            keyLength = 12;
                            next = 26;
                            break; // OutputHeader
                        }
                        goto case 26;
                    case 26: // Header: "Proxy-Authenticate"
                        if ((tempBits & 0x1000000L) != 0)
                        {
                            tempBits ^= 0x1000000L;
                            values = ref _headers._ProxyAuthenticate;
                            keyStart = 324;
                            keyLength = 22;
                            next = 27;
                            break; // OutputHeader
                        }
                        goto case 27;
                    case 27: // Header: "Retry-After"
                        if ((tempBits & 0x2000000L) != 0)
                        {
                            tempBits ^= 0x2000000L;
                            values = ref _headers._RetryAfter;
                            keyStart = 346;
                            keyLength = 15;
                            next = 28;
                            break; // OutputHeader
                        }
                        goto case 28;
                    case 28: // Header: "Set-Cookie"
                        if ((tempBits & 0x8000000L) != 0)
                        {
                            tempBits ^= 0x8000000L;
                            values = ref _headers._SetCookie;
                            keyStart = 371;
                            keyLength = 14;
                            next = 29;
                            break; // OutputHeader
                        }
                        goto case 29;
                    case 29: // Header: "Vary"
                        if ((tempBits & 0x10000000L) != 0)
                        {
                            tempBits ^= 0x10000000L;
                            values = ref _headers._Vary;
                            keyStart = 385;
                            keyLength = 8;
                            next = 30;
                            break; // OutputHeader
                        }
                        goto case 30;
                    case 30: // Header: "WWW-Authenticate"
                        if ((tempBits & 0x20000000L) != 0)
                        {
                            tempBits ^= 0x20000000L;
                            values = ref _headers._WWWAuthenticate;
                            keyStart = 393;
                            keyLength = 20;
                            next = 31;
                            break; // OutputHeader
                        }
                        goto case 31;
                    case 31: // Header: "Access-Control-Allow-Credentials"
                        if ((tempBits & 0x40000000L) != 0)
                        {
                            tempBits ^= 0x40000000L;
                            values = ref _headers._AccessControlAllowCredentials;
                            keyStart = 413;
                            keyLength = 36;
                            next = 32;
                            break; // OutputHeader
                        }
                        goto case 32;
                    case 32: // Header: "Access-Control-Allow-Headers"
                        if ((tempBits & 0x80000000L) != 0)
                        {
                            tempBits ^= 0x80000000L;
                            values = ref _headers._AccessControlAllowHeaders;
                            keyStart = 449;
                            keyLength = 32;
                            next = 33;
                            break; // OutputHeader
                        }
                        goto case 33;
                    case 33: // Header: "Access-Control-Allow-Methods"
                        if ((tempBits & 0x100000000L) != 0)
                        {
                            tempBits ^= 0x100000000L;
                            values = ref _headers._AccessControlAllowMethods;
                            keyStart = 481;
                            keyLength = 32;
                            next = 34;
                            break; // OutputHeader
                        }
                        goto case 34;
                    case 34: // Header: "Access-Control-Allow-Origin"
                        if ((tempBits & 0x200000000L) != 0)
                        {
                            tempBits ^= 0x200000000L;
                            values = ref _headers._AccessControlAllowOrigin;
                            keyStart = 513;
                            keyLength = 31;
                            next = 35;
                            break; // OutputHeader
                        }
                        goto case 35;
                    case 35: // Header: "Access-Control-Expose-Headers"
                        if ((tempBits & 0x400000000L) != 0)
                        {
                            tempBits ^= 0x400000000L;
                            values = ref _headers._AccessControlExposeHeaders;
                            keyStart = 544;
                            keyLength = 33;
                            next = 36;
                            break; // OutputHeader
                        }
                        goto case 36;
                    case 36: // Header: "Access-Control-Max-Age"
                        if ((tempBits & 0x800000000L) != 0)
                        {
                            tempBits ^= 0x800000000L;
                            values = ref _headers._AccessControlMaxAge;
                            keyStart = 577;
                            keyLength = 26;
                            next = 37;
                            break; // OutputHeader
                        }
                        return;
                    default:
                        return;
                }

                // OutputHeader
                {
                    var valueCount = values.Count;
                    var headerKey = HeaderBytes.Slice(keyStart, keyLength);
                    for (var i = 0; i < valueCount; i++)
                    {
                        var value = values[i];
                        if (value != null)
                        {
                            output.Write(headerKey);
                            output.WriteAscii(value);
                        }
                    }
                }
            } while (tempBits != 0);
        }

        private struct HeaderReferences
        {
            public StringValues _CacheControl;
            public StringValues _Connection;
            public StringValues _Date;
            public StringValues _KeepAlive;
            public StringValues _Pragma;
            public StringValues _Trailer;
            public StringValues _TransferEncoding;
            public StringValues _Upgrade;
            public StringValues _Via;
            public StringValues _Warning;
            public StringValues _Allow;
            public StringValues _ContentType;
            public StringValues _ContentEncoding;
            public StringValues _ContentLanguage;
            public StringValues _ContentLocation;
            public StringValues _ContentMD5;
            public StringValues _ContentRange;
            public StringValues _Expires;
            public StringValues _LastModified;
            public StringValues _AcceptRanges;
            public StringValues _Age;
            public StringValues _AltSvc;
            public StringValues _ETag;
            public StringValues _Location;
            public StringValues _ProxyAuthenticate;
            public StringValues _RetryAfter;
            public StringValues _Server;
            public StringValues _SetCookie;
            public StringValues _Vary;
            public StringValues _WWWAuthenticate;
            public StringValues _AccessControlAllowCredentials;
            public StringValues _AccessControlAllowHeaders;
            public StringValues _AccessControlAllowMethods;
            public StringValues _AccessControlAllowOrigin;
            public StringValues _AccessControlExposeHeaders;
            public StringValues _AccessControlMaxAge;
            
            public byte[] _rawConnection;
            public byte[] _rawDate;
            public byte[] _rawTransferEncoding;
            public byte[] _rawServer;
        }

        public partial struct Enumerator
        {
            // Compiled to Jump table
            public bool MoveNext()
            {
                switch (_next)
                {
                    case 0:
                        goto HeaderCacheControl;
                    case 1:
                        goto HeaderConnection;
                    case 2:
                        goto HeaderDate;
                    case 3:
                        goto HeaderKeepAlive;
                    case 4:
                        goto HeaderPragma;
                    case 5:
                        goto HeaderTrailer;
                    case 6:
                        goto HeaderTransferEncoding;
                    case 7:
                        goto HeaderUpgrade;
                    case 8:
                        goto HeaderVia;
                    case 9:
                        goto HeaderWarning;
                    case 10:
                        goto HeaderAllow;
                    case 11:
                        goto HeaderContentType;
                    case 12:
                        goto HeaderContentEncoding;
                    case 13:
                        goto HeaderContentLanguage;
                    case 14:
                        goto HeaderContentLocation;
                    case 15:
                        goto HeaderContentMD5;
                    case 16:
                        goto HeaderContentRange;
                    case 17:
                        goto HeaderExpires;
                    case 18:
                        goto HeaderLastModified;
                    case 19:
                        goto HeaderAcceptRanges;
                    case 20:
                        goto HeaderAge;
                    case 21:
                        goto HeaderAltSvc;
                    case 22:
                        goto HeaderETag;
                    case 23:
                        goto HeaderLocation;
                    case 24:
                        goto HeaderProxyAuthenticate;
                    case 25:
                        goto HeaderRetryAfter;
                    case 26:
                        goto HeaderServer;
                    case 27:
                        goto HeaderSetCookie;
                    case 28:
                        goto HeaderVary;
                    case 29:
                        goto HeaderWWWAuthenticate;
                    case 30:
                        goto HeaderAccessControlAllowCredentials;
                    case 31:
                        goto HeaderAccessControlAllowHeaders;
                    case 32:
                        goto HeaderAccessControlAllowMethods;
                    case 33:
                        goto HeaderAccessControlAllowOrigin;
                    case 34:
                        goto HeaderAccessControlExposeHeaders;
                    case 35:
                        goto HeaderAccessControlMaxAge;
                    case 36:
                        goto HeaderContentLength;
                    default:
                        goto ExtraHeaders;
                }
                
                HeaderCacheControl: // case 0
                    if ((_bits & 0x1L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.CacheControl, _collection._headers._CacheControl);
                        _currentKnownType = KnownHeaderType.CacheControl;
                        _next = 1;
                        return true;
                    }
                HeaderConnection: // case 1
                    if ((_bits & 0x2L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Connection, _collection._headers._Connection);
                        _currentKnownType = KnownHeaderType.Connection;
                        _next = 2;
                        return true;
                    }
                HeaderDate: // case 2
                    if ((_bits & 0x4L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Date, _collection._headers._Date);
                        _currentKnownType = KnownHeaderType.Date;
                        _next = 3;
                        return true;
                    }
                HeaderKeepAlive: // case 3
                    if ((_bits & 0x8L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.KeepAlive, _collection._headers._KeepAlive);
                        _currentKnownType = KnownHeaderType.KeepAlive;
                        _next = 4;
                        return true;
                    }
                HeaderPragma: // case 4
                    if ((_bits & 0x10L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Pragma, _collection._headers._Pragma);
                        _currentKnownType = KnownHeaderType.Pragma;
                        _next = 5;
                        return true;
                    }
                HeaderTrailer: // case 5
                    if ((_bits & 0x20L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Trailer, _collection._headers._Trailer);
                        _currentKnownType = KnownHeaderType.Trailer;
                        _next = 6;
                        return true;
                    }
                HeaderTransferEncoding: // case 6
                    if ((_bits & 0x40L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.TransferEncoding, _collection._headers._TransferEncoding);
                        _currentKnownType = KnownHeaderType.TransferEncoding;
                        _next = 7;
                        return true;
                    }
                HeaderUpgrade: // case 7
                    if ((_bits & 0x80L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Upgrade, _collection._headers._Upgrade);
                        _currentKnownType = KnownHeaderType.Upgrade;
                        _next = 8;
                        return true;
                    }
                HeaderVia: // case 8
                    if ((_bits & 0x100L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Via, _collection._headers._Via);
                        _currentKnownType = KnownHeaderType.Via;
                        _next = 9;
                        return true;
                    }
                HeaderWarning: // case 9
                    if ((_bits & 0x200L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Warning, _collection._headers._Warning);
                        _currentKnownType = KnownHeaderType.Warning;
                        _next = 10;
                        return true;
                    }
                HeaderAllow: // case 10
                    if ((_bits & 0x400L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Allow, _collection._headers._Allow);
                        _currentKnownType = KnownHeaderType.Allow;
                        _next = 11;
                        return true;
                    }
                HeaderContentType: // case 11
                    if ((_bits & 0x800L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ContentType, _collection._headers._ContentType);
                        _currentKnownType = KnownHeaderType.ContentType;
                        _next = 12;
                        return true;
                    }
                HeaderContentEncoding: // case 12
                    if ((_bits & 0x1000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ContentEncoding, _collection._headers._ContentEncoding);
                        _currentKnownType = KnownHeaderType.ContentEncoding;
                        _next = 13;
                        return true;
                    }
                HeaderContentLanguage: // case 13
                    if ((_bits & 0x2000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ContentLanguage, _collection._headers._ContentLanguage);
                        _currentKnownType = KnownHeaderType.ContentLanguage;
                        _next = 14;
                        return true;
                    }
                HeaderContentLocation: // case 14
                    if ((_bits & 0x4000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ContentLocation, _collection._headers._ContentLocation);
                        _currentKnownType = KnownHeaderType.ContentLocation;
                        _next = 15;
                        return true;
                    }
                HeaderContentMD5: // case 15
                    if ((_bits & 0x8000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ContentMD5, _collection._headers._ContentMD5);
                        _currentKnownType = KnownHeaderType.ContentMD5;
                        _next = 16;
                        return true;
                    }
                HeaderContentRange: // case 16
                    if ((_bits & 0x10000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ContentRange, _collection._headers._ContentRange);
                        _currentKnownType = KnownHeaderType.ContentRange;
                        _next = 17;
                        return true;
                    }
                HeaderExpires: // case 17
                    if ((_bits & 0x20000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Expires, _collection._headers._Expires);
                        _currentKnownType = KnownHeaderType.Expires;
                        _next = 18;
                        return true;
                    }
                HeaderLastModified: // case 18
                    if ((_bits & 0x40000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.LastModified, _collection._headers._LastModified);
                        _currentKnownType = KnownHeaderType.LastModified;
                        _next = 19;
                        return true;
                    }
                HeaderAcceptRanges: // case 19
                    if ((_bits & 0x80000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.AcceptRanges, _collection._headers._AcceptRanges);
                        _currentKnownType = KnownHeaderType.AcceptRanges;
                        _next = 20;
                        return true;
                    }
                HeaderAge: // case 20
                    if ((_bits & 0x100000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Age, _collection._headers._Age);
                        _currentKnownType = KnownHeaderType.Age;
                        _next = 21;
                        return true;
                    }
                HeaderAltSvc: // case 21
                    if ((_bits & 0x200000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.AltSvc, _collection._headers._AltSvc);
                        _currentKnownType = KnownHeaderType.AltSvc;
                        _next = 22;
                        return true;
                    }
                HeaderETag: // case 22
                    if ((_bits & 0x400000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ETag, _collection._headers._ETag);
                        _currentKnownType = KnownHeaderType.ETag;
                        _next = 23;
                        return true;
                    }
                HeaderLocation: // case 23
                    if ((_bits & 0x800000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Location, _collection._headers._Location);
                        _currentKnownType = KnownHeaderType.Location;
                        _next = 24;
                        return true;
                    }
                HeaderProxyAuthenticate: // case 24
                    if ((_bits & 0x1000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ProxyAuthenticate, _collection._headers._ProxyAuthenticate);
                        _currentKnownType = KnownHeaderType.ProxyAuthenticate;
                        _next = 25;
                        return true;
                    }
                HeaderRetryAfter: // case 25
                    if ((_bits & 0x2000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.RetryAfter, _collection._headers._RetryAfter);
                        _currentKnownType = KnownHeaderType.RetryAfter;
                        _next = 26;
                        return true;
                    }
                HeaderServer: // case 26
                    if ((_bits & 0x4000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Server, _collection._headers._Server);
                        _currentKnownType = KnownHeaderType.Server;
                        _next = 27;
                        return true;
                    }
                HeaderSetCookie: // case 27
                    if ((_bits & 0x8000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.SetCookie, _collection._headers._SetCookie);
                        _currentKnownType = KnownHeaderType.SetCookie;
                        _next = 28;
                        return true;
                    }
                HeaderVary: // case 28
                    if ((_bits & 0x10000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.Vary, _collection._headers._Vary);
                        _currentKnownType = KnownHeaderType.Vary;
                        _next = 29;
                        return true;
                    }
                HeaderWWWAuthenticate: // case 29
                    if ((_bits & 0x20000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.WWWAuthenticate, _collection._headers._WWWAuthenticate);
                        _currentKnownType = KnownHeaderType.WWWAuthenticate;
                        _next = 30;
                        return true;
                    }
                HeaderAccessControlAllowCredentials: // case 30
                    if ((_bits & 0x40000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlAllowCredentials, _collection._headers._AccessControlAllowCredentials);
                        _currentKnownType = KnownHeaderType.AccessControlAllowCredentials;
                        _next = 31;
                        return true;
                    }
                HeaderAccessControlAllowHeaders: // case 31
                    if ((_bits & 0x80000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlAllowHeaders, _collection._headers._AccessControlAllowHeaders);
                        _currentKnownType = KnownHeaderType.AccessControlAllowHeaders;
                        _next = 32;
                        return true;
                    }
                HeaderAccessControlAllowMethods: // case 32
                    if ((_bits & 0x100000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlAllowMethods, _collection._headers._AccessControlAllowMethods);
                        _currentKnownType = KnownHeaderType.AccessControlAllowMethods;
                        _next = 33;
                        return true;
                    }
                HeaderAccessControlAllowOrigin: // case 33
                    if ((_bits & 0x200000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlAllowOrigin, _collection._headers._AccessControlAllowOrigin);
                        _currentKnownType = KnownHeaderType.AccessControlAllowOrigin;
                        _next = 34;
                        return true;
                    }
                HeaderAccessControlExposeHeaders: // case 34
                    if ((_bits & 0x400000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlExposeHeaders, _collection._headers._AccessControlExposeHeaders);
                        _currentKnownType = KnownHeaderType.AccessControlExposeHeaders;
                        _next = 35;
                        return true;
                    }
                HeaderAccessControlMaxAge: // case 35
                    if ((_bits & 0x800000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.AccessControlMaxAge, _collection._headers._AccessControlMaxAge);
                        _currentKnownType = KnownHeaderType.AccessControlMaxAge;
                        _next = 36;
                        return true;
                    }
                HeaderContentLength: // case 36
                    if (_collection._contentLength.HasValue)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ContentLength, HeaderUtilities.FormatNonNegativeInt64(_collection._contentLength.Value));
                        _currentKnownType = KnownHeaderType.ContentLength;
                        _next = 37;
                        return true;
                    }
                ExtraHeaders:
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
        }
    }

    internal partial class HttpResponseTrailers
    {
        private static ReadOnlySpan<byte> HeaderBytes => new byte[]
        {
            13,10,69,84,97,103,58,32,
        };
        private HeaderReferences _headers;


        
        public StringValues HeaderETag
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x1L) != 0)
                {
                    value = _headers._ETag;
                }
                return value;
            }
            set
            {
                _bits |= 0x1L;
                _headers._ETag = value; 
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
            }

            return TryGetUnknown(key, ref value);
        }

        protected override void SetValueFast(string key, StringValues value)
        {
            ValidateHeaderValueCharacters(value);
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
            }

            SetValueUnknown(key, value);
        }

        protected override bool AddValueFast(string key, StringValues value)
        {
            ValidateHeaderValueCharacters(value);
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
                if (_contentLength.HasValue)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ContentLength, HeaderUtilities.FormatNonNegativeInt64(_contentLength.Value));
                    ++arrayIndex;
                }
            ((ICollection<KeyValuePair<string, StringValues>>)MaybeUnknown)?.CopyTo(array, arrayIndex);

            return true;
        }
        

        private struct HeaderReferences
        {
            public StringValues _ETag;
            
        }

        public partial struct Enumerator
        {
            // Compiled to Jump table
            public bool MoveNext()
            {
                switch (_next)
                {
                    case 0:
                        goto HeaderETag;
                    
                    default:
                        goto ExtraHeaders;
                }
                
                HeaderETag: // case 0
                    if ((_bits & 0x1L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ETag, _collection._headers._ETag);
                        _currentKnownType = KnownHeaderType.ETag;
                        _next = 1;
                        return true;
                    }
                
                ExtraHeaders:
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
        }
    }
}