// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using System.Buffers;
using System.IO.Pipelines;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{

    public partial class HttpRequestHeaders
    {

        private long _bits = 0;
        private HeaderReferences _headers;

        public bool HasConnection => (_bits & 2L) != 0;
        public bool HasTransferEncoding => (_bits & 64L) != 0;

        public int HostCount => _headers._Host.Count;
        
        public StringValues HeaderCacheControl
        {
            get
            {
                StringValues value;
                if ((_bits & 1L) != 0)
                {
                    value = _headers._CacheControl;
                }
                return value;
            }
            set
            {
                _bits |= 1L;
                _headers._CacheControl = value; 
            }
        }
        public StringValues HeaderConnection
        {
            get
            {
                StringValues value;
                if ((_bits & 2L) != 0)
                {
                    value = _headers._Connection;
                }
                return value;
            }
            set
            {
                _bits |= 2L;
                _headers._Connection = value; 
            }
        }
        public StringValues HeaderDate
        {
            get
            {
                StringValues value;
                if ((_bits & 4L) != 0)
                {
                    value = _headers._Date;
                }
                return value;
            }
            set
            {
                _bits |= 4L;
                _headers._Date = value; 
            }
        }
        public StringValues HeaderKeepAlive
        {
            get
            {
                StringValues value;
                if ((_bits & 8L) != 0)
                {
                    value = _headers._KeepAlive;
                }
                return value;
            }
            set
            {
                _bits |= 8L;
                _headers._KeepAlive = value; 
            }
        }
        public StringValues HeaderPragma
        {
            get
            {
                StringValues value;
                if ((_bits & 16L) != 0)
                {
                    value = _headers._Pragma;
                }
                return value;
            }
            set
            {
                _bits |= 16L;
                _headers._Pragma = value; 
            }
        }
        public StringValues HeaderTrailer
        {
            get
            {
                StringValues value;
                if ((_bits & 32L) != 0)
                {
                    value = _headers._Trailer;
                }
                return value;
            }
            set
            {
                _bits |= 32L;
                _headers._Trailer = value; 
            }
        }
        public StringValues HeaderTransferEncoding
        {
            get
            {
                StringValues value;
                if ((_bits & 64L) != 0)
                {
                    value = _headers._TransferEncoding;
                }
                return value;
            }
            set
            {
                _bits |= 64L;
                _headers._TransferEncoding = value; 
            }
        }
        public StringValues HeaderUpgrade
        {
            get
            {
                StringValues value;
                if ((_bits & 128L) != 0)
                {
                    value = _headers._Upgrade;
                }
                return value;
            }
            set
            {
                _bits |= 128L;
                _headers._Upgrade = value; 
            }
        }
        public StringValues HeaderVia
        {
            get
            {
                StringValues value;
                if ((_bits & 256L) != 0)
                {
                    value = _headers._Via;
                }
                return value;
            }
            set
            {
                _bits |= 256L;
                _headers._Via = value; 
            }
        }
        public StringValues HeaderWarning
        {
            get
            {
                StringValues value;
                if ((_bits & 512L) != 0)
                {
                    value = _headers._Warning;
                }
                return value;
            }
            set
            {
                _bits |= 512L;
                _headers._Warning = value; 
            }
        }
        public StringValues HeaderAllow
        {
            get
            {
                StringValues value;
                if ((_bits & 1024L) != 0)
                {
                    value = _headers._Allow;
                }
                return value;
            }
            set
            {
                _bits |= 1024L;
                _headers._Allow = value; 
            }
        }
        public StringValues HeaderContentType
        {
            get
            {
                StringValues value;
                if ((_bits & 2048L) != 0)
                {
                    value = _headers._ContentType;
                }
                return value;
            }
            set
            {
                _bits |= 2048L;
                _headers._ContentType = value; 
            }
        }
        public StringValues HeaderContentEncoding
        {
            get
            {
                StringValues value;
                if ((_bits & 4096L) != 0)
                {
                    value = _headers._ContentEncoding;
                }
                return value;
            }
            set
            {
                _bits |= 4096L;
                _headers._ContentEncoding = value; 
            }
        }
        public StringValues HeaderContentLanguage
        {
            get
            {
                StringValues value;
                if ((_bits & 8192L) != 0)
                {
                    value = _headers._ContentLanguage;
                }
                return value;
            }
            set
            {
                _bits |= 8192L;
                _headers._ContentLanguage = value; 
            }
        }
        public StringValues HeaderContentLocation
        {
            get
            {
                StringValues value;
                if ((_bits & 16384L) != 0)
                {
                    value = _headers._ContentLocation;
                }
                return value;
            }
            set
            {
                _bits |= 16384L;
                _headers._ContentLocation = value; 
            }
        }
        public StringValues HeaderContentMD5
        {
            get
            {
                StringValues value;
                if ((_bits & 32768L) != 0)
                {
                    value = _headers._ContentMD5;
                }
                return value;
            }
            set
            {
                _bits |= 32768L;
                _headers._ContentMD5 = value; 
            }
        }
        public StringValues HeaderContentRange
        {
            get
            {
                StringValues value;
                if ((_bits & 65536L) != 0)
                {
                    value = _headers._ContentRange;
                }
                return value;
            }
            set
            {
                _bits |= 65536L;
                _headers._ContentRange = value; 
            }
        }
        public StringValues HeaderExpires
        {
            get
            {
                StringValues value;
                if ((_bits & 131072L) != 0)
                {
                    value = _headers._Expires;
                }
                return value;
            }
            set
            {
                _bits |= 131072L;
                _headers._Expires = value; 
            }
        }
        public StringValues HeaderLastModified
        {
            get
            {
                StringValues value;
                if ((_bits & 262144L) != 0)
                {
                    value = _headers._LastModified;
                }
                return value;
            }
            set
            {
                _bits |= 262144L;
                _headers._LastModified = value; 
            }
        }
        public StringValues HeaderAccept
        {
            get
            {
                StringValues value;
                if ((_bits & 524288L) != 0)
                {
                    value = _headers._Accept;
                }
                return value;
            }
            set
            {
                _bits |= 524288L;
                _headers._Accept = value; 
            }
        }
        public StringValues HeaderAcceptCharset
        {
            get
            {
                StringValues value;
                if ((_bits & 1048576L) != 0)
                {
                    value = _headers._AcceptCharset;
                }
                return value;
            }
            set
            {
                _bits |= 1048576L;
                _headers._AcceptCharset = value; 
            }
        }
        public StringValues HeaderAcceptEncoding
        {
            get
            {
                StringValues value;
                if ((_bits & 2097152L) != 0)
                {
                    value = _headers._AcceptEncoding;
                }
                return value;
            }
            set
            {
                _bits |= 2097152L;
                _headers._AcceptEncoding = value; 
            }
        }
        public StringValues HeaderAcceptLanguage
        {
            get
            {
                StringValues value;
                if ((_bits & 4194304L) != 0)
                {
                    value = _headers._AcceptLanguage;
                }
                return value;
            }
            set
            {
                _bits |= 4194304L;
                _headers._AcceptLanguage = value; 
            }
        }
        public StringValues HeaderAuthorization
        {
            get
            {
                StringValues value;
                if ((_bits & 8388608L) != 0)
                {
                    value = _headers._Authorization;
                }
                return value;
            }
            set
            {
                _bits |= 8388608L;
                _headers._Authorization = value; 
            }
        }
        public StringValues HeaderCookie
        {
            get
            {
                StringValues value;
                if ((_bits & 16777216L) != 0)
                {
                    value = _headers._Cookie;
                }
                return value;
            }
            set
            {
                _bits |= 16777216L;
                _headers._Cookie = value; 
            }
        }
        public StringValues HeaderExpect
        {
            get
            {
                StringValues value;
                if ((_bits & 33554432L) != 0)
                {
                    value = _headers._Expect;
                }
                return value;
            }
            set
            {
                _bits |= 33554432L;
                _headers._Expect = value; 
            }
        }
        public StringValues HeaderFrom
        {
            get
            {
                StringValues value;
                if ((_bits & 67108864L) != 0)
                {
                    value = _headers._From;
                }
                return value;
            }
            set
            {
                _bits |= 67108864L;
                _headers._From = value; 
            }
        }
        public StringValues HeaderHost
        {
            get
            {
                StringValues value;
                if ((_bits & 134217728L) != 0)
                {
                    value = _headers._Host;
                }
                return value;
            }
            set
            {
                _bits |= 134217728L;
                _headers._Host = value; 
            }
        }
        public StringValues HeaderIfMatch
        {
            get
            {
                StringValues value;
                if ((_bits & 268435456L) != 0)
                {
                    value = _headers._IfMatch;
                }
                return value;
            }
            set
            {
                _bits |= 268435456L;
                _headers._IfMatch = value; 
            }
        }
        public StringValues HeaderIfModifiedSince
        {
            get
            {
                StringValues value;
                if ((_bits & 536870912L) != 0)
                {
                    value = _headers._IfModifiedSince;
                }
                return value;
            }
            set
            {
                _bits |= 536870912L;
                _headers._IfModifiedSince = value; 
            }
        }
        public StringValues HeaderIfNoneMatch
        {
            get
            {
                StringValues value;
                if ((_bits & 1073741824L) != 0)
                {
                    value = _headers._IfNoneMatch;
                }
                return value;
            }
            set
            {
                _bits |= 1073741824L;
                _headers._IfNoneMatch = value; 
            }
        }
        public StringValues HeaderIfRange
        {
            get
            {
                StringValues value;
                if ((_bits & 2147483648L) != 0)
                {
                    value = _headers._IfRange;
                }
                return value;
            }
            set
            {
                _bits |= 2147483648L;
                _headers._IfRange = value; 
            }
        }
        public StringValues HeaderIfUnmodifiedSince
        {
            get
            {
                StringValues value;
                if ((_bits & 4294967296L) != 0)
                {
                    value = _headers._IfUnmodifiedSince;
                }
                return value;
            }
            set
            {
                _bits |= 4294967296L;
                _headers._IfUnmodifiedSince = value; 
            }
        }
        public StringValues HeaderMaxForwards
        {
            get
            {
                StringValues value;
                if ((_bits & 8589934592L) != 0)
                {
                    value = _headers._MaxForwards;
                }
                return value;
            }
            set
            {
                _bits |= 8589934592L;
                _headers._MaxForwards = value; 
            }
        }
        public StringValues HeaderProxyAuthorization
        {
            get
            {
                StringValues value;
                if ((_bits & 17179869184L) != 0)
                {
                    value = _headers._ProxyAuthorization;
                }
                return value;
            }
            set
            {
                _bits |= 17179869184L;
                _headers._ProxyAuthorization = value; 
            }
        }
        public StringValues HeaderReferer
        {
            get
            {
                StringValues value;
                if ((_bits & 34359738368L) != 0)
                {
                    value = _headers._Referer;
                }
                return value;
            }
            set
            {
                _bits |= 34359738368L;
                _headers._Referer = value; 
            }
        }
        public StringValues HeaderRange
        {
            get
            {
                StringValues value;
                if ((_bits & 68719476736L) != 0)
                {
                    value = _headers._Range;
                }
                return value;
            }
            set
            {
                _bits |= 68719476736L;
                _headers._Range = value; 
            }
        }
        public StringValues HeaderTE
        {
            get
            {
                StringValues value;
                if ((_bits & 137438953472L) != 0)
                {
                    value = _headers._TE;
                }
                return value;
            }
            set
            {
                _bits |= 137438953472L;
                _headers._TE = value; 
            }
        }
        public StringValues HeaderTranslate
        {
            get
            {
                StringValues value;
                if ((_bits & 274877906944L) != 0)
                {
                    value = _headers._Translate;
                }
                return value;
            }
            set
            {
                _bits |= 274877906944L;
                _headers._Translate = value; 
            }
        }
        public StringValues HeaderUserAgent
        {
            get
            {
                StringValues value;
                if ((_bits & 549755813888L) != 0)
                {
                    value = _headers._UserAgent;
                }
                return value;
            }
            set
            {
                _bits |= 549755813888L;
                _headers._UserAgent = value; 
            }
        }
        public StringValues HeaderOrigin
        {
            get
            {
                StringValues value;
                if ((_bits & 1099511627776L) != 0)
                {
                    value = _headers._Origin;
                }
                return value;
            }
            set
            {
                _bits |= 1099511627776L;
                _headers._Origin = value; 
            }
        }
        public StringValues HeaderAccessControlRequestMethod
        {
            get
            {
                StringValues value;
                if ((_bits & 2199023255552L) != 0)
                {
                    value = _headers._AccessControlRequestMethod;
                }
                return value;
            }
            set
            {
                _bits |= 2199023255552L;
                _headers._AccessControlRequestMethod = value; 
            }
        }
        public StringValues HeaderAccessControlRequestHeaders
        {
            get
            {
                StringValues value;
                if ((_bits & 4398046511104L) != 0)
                {
                    value = _headers._AccessControlRequestHeaders;
                }
                return value;
            }
            set
            {
                _bits |= 4398046511104L;
                _headers._AccessControlRequestHeaders = value; 
            }
        }
        public StringValues HeaderContentLength
        {
            get
            {
                StringValues value;
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
            switch (key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 1L) != 0)
                            {
                                value = _headers._CacheControl;
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 65536L) != 0)
                            {
                                value = _headers._ContentRange;
                                return true;
                            }
                            return false;
                        }
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 262144L) != 0)
                            {
                                value = _headers._LastModified;
                                return true;
                            }
                            return false;
                        }
                        if ("Authorization".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 8388608L) != 0)
                            {
                                value = _headers._Authorization;
                                return true;
                            }
                            return false;
                        }
                        if ("If-None-Match".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 1073741824L) != 0)
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
                            if ((_bits & 2L) != 0)
                            {
                                value = _headers._Connection;
                                return true;
                            }
                            return false;
                        }
                        if ("Keep-Alive".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 8L) != 0)
                            {
                                value = _headers._KeepAlive;
                                return true;
                            }
                            return false;
                        }
                        if ("User-Agent".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 549755813888L) != 0)
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
                            if ((_bits & 4L) != 0)
                            {
                                value = _headers._Date;
                                return true;
                            }
                            return false;
                        }
                        if ("From".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 67108864L) != 0)
                            {
                                value = _headers._From;
                                return true;
                            }
                            return false;
                        }
                        if ("Host".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 134217728L) != 0)
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
                            if ((_bits & 16L) != 0)
                            {
                                value = _headers._Pragma;
                                return true;
                            }
                            return false;
                        }
                        if ("Accept".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 524288L) != 0)
                            {
                                value = _headers._Accept;
                                return true;
                            }
                            return false;
                        }
                        if ("Cookie".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 16777216L) != 0)
                            {
                                value = _headers._Cookie;
                                return true;
                            }
                            return false;
                        }
                        if ("Expect".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 33554432L) != 0)
                            {
                                value = _headers._Expect;
                                return true;
                            }
                            return false;
                        }
                        if ("Origin".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 1099511627776L) != 0)
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
                            if ((_bits & 32L) != 0)
                            {
                                value = _headers._Trailer;
                                return true;
                            }
                            return false;
                        }
                        if ("Upgrade".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 128L) != 0)
                            {
                                value = _headers._Upgrade;
                                return true;
                            }
                            return false;
                        }
                        if ("Warning".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 512L) != 0)
                            {
                                value = _headers._Warning;
                                return true;
                            }
                            return false;
                        }
                        if ("Expires".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 131072L) != 0)
                            {
                                value = _headers._Expires;
                                return true;
                            }
                            return false;
                        }
                        if ("Referer".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 34359738368L) != 0)
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
                            if ((_bits & 64L) != 0)
                            {
                                value = _headers._TransferEncoding;
                                return true;
                            }
                            return false;
                        }
                        if ("If-Modified-Since".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 536870912L) != 0)
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
                            if ((_bits & 256L) != 0)
                            {
                                value = _headers._Via;
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
                            if ((_bits & 1024L) != 0)
                            {
                                value = _headers._Allow;
                                return true;
                            }
                            return false;
                        }
                        if ("Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 68719476736L) != 0)
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
                            if ((_bits & 2048L) != 0)
                            {
                                value = _headers._ContentType;
                                return true;
                            }
                            return false;
                        }
                        if ("Max-Forwards".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 8589934592L) != 0)
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
                            if ((_bits & 4096L) != 0)
                            {
                                value = _headers._ContentEncoding;
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 8192L) != 0)
                            {
                                value = _headers._ContentLanguage;
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 16384L) != 0)
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
                            if ((_bits & 32768L) != 0)
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
                            if ((_bits & 1048576L) != 0)
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
                            if ((_bits & 2097152L) != 0)
                            {
                                value = _headers._AcceptEncoding;
                                return true;
                            }
                            return false;
                        }
                        if ("Accept-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 4194304L) != 0)
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
                            if ((_bits & 268435456L) != 0)
                            {
                                value = _headers._IfMatch;
                                return true;
                            }
                            return false;
                        }
                        if ("If-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 2147483648L) != 0)
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
                            if ((_bits & 4294967296L) != 0)
                            {
                                value = _headers._IfUnmodifiedSince;
                                return true;
                            }
                            return false;
                        }
                        if ("Proxy-Authorization".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 17179869184L) != 0)
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
                            if ((_bits & 137438953472L) != 0)
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
                            if ((_bits & 274877906944L) != 0)
                            {
                                value = _headers._Translate;
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
                            if ((_bits & 2199023255552L) != 0)
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
                            if ((_bits & 4398046511104L) != 0)
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

        protected override void SetValueFast(string key, in StringValues value)
        {
            switch (key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 1L;
                            _headers._CacheControl = value;
                            return;
                        }
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 65536L;
                            _headers._ContentRange = value;
                            return;
                        }
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 262144L;
                            _headers._LastModified = value;
                            return;
                        }
                        if ("Authorization".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 8388608L;
                            _headers._Authorization = value;
                            return;
                        }
                        if ("If-None-Match".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 1073741824L;
                            _headers._IfNoneMatch = value;
                            return;
                        }
                    }
                    break;
                case 10:
                    {
                        if ("Connection".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 2L;
                            _headers._Connection = value;
                            return;
                        }
                        if ("Keep-Alive".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 8L;
                            _headers._KeepAlive = value;
                            return;
                        }
                        if ("User-Agent".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 549755813888L;
                            _headers._UserAgent = value;
                            return;
                        }
                    }
                    break;
                case 4:
                    {
                        if ("Date".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 4L;
                            _headers._Date = value;
                            return;
                        }
                        if ("From".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 67108864L;
                            _headers._From = value;
                            return;
                        }
                        if ("Host".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 134217728L;
                            _headers._Host = value;
                            return;
                        }
                    }
                    break;
                case 6:
                    {
                        if ("Pragma".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 16L;
                            _headers._Pragma = value;
                            return;
                        }
                        if ("Accept".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 524288L;
                            _headers._Accept = value;
                            return;
                        }
                        if ("Cookie".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 16777216L;
                            _headers._Cookie = value;
                            return;
                        }
                        if ("Expect".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 33554432L;
                            _headers._Expect = value;
                            return;
                        }
                        if ("Origin".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 1099511627776L;
                            _headers._Origin = value;
                            return;
                        }
                    }
                    break;
                case 7:
                    {
                        if ("Trailer".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 32L;
                            _headers._Trailer = value;
                            return;
                        }
                        if ("Upgrade".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 128L;
                            _headers._Upgrade = value;
                            return;
                        }
                        if ("Warning".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 512L;
                            _headers._Warning = value;
                            return;
                        }
                        if ("Expires".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 131072L;
                            _headers._Expires = value;
                            return;
                        }
                        if ("Referer".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 34359738368L;
                            _headers._Referer = value;
                            return;
                        }
                    }
                    break;
                case 17:
                    {
                        if ("Transfer-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 64L;
                            _headers._TransferEncoding = value;
                            return;
                        }
                        if ("If-Modified-Since".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 536870912L;
                            _headers._IfModifiedSince = value;
                            return;
                        }
                    }
                    break;
                case 3:
                    {
                        if ("Via".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 256L;
                            _headers._Via = value;
                            return;
                        }
                    }
                    break;
                case 5:
                    {
                        if ("Allow".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 1024L;
                            _headers._Allow = value;
                            return;
                        }
                        if ("Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 68719476736L;
                            _headers._Range = value;
                            return;
                        }
                    }
                    break;
                case 12:
                    {
                        if ("Content-Type".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 2048L;
                            _headers._ContentType = value;
                            return;
                        }
                        if ("Max-Forwards".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 8589934592L;
                            _headers._MaxForwards = value;
                            return;
                        }
                    }
                    break;
                case 16:
                    {
                        if ("Content-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 4096L;
                            _headers._ContentEncoding = value;
                            return;
                        }
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 8192L;
                            _headers._ContentLanguage = value;
                            return;
                        }
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 16384L;
                            _headers._ContentLocation = value;
                            return;
                        }
                    }
                    break;
                case 11:
                    {
                        if ("Content-MD5".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 32768L;
                            _headers._ContentMD5 = value;
                            return;
                        }
                    }
                    break;
                case 14:
                    {
                        if ("Accept-Charset".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 1048576L;
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
                            _bits |= 2097152L;
                            _headers._AcceptEncoding = value;
                            return;
                        }
                        if ("Accept-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 4194304L;
                            _headers._AcceptLanguage = value;
                            return;
                        }
                    }
                    break;
                case 8:
                    {
                        if ("If-Match".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 268435456L;
                            _headers._IfMatch = value;
                            return;
                        }
                        if ("If-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 2147483648L;
                            _headers._IfRange = value;
                            return;
                        }
                    }
                    break;
                case 19:
                    {
                        if ("If-Unmodified-Since".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 4294967296L;
                            _headers._IfUnmodifiedSince = value;
                            return;
                        }
                        if ("Proxy-Authorization".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 17179869184L;
                            _headers._ProxyAuthorization = value;
                            return;
                        }
                    }
                    break;
                case 2:
                    {
                        if ("TE".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 137438953472L;
                            _headers._TE = value;
                            return;
                        }
                    }
                    break;
                case 9:
                    {
                        if ("Translate".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 274877906944L;
                            _headers._Translate = value;
                            return;
                        }
                    }
                    break;
                case 29:
                    {
                        if ("Access-Control-Request-Method".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 2199023255552L;
                            _headers._AccessControlRequestMethod = value;
                            return;
                        }
                    }
                    break;
                case 30:
                    {
                        if ("Access-Control-Request-Headers".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 4398046511104L;
                            _headers._AccessControlRequestHeaders = value;
                            return;
                        }
                    }
                    break;
            }

            SetValueUnknown(key, value);
        }

        protected override bool AddValueFast(string key, in StringValues value)
        {
            switch (key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 1L) == 0)
                            {
                                _bits |= 1L;
                                _headers._CacheControl = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 65536L) == 0)
                            {
                                _bits |= 65536L;
                                _headers._ContentRange = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 262144L) == 0)
                            {
                                _bits |= 262144L;
                                _headers._LastModified = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Authorization".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 8388608L) == 0)
                            {
                                _bits |= 8388608L;
                                _headers._Authorization = value;
                                return true;
                            }
                            return false;
                        }
                        if ("If-None-Match".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 1073741824L) == 0)
                            {
                                _bits |= 1073741824L;
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
                            if ((_bits & 2L) == 0)
                            {
                                _bits |= 2L;
                                _headers._Connection = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Keep-Alive".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 8L) == 0)
                            {
                                _bits |= 8L;
                                _headers._KeepAlive = value;
                                return true;
                            }
                            return false;
                        }
                        if ("User-Agent".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 549755813888L) == 0)
                            {
                                _bits |= 549755813888L;
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
                            if ((_bits & 4L) == 0)
                            {
                                _bits |= 4L;
                                _headers._Date = value;
                                return true;
                            }
                            return false;
                        }
                        if ("From".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 67108864L) == 0)
                            {
                                _bits |= 67108864L;
                                _headers._From = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Host".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 134217728L) == 0)
                            {
                                _bits |= 134217728L;
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
                            if ((_bits & 16L) == 0)
                            {
                                _bits |= 16L;
                                _headers._Pragma = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Accept".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 524288L) == 0)
                            {
                                _bits |= 524288L;
                                _headers._Accept = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Cookie".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 16777216L) == 0)
                            {
                                _bits |= 16777216L;
                                _headers._Cookie = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Expect".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 33554432L) == 0)
                            {
                                _bits |= 33554432L;
                                _headers._Expect = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Origin".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 1099511627776L) == 0)
                            {
                                _bits |= 1099511627776L;
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
                            if ((_bits & 32L) == 0)
                            {
                                _bits |= 32L;
                                _headers._Trailer = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Upgrade".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 128L) == 0)
                            {
                                _bits |= 128L;
                                _headers._Upgrade = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Warning".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 512L) == 0)
                            {
                                _bits |= 512L;
                                _headers._Warning = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Expires".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 131072L) == 0)
                            {
                                _bits |= 131072L;
                                _headers._Expires = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Referer".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 34359738368L) == 0)
                            {
                                _bits |= 34359738368L;
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
                            if ((_bits & 64L) == 0)
                            {
                                _bits |= 64L;
                                _headers._TransferEncoding = value;
                                return true;
                            }
                            return false;
                        }
                        if ("If-Modified-Since".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 536870912L) == 0)
                            {
                                _bits |= 536870912L;
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
                            if ((_bits & 256L) == 0)
                            {
                                _bits |= 256L;
                                _headers._Via = value;
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
                            if ((_bits & 1024L) == 0)
                            {
                                _bits |= 1024L;
                                _headers._Allow = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 68719476736L) == 0)
                            {
                                _bits |= 68719476736L;
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
                            if ((_bits & 2048L) == 0)
                            {
                                _bits |= 2048L;
                                _headers._ContentType = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Max-Forwards".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 8589934592L) == 0)
                            {
                                _bits |= 8589934592L;
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
                            if ((_bits & 4096L) == 0)
                            {
                                _bits |= 4096L;
                                _headers._ContentEncoding = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 8192L) == 0)
                            {
                                _bits |= 8192L;
                                _headers._ContentLanguage = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 16384L) == 0)
                            {
                                _bits |= 16384L;
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
                            if ((_bits & 32768L) == 0)
                            {
                                _bits |= 32768L;
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
                            if ((_bits & 1048576L) == 0)
                            {
                                _bits |= 1048576L;
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
                            if ((_bits & 2097152L) == 0)
                            {
                                _bits |= 2097152L;
                                _headers._AcceptEncoding = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Accept-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 4194304L) == 0)
                            {
                                _bits |= 4194304L;
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
                            if ((_bits & 268435456L) == 0)
                            {
                                _bits |= 268435456L;
                                _headers._IfMatch = value;
                                return true;
                            }
                            return false;
                        }
                        if ("If-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 2147483648L) == 0)
                            {
                                _bits |= 2147483648L;
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
                            if ((_bits & 4294967296L) == 0)
                            {
                                _bits |= 4294967296L;
                                _headers._IfUnmodifiedSince = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Proxy-Authorization".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 17179869184L) == 0)
                            {
                                _bits |= 17179869184L;
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
                            if ((_bits & 137438953472L) == 0)
                            {
                                _bits |= 137438953472L;
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
                            if ((_bits & 274877906944L) == 0)
                            {
                                _bits |= 274877906944L;
                                _headers._Translate = value;
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
                            if ((_bits & 2199023255552L) == 0)
                            {
                                _bits |= 2199023255552L;
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
                            if ((_bits & 4398046511104L) == 0)
                            {
                                _bits |= 4398046511104L;
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
                            if ((_bits & 1L) != 0)
                            {
                                _bits &= ~1L;
                                _headers._CacheControl = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 65536L) != 0)
                            {
                                _bits &= ~65536L;
                                _headers._ContentRange = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 262144L) != 0)
                            {
                                _bits &= ~262144L;
                                _headers._LastModified = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Authorization".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 8388608L) != 0)
                            {
                                _bits &= ~8388608L;
                                _headers._Authorization = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("If-None-Match".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 1073741824L) != 0)
                            {
                                _bits &= ~1073741824L;
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
                            if ((_bits & 2L) != 0)
                            {
                                _bits &= ~2L;
                                _headers._Connection = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Keep-Alive".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 8L) != 0)
                            {
                                _bits &= ~8L;
                                _headers._KeepAlive = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("User-Agent".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 549755813888L) != 0)
                            {
                                _bits &= ~549755813888L;
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
                            if ((_bits & 4L) != 0)
                            {
                                _bits &= ~4L;
                                _headers._Date = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("From".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 67108864L) != 0)
                            {
                                _bits &= ~67108864L;
                                _headers._From = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Host".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 134217728L) != 0)
                            {
                                _bits &= ~134217728L;
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
                            if ((_bits & 16L) != 0)
                            {
                                _bits &= ~16L;
                                _headers._Pragma = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Accept".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 524288L) != 0)
                            {
                                _bits &= ~524288L;
                                _headers._Accept = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Cookie".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 16777216L) != 0)
                            {
                                _bits &= ~16777216L;
                                _headers._Cookie = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Expect".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 33554432L) != 0)
                            {
                                _bits &= ~33554432L;
                                _headers._Expect = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Origin".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 1099511627776L) != 0)
                            {
                                _bits &= ~1099511627776L;
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
                            if ((_bits & 32L) != 0)
                            {
                                _bits &= ~32L;
                                _headers._Trailer = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Upgrade".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 128L) != 0)
                            {
                                _bits &= ~128L;
                                _headers._Upgrade = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Warning".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 512L) != 0)
                            {
                                _bits &= ~512L;
                                _headers._Warning = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Expires".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 131072L) != 0)
                            {
                                _bits &= ~131072L;
                                _headers._Expires = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Referer".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 34359738368L) != 0)
                            {
                                _bits &= ~34359738368L;
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
                            if ((_bits & 64L) != 0)
                            {
                                _bits &= ~64L;
                                _headers._TransferEncoding = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("If-Modified-Since".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 536870912L) != 0)
                            {
                                _bits &= ~536870912L;
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
                            if ((_bits & 256L) != 0)
                            {
                                _bits &= ~256L;
                                _headers._Via = default(StringValues);
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
                            if ((_bits & 1024L) != 0)
                            {
                                _bits &= ~1024L;
                                _headers._Allow = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 68719476736L) != 0)
                            {
                                _bits &= ~68719476736L;
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
                            if ((_bits & 2048L) != 0)
                            {
                                _bits &= ~2048L;
                                _headers._ContentType = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Max-Forwards".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 8589934592L) != 0)
                            {
                                _bits &= ~8589934592L;
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
                            if ((_bits & 4096L) != 0)
                            {
                                _bits &= ~4096L;
                                _headers._ContentEncoding = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 8192L) != 0)
                            {
                                _bits &= ~8192L;
                                _headers._ContentLanguage = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 16384L) != 0)
                            {
                                _bits &= ~16384L;
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
                            if ((_bits & 32768L) != 0)
                            {
                                _bits &= ~32768L;
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
                            if ((_bits & 1048576L) != 0)
                            {
                                _bits &= ~1048576L;
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
                            if ((_bits & 2097152L) != 0)
                            {
                                _bits &= ~2097152L;
                                _headers._AcceptEncoding = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Accept-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 4194304L) != 0)
                            {
                                _bits &= ~4194304L;
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
                            if ((_bits & 268435456L) != 0)
                            {
                                _bits &= ~268435456L;
                                _headers._IfMatch = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("If-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 2147483648L) != 0)
                            {
                                _bits &= ~2147483648L;
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
                            if ((_bits & 4294967296L) != 0)
                            {
                                _bits &= ~4294967296L;
                                _headers._IfUnmodifiedSince = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Proxy-Authorization".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 17179869184L) != 0)
                            {
                                _bits &= ~17179869184L;
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
                            if ((_bits & 137438953472L) != 0)
                            {
                                _bits &= ~137438953472L;
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
                            if ((_bits & 274877906944L) != 0)
                            {
                                _bits &= ~274877906944L;
                                _headers._Translate = default(StringValues);
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
                            if ((_bits & 2199023255552L) != 0)
                            {
                                _bits &= ~2199023255552L;
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
                            if ((_bits & 4398046511104L) != 0)
                            {
                                _bits &= ~4398046511104L;
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
            
            if ((tempBits & 2L) != 0)
            {
                _headers._Connection = default(StringValues);
                if((tempBits & ~2L) == 0)
                {
                    return;
                }
                tempBits &= ~2L;
            }
            
            if ((tempBits & 524288L) != 0)
            {
                _headers._Accept = default(StringValues);
                if((tempBits & ~524288L) == 0)
                {
                    return;
                }
                tempBits &= ~524288L;
            }
            
            if ((tempBits & 134217728L) != 0)
            {
                _headers._Host = default(StringValues);
                if((tempBits & ~134217728L) == 0)
                {
                    return;
                }
                tempBits &= ~134217728L;
            }
            
            if ((tempBits & 549755813888L) != 0)
            {
                _headers._UserAgent = default(StringValues);
                if((tempBits & ~549755813888L) == 0)
                {
                    return;
                }
                tempBits &= ~549755813888L;
            }
            
            if ((tempBits & 1L) != 0)
            {
                _headers._CacheControl = default(StringValues);
                if((tempBits & ~1L) == 0)
                {
                    return;
                }
                tempBits &= ~1L;
            }
            
            if ((tempBits & 4L) != 0)
            {
                _headers._Date = default(StringValues);
                if((tempBits & ~4L) == 0)
                {
                    return;
                }
                tempBits &= ~4L;
            }
            
            if ((tempBits & 8L) != 0)
            {
                _headers._KeepAlive = default(StringValues);
                if((tempBits & ~8L) == 0)
                {
                    return;
                }
                tempBits &= ~8L;
            }
            
            if ((tempBits & 16L) != 0)
            {
                _headers._Pragma = default(StringValues);
                if((tempBits & ~16L) == 0)
                {
                    return;
                }
                tempBits &= ~16L;
            }
            
            if ((tempBits & 32L) != 0)
            {
                _headers._Trailer = default(StringValues);
                if((tempBits & ~32L) == 0)
                {
                    return;
                }
                tempBits &= ~32L;
            }
            
            if ((tempBits & 64L) != 0)
            {
                _headers._TransferEncoding = default(StringValues);
                if((tempBits & ~64L) == 0)
                {
                    return;
                }
                tempBits &= ~64L;
            }
            
            if ((tempBits & 128L) != 0)
            {
                _headers._Upgrade = default(StringValues);
                if((tempBits & ~128L) == 0)
                {
                    return;
                }
                tempBits &= ~128L;
            }
            
            if ((tempBits & 256L) != 0)
            {
                _headers._Via = default(StringValues);
                if((tempBits & ~256L) == 0)
                {
                    return;
                }
                tempBits &= ~256L;
            }
            
            if ((tempBits & 512L) != 0)
            {
                _headers._Warning = default(StringValues);
                if((tempBits & ~512L) == 0)
                {
                    return;
                }
                tempBits &= ~512L;
            }
            
            if ((tempBits & 1024L) != 0)
            {
                _headers._Allow = default(StringValues);
                if((tempBits & ~1024L) == 0)
                {
                    return;
                }
                tempBits &= ~1024L;
            }
            
            if ((tempBits & 2048L) != 0)
            {
                _headers._ContentType = default(StringValues);
                if((tempBits & ~2048L) == 0)
                {
                    return;
                }
                tempBits &= ~2048L;
            }
            
            if ((tempBits & 4096L) != 0)
            {
                _headers._ContentEncoding = default(StringValues);
                if((tempBits & ~4096L) == 0)
                {
                    return;
                }
                tempBits &= ~4096L;
            }
            
            if ((tempBits & 8192L) != 0)
            {
                _headers._ContentLanguage = default(StringValues);
                if((tempBits & ~8192L) == 0)
                {
                    return;
                }
                tempBits &= ~8192L;
            }
            
            if ((tempBits & 16384L) != 0)
            {
                _headers._ContentLocation = default(StringValues);
                if((tempBits & ~16384L) == 0)
                {
                    return;
                }
                tempBits &= ~16384L;
            }
            
            if ((tempBits & 32768L) != 0)
            {
                _headers._ContentMD5 = default(StringValues);
                if((tempBits & ~32768L) == 0)
                {
                    return;
                }
                tempBits &= ~32768L;
            }
            
            if ((tempBits & 65536L) != 0)
            {
                _headers._ContentRange = default(StringValues);
                if((tempBits & ~65536L) == 0)
                {
                    return;
                }
                tempBits &= ~65536L;
            }
            
            if ((tempBits & 131072L) != 0)
            {
                _headers._Expires = default(StringValues);
                if((tempBits & ~131072L) == 0)
                {
                    return;
                }
                tempBits &= ~131072L;
            }
            
            if ((tempBits & 262144L) != 0)
            {
                _headers._LastModified = default(StringValues);
                if((tempBits & ~262144L) == 0)
                {
                    return;
                }
                tempBits &= ~262144L;
            }
            
            if ((tempBits & 1048576L) != 0)
            {
                _headers._AcceptCharset = default(StringValues);
                if((tempBits & ~1048576L) == 0)
                {
                    return;
                }
                tempBits &= ~1048576L;
            }
            
            if ((tempBits & 2097152L) != 0)
            {
                _headers._AcceptEncoding = default(StringValues);
                if((tempBits & ~2097152L) == 0)
                {
                    return;
                }
                tempBits &= ~2097152L;
            }
            
            if ((tempBits & 4194304L) != 0)
            {
                _headers._AcceptLanguage = default(StringValues);
                if((tempBits & ~4194304L) == 0)
                {
                    return;
                }
                tempBits &= ~4194304L;
            }
            
            if ((tempBits & 8388608L) != 0)
            {
                _headers._Authorization = default(StringValues);
                if((tempBits & ~8388608L) == 0)
                {
                    return;
                }
                tempBits &= ~8388608L;
            }
            
            if ((tempBits & 16777216L) != 0)
            {
                _headers._Cookie = default(StringValues);
                if((tempBits & ~16777216L) == 0)
                {
                    return;
                }
                tempBits &= ~16777216L;
            }
            
            if ((tempBits & 33554432L) != 0)
            {
                _headers._Expect = default(StringValues);
                if((tempBits & ~33554432L) == 0)
                {
                    return;
                }
                tempBits &= ~33554432L;
            }
            
            if ((tempBits & 67108864L) != 0)
            {
                _headers._From = default(StringValues);
                if((tempBits & ~67108864L) == 0)
                {
                    return;
                }
                tempBits &= ~67108864L;
            }
            
            if ((tempBits & 268435456L) != 0)
            {
                _headers._IfMatch = default(StringValues);
                if((tempBits & ~268435456L) == 0)
                {
                    return;
                }
                tempBits &= ~268435456L;
            }
            
            if ((tempBits & 536870912L) != 0)
            {
                _headers._IfModifiedSince = default(StringValues);
                if((tempBits & ~536870912L) == 0)
                {
                    return;
                }
                tempBits &= ~536870912L;
            }
            
            if ((tempBits & 1073741824L) != 0)
            {
                _headers._IfNoneMatch = default(StringValues);
                if((tempBits & ~1073741824L) == 0)
                {
                    return;
                }
                tempBits &= ~1073741824L;
            }
            
            if ((tempBits & 2147483648L) != 0)
            {
                _headers._IfRange = default(StringValues);
                if((tempBits & ~2147483648L) == 0)
                {
                    return;
                }
                tempBits &= ~2147483648L;
            }
            
            if ((tempBits & 4294967296L) != 0)
            {
                _headers._IfUnmodifiedSince = default(StringValues);
                if((tempBits & ~4294967296L) == 0)
                {
                    return;
                }
                tempBits &= ~4294967296L;
            }
            
            if ((tempBits & 8589934592L) != 0)
            {
                _headers._MaxForwards = default(StringValues);
                if((tempBits & ~8589934592L) == 0)
                {
                    return;
                }
                tempBits &= ~8589934592L;
            }
            
            if ((tempBits & 17179869184L) != 0)
            {
                _headers._ProxyAuthorization = default(StringValues);
                if((tempBits & ~17179869184L) == 0)
                {
                    return;
                }
                tempBits &= ~17179869184L;
            }
            
            if ((tempBits & 34359738368L) != 0)
            {
                _headers._Referer = default(StringValues);
                if((tempBits & ~34359738368L) == 0)
                {
                    return;
                }
                tempBits &= ~34359738368L;
            }
            
            if ((tempBits & 68719476736L) != 0)
            {
                _headers._Range = default(StringValues);
                if((tempBits & ~68719476736L) == 0)
                {
                    return;
                }
                tempBits &= ~68719476736L;
            }
            
            if ((tempBits & 137438953472L) != 0)
            {
                _headers._TE = default(StringValues);
                if((tempBits & ~137438953472L) == 0)
                {
                    return;
                }
                tempBits &= ~137438953472L;
            }
            
            if ((tempBits & 274877906944L) != 0)
            {
                _headers._Translate = default(StringValues);
                if((tempBits & ~274877906944L) == 0)
                {
                    return;
                }
                tempBits &= ~274877906944L;
            }
            
            if ((tempBits & 1099511627776L) != 0)
            {
                _headers._Origin = default(StringValues);
                if((tempBits & ~1099511627776L) == 0)
                {
                    return;
                }
                tempBits &= ~1099511627776L;
            }
            
            if ((tempBits & 2199023255552L) != 0)
            {
                _headers._AccessControlRequestMethod = default(StringValues);
                if((tempBits & ~2199023255552L) == 0)
                {
                    return;
                }
                tempBits &= ~2199023255552L;
            }
            
            if ((tempBits & 4398046511104L) != 0)
            {
                _headers._AccessControlRequestHeaders = default(StringValues);
                if((tempBits & ~4398046511104L) == 0)
                {
                    return;
                }
                tempBits &= ~4398046511104L;
            }
            
        }

        protected override bool CopyToFast(KeyValuePair<string, StringValues>[] array, int arrayIndex)
        {
            if (arrayIndex < 0)
            {
                return false;
            }
            
                if ((_bits & 1L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Cache-Control", _headers._CacheControl);
                    ++arrayIndex;
                }
                if ((_bits & 2L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Connection", _headers._Connection);
                    ++arrayIndex;
                }
                if ((_bits & 4L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Date", _headers._Date);
                    ++arrayIndex;
                }
                if ((_bits & 8L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Keep-Alive", _headers._KeepAlive);
                    ++arrayIndex;
                }
                if ((_bits & 16L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Pragma", _headers._Pragma);
                    ++arrayIndex;
                }
                if ((_bits & 32L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Trailer", _headers._Trailer);
                    ++arrayIndex;
                }
                if ((_bits & 64L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Transfer-Encoding", _headers._TransferEncoding);
                    ++arrayIndex;
                }
                if ((_bits & 128L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Upgrade", _headers._Upgrade);
                    ++arrayIndex;
                }
                if ((_bits & 256L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Via", _headers._Via);
                    ++arrayIndex;
                }
                if ((_bits & 512L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Warning", _headers._Warning);
                    ++arrayIndex;
                }
                if ((_bits & 1024L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Allow", _headers._Allow);
                    ++arrayIndex;
                }
                if ((_bits & 2048L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Type", _headers._ContentType);
                    ++arrayIndex;
                }
                if ((_bits & 4096L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Encoding", _headers._ContentEncoding);
                    ++arrayIndex;
                }
                if ((_bits & 8192L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Language", _headers._ContentLanguage);
                    ++arrayIndex;
                }
                if ((_bits & 16384L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Location", _headers._ContentLocation);
                    ++arrayIndex;
                }
                if ((_bits & 32768L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-MD5", _headers._ContentMD5);
                    ++arrayIndex;
                }
                if ((_bits & 65536L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Range", _headers._ContentRange);
                    ++arrayIndex;
                }
                if ((_bits & 131072L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Expires", _headers._Expires);
                    ++arrayIndex;
                }
                if ((_bits & 262144L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Last-Modified", _headers._LastModified);
                    ++arrayIndex;
                }
                if ((_bits & 524288L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Accept", _headers._Accept);
                    ++arrayIndex;
                }
                if ((_bits & 1048576L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Accept-Charset", _headers._AcceptCharset);
                    ++arrayIndex;
                }
                if ((_bits & 2097152L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Accept-Encoding", _headers._AcceptEncoding);
                    ++arrayIndex;
                }
                if ((_bits & 4194304L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Accept-Language", _headers._AcceptLanguage);
                    ++arrayIndex;
                }
                if ((_bits & 8388608L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Authorization", _headers._Authorization);
                    ++arrayIndex;
                }
                if ((_bits & 16777216L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Cookie", _headers._Cookie);
                    ++arrayIndex;
                }
                if ((_bits & 33554432L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Expect", _headers._Expect);
                    ++arrayIndex;
                }
                if ((_bits & 67108864L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("From", _headers._From);
                    ++arrayIndex;
                }
                if ((_bits & 134217728L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Host", _headers._Host);
                    ++arrayIndex;
                }
                if ((_bits & 268435456L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("If-Match", _headers._IfMatch);
                    ++arrayIndex;
                }
                if ((_bits & 536870912L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("If-Modified-Since", _headers._IfModifiedSince);
                    ++arrayIndex;
                }
                if ((_bits & 1073741824L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("If-None-Match", _headers._IfNoneMatch);
                    ++arrayIndex;
                }
                if ((_bits & 2147483648L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("If-Range", _headers._IfRange);
                    ++arrayIndex;
                }
                if ((_bits & 4294967296L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("If-Unmodified-Since", _headers._IfUnmodifiedSince);
                    ++arrayIndex;
                }
                if ((_bits & 8589934592L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Max-Forwards", _headers._MaxForwards);
                    ++arrayIndex;
                }
                if ((_bits & 17179869184L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Proxy-Authorization", _headers._ProxyAuthorization);
                    ++arrayIndex;
                }
                if ((_bits & 34359738368L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Referer", _headers._Referer);
                    ++arrayIndex;
                }
                if ((_bits & 68719476736L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Range", _headers._Range);
                    ++arrayIndex;
                }
                if ((_bits & 137438953472L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("TE", _headers._TE);
                    ++arrayIndex;
                }
                if ((_bits & 274877906944L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Translate", _headers._Translate);
                    ++arrayIndex;
                }
                if ((_bits & 549755813888L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("User-Agent", _headers._UserAgent);
                    ++arrayIndex;
                }
                if ((_bits & 1099511627776L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Origin", _headers._Origin);
                    ++arrayIndex;
                }
                if ((_bits & 2199023255552L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Access-Control-Request-Method", _headers._AccessControlRequestMethod);
                    ++arrayIndex;
                }
                if ((_bits & 4398046511104L) != 0)
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
        
        
        public unsafe void Append(byte* pKeyBytes, int keyLength, string value)
        {
            var pUB = pKeyBytes;
            var pUL = (ulong*)pUB;
                var pUI = (uint*)pUB;
                var pUS = (ushort*)pUB;
                var stringValue = new StringValues(value);
                switch (keyLength)
                {
                    case 10:
                        {
                            if ((((pUL[0] & 16131858542891098079uL) == 5283922227757993795uL) && ((pUS[4] & 57311u) == 20047u)))
                            {
                                if ((_bits & 2L) != 0)
                                {
                                    _headers._Connection = AppendValue(_headers._Connection, value);
                                }
                                else
                                {
                                    _bits |= 2L;
                                    _headers._Connection = stringValue;
                                }
                                return;
                            }
                        
                            if ((((pUL[0] & 16131858680330051551uL) == 4992030374873092949uL) && ((pUS[4] & 57311u) == 21582u)))
                            {
                                if ((_bits & 549755813888L) != 0)
                                {
                                    _headers._UserAgent = AppendValue(_headers._UserAgent, value);
                                }
                                else
                                {
                                    _bits |= 549755813888L;
                                    _headers._UserAgent = stringValue;
                                }
                                return;
                            }
                        }
                        break;
                
                    case 6:
                        {
                            if ((((pUI[0] & 3755991007u) == 1162036033u) && ((pUS[2] & 57311u) == 21584u)))
                            {
                                if ((_bits & 524288L) != 0)
                                {
                                    _headers._Accept = AppendValue(_headers._Accept, value);
                                }
                                else
                                {
                                    _bits |= 524288L;
                                    _headers._Accept = stringValue;
                                }
                                return;
                            }
                        }
                        break;
                
                    case 4:
                        {
                            if ((((pUI[0] & 3755991007u) == 1414745928u)))
                            {
                                if ((_bits & 134217728L) != 0)
                                {
                                    _headers._Host = AppendValue(_headers._Host, value);
                                }
                                else
                                {
                                    _bits |= 134217728L;
                                    _headers._Host = stringValue;
                                }
                                return;
                            }
                        }
                        break;
                }

            AppendNonPrimaryHeaders(pKeyBytes, keyLength, value);
        }

        private unsafe void AppendNonPrimaryHeaders(byte* pKeyBytes, int keyLength, string value)
        {
                var pUB = pKeyBytes;
                var pUL = (ulong*)pUB;
                var pUI = (uint*)pUB;
                var pUS = (ushort*)pUB;
                var stringValue = new StringValues(value);
                switch (keyLength)
                {
                    case 13:
                        {
                            if ((((pUL[0] & 16131893727263186911uL) == 5711458528024281411uL) && ((pUI[2] & 3755991007u) == 1330795598u) && ((pUB[12] & 223u) == 76u)))
                            {
                                if ((_bits & 1L) != 0)
                                {
                                    _headers._CacheControl = AppendValue(_headers._CacheControl, value);
                                }
                                else
                                {
                                    _bits |= 1L;
                                    _headers._CacheControl = stringValue;
                                }
                                return;
                            }
                        
                            if ((((pUL[0] & 18437701552104792031uL) == 3266321689424580419uL) && ((pUI[2] & 3755991007u) == 1196310866u) && ((pUB[12] & 223u) == 69u)))
                            {
                                if ((_bits & 65536L) != 0)
                                {
                                    _headers._ContentRange = AppendValue(_headers._ContentRange, value);
                                }
                                else
                                {
                                    _bits |= 65536L;
                                    _headers._ContentRange = stringValue;
                                }
                                return;
                            }
                        
                            if ((((pUL[0] & 16131858680330051551uL) == 4922237774822850892uL) && ((pUI[2] & 3755991007u) == 1162430025u) && ((pUB[12] & 223u) == 68u)))
                            {
                                if ((_bits & 262144L) != 0)
                                {
                                    _headers._LastModified = AppendValue(_headers._LastModified, value);
                                }
                                else
                                {
                                    _bits |= 262144L;
                                    _headers._LastModified = stringValue;
                                }
                                return;
                            }
                        
                            if ((((pUL[0] & 16131858542891098079uL) == 6505821637182772545uL) && ((pUI[2] & 3755991007u) == 1330205761u) && ((pUB[12] & 223u) == 78u)))
                            {
                                if ((_bits & 8388608L) != 0)
                                {
                                    _headers._Authorization = AppendValue(_headers._Authorization, value);
                                }
                                else
                                {
                                    _bits |= 8388608L;
                                    _headers._Authorization = stringValue;
                                }
                                return;
                            }
                        
                            if ((((pUL[0] & 18437701552106889183uL) == 3262099607620765257uL) && ((pUI[2] & 3755991007u) == 1129595213u) && ((pUB[12] & 223u) == 72u)))
                            {
                                if ((_bits & 1073741824L) != 0)
                                {
                                    _headers._IfNoneMatch = AppendValue(_headers._IfNoneMatch, value);
                                }
                                else
                                {
                                    _bits |= 1073741824L;
                                    _headers._IfNoneMatch = stringValue;
                                }
                                return;
                            }
                        }
                        break;
                
                    case 4:
                        {
                            if ((((pUI[0] & 3755991007u) == 1163149636u)))
                            {
                                if ((_bits & 4L) != 0)
                                {
                                    _headers._Date = AppendValue(_headers._Date, value);
                                }
                                else
                                {
                                    _bits |= 4L;
                                    _headers._Date = stringValue;
                                }
                                return;
                            }
                        
                            if ((((pUI[0] & 3755991007u) == 1297044038u)))
                            {
                                if ((_bits & 67108864L) != 0)
                                {
                                    _headers._From = AppendValue(_headers._From, value);
                                }
                                else
                                {
                                    _bits |= 67108864L;
                                    _headers._From = stringValue;
                                }
                                return;
                            }
                        }
                        break;
                
                    case 10:
                        {
                            if ((((pUL[0] & 16131858680330051551uL) == 5281668125874799947uL) && ((pUS[4] & 57311u) == 17750u)))
                            {
                                if ((_bits & 8L) != 0)
                                {
                                    _headers._KeepAlive = AppendValue(_headers._KeepAlive, value);
                                }
                                else
                                {
                                    _bits |= 8L;
                                    _headers._KeepAlive = stringValue;
                                }
                                return;
                            }
                        }
                        break;
                
                    case 6:
                        {
                            if ((((pUI[0] & 3755991007u) == 1195463248u) && ((pUS[2] & 57311u) == 16717u)))
                            {
                                if ((_bits & 16L) != 0)
                                {
                                    _headers._Pragma = AppendValue(_headers._Pragma, value);
                                }
                                else
                                {
                                    _bits |= 16L;
                                    _headers._Pragma = stringValue;
                                }
                                return;
                            }
                        
                            if ((((pUI[0] & 3755991007u) == 1263488835u) && ((pUS[2] & 57311u) == 17737u)))
                            {
                                if ((_bits & 16777216L) != 0)
                                {
                                    _headers._Cookie = AppendValue(_headers._Cookie, value);
                                }
                                else
                                {
                                    _bits |= 16777216L;
                                    _headers._Cookie = stringValue;
                                }
                                return;
                            }
                        
                            if ((((pUI[0] & 3755991007u) == 1162893381u) && ((pUS[2] & 57311u) == 21571u)))
                            {
                                if ((_bits & 33554432L) != 0)
                                {
                                    _headers._Expect = AppendValue(_headers._Expect, value);
                                }
                                else
                                {
                                    _bits |= 33554432L;
                                    _headers._Expect = stringValue;
                                }
                                return;
                            }
                        
                            if ((((pUI[0] & 3755991007u) == 1195987535u) && ((pUS[2] & 57311u) == 20041u)))
                            {
                                if ((_bits & 1099511627776L) != 0)
                                {
                                    _headers._Origin = AppendValue(_headers._Origin, value);
                                }
                                else
                                {
                                    _bits |= 1099511627776L;
                                    _headers._Origin = stringValue;
                                }
                                return;
                            }
                        }
                        break;
                
                    case 7:
                        {
                            if ((((pUI[0] & 3755991007u) == 1229017684u) && ((pUS[2] & 57311u) == 17740u) && ((pUB[6] & 223u) == 82u)))
                            {
                                if ((_bits & 32L) != 0)
                                {
                                    _headers._Trailer = AppendValue(_headers._Trailer, value);
                                }
                                else
                                {
                                    _bits |= 32L;
                                    _headers._Trailer = stringValue;
                                }
                                return;
                            }
                        
                            if ((((pUI[0] & 3755991007u) == 1380405333u) && ((pUS[2] & 57311u) == 17473u) && ((pUB[6] & 223u) == 69u)))
                            {
                                if ((_bits & 128L) != 0)
                                {
                                    _headers._Upgrade = AppendValue(_headers._Upgrade, value);
                                }
                                else
                                {
                                    _bits |= 128L;
                                    _headers._Upgrade = stringValue;
                                }
                                return;
                            }
                        
                            if ((((pUI[0] & 3755991007u) == 1314013527u) && ((pUS[2] & 57311u) == 20041u) && ((pUB[6] & 223u) == 71u)))
                            {
                                if ((_bits & 512L) != 0)
                                {
                                    _headers._Warning = AppendValue(_headers._Warning, value);
                                }
                                else
                                {
                                    _bits |= 512L;
                                    _headers._Warning = stringValue;
                                }
                                return;
                            }
                        
                            if ((((pUI[0] & 3755991007u) == 1230002245u) && ((pUS[2] & 57311u) == 17746u) && ((pUB[6] & 223u) == 83u)))
                            {
                                if ((_bits & 131072L) != 0)
                                {
                                    _headers._Expires = AppendValue(_headers._Expires, value);
                                }
                                else
                                {
                                    _bits |= 131072L;
                                    _headers._Expires = stringValue;
                                }
                                return;
                            }
                        
                            if ((((pUI[0] & 3755991007u) == 1162233170u) && ((pUS[2] & 57311u) == 17746u) && ((pUB[6] & 223u) == 82u)))
                            {
                                if ((_bits & 34359738368L) != 0)
                                {
                                    _headers._Referer = AppendValue(_headers._Referer, value);
                                }
                                else
                                {
                                    _bits |= 34359738368L;
                                    _headers._Referer = stringValue;
                                }
                                return;
                            }
                        }
                        break;
                
                    case 17:
                        {
                            if ((((pUL[0] & 16131858542891098079uL) == 5928221808112259668uL) && ((pUL[1] & 16131858542891098111uL) == 5641115115480565037uL) && ((pUB[16] & 223u) == 71u)))
                            {
                                if ((_bits & 64L) != 0)
                                {
                                    _headers._TransferEncoding = AppendValue(_headers._TransferEncoding, value);
                                }
                                else
                                {
                                    _bits |= 64L;
                                    _headers._TransferEncoding = stringValue;
                                }
                                return;
                            }
                        
                            if ((((pUL[0] & 16131858542893195231uL) == 5064654363342751305uL) && ((pUL[1] & 16131858543427968991uL) == 4849894470315165001uL) && ((pUB[16] & 223u) == 69u)))
                            {
                                if ((_bits & 536870912L) != 0)
                                {
                                    _headers._IfModifiedSince = AppendValue(_headers._IfModifiedSince, value);
                                }
                                else
                                {
                                    _bits |= 536870912L;
                                    _headers._IfModifiedSince = stringValue;
                                }
                                return;
                            }
                        }
                        break;
                
                    case 3:
                        {
                            if ((((pUS[0] & 57311u) == 18774u) && ((pUB[2] & 223u) == 65u)))
                            {
                                if ((_bits & 256L) != 0)
                                {
                                    _headers._Via = AppendValue(_headers._Via, value);
                                }
                                else
                                {
                                    _bits |= 256L;
                                    _headers._Via = stringValue;
                                }
                                return;
                            }
                        }
                        break;
                
                    case 5:
                        {
                            if ((((pUI[0] & 3755991007u) == 1330400321u) && ((pUB[4] & 223u) == 87u)))
                            {
                                if ((_bits & 1024L) != 0)
                                {
                                    _headers._Allow = AppendValue(_headers._Allow, value);
                                }
                                else
                                {
                                    _bits |= 1024L;
                                    _headers._Allow = stringValue;
                                }
                                return;
                            }
                        
                            if ((((pUI[0] & 3755991007u) == 1196310866u) && ((pUB[4] & 223u) == 69u)))
                            {
                                if ((_bits & 68719476736L) != 0)
                                {
                                    _headers._Range = AppendValue(_headers._Range, value);
                                }
                                else
                                {
                                    _bits |= 68719476736L;
                                    _headers._Range = stringValue;
                                }
                                return;
                            }
                        }
                        break;
                
                    case 12:
                        {
                            if ((((pUL[0] & 18437701552104792031uL) == 3266321689424580419uL) && ((pUI[2] & 3755991007u) == 1162893652u)))
                            {
                                if ((_bits & 2048L) != 0)
                                {
                                    _headers._ContentType = AppendValue(_headers._ContentType, value);
                                }
                                else
                                {
                                    _bits |= 2048L;
                                    _headers._ContentType = stringValue;
                                }
                                return;
                            }
                        
                            if ((((pUL[0] & 16131858543427968991uL) == 6292178792217067853uL) && ((pUI[2] & 3755991007u) == 1396986433u)))
                            {
                                if ((_bits & 8589934592L) != 0)
                                {
                                    _headers._MaxForwards = AppendValue(_headers._MaxForwards, value);
                                }
                                else
                                {
                                    _bits |= 8589934592L;
                                    _headers._MaxForwards = stringValue;
                                }
                                return;
                            }
                        }
                        break;
                
                    case 16:
                        {
                            if ((((pUL[0] & 18437701552104792031uL) == 3266321689424580419uL) && ((pUL[1] & 16131858542891098079uL) == 5138124782612729413uL)))
                            {
                                if ((_bits & 4096L) != 0)
                                {
                                    _headers._ContentEncoding = AppendValue(_headers._ContentEncoding, value);
                                }
                                else
                                {
                                    _bits |= 4096L;
                                    _headers._ContentEncoding = stringValue;
                                }
                                return;
                            }
                        
                            if ((((pUL[0] & 18437701552104792031uL) == 3266321689424580419uL) && ((pUL[1] & 16131858542891098079uL) == 4992030546487820620uL)))
                            {
                                if ((_bits & 8192L) != 0)
                                {
                                    _headers._ContentLanguage = AppendValue(_headers._ContentLanguage, value);
                                }
                                else
                                {
                                    _bits |= 8192L;
                                    _headers._ContentLanguage = stringValue;
                                }
                                return;
                            }
                        
                            if ((((pUL[0] & 18437701552104792031uL) == 3266321689424580419uL) && ((pUL[1] & 16131858542891098079uL) == 5642809484339531596uL)))
                            {
                                if ((_bits & 16384L) != 0)
                                {
                                    _headers._ContentLocation = AppendValue(_headers._ContentLocation, value);
                                }
                                else
                                {
                                    _bits |= 16384L;
                                    _headers._ContentLocation = stringValue;
                                }
                                return;
                            }
                        }
                        break;
                
                    case 11:
                        {
                            if ((((pUL[0] & 18437701552104792031uL) == 3266321689424580419uL) && ((pUS[4] & 57311u) == 17485u) && ((pUB[10] & 255u) == 53u)))
                            {
                                if ((_bits & 32768L) != 0)
                                {
                                    _headers._ContentMD5 = AppendValue(_headers._ContentMD5, value);
                                }
                                else
                                {
                                    _bits |= 32768L;
                                    _headers._ContentMD5 = stringValue;
                                }
                                return;
                            }
                        }
                        break;
                
                    case 14:
                        {
                            if ((((pUL[0] & 16140865742145839071uL) == 4840617878229304129uL) && ((pUI[2] & 3755991007u) == 1397899592u) && ((pUS[6] & 57311u) == 21573u)))
                            {
                                if ((_bits & 1048576L) != 0)
                                {
                                    _headers._AcceptCharset = AppendValue(_headers._AcceptCharset, value);
                                }
                                else
                                {
                                    _bits |= 1048576L;
                                    _headers._AcceptCharset = stringValue;
                                }
                                return;
                            }
                        
                            if ((((pUL[0] & 18437701552104792031uL) == 3266321689424580419uL) && ((pUI[2] & 3755991007u) == 1196311884u) && ((pUS[6] & 57311u) == 18516u)))
                            {
                                if (_contentLength.HasValue)
                                {
                                    BadHttpRequestException.Throw(RequestRejectionReason.MultipleContentLengths);
                                }
                                else
                                {
                                    _contentLength = ParseContentLength(value);
                                }
                                return;
                            }
                        }
                        break;
                
                    case 15:
                        {
                            if ((((pUL[0] & 16140865742145839071uL) == 4984733066305160001uL) && ((pUI[2] & 3755991007u) == 1146045262u) && ((pUS[6] & 57311u) == 20041u) && ((pUB[14] & 223u) == 71u)))
                            {
                                if ((_bits & 2097152L) != 0)
                                {
                                    _headers._AcceptEncoding = AppendValue(_headers._AcceptEncoding, value);
                                }
                                else
                                {
                                    _bits |= 2097152L;
                                    _headers._AcceptEncoding = stringValue;
                                }
                                return;
                            }
                        
                            if ((((pUL[0] & 16140865742145839071uL) == 5489136224570655553uL) && ((pUI[2] & 3755991007u) == 1430736449u) && ((pUS[6] & 57311u) == 18241u) && ((pUB[14] & 223u) == 69u)))
                            {
                                if ((_bits & 4194304L) != 0)
                                {
                                    _headers._AcceptLanguage = AppendValue(_headers._AcceptLanguage, value);
                                }
                                else
                                {
                                    _bits |= 4194304L;
                                    _headers._AcceptLanguage = stringValue;
                                }
                                return;
                            }
                        }
                        break;
                
                    case 8:
                        {
                            if ((((pUL[0] & 16131858542893195231uL) == 5207098233614845513uL)))
                            {
                                if ((_bits & 268435456L) != 0)
                                {
                                    _headers._IfMatch = AppendValue(_headers._IfMatch, value);
                                }
                                else
                                {
                                    _bits |= 268435456L;
                                    _headers._IfMatch = stringValue;
                                }
                                return;
                            }
                        
                            if ((((pUL[0] & 16131858542893195231uL) == 4992044754422023753uL)))
                            {
                                if ((_bits & 2147483648L) != 0)
                                {
                                    _headers._IfRange = AppendValue(_headers._IfRange, value);
                                }
                                else
                                {
                                    _bits |= 2147483648L;
                                    _headers._IfRange = stringValue;
                                }
                                return;
                            }
                        }
                        break;
                
                    case 19:
                        {
                            if ((((pUL[0] & 16131858542893195231uL) == 4922237916571059785uL) && ((pUL[1] & 16131893727263186911uL) == 5283616559079179849uL) && ((pUS[8] & 57311u) == 17230u) && ((pUB[18] & 223u) == 69u)))
                            {
                                if ((_bits & 4294967296L) != 0)
                                {
                                    _headers._IfUnmodifiedSince = AppendValue(_headers._IfUnmodifiedSince, value);
                                }
                                else
                                {
                                    _bits |= 4294967296L;
                                    _headers._IfUnmodifiedSince = stringValue;
                                }
                                return;
                            }
                        
                            if ((((pUL[0] & 16131893727263186911uL) == 6143241228466999888uL) && ((pUL[1] & 16131858542891098079uL) == 6071233043632179284uL) && ((pUS[8] & 57311u) == 20297u) && ((pUB[18] & 223u) == 78u)))
                            {
                                if ((_bits & 17179869184L) != 0)
                                {
                                    _headers._ProxyAuthorization = AppendValue(_headers._ProxyAuthorization, value);
                                }
                                else
                                {
                                    _bits |= 17179869184L;
                                    _headers._ProxyAuthorization = stringValue;
                                }
                                return;
                            }
                        }
                        break;
                
                    case 2:
                        {
                            if ((((pUS[0] & 57311u) == 17748u)))
                            {
                                if ((_bits & 137438953472L) != 0)
                                {
                                    _headers._TE = AppendValue(_headers._TE, value);
                                }
                                else
                                {
                                    _bits |= 137438953472L;
                                    _headers._TE = stringValue;
                                }
                                return;
                            }
                        }
                        break;
                
                    case 9:
                        {
                            if ((((pUL[0] & 16131858542891098079uL) == 6071217693351039572uL) && ((pUB[8] & 223u) == 69u)))
                            {
                                if ((_bits & 274877906944L) != 0)
                                {
                                    _headers._Translate = AppendValue(_headers._Translate, value);
                                }
                                else
                                {
                                    _bits |= 274877906944L;
                                    _headers._Translate = stringValue;
                                }
                                return;
                            }
                        }
                        break;
                
                    case 29:
                        {
                            if ((((pUL[0] & 16140865742145839071uL) == 4840616791602578241uL) && ((pUL[1] & 16140865742145839071uL) == 5921472988629454415uL) && ((pUL[2] & 16140865742145839071uL) == 5561193831494668613uL) && ((pUI[6] & 3755991007u) == 1330140229u) && ((pUB[28] & 223u) == 68u)))
                            {
                                if ((_bits & 2199023255552L) != 0)
                                {
                                    _headers._AccessControlRequestMethod = AppendValue(_headers._AccessControlRequestMethod, value);
                                }
                                else
                                {
                                    _bits |= 2199023255552L;
                                    _headers._AccessControlRequestMethod = stringValue;
                                }
                                return;
                            }
                        }
                        break;
                
                    case 30:
                        {
                            if ((((pUL[0] & 16140865742145839071uL) == 4840616791602578241uL) && ((pUL[1] & 16140865742145839071uL) == 5921472988629454415uL) && ((pUL[2] & 16140865742145839071uL) == 5200905861305028933uL) && ((pUI[6] & 3755991007u) == 1162101061u) && ((pUS[14] & 57311u) == 21330u)))
                            {
                                if ((_bits & 4398046511104L) != 0)
                                {
                                    _headers._AccessControlRequestHeaders = AppendValue(_headers._AccessControlRequestHeaders, value);
                                }
                                else
                                {
                                    _bits |= 4398046511104L;
                                    _headers._AccessControlRequestHeaders = stringValue;
                                }
                                return;
                            }
                        }
                        break;
                }

                AppendUnknownHeaders(pKeyBytes, keyLength, value);
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
            public StringValues _Origin;
            public StringValues _AccessControlRequestMethod;
            public StringValues _AccessControlRequestHeaders;
            
        }

        public partial struct Enumerator
        {
            public bool MoveNext()
            {
                switch (_state)
                {
                    
                    case 0:
                        goto state0;
                    
                    case 1:
                        goto state1;
                    
                    case 2:
                        goto state2;
                    
                    case 3:
                        goto state3;
                    
                    case 4:
                        goto state4;
                    
                    case 5:
                        goto state5;
                    
                    case 6:
                        goto state6;
                    
                    case 7:
                        goto state7;
                    
                    case 8:
                        goto state8;
                    
                    case 9:
                        goto state9;
                    
                    case 10:
                        goto state10;
                    
                    case 11:
                        goto state11;
                    
                    case 12:
                        goto state12;
                    
                    case 13:
                        goto state13;
                    
                    case 14:
                        goto state14;
                    
                    case 15:
                        goto state15;
                    
                    case 16:
                        goto state16;
                    
                    case 17:
                        goto state17;
                    
                    case 18:
                        goto state18;
                    
                    case 19:
                        goto state19;
                    
                    case 20:
                        goto state20;
                    
                    case 21:
                        goto state21;
                    
                    case 22:
                        goto state22;
                    
                    case 23:
                        goto state23;
                    
                    case 24:
                        goto state24;
                    
                    case 25:
                        goto state25;
                    
                    case 26:
                        goto state26;
                    
                    case 27:
                        goto state27;
                    
                    case 28:
                        goto state28;
                    
                    case 29:
                        goto state29;
                    
                    case 30:
                        goto state30;
                    
                    case 31:
                        goto state31;
                    
                    case 32:
                        goto state32;
                    
                    case 33:
                        goto state33;
                    
                    case 34:
                        goto state34;
                    
                    case 35:
                        goto state35;
                    
                    case 36:
                        goto state36;
                    
                    case 37:
                        goto state37;
                    
                    case 38:
                        goto state38;
                    
                    case 39:
                        goto state39;
                    
                    case 40:
                        goto state40;
                    
                    case 41:
                        goto state41;
                    
                    case 42:
                        goto state42;
                    
                    case 44:
                        goto state44;
                    default:
                        goto state_default;
                }
                
                state0:
                    if ((_bits & 1L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Cache-Control", _collection._headers._CacheControl);
                        _state = 1;
                        return true;
                    }
                
                state1:
                    if ((_bits & 2L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Connection", _collection._headers._Connection);
                        _state = 2;
                        return true;
                    }
                
                state2:
                    if ((_bits & 4L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Date", _collection._headers._Date);
                        _state = 3;
                        return true;
                    }
                
                state3:
                    if ((_bits & 8L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Keep-Alive", _collection._headers._KeepAlive);
                        _state = 4;
                        return true;
                    }
                
                state4:
                    if ((_bits & 16L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Pragma", _collection._headers._Pragma);
                        _state = 5;
                        return true;
                    }
                
                state5:
                    if ((_bits & 32L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Trailer", _collection._headers._Trailer);
                        _state = 6;
                        return true;
                    }
                
                state6:
                    if ((_bits & 64L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Transfer-Encoding", _collection._headers._TransferEncoding);
                        _state = 7;
                        return true;
                    }
                
                state7:
                    if ((_bits & 128L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Upgrade", _collection._headers._Upgrade);
                        _state = 8;
                        return true;
                    }
                
                state8:
                    if ((_bits & 256L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Via", _collection._headers._Via);
                        _state = 9;
                        return true;
                    }
                
                state9:
                    if ((_bits & 512L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Warning", _collection._headers._Warning);
                        _state = 10;
                        return true;
                    }
                
                state10:
                    if ((_bits & 1024L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Allow", _collection._headers._Allow);
                        _state = 11;
                        return true;
                    }
                
                state11:
                    if ((_bits & 2048L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Type", _collection._headers._ContentType);
                        _state = 12;
                        return true;
                    }
                
                state12:
                    if ((_bits & 4096L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Encoding", _collection._headers._ContentEncoding);
                        _state = 13;
                        return true;
                    }
                
                state13:
                    if ((_bits & 8192L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Language", _collection._headers._ContentLanguage);
                        _state = 14;
                        return true;
                    }
                
                state14:
                    if ((_bits & 16384L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Location", _collection._headers._ContentLocation);
                        _state = 15;
                        return true;
                    }
                
                state15:
                    if ((_bits & 32768L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-MD5", _collection._headers._ContentMD5);
                        _state = 16;
                        return true;
                    }
                
                state16:
                    if ((_bits & 65536L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Range", _collection._headers._ContentRange);
                        _state = 17;
                        return true;
                    }
                
                state17:
                    if ((_bits & 131072L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Expires", _collection._headers._Expires);
                        _state = 18;
                        return true;
                    }
                
                state18:
                    if ((_bits & 262144L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Last-Modified", _collection._headers._LastModified);
                        _state = 19;
                        return true;
                    }
                
                state19:
                    if ((_bits & 524288L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Accept", _collection._headers._Accept);
                        _state = 20;
                        return true;
                    }
                
                state20:
                    if ((_bits & 1048576L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Accept-Charset", _collection._headers._AcceptCharset);
                        _state = 21;
                        return true;
                    }
                
                state21:
                    if ((_bits & 2097152L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Accept-Encoding", _collection._headers._AcceptEncoding);
                        _state = 22;
                        return true;
                    }
                
                state22:
                    if ((_bits & 4194304L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Accept-Language", _collection._headers._AcceptLanguage);
                        _state = 23;
                        return true;
                    }
                
                state23:
                    if ((_bits & 8388608L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Authorization", _collection._headers._Authorization);
                        _state = 24;
                        return true;
                    }
                
                state24:
                    if ((_bits & 16777216L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Cookie", _collection._headers._Cookie);
                        _state = 25;
                        return true;
                    }
                
                state25:
                    if ((_bits & 33554432L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Expect", _collection._headers._Expect);
                        _state = 26;
                        return true;
                    }
                
                state26:
                    if ((_bits & 67108864L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("From", _collection._headers._From);
                        _state = 27;
                        return true;
                    }
                
                state27:
                    if ((_bits & 134217728L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Host", _collection._headers._Host);
                        _state = 28;
                        return true;
                    }
                
                state28:
                    if ((_bits & 268435456L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("If-Match", _collection._headers._IfMatch);
                        _state = 29;
                        return true;
                    }
                
                state29:
                    if ((_bits & 536870912L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("If-Modified-Since", _collection._headers._IfModifiedSince);
                        _state = 30;
                        return true;
                    }
                
                state30:
                    if ((_bits & 1073741824L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("If-None-Match", _collection._headers._IfNoneMatch);
                        _state = 31;
                        return true;
                    }
                
                state31:
                    if ((_bits & 2147483648L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("If-Range", _collection._headers._IfRange);
                        _state = 32;
                        return true;
                    }
                
                state32:
                    if ((_bits & 4294967296L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("If-Unmodified-Since", _collection._headers._IfUnmodifiedSince);
                        _state = 33;
                        return true;
                    }
                
                state33:
                    if ((_bits & 8589934592L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Max-Forwards", _collection._headers._MaxForwards);
                        _state = 34;
                        return true;
                    }
                
                state34:
                    if ((_bits & 17179869184L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Proxy-Authorization", _collection._headers._ProxyAuthorization);
                        _state = 35;
                        return true;
                    }
                
                state35:
                    if ((_bits & 34359738368L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Referer", _collection._headers._Referer);
                        _state = 36;
                        return true;
                    }
                
                state36:
                    if ((_bits & 68719476736L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Range", _collection._headers._Range);
                        _state = 37;
                        return true;
                    }
                
                state37:
                    if ((_bits & 137438953472L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("TE", _collection._headers._TE);
                        _state = 38;
                        return true;
                    }
                
                state38:
                    if ((_bits & 274877906944L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Translate", _collection._headers._Translate);
                        _state = 39;
                        return true;
                    }
                
                state39:
                    if ((_bits & 549755813888L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("User-Agent", _collection._headers._UserAgent);
                        _state = 40;
                        return true;
                    }
                
                state40:
                    if ((_bits & 1099511627776L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Origin", _collection._headers._Origin);
                        _state = 41;
                        return true;
                    }
                
                state41:
                    if ((_bits & 2199023255552L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Request-Method", _collection._headers._AccessControlRequestMethod);
                        _state = 42;
                        return true;
                    }
                
                state42:
                    if ((_bits & 4398046511104L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Request-Headers", _collection._headers._AccessControlRequestHeaders);
                        _state = 43;
                        return true;
                    }
                
                state44:
                    if (_collection._contentLength.HasValue)
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Length", HeaderUtilities.FormatNonNegativeInt64(_collection._contentLength.Value));
                        _state = 45;
                        return true;
                    }
                state_default:
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

    public partial class HttpResponseHeaders
    {
        private static byte[] _headerBytes = new byte[]
        {
            13,10,67,97,99,104,101,45,67,111,110,116,114,111,108,58,32,13,10,67,111,110,110,101,99,116,105,111,110,58,32,13,10,68,97,116,101,58,32,13,10,75,101,101,112,45,65,108,105,118,101,58,32,13,10,80,114,97,103,109,97,58,32,13,10,84,114,97,105,108,101,114,58,32,13,10,84,114,97,110,115,102,101,114,45,69,110,99,111,100,105,110,103,58,32,13,10,85,112,103,114,97,100,101,58,32,13,10,86,105,97,58,32,13,10,87,97,114,110,105,110,103,58,32,13,10,65,108,108,111,119,58,32,13,10,67,111,110,116,101,110,116,45,84,121,112,101,58,32,13,10,67,111,110,116,101,110,116,45,69,110,99,111,100,105,110,103,58,32,13,10,67,111,110,116,101,110,116,45,76,97,110,103,117,97,103,101,58,32,13,10,67,111,110,116,101,110,116,45,76,111,99,97,116,105,111,110,58,32,13,10,67,111,110,116,101,110,116,45,77,68,53,58,32,13,10,67,111,110,116,101,110,116,45,82,97,110,103,101,58,32,13,10,69,120,112,105,114,101,115,58,32,13,10,76,97,115,116,45,77,111,100,105,102,105,101,100,58,32,13,10,65,99,99,101,112,116,45,82,97,110,103,101,115,58,32,13,10,65,103,101,58,32,13,10,69,84,97,103,58,32,13,10,76,111,99,97,116,105,111,110,58,32,13,10,80,114,111,120,121,45,65,117,116,104,101,110,116,105,99,97,116,101,58,32,13,10,82,101,116,114,121,45,65,102,116,101,114,58,32,13,10,83,101,114,118,101,114,58,32,13,10,83,101,116,45,67,111,111,107,105,101,58,32,13,10,86,97,114,121,58,32,13,10,87,87,87,45,65,117,116,104,101,110,116,105,99,97,116,101,58,32,13,10,65,99,99,101,115,115,45,67,111,110,116,114,111,108,45,65,108,108,111,119,45,67,114,101,100,101,110,116,105,97,108,115,58,32,13,10,65,99,99,101,115,115,45,67,111,110,116,114,111,108,45,65,108,108,111,119,45,72,101,97,100,101,114,115,58,32,13,10,65,99,99,101,115,115,45,67,111,110,116,114,111,108,45,65,108,108,111,119,45,77,101,116,104,111,100,115,58,32,13,10,65,99,99,101,115,115,45,67,111,110,116,114,111,108,45,65,108,108,111,119,45,79,114,105,103,105,110,58,32,13,10,65,99,99,101,115,115,45,67,111,110,116,114,111,108,45,69,120,112,111,115,101,45,72,101,97,100,101,114,115,58,32,13,10,65,99,99,101,115,115,45,67,111,110,116,114,111,108,45,77,97,120,45,65,103,101,58,32,13,10,67,111,110,116,101,110,116,45,76,101,110,103,116,104,58,32,
        };

        private long _bits = 0;
        private HeaderReferences _headers;

        public bool HasConnection => (_bits & 2L) != 0;
        public bool HasDate => (_bits & 4L) != 0;
        public bool HasTransferEncoding => (_bits & 64L) != 0;
        public bool HasServer => (_bits & 33554432L) != 0;

        
        public StringValues HeaderCacheControl
        {
            get
            {
                StringValues value;
                if ((_bits & 1L) != 0)
                {
                    value = _headers._CacheControl;
                }
                return value;
            }
            set
            {
                _bits |= 1L;
                _headers._CacheControl = value; 
            }
        }
        public StringValues HeaderConnection
        {
            get
            {
                StringValues value;
                if ((_bits & 2L) != 0)
                {
                    value = _headers._Connection;
                }
                return value;
            }
            set
            {
                _bits |= 2L;
                _headers._Connection = value; 
                _headers._rawConnection = null;
            }
        }
        public StringValues HeaderDate
        {
            get
            {
                StringValues value;
                if ((_bits & 4L) != 0)
                {
                    value = _headers._Date;
                }
                return value;
            }
            set
            {
                _bits |= 4L;
                _headers._Date = value; 
                _headers._rawDate = null;
            }
        }
        public StringValues HeaderKeepAlive
        {
            get
            {
                StringValues value;
                if ((_bits & 8L) != 0)
                {
                    value = _headers._KeepAlive;
                }
                return value;
            }
            set
            {
                _bits |= 8L;
                _headers._KeepAlive = value; 
            }
        }
        public StringValues HeaderPragma
        {
            get
            {
                StringValues value;
                if ((_bits & 16L) != 0)
                {
                    value = _headers._Pragma;
                }
                return value;
            }
            set
            {
                _bits |= 16L;
                _headers._Pragma = value; 
            }
        }
        public StringValues HeaderTrailer
        {
            get
            {
                StringValues value;
                if ((_bits & 32L) != 0)
                {
                    value = _headers._Trailer;
                }
                return value;
            }
            set
            {
                _bits |= 32L;
                _headers._Trailer = value; 
            }
        }
        public StringValues HeaderTransferEncoding
        {
            get
            {
                StringValues value;
                if ((_bits & 64L) != 0)
                {
                    value = _headers._TransferEncoding;
                }
                return value;
            }
            set
            {
                _bits |= 64L;
                _headers._TransferEncoding = value; 
                _headers._rawTransferEncoding = null;
            }
        }
        public StringValues HeaderUpgrade
        {
            get
            {
                StringValues value;
                if ((_bits & 128L) != 0)
                {
                    value = _headers._Upgrade;
                }
                return value;
            }
            set
            {
                _bits |= 128L;
                _headers._Upgrade = value; 
            }
        }
        public StringValues HeaderVia
        {
            get
            {
                StringValues value;
                if ((_bits & 256L) != 0)
                {
                    value = _headers._Via;
                }
                return value;
            }
            set
            {
                _bits |= 256L;
                _headers._Via = value; 
            }
        }
        public StringValues HeaderWarning
        {
            get
            {
                StringValues value;
                if ((_bits & 512L) != 0)
                {
                    value = _headers._Warning;
                }
                return value;
            }
            set
            {
                _bits |= 512L;
                _headers._Warning = value; 
            }
        }
        public StringValues HeaderAllow
        {
            get
            {
                StringValues value;
                if ((_bits & 1024L) != 0)
                {
                    value = _headers._Allow;
                }
                return value;
            }
            set
            {
                _bits |= 1024L;
                _headers._Allow = value; 
            }
        }
        public StringValues HeaderContentType
        {
            get
            {
                StringValues value;
                if ((_bits & 2048L) != 0)
                {
                    value = _headers._ContentType;
                }
                return value;
            }
            set
            {
                _bits |= 2048L;
                _headers._ContentType = value; 
            }
        }
        public StringValues HeaderContentEncoding
        {
            get
            {
                StringValues value;
                if ((_bits & 4096L) != 0)
                {
                    value = _headers._ContentEncoding;
                }
                return value;
            }
            set
            {
                _bits |= 4096L;
                _headers._ContentEncoding = value; 
            }
        }
        public StringValues HeaderContentLanguage
        {
            get
            {
                StringValues value;
                if ((_bits & 8192L) != 0)
                {
                    value = _headers._ContentLanguage;
                }
                return value;
            }
            set
            {
                _bits |= 8192L;
                _headers._ContentLanguage = value; 
            }
        }
        public StringValues HeaderContentLocation
        {
            get
            {
                StringValues value;
                if ((_bits & 16384L) != 0)
                {
                    value = _headers._ContentLocation;
                }
                return value;
            }
            set
            {
                _bits |= 16384L;
                _headers._ContentLocation = value; 
            }
        }
        public StringValues HeaderContentMD5
        {
            get
            {
                StringValues value;
                if ((_bits & 32768L) != 0)
                {
                    value = _headers._ContentMD5;
                }
                return value;
            }
            set
            {
                _bits |= 32768L;
                _headers._ContentMD5 = value; 
            }
        }
        public StringValues HeaderContentRange
        {
            get
            {
                StringValues value;
                if ((_bits & 65536L) != 0)
                {
                    value = _headers._ContentRange;
                }
                return value;
            }
            set
            {
                _bits |= 65536L;
                _headers._ContentRange = value; 
            }
        }
        public StringValues HeaderExpires
        {
            get
            {
                StringValues value;
                if ((_bits & 131072L) != 0)
                {
                    value = _headers._Expires;
                }
                return value;
            }
            set
            {
                _bits |= 131072L;
                _headers._Expires = value; 
            }
        }
        public StringValues HeaderLastModified
        {
            get
            {
                StringValues value;
                if ((_bits & 262144L) != 0)
                {
                    value = _headers._LastModified;
                }
                return value;
            }
            set
            {
                _bits |= 262144L;
                _headers._LastModified = value; 
            }
        }
        public StringValues HeaderAcceptRanges
        {
            get
            {
                StringValues value;
                if ((_bits & 524288L) != 0)
                {
                    value = _headers._AcceptRanges;
                }
                return value;
            }
            set
            {
                _bits |= 524288L;
                _headers._AcceptRanges = value; 
            }
        }
        public StringValues HeaderAge
        {
            get
            {
                StringValues value;
                if ((_bits & 1048576L) != 0)
                {
                    value = _headers._Age;
                }
                return value;
            }
            set
            {
                _bits |= 1048576L;
                _headers._Age = value; 
            }
        }
        public StringValues HeaderETag
        {
            get
            {
                StringValues value;
                if ((_bits & 2097152L) != 0)
                {
                    value = _headers._ETag;
                }
                return value;
            }
            set
            {
                _bits |= 2097152L;
                _headers._ETag = value; 
            }
        }
        public StringValues HeaderLocation
        {
            get
            {
                StringValues value;
                if ((_bits & 4194304L) != 0)
                {
                    value = _headers._Location;
                }
                return value;
            }
            set
            {
                _bits |= 4194304L;
                _headers._Location = value; 
            }
        }
        public StringValues HeaderProxyAuthenticate
        {
            get
            {
                StringValues value;
                if ((_bits & 8388608L) != 0)
                {
                    value = _headers._ProxyAuthenticate;
                }
                return value;
            }
            set
            {
                _bits |= 8388608L;
                _headers._ProxyAuthenticate = value; 
            }
        }
        public StringValues HeaderRetryAfter
        {
            get
            {
                StringValues value;
                if ((_bits & 16777216L) != 0)
                {
                    value = _headers._RetryAfter;
                }
                return value;
            }
            set
            {
                _bits |= 16777216L;
                _headers._RetryAfter = value; 
            }
        }
        public StringValues HeaderServer
        {
            get
            {
                StringValues value;
                if ((_bits & 33554432L) != 0)
                {
                    value = _headers._Server;
                }
                return value;
            }
            set
            {
                _bits |= 33554432L;
                _headers._Server = value; 
                _headers._rawServer = null;
            }
        }
        public StringValues HeaderSetCookie
        {
            get
            {
                StringValues value;
                if ((_bits & 67108864L) != 0)
                {
                    value = _headers._SetCookie;
                }
                return value;
            }
            set
            {
                _bits |= 67108864L;
                _headers._SetCookie = value; 
            }
        }
        public StringValues HeaderVary
        {
            get
            {
                StringValues value;
                if ((_bits & 134217728L) != 0)
                {
                    value = _headers._Vary;
                }
                return value;
            }
            set
            {
                _bits |= 134217728L;
                _headers._Vary = value; 
            }
        }
        public StringValues HeaderWWWAuthenticate
        {
            get
            {
                StringValues value;
                if ((_bits & 268435456L) != 0)
                {
                    value = _headers._WWWAuthenticate;
                }
                return value;
            }
            set
            {
                _bits |= 268435456L;
                _headers._WWWAuthenticate = value; 
            }
        }
        public StringValues HeaderAccessControlAllowCredentials
        {
            get
            {
                StringValues value;
                if ((_bits & 536870912L) != 0)
                {
                    value = _headers._AccessControlAllowCredentials;
                }
                return value;
            }
            set
            {
                _bits |= 536870912L;
                _headers._AccessControlAllowCredentials = value; 
            }
        }
        public StringValues HeaderAccessControlAllowHeaders
        {
            get
            {
                StringValues value;
                if ((_bits & 1073741824L) != 0)
                {
                    value = _headers._AccessControlAllowHeaders;
                }
                return value;
            }
            set
            {
                _bits |= 1073741824L;
                _headers._AccessControlAllowHeaders = value; 
            }
        }
        public StringValues HeaderAccessControlAllowMethods
        {
            get
            {
                StringValues value;
                if ((_bits & 2147483648L) != 0)
                {
                    value = _headers._AccessControlAllowMethods;
                }
                return value;
            }
            set
            {
                _bits |= 2147483648L;
                _headers._AccessControlAllowMethods = value; 
            }
        }
        public StringValues HeaderAccessControlAllowOrigin
        {
            get
            {
                StringValues value;
                if ((_bits & 4294967296L) != 0)
                {
                    value = _headers._AccessControlAllowOrigin;
                }
                return value;
            }
            set
            {
                _bits |= 4294967296L;
                _headers._AccessControlAllowOrigin = value; 
            }
        }
        public StringValues HeaderAccessControlExposeHeaders
        {
            get
            {
                StringValues value;
                if ((_bits & 8589934592L) != 0)
                {
                    value = _headers._AccessControlExposeHeaders;
                }
                return value;
            }
            set
            {
                _bits |= 8589934592L;
                _headers._AccessControlExposeHeaders = value; 
            }
        }
        public StringValues HeaderAccessControlMaxAge
        {
            get
            {
                StringValues value;
                if ((_bits & 17179869184L) != 0)
                {
                    value = _headers._AccessControlMaxAge;
                }
                return value;
            }
            set
            {
                _bits |= 17179869184L;
                _headers._AccessControlMaxAge = value; 
            }
        }
        public StringValues HeaderContentLength
        {
            get
            {
                StringValues value;
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

        public void SetRawConnection(in StringValues value, byte[] raw)
        {
            _bits |= 2L;
            _headers._Connection = value;
            _headers._rawConnection = raw;
        }
        public void SetRawDate(in StringValues value, byte[] raw)
        {
            _bits |= 4L;
            _headers._Date = value;
            _headers._rawDate = raw;
        }
        public void SetRawTransferEncoding(in StringValues value, byte[] raw)
        {
            _bits |= 64L;
            _headers._TransferEncoding = value;
            _headers._rawTransferEncoding = raw;
        }
        public void SetRawServer(in StringValues value, byte[] raw)
        {
            _bits |= 33554432L;
            _headers._Server = value;
            _headers._rawServer = raw;
        }
        protected override int GetCountFast()
        {
            return (_contentLength.HasValue ? 1 : 0 ) + BitCount(_bits) + (MaybeUnknown?.Count ?? 0);
        }

        protected override bool TryGetValueFast(string key, out StringValues value)
        {
            switch (key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 1L) != 0)
                            {
                                value = _headers._CacheControl;
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 65536L) != 0)
                            {
                                value = _headers._ContentRange;
                                return true;
                            }
                            return false;
                        }
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 262144L) != 0)
                            {
                                value = _headers._LastModified;
                                return true;
                            }
                            return false;
                        }
                        if ("Accept-Ranges".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 524288L) != 0)
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
                            if ((_bits & 2L) != 0)
                            {
                                value = _headers._Connection;
                                return true;
                            }
                            return false;
                        }
                        if ("Keep-Alive".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 8L) != 0)
                            {
                                value = _headers._KeepAlive;
                                return true;
                            }
                            return false;
                        }
                        if ("Set-Cookie".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 67108864L) != 0)
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
                            if ((_bits & 4L) != 0)
                            {
                                value = _headers._Date;
                                return true;
                            }
                            return false;
                        }
                        if ("ETag".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 2097152L) != 0)
                            {
                                value = _headers._ETag;
                                return true;
                            }
                            return false;
                        }
                        if ("Vary".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 134217728L) != 0)
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
                            if ((_bits & 16L) != 0)
                            {
                                value = _headers._Pragma;
                                return true;
                            }
                            return false;
                        }
                        if ("Server".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 33554432L) != 0)
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
                            if ((_bits & 32L) != 0)
                            {
                                value = _headers._Trailer;
                                return true;
                            }
                            return false;
                        }
                        if ("Upgrade".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 128L) != 0)
                            {
                                value = _headers._Upgrade;
                                return true;
                            }
                            return false;
                        }
                        if ("Warning".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 512L) != 0)
                            {
                                value = _headers._Warning;
                                return true;
                            }
                            return false;
                        }
                        if ("Expires".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 131072L) != 0)
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
                            if ((_bits & 64L) != 0)
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
                            if ((_bits & 256L) != 0)
                            {
                                value = _headers._Via;
                                return true;
                            }
                            return false;
                        }
                        if ("Age".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 1048576L) != 0)
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
                            if ((_bits & 1024L) != 0)
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
                            if ((_bits & 2048L) != 0)
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
                            if ((_bits & 4096L) != 0)
                            {
                                value = _headers._ContentEncoding;
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 8192L) != 0)
                            {
                                value = _headers._ContentLanguage;
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 16384L) != 0)
                            {
                                value = _headers._ContentLocation;
                                return true;
                            }
                            return false;
                        }
                        if ("WWW-Authenticate".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 268435456L) != 0)
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
                            if ((_bits & 32768L) != 0)
                            {
                                value = _headers._ContentMD5;
                                return true;
                            }
                            return false;
                        }
                        if ("Retry-After".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 16777216L) != 0)
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
                            if ((_bits & 4194304L) != 0)
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
                            if ((_bits & 8388608L) != 0)
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
                            if ((_bits & 536870912L) != 0)
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
                            if ((_bits & 1073741824L) != 0)
                            {
                                value = _headers._AccessControlAllowHeaders;
                                return true;
                            }
                            return false;
                        }
                        if ("Access-Control-Allow-Methods".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 2147483648L) != 0)
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
                            if ((_bits & 4294967296L) != 0)
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
                            if ((_bits & 8589934592L) != 0)
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
                            if ((_bits & 17179869184L) != 0)
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

        protected override void SetValueFast(string key, in StringValues value)
        {
            ValidateHeaderValueCharacters(value);
            switch (key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 1L;
                            _headers._CacheControl = value;
                            return;
                        }
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 65536L;
                            _headers._ContentRange = value;
                            return;
                        }
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 262144L;
                            _headers._LastModified = value;
                            return;
                        }
                        if ("Accept-Ranges".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 524288L;
                            _headers._AcceptRanges = value;
                            return;
                        }
                    }
                    break;
                case 10:
                    {
                        if ("Connection".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 2L;
                            _headers._Connection = value;
                            _headers._rawConnection = null;
                            return;
                        }
                        if ("Keep-Alive".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 8L;
                            _headers._KeepAlive = value;
                            return;
                        }
                        if ("Set-Cookie".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 67108864L;
                            _headers._SetCookie = value;
                            return;
                        }
                    }
                    break;
                case 4:
                    {
                        if ("Date".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 4L;
                            _headers._Date = value;
                            _headers._rawDate = null;
                            return;
                        }
                        if ("ETag".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 2097152L;
                            _headers._ETag = value;
                            return;
                        }
                        if ("Vary".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 134217728L;
                            _headers._Vary = value;
                            return;
                        }
                    }
                    break;
                case 6:
                    {
                        if ("Pragma".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 16L;
                            _headers._Pragma = value;
                            return;
                        }
                        if ("Server".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 33554432L;
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
                            _bits |= 32L;
                            _headers._Trailer = value;
                            return;
                        }
                        if ("Upgrade".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 128L;
                            _headers._Upgrade = value;
                            return;
                        }
                        if ("Warning".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 512L;
                            _headers._Warning = value;
                            return;
                        }
                        if ("Expires".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 131072L;
                            _headers._Expires = value;
                            return;
                        }
                    }
                    break;
                case 17:
                    {
                        if ("Transfer-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 64L;
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
                            _bits |= 256L;
                            _headers._Via = value;
                            return;
                        }
                        if ("Age".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 1048576L;
                            _headers._Age = value;
                            return;
                        }
                    }
                    break;
                case 5:
                    {
                        if ("Allow".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 1024L;
                            _headers._Allow = value;
                            return;
                        }
                    }
                    break;
                case 12:
                    {
                        if ("Content-Type".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 2048L;
                            _headers._ContentType = value;
                            return;
                        }
                    }
                    break;
                case 16:
                    {
                        if ("Content-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 4096L;
                            _headers._ContentEncoding = value;
                            return;
                        }
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 8192L;
                            _headers._ContentLanguage = value;
                            return;
                        }
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 16384L;
                            _headers._ContentLocation = value;
                            return;
                        }
                        if ("WWW-Authenticate".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 268435456L;
                            _headers._WWWAuthenticate = value;
                            return;
                        }
                    }
                    break;
                case 11:
                    {
                        if ("Content-MD5".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 32768L;
                            _headers._ContentMD5 = value;
                            return;
                        }
                        if ("Retry-After".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 16777216L;
                            _headers._RetryAfter = value;
                            return;
                        }
                    }
                    break;
                case 8:
                    {
                        if ("Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 4194304L;
                            _headers._Location = value;
                            return;
                        }
                    }
                    break;
                case 18:
                    {
                        if ("Proxy-Authenticate".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 8388608L;
                            _headers._ProxyAuthenticate = value;
                            return;
                        }
                    }
                    break;
                case 32:
                    {
                        if ("Access-Control-Allow-Credentials".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 536870912L;
                            _headers._AccessControlAllowCredentials = value;
                            return;
                        }
                    }
                    break;
                case 28:
                    {
                        if ("Access-Control-Allow-Headers".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 1073741824L;
                            _headers._AccessControlAllowHeaders = value;
                            return;
                        }
                        if ("Access-Control-Allow-Methods".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 2147483648L;
                            _headers._AccessControlAllowMethods = value;
                            return;
                        }
                    }
                    break;
                case 27:
                    {
                        if ("Access-Control-Allow-Origin".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 4294967296L;
                            _headers._AccessControlAllowOrigin = value;
                            return;
                        }
                    }
                    break;
                case 29:
                    {
                        if ("Access-Control-Expose-Headers".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 8589934592L;
                            _headers._AccessControlExposeHeaders = value;
                            return;
                        }
                    }
                    break;
                case 22:
                    {
                        if ("Access-Control-Max-Age".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            _bits |= 17179869184L;
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

        protected override bool AddValueFast(string key, in StringValues value)
        {
            ValidateHeaderValueCharacters(value);
            switch (key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 1L) == 0)
                            {
                                _bits |= 1L;
                                _headers._CacheControl = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 65536L) == 0)
                            {
                                _bits |= 65536L;
                                _headers._ContentRange = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 262144L) == 0)
                            {
                                _bits |= 262144L;
                                _headers._LastModified = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Accept-Ranges".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 524288L) == 0)
                            {
                                _bits |= 524288L;
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
                            if ((_bits & 2L) == 0)
                            {
                                _bits |= 2L;
                                _headers._Connection = value;
                                _headers._rawConnection = null;
                                return true;
                            }
                            return false;
                        }
                        if ("Keep-Alive".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 8L) == 0)
                            {
                                _bits |= 8L;
                                _headers._KeepAlive = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Set-Cookie".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 67108864L) == 0)
                            {
                                _bits |= 67108864L;
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
                            if ((_bits & 4L) == 0)
                            {
                                _bits |= 4L;
                                _headers._Date = value;
                                _headers._rawDate = null;
                                return true;
                            }
                            return false;
                        }
                        if ("ETag".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 2097152L) == 0)
                            {
                                _bits |= 2097152L;
                                _headers._ETag = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Vary".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 134217728L) == 0)
                            {
                                _bits |= 134217728L;
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
                            if ((_bits & 16L) == 0)
                            {
                                _bits |= 16L;
                                _headers._Pragma = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Server".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 33554432L) == 0)
                            {
                                _bits |= 33554432L;
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
                            if ((_bits & 32L) == 0)
                            {
                                _bits |= 32L;
                                _headers._Trailer = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Upgrade".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 128L) == 0)
                            {
                                _bits |= 128L;
                                _headers._Upgrade = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Warning".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 512L) == 0)
                            {
                                _bits |= 512L;
                                _headers._Warning = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Expires".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 131072L) == 0)
                            {
                                _bits |= 131072L;
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
                            if ((_bits & 64L) == 0)
                            {
                                _bits |= 64L;
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
                            if ((_bits & 256L) == 0)
                            {
                                _bits |= 256L;
                                _headers._Via = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Age".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 1048576L) == 0)
                            {
                                _bits |= 1048576L;
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
                            if ((_bits & 1024L) == 0)
                            {
                                _bits |= 1024L;
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
                            if ((_bits & 2048L) == 0)
                            {
                                _bits |= 2048L;
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
                            if ((_bits & 4096L) == 0)
                            {
                                _bits |= 4096L;
                                _headers._ContentEncoding = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 8192L) == 0)
                            {
                                _bits |= 8192L;
                                _headers._ContentLanguage = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 16384L) == 0)
                            {
                                _bits |= 16384L;
                                _headers._ContentLocation = value;
                                return true;
                            }
                            return false;
                        }
                        if ("WWW-Authenticate".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 268435456L) == 0)
                            {
                                _bits |= 268435456L;
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
                            if ((_bits & 32768L) == 0)
                            {
                                _bits |= 32768L;
                                _headers._ContentMD5 = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Retry-After".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 16777216L) == 0)
                            {
                                _bits |= 16777216L;
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
                            if ((_bits & 4194304L) == 0)
                            {
                                _bits |= 4194304L;
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
                            if ((_bits & 8388608L) == 0)
                            {
                                _bits |= 8388608L;
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
                            if ((_bits & 536870912L) == 0)
                            {
                                _bits |= 536870912L;
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
                            if ((_bits & 1073741824L) == 0)
                            {
                                _bits |= 1073741824L;
                                _headers._AccessControlAllowHeaders = value;
                                return true;
                            }
                            return false;
                        }
                        if ("Access-Control-Allow-Methods".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 2147483648L) == 0)
                            {
                                _bits |= 2147483648L;
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
                            if ((_bits & 4294967296L) == 0)
                            {
                                _bits |= 4294967296L;
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
                            if ((_bits & 8589934592L) == 0)
                            {
                                _bits |= 8589934592L;
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
                            if ((_bits & 17179869184L) == 0)
                            {
                                _bits |= 17179869184L;
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
                            if ((_bits & 1L) != 0)
                            {
                                _bits &= ~1L;
                                _headers._CacheControl = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 65536L) != 0)
                            {
                                _bits &= ~65536L;
                                _headers._ContentRange = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 262144L) != 0)
                            {
                                _bits &= ~262144L;
                                _headers._LastModified = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Accept-Ranges".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 524288L) != 0)
                            {
                                _bits &= ~524288L;
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
                            if ((_bits & 2L) != 0)
                            {
                                _bits &= ~2L;
                                _headers._Connection = default(StringValues);
                                _headers._rawConnection = null;
                                return true;
                            }
                            return false;
                        }
                        if ("Keep-Alive".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 8L) != 0)
                            {
                                _bits &= ~8L;
                                _headers._KeepAlive = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Set-Cookie".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 67108864L) != 0)
                            {
                                _bits &= ~67108864L;
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
                            if ((_bits & 4L) != 0)
                            {
                                _bits &= ~4L;
                                _headers._Date = default(StringValues);
                                _headers._rawDate = null;
                                return true;
                            }
                            return false;
                        }
                        if ("ETag".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 2097152L) != 0)
                            {
                                _bits &= ~2097152L;
                                _headers._ETag = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Vary".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 134217728L) != 0)
                            {
                                _bits &= ~134217728L;
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
                            if ((_bits & 16L) != 0)
                            {
                                _bits &= ~16L;
                                _headers._Pragma = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Server".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 33554432L) != 0)
                            {
                                _bits &= ~33554432L;
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
                            if ((_bits & 32L) != 0)
                            {
                                _bits &= ~32L;
                                _headers._Trailer = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Upgrade".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 128L) != 0)
                            {
                                _bits &= ~128L;
                                _headers._Upgrade = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Warning".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 512L) != 0)
                            {
                                _bits &= ~512L;
                                _headers._Warning = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Expires".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 131072L) != 0)
                            {
                                _bits &= ~131072L;
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
                            if ((_bits & 64L) != 0)
                            {
                                _bits &= ~64L;
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
                            if ((_bits & 256L) != 0)
                            {
                                _bits &= ~256L;
                                _headers._Via = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Age".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 1048576L) != 0)
                            {
                                _bits &= ~1048576L;
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
                            if ((_bits & 1024L) != 0)
                            {
                                _bits &= ~1024L;
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
                            if ((_bits & 2048L) != 0)
                            {
                                _bits &= ~2048L;
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
                            if ((_bits & 4096L) != 0)
                            {
                                _bits &= ~4096L;
                                _headers._ContentEncoding = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 8192L) != 0)
                            {
                                _bits &= ~8192L;
                                _headers._ContentLanguage = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 16384L) != 0)
                            {
                                _bits &= ~16384L;
                                _headers._ContentLocation = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("WWW-Authenticate".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 268435456L) != 0)
                            {
                                _bits &= ~268435456L;
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
                            if ((_bits & 32768L) != 0)
                            {
                                _bits &= ~32768L;
                                _headers._ContentMD5 = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Retry-After".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 16777216L) != 0)
                            {
                                _bits &= ~16777216L;
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
                            if ((_bits & 4194304L) != 0)
                            {
                                _bits &= ~4194304L;
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
                            if ((_bits & 8388608L) != 0)
                            {
                                _bits &= ~8388608L;
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
                            if ((_bits & 536870912L) != 0)
                            {
                                _bits &= ~536870912L;
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
                            if ((_bits & 1073741824L) != 0)
                            {
                                _bits &= ~1073741824L;
                                _headers._AccessControlAllowHeaders = default(StringValues);
                                return true;
                            }
                            return false;
                        }
                        if ("Access-Control-Allow-Methods".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((_bits & 2147483648L) != 0)
                            {
                                _bits &= ~2147483648L;
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
                            if ((_bits & 4294967296L) != 0)
                            {
                                _bits &= ~4294967296L;
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
                            if ((_bits & 8589934592L) != 0)
                            {
                                _bits &= ~8589934592L;
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
                            if ((_bits & 17179869184L) != 0)
                            {
                                _bits &= ~17179869184L;
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
            
            if ((tempBits & 2L) != 0)
            {
                _headers._Connection = default(StringValues);
                if((tempBits & ~2L) == 0)
                {
                    return;
                }
                tempBits &= ~2L;
            }
            
            if ((tempBits & 4L) != 0)
            {
                _headers._Date = default(StringValues);
                if((tempBits & ~4L) == 0)
                {
                    return;
                }
                tempBits &= ~4L;
            }
            
            if ((tempBits & 2048L) != 0)
            {
                _headers._ContentType = default(StringValues);
                if((tempBits & ~2048L) == 0)
                {
                    return;
                }
                tempBits &= ~2048L;
            }
            
            if ((tempBits & 33554432L) != 0)
            {
                _headers._Server = default(StringValues);
                if((tempBits & ~33554432L) == 0)
                {
                    return;
                }
                tempBits &= ~33554432L;
            }
            
            if ((tempBits & 1L) != 0)
            {
                _headers._CacheControl = default(StringValues);
                if((tempBits & ~1L) == 0)
                {
                    return;
                }
                tempBits &= ~1L;
            }
            
            if ((tempBits & 8L) != 0)
            {
                _headers._KeepAlive = default(StringValues);
                if((tempBits & ~8L) == 0)
                {
                    return;
                }
                tempBits &= ~8L;
            }
            
            if ((tempBits & 16L) != 0)
            {
                _headers._Pragma = default(StringValues);
                if((tempBits & ~16L) == 0)
                {
                    return;
                }
                tempBits &= ~16L;
            }
            
            if ((tempBits & 32L) != 0)
            {
                _headers._Trailer = default(StringValues);
                if((tempBits & ~32L) == 0)
                {
                    return;
                }
                tempBits &= ~32L;
            }
            
            if ((tempBits & 64L) != 0)
            {
                _headers._TransferEncoding = default(StringValues);
                if((tempBits & ~64L) == 0)
                {
                    return;
                }
                tempBits &= ~64L;
            }
            
            if ((tempBits & 128L) != 0)
            {
                _headers._Upgrade = default(StringValues);
                if((tempBits & ~128L) == 0)
                {
                    return;
                }
                tempBits &= ~128L;
            }
            
            if ((tempBits & 256L) != 0)
            {
                _headers._Via = default(StringValues);
                if((tempBits & ~256L) == 0)
                {
                    return;
                }
                tempBits &= ~256L;
            }
            
            if ((tempBits & 512L) != 0)
            {
                _headers._Warning = default(StringValues);
                if((tempBits & ~512L) == 0)
                {
                    return;
                }
                tempBits &= ~512L;
            }
            
            if ((tempBits & 1024L) != 0)
            {
                _headers._Allow = default(StringValues);
                if((tempBits & ~1024L) == 0)
                {
                    return;
                }
                tempBits &= ~1024L;
            }
            
            if ((tempBits & 4096L) != 0)
            {
                _headers._ContentEncoding = default(StringValues);
                if((tempBits & ~4096L) == 0)
                {
                    return;
                }
                tempBits &= ~4096L;
            }
            
            if ((tempBits & 8192L) != 0)
            {
                _headers._ContentLanguage = default(StringValues);
                if((tempBits & ~8192L) == 0)
                {
                    return;
                }
                tempBits &= ~8192L;
            }
            
            if ((tempBits & 16384L) != 0)
            {
                _headers._ContentLocation = default(StringValues);
                if((tempBits & ~16384L) == 0)
                {
                    return;
                }
                tempBits &= ~16384L;
            }
            
            if ((tempBits & 32768L) != 0)
            {
                _headers._ContentMD5 = default(StringValues);
                if((tempBits & ~32768L) == 0)
                {
                    return;
                }
                tempBits &= ~32768L;
            }
            
            if ((tempBits & 65536L) != 0)
            {
                _headers._ContentRange = default(StringValues);
                if((tempBits & ~65536L) == 0)
                {
                    return;
                }
                tempBits &= ~65536L;
            }
            
            if ((tempBits & 131072L) != 0)
            {
                _headers._Expires = default(StringValues);
                if((tempBits & ~131072L) == 0)
                {
                    return;
                }
                tempBits &= ~131072L;
            }
            
            if ((tempBits & 262144L) != 0)
            {
                _headers._LastModified = default(StringValues);
                if((tempBits & ~262144L) == 0)
                {
                    return;
                }
                tempBits &= ~262144L;
            }
            
            if ((tempBits & 524288L) != 0)
            {
                _headers._AcceptRanges = default(StringValues);
                if((tempBits & ~524288L) == 0)
                {
                    return;
                }
                tempBits &= ~524288L;
            }
            
            if ((tempBits & 1048576L) != 0)
            {
                _headers._Age = default(StringValues);
                if((tempBits & ~1048576L) == 0)
                {
                    return;
                }
                tempBits &= ~1048576L;
            }
            
            if ((tempBits & 2097152L) != 0)
            {
                _headers._ETag = default(StringValues);
                if((tempBits & ~2097152L) == 0)
                {
                    return;
                }
                tempBits &= ~2097152L;
            }
            
            if ((tempBits & 4194304L) != 0)
            {
                _headers._Location = default(StringValues);
                if((tempBits & ~4194304L) == 0)
                {
                    return;
                }
                tempBits &= ~4194304L;
            }
            
            if ((tempBits & 8388608L) != 0)
            {
                _headers._ProxyAuthenticate = default(StringValues);
                if((tempBits & ~8388608L) == 0)
                {
                    return;
                }
                tempBits &= ~8388608L;
            }
            
            if ((tempBits & 16777216L) != 0)
            {
                _headers._RetryAfter = default(StringValues);
                if((tempBits & ~16777216L) == 0)
                {
                    return;
                }
                tempBits &= ~16777216L;
            }
            
            if ((tempBits & 67108864L) != 0)
            {
                _headers._SetCookie = default(StringValues);
                if((tempBits & ~67108864L) == 0)
                {
                    return;
                }
                tempBits &= ~67108864L;
            }
            
            if ((tempBits & 134217728L) != 0)
            {
                _headers._Vary = default(StringValues);
                if((tempBits & ~134217728L) == 0)
                {
                    return;
                }
                tempBits &= ~134217728L;
            }
            
            if ((tempBits & 268435456L) != 0)
            {
                _headers._WWWAuthenticate = default(StringValues);
                if((tempBits & ~268435456L) == 0)
                {
                    return;
                }
                tempBits &= ~268435456L;
            }
            
            if ((tempBits & 536870912L) != 0)
            {
                _headers._AccessControlAllowCredentials = default(StringValues);
                if((tempBits & ~536870912L) == 0)
                {
                    return;
                }
                tempBits &= ~536870912L;
            }
            
            if ((tempBits & 1073741824L) != 0)
            {
                _headers._AccessControlAllowHeaders = default(StringValues);
                if((tempBits & ~1073741824L) == 0)
                {
                    return;
                }
                tempBits &= ~1073741824L;
            }
            
            if ((tempBits & 2147483648L) != 0)
            {
                _headers._AccessControlAllowMethods = default(StringValues);
                if((tempBits & ~2147483648L) == 0)
                {
                    return;
                }
                tempBits &= ~2147483648L;
            }
            
            if ((tempBits & 4294967296L) != 0)
            {
                _headers._AccessControlAllowOrigin = default(StringValues);
                if((tempBits & ~4294967296L) == 0)
                {
                    return;
                }
                tempBits &= ~4294967296L;
            }
            
            if ((tempBits & 8589934592L) != 0)
            {
                _headers._AccessControlExposeHeaders = default(StringValues);
                if((tempBits & ~8589934592L) == 0)
                {
                    return;
                }
                tempBits &= ~8589934592L;
            }
            
            if ((tempBits & 17179869184L) != 0)
            {
                _headers._AccessControlMaxAge = default(StringValues);
                if((tempBits & ~17179869184L) == 0)
                {
                    return;
                }
                tempBits &= ~17179869184L;
            }
            
        }

        protected override bool CopyToFast(KeyValuePair<string, StringValues>[] array, int arrayIndex)
        {
            if (arrayIndex < 0)
            {
                return false;
            }
            
                if ((_bits & 1L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Cache-Control", _headers._CacheControl);
                    ++arrayIndex;
                }
                if ((_bits & 2L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Connection", _headers._Connection);
                    ++arrayIndex;
                }
                if ((_bits & 4L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Date", _headers._Date);
                    ++arrayIndex;
                }
                if ((_bits & 8L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Keep-Alive", _headers._KeepAlive);
                    ++arrayIndex;
                }
                if ((_bits & 16L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Pragma", _headers._Pragma);
                    ++arrayIndex;
                }
                if ((_bits & 32L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Trailer", _headers._Trailer);
                    ++arrayIndex;
                }
                if ((_bits & 64L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Transfer-Encoding", _headers._TransferEncoding);
                    ++arrayIndex;
                }
                if ((_bits & 128L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Upgrade", _headers._Upgrade);
                    ++arrayIndex;
                }
                if ((_bits & 256L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Via", _headers._Via);
                    ++arrayIndex;
                }
                if ((_bits & 512L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Warning", _headers._Warning);
                    ++arrayIndex;
                }
                if ((_bits & 1024L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Allow", _headers._Allow);
                    ++arrayIndex;
                }
                if ((_bits & 2048L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Type", _headers._ContentType);
                    ++arrayIndex;
                }
                if ((_bits & 4096L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Encoding", _headers._ContentEncoding);
                    ++arrayIndex;
                }
                if ((_bits & 8192L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Language", _headers._ContentLanguage);
                    ++arrayIndex;
                }
                if ((_bits & 16384L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Location", _headers._ContentLocation);
                    ++arrayIndex;
                }
                if ((_bits & 32768L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-MD5", _headers._ContentMD5);
                    ++arrayIndex;
                }
                if ((_bits & 65536L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Range", _headers._ContentRange);
                    ++arrayIndex;
                }
                if ((_bits & 131072L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Expires", _headers._Expires);
                    ++arrayIndex;
                }
                if ((_bits & 262144L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Last-Modified", _headers._LastModified);
                    ++arrayIndex;
                }
                if ((_bits & 524288L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Accept-Ranges", _headers._AcceptRanges);
                    ++arrayIndex;
                }
                if ((_bits & 1048576L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Age", _headers._Age);
                    ++arrayIndex;
                }
                if ((_bits & 2097152L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("ETag", _headers._ETag);
                    ++arrayIndex;
                }
                if ((_bits & 4194304L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Location", _headers._Location);
                    ++arrayIndex;
                }
                if ((_bits & 8388608L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Proxy-Authenticate", _headers._ProxyAuthenticate);
                    ++arrayIndex;
                }
                if ((_bits & 16777216L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Retry-After", _headers._RetryAfter);
                    ++arrayIndex;
                }
                if ((_bits & 33554432L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Server", _headers._Server);
                    ++arrayIndex;
                }
                if ((_bits & 67108864L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Set-Cookie", _headers._SetCookie);
                    ++arrayIndex;
                }
                if ((_bits & 134217728L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Vary", _headers._Vary);
                    ++arrayIndex;
                }
                if ((_bits & 268435456L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("WWW-Authenticate", _headers._WWWAuthenticate);
                    ++arrayIndex;
                }
                if ((_bits & 536870912L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Access-Control-Allow-Credentials", _headers._AccessControlAllowCredentials);
                    ++arrayIndex;
                }
                if ((_bits & 1073741824L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Access-Control-Allow-Headers", _headers._AccessControlAllowHeaders);
                    ++arrayIndex;
                }
                if ((_bits & 2147483648L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Access-Control-Allow-Methods", _headers._AccessControlAllowMethods);
                    ++arrayIndex;
                }
                if ((_bits & 4294967296L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Access-Control-Allow-Origin", _headers._AccessControlAllowOrigin);
                    ++arrayIndex;
                }
                if ((_bits & 8589934592L) != 0)
                {
                    if (arrayIndex == array.Length)
                    {
                        return false;
                    }
                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Access-Control-Expose-Headers", _headers._AccessControlExposeHeaders);
                    ++arrayIndex;
                }
                if ((_bits & 17179869184L) != 0)
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
        
        internal void CopyToFast(ref CountingBufferWriter<PipeWriter> output)
        {
            var tempBits = _bits | (_contentLength.HasValue ? -9223372036854775808L : 0);
            
                if ((tempBits & 2L) != 0)
                { 
                    if (_headers._rawConnection != null)
                    {
                        output.Write(_headers._rawConnection);
                    }
                    else 
                    {
                        var valueCount = _headers._Connection.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._Connection[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 17, 14));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~2L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~2L;
                }
                if ((tempBits & 4L) != 0)
                { 
                    if (_headers._rawDate != null)
                    {
                        output.Write(_headers._rawDate);
                    }
                    else 
                    {
                        var valueCount = _headers._Date.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._Date[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 31, 8));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~4L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~4L;
                }
                if ((tempBits & 2048L) != 0)
                { 
                    {
                        var valueCount = _headers._ContentType.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._ContentType[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 133, 16));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~2048L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~2048L;
                }
                if ((tempBits & 33554432L) != 0)
                { 
                    if (_headers._rawServer != null)
                    {
                        output.Write(_headers._rawServer);
                    }
                    else 
                    {
                        var valueCount = _headers._Server.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._Server[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 350, 10));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~33554432L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~33554432L;
                }
                if ((tempBits & -9223372036854775808L) != 0)
                {
                    output.Write(new ReadOnlySpan<byte>(_headerBytes, 592, 18));
                    PipelineExtensions.WriteNumeric(ref output, (ulong)ContentLength.Value);

                    if((tempBits & ~-9223372036854775808L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~-9223372036854775808L;
                }
                if ((tempBits & 1L) != 0)
                { 
                    {
                        var valueCount = _headers._CacheControl.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._CacheControl[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 0, 17));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~1L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~1L;
                }
                if ((tempBits & 8L) != 0)
                { 
                    {
                        var valueCount = _headers._KeepAlive.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._KeepAlive[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 39, 14));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~8L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~8L;
                }
                if ((tempBits & 16L) != 0)
                { 
                    {
                        var valueCount = _headers._Pragma.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._Pragma[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 53, 10));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~16L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~16L;
                }
                if ((tempBits & 32L) != 0)
                { 
                    {
                        var valueCount = _headers._Trailer.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._Trailer[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 63, 11));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~32L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~32L;
                }
                if ((tempBits & 64L) != 0)
                { 
                    if (_headers._rawTransferEncoding != null)
                    {
                        output.Write(_headers._rawTransferEncoding);
                    }
                    else 
                    {
                        var valueCount = _headers._TransferEncoding.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._TransferEncoding[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 74, 21));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~64L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~64L;
                }
                if ((tempBits & 128L) != 0)
                { 
                    {
                        var valueCount = _headers._Upgrade.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._Upgrade[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 95, 11));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~128L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~128L;
                }
                if ((tempBits & 256L) != 0)
                { 
                    {
                        var valueCount = _headers._Via.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._Via[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 106, 7));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~256L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~256L;
                }
                if ((tempBits & 512L) != 0)
                { 
                    {
                        var valueCount = _headers._Warning.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._Warning[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 113, 11));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~512L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~512L;
                }
                if ((tempBits & 1024L) != 0)
                { 
                    {
                        var valueCount = _headers._Allow.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._Allow[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 124, 9));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~1024L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~1024L;
                }
                if ((tempBits & 4096L) != 0)
                { 
                    {
                        var valueCount = _headers._ContentEncoding.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._ContentEncoding[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 149, 20));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~4096L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~4096L;
                }
                if ((tempBits & 8192L) != 0)
                { 
                    {
                        var valueCount = _headers._ContentLanguage.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._ContentLanguage[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 169, 20));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~8192L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~8192L;
                }
                if ((tempBits & 16384L) != 0)
                { 
                    {
                        var valueCount = _headers._ContentLocation.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._ContentLocation[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 189, 20));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~16384L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~16384L;
                }
                if ((tempBits & 32768L) != 0)
                { 
                    {
                        var valueCount = _headers._ContentMD5.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._ContentMD5[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 209, 15));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~32768L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~32768L;
                }
                if ((tempBits & 65536L) != 0)
                { 
                    {
                        var valueCount = _headers._ContentRange.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._ContentRange[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 224, 17));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~65536L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~65536L;
                }
                if ((tempBits & 131072L) != 0)
                { 
                    {
                        var valueCount = _headers._Expires.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._Expires[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 241, 11));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~131072L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~131072L;
                }
                if ((tempBits & 262144L) != 0)
                { 
                    {
                        var valueCount = _headers._LastModified.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._LastModified[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 252, 17));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~262144L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~262144L;
                }
                if ((tempBits & 524288L) != 0)
                { 
                    {
                        var valueCount = _headers._AcceptRanges.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._AcceptRanges[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 269, 17));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~524288L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~524288L;
                }
                if ((tempBits & 1048576L) != 0)
                { 
                    {
                        var valueCount = _headers._Age.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._Age[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 286, 7));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~1048576L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~1048576L;
                }
                if ((tempBits & 2097152L) != 0)
                { 
                    {
                        var valueCount = _headers._ETag.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._ETag[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 293, 8));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~2097152L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~2097152L;
                }
                if ((tempBits & 4194304L) != 0)
                { 
                    {
                        var valueCount = _headers._Location.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._Location[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 301, 12));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~4194304L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~4194304L;
                }
                if ((tempBits & 8388608L) != 0)
                { 
                    {
                        var valueCount = _headers._ProxyAuthenticate.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._ProxyAuthenticate[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 313, 22));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~8388608L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~8388608L;
                }
                if ((tempBits & 16777216L) != 0)
                { 
                    {
                        var valueCount = _headers._RetryAfter.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._RetryAfter[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 335, 15));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~16777216L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~16777216L;
                }
                if ((tempBits & 67108864L) != 0)
                { 
                    {
                        var valueCount = _headers._SetCookie.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._SetCookie[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 360, 14));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~67108864L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~67108864L;
                }
                if ((tempBits & 134217728L) != 0)
                { 
                    {
                        var valueCount = _headers._Vary.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._Vary[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 374, 8));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~134217728L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~134217728L;
                }
                if ((tempBits & 268435456L) != 0)
                { 
                    {
                        var valueCount = _headers._WWWAuthenticate.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._WWWAuthenticate[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 382, 20));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~268435456L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~268435456L;
                }
                if ((tempBits & 536870912L) != 0)
                { 
                    {
                        var valueCount = _headers._AccessControlAllowCredentials.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._AccessControlAllowCredentials[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 402, 36));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~536870912L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~536870912L;
                }
                if ((tempBits & 1073741824L) != 0)
                { 
                    {
                        var valueCount = _headers._AccessControlAllowHeaders.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._AccessControlAllowHeaders[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 438, 32));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~1073741824L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~1073741824L;
                }
                if ((tempBits & 2147483648L) != 0)
                { 
                    {
                        var valueCount = _headers._AccessControlAllowMethods.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._AccessControlAllowMethods[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 470, 32));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~2147483648L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~2147483648L;
                }
                if ((tempBits & 4294967296L) != 0)
                { 
                    {
                        var valueCount = _headers._AccessControlAllowOrigin.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._AccessControlAllowOrigin[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 502, 31));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~4294967296L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~4294967296L;
                }
                if ((tempBits & 8589934592L) != 0)
                { 
                    {
                        var valueCount = _headers._AccessControlExposeHeaders.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._AccessControlExposeHeaders[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 533, 33));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~8589934592L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~8589934592L;
                }
                if ((tempBits & 17179869184L) != 0)
                { 
                    {
                        var valueCount = _headers._AccessControlMaxAge.Count;
                        for (var i = 0; i < valueCount; i++)
                        {
                            var value = _headers._AccessControlMaxAge[i];
                            if (value != null)
                            {
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, 566, 26));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }
                        }
                    }

                    if((tempBits & ~17179869184L) == 0)
                    {
                        return;
                    }
                    tempBits &= ~17179869184L;
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
            public bool MoveNext()
            {
                switch (_state)
                {
                    
                    case 0:
                        goto state0;
                    
                    case 1:
                        goto state1;
                    
                    case 2:
                        goto state2;
                    
                    case 3:
                        goto state3;
                    
                    case 4:
                        goto state4;
                    
                    case 5:
                        goto state5;
                    
                    case 6:
                        goto state6;
                    
                    case 7:
                        goto state7;
                    
                    case 8:
                        goto state8;
                    
                    case 9:
                        goto state9;
                    
                    case 10:
                        goto state10;
                    
                    case 11:
                        goto state11;
                    
                    case 12:
                        goto state12;
                    
                    case 13:
                        goto state13;
                    
                    case 14:
                        goto state14;
                    
                    case 15:
                        goto state15;
                    
                    case 16:
                        goto state16;
                    
                    case 17:
                        goto state17;
                    
                    case 18:
                        goto state18;
                    
                    case 19:
                        goto state19;
                    
                    case 20:
                        goto state20;
                    
                    case 21:
                        goto state21;
                    
                    case 22:
                        goto state22;
                    
                    case 23:
                        goto state23;
                    
                    case 24:
                        goto state24;
                    
                    case 25:
                        goto state25;
                    
                    case 26:
                        goto state26;
                    
                    case 27:
                        goto state27;
                    
                    case 28:
                        goto state28;
                    
                    case 29:
                        goto state29;
                    
                    case 30:
                        goto state30;
                    
                    case 31:
                        goto state31;
                    
                    case 32:
                        goto state32;
                    
                    case 33:
                        goto state33;
                    
                    case 34:
                        goto state34;
                    
                    case 36:
                        goto state36;
                    default:
                        goto state_default;
                }
                
                state0:
                    if ((_bits & 1L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Cache-Control", _collection._headers._CacheControl);
                        _state = 1;
                        return true;
                    }
                
                state1:
                    if ((_bits & 2L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Connection", _collection._headers._Connection);
                        _state = 2;
                        return true;
                    }
                
                state2:
                    if ((_bits & 4L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Date", _collection._headers._Date);
                        _state = 3;
                        return true;
                    }
                
                state3:
                    if ((_bits & 8L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Keep-Alive", _collection._headers._KeepAlive);
                        _state = 4;
                        return true;
                    }
                
                state4:
                    if ((_bits & 16L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Pragma", _collection._headers._Pragma);
                        _state = 5;
                        return true;
                    }
                
                state5:
                    if ((_bits & 32L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Trailer", _collection._headers._Trailer);
                        _state = 6;
                        return true;
                    }
                
                state6:
                    if ((_bits & 64L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Transfer-Encoding", _collection._headers._TransferEncoding);
                        _state = 7;
                        return true;
                    }
                
                state7:
                    if ((_bits & 128L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Upgrade", _collection._headers._Upgrade);
                        _state = 8;
                        return true;
                    }
                
                state8:
                    if ((_bits & 256L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Via", _collection._headers._Via);
                        _state = 9;
                        return true;
                    }
                
                state9:
                    if ((_bits & 512L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Warning", _collection._headers._Warning);
                        _state = 10;
                        return true;
                    }
                
                state10:
                    if ((_bits & 1024L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Allow", _collection._headers._Allow);
                        _state = 11;
                        return true;
                    }
                
                state11:
                    if ((_bits & 2048L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Type", _collection._headers._ContentType);
                        _state = 12;
                        return true;
                    }
                
                state12:
                    if ((_bits & 4096L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Encoding", _collection._headers._ContentEncoding);
                        _state = 13;
                        return true;
                    }
                
                state13:
                    if ((_bits & 8192L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Language", _collection._headers._ContentLanguage);
                        _state = 14;
                        return true;
                    }
                
                state14:
                    if ((_bits & 16384L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Location", _collection._headers._ContentLocation);
                        _state = 15;
                        return true;
                    }
                
                state15:
                    if ((_bits & 32768L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-MD5", _collection._headers._ContentMD5);
                        _state = 16;
                        return true;
                    }
                
                state16:
                    if ((_bits & 65536L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Range", _collection._headers._ContentRange);
                        _state = 17;
                        return true;
                    }
                
                state17:
                    if ((_bits & 131072L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Expires", _collection._headers._Expires);
                        _state = 18;
                        return true;
                    }
                
                state18:
                    if ((_bits & 262144L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Last-Modified", _collection._headers._LastModified);
                        _state = 19;
                        return true;
                    }
                
                state19:
                    if ((_bits & 524288L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Accept-Ranges", _collection._headers._AcceptRanges);
                        _state = 20;
                        return true;
                    }
                
                state20:
                    if ((_bits & 1048576L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Age", _collection._headers._Age);
                        _state = 21;
                        return true;
                    }
                
                state21:
                    if ((_bits & 2097152L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("ETag", _collection._headers._ETag);
                        _state = 22;
                        return true;
                    }
                
                state22:
                    if ((_bits & 4194304L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Location", _collection._headers._Location);
                        _state = 23;
                        return true;
                    }
                
                state23:
                    if ((_bits & 8388608L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Proxy-Authenticate", _collection._headers._ProxyAuthenticate);
                        _state = 24;
                        return true;
                    }
                
                state24:
                    if ((_bits & 16777216L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Retry-After", _collection._headers._RetryAfter);
                        _state = 25;
                        return true;
                    }
                
                state25:
                    if ((_bits & 33554432L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Server", _collection._headers._Server);
                        _state = 26;
                        return true;
                    }
                
                state26:
                    if ((_bits & 67108864L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Set-Cookie", _collection._headers._SetCookie);
                        _state = 27;
                        return true;
                    }
                
                state27:
                    if ((_bits & 134217728L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Vary", _collection._headers._Vary);
                        _state = 28;
                        return true;
                    }
                
                state28:
                    if ((_bits & 268435456L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("WWW-Authenticate", _collection._headers._WWWAuthenticate);
                        _state = 29;
                        return true;
                    }
                
                state29:
                    if ((_bits & 536870912L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Allow-Credentials", _collection._headers._AccessControlAllowCredentials);
                        _state = 30;
                        return true;
                    }
                
                state30:
                    if ((_bits & 1073741824L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Allow-Headers", _collection._headers._AccessControlAllowHeaders);
                        _state = 31;
                        return true;
                    }
                
                state31:
                    if ((_bits & 2147483648L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Allow-Methods", _collection._headers._AccessControlAllowMethods);
                        _state = 32;
                        return true;
                    }
                
                state32:
                    if ((_bits & 4294967296L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Allow-Origin", _collection._headers._AccessControlAllowOrigin);
                        _state = 33;
                        return true;
                    }
                
                state33:
                    if ((_bits & 8589934592L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Expose-Headers", _collection._headers._AccessControlExposeHeaders);
                        _state = 34;
                        return true;
                    }
                
                state34:
                    if ((_bits & 17179869184L) != 0)
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Max-Age", _collection._headers._AccessControlMaxAge);
                        _state = 35;
                        return true;
                    }
                
                state36:
                    if (_collection._contentLength.HasValue)
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Length", HeaderUtilities.FormatNonNegativeInt64(_collection._contentLength.Value));
                        _state = 37;
                        return true;
                    }
                state_default:
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