// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{

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
        public StringValues HeaderAccept
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x80000L) != 0)
                {
                    value = _headers._Accept;
                }
                return value;
            }
            set
            {
                _bits |= 0x80000L;
                _headers._Accept = value; 
            }
        }
        public StringValues HeaderAcceptCharset
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x100000L) != 0)
                {
                    value = _headers._AcceptCharset;
                }
                return value;
            }
            set
            {
                _bits |= 0x100000L;
                _headers._AcceptCharset = value; 
            }
        }
        public StringValues HeaderAcceptEncoding
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x200000L) != 0)
                {
                    value = _headers._AcceptEncoding;
                }
                return value;
            }
            set
            {
                _bits |= 0x200000L;
                _headers._AcceptEncoding = value; 
            }
        }
        public StringValues HeaderAcceptLanguage
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x400000L) != 0)
                {
                    value = _headers._AcceptLanguage;
                }
                return value;
            }
            set
            {
                _bits |= 0x400000L;
                _headers._AcceptLanguage = value; 
            }
        }
        public StringValues HeaderAuthorization
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x800000L) != 0)
                {
                    value = _headers._Authorization;
                }
                return value;
            }
            set
            {
                _bits |= 0x800000L;
                _headers._Authorization = value; 
            }
        }
        public StringValues HeaderCookie
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x1000000L) != 0)
                {
                    value = _headers._Cookie;
                }
                return value;
            }
            set
            {
                _bits |= 0x1000000L;
                _headers._Cookie = value; 
            }
        }
        public StringValues HeaderExpect
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x2000000L) != 0)
                {
                    value = _headers._Expect;
                }
                return value;
            }
            set
            {
                _bits |= 0x2000000L;
                _headers._Expect = value; 
            }
        }
        public StringValues HeaderFrom
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x4000000L) != 0)
                {
                    value = _headers._From;
                }
                return value;
            }
            set
            {
                _bits |= 0x4000000L;
                _headers._From = value; 
            }
        }
        public StringValues HeaderHost
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x8000000L) != 0)
                {
                    value = _headers._Host;
                }
                return value;
            }
            set
            {
                _bits |= 0x8000000L;
                _headers._Host = value; 
            }
        }
        public StringValues HeaderIfMatch
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x10000000L) != 0)
                {
                    value = _headers._IfMatch;
                }
                return value;
            }
            set
            {
                _bits |= 0x10000000L;
                _headers._IfMatch = value; 
            }
        }
        public StringValues HeaderIfModifiedSince
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x20000000L) != 0)
                {
                    value = _headers._IfModifiedSince;
                }
                return value;
            }
            set
            {
                _bits |= 0x20000000L;
                _headers._IfModifiedSince = value; 
            }
        }
        public StringValues HeaderIfNoneMatch
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x40000000L) != 0)
                {
                    value = _headers._IfNoneMatch;
                }
                return value;
            }
            set
            {
                _bits |= 0x40000000L;
                _headers._IfNoneMatch = value; 
            }
        }
        public StringValues HeaderIfRange
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x80000000L) != 0)
                {
                    value = _headers._IfRange;
                }
                return value;
            }
            set
            {
                _bits |= 0x80000000L;
                _headers._IfRange = value; 
            }
        }
        public StringValues HeaderIfUnmodifiedSince
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x100000000L) != 0)
                {
                    value = _headers._IfUnmodifiedSince;
                }
                return value;
            }
            set
            {
                _bits |= 0x100000000L;
                _headers._IfUnmodifiedSince = value; 
            }
        }
        public StringValues HeaderMaxForwards
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x200000000L) != 0)
                {
                    value = _headers._MaxForwards;
                }
                return value;
            }
            set
            {
                _bits |= 0x200000000L;
                _headers._MaxForwards = value; 
            }
        }
        public StringValues HeaderProxyAuthorization
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x400000000L) != 0)
                {
                    value = _headers._ProxyAuthorization;
                }
                return value;
            }
            set
            {
                _bits |= 0x400000000L;
                _headers._ProxyAuthorization = value; 
            }
        }
        public StringValues HeaderReferer
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x800000000L) != 0)
                {
                    value = _headers._Referer;
                }
                return value;
            }
            set
            {
                _bits |= 0x800000000L;
                _headers._Referer = value; 
            }
        }
        public StringValues HeaderRange
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x1000000000L) != 0)
                {
                    value = _headers._Range;
                }
                return value;
            }
            set
            {
                _bits |= 0x1000000000L;
                _headers._Range = value; 
            }
        }
        public StringValues HeaderTE
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x2000000000L) != 0)
                {
                    value = _headers._TE;
                }
                return value;
            }
            set
            {
                _bits |= 0x2000000000L;
                _headers._TE = value; 
            }
        }
        public StringValues HeaderTranslate
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x4000000000L) != 0)
                {
                    value = _headers._Translate;
                }
                return value;
            }
            set
            {
                _bits |= 0x4000000000L;
                _headers._Translate = value; 
            }
        }
        public StringValues HeaderUserAgent
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x8000000000L) != 0)
                {
                    value = _headers._UserAgent;
                }
                return value;
            }
            set
            {
                _bits |= 0x8000000000L;
                _headers._UserAgent = value; 
            }
        }
        public StringValues HeaderDNT
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x10000000000L) != 0)
                {
                    value = _headers._DNT;
                }
                return value;
            }
            set
            {
                _bits |= 0x10000000000L;
                _headers._DNT = value; 
            }
        }
        public StringValues HeaderUpgradeInsecureRequests
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x20000000000L) != 0)
                {
                    value = _headers._UpgradeInsecureRequests;
                }
                return value;
            }
            set
            {
                _bits |= 0x20000000000L;
                _headers._UpgradeInsecureRequests = value; 
            }
        }
        public StringValues HeaderOrigin
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x40000000000L) != 0)
                {
                    value = _headers._Origin;
                }
                return value;
            }
            set
            {
                _bits |= 0x40000000000L;
                _headers._Origin = value; 
            }
        }
        public StringValues HeaderAccessControlRequestMethod
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x80000000000L) != 0)
                {
                    value = _headers._AccessControlRequestMethod;
                }
                return value;
            }
            set
            {
                _bits |= 0x80000000000L;
                _headers._AccessControlRequestMethod = value; 
            }
        }
        public StringValues HeaderAccessControlRequestHeaders
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x100000000000L) != 0)
                {
                    value = _headers._AccessControlRequestHeaders;
                }
                return value;
            }
            set
            {
                _bits |= 0x100000000000L;
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
            return (_contentLength.HasValue ? 1 : 0 ) + BitCount(_bits) + (MaybeUnknown?.Count ?? 0);
        }

        protected override bool TryGetValueFast(string key, out StringValues value)
        {
            value = default;
            switch (key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x1L) != 0)
                            {
                                value = _headers._CacheControl;
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x10000L) != 0)
                            {
                                value = _headers._ContentRange;
                                return true;
                            }
                            return false;
                        }
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x40000L) != 0)
                            {
                                value = _headers._LastModified;
                                return true;
                            }
                            return false;
                        }
                        if ("Authorization".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x800000L) != 0)
                            {
                                value = _headers._Authorization;
                                return true;
                            }
                            return false;
                        }
                        if ("If-None-Match".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x40000000L) != 0)
                            {
                                value = _headers._IfNoneMatch;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 10:
                    {
                        if ("Connection".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x2L) != 0)
                            {
                                value = _headers._Connection;
                                return true;
                            }
                            return false;
                        }
                        if ("Keep-Alive".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x8L) != 0)
                            {
                                value = _headers._KeepAlive;
                                return true;
                            }
                            return false;
                        }
                        if ("User-Agent".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x8000000000L) != 0)
                            {
                                value = _headers._UserAgent;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 4:
                    {
                        if ("Date".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x4L) != 0)
                            {
                                value = _headers._Date;
                                return true;
                            }
                            return false;
                        }
                        if ("From".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x4000000L) != 0)
                            {
                                value = _headers._From;
                                return true;
                            }
                            return false;
                        }
                        if ("Host".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x8000000L) != 0)
                            {
                                value = _headers._Host;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 6:
                    {
                        if ("Pragma".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x10L) != 0)
                            {
                                value = _headers._Pragma;
                                return true;
                            }
                            return false;
                        }
                        if ("Accept".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x80000L) != 0)
                            {
                                value = _headers._Accept;
                                return true;
                            }
                            return false;
                        }
                        if ("Cookie".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x1000000L) != 0)
                            {
                                value = _headers._Cookie;
                                return true;
                            }
                            return false;
                        }
                        if ("Expect".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x2000000L) != 0)
                            {
                                value = _headers._Expect;
                                return true;
                            }
                            return false;
                        }
                        if ("Origin".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x40000000000L) != 0)
                            {
                                value = _headers._Origin;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 7:
                    {
                        if ("Trailer".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x20L) != 0)
                            {
                                value = _headers._Trailer;
                                return true;
                            }
                            return false;
                        }
                        if ("Upgrade".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x80L) != 0)
                            {
                                value = _headers._Upgrade;
                                return true;
                            }
                            return false;
                        }
                        if ("Warning".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x200L) != 0)
                            {
                                value = _headers._Warning;
                                return true;
                            }
                            return false;
                        }
                        if ("Expires".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x20000L) != 0)
                            {
                                value = _headers._Expires;
                                return true;
                            }
                            return false;
                        }
                        if ("Referer".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x800000000L) != 0)
                            {
                                value = _headers._Referer;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 17:
                    {
                        if ("Transfer-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x40L) != 0)
                            {
                                value = _headers._TransferEncoding;
                                return true;
                            }
                            return false;
                        }
                        if ("If-Modified-Since".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x20000000L) != 0)
                            {
                                value = _headers._IfModifiedSince;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 3:
                    {
                        if ("Via".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x100L) != 0)
                            {
                                value = _headers._Via;
                                return true;
                            }
                            return false;
                        }
                        if ("DNT".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x10000000000L) != 0)
                            {
                                value = _headers._DNT;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 5:
                    {
                        if ("Allow".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x400L) != 0)
                            {
                                value = _headers._Allow;
                                return true;
                            }
                            return false;
                        }
                        if ("Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x1000000000L) != 0)
                            {
                                value = _headers._Range;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 12:
                    {
                        if ("Content-Type".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x800L) != 0)
                            {
                                value = _headers._ContentType;
                                return true;
                            }
                            return false;
                        }
                        if ("Max-Forwards".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x200000000L) != 0)
                            {
                                value = _headers._MaxForwards;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 16:
                    {
                        if ("Content-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x1000L) != 0)
                            {
                                value = _headers._ContentEncoding;
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x2000L) != 0)
                            {
                                value = _headers._ContentLanguage;
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x4000L) != 0)
                            {
                                value = _headers._ContentLocation;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 11:
                    {
                        if ("Content-MD5".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x8000L) != 0)
                            {
                                value = _headers._ContentMD5;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 14:
                    {
                        if ("Accept-Charset".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x100000L) != 0)
                            {
                                value = _headers._AcceptCharset;
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Length".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (_contentLength.HasValue)
                            {
                                value = HeaderUtilities.FormatNonNegativeInt64(_contentLength.Value);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 15:
                    {
                        if ("Accept-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x200000L) != 0)
                            {
                                value = _headers._AcceptEncoding;
                                return true;
                            }
                            return false;
                        }
                        if ("Accept-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x400000L) != 0)
                            {
                                value = _headers._AcceptLanguage;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 8:
                    {
                        if ("If-Match".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x10000000L) != 0)
                            {
                                value = _headers._IfMatch;
                                return true;
                            }
                            return false;
                        }
                        if ("If-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x80000000L) != 0)
                            {
                                value = _headers._IfRange;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 19:
                    {
                        if ("If-Unmodified-Since".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x100000000L) != 0)
                            {
                                value = _headers._IfUnmodifiedSince;
                                return true;
                            }
                            return false;
                        }
                        if ("Proxy-Authorization".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x400000000L) != 0)
                            {
                                value = _headers._ProxyAuthorization;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 2:
                    {
                        if ("TE".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x2000000000L) != 0)
                            {
                                value = _headers._TE;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 9:
                    {
                        if ("Translate".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x4000000000L) != 0)
                            {
                                value = _headers._Translate;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 25:
                    {
                        if ("Upgrade-Insecure-Requests".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x20000000000L) != 0)
                            {
                                value = _headers._UpgradeInsecureRequests;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 29:
                    {
                        if ("Access-Control-Request-Method".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x80000000000L) != 0)
                            {
                                value = _headers._AccessControlRequestMethod;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 30:
                    {
                        if ("Access-Control-Request-Headers".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x100000000000L) != 0)
                            {
                                value = _headers._AccessControlRequestHeaders;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
            }

            return MaybeUnknown?.TryGetValue(key, out value) ?? false;
        }

        protected override void SetValueFast(string key, StringValues value)
        {
            switch (key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x1L;
                            _headers._CacheControl = value;
                            return;
                        }
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x10000L;
                            _headers._ContentRange = value;
                            return;
                        }
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x40000L;
                            _headers._LastModified = value;
                            return;
                        }
                        if ("Authorization".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x800000L;
                            _headers._Authorization = value;
                            return;
                        }
                        if ("If-None-Match".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x40000000L;
                            _headers._IfNoneMatch = value;
                            return;
                        }
                    }
                    break;
                case 10:
                    {
                        if ("Connection".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x2L;
                            _headers._Connection = value;
                            return;
                        }
                        if ("Keep-Alive".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x8L;
                            _headers._KeepAlive = value;
                            return;
                        }
                        if ("User-Agent".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x8000000000L;
                            _headers._UserAgent = value;
                            return;
                        }
                    }
                    break;
                case 4:
                    {
                        if ("Date".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x4L;
                            _headers._Date = value;
                            return;
                        }
                        if ("From".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x4000000L;
                            _headers._From = value;
                            return;
                        }
                        if ("Host".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x8000000L;
                            _headers._Host = value;
                            return;
                        }
                    }
                    break;
                case 6:
                    {
                        if ("Pragma".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x10L;
                            _headers._Pragma = value;
                            return;
                        }
                        if ("Accept".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x80000L;
                            _headers._Accept = value;
                            return;
                        }
                        if ("Cookie".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x1000000L;
                            _headers._Cookie = value;
                            return;
                        }
                        if ("Expect".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x2000000L;
                            _headers._Expect = value;
                            return;
                        }
                        if ("Origin".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x40000000000L;
                            _headers._Origin = value;
                            return;
                        }
                    }
                    break;
                case 7:
                    {
                        if ("Trailer".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x20L;
                            _headers._Trailer = value;
                            return;
                        }
                        if ("Upgrade".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x80L;
                            _headers._Upgrade = value;
                            return;
                        }
                        if ("Warning".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x200L;
                            _headers._Warning = value;
                            return;
                        }
                        if ("Expires".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x20000L;
                            _headers._Expires = value;
                            return;
                        }
                        if ("Referer".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x800000000L;
                            _headers._Referer = value;
                            return;
                        }
                    }
                    break;
                case 17:
                    {
                        if ("Transfer-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x40L;
                            _headers._TransferEncoding = value;
                            return;
                        }
                        if ("If-Modified-Since".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x20000000L;
                            _headers._IfModifiedSince = value;
                            return;
                        }
                    }
                    break;
                case 3:
                    {
                        if ("Via".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x100L;
                            _headers._Via = value;
                            return;
                        }
                        if ("DNT".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x10000000000L;
                            _headers._DNT = value;
                            return;
                        }
                    }
                    break;
                case 5:
                    {
                        if ("Allow".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x400L;
                            _headers._Allow = value;
                            return;
                        }
                        if ("Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x1000000000L;
                            _headers._Range = value;
                            return;
                        }
                    }
                    break;
                case 12:
                    {
                        if ("Content-Type".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x800L;
                            _headers._ContentType = value;
                            return;
                        }
                        if ("Max-Forwards".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x200000000L;
                            _headers._MaxForwards = value;
                            return;
                        }
                    }
                    break;
                case 16:
                    {
                        if ("Content-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x1000L;
                            _headers._ContentEncoding = value;
                            return;
                        }
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x2000L;
                            _headers._ContentLanguage = value;
                            return;
                        }
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x4000L;
                            _headers._ContentLocation = value;
                            return;
                        }
                    }
                    break;
                case 11:
                    {
                        if ("Content-MD5".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x8000L;
                            _headers._ContentMD5 = value;
                            return;
                        }
                    }
                    break;
                case 14:
                    {
                        if ("Accept-Charset".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x100000L;
                            _headers._AcceptCharset = value;
                            return;
                        }
                        if ("Content-Length".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _contentLength = ParseContentLength(value.ToString());
                            return;
                        }
                    }
                    break;
                case 15:
                    {
                        if ("Accept-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x200000L;
                            _headers._AcceptEncoding = value;
                            return;
                        }
                        if ("Accept-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x400000L;
                            _headers._AcceptLanguage = value;
                            return;
                        }
                    }
                    break;
                case 8:
                    {
                        if ("If-Match".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x10000000L;
                            _headers._IfMatch = value;
                            return;
                        }
                        if ("If-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x80000000L;
                            _headers._IfRange = value;
                            return;
                        }
                    }
                    break;
                case 19:
                    {
                        if ("If-Unmodified-Since".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x100000000L;
                            _headers._IfUnmodifiedSince = value;
                            return;
                        }
                        if ("Proxy-Authorization".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x400000000L;
                            _headers._ProxyAuthorization = value;
                            return;
                        }
                    }
                    break;
                case 2:
                    {
                        if ("TE".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x2000000000L;
                            _headers._TE = value;
                            return;
                        }
                    }
                    break;
                case 9:
                    {
                        if ("Translate".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x4000000000L;
                            _headers._Translate = value;
                            return;
                        }
                    }
                    break;
                case 25:
                    {
                        if ("Upgrade-Insecure-Requests".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x20000000000L;
                            _headers._UpgradeInsecureRequests = value;
                            return;
                        }
                    }
                    break;
                case 29:
                    {
                        if ("Access-Control-Request-Method".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x80000000000L;
                            _headers._AccessControlRequestMethod = value;
                            return;
                        }
                    }
                    break;
                case 30:
                    {
                        if ("Access-Control-Request-Headers".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x100000000000L;
                            _headers._AccessControlRequestHeaders = value;
                            return;
                        }
                    }
                    break;
            }

            SetValueUnknown(key, value);
        }

        protected override bool AddValueFast(string key, StringValues value)
        {
            switch (key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x1L) == 0)
                            {
                                _bits |= 0x1L;
                                _headers._CacheControl = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x10000L) == 0)
                            {
                                _bits |= 0x10000L;
                                _headers._ContentRange = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x40000L) == 0)
                            {
                                _bits |= 0x40000L;
                                _headers._LastModified = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Authorization".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x800000L) == 0)
                            {
                                _bits |= 0x800000L;
                                _headers._Authorization = value;
                                return true;
                            }
                            return false;
                        }
                        if ("If-None-Match".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x40000000L) == 0)
                            {
                                _bits |= 0x40000000L;
                                _headers._IfNoneMatch = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 10:
                    {
                        if ("Connection".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x2L) == 0)
                            {
                                _bits |= 0x2L;
                                _headers._Connection = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Keep-Alive".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x8L) == 0)
                            {
                                _bits |= 0x8L;
                                _headers._KeepAlive = value;
                                return true;
                            }
                            return false;
                        }
                        if ("User-Agent".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x8000000000L) == 0)
                            {
                                _bits |= 0x8000000000L;
                                _headers._UserAgent = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 4:
                    {
                        if ("Date".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x4L) == 0)
                            {
                                _bits |= 0x4L;
                                _headers._Date = value;
                                return true;
                            }
                            return false;
                        }
                        if ("From".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x4000000L) == 0)
                            {
                                _bits |= 0x4000000L;
                                _headers._From = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Host".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x8000000L) == 0)
                            {
                                _bits |= 0x8000000L;
                                _headers._Host = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 6:
                    {
                        if ("Pragma".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x10L) == 0)
                            {
                                _bits |= 0x10L;
                                _headers._Pragma = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Accept".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x80000L) == 0)
                            {
                                _bits |= 0x80000L;
                                _headers._Accept = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Cookie".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x1000000L) == 0)
                            {
                                _bits |= 0x1000000L;
                                _headers._Cookie = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Expect".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x2000000L) == 0)
                            {
                                _bits |= 0x2000000L;
                                _headers._Expect = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Origin".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x40000000000L) == 0)
                            {
                                _bits |= 0x40000000000L;
                                _headers._Origin = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 7:
                    {
                        if ("Trailer".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x20L) == 0)
                            {
                                _bits |= 0x20L;
                                _headers._Trailer = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Upgrade".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x80L) == 0)
                            {
                                _bits |= 0x80L;
                                _headers._Upgrade = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Warning".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x200L) == 0)
                            {
                                _bits |= 0x200L;
                                _headers._Warning = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Expires".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x20000L) == 0)
                            {
                                _bits |= 0x20000L;
                                _headers._Expires = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Referer".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x800000000L) == 0)
                            {
                                _bits |= 0x800000000L;
                                _headers._Referer = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 17:
                    {
                        if ("Transfer-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x40L) == 0)
                            {
                                _bits |= 0x40L;
                                _headers._TransferEncoding = value;
                                return true;
                            }
                            return false;
                        }
                        if ("If-Modified-Since".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x20000000L) == 0)
                            {
                                _bits |= 0x20000000L;
                                _headers._IfModifiedSince = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 3:
                    {
                        if ("Via".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x100L) == 0)
                            {
                                _bits |= 0x100L;
                                _headers._Via = value;
                                return true;
                            }
                            return false;
                        }
                        if ("DNT".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x10000000000L) == 0)
                            {
                                _bits |= 0x10000000000L;
                                _headers._DNT = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 5:
                    {
                        if ("Allow".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x400L) == 0)
                            {
                                _bits |= 0x400L;
                                _headers._Allow = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x1000000000L) == 0)
                            {
                                _bits |= 0x1000000000L;
                                _headers._Range = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 12:
                    {
                        if ("Content-Type".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x800L) == 0)
                            {
                                _bits |= 0x800L;
                                _headers._ContentType = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Max-Forwards".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x200000000L) == 0)
                            {
                                _bits |= 0x200000000L;
                                _headers._MaxForwards = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 16:
                    {
                        if ("Content-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x1000L) == 0)
                            {
                                _bits |= 0x1000L;
                                _headers._ContentEncoding = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x2000L) == 0)
                            {
                                _bits |= 0x2000L;
                                _headers._ContentLanguage = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x4000L) == 0)
                            {
                                _bits |= 0x4000L;
                                _headers._ContentLocation = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 11:
                    {
                        if ("Content-MD5".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x8000L) == 0)
                            {
                                _bits |= 0x8000L;
                                _headers._ContentMD5 = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 14:
                    {
                        if ("Accept-Charset".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x100000L) == 0)
                            {
                                _bits |= 0x100000L;
                                _headers._AcceptCharset = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Length".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (!_contentLength.HasValue)
                            {
                                _contentLength = ParseContentLength(value);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 15:
                    {
                        if ("Accept-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x200000L) == 0)
                            {
                                _bits |= 0x200000L;
                                _headers._AcceptEncoding = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Accept-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x400000L) == 0)
                            {
                                _bits |= 0x400000L;
                                _headers._AcceptLanguage = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 8:
                    {
                        if ("If-Match".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x10000000L) == 0)
                            {
                                _bits |= 0x10000000L;
                                _headers._IfMatch = value;
                                return true;
                            }
                            return false;
                        }
                        if ("If-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x80000000L) == 0)
                            {
                                _bits |= 0x80000000L;
                                _headers._IfRange = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 19:
                    {
                        if ("If-Unmodified-Since".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x100000000L) == 0)
                            {
                                _bits |= 0x100000000L;
                                _headers._IfUnmodifiedSince = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Proxy-Authorization".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x400000000L) == 0)
                            {
                                _bits |= 0x400000000L;
                                _headers._ProxyAuthorization = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 2:
                    {
                        if ("TE".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x2000000000L) == 0)
                            {
                                _bits |= 0x2000000000L;
                                _headers._TE = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 9:
                    {
                        if ("Translate".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x4000000000L) == 0)
                            {
                                _bits |= 0x4000000000L;
                                _headers._Translate = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 25:
                    {
                        if ("Upgrade-Insecure-Requests".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x20000000000L) == 0)
                            {
                                _bits |= 0x20000000000L;
                                _headers._UpgradeInsecureRequests = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 29:
                    {
                        if ("Access-Control-Request-Method".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x80000000000L) == 0)
                            {
                                _bits |= 0x80000000000L;
                                _headers._AccessControlRequestMethod = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 30:
                    {
                        if ("Access-Control-Request-Headers".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x100000000000L) == 0)
                            {
                                _bits |= 0x100000000000L;
                                _headers._AccessControlRequestHeaders = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
            }

            Unknown.Add(key, value);
            // Return true, above will throw and exit for false
            return true;
        }

        protected override bool RemoveFast(string key)
        {
            switch (key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x1L) != 0)
                            {
                                _bits &= ~0x1L;
                                _headers._CacheControl = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x10000L) != 0)
                            {
                                _bits &= ~0x10000L;
                                _headers._ContentRange = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x40000L) != 0)
                            {
                                _bits &= ~0x40000L;
                                _headers._LastModified = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Authorization".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x800000L) != 0)
                            {
                                _bits &= ~0x800000L;
                                _headers._Authorization = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("If-None-Match".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x40000000L) != 0)
                            {
                                _bits &= ~0x40000000L;
                                _headers._IfNoneMatch = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 10:
                    {
                        if ("Connection".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x2L) != 0)
                            {
                                _bits &= ~0x2L;
                                _headers._Connection = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Keep-Alive".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x8L) != 0)
                            {
                                _bits &= ~0x8L;
                                _headers._KeepAlive = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("User-Agent".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x8000000000L) != 0)
                            {
                                _bits &= ~0x8000000000L;
                                _headers._UserAgent = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 4:
                    {
                        if ("Date".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x4L) != 0)
                            {
                                _bits &= ~0x4L;
                                _headers._Date = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("From".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x4000000L) != 0)
                            {
                                _bits &= ~0x4000000L;
                                _headers._From = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Host".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x8000000L) != 0)
                            {
                                _bits &= ~0x8000000L;
                                _headers._Host = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 6:
                    {
                        if ("Pragma".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x10L) != 0)
                            {
                                _bits &= ~0x10L;
                                _headers._Pragma = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Accept".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x80000L) != 0)
                            {
                                _bits &= ~0x80000L;
                                _headers._Accept = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Cookie".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x1000000L) != 0)
                            {
                                _bits &= ~0x1000000L;
                                _headers._Cookie = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Expect".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x2000000L) != 0)
                            {
                                _bits &= ~0x2000000L;
                                _headers._Expect = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Origin".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x40000000000L) != 0)
                            {
                                _bits &= ~0x40000000000L;
                                _headers._Origin = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 7:
                    {
                        if ("Trailer".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x20L) != 0)
                            {
                                _bits &= ~0x20L;
                                _headers._Trailer = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Upgrade".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x80L) != 0)
                            {
                                _bits &= ~0x80L;
                                _headers._Upgrade = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Warning".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x200L) != 0)
                            {
                                _bits &= ~0x200L;
                                _headers._Warning = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Expires".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x20000L) != 0)
                            {
                                _bits &= ~0x20000L;
                                _headers._Expires = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Referer".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x800000000L) != 0)
                            {
                                _bits &= ~0x800000000L;
                                _headers._Referer = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 17:
                    {
                        if ("Transfer-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x40L) != 0)
                            {
                                _bits &= ~0x40L;
                                _headers._TransferEncoding = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("If-Modified-Since".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x20000000L) != 0)
                            {
                                _bits &= ~0x20000000L;
                                _headers._IfModifiedSince = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 3:
                    {
                        if ("Via".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x100L) != 0)
                            {
                                _bits &= ~0x100L;
                                _headers._Via = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("DNT".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x10000000000L) != 0)
                            {
                                _bits &= ~0x10000000000L;
                                _headers._DNT = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 5:
                    {
                        if ("Allow".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x400L) != 0)
                            {
                                _bits &= ~0x400L;
                                _headers._Allow = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x1000000000L) != 0)
                            {
                                _bits &= ~0x1000000000L;
                                _headers._Range = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 12:
                    {
                        if ("Content-Type".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x800L) != 0)
                            {
                                _bits &= ~0x800L;
                                _headers._ContentType = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Max-Forwards".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x200000000L) != 0)
                            {
                                _bits &= ~0x200000000L;
                                _headers._MaxForwards = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 16:
                    {
                        if ("Content-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x1000L) != 0)
                            {
                                _bits &= ~0x1000L;
                                _headers._ContentEncoding = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x2000L) != 0)
                            {
                                _bits &= ~0x2000L;
                                _headers._ContentLanguage = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x4000L) != 0)
                            {
                                _bits &= ~0x4000L;
                                _headers._ContentLocation = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 11:
                    {
                        if ("Content-MD5".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x8000L) != 0)
                            {
                                _bits &= ~0x8000L;
                                _headers._ContentMD5 = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 14:
                    {
                        if ("Accept-Charset".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x100000L) != 0)
                            {
                                _bits &= ~0x100000L;
                                _headers._AcceptCharset = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Length".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (_contentLength.HasValue)
                            {
                                _contentLength = null;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 15:
                    {
                        if ("Accept-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x200000L) != 0)
                            {
                                _bits &= ~0x200000L;
                                _headers._AcceptEncoding = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Accept-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x400000L) != 0)
                            {
                                _bits &= ~0x400000L;
                                _headers._AcceptLanguage = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 8:
                    {
                        if ("If-Match".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x10000000L) != 0)
                            {
                                _bits &= ~0x10000000L;
                                _headers._IfMatch = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("If-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x80000000L) != 0)
                            {
                                _bits &= ~0x80000000L;
                                _headers._IfRange = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 19:
                    {
                        if ("If-Unmodified-Since".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x100000000L) != 0)
                            {
                                _bits &= ~0x100000000L;
                                _headers._IfUnmodifiedSince = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Proxy-Authorization".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x400000000L) != 0)
                            {
                                _bits &= ~0x400000000L;
                                _headers._ProxyAuthorization = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 2:
                    {
                        if ("TE".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x2000000000L) != 0)
                            {
                                _bits &= ~0x2000000000L;
                                _headers._TE = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 9:
                    {
                        if ("Translate".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x4000000000L) != 0)
                            {
                                _bits &= ~0x4000000000L;
                                _headers._Translate = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 25:
                    {
                        if ("Upgrade-Insecure-Requests".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x20000000000L) != 0)
                            {
                                _bits &= ~0x20000000000L;
                                _headers._UpgradeInsecureRequests = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 29:
                    {
                        if ("Access-Control-Request-Method".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x80000000000L) != 0)
                            {
                                _bits &= ~0x80000000000L;
                                _headers._AccessControlRequestMethod = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 30:
                    {
                        if ("Access-Control-Request-Headers".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x100000000000L) != 0)
                            {
                                _bits &= ~0x100000000000L;
                                _headers._AccessControlRequestHeaders = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
            }

            return MaybeUnknown?.Remove(key) ?? false;
        }
        private void Clear(long bitsToClear)
        {
            var tempBits = bitsToClear;
            
            if ((tempBits & 0x2L) != 0)
            {
                _headers._Connection = default(StringValues);
                if((tempBits & ~0x2L) == 0)
                {
                    return;
                }
                tempBits &= ~0x2L;
            }
            
            if ((tempBits & 0x80000L) != 0)
            {
                _headers._Accept = default(StringValues);
                if((tempBits & ~0x80000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x80000L;
            }
            
            if ((tempBits & 0x8000000L) != 0)
            {
                _headers._Host = default(StringValues);
                if((tempBits & ~0x8000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x8000000L;
            }
            
            if ((tempBits & 0x8000000000L) != 0)
            {
                _headers._UserAgent = default(StringValues);
                if((tempBits & ~0x8000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x8000000000L;
            }
            
            if ((tempBits & 0x1L) != 0)
            {
                _headers._CacheControl = default(StringValues);
                if((tempBits & ~0x1L) == 0)
                {
                    return;
                }
                tempBits &= ~0x1L;
            }
            
            if ((tempBits & 0x4L) != 0)
            {
                _headers._Date = default(StringValues);
                if((tempBits & ~0x4L) == 0)
                {
                    return;
                }
                tempBits &= ~0x4L;
            }
            
            if ((tempBits & 0x8L) != 0)
            {
                _headers._KeepAlive = default(StringValues);
                if((tempBits & ~0x8L) == 0)
                {
                    return;
                }
                tempBits &= ~0x8L;
            }
            
            if ((tempBits & 0x10L) != 0)
            {
                _headers._Pragma = default(StringValues);
                if((tempBits & ~0x10L) == 0)
                {
                    return;
                }
                tempBits &= ~0x10L;
            }
            
            if ((tempBits & 0x20L) != 0)
            {
                _headers._Trailer = default(StringValues);
                if((tempBits & ~0x20L) == 0)
                {
                    return;
                }
                tempBits &= ~0x20L;
            }
            
            if ((tempBits & 0x40L) != 0)
            {
                _headers._TransferEncoding = default(StringValues);
                if((tempBits & ~0x40L) == 0)
                {
                    return;
                }
                tempBits &= ~0x40L;
            }
            
            if ((tempBits & 0x80L) != 0)
            {
                _headers._Upgrade = default(StringValues);
                if((tempBits & ~0x80L) == 0)
                {
                    return;
                }
                tempBits &= ~0x80L;
            }
            
            if ((tempBits & 0x100L) != 0)
            {
                _headers._Via = default(StringValues);
                if((tempBits & ~0x100L) == 0)
                {
                    return;
                }
                tempBits &= ~0x100L;
            }
            
            if ((tempBits & 0x200L) != 0)
            {
                _headers._Warning = default(StringValues);
                if((tempBits & ~0x200L) == 0)
                {
                    return;
                }
                tempBits &= ~0x200L;
            }
            
            if ((tempBits & 0x400L) != 0)
            {
                _headers._Allow = default(StringValues);
                if((tempBits & ~0x400L) == 0)
                {
                    return;
                }
                tempBits &= ~0x400L;
            }
            
            if ((tempBits & 0x800L) != 0)
            {
                _headers._ContentType = default(StringValues);
                if((tempBits & ~0x800L) == 0)
                {
                    return;
                }
                tempBits &= ~0x800L;
            }
            
            if ((tempBits & 0x1000L) != 0)
            {
                _headers._ContentEncoding = default(StringValues);
                if((tempBits & ~0x1000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x1000L;
            }
            
            if ((tempBits & 0x2000L) != 0)
            {
                _headers._ContentLanguage = default(StringValues);
                if((tempBits & ~0x2000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x2000L;
            }
            
            if ((tempBits & 0x4000L) != 0)
            {
                _headers._ContentLocation = default(StringValues);
                if((tempBits & ~0x4000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x4000L;
            }
            
            if ((tempBits & 0x8000L) != 0)
            {
                _headers._ContentMD5 = default(StringValues);
                if((tempBits & ~0x8000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x8000L;
            }
            
            if ((tempBits & 0x10000L) != 0)
            {
                _headers._ContentRange = default(StringValues);
                if((tempBits & ~0x10000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x10000L;
            }
            
            if ((tempBits & 0x20000L) != 0)
            {
                _headers._Expires = default(StringValues);
                if((tempBits & ~0x20000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x20000L;
            }
            
            if ((tempBits & 0x40000L) != 0)
            {
                _headers._LastModified = default(StringValues);
                if((tempBits & ~0x40000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x40000L;
            }
            
            if ((tempBits & 0x100000L) != 0)
            {
                _headers._AcceptCharset = default(StringValues);
                if((tempBits & ~0x100000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x100000L;
            }
            
            if ((tempBits & 0x200000L) != 0)
            {
                _headers._AcceptEncoding = default(StringValues);
                if((tempBits & ~0x200000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x200000L;
            }
            
            if ((tempBits & 0x400000L) != 0)
            {
                _headers._AcceptLanguage = default(StringValues);
                if((tempBits & ~0x400000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x400000L;
            }
            
            if ((tempBits & 0x800000L) != 0)
            {
                _headers._Authorization = default(StringValues);
                if((tempBits & ~0x800000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x800000L;
            }
            
            if ((tempBits & 0x1000000L) != 0)
            {
                _headers._Cookie = default(StringValues);
                if((tempBits & ~0x1000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x1000000L;
            }
            
            if ((tempBits & 0x2000000L) != 0)
            {
                _headers._Expect = default(StringValues);
                if((tempBits & ~0x2000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x2000000L;
            }
            
            if ((tempBits & 0x4000000L) != 0)
            {
                _headers._From = default(StringValues);
                if((tempBits & ~0x4000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x4000000L;
            }
            
            if ((tempBits & 0x10000000L) != 0)
            {
                _headers._IfMatch = default(StringValues);
                if((tempBits & ~0x10000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x10000000L;
            }
            
            if ((tempBits & 0x20000000L) != 0)
            {
                _headers._IfModifiedSince = default(StringValues);
                if((tempBits & ~0x20000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x20000000L;
            }
            
            if ((tempBits & 0x40000000L) != 0)
            {
                _headers._IfNoneMatch = default(StringValues);
                if((tempBits & ~0x40000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x40000000L;
            }
            
            if ((tempBits & 0x80000000L) != 0)
            {
                _headers._IfRange = default(StringValues);
                if((tempBits & ~0x80000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x80000000L;
            }
            
            if ((tempBits & 0x100000000L) != 0)
            {
                _headers._IfUnmodifiedSince = default(StringValues);
                if((tempBits & ~0x100000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x100000000L;
            }
            
            if ((tempBits & 0x200000000L) != 0)
            {
                _headers._MaxForwards = default(StringValues);
                if((tempBits & ~0x200000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x200000000L;
            }
            
            if ((tempBits & 0x400000000L) != 0)
            {
                _headers._ProxyAuthorization = default(StringValues);
                if((tempBits & ~0x400000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x400000000L;
            }
            
            if ((tempBits & 0x800000000L) != 0)
            {
                _headers._Referer = default(StringValues);
                if((tempBits & ~0x800000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x800000000L;
            }
            
            if ((tempBits & 0x1000000000L) != 0)
            {
                _headers._Range = default(StringValues);
                if((tempBits & ~0x1000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x1000000000L;
            }
            
            if ((tempBits & 0x2000000000L) != 0)
            {
                _headers._TE = default(StringValues);
                if((tempBits & ~0x2000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x2000000000L;
            }
            
            if ((tempBits & 0x4000000000L) != 0)
            {
                _headers._Translate = default(StringValues);
                if((tempBits & ~0x4000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x4000000000L;
            }
            
            if ((tempBits & 0x10000000000L) != 0)
            {
                _headers._DNT = default(StringValues);
                if((tempBits & ~0x10000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x10000000000L;
            }
            
            if ((tempBits & 0x20000000000L) != 0)
            {
                _headers._UpgradeInsecureRequests = default(StringValues);
                if((tempBits & ~0x20000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x20000000000L;
            }
            
            if ((tempBits & 0x40000000000L) != 0)
            {
                _headers._Origin = default(StringValues);
                if((tempBits & ~0x40000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x40000000000L;
            }
            
            if ((tempBits & 0x80000000000L) != 0)
            {
                _headers._AccessControlRequestMethod = default(StringValues);
                if((tempBits & ~0x80000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x80000000000L;
            }
            
            if ((tempBits & 0x100000000000L) != 0)
            {
                _headers._AccessControlRequestHeaders = default(StringValues);
                if((tempBits & ~0x100000000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x100000000000L;
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
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Cache-Control", _headers._CacheControl);
                    ++arrayIndex;
                }
                if ((_bits & 0x2L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Connection", _headers._Connection);
                    ++arrayIndex;
                }
                if ((_bits & 0x4L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Date", _headers._Date);
                    ++arrayIndex;
                }
                if ((_bits & 0x8L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Keep-Alive", _headers._KeepAlive);
                    ++arrayIndex;
                }
                if ((_bits & 0x10L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Pragma", _headers._Pragma);
                    ++arrayIndex;
                }
                if ((_bits & 0x20L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Trailer", _headers._Trailer);
                    ++arrayIndex;
                }
                if ((_bits & 0x40L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Transfer-Encoding", _headers._TransferEncoding);
                    ++arrayIndex;
                }
                if ((_bits & 0x80L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Upgrade", _headers._Upgrade);
                    ++arrayIndex;
                }
                if ((_bits & 0x100L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Via", _headers._Via);
                    ++arrayIndex;
                }
                if ((_bits & 0x200L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Warning", _headers._Warning);
                    ++arrayIndex;
                }
                if ((_bits & 0x400L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Allow", _headers._Allow);
                    ++arrayIndex;
                }
                if ((_bits & 0x800L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Type", _headers._ContentType);
                    ++arrayIndex;
                }
                if ((_bits & 0x1000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Encoding", _headers._ContentEncoding);
                    ++arrayIndex;
                }
                if ((_bits & 0x2000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Language", _headers._ContentLanguage);
                    ++arrayIndex;
                }
                if ((_bits & 0x4000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Location", _headers._ContentLocation);
                    ++arrayIndex;
                }
                if ((_bits & 0x8000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-MD5", _headers._ContentMD5);
                    ++arrayIndex;
                }
                if ((_bits & 0x10000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Range", _headers._ContentRange);
                    ++arrayIndex;
                }
                if ((_bits & 0x20000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Expires", _headers._Expires);
                    ++arrayIndex;
                }
                if ((_bits & 0x40000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Last-Modified", _headers._LastModified);
                    ++arrayIndex;
                }
                if ((_bits & 0x80000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Accept", _headers._Accept);
                    ++arrayIndex;
                }
                if ((_bits & 0x100000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Accept-Charset", _headers._AcceptCharset);
                    ++arrayIndex;
                }
                if ((_bits & 0x200000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Accept-Encoding", _headers._AcceptEncoding);
                    ++arrayIndex;
                }
                if ((_bits & 0x400000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Accept-Language", _headers._AcceptLanguage);
                    ++arrayIndex;
                }
                if ((_bits & 0x800000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Authorization", _headers._Authorization);
                    ++arrayIndex;
                }
                if ((_bits & 0x1000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Cookie", _headers._Cookie);
                    ++arrayIndex;
                }
                if ((_bits & 0x2000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Expect", _headers._Expect);
                    ++arrayIndex;
                }
                if ((_bits & 0x4000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("From", _headers._From);
                    ++arrayIndex;
                }
                if ((_bits & 0x8000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Host", _headers._Host);
                    ++arrayIndex;
                }
                if ((_bits & 0x10000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("If-Match", _headers._IfMatch);
                    ++arrayIndex;
                }
                if ((_bits & 0x20000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("If-Modified-Since", _headers._IfModifiedSince);
                    ++arrayIndex;
                }
                if ((_bits & 0x40000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("If-None-Match", _headers._IfNoneMatch);
                    ++arrayIndex;
                }
                if ((_bits & 0x80000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("If-Range", _headers._IfRange);
                    ++arrayIndex;
                }
                if ((_bits & 0x100000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("If-Unmodified-Since", _headers._IfUnmodifiedSince);
                    ++arrayIndex;
                }
                if ((_bits & 0x200000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Max-Forwards", _headers._MaxForwards);
                    ++arrayIndex;
                }
                if ((_bits & 0x400000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Proxy-Authorization", _headers._ProxyAuthorization);
                    ++arrayIndex;
                }
                if ((_bits & 0x800000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Referer", _headers._Referer);
                    ++arrayIndex;
                }
                if ((_bits & 0x1000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Range", _headers._Range);
                    ++arrayIndex;
                }
                if ((_bits & 0x2000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("TE", _headers._TE);
                    ++arrayIndex;
                }
                if ((_bits & 0x4000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Translate", _headers._Translate);
                    ++arrayIndex;
                }
                if ((_bits & 0x8000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("User-Agent", _headers._UserAgent);
                    ++arrayIndex;
                }
                if ((_bits & 0x10000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("DNT", _headers._DNT);
                    ++arrayIndex;
                }
                if ((_bits & 0x20000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Upgrade-Insecure-Requests", _headers._UpgradeInsecureRequests);
                    ++arrayIndex;
                }
                if ((_bits & 0x40000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Origin", _headers._Origin);
                    ++arrayIndex;
                }
                if ((_bits & 0x80000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Access-Control-Request-Method", _headers._AccessControlRequestMethod);
                    ++arrayIndex;
                }
                if ((_bits & 0x100000000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Access-Control-Request-Headers", _headers._AccessControlRequestHeaders);
                    ++arrayIndex;
                }
                if (_contentLength.HasValue)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Length", HeaderUtilities.FormatNonNegativeInt64(_contentLength.Value));
                    ++arrayIndex;
                }
            ((ICollection<KeyValuePair<string, StringValues>>)MaybeUnknown)?.CopyTo(array, arrayIndex);

            return true;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public unsafe void Append(Span<byte> name, Span<byte> value)
        {
            ref byte nameStart = ref MemoryMarshal.GetReference(name);
            ref StringValues values = ref Unsafe.AsRef<StringValues>(null);
            var flag = 0L;

            // Does the name matched any "known" headers
            switch (name.Length)
            {
                case 2:
                    if (((Unsafe.ReadUnaligned<ushort>(ref nameStart) & 0xdfdfu) == 0x4554u))
                    {
                        flag = 0x2000000000L;
                        values = ref _headers._TE;
                    }
                    break;
                case 3:
                    var firstTerm3 = (Unsafe.ReadUnaligned<ushort>(ref nameStart) & 0xdfdfu);
                    if ((firstTerm3 == 0x4e44u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)2) & 0xdfu) == 0x54u))
                    {
                        flag = 0x10000000000L;
                        values = ref _headers._DNT;
                    }
                    else if ((firstTerm3 == 0x4956u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)2) & 0xdfu) == 0x41u))
                    {
                        flag = 0x100L;
                        values = ref _headers._Via;
                    }
                    break;
                case 4:
                    var firstTerm4 = (Unsafe.ReadUnaligned<uint>(ref nameStart) & 0xdfdfdfdfu);
                    if ((firstTerm4 == 0x54534f48u))
                    {
                        flag = 0x8000000L;
                        values = ref _headers._Host;
                    }
                    else if ((firstTerm4 == 0x45544144u))
                    {
                        flag = 0x4L;
                        values = ref _headers._Date;
                    }
                    else if ((firstTerm4 == 0x4d4f5246u))
                    {
                        flag = 0x4000000L;
                        values = ref _headers._From;
                    }
                    break;
                case 5:
                    var firstTerm5 = (Unsafe.ReadUnaligned<uint>(ref nameStart) & 0xdfdfdfdfu);
                    if ((firstTerm5 == 0x4f4c4c41u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)4) & 0xdfu) == 0x57u))
                    {
                        flag = 0x400L;
                        values = ref _headers._Allow;
                    }
                    else if ((firstTerm5 == 0x474e4152u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)4) & 0xdfu) == 0x45u))
                    {
                        flag = 0x1000000000L;
                        values = ref _headers._Range;
                    }
                    break;
                case 6:
                    var firstTerm6 = (Unsafe.ReadUnaligned<uint>(ref nameStart) & 0xdfdfdfdfu);
                    if ((firstTerm6 == 0x45434341u) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x5450u))
                    {
                        flag = 0x80000L;
                        values = ref _headers._Accept;
                    }
                    else if ((firstTerm6 == 0x4b4f4f43u) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x4549u))
                    {
                        flag = 0x1000000L;
                        values = ref _headers._Cookie;
                    }
                    else if ((firstTerm6 == 0x45505845u) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x5443u))
                    {
                        flag = 0x2000000L;
                        values = ref _headers._Expect;
                    }
                    else if ((firstTerm6 == 0x4749524fu) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x4e49u))
                    {
                        flag = 0x40000000000L;
                        values = ref _headers._Origin;
                    }
                    else if ((firstTerm6 == 0x47415250u) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x414du))
                    {
                        flag = 0x10L;
                        values = ref _headers._Pragma;
                    }
                    break;
                case 7:
                    var firstTerm7 = (Unsafe.ReadUnaligned<uint>(ref nameStart) & 0xdfdfdfdfu);
                    if ((firstTerm7 == 0x49505845u) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x4552u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)6) & 0xdfu) == 0x53u))
                    {
                        flag = 0x20000L;
                        values = ref _headers._Expires;
                    }
                    else if ((firstTerm7 == 0x45464552u) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x4552u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)6) & 0xdfu) == 0x52u))
                    {
                        flag = 0x800000000L;
                        values = ref _headers._Referer;
                    }
                    else if ((firstTerm7 == 0x49415254u) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x454cu) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)6) & 0xdfu) == 0x52u))
                    {
                        flag = 0x20L;
                        values = ref _headers._Trailer;
                    }
                    else if ((firstTerm7 == 0x52475055u) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x4441u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)6) & 0xdfu) == 0x45u))
                    {
                        flag = 0x80L;
                        values = ref _headers._Upgrade;
                    }
                    else if ((firstTerm7 == 0x4e524157u) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ushort)))) & 0xdfdfu) == 0x4e49u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)6) & 0xdfu) == 0x47u))
                    {
                        flag = 0x200L;
                        values = ref _headers._Warning;
                    }
                    break;
                case 8:
                    var firstTerm8 = (Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfdfdfdfdfffdfdfuL);
                    if ((firstTerm8 == 0x484354414d2d4649uL))
                    {
                        flag = 0x10000000L;
                        values = ref _headers._IfMatch;
                    }
                    else if ((firstTerm8 == 0x45474e41522d4649uL))
                    {
                        flag = 0x80000000L;
                        values = ref _headers._IfRange;
                    }
                    break;
                case 9:
                    if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfdfdfdfdfdfdfdfuL) == 0x54414c534e415254uL) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)8) & 0xdfu) == 0x45u))
                    {
                        flag = 0x4000000000L;
                        values = ref _headers._Translate;
                    }
                    break;
                case 10:
                    if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfdfdfdfdfdfdfdfuL) == 0x495443454e4e4f43uL) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(4 * sizeof(ushort)))) & 0xdfdfu) == 0x4e4fu))
                    {
                        flag = 0x2L;
                        values = ref _headers._Connection;
                    }
                    else if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfdfdfffdfdfdfdfuL) == 0x4547412d52455355uL) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(4 * sizeof(ushort)))) & 0xdfdfu) == 0x544eu))
                    {
                        flag = 0x8000000000L;
                        values = ref _headers._UserAgent;
                    }
                    else if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfdfdfffdfdfdfdfuL) == 0x494c412d5045454buL) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(4 * sizeof(ushort)))) & 0xdfdfu) == 0x4556u))
                    {
                        flag = 0x8L;
                        values = ref _headers._KeepAlive;
                    }
                    break;
                case 11:
                    if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xffdfdfdfdfdfdfdfuL) == 0x2d544e45544e4f43uL) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(4 * sizeof(ushort)))) & 0xdfdfu) == 0x444du) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)10) & 0xffu) == 0x35u))
                    {
                        flag = 0x8000L;
                        values = ref _headers._ContentMD5;
                    }
                    break;
                case 12:
                    if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xffdfdfdfdfdfdfdfuL) == 0x2d544e45544e4f43uL) && ((Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x45505954u))
                    {
                        flag = 0x800L;
                        values = ref _headers._ContentType;
                    }
                    else if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfdfdfdfffdfdfdfuL) == 0x57524f462d58414duL) && ((Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x53445241u))
                    {
                        flag = 0x200000000L;
                        values = ref _headers._MaxForwards;
                    }
                    break;
                case 13:
                    if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfdfdfdfdfdfdfdfuL) == 0x5a49524f48545541uL) && ((Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x4f495441u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)12) & 0xdfu) == 0x4eu))
                    {
                        flag = 0x800000L;
                        values = ref _headers._Authorization;
                    }
                    else if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfdfffdfdfdfdfdfuL) == 0x4f432d4548434143uL) && ((Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x4f52544eu) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)12) & 0xdfu) == 0x4cu))
                    {
                        flag = 0x1L;
                        values = ref _headers._CacheControl;
                    }
                    else if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xffdfdfdfdfdfdfdfuL) == 0x2d544e45544e4f43uL) && ((Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x474e4152u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)12) & 0xdfu) == 0x45u))
                    {
                        flag = 0x10000L;
                        values = ref _headers._ContentRange;
                    }
                    else if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xffdfdfdfdfffdfdfuL) == 0x2d454e4f4e2d4649uL) && ((Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x4354414du) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)12) & 0xdfu) == 0x48u))
                    {
                        flag = 0x40000000L;
                        values = ref _headers._IfNoneMatch;
                    }
                    else if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfdfdfffdfdfdfdfuL) == 0x444f4d2d5453414cuL) && ((Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x45494649u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)12) & 0xdfu) == 0x44u))
                    {
                        flag = 0x40000L;
                        values = ref _headers._LastModified;
                    }
                    break;
                case 14:
                    if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfffdfdfdfdfdfdfuL) == 0x432d545045434341uL) && ((Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x53524148u) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(6 * sizeof(ushort)))) & 0xdfdfu) == 0x5445u))
                    {
                        flag = 0x100000L;
                        values = ref _headers._AcceptCharset;
                    }
                    else if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xffdfdfdfdfdfdfdfuL) == 0x2d544e45544e4f43uL) && ((Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x474e454cu) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(6 * sizeof(ushort)))) & 0xdfdfu) == 0x4854u))
                    {
                        AppendContentLength(value);
                        return;
                    }
                    break;
                case 15:
                    var firstTerm15 = (Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfffdfdfdfdfdfdfuL);
                    if ((firstTerm15 == 0x452d545045434341uL) && ((Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x444f434eu) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(6 * sizeof(ushort)))) & 0xdfdfu) == 0x4e49u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)14) & 0xdfu) == 0x47u))
                    {
                        flag = 0x200000L;
                        values = ref _headers._AcceptEncoding;
                    }
                    else if ((firstTerm15 == 0x4c2d545045434341uL) && ((Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x55474e41u) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(6 * sizeof(ushort)))) & 0xdfdfu) == 0x4741u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)14) & 0xdfu) == 0x45u))
                    {
                        flag = 0x400000L;
                        values = ref _headers._AcceptLanguage;
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
                        }
                        else if (((Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)sizeof(ulong))) & 0xdfdfdfdfdfdfdfdfuL) == 0x45474155474e414cuL))
                        {
                            flag = 0x2000L;
                            values = ref _headers._ContentLanguage;
                        }
                        else if (((Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)sizeof(ulong))) & 0xdfdfdfdfdfdfdfdfuL) == 0x4e4f495441434f4cuL))
                        {
                            flag = 0x4000L;
                            values = ref _headers._ContentLocation;
                        }
                    }
                    break;
                case 17:
                    if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfdfdfdfdfffdfdfuL) == 0x4649444f4d2d4649uL) && ((Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)sizeof(ulong))) & 0xdfdfdfdfffdfdfdfuL) == 0x434e49532d444549uL) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)16) & 0xdfu) == 0x45u))
                    {
                        flag = 0x20000000L;
                        values = ref _headers._IfModifiedSince;
                    }
                    else if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfdfdfdfdfdfdfdfuL) == 0x524546534e415254uL) && ((Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)sizeof(ulong))) & 0xdfdfdfdfdfdfdfffuL) == 0x4e49444f434e452duL) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)16) & 0xdfu) == 0x47u))
                    {
                        flag = 0x40L;
                        values = ref _headers._TransferEncoding;
                    }
                    break;
                case 19:
                    if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfdfdfdfdfffdfdfuL) == 0x444f4d4e552d4649uL) && ((Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)sizeof(ulong))) & 0xdfdfffdfdfdfdfdfuL) == 0x49532d4445494649uL) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(8 * sizeof(ushort)))) & 0xdfdfu) == 0x434eu) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)18) & 0xdfu) == 0x45u))
                    {
                        flag = 0x100000000L;
                        values = ref _headers._IfUnmodifiedSince;
                    }
                    else if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfdfffdfdfdfdfdfuL) == 0x55412d59584f5250uL) && ((Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)sizeof(ulong))) & 0xdfdfdfdfdfdfdfdfuL) == 0x54415a49524f4854uL) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(8 * sizeof(ushort)))) & 0xdfdfu) == 0x4f49u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)18) & 0xdfu) == 0x4eu))
                    {
                        flag = 0x400000000L;
                        values = ref _headers._ProxyAuthorization;
                    }
                    break;
                case 25:
                    if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xffdfdfdfdfdfdfdfuL) == 0x2d45444152475055uL) && ((Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)sizeof(ulong))) & 0xdfdfdfdfdfdfdfdfuL) == 0x4552554345534e49uL) && ((Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ulong)))) & 0xdfdfdfdfdfdfdfffuL) == 0x545345555145522duL) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)24) & 0xdfu) == 0x53u))
                    {
                        flag = 0x20000000000L;
                        values = ref _headers._UpgradeInsecureRequests;
                    }
                    break;
                case 29:
                    if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfffdfdfdfdfdfdfuL) == 0x432d535345434341uL) && ((Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)sizeof(ulong))) & 0xdfffdfdfdfdfdfdfuL) == 0x522d4c4f52544e4fuL) && ((Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ulong)))) & 0xdfffdfdfdfdfdfdfuL) == 0x4d2d545345555145uL) && ((Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(6 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x4f485445u) && ((Unsafe.AddByteOffset(ref nameStart, (IntPtr)28) & 0xdfu) == 0x44u))
                    {
                        flag = 0x80000000000L;
                        values = ref _headers._AccessControlRequestMethod;
                    }
                    break;
                case 30:
                    if (((Unsafe.ReadUnaligned<ulong>(ref nameStart) & 0xdfffdfdfdfdfdfdfuL) == 0x432d535345434341uL) && ((Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)sizeof(ulong))) & 0xdfffdfdfdfdfdfdfuL) == 0x522d4c4f52544e4fuL) && ((Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(2 * sizeof(ulong)))) & 0xdfffdfdfdfdfdfdfuL) == 0x482d545345555145uL) && ((Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(6 * sizeof(uint)))) & 0xdfdfdfdfu) == 0x45444145u) && ((Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)(14 * sizeof(ushort)))) & 0xdfdfu) == 0x5352u))
                    {
                        flag = 0x100000000000L;
                        values = ref _headers._AccessControlRequestHeaders;
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
                var valueStr = value.GetAsciiOrUTF8StringNonNullCharacters();
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
                var valueStr = value.GetAsciiOrUTF8StringNonNullCharacters();
                AppendUnknownHeaders(name, valueStr);
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
                        goto HeaderAccept;
                    case 20:
                        goto HeaderAcceptCharset;
                    case 21:
                        goto HeaderAcceptEncoding;
                    case 22:
                        goto HeaderAcceptLanguage;
                    case 23:
                        goto HeaderAuthorization;
                    case 24:
                        goto HeaderCookie;
                    case 25:
                        goto HeaderExpect;
                    case 26:
                        goto HeaderFrom;
                    case 27:
                        goto HeaderHost;
                    case 28:
                        goto HeaderIfMatch;
                    case 29:
                        goto HeaderIfModifiedSince;
                    case 30:
                        goto HeaderIfNoneMatch;
                    case 31:
                        goto HeaderIfRange;
                    case 32:
                        goto HeaderIfUnmodifiedSince;
                    case 33:
                        goto HeaderMaxForwards;
                    case 34:
                        goto HeaderProxyAuthorization;
                    case 35:
                        goto HeaderReferer;
                    case 36:
                        goto HeaderRange;
                    case 37:
                        goto HeaderTE;
                    case 38:
                        goto HeaderTranslate;
                    case 39:
                        goto HeaderUserAgent;
                    case 40:
                        goto HeaderDNT;
                    case 41:
                        goto HeaderUpgradeInsecureRequests;
                    case 42:
                        goto HeaderOrigin;
                    case 43:
                        goto HeaderAccessControlRequestMethod;
                    case 44:
                        goto HeaderAccessControlRequestHeaders;
                    case 45:
                        goto HeaderContentLength;
                    default:
                        goto ExtraHeaders;
                }
                
                HeaderCacheControl: // case 0
                    if ((_bits & 0x1L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Cache-Control", _collection._headers._CacheControl);
                        _next = 1;
                        return true;
                    }
                HeaderConnection: // case 1
                    if ((_bits & 0x2L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Connection", _collection._headers._Connection);
                        _next = 2;
                        return true;
                    }
                HeaderDate: // case 2
                    if ((_bits & 0x4L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Date", _collection._headers._Date);
                        _next = 3;
                        return true;
                    }
                HeaderKeepAlive: // case 3
                    if ((_bits & 0x8L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Keep-Alive", _collection._headers._KeepAlive);
                        _next = 4;
                        return true;
                    }
                HeaderPragma: // case 4
                    if ((_bits & 0x10L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Pragma", _collection._headers._Pragma);
                        _next = 5;
                        return true;
                    }
                HeaderTrailer: // case 5
                    if ((_bits & 0x20L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Trailer", _collection._headers._Trailer);
                        _next = 6;
                        return true;
                    }
                HeaderTransferEncoding: // case 6
                    if ((_bits & 0x40L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Transfer-Encoding", _collection._headers._TransferEncoding);
                        _next = 7;
                        return true;
                    }
                HeaderUpgrade: // case 7
                    if ((_bits & 0x80L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Upgrade", _collection._headers._Upgrade);
                        _next = 8;
                        return true;
                    }
                HeaderVia: // case 8
                    if ((_bits & 0x100L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Via", _collection._headers._Via);
                        _next = 9;
                        return true;
                    }
                HeaderWarning: // case 9
                    if ((_bits & 0x200L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Warning", _collection._headers._Warning);
                        _next = 10;
                        return true;
                    }
                HeaderAllow: // case 10
                    if ((_bits & 0x400L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Allow", _collection._headers._Allow);
                        _next = 11;
                        return true;
                    }
                HeaderContentType: // case 11
                    if ((_bits & 0x800L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Type", _collection._headers._ContentType);
                        _next = 12;
                        return true;
                    }
                HeaderContentEncoding: // case 12
                    if ((_bits & 0x1000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Encoding", _collection._headers._ContentEncoding);
                        _next = 13;
                        return true;
                    }
                HeaderContentLanguage: // case 13
                    if ((_bits & 0x2000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Language", _collection._headers._ContentLanguage);
                        _next = 14;
                        return true;
                    }
                HeaderContentLocation: // case 14
                    if ((_bits & 0x4000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Location", _collection._headers._ContentLocation);
                        _next = 15;
                        return true;
                    }
                HeaderContentMD5: // case 15
                    if ((_bits & 0x8000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-MD5", _collection._headers._ContentMD5);
                        _next = 16;
                        return true;
                    }
                HeaderContentRange: // case 16
                    if ((_bits & 0x10000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Range", _collection._headers._ContentRange);
                        _next = 17;
                        return true;
                    }
                HeaderExpires: // case 17
                    if ((_bits & 0x20000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Expires", _collection._headers._Expires);
                        _next = 18;
                        return true;
                    }
                HeaderLastModified: // case 18
                    if ((_bits & 0x40000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Last-Modified", _collection._headers._LastModified);
                        _next = 19;
                        return true;
                    }
                HeaderAccept: // case 19
                    if ((_bits & 0x80000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Accept", _collection._headers._Accept);
                        _next = 20;
                        return true;
                    }
                HeaderAcceptCharset: // case 20
                    if ((_bits & 0x100000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Accept-Charset", _collection._headers._AcceptCharset);
                        _next = 21;
                        return true;
                    }
                HeaderAcceptEncoding: // case 21
                    if ((_bits & 0x200000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Accept-Encoding", _collection._headers._AcceptEncoding);
                        _next = 22;
                        return true;
                    }
                HeaderAcceptLanguage: // case 22
                    if ((_bits & 0x400000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Accept-Language", _collection._headers._AcceptLanguage);
                        _next = 23;
                        return true;
                    }
                HeaderAuthorization: // case 23
                    if ((_bits & 0x800000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Authorization", _collection._headers._Authorization);
                        _next = 24;
                        return true;
                    }
                HeaderCookie: // case 24
                    if ((_bits & 0x1000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Cookie", _collection._headers._Cookie);
                        _next = 25;
                        return true;
                    }
                HeaderExpect: // case 25
                    if ((_bits & 0x2000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Expect", _collection._headers._Expect);
                        _next = 26;
                        return true;
                    }
                HeaderFrom: // case 26
                    if ((_bits & 0x4000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("From", _collection._headers._From);
                        _next = 27;
                        return true;
                    }
                HeaderHost: // case 27
                    if ((_bits & 0x8000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Host", _collection._headers._Host);
                        _next = 28;
                        return true;
                    }
                HeaderIfMatch: // case 28
                    if ((_bits & 0x10000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("If-Match", _collection._headers._IfMatch);
                        _next = 29;
                        return true;
                    }
                HeaderIfModifiedSince: // case 29
                    if ((_bits & 0x20000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("If-Modified-Since", _collection._headers._IfModifiedSince);
                        _next = 30;
                        return true;
                    }
                HeaderIfNoneMatch: // case 30
                    if ((_bits & 0x40000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("If-None-Match", _collection._headers._IfNoneMatch);
                        _next = 31;
                        return true;
                    }
                HeaderIfRange: // case 31
                    if ((_bits & 0x80000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("If-Range", _collection._headers._IfRange);
                        _next = 32;
                        return true;
                    }
                HeaderIfUnmodifiedSince: // case 32
                    if ((_bits & 0x100000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("If-Unmodified-Since", _collection._headers._IfUnmodifiedSince);
                        _next = 33;
                        return true;
                    }
                HeaderMaxForwards: // case 33
                    if ((_bits & 0x200000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Max-Forwards", _collection._headers._MaxForwards);
                        _next = 34;
                        return true;
                    }
                HeaderProxyAuthorization: // case 34
                    if ((_bits & 0x400000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Proxy-Authorization", _collection._headers._ProxyAuthorization);
                        _next = 35;
                        return true;
                    }
                HeaderReferer: // case 35
                    if ((_bits & 0x800000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Referer", _collection._headers._Referer);
                        _next = 36;
                        return true;
                    }
                HeaderRange: // case 36
                    if ((_bits & 0x1000000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Range", _collection._headers._Range);
                        _next = 37;
                        return true;
                    }
                HeaderTE: // case 37
                    if ((_bits & 0x2000000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("TE", _collection._headers._TE);
                        _next = 38;
                        return true;
                    }
                HeaderTranslate: // case 38
                    if ((_bits & 0x4000000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Translate", _collection._headers._Translate);
                        _next = 39;
                        return true;
                    }
                HeaderUserAgent: // case 39
                    if ((_bits & 0x8000000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("User-Agent", _collection._headers._UserAgent);
                        _next = 40;
                        return true;
                    }
                HeaderDNT: // case 40
                    if ((_bits & 0x10000000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("DNT", _collection._headers._DNT);
                        _next = 41;
                        return true;
                    }
                HeaderUpgradeInsecureRequests: // case 41
                    if ((_bits & 0x20000000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Upgrade-Insecure-Requests", _collection._headers._UpgradeInsecureRequests);
                        _next = 42;
                        return true;
                    }
                HeaderOrigin: // case 42
                    if ((_bits & 0x40000000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Origin", _collection._headers._Origin);
                        _next = 43;
                        return true;
                    }
                HeaderAccessControlRequestMethod: // case 43
                    if ((_bits & 0x80000000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Request-Method", _collection._headers._AccessControlRequestMethod);
                        _next = 44;
                        return true;
                    }
                HeaderAccessControlRequestHeaders: // case 44
                    if ((_bits & 0x100000000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Request-Headers", _collection._headers._AccessControlRequestHeaders);
                        _next = 45;
                        return true;
                    }
                HeaderContentLength: // case 45
                    if (_collection._contentLength.HasValue)
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Length", HeaderUtilities.FormatNonNegativeInt64(_collection._contentLength.Value));
                        _next = 46;
                        return true;
                    }
                ExtraHeaders:
                    if (!_hasUnknown || !_unknownEnumerator.MoveNext())
                    {
                        _current = default(KeyValuePair<string, StringValues>);
                        return false;
                    }
                    _current = _unknownEnumerator.Current;
                    return true;
            }
        }
    }

    internal partial class HttpResponseHeaders
    {
        private static ReadOnlySpan<byte> HeaderBytes => new byte[]
        {
            13,10,67,97,99,104,101,45,67,111,110,116,114,111,108,58,32,13,10,67,111,110,110,101,99,116,105,111,110,58,32,13,10,68,97,116,101,58,32,13,10,75,101,101,112,45,65,108,105,118,101,58,32,13,10,80,114,97,103,109,97,58,32,13,10,84,114,97,105,108,101,114,58,32,13,10,84,114,97,110,115,102,101,114,45,69,110,99,111,100,105,110,103,58,32,13,10,85,112,103,114,97,100,101,58,32,13,10,86,105,97,58,32,13,10,87,97,114,110,105,110,103,58,32,13,10,65,108,108,111,119,58,32,13,10,67,111,110,116,101,110,116,45,84,121,112,101,58,32,13,10,67,111,110,116,101,110,116,45,69,110,99,111,100,105,110,103,58,32,13,10,67,111,110,116,101,110,116,45,76,97,110,103,117,97,103,101,58,32,13,10,67,111,110,116,101,110,116,45,76,111,99,97,116,105,111,110,58,32,13,10,67,111,110,116,101,110,116,45,77,68,53,58,32,13,10,67,111,110,116,101,110,116,45,82,97,110,103,101,58,32,13,10,69,120,112,105,114,101,115,58,32,13,10,76,97,115,116,45,77,111,100,105,102,105,101,100,58,32,13,10,65,99,99,101,112,116,45,82,97,110,103,101,115,58,32,13,10,65,103,101,58,32,13,10,69,84,97,103,58,32,13,10,76,111,99,97,116,105,111,110,58,32,13,10,80,114,111,120,121,45,65,117,116,104,101,110,116,105,99,97,116,101,58,32,13,10,82,101,116,114,121,45,65,102,116,101,114,58,32,13,10,83,101,114,118,101,114,58,32,13,10,83,101,116,45,67,111,111,107,105,101,58,32,13,10,86,97,114,121,58,32,13,10,87,87,87,45,65,117,116,104,101,110,116,105,99,97,116,101,58,32,13,10,65,99,99,101,115,115,45,67,111,110,116,114,111,108,45,65,108,108,111,119,45,67,114,101,100,101,110,116,105,97,108,115,58,32,13,10,65,99,99,101,115,115,45,67,111,110,116,114,111,108,45,65,108,108,111,119,45,72,101,97,100,101,114,115,58,32,13,10,65,99,99,101,115,115,45,67,111,110,116,114,111,108,45,65,108,108,111,119,45,77,101,116,104,111,100,115,58,32,13,10,65,99,99,101,115,115,45,67,111,110,116,114,111,108,45,65,108,108,111,119,45,79,114,105,103,105,110,58,32,13,10,65,99,99,101,115,115,45,67,111,110,116,114,111,108,45,69,120,112,111,115,101,45,72,101,97,100,101,114,115,58,32,13,10,65,99,99,101,115,115,45,67,111,110,116,114,111,108,45,77,97,120,45,65,103,101,58,32,13,10,67,111,110,116,101,110,116,45,76,101,110,103,116,104,58,32,
        };
        private HeaderReferences _headers;

        public bool HasConnection => (_bits & 0x2L) != 0;
        public bool HasDate => (_bits & 0x4L) != 0;
        public bool HasTransferEncoding => (_bits & 0x40L) != 0;
        public bool HasServer => (_bits & 0x2000000L) != 0;

        
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
        public StringValues HeaderETag
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x200000L) != 0)
                {
                    value = _headers._ETag;
                }
                return value;
            }
            set
            {
                _bits |= 0x200000L;
                _headers._ETag = value; 
            }
        }
        public StringValues HeaderLocation
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x400000L) != 0)
                {
                    value = _headers._Location;
                }
                return value;
            }
            set
            {
                _bits |= 0x400000L;
                _headers._Location = value; 
            }
        }
        public StringValues HeaderProxyAuthenticate
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x800000L) != 0)
                {
                    value = _headers._ProxyAuthenticate;
                }
                return value;
            }
            set
            {
                _bits |= 0x800000L;
                _headers._ProxyAuthenticate = value; 
            }
        }
        public StringValues HeaderRetryAfter
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x1000000L) != 0)
                {
                    value = _headers._RetryAfter;
                }
                return value;
            }
            set
            {
                _bits |= 0x1000000L;
                _headers._RetryAfter = value; 
            }
        }
        public StringValues HeaderServer
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x2000000L) != 0)
                {
                    value = _headers._Server;
                }
                return value;
            }
            set
            {
                _bits |= 0x2000000L;
                _headers._Server = value; 
                _headers._rawServer = null;
            }
        }
        public StringValues HeaderSetCookie
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x4000000L) != 0)
                {
                    value = _headers._SetCookie;
                }
                return value;
            }
            set
            {
                _bits |= 0x4000000L;
                _headers._SetCookie = value; 
            }
        }
        public StringValues HeaderVary
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x8000000L) != 0)
                {
                    value = _headers._Vary;
                }
                return value;
            }
            set
            {
                _bits |= 0x8000000L;
                _headers._Vary = value; 
            }
        }
        public StringValues HeaderWWWAuthenticate
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x10000000L) != 0)
                {
                    value = _headers._WWWAuthenticate;
                }
                return value;
            }
            set
            {
                _bits |= 0x10000000L;
                _headers._WWWAuthenticate = value; 
            }
        }
        public StringValues HeaderAccessControlAllowCredentials
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x20000000L) != 0)
                {
                    value = _headers._AccessControlAllowCredentials;
                }
                return value;
            }
            set
            {
                _bits |= 0x20000000L;
                _headers._AccessControlAllowCredentials = value; 
            }
        }
        public StringValues HeaderAccessControlAllowHeaders
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x40000000L) != 0)
                {
                    value = _headers._AccessControlAllowHeaders;
                }
                return value;
            }
            set
            {
                _bits |= 0x40000000L;
                _headers._AccessControlAllowHeaders = value; 
            }
        }
        public StringValues HeaderAccessControlAllowMethods
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x80000000L) != 0)
                {
                    value = _headers._AccessControlAllowMethods;
                }
                return value;
            }
            set
            {
                _bits |= 0x80000000L;
                _headers._AccessControlAllowMethods = value; 
            }
        }
        public StringValues HeaderAccessControlAllowOrigin
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x100000000L) != 0)
                {
                    value = _headers._AccessControlAllowOrigin;
                }
                return value;
            }
            set
            {
                _bits |= 0x100000000L;
                _headers._AccessControlAllowOrigin = value; 
            }
        }
        public StringValues HeaderAccessControlExposeHeaders
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x200000000L) != 0)
                {
                    value = _headers._AccessControlExposeHeaders;
                }
                return value;
            }
            set
            {
                _bits |= 0x200000000L;
                _headers._AccessControlExposeHeaders = value; 
            }
        }
        public StringValues HeaderAccessControlMaxAge
        {
            get
            {
                StringValues value = default;
                if ((_bits & 0x400000000L) != 0)
                {
                    value = _headers._AccessControlMaxAge;
                }
                return value;
            }
            set
            {
                _bits |= 0x400000000L;
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
            _bits |= 0x2000000L;
            _headers._Server = value;
            _headers._rawServer = raw;
        }
        protected override int GetCountFast()
        {
            return (_contentLength.HasValue ? 1 : 0 ) + BitCount(_bits) + (MaybeUnknown?.Count ?? 0);
        }

        protected override bool TryGetValueFast(string key, out StringValues value)
        {
            value = default;
            switch (key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x1L) != 0)
                            {
                                value = _headers._CacheControl;
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x10000L) != 0)
                            {
                                value = _headers._ContentRange;
                                return true;
                            }
                            return false;
                        }
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x40000L) != 0)
                            {
                                value = _headers._LastModified;
                                return true;
                            }
                            return false;
                        }
                        if ("Accept-Ranges".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x80000L) != 0)
                            {
                                value = _headers._AcceptRanges;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 10:
                    {
                        if ("Connection".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x2L) != 0)
                            {
                                value = _headers._Connection;
                                return true;
                            }
                            return false;
                        }
                        if ("Keep-Alive".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x8L) != 0)
                            {
                                value = _headers._KeepAlive;
                                return true;
                            }
                            return false;
                        }
                        if ("Set-Cookie".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x4000000L) != 0)
                            {
                                value = _headers._SetCookie;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 4:
                    {
                        if ("Date".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x4L) != 0)
                            {
                                value = _headers._Date;
                                return true;
                            }
                            return false;
                        }
                        if ("ETag".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x200000L) != 0)
                            {
                                value = _headers._ETag;
                                return true;
                            }
                            return false;
                        }
                        if ("Vary".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x8000000L) != 0)
                            {
                                value = _headers._Vary;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 6:
                    {
                        if ("Pragma".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x10L) != 0)
                            {
                                value = _headers._Pragma;
                                return true;
                            }
                            return false;
                        }
                        if ("Server".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x2000000L) != 0)
                            {
                                value = _headers._Server;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 7:
                    {
                        if ("Trailer".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x20L) != 0)
                            {
                                value = _headers._Trailer;
                                return true;
                            }
                            return false;
                        }
                        if ("Upgrade".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x80L) != 0)
                            {
                                value = _headers._Upgrade;
                                return true;
                            }
                            return false;
                        }
                        if ("Warning".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x200L) != 0)
                            {
                                value = _headers._Warning;
                                return true;
                            }
                            return false;
                        }
                        if ("Expires".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x20000L) != 0)
                            {
                                value = _headers._Expires;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 17:
                    {
                        if ("Transfer-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x40L) != 0)
                            {
                                value = _headers._TransferEncoding;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 3:
                    {
                        if ("Via".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x100L) != 0)
                            {
                                value = _headers._Via;
                                return true;
                            }
                            return false;
                        }
                        if ("Age".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x100000L) != 0)
                            {
                                value = _headers._Age;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 5:
                    {
                        if ("Allow".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x400L) != 0)
                            {
                                value = _headers._Allow;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 12:
                    {
                        if ("Content-Type".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x800L) != 0)
                            {
                                value = _headers._ContentType;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 16:
                    {
                        if ("Content-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x1000L) != 0)
                            {
                                value = _headers._ContentEncoding;
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x2000L) != 0)
                            {
                                value = _headers._ContentLanguage;
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x4000L) != 0)
                            {
                                value = _headers._ContentLocation;
                                return true;
                            }
                            return false;
                        }
                        if ("WWW-Authenticate".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x10000000L) != 0)
                            {
                                value = _headers._WWWAuthenticate;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 11:
                    {
                        if ("Content-MD5".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x8000L) != 0)
                            {
                                value = _headers._ContentMD5;
                                return true;
                            }
                            return false;
                        }
                        if ("Retry-After".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x1000000L) != 0)
                            {
                                value = _headers._RetryAfter;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 8:
                    {
                        if ("Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x400000L) != 0)
                            {
                                value = _headers._Location;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 18:
                    {
                        if ("Proxy-Authenticate".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x800000L) != 0)
                            {
                                value = _headers._ProxyAuthenticate;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 32:
                    {
                        if ("Access-Control-Allow-Credentials".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x20000000L) != 0)
                            {
                                value = _headers._AccessControlAllowCredentials;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 28:
                    {
                        if ("Access-Control-Allow-Headers".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x40000000L) != 0)
                            {
                                value = _headers._AccessControlAllowHeaders;
                                return true;
                            }
                            return false;
                        }
                        if ("Access-Control-Allow-Methods".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x80000000L) != 0)
                            {
                                value = _headers._AccessControlAllowMethods;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 27:
                    {
                        if ("Access-Control-Allow-Origin".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x100000000L) != 0)
                            {
                                value = _headers._AccessControlAllowOrigin;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 29:
                    {
                        if ("Access-Control-Expose-Headers".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x200000000L) != 0)
                            {
                                value = _headers._AccessControlExposeHeaders;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 22:
                    {
                        if ("Access-Control-Max-Age".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x400000000L) != 0)
                            {
                                value = _headers._AccessControlMaxAge;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 14:
                    {
                        if ("Content-Length".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (_contentLength.HasValue)
                            {
                                value = HeaderUtilities.FormatNonNegativeInt64(_contentLength.Value);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
            }

            return MaybeUnknown?.TryGetValue(key, out value) ?? false;
        }

        protected override void SetValueFast(string key, StringValues value)
        {
            ValidateHeaderValueCharacters(value);
            switch (key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x1L;
                            _headers._CacheControl = value;
                            return;
                        }
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x10000L;
                            _headers._ContentRange = value;
                            return;
                        }
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x40000L;
                            _headers._LastModified = value;
                            return;
                        }
                        if ("Accept-Ranges".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x80000L;
                            _headers._AcceptRanges = value;
                            return;
                        }
                    }
                    break;
                case 10:
                    {
                        if ("Connection".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x2L;
                            _headers._Connection = value;
                            _headers._rawConnection = null;
                            return;
                        }
                        if ("Keep-Alive".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x8L;
                            _headers._KeepAlive = value;
                            return;
                        }
                        if ("Set-Cookie".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x4000000L;
                            _headers._SetCookie = value;
                            return;
                        }
                    }
                    break;
                case 4:
                    {
                        if ("Date".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x4L;
                            _headers._Date = value;
                            _headers._rawDate = null;
                            return;
                        }
                        if ("ETag".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x200000L;
                            _headers._ETag = value;
                            return;
                        }
                        if ("Vary".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x8000000L;
                            _headers._Vary = value;
                            return;
                        }
                    }
                    break;
                case 6:
                    {
                        if ("Pragma".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x10L;
                            _headers._Pragma = value;
                            return;
                        }
                        if ("Server".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x2000000L;
                            _headers._Server = value;
                            _headers._rawServer = null;
                            return;
                        }
                    }
                    break;
                case 7:
                    {
                        if ("Trailer".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x20L;
                            _headers._Trailer = value;
                            return;
                        }
                        if ("Upgrade".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x80L;
                            _headers._Upgrade = value;
                            return;
                        }
                        if ("Warning".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x200L;
                            _headers._Warning = value;
                            return;
                        }
                        if ("Expires".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x20000L;
                            _headers._Expires = value;
                            return;
                        }
                    }
                    break;
                case 17:
                    {
                        if ("Transfer-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x40L;
                            _headers._TransferEncoding = value;
                            _headers._rawTransferEncoding = null;
                            return;
                        }
                    }
                    break;
                case 3:
                    {
                        if ("Via".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x100L;
                            _headers._Via = value;
                            return;
                        }
                        if ("Age".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x100000L;
                            _headers._Age = value;
                            return;
                        }
                    }
                    break;
                case 5:
                    {
                        if ("Allow".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x400L;
                            _headers._Allow = value;
                            return;
                        }
                    }
                    break;
                case 12:
                    {
                        if ("Content-Type".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x800L;
                            _headers._ContentType = value;
                            return;
                        }
                    }
                    break;
                case 16:
                    {
                        if ("Content-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x1000L;
                            _headers._ContentEncoding = value;
                            return;
                        }
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x2000L;
                            _headers._ContentLanguage = value;
                            return;
                        }
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x4000L;
                            _headers._ContentLocation = value;
                            return;
                        }
                        if ("WWW-Authenticate".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x10000000L;
                            _headers._WWWAuthenticate = value;
                            return;
                        }
                    }
                    break;
                case 11:
                    {
                        if ("Content-MD5".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x8000L;
                            _headers._ContentMD5 = value;
                            return;
                        }
                        if ("Retry-After".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x1000000L;
                            _headers._RetryAfter = value;
                            return;
                        }
                    }
                    break;
                case 8:
                    {
                        if ("Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x400000L;
                            _headers._Location = value;
                            return;
                        }
                    }
                    break;
                case 18:
                    {
                        if ("Proxy-Authenticate".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x800000L;
                            _headers._ProxyAuthenticate = value;
                            return;
                        }
                    }
                    break;
                case 32:
                    {
                        if ("Access-Control-Allow-Credentials".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x20000000L;
                            _headers._AccessControlAllowCredentials = value;
                            return;
                        }
                    }
                    break;
                case 28:
                    {
                        if ("Access-Control-Allow-Headers".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x40000000L;
                            _headers._AccessControlAllowHeaders = value;
                            return;
                        }
                        if ("Access-Control-Allow-Methods".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x80000000L;
                            _headers._AccessControlAllowMethods = value;
                            return;
                        }
                    }
                    break;
                case 27:
                    {
                        if ("Access-Control-Allow-Origin".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x100000000L;
                            _headers._AccessControlAllowOrigin = value;
                            return;
                        }
                    }
                    break;
                case 29:
                    {
                        if ("Access-Control-Expose-Headers".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x200000000L;
                            _headers._AccessControlExposeHeaders = value;
                            return;
                        }
                    }
                    break;
                case 22:
                    {
                        if ("Access-Control-Max-Age".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x400000000L;
                            _headers._AccessControlMaxAge = value;
                            return;
                        }
                    }
                    break;
                case 14:
                    {
                        if ("Content-Length".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _contentLength = ParseContentLength(value.ToString());
                            return;
                        }
                    }
                    break;
            }

            SetValueUnknown(key, value);
        }

        protected override bool AddValueFast(string key, StringValues value)
        {
            ValidateHeaderValueCharacters(value);
            switch (key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x1L) == 0)
                            {
                                _bits |= 0x1L;
                                _headers._CacheControl = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x10000L) == 0)
                            {
                                _bits |= 0x10000L;
                                _headers._ContentRange = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x40000L) == 0)
                            {
                                _bits |= 0x40000L;
                                _headers._LastModified = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Accept-Ranges".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x80000L) == 0)
                            {
                                _bits |= 0x80000L;
                                _headers._AcceptRanges = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 10:
                    {
                        if ("Connection".Equals(key, StringComparison.OrdinalIgnoreCase))
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
                        if ("Keep-Alive".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x8L) == 0)
                            {
                                _bits |= 0x8L;
                                _headers._KeepAlive = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Set-Cookie".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x4000000L) == 0)
                            {
                                _bits |= 0x4000000L;
                                _headers._SetCookie = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 4:
                    {
                        if ("Date".Equals(key, StringComparison.OrdinalIgnoreCase))
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
                        if ("ETag".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x200000L) == 0)
                            {
                                _bits |= 0x200000L;
                                _headers._ETag = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Vary".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x8000000L) == 0)
                            {
                                _bits |= 0x8000000L;
                                _headers._Vary = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 6:
                    {
                        if ("Pragma".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x10L) == 0)
                            {
                                _bits |= 0x10L;
                                _headers._Pragma = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Server".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x2000000L) == 0)
                            {
                                _bits |= 0x2000000L;
                                _headers._Server = value;
                                _headers._rawServer = null;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 7:
                    {
                        if ("Trailer".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x20L) == 0)
                            {
                                _bits |= 0x20L;
                                _headers._Trailer = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Upgrade".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x80L) == 0)
                            {
                                _bits |= 0x80L;
                                _headers._Upgrade = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Warning".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x200L) == 0)
                            {
                                _bits |= 0x200L;
                                _headers._Warning = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Expires".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x20000L) == 0)
                            {
                                _bits |= 0x20000L;
                                _headers._Expires = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 17:
                    {
                        if ("Transfer-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
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
                    }
                    break;
                case 3:
                    {
                        if ("Via".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x100L) == 0)
                            {
                                _bits |= 0x100L;
                                _headers._Via = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Age".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x100000L) == 0)
                            {
                                _bits |= 0x100000L;
                                _headers._Age = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 5:
                    {
                        if ("Allow".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x400L) == 0)
                            {
                                _bits |= 0x400L;
                                _headers._Allow = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 12:
                    {
                        if ("Content-Type".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x800L) == 0)
                            {
                                _bits |= 0x800L;
                                _headers._ContentType = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 16:
                    {
                        if ("Content-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x1000L) == 0)
                            {
                                _bits |= 0x1000L;
                                _headers._ContentEncoding = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x2000L) == 0)
                            {
                                _bits |= 0x2000L;
                                _headers._ContentLanguage = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x4000L) == 0)
                            {
                                _bits |= 0x4000L;
                                _headers._ContentLocation = value;
                                return true;
                            }
                            return false;
                        }
                        if ("WWW-Authenticate".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x10000000L) == 0)
                            {
                                _bits |= 0x10000000L;
                                _headers._WWWAuthenticate = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 11:
                    {
                        if ("Content-MD5".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x8000L) == 0)
                            {
                                _bits |= 0x8000L;
                                _headers._ContentMD5 = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Retry-After".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x1000000L) == 0)
                            {
                                _bits |= 0x1000000L;
                                _headers._RetryAfter = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 8:
                    {
                        if ("Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x400000L) == 0)
                            {
                                _bits |= 0x400000L;
                                _headers._Location = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 18:
                    {
                        if ("Proxy-Authenticate".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x800000L) == 0)
                            {
                                _bits |= 0x800000L;
                                _headers._ProxyAuthenticate = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 32:
                    {
                        if ("Access-Control-Allow-Credentials".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x20000000L) == 0)
                            {
                                _bits |= 0x20000000L;
                                _headers._AccessControlAllowCredentials = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 28:
                    {
                        if ("Access-Control-Allow-Headers".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x40000000L) == 0)
                            {
                                _bits |= 0x40000000L;
                                _headers._AccessControlAllowHeaders = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Access-Control-Allow-Methods".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x80000000L) == 0)
                            {
                                _bits |= 0x80000000L;
                                _headers._AccessControlAllowMethods = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 27:
                    {
                        if ("Access-Control-Allow-Origin".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x100000000L) == 0)
                            {
                                _bits |= 0x100000000L;
                                _headers._AccessControlAllowOrigin = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 29:
                    {
                        if ("Access-Control-Expose-Headers".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x200000000L) == 0)
                            {
                                _bits |= 0x200000000L;
                                _headers._AccessControlExposeHeaders = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 22:
                    {
                        if ("Access-Control-Max-Age".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x400000000L) == 0)
                            {
                                _bits |= 0x400000000L;
                                _headers._AccessControlMaxAge = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 14:
                    {
                        if ("Content-Length".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (!_contentLength.HasValue)
                            {
                                _contentLength = ParseContentLength(value);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
            }

            ValidateHeaderNameCharacters(key);
            Unknown.Add(key, value);
            // Return true, above will throw and exit for false
            return true;
        }

        protected override bool RemoveFast(string key)
        {
            switch (key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x1L) != 0)
                            {
                                _bits &= ~0x1L;
                                _headers._CacheControl = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x10000L) != 0)
                            {
                                _bits &= ~0x10000L;
                                _headers._ContentRange = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x40000L) != 0)
                            {
                                _bits &= ~0x40000L;
                                _headers._LastModified = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Accept-Ranges".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x80000L) != 0)
                            {
                                _bits &= ~0x80000L;
                                _headers._AcceptRanges = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 10:
                    {
                        if ("Connection".Equals(key, StringComparison.OrdinalIgnoreCase))
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
                        if ("Keep-Alive".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x8L) != 0)
                            {
                                _bits &= ~0x8L;
                                _headers._KeepAlive = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Set-Cookie".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x4000000L) != 0)
                            {
                                _bits &= ~0x4000000L;
                                _headers._SetCookie = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 4:
                    {
                        if ("Date".Equals(key, StringComparison.OrdinalIgnoreCase))
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
                        if ("ETag".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x200000L) != 0)
                            {
                                _bits &= ~0x200000L;
                                _headers._ETag = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Vary".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x8000000L) != 0)
                            {
                                _bits &= ~0x8000000L;
                                _headers._Vary = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 6:
                    {
                        if ("Pragma".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x10L) != 0)
                            {
                                _bits &= ~0x10L;
                                _headers._Pragma = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Server".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x2000000L) != 0)
                            {
                                _bits &= ~0x2000000L;
                                _headers._Server = default(StringValues);
                                _headers._rawServer = null;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 7:
                    {
                        if ("Trailer".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x20L) != 0)
                            {
                                _bits &= ~0x20L;
                                _headers._Trailer = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Upgrade".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x80L) != 0)
                            {
                                _bits &= ~0x80L;
                                _headers._Upgrade = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Warning".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x200L) != 0)
                            {
                                _bits &= ~0x200L;
                                _headers._Warning = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Expires".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x20000L) != 0)
                            {
                                _bits &= ~0x20000L;
                                _headers._Expires = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 17:
                    {
                        if ("Transfer-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
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
                    }
                    break;
                case 3:
                    {
                        if ("Via".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x100L) != 0)
                            {
                                _bits &= ~0x100L;
                                _headers._Via = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Age".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x100000L) != 0)
                            {
                                _bits &= ~0x100000L;
                                _headers._Age = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 5:
                    {
                        if ("Allow".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x400L) != 0)
                            {
                                _bits &= ~0x400L;
                                _headers._Allow = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 12:
                    {
                        if ("Content-Type".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x800L) != 0)
                            {
                                _bits &= ~0x800L;
                                _headers._ContentType = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 16:
                    {
                        if ("Content-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x1000L) != 0)
                            {
                                _bits &= ~0x1000L;
                                _headers._ContentEncoding = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x2000L) != 0)
                            {
                                _bits &= ~0x2000L;
                                _headers._ContentLanguage = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x4000L) != 0)
                            {
                                _bits &= ~0x4000L;
                                _headers._ContentLocation = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("WWW-Authenticate".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x10000000L) != 0)
                            {
                                _bits &= ~0x10000000L;
                                _headers._WWWAuthenticate = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 11:
                    {
                        if ("Content-MD5".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x8000L) != 0)
                            {
                                _bits &= ~0x8000L;
                                _headers._ContentMD5 = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Retry-After".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x1000000L) != 0)
                            {
                                _bits &= ~0x1000000L;
                                _headers._RetryAfter = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 8:
                    {
                        if ("Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x400000L) != 0)
                            {
                                _bits &= ~0x400000L;
                                _headers._Location = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 18:
                    {
                        if ("Proxy-Authenticate".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x800000L) != 0)
                            {
                                _bits &= ~0x800000L;
                                _headers._ProxyAuthenticate = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 32:
                    {
                        if ("Access-Control-Allow-Credentials".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x20000000L) != 0)
                            {
                                _bits &= ~0x20000000L;
                                _headers._AccessControlAllowCredentials = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 28:
                    {
                        if ("Access-Control-Allow-Headers".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x40000000L) != 0)
                            {
                                _bits &= ~0x40000000L;
                                _headers._AccessControlAllowHeaders = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Access-Control-Allow-Methods".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x80000000L) != 0)
                            {
                                _bits &= ~0x80000000L;
                                _headers._AccessControlAllowMethods = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 27:
                    {
                        if ("Access-Control-Allow-Origin".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x100000000L) != 0)
                            {
                                _bits &= ~0x100000000L;
                                _headers._AccessControlAllowOrigin = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 29:
                    {
                        if ("Access-Control-Expose-Headers".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x200000000L) != 0)
                            {
                                _bits &= ~0x200000000L;
                                _headers._AccessControlExposeHeaders = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 22:
                    {
                        if ("Access-Control-Max-Age".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x400000000L) != 0)
                            {
                                _bits &= ~0x400000000L;
                                _headers._AccessControlMaxAge = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                case 14:
                    {
                        if ("Content-Length".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (_contentLength.HasValue)
                            {
                                _contentLength = null;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
            }

            return MaybeUnknown?.Remove(key) ?? false;
        }
        protected override void ClearFast()
        {
            MaybeUnknown?.Clear();
            _contentLength = null;
            var tempBits = _bits;
            _bits = 0;
            if(HttpHeaders.BitCount(tempBits) > 12)
            {
                _headers = default(HeaderReferences);
                return;
            }
            
            if ((tempBits & 0x2L) != 0)
            {
                _headers._Connection = default(StringValues);
                if((tempBits & ~0x2L) == 0)
                {
                    return;
                }
                tempBits &= ~0x2L;
            }
            
            if ((tempBits & 0x4L) != 0)
            {
                _headers._Date = default(StringValues);
                if((tempBits & ~0x4L) == 0)
                {
                    return;
                }
                tempBits &= ~0x4L;
            }
            
            if ((tempBits & 0x800L) != 0)
            {
                _headers._ContentType = default(StringValues);
                if((tempBits & ~0x800L) == 0)
                {
                    return;
                }
                tempBits &= ~0x800L;
            }
            
            if ((tempBits & 0x2000000L) != 0)
            {
                _headers._Server = default(StringValues);
                if((tempBits & ~0x2000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x2000000L;
            }
            
            if ((tempBits & 0x1L) != 0)
            {
                _headers._CacheControl = default(StringValues);
                if((tempBits & ~0x1L) == 0)
                {
                    return;
                }
                tempBits &= ~0x1L;
            }
            
            if ((tempBits & 0x8L) != 0)
            {
                _headers._KeepAlive = default(StringValues);
                if((tempBits & ~0x8L) == 0)
                {
                    return;
                }
                tempBits &= ~0x8L;
            }
            
            if ((tempBits & 0x10L) != 0)
            {
                _headers._Pragma = default(StringValues);
                if((tempBits & ~0x10L) == 0)
                {
                    return;
                }
                tempBits &= ~0x10L;
            }
            
            if ((tempBits & 0x20L) != 0)
            {
                _headers._Trailer = default(StringValues);
                if((tempBits & ~0x20L) == 0)
                {
                    return;
                }
                tempBits &= ~0x20L;
            }
            
            if ((tempBits & 0x40L) != 0)
            {
                _headers._TransferEncoding = default(StringValues);
                if((tempBits & ~0x40L) == 0)
                {
                    return;
                }
                tempBits &= ~0x40L;
            }
            
            if ((tempBits & 0x80L) != 0)
            {
                _headers._Upgrade = default(StringValues);
                if((tempBits & ~0x80L) == 0)
                {
                    return;
                }
                tempBits &= ~0x80L;
            }
            
            if ((tempBits & 0x100L) != 0)
            {
                _headers._Via = default(StringValues);
                if((tempBits & ~0x100L) == 0)
                {
                    return;
                }
                tempBits &= ~0x100L;
            }
            
            if ((tempBits & 0x200L) != 0)
            {
                _headers._Warning = default(StringValues);
                if((tempBits & ~0x200L) == 0)
                {
                    return;
                }
                tempBits &= ~0x200L;
            }
            
            if ((tempBits & 0x400L) != 0)
            {
                _headers._Allow = default(StringValues);
                if((tempBits & ~0x400L) == 0)
                {
                    return;
                }
                tempBits &= ~0x400L;
            }
            
            if ((tempBits & 0x1000L) != 0)
            {
                _headers._ContentEncoding = default(StringValues);
                if((tempBits & ~0x1000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x1000L;
            }
            
            if ((tempBits & 0x2000L) != 0)
            {
                _headers._ContentLanguage = default(StringValues);
                if((tempBits & ~0x2000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x2000L;
            }
            
            if ((tempBits & 0x4000L) != 0)
            {
                _headers._ContentLocation = default(StringValues);
                if((tempBits & ~0x4000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x4000L;
            }
            
            if ((tempBits & 0x8000L) != 0)
            {
                _headers._ContentMD5 = default(StringValues);
                if((tempBits & ~0x8000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x8000L;
            }
            
            if ((tempBits & 0x10000L) != 0)
            {
                _headers._ContentRange = default(StringValues);
                if((tempBits & ~0x10000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x10000L;
            }
            
            if ((tempBits & 0x20000L) != 0)
            {
                _headers._Expires = default(StringValues);
                if((tempBits & ~0x20000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x20000L;
            }
            
            if ((tempBits & 0x40000L) != 0)
            {
                _headers._LastModified = default(StringValues);
                if((tempBits & ~0x40000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x40000L;
            }
            
            if ((tempBits & 0x80000L) != 0)
            {
                _headers._AcceptRanges = default(StringValues);
                if((tempBits & ~0x80000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x80000L;
            }
            
            if ((tempBits & 0x100000L) != 0)
            {
                _headers._Age = default(StringValues);
                if((tempBits & ~0x100000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x100000L;
            }
            
            if ((tempBits & 0x200000L) != 0)
            {
                _headers._ETag = default(StringValues);
                if((tempBits & ~0x200000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x200000L;
            }
            
            if ((tempBits & 0x400000L) != 0)
            {
                _headers._Location = default(StringValues);
                if((tempBits & ~0x400000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x400000L;
            }
            
            if ((tempBits & 0x800000L) != 0)
            {
                _headers._ProxyAuthenticate = default(StringValues);
                if((tempBits & ~0x800000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x800000L;
            }
            
            if ((tempBits & 0x1000000L) != 0)
            {
                _headers._RetryAfter = default(StringValues);
                if((tempBits & ~0x1000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x1000000L;
            }
            
            if ((tempBits & 0x4000000L) != 0)
            {
                _headers._SetCookie = default(StringValues);
                if((tempBits & ~0x4000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x4000000L;
            }
            
            if ((tempBits & 0x8000000L) != 0)
            {
                _headers._Vary = default(StringValues);
                if((tempBits & ~0x8000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x8000000L;
            }
            
            if ((tempBits & 0x10000000L) != 0)
            {
                _headers._WWWAuthenticate = default(StringValues);
                if((tempBits & ~0x10000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x10000000L;
            }
            
            if ((tempBits & 0x20000000L) != 0)
            {
                _headers._AccessControlAllowCredentials = default(StringValues);
                if((tempBits & ~0x20000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x20000000L;
            }
            
            if ((tempBits & 0x40000000L) != 0)
            {
                _headers._AccessControlAllowHeaders = default(StringValues);
                if((tempBits & ~0x40000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x40000000L;
            }
            
            if ((tempBits & 0x80000000L) != 0)
            {
                _headers._AccessControlAllowMethods = default(StringValues);
                if((tempBits & ~0x80000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x80000000L;
            }
            
            if ((tempBits & 0x100000000L) != 0)
            {
                _headers._AccessControlAllowOrigin = default(StringValues);
                if((tempBits & ~0x100000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x100000000L;
            }
            
            if ((tempBits & 0x200000000L) != 0)
            {
                _headers._AccessControlExposeHeaders = default(StringValues);
                if((tempBits & ~0x200000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x200000000L;
            }
            
            if ((tempBits & 0x400000000L) != 0)
            {
                _headers._AccessControlMaxAge = default(StringValues);
                if((tempBits & ~0x400000000L) == 0)
                {
                    return;
                }
                tempBits &= ~0x400000000L;
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
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Cache-Control", _headers._CacheControl);
                    ++arrayIndex;
                }
                if ((_bits & 0x2L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Connection", _headers._Connection);
                    ++arrayIndex;
                }
                if ((_bits & 0x4L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Date", _headers._Date);
                    ++arrayIndex;
                }
                if ((_bits & 0x8L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Keep-Alive", _headers._KeepAlive);
                    ++arrayIndex;
                }
                if ((_bits & 0x10L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Pragma", _headers._Pragma);
                    ++arrayIndex;
                }
                if ((_bits & 0x20L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Trailer", _headers._Trailer);
                    ++arrayIndex;
                }
                if ((_bits & 0x40L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Transfer-Encoding", _headers._TransferEncoding);
                    ++arrayIndex;
                }
                if ((_bits & 0x80L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Upgrade", _headers._Upgrade);
                    ++arrayIndex;
                }
                if ((_bits & 0x100L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Via", _headers._Via);
                    ++arrayIndex;
                }
                if ((_bits & 0x200L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Warning", _headers._Warning);
                    ++arrayIndex;
                }
                if ((_bits & 0x400L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Allow", _headers._Allow);
                    ++arrayIndex;
                }
                if ((_bits & 0x800L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Type", _headers._ContentType);
                    ++arrayIndex;
                }
                if ((_bits & 0x1000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Encoding", _headers._ContentEncoding);
                    ++arrayIndex;
                }
                if ((_bits & 0x2000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Language", _headers._ContentLanguage);
                    ++arrayIndex;
                }
                if ((_bits & 0x4000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Location", _headers._ContentLocation);
                    ++arrayIndex;
                }
                if ((_bits & 0x8000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-MD5", _headers._ContentMD5);
                    ++arrayIndex;
                }
                if ((_bits & 0x10000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Range", _headers._ContentRange);
                    ++arrayIndex;
                }
                if ((_bits & 0x20000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Expires", _headers._Expires);
                    ++arrayIndex;
                }
                if ((_bits & 0x40000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Last-Modified", _headers._LastModified);
                    ++arrayIndex;
                }
                if ((_bits & 0x80000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Accept-Ranges", _headers._AcceptRanges);
                    ++arrayIndex;
                }
                if ((_bits & 0x100000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Age", _headers._Age);
                    ++arrayIndex;
                }
                if ((_bits & 0x200000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("ETag", _headers._ETag);
                    ++arrayIndex;
                }
                if ((_bits & 0x400000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Location", _headers._Location);
                    ++arrayIndex;
                }
                if ((_bits & 0x800000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Proxy-Authenticate", _headers._ProxyAuthenticate);
                    ++arrayIndex;
                }
                if ((_bits & 0x1000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Retry-After", _headers._RetryAfter);
                    ++arrayIndex;
                }
                if ((_bits & 0x2000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Server", _headers._Server);
                    ++arrayIndex;
                }
                if ((_bits & 0x4000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Set-Cookie", _headers._SetCookie);
                    ++arrayIndex;
                }
                if ((_bits & 0x8000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Vary", _headers._Vary);
                    ++arrayIndex;
                }
                if ((_bits & 0x10000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("WWW-Authenticate", _headers._WWWAuthenticate);
                    ++arrayIndex;
                }
                if ((_bits & 0x20000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Access-Control-Allow-Credentials", _headers._AccessControlAllowCredentials);
                    ++arrayIndex;
                }
                if ((_bits & 0x40000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Access-Control-Allow-Headers", _headers._AccessControlAllowHeaders);
                    ++arrayIndex;
                }
                if ((_bits & 0x80000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Access-Control-Allow-Methods", _headers._AccessControlAllowMethods);
                    ++arrayIndex;
                }
                if ((_bits & 0x100000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Access-Control-Allow-Origin", _headers._AccessControlAllowOrigin);
                    ++arrayIndex;
                }
                if ((_bits & 0x200000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Access-Control-Expose-Headers", _headers._AccessControlExposeHeaders);
                    ++arrayIndex;
                }
                if ((_bits & 0x400000000L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Access-Control-Max-Age", _headers._AccessControlMaxAge);
                    ++arrayIndex;
                }
                if (_contentLength.HasValue)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Length", HeaderUtilities.FormatNonNegativeInt64(_contentLength.Value));
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
                        if ((tempBits & 0x2000000L) != 0)
                        {
                            tempBits ^= 0x2000000L;
                            if (_headers._rawServer != null)
                            {
                                output.Write(_headers._rawServer);
                            }
                            else
                            {
                                values = ref _headers._Server;
                                keyStart = 350;
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
                            output.Write(HeaderBytes.Slice(592, 18));
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
                    case 23: // Header: "ETag"
                        if ((tempBits & 0x200000L) != 0)
                        {
                            tempBits ^= 0x200000L;
                            values = ref _headers._ETag;
                            keyStart = 293;
                            keyLength = 8;
                            next = 24;
                            break; // OutputHeader
                        }
                        goto case 24;
                    case 24: // Header: "Location"
                        if ((tempBits & 0x400000L) != 0)
                        {
                            tempBits ^= 0x400000L;
                            values = ref _headers._Location;
                            keyStart = 301;
                            keyLength = 12;
                            next = 25;
                            break; // OutputHeader
                        }
                        goto case 25;
                    case 25: // Header: "Proxy-Authenticate"
                        if ((tempBits & 0x800000L) != 0)
                        {
                            tempBits ^= 0x800000L;
                            values = ref _headers._ProxyAuthenticate;
                            keyStart = 313;
                            keyLength = 22;
                            next = 26;
                            break; // OutputHeader
                        }
                        goto case 26;
                    case 26: // Header: "Retry-After"
                        if ((tempBits & 0x1000000L) != 0)
                        {
                            tempBits ^= 0x1000000L;
                            values = ref _headers._RetryAfter;
                            keyStart = 335;
                            keyLength = 15;
                            next = 27;
                            break; // OutputHeader
                        }
                        goto case 27;
                    case 27: // Header: "Set-Cookie"
                        if ((tempBits & 0x4000000L) != 0)
                        {
                            tempBits ^= 0x4000000L;
                            values = ref _headers._SetCookie;
                            keyStart = 360;
                            keyLength = 14;
                            next = 28;
                            break; // OutputHeader
                        }
                        goto case 28;
                    case 28: // Header: "Vary"
                        if ((tempBits & 0x8000000L) != 0)
                        {
                            tempBits ^= 0x8000000L;
                            values = ref _headers._Vary;
                            keyStart = 374;
                            keyLength = 8;
                            next = 29;
                            break; // OutputHeader
                        }
                        goto case 29;
                    case 29: // Header: "WWW-Authenticate"
                        if ((tempBits & 0x10000000L) != 0)
                        {
                            tempBits ^= 0x10000000L;
                            values = ref _headers._WWWAuthenticate;
                            keyStart = 382;
                            keyLength = 20;
                            next = 30;
                            break; // OutputHeader
                        }
                        goto case 30;
                    case 30: // Header: "Access-Control-Allow-Credentials"
                        if ((tempBits & 0x20000000L) != 0)
                        {
                            tempBits ^= 0x20000000L;
                            values = ref _headers._AccessControlAllowCredentials;
                            keyStart = 402;
                            keyLength = 36;
                            next = 31;
                            break; // OutputHeader
                        }
                        goto case 31;
                    case 31: // Header: "Access-Control-Allow-Headers"
                        if ((tempBits & 0x40000000L) != 0)
                        {
                            tempBits ^= 0x40000000L;
                            values = ref _headers._AccessControlAllowHeaders;
                            keyStart = 438;
                            keyLength = 32;
                            next = 32;
                            break; // OutputHeader
                        }
                        goto case 32;
                    case 32: // Header: "Access-Control-Allow-Methods"
                        if ((tempBits & 0x80000000L) != 0)
                        {
                            tempBits ^= 0x80000000L;
                            values = ref _headers._AccessControlAllowMethods;
                            keyStart = 470;
                            keyLength = 32;
                            next = 33;
                            break; // OutputHeader
                        }
                        goto case 33;
                    case 33: // Header: "Access-Control-Allow-Origin"
                        if ((tempBits & 0x100000000L) != 0)
                        {
                            tempBits ^= 0x100000000L;
                            values = ref _headers._AccessControlAllowOrigin;
                            keyStart = 502;
                            keyLength = 31;
                            next = 34;
                            break; // OutputHeader
                        }
                        goto case 34;
                    case 34: // Header: "Access-Control-Expose-Headers"
                        if ((tempBits & 0x200000000L) != 0)
                        {
                            tempBits ^= 0x200000000L;
                            values = ref _headers._AccessControlExposeHeaders;
                            keyStart = 533;
                            keyLength = 33;
                            next = 35;
                            break; // OutputHeader
                        }
                        goto case 35;
                    case 35: // Header: "Access-Control-Max-Age"
                        if ((tempBits & 0x400000000L) != 0)
                        {
                            tempBits ^= 0x400000000L;
                            values = ref _headers._AccessControlMaxAge;
                            keyStart = 566;
                            keyLength = 26;
                            next = 36;
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
                            output.WriteAsciiNoValidation(value);
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
                        goto HeaderETag;
                    case 22:
                        goto HeaderLocation;
                    case 23:
                        goto HeaderProxyAuthenticate;
                    case 24:
                        goto HeaderRetryAfter;
                    case 25:
                        goto HeaderServer;
                    case 26:
                        goto HeaderSetCookie;
                    case 27:
                        goto HeaderVary;
                    case 28:
                        goto HeaderWWWAuthenticate;
                    case 29:
                        goto HeaderAccessControlAllowCredentials;
                    case 30:
                        goto HeaderAccessControlAllowHeaders;
                    case 31:
                        goto HeaderAccessControlAllowMethods;
                    case 32:
                        goto HeaderAccessControlAllowOrigin;
                    case 33:
                        goto HeaderAccessControlExposeHeaders;
                    case 34:
                        goto HeaderAccessControlMaxAge;
                    case 35:
                        goto HeaderContentLength;
                    default:
                        goto ExtraHeaders;
                }
                
                HeaderCacheControl: // case 0
                    if ((_bits & 0x1L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Cache-Control", _collection._headers._CacheControl);
                        _next = 1;
                        return true;
                    }
                HeaderConnection: // case 1
                    if ((_bits & 0x2L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Connection", _collection._headers._Connection);
                        _next = 2;
                        return true;
                    }
                HeaderDate: // case 2
                    if ((_bits & 0x4L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Date", _collection._headers._Date);
                        _next = 3;
                        return true;
                    }
                HeaderKeepAlive: // case 3
                    if ((_bits & 0x8L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Keep-Alive", _collection._headers._KeepAlive);
                        _next = 4;
                        return true;
                    }
                HeaderPragma: // case 4
                    if ((_bits & 0x10L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Pragma", _collection._headers._Pragma);
                        _next = 5;
                        return true;
                    }
                HeaderTrailer: // case 5
                    if ((_bits & 0x20L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Trailer", _collection._headers._Trailer);
                        _next = 6;
                        return true;
                    }
                HeaderTransferEncoding: // case 6
                    if ((_bits & 0x40L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Transfer-Encoding", _collection._headers._TransferEncoding);
                        _next = 7;
                        return true;
                    }
                HeaderUpgrade: // case 7
                    if ((_bits & 0x80L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Upgrade", _collection._headers._Upgrade);
                        _next = 8;
                        return true;
                    }
                HeaderVia: // case 8
                    if ((_bits & 0x100L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Via", _collection._headers._Via);
                        _next = 9;
                        return true;
                    }
                HeaderWarning: // case 9
                    if ((_bits & 0x200L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Warning", _collection._headers._Warning);
                        _next = 10;
                        return true;
                    }
                HeaderAllow: // case 10
                    if ((_bits & 0x400L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Allow", _collection._headers._Allow);
                        _next = 11;
                        return true;
                    }
                HeaderContentType: // case 11
                    if ((_bits & 0x800L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Type", _collection._headers._ContentType);
                        _next = 12;
                        return true;
                    }
                HeaderContentEncoding: // case 12
                    if ((_bits & 0x1000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Encoding", _collection._headers._ContentEncoding);
                        _next = 13;
                        return true;
                    }
                HeaderContentLanguage: // case 13
                    if ((_bits & 0x2000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Language", _collection._headers._ContentLanguage);
                        _next = 14;
                        return true;
                    }
                HeaderContentLocation: // case 14
                    if ((_bits & 0x4000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Location", _collection._headers._ContentLocation);
                        _next = 15;
                        return true;
                    }
                HeaderContentMD5: // case 15
                    if ((_bits & 0x8000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-MD5", _collection._headers._ContentMD5);
                        _next = 16;
                        return true;
                    }
                HeaderContentRange: // case 16
                    if ((_bits & 0x10000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Range", _collection._headers._ContentRange);
                        _next = 17;
                        return true;
                    }
                HeaderExpires: // case 17
                    if ((_bits & 0x20000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Expires", _collection._headers._Expires);
                        _next = 18;
                        return true;
                    }
                HeaderLastModified: // case 18
                    if ((_bits & 0x40000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Last-Modified", _collection._headers._LastModified);
                        _next = 19;
                        return true;
                    }
                HeaderAcceptRanges: // case 19
                    if ((_bits & 0x80000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Accept-Ranges", _collection._headers._AcceptRanges);
                        _next = 20;
                        return true;
                    }
                HeaderAge: // case 20
                    if ((_bits & 0x100000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Age", _collection._headers._Age);
                        _next = 21;
                        return true;
                    }
                HeaderETag: // case 21
                    if ((_bits & 0x200000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("ETag", _collection._headers._ETag);
                        _next = 22;
                        return true;
                    }
                HeaderLocation: // case 22
                    if ((_bits & 0x400000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Location", _collection._headers._Location);
                        _next = 23;
                        return true;
                    }
                HeaderProxyAuthenticate: // case 23
                    if ((_bits & 0x800000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Proxy-Authenticate", _collection._headers._ProxyAuthenticate);
                        _next = 24;
                        return true;
                    }
                HeaderRetryAfter: // case 24
                    if ((_bits & 0x1000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Retry-After", _collection._headers._RetryAfter);
                        _next = 25;
                        return true;
                    }
                HeaderServer: // case 25
                    if ((_bits & 0x2000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Server", _collection._headers._Server);
                        _next = 26;
                        return true;
                    }
                HeaderSetCookie: // case 26
                    if ((_bits & 0x4000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Set-Cookie", _collection._headers._SetCookie);
                        _next = 27;
                        return true;
                    }
                HeaderVary: // case 27
                    if ((_bits & 0x8000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Vary", _collection._headers._Vary);
                        _next = 28;
                        return true;
                    }
                HeaderWWWAuthenticate: // case 28
                    if ((_bits & 0x10000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("WWW-Authenticate", _collection._headers._WWWAuthenticate);
                        _next = 29;
                        return true;
                    }
                HeaderAccessControlAllowCredentials: // case 29
                    if ((_bits & 0x20000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Allow-Credentials", _collection._headers._AccessControlAllowCredentials);
                        _next = 30;
                        return true;
                    }
                HeaderAccessControlAllowHeaders: // case 30
                    if ((_bits & 0x40000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Allow-Headers", _collection._headers._AccessControlAllowHeaders);
                        _next = 31;
                        return true;
                    }
                HeaderAccessControlAllowMethods: // case 31
                    if ((_bits & 0x80000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Allow-Methods", _collection._headers._AccessControlAllowMethods);
                        _next = 32;
                        return true;
                    }
                HeaderAccessControlAllowOrigin: // case 32
                    if ((_bits & 0x100000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Allow-Origin", _collection._headers._AccessControlAllowOrigin);
                        _next = 33;
                        return true;
                    }
                HeaderAccessControlExposeHeaders: // case 33
                    if ((_bits & 0x200000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Expose-Headers", _collection._headers._AccessControlExposeHeaders);
                        _next = 34;
                        return true;
                    }
                HeaderAccessControlMaxAge: // case 34
                    if ((_bits & 0x400000000L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Max-Age", _collection._headers._AccessControlMaxAge);
                        _next = 35;
                        return true;
                    }
                HeaderContentLength: // case 35
                    if (_collection._contentLength.HasValue)
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Length", HeaderUtilities.FormatNonNegativeInt64(_collection._contentLength.Value));
                        _next = 36;
                        return true;
                    }
                ExtraHeaders:
                    if (!_hasUnknown || !_unknownEnumerator.MoveNext())
                    {
                        _current = default(KeyValuePair<string, StringValues>);
                        return false;
                    }
                    _current = _unknownEnumerator.Current;
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
            return (_contentLength.HasValue ? 1 : 0 ) + BitCount(_bits) + (MaybeUnknown?.Count ?? 0);
        }

        protected override bool TryGetValueFast(string key, out StringValues value)
        {
            value = default;
            switch (key.Length)
            {
                case 4:
                    {
                        if ("ETag".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x1L) != 0)
                            {
                                value = _headers._ETag;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
            }

            return MaybeUnknown?.TryGetValue(key, out value) ?? false;
        }

        protected override void SetValueFast(string key, StringValues value)
        {
            ValidateHeaderValueCharacters(value);
            switch (key.Length)
            {
                case 4:
                    {
                        if ("ETag".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 0x1L;
                            _headers._ETag = value;
                            return;
                        }
                    }
                    break;
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
                        if ("ETag".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x1L) == 0)
                            {
                                _bits |= 0x1L;
                                _headers._ETag = value;
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
            }

            Unknown.Add(key, value);
            // Return true, above will throw and exit for false
            return true;
        }

        protected override bool RemoveFast(string key)
        {
            switch (key.Length)
            {
                case 4:
                    {
                        if ("ETag".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 0x1L) != 0)
                            {
                                _bits &= ~0x1L;
                                _headers._ETag = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
            }

            return MaybeUnknown?.Remove(key) ?? false;
        }
        protected override void ClearFast()
        {
            MaybeUnknown?.Clear();
            _contentLength = null;
            var tempBits = _bits;
            _bits = 0;
            if(HttpHeaders.BitCount(tempBits) > 12)
            {
                _headers = default(HeaderReferences);
                return;
            }
            
            if ((tempBits & 0x1L) != 0)
            {
                _headers._ETag = default(StringValues);
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
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("ETag", _headers._ETag);
                    ++arrayIndex;
                }
                if (_contentLength.HasValue)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Length", HeaderUtilities.FormatNonNegativeInt64(_contentLength.Value));
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
                        _current = new KeyValuePair<string, StringValues>("ETag", _collection._headers._ETag);
                        _next = 1;
                        return true;
                    }
                
                ExtraHeaders:
                    if (!_hasUnknown || !_unknownEnumerator.MoveNext())
                    {
                        _current = default(KeyValuePair<string, StringValues>);
                        return false;
                    }
                    _current = _unknownEnumerator.Current;
                    return true;
            }
        }
    }
}