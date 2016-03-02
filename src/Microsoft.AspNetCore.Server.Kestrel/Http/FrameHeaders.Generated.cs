
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.Kestrel.Http 
{

    public partial class FrameRequestHeaders
    {
        
        private long _bits = 0;
        private HeaderReferences _headers;
        
        public StringValues HeaderCacheControl
        {
            get
            {
                if (((_bits & 1L) != 0))
                {
                    return _headers._CacheControl;
                }
                return StringValues.Empty;
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
                if (((_bits & 2L) != 0))
                {
                    return _headers._Connection;
                }
                return StringValues.Empty;
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
                if (((_bits & 4L) != 0))
                {
                    return _headers._Date;
                }
                return StringValues.Empty;
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
                if (((_bits & 8L) != 0))
                {
                    return _headers._KeepAlive;
                }
                return StringValues.Empty;
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
                if (((_bits & 16L) != 0))
                {
                    return _headers._Pragma;
                }
                return StringValues.Empty;
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
                if (((_bits & 32L) != 0))
                {
                    return _headers._Trailer;
                }
                return StringValues.Empty;
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
                if (((_bits & 64L) != 0))
                {
                    return _headers._TransferEncoding;
                }
                return StringValues.Empty;
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
                if (((_bits & 128L) != 0))
                {
                    return _headers._Upgrade;
                }
                return StringValues.Empty;
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
                if (((_bits & 256L) != 0))
                {
                    return _headers._Via;
                }
                return StringValues.Empty;
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
                if (((_bits & 512L) != 0))
                {
                    return _headers._Warning;
                }
                return StringValues.Empty;
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
                if (((_bits & 1024L) != 0))
                {
                    return _headers._Allow;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 1024L;
                _headers._Allow = value; 
            }
        }
        public StringValues HeaderContentLength
        {
            get
            {
                if (((_bits & 2048L) != 0))
                {
                    return _headers._ContentLength;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 2048L;
                _headers._ContentLength = value; 
            }
        }
        public StringValues HeaderContentType
        {
            get
            {
                if (((_bits & 4096L) != 0))
                {
                    return _headers._ContentType;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 4096L;
                _headers._ContentType = value; 
            }
        }
        public StringValues HeaderContentEncoding
        {
            get
            {
                if (((_bits & 8192L) != 0))
                {
                    return _headers._ContentEncoding;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 8192L;
                _headers._ContentEncoding = value; 
            }
        }
        public StringValues HeaderContentLanguage
        {
            get
            {
                if (((_bits & 16384L) != 0))
                {
                    return _headers._ContentLanguage;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 16384L;
                _headers._ContentLanguage = value; 
            }
        }
        public StringValues HeaderContentLocation
        {
            get
            {
                if (((_bits & 32768L) != 0))
                {
                    return _headers._ContentLocation;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 32768L;
                _headers._ContentLocation = value; 
            }
        }
        public StringValues HeaderContentMD5
        {
            get
            {
                if (((_bits & 65536L) != 0))
                {
                    return _headers._ContentMD5;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 65536L;
                _headers._ContentMD5 = value; 
            }
        }
        public StringValues HeaderContentRange
        {
            get
            {
                if (((_bits & 131072L) != 0))
                {
                    return _headers._ContentRange;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 131072L;
                _headers._ContentRange = value; 
            }
        }
        public StringValues HeaderExpires
        {
            get
            {
                if (((_bits & 262144L) != 0))
                {
                    return _headers._Expires;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 262144L;
                _headers._Expires = value; 
            }
        }
        public StringValues HeaderLastModified
        {
            get
            {
                if (((_bits & 524288L) != 0))
                {
                    return _headers._LastModified;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 524288L;
                _headers._LastModified = value; 
            }
        }
        public StringValues HeaderAccept
        {
            get
            {
                if (((_bits & 1048576L) != 0))
                {
                    return _headers._Accept;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 1048576L;
                _headers._Accept = value; 
            }
        }
        public StringValues HeaderAcceptCharset
        {
            get
            {
                if (((_bits & 2097152L) != 0))
                {
                    return _headers._AcceptCharset;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 2097152L;
                _headers._AcceptCharset = value; 
            }
        }
        public StringValues HeaderAcceptEncoding
        {
            get
            {
                if (((_bits & 4194304L) != 0))
                {
                    return _headers._AcceptEncoding;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 4194304L;
                _headers._AcceptEncoding = value; 
            }
        }
        public StringValues HeaderAcceptLanguage
        {
            get
            {
                if (((_bits & 8388608L) != 0))
                {
                    return _headers._AcceptLanguage;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 8388608L;
                _headers._AcceptLanguage = value; 
            }
        }
        public StringValues HeaderAuthorization
        {
            get
            {
                if (((_bits & 16777216L) != 0))
                {
                    return _headers._Authorization;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 16777216L;
                _headers._Authorization = value; 
            }
        }
        public StringValues HeaderCookie
        {
            get
            {
                if (((_bits & 33554432L) != 0))
                {
                    return _headers._Cookie;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 33554432L;
                _headers._Cookie = value; 
            }
        }
        public StringValues HeaderExpect
        {
            get
            {
                if (((_bits & 67108864L) != 0))
                {
                    return _headers._Expect;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 67108864L;
                _headers._Expect = value; 
            }
        }
        public StringValues HeaderFrom
        {
            get
            {
                if (((_bits & 134217728L) != 0))
                {
                    return _headers._From;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 134217728L;
                _headers._From = value; 
            }
        }
        public StringValues HeaderHost
        {
            get
            {
                if (((_bits & 268435456L) != 0))
                {
                    return _headers._Host;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 268435456L;
                _headers._Host = value; 
            }
        }
        public StringValues HeaderIfMatch
        {
            get
            {
                if (((_bits & 536870912L) != 0))
                {
                    return _headers._IfMatch;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 536870912L;
                _headers._IfMatch = value; 
            }
        }
        public StringValues HeaderIfModifiedSince
        {
            get
            {
                if (((_bits & 1073741824L) != 0))
                {
                    return _headers._IfModifiedSince;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 1073741824L;
                _headers._IfModifiedSince = value; 
            }
        }
        public StringValues HeaderIfNoneMatch
        {
            get
            {
                if (((_bits & 2147483648L) != 0))
                {
                    return _headers._IfNoneMatch;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 2147483648L;
                _headers._IfNoneMatch = value; 
            }
        }
        public StringValues HeaderIfRange
        {
            get
            {
                if (((_bits & 4294967296L) != 0))
                {
                    return _headers._IfRange;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 4294967296L;
                _headers._IfRange = value; 
            }
        }
        public StringValues HeaderIfUnmodifiedSince
        {
            get
            {
                if (((_bits & 8589934592L) != 0))
                {
                    return _headers._IfUnmodifiedSince;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 8589934592L;
                _headers._IfUnmodifiedSince = value; 
            }
        }
        public StringValues HeaderMaxForwards
        {
            get
            {
                if (((_bits & 17179869184L) != 0))
                {
                    return _headers._MaxForwards;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 17179869184L;
                _headers._MaxForwards = value; 
            }
        }
        public StringValues HeaderProxyAuthorization
        {
            get
            {
                if (((_bits & 34359738368L) != 0))
                {
                    return _headers._ProxyAuthorization;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 34359738368L;
                _headers._ProxyAuthorization = value; 
            }
        }
        public StringValues HeaderReferer
        {
            get
            {
                if (((_bits & 68719476736L) != 0))
                {
                    return _headers._Referer;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 68719476736L;
                _headers._Referer = value; 
            }
        }
        public StringValues HeaderRange
        {
            get
            {
                if (((_bits & 137438953472L) != 0))
                {
                    return _headers._Range;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 137438953472L;
                _headers._Range = value; 
            }
        }
        public StringValues HeaderTE
        {
            get
            {
                if (((_bits & 274877906944L) != 0))
                {
                    return _headers._TE;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 274877906944L;
                _headers._TE = value; 
            }
        }
        public StringValues HeaderTranslate
        {
            get
            {
                if (((_bits & 549755813888L) != 0))
                {
                    return _headers._Translate;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 549755813888L;
                _headers._Translate = value; 
            }
        }
        public StringValues HeaderUserAgent
        {
            get
            {
                if (((_bits & 1099511627776L) != 0))
                {
                    return _headers._UserAgent;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 1099511627776L;
                _headers._UserAgent = value; 
            }
        }
        public StringValues HeaderOrigin
        {
            get
            {
                if (((_bits & 2199023255552L) != 0))
                {
                    return _headers._Origin;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 2199023255552L;
                _headers._Origin = value; 
            }
        }
        public StringValues HeaderAccessControlRequestMethod
        {
            get
            {
                if (((_bits & 4398046511104L) != 0))
                {
                    return _headers._AccessControlRequestMethod;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 4398046511104L;
                _headers._AccessControlRequestMethod = value; 
            }
        }
        public StringValues HeaderAccessControlRequestHeaders
        {
            get
            {
                if (((_bits & 8796093022208L) != 0))
                {
                    return _headers._AccessControlRequestHeaders;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 8796093022208L;
                _headers._AccessControlRequestHeaders = value; 
            }
        }
        
        protected override int GetCountFast()
        {
            return BitCount(_bits) + (MaybeUnknown?.Count ?? 0);
        }
        protected override StringValues GetValueFast(string key)
        {
            switch (key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 1L) != 0))
                            {
                                return _headers._CacheControl;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 131072L) != 0))
                            {
                                return _headers._ContentRange;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 524288L) != 0))
                            {
                                return _headers._LastModified;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Authorization".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 16777216L) != 0))
                            {
                                return _headers._Authorization;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("If-None-Match".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 2147483648L) != 0))
                            {
                                return _headers._IfNoneMatch;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 10:
                    {
                        if ("Connection".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 2L) != 0))
                            {
                                return _headers._Connection;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Keep-Alive".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 8L) != 0))
                            {
                                return _headers._KeepAlive;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("User-Agent".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 1099511627776L) != 0))
                            {
                                return _headers._UserAgent;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 4:
                    {
                        if ("Date".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 4L) != 0))
                            {
                                return _headers._Date;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("From".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 134217728L) != 0))
                            {
                                return _headers._From;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Host".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 268435456L) != 0))
                            {
                                return _headers._Host;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 6:
                    {
                        if ("Pragma".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 16L) != 0))
                            {
                                return _headers._Pragma;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Accept".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 1048576L) != 0))
                            {
                                return _headers._Accept;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Cookie".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 33554432L) != 0))
                            {
                                return _headers._Cookie;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Expect".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 67108864L) != 0))
                            {
                                return _headers._Expect;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Origin".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 2199023255552L) != 0))
                            {
                                return _headers._Origin;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 7:
                    {
                        if ("Trailer".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 32L) != 0))
                            {
                                return _headers._Trailer;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Upgrade".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 128L) != 0))
                            {
                                return _headers._Upgrade;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Warning".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 512L) != 0))
                            {
                                return _headers._Warning;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Expires".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 262144L) != 0))
                            {
                                return _headers._Expires;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Referer".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 68719476736L) != 0))
                            {
                                return _headers._Referer;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 17:
                    {
                        if ("Transfer-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 64L) != 0))
                            {
                                return _headers._TransferEncoding;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("If-Modified-Since".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 1073741824L) != 0))
                            {
                                return _headers._IfModifiedSince;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 3:
                    {
                        if ("Via".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 256L) != 0))
                            {
                                return _headers._Via;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 5:
                    {
                        if ("Allow".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 1024L) != 0))
                            {
                                return _headers._Allow;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 137438953472L) != 0))
                            {
                                return _headers._Range;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 14:
                    {
                        if ("Content-Length".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 2048L) != 0))
                            {
                                return _headers._ContentLength;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Accept-Charset".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 2097152L) != 0))
                            {
                                return _headers._AcceptCharset;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 12:
                    {
                        if ("Content-Type".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 4096L) != 0))
                            {
                                return _headers._ContentType;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Max-Forwards".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 17179869184L) != 0))
                            {
                                return _headers._MaxForwards;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 16:
                    {
                        if ("Content-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 8192L) != 0))
                            {
                                return _headers._ContentEncoding;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 16384L) != 0))
                            {
                                return _headers._ContentLanguage;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 32768L) != 0))
                            {
                                return _headers._ContentLocation;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 11:
                    {
                        if ("Content-MD5".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 65536L) != 0))
                            {
                                return _headers._ContentMD5;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 15:
                    {
                        if ("Accept-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 4194304L) != 0))
                            {
                                return _headers._AcceptEncoding;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Accept-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 8388608L) != 0))
                            {
                                return _headers._AcceptLanguage;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 8:
                    {
                        if ("If-Match".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 536870912L) != 0))
                            {
                                return _headers._IfMatch;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("If-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 4294967296L) != 0))
                            {
                                return _headers._IfRange;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 19:
                    {
                        if ("If-Unmodified-Since".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 8589934592L) != 0))
                            {
                                return _headers._IfUnmodifiedSince;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Proxy-Authorization".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 34359738368L) != 0))
                            {
                                return _headers._ProxyAuthorization;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 2:
                    {
                        if ("TE".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 274877906944L) != 0))
                            {
                                return _headers._TE;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 9:
                    {
                        if ("Translate".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 549755813888L) != 0))
                            {
                                return _headers._Translate;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 29:
                    {
                        if ("Access-Control-Request-Method".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 4398046511104L) != 0))
                            {
                                return _headers._AccessControlRequestMethod;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 30:
                    {
                        if ("Access-Control-Request-Headers".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 8796093022208L) != 0))
                            {
                                return _headers._AccessControlRequestHeaders;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;
}
            if (MaybeUnknown == null) 
            {
                ThrowKeyNotFoundException();
            }
            return MaybeUnknown[key];
        }
        protected override bool TryGetValueFast(string key, out StringValues value)
        {
            switch (key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 1L) != 0))
                            {
                                value = _headers._CacheControl;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 131072L) != 0))
                            {
                                value = _headers._ContentRange;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 524288L) != 0))
                            {
                                value = _headers._LastModified;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Authorization".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 16777216L) != 0))
                            {
                                value = _headers._Authorization;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("If-None-Match".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 2147483648L) != 0))
                            {
                                value = _headers._IfNoneMatch;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 10:
                    {
                        if ("Connection".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 2L) != 0))
                            {
                                value = _headers._Connection;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Keep-Alive".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 8L) != 0))
                            {
                                value = _headers._KeepAlive;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("User-Agent".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 1099511627776L) != 0))
                            {
                                value = _headers._UserAgent;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 4:
                    {
                        if ("Date".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 4L) != 0))
                            {
                                value = _headers._Date;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("From".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 134217728L) != 0))
                            {
                                value = _headers._From;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Host".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 268435456L) != 0))
                            {
                                value = _headers._Host;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 6:
                    {
                        if ("Pragma".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 16L) != 0))
                            {
                                value = _headers._Pragma;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Accept".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 1048576L) != 0))
                            {
                                value = _headers._Accept;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Cookie".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 33554432L) != 0))
                            {
                                value = _headers._Cookie;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Expect".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 67108864L) != 0))
                            {
                                value = _headers._Expect;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Origin".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 2199023255552L) != 0))
                            {
                                value = _headers._Origin;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 7:
                    {
                        if ("Trailer".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 32L) != 0))
                            {
                                value = _headers._Trailer;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Upgrade".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 128L) != 0))
                            {
                                value = _headers._Upgrade;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Warning".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 512L) != 0))
                            {
                                value = _headers._Warning;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Expires".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 262144L) != 0))
                            {
                                value = _headers._Expires;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Referer".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 68719476736L) != 0))
                            {
                                value = _headers._Referer;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 17:
                    {
                        if ("Transfer-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 64L) != 0))
                            {
                                value = _headers._TransferEncoding;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("If-Modified-Since".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 1073741824L) != 0))
                            {
                                value = _headers._IfModifiedSince;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 3:
                    {
                        if ("Via".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 256L) != 0))
                            {
                                value = _headers._Via;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 5:
                    {
                        if ("Allow".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 1024L) != 0))
                            {
                                value = _headers._Allow;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Range".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 137438953472L) != 0))
                            {
                                value = _headers._Range;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 14:
                    {
                        if ("Content-Length".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 2048L) != 0))
                            {
                                value = _headers._ContentLength;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Accept-Charset".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 2097152L) != 0))
                            {
                                value = _headers._AcceptCharset;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 12:
                    {
                        if ("Content-Type".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 4096L) != 0))
                            {
                                value = _headers._ContentType;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Max-Forwards".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 17179869184L) != 0))
                            {
                                value = _headers._MaxForwards;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 16:
                    {
                        if ("Content-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 8192L) != 0))
                            {
                                value = _headers._ContentEncoding;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 16384L) != 0))
                            {
                                value = _headers._ContentLanguage;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 32768L) != 0))
                            {
                                value = _headers._ContentLocation;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 11:
                    {
                        if ("Content-MD5".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 65536L) != 0))
                            {
                                value = _headers._ContentMD5;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 15:
                    {
                        if ("Accept-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 4194304L) != 0))
                            {
                                value = _headers._AcceptEncoding;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Accept-Language".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 8388608L) != 0))
                            {
                                value = _headers._AcceptLanguage;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 8:
                    {
                        if ("If-Match".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 536870912L) != 0))
                            {
                                value = _headers._IfMatch;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("If-Range".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 4294967296L) != 0))
                            {
                                value = _headers._IfRange;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 19:
                    {
                        if ("If-Unmodified-Since".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 8589934592L) != 0))
                            {
                                value = _headers._IfUnmodifiedSince;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Proxy-Authorization".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 34359738368L) != 0))
                            {
                                value = _headers._ProxyAuthorization;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 2:
                    {
                        if ("TE".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 274877906944L) != 0))
                            {
                                value = _headers._TE;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 9:
                    {
                        if ("Translate".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 549755813888L) != 0))
                            {
                                value = _headers._Translate;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 29:
                    {
                        if ("Access-Control-Request-Method".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 4398046511104L) != 0))
                            {
                                value = _headers._AccessControlRequestMethod;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 30:
                    {
                        if ("Access-Control-Request-Headers".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 8796093022208L) != 0))
                            {
                                value = _headers._AccessControlRequestHeaders;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;
}
            value = StringValues.Empty;
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
                            _bits |= 1L;
                            _headers._CacheControl = value;
                            return;
                        }
                    
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 131072L;
                            _headers._ContentRange = value;
                            return;
                        }
                    
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 524288L;
                            _headers._LastModified = value;
                            return;
                        }
                    
                        if ("Authorization".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 16777216L;
                            _headers._Authorization = value;
                            return;
                        }
                    
                        if ("If-None-Match".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 2147483648L;
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
                            _bits |= 1099511627776L;
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
                            _bits |= 134217728L;
                            _headers._From = value;
                            return;
                        }
                    
                        if ("Host".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 268435456L;
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
                            _bits |= 1048576L;
                            _headers._Accept = value;
                            return;
                        }
                    
                        if ("Cookie".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 33554432L;
                            _headers._Cookie = value;
                            return;
                        }
                    
                        if ("Expect".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 67108864L;
                            _headers._Expect = value;
                            return;
                        }
                    
                        if ("Origin".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 2199023255552L;
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
                            _bits |= 262144L;
                            _headers._Expires = value;
                            return;
                        }
                    
                        if ("Referer".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 68719476736L;
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
                            _bits |= 1073741824L;
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
                            _bits |= 137438953472L;
                            _headers._Range = value;
                            return;
                        }
                    }
                    break;

                case 14:
                    {
                        if ("Content-Length".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 2048L;
                            _headers._ContentLength = value;
                            return;
                        }
                    
                        if ("Accept-Charset".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 2097152L;
                            _headers._AcceptCharset = value;
                            return;
                        }
                    }
                    break;

                case 12:
                    {
                        if ("Content-Type".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 4096L;
                            _headers._ContentType = value;
                            return;
                        }
                    
                        if ("Max-Forwards".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 17179869184L;
                            _headers._MaxForwards = value;
                            return;
                        }
                    }
                    break;

                case 16:
                    {
                        if ("Content-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 8192L;
                            _headers._ContentEncoding = value;
                            return;
                        }
                    
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 16384L;
                            _headers._ContentLanguage = value;
                            return;
                        }
                    
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 32768L;
                            _headers._ContentLocation = value;
                            return;
                        }
                    }
                    break;

                case 11:
                    {
                        if ("Content-MD5".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 65536L;
                            _headers._ContentMD5 = value;
                            return;
                        }
                    }
                    break;

                case 15:
                    {
                        if ("Accept-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 4194304L;
                            _headers._AcceptEncoding = value;
                            return;
                        }
                    
                        if ("Accept-Language".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 8388608L;
                            _headers._AcceptLanguage = value;
                            return;
                        }
                    }
                    break;

                case 8:
                    {
                        if ("If-Match".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 536870912L;
                            _headers._IfMatch = value;
                            return;
                        }
                    
                        if ("If-Range".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 4294967296L;
                            _headers._IfRange = value;
                            return;
                        }
                    }
                    break;

                case 19:
                    {
                        if ("If-Unmodified-Since".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 8589934592L;
                            _headers._IfUnmodifiedSince = value;
                            return;
                        }
                    
                        if ("Proxy-Authorization".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 34359738368L;
                            _headers._ProxyAuthorization = value;
                            return;
                        }
                    }
                    break;

                case 2:
                    {
                        if ("TE".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 274877906944L;
                            _headers._TE = value;
                            return;
                        }
                    }
                    break;

                case 9:
                    {
                        if ("Translate".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 549755813888L;
                            _headers._Translate = value;
                            return;
                        }
                    }
                    break;

                case 29:
                    {
                        if ("Access-Control-Request-Method".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 4398046511104L;
                            _headers._AccessControlRequestMethod = value;
                            return;
                        }
                    }
                    break;

                case 30:
                    {
                        if ("Access-Control-Request-Headers".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 8796093022208L;
                            _headers._AccessControlRequestHeaders = value;
                            return;
                        }
                    }
                    break;
}
            Unknown[key] = value;
        }
        protected override void AddValueFast(string key, StringValues value)
        {
            switch (key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 1L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 1L;
                            _headers._CacheControl = value;
                            return;
                        }
                    
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 131072L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 131072L;
                            _headers._ContentRange = value;
                            return;
                        }
                    
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 524288L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 524288L;
                            _headers._LastModified = value;
                            return;
                        }
                    
                        if ("Authorization".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 16777216L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 16777216L;
                            _headers._Authorization = value;
                            return;
                        }
                    
                        if ("If-None-Match".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 2147483648L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 2147483648L;
                            _headers._IfNoneMatch = value;
                            return;
                        }
                    }
                    break;
            
                case 10:
                    {
                        if ("Connection".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 2L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 2L;
                            _headers._Connection = value;
                            return;
                        }
                    
                        if ("Keep-Alive".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 8L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 8L;
                            _headers._KeepAlive = value;
                            return;
                        }
                    
                        if ("User-Agent".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 1099511627776L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 1099511627776L;
                            _headers._UserAgent = value;
                            return;
                        }
                    }
                    break;
            
                case 4:
                    {
                        if ("Date".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 4L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 4L;
                            _headers._Date = value;
                            return;
                        }
                    
                        if ("From".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 134217728L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 134217728L;
                            _headers._From = value;
                            return;
                        }
                    
                        if ("Host".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 268435456L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 268435456L;
                            _headers._Host = value;
                            return;
                        }
                    }
                    break;
            
                case 6:
                    {
                        if ("Pragma".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 16L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 16L;
                            _headers._Pragma = value;
                            return;
                        }
                    
                        if ("Accept".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 1048576L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 1048576L;
                            _headers._Accept = value;
                            return;
                        }
                    
                        if ("Cookie".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 33554432L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 33554432L;
                            _headers._Cookie = value;
                            return;
                        }
                    
                        if ("Expect".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 67108864L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 67108864L;
                            _headers._Expect = value;
                            return;
                        }
                    
                        if ("Origin".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 2199023255552L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 2199023255552L;
                            _headers._Origin = value;
                            return;
                        }
                    }
                    break;
            
                case 7:
                    {
                        if ("Trailer".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 32L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 32L;
                            _headers._Trailer = value;
                            return;
                        }
                    
                        if ("Upgrade".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 128L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 128L;
                            _headers._Upgrade = value;
                            return;
                        }
                    
                        if ("Warning".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 512L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 512L;
                            _headers._Warning = value;
                            return;
                        }
                    
                        if ("Expires".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 262144L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 262144L;
                            _headers._Expires = value;
                            return;
                        }
                    
                        if ("Referer".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 68719476736L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 68719476736L;
                            _headers._Referer = value;
                            return;
                        }
                    }
                    break;
            
                case 17:
                    {
                        if ("Transfer-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 64L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 64L;
                            _headers._TransferEncoding = value;
                            return;
                        }
                    
                        if ("If-Modified-Since".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 1073741824L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 1073741824L;
                            _headers._IfModifiedSince = value;
                            return;
                        }
                    }
                    break;
            
                case 3:
                    {
                        if ("Via".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 256L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
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
                            if (((_bits & 1024L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 1024L;
                            _headers._Allow = value;
                            return;
                        }
                    
                        if ("Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 137438953472L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 137438953472L;
                            _headers._Range = value;
                            return;
                        }
                    }
                    break;
            
                case 14:
                    {
                        if ("Content-Length".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 2048L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 2048L;
                            _headers._ContentLength = value;
                            return;
                        }
                    
                        if ("Accept-Charset".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 2097152L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 2097152L;
                            _headers._AcceptCharset = value;
                            return;
                        }
                    }
                    break;
            
                case 12:
                    {
                        if ("Content-Type".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 4096L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 4096L;
                            _headers._ContentType = value;
                            return;
                        }
                    
                        if ("Max-Forwards".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 17179869184L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 17179869184L;
                            _headers._MaxForwards = value;
                            return;
                        }
                    }
                    break;
            
                case 16:
                    {
                        if ("Content-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 8192L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 8192L;
                            _headers._ContentEncoding = value;
                            return;
                        }
                    
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 16384L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 16384L;
                            _headers._ContentLanguage = value;
                            return;
                        }
                    
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 32768L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 32768L;
                            _headers._ContentLocation = value;
                            return;
                        }
                    }
                    break;
            
                case 11:
                    {
                        if ("Content-MD5".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 65536L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 65536L;
                            _headers._ContentMD5 = value;
                            return;
                        }
                    }
                    break;
            
                case 15:
                    {
                        if ("Accept-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 4194304L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 4194304L;
                            _headers._AcceptEncoding = value;
                            return;
                        }
                    
                        if ("Accept-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 8388608L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 8388608L;
                            _headers._AcceptLanguage = value;
                            return;
                        }
                    }
                    break;
            
                case 8:
                    {
                        if ("If-Match".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 536870912L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 536870912L;
                            _headers._IfMatch = value;
                            return;
                        }
                    
                        if ("If-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 4294967296L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 4294967296L;
                            _headers._IfRange = value;
                            return;
                        }
                    }
                    break;
            
                case 19:
                    {
                        if ("If-Unmodified-Since".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 8589934592L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 8589934592L;
                            _headers._IfUnmodifiedSince = value;
                            return;
                        }
                    
                        if ("Proxy-Authorization".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 34359738368L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 34359738368L;
                            _headers._ProxyAuthorization = value;
                            return;
                        }
                    }
                    break;
            
                case 2:
                    {
                        if ("TE".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 274877906944L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 274877906944L;
                            _headers._TE = value;
                            return;
                        }
                    }
                    break;
            
                case 9:
                    {
                        if ("Translate".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 549755813888L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 549755813888L;
                            _headers._Translate = value;
                            return;
                        }
                    }
                    break;
            
                case 29:
                    {
                        if ("Access-Control-Request-Method".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 4398046511104L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 4398046511104L;
                            _headers._AccessControlRequestMethod = value;
                            return;
                        }
                    }
                    break;
            
                case 30:
                    {
                        if ("Access-Control-Request-Headers".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 8796093022208L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 8796093022208L;
                            _headers._AccessControlRequestHeaders = value;
                            return;
                        }
                    }
                    break;
            }
            Unknown.Add(key, value);
        }
        protected override bool RemoveFast(string key)
        {
            switch (key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 1L) != 0))
                            {
                                _bits &= ~1L;
                                _headers._CacheControl = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 131072L) != 0))
                            {
                                _bits &= ~131072L;
                                _headers._ContentRange = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 524288L) != 0))
                            {
                                _bits &= ~524288L;
                                _headers._LastModified = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Authorization".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 16777216L) != 0))
                            {
                                _bits &= ~16777216L;
                                _headers._Authorization = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("If-None-Match".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 2147483648L) != 0))
                            {
                                _bits &= ~2147483648L;
                                _headers._IfNoneMatch = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 10:
                    {
                        if ("Connection".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 2L) != 0))
                            {
                                _bits &= ~2L;
                                _headers._Connection = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Keep-Alive".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 8L) != 0))
                            {
                                _bits &= ~8L;
                                _headers._KeepAlive = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("User-Agent".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 1099511627776L) != 0))
                            {
                                _bits &= ~1099511627776L;
                                _headers._UserAgent = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 4:
                    {
                        if ("Date".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 4L) != 0))
                            {
                                _bits &= ~4L;
                                _headers._Date = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("From".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 134217728L) != 0))
                            {
                                _bits &= ~134217728L;
                                _headers._From = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Host".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 268435456L) != 0))
                            {
                                _bits &= ~268435456L;
                                _headers._Host = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 6:
                    {
                        if ("Pragma".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 16L) != 0))
                            {
                                _bits &= ~16L;
                                _headers._Pragma = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Accept".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 1048576L) != 0))
                            {
                                _bits &= ~1048576L;
                                _headers._Accept = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Cookie".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 33554432L) != 0))
                            {
                                _bits &= ~33554432L;
                                _headers._Cookie = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Expect".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 67108864L) != 0))
                            {
                                _bits &= ~67108864L;
                                _headers._Expect = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Origin".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 2199023255552L) != 0))
                            {
                                _bits &= ~2199023255552L;
                                _headers._Origin = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 7:
                    {
                        if ("Trailer".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 32L) != 0))
                            {
                                _bits &= ~32L;
                                _headers._Trailer = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Upgrade".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 128L) != 0))
                            {
                                _bits &= ~128L;
                                _headers._Upgrade = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Warning".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 512L) != 0))
                            {
                                _bits &= ~512L;
                                _headers._Warning = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Expires".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 262144L) != 0))
                            {
                                _bits &= ~262144L;
                                _headers._Expires = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Referer".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 68719476736L) != 0))
                            {
                                _bits &= ~68719476736L;
                                _headers._Referer = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 17:
                    {
                        if ("Transfer-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 64L) != 0))
                            {
                                _bits &= ~64L;
                                _headers._TransferEncoding = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("If-Modified-Since".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 1073741824L) != 0))
                            {
                                _bits &= ~1073741824L;
                                _headers._IfModifiedSince = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 3:
                    {
                        if ("Via".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 256L) != 0))
                            {
                                _bits &= ~256L;
                                _headers._Via = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 5:
                    {
                        if ("Allow".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 1024L) != 0))
                            {
                                _bits &= ~1024L;
                                _headers._Allow = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Range".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 137438953472L) != 0))
                            {
                                _bits &= ~137438953472L;
                                _headers._Range = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 14:
                    {
                        if ("Content-Length".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 2048L) != 0))
                            {
                                _bits &= ~2048L;
                                _headers._ContentLength = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Accept-Charset".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 2097152L) != 0))
                            {
                                _bits &= ~2097152L;
                                _headers._AcceptCharset = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 12:
                    {
                        if ("Content-Type".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 4096L) != 0))
                            {
                                _bits &= ~4096L;
                                _headers._ContentType = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Max-Forwards".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 17179869184L) != 0))
                            {
                                _bits &= ~17179869184L;
                                _headers._MaxForwards = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 16:
                    {
                        if ("Content-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 8192L) != 0))
                            {
                                _bits &= ~8192L;
                                _headers._ContentEncoding = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 16384L) != 0))
                            {
                                _bits &= ~16384L;
                                _headers._ContentLanguage = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 32768L) != 0))
                            {
                                _bits &= ~32768L;
                                _headers._ContentLocation = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 11:
                    {
                        if ("Content-MD5".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 65536L) != 0))
                            {
                                _bits &= ~65536L;
                                _headers._ContentMD5 = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 15:
                    {
                        if ("Accept-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 4194304L) != 0))
                            {
                                _bits &= ~4194304L;
                                _headers._AcceptEncoding = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Accept-Language".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 8388608L) != 0))
                            {
                                _bits &= ~8388608L;
                                _headers._AcceptLanguage = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 8:
                    {
                        if ("If-Match".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 536870912L) != 0))
                            {
                                _bits &= ~536870912L;
                                _headers._IfMatch = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("If-Range".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 4294967296L) != 0))
                            {
                                _bits &= ~4294967296L;
                                _headers._IfRange = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 19:
                    {
                        if ("If-Unmodified-Since".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 8589934592L) != 0))
                            {
                                _bits &= ~8589934592L;
                                _headers._IfUnmodifiedSince = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Proxy-Authorization".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 34359738368L) != 0))
                            {
                                _bits &= ~34359738368L;
                                _headers._ProxyAuthorization = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 2:
                    {
                        if ("TE".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 274877906944L) != 0))
                            {
                                _bits &= ~274877906944L;
                                _headers._TE = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 9:
                    {
                        if ("Translate".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 549755813888L) != 0))
                            {
                                _bits &= ~549755813888L;
                                _headers._Translate = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 29:
                    {
                        if ("Access-Control-Request-Method".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 4398046511104L) != 0))
                            {
                                _bits &= ~4398046511104L;
                                _headers._AccessControlRequestMethod = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 30:
                    {
                        if ("Access-Control-Request-Headers".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 8796093022208L) != 0))
                            {
                                _bits &= ~8796093022208L;
                                _headers._AccessControlRequestHeaders = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            }
            return MaybeUnknown?.Remove(key) ?? false;
        }
        protected override void ClearFast()
        {
            _bits = 0;
            _headers = default(HeaderReferences);
            MaybeUnknown?.Clear();
        }
        
        protected override void CopyToFast(KeyValuePair<string, StringValues>[] array, int arrayIndex)
        {
            if (arrayIndex < 0)
            {
                ThrowArgumentException();
            }
            
                if (((_bits & 1L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Cache-Control", _headers._CacheControl);
                    ++arrayIndex;
                }
            
                if (((_bits & 2L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Connection", _headers._Connection);
                    ++arrayIndex;
                }
            
                if (((_bits & 4L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Date", _headers._Date);
                    ++arrayIndex;
                }
            
                if (((_bits & 8L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Keep-Alive", _headers._KeepAlive);
                    ++arrayIndex;
                }
            
                if (((_bits & 16L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Pragma", _headers._Pragma);
                    ++arrayIndex;
                }
            
                if (((_bits & 32L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Trailer", _headers._Trailer);
                    ++arrayIndex;
                }
            
                if (((_bits & 64L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Transfer-Encoding", _headers._TransferEncoding);
                    ++arrayIndex;
                }
            
                if (((_bits & 128L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Upgrade", _headers._Upgrade);
                    ++arrayIndex;
                }
            
                if (((_bits & 256L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Via", _headers._Via);
                    ++arrayIndex;
                }
            
                if (((_bits & 512L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Warning", _headers._Warning);
                    ++arrayIndex;
                }
            
                if (((_bits & 1024L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Allow", _headers._Allow);
                    ++arrayIndex;
                }
            
                if (((_bits & 2048L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Length", _headers._ContentLength);
                    ++arrayIndex;
                }
            
                if (((_bits & 4096L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Type", _headers._ContentType);
                    ++arrayIndex;
                }
            
                if (((_bits & 8192L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Encoding", _headers._ContentEncoding);
                    ++arrayIndex;
                }
            
                if (((_bits & 16384L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Language", _headers._ContentLanguage);
                    ++arrayIndex;
                }
            
                if (((_bits & 32768L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Location", _headers._ContentLocation);
                    ++arrayIndex;
                }
            
                if (((_bits & 65536L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-MD5", _headers._ContentMD5);
                    ++arrayIndex;
                }
            
                if (((_bits & 131072L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Range", _headers._ContentRange);
                    ++arrayIndex;
                }
            
                if (((_bits & 262144L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Expires", _headers._Expires);
                    ++arrayIndex;
                }
            
                if (((_bits & 524288L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Last-Modified", _headers._LastModified);
                    ++arrayIndex;
                }
            
                if (((_bits & 1048576L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Accept", _headers._Accept);
                    ++arrayIndex;
                }
            
                if (((_bits & 2097152L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Accept-Charset", _headers._AcceptCharset);
                    ++arrayIndex;
                }
            
                if (((_bits & 4194304L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Accept-Encoding", _headers._AcceptEncoding);
                    ++arrayIndex;
                }
            
                if (((_bits & 8388608L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Accept-Language", _headers._AcceptLanguage);
                    ++arrayIndex;
                }
            
                if (((_bits & 16777216L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Authorization", _headers._Authorization);
                    ++arrayIndex;
                }
            
                if (((_bits & 33554432L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Cookie", _headers._Cookie);
                    ++arrayIndex;
                }
            
                if (((_bits & 67108864L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Expect", _headers._Expect);
                    ++arrayIndex;
                }
            
                if (((_bits & 134217728L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("From", _headers._From);
                    ++arrayIndex;
                }
            
                if (((_bits & 268435456L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Host", _headers._Host);
                    ++arrayIndex;
                }
            
                if (((_bits & 536870912L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("If-Match", _headers._IfMatch);
                    ++arrayIndex;
                }
            
                if (((_bits & 1073741824L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("If-Modified-Since", _headers._IfModifiedSince);
                    ++arrayIndex;
                }
            
                if (((_bits & 2147483648L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("If-None-Match", _headers._IfNoneMatch);
                    ++arrayIndex;
                }
            
                if (((_bits & 4294967296L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("If-Range", _headers._IfRange);
                    ++arrayIndex;
                }
            
                if (((_bits & 8589934592L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("If-Unmodified-Since", _headers._IfUnmodifiedSince);
                    ++arrayIndex;
                }
            
                if (((_bits & 17179869184L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Max-Forwards", _headers._MaxForwards);
                    ++arrayIndex;
                }
            
                if (((_bits & 34359738368L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Proxy-Authorization", _headers._ProxyAuthorization);
                    ++arrayIndex;
                }
            
                if (((_bits & 68719476736L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Referer", _headers._Referer);
                    ++arrayIndex;
                }
            
                if (((_bits & 137438953472L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Range", _headers._Range);
                    ++arrayIndex;
                }
            
                if (((_bits & 274877906944L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("TE", _headers._TE);
                    ++arrayIndex;
                }
            
                if (((_bits & 549755813888L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Translate", _headers._Translate);
                    ++arrayIndex;
                }
            
                if (((_bits & 1099511627776L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("User-Agent", _headers._UserAgent);
                    ++arrayIndex;
                }
            
                if (((_bits & 2199023255552L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Origin", _headers._Origin);
                    ++arrayIndex;
                }
            
                if (((_bits & 4398046511104L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Access-Control-Request-Method", _headers._AccessControlRequestMethod);
                    ++arrayIndex;
                }
            
                if (((_bits & 8796093022208L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Access-Control-Request-Headers", _headers._AccessControlRequestHeaders);
                    ++arrayIndex;
                }
            
            ((ICollection<KeyValuePair<string, StringValues>>)MaybeUnknown)?.CopyTo(array, arrayIndex);
        }
        
        
        public unsafe void Append(byte[] keyBytes, int keyOffset, int keyLength, string value)
        {
            fixed (byte* ptr = &keyBytes[keyOffset]) 
            { 
                var pUB = ptr; 
                var pUL = (ulong*)pUB; 
                var pUI = (uint*)pUB; 
                var pUS = (ushort*)pUB;
                switch (keyLength)
                {
                    case 13:
                        {
                            if ((((pUL[0] & 16131893727263186911uL) == 5711458528024281411uL) && ((pUI[2] & 3755991007u) == 1330795598u) && ((pUB[12] & 223u) == 76u))) 
                            {
                                if (((_bits & 1L) != 0))
                                {
                                    _headers._CacheControl = AppendValue(_headers._CacheControl, value);
                                }
                                else
                                {
                                    _bits |= 1L;
                                    _headers._CacheControl = new StringValues(value);
                                }
                                return;
                            }
                        
                            if ((((pUL[0] & 18437701552104792031uL) == 3266321689424580419uL) && ((pUI[2] & 3755991007u) == 1196310866u) && ((pUB[12] & 223u) == 69u))) 
                            {
                                if (((_bits & 131072L) != 0))
                                {
                                    _headers._ContentRange = AppendValue(_headers._ContentRange, value);
                                }
                                else
                                {
                                    _bits |= 131072L;
                                    _headers._ContentRange = new StringValues(value);
                                }
                                return;
                            }
                        
                            if ((((pUL[0] & 16131858680330051551uL) == 4922237774822850892uL) && ((pUI[2] & 3755991007u) == 1162430025u) && ((pUB[12] & 223u) == 68u))) 
                            {
                                if (((_bits & 524288L) != 0))
                                {
                                    _headers._LastModified = AppendValue(_headers._LastModified, value);
                                }
                                else
                                {
                                    _bits |= 524288L;
                                    _headers._LastModified = new StringValues(value);
                                }
                                return;
                            }
                        
                            if ((((pUL[0] & 16131858542891098079uL) == 6505821637182772545uL) && ((pUI[2] & 3755991007u) == 1330205761u) && ((pUB[12] & 223u) == 78u))) 
                            {
                                if (((_bits & 16777216L) != 0))
                                {
                                    _headers._Authorization = AppendValue(_headers._Authorization, value);
                                }
                                else
                                {
                                    _bits |= 16777216L;
                                    _headers._Authorization = new StringValues(value);
                                }
                                return;
                            }
                        
                            if ((((pUL[0] & 18437701552106889183uL) == 3262099607620765257uL) && ((pUI[2] & 3755991007u) == 1129595213u) && ((pUB[12] & 223u) == 72u))) 
                            {
                                if (((_bits & 2147483648L) != 0))
                                {
                                    _headers._IfNoneMatch = AppendValue(_headers._IfNoneMatch, value);
                                }
                                else
                                {
                                    _bits |= 2147483648L;
                                    _headers._IfNoneMatch = new StringValues(value);
                                }
                                return;
                            }
                        }
                        break;
                
                    case 10:
                        {
                            if ((((pUL[0] & 16131858542891098079uL) == 5283922227757993795uL) && ((pUS[4] & 57311u) == 20047u))) 
                            {
                                if (((_bits & 2L) != 0))
                                {
                                    _headers._Connection = AppendValue(_headers._Connection, value);
                                }
                                else
                                {
                                    _bits |= 2L;
                                    _headers._Connection = new StringValues(value);
                                }
                                return;
                            }
                        
                            if ((((pUL[0] & 16131858680330051551uL) == 5281668125874799947uL) && ((pUS[4] & 57311u) == 17750u))) 
                            {
                                if (((_bits & 8L) != 0))
                                {
                                    _headers._KeepAlive = AppendValue(_headers._KeepAlive, value);
                                }
                                else
                                {
                                    _bits |= 8L;
                                    _headers._KeepAlive = new StringValues(value);
                                }
                                return;
                            }
                        
                            if ((((pUL[0] & 16131858680330051551uL) == 4992030374873092949uL) && ((pUS[4] & 57311u) == 21582u))) 
                            {
                                if (((_bits & 1099511627776L) != 0))
                                {
                                    _headers._UserAgent = AppendValue(_headers._UserAgent, value);
                                }
                                else
                                {
                                    _bits |= 1099511627776L;
                                    _headers._UserAgent = new StringValues(value);
                                }
                                return;
                            }
                        }
                        break;
                
                    case 4:
                        {
                            if ((((pUI[0] & 3755991007u) == 1163149636u))) 
                            {
                                if (((_bits & 4L) != 0))
                                {
                                    _headers._Date = AppendValue(_headers._Date, value);
                                }
                                else
                                {
                                    _bits |= 4L;
                                    _headers._Date = new StringValues(value);
                                }
                                return;
                            }
                        
                            if ((((pUI[0] & 3755991007u) == 1297044038u))) 
                            {
                                if (((_bits & 134217728L) != 0))
                                {
                                    _headers._From = AppendValue(_headers._From, value);
                                }
                                else
                                {
                                    _bits |= 134217728L;
                                    _headers._From = new StringValues(value);
                                }
                                return;
                            }
                        
                            if ((((pUI[0] & 3755991007u) == 1414745928u))) 
                            {
                                if (((_bits & 268435456L) != 0))
                                {
                                    _headers._Host = AppendValue(_headers._Host, value);
                                }
                                else
                                {
                                    _bits |= 268435456L;
                                    _headers._Host = new StringValues(value);
                                }
                                return;
                            }
                        }
                        break;
                
                    case 6:
                        {
                            if ((((pUI[0] & 3755991007u) == 1195463248u) && ((pUS[2] & 57311u) == 16717u))) 
                            {
                                if (((_bits & 16L) != 0))
                                {
                                    _headers._Pragma = AppendValue(_headers._Pragma, value);
                                }
                                else
                                {
                                    _bits |= 16L;
                                    _headers._Pragma = new StringValues(value);
                                }
                                return;
                            }
                        
                            if ((((pUI[0] & 3755991007u) == 1162036033u) && ((pUS[2] & 57311u) == 21584u))) 
                            {
                                if (((_bits & 1048576L) != 0))
                                {
                                    _headers._Accept = AppendValue(_headers._Accept, value);
                                }
                                else
                                {
                                    _bits |= 1048576L;
                                    _headers._Accept = new StringValues(value);
                                }
                                return;
                            }
                        
                            if ((((pUI[0] & 3755991007u) == 1263488835u) && ((pUS[2] & 57311u) == 17737u))) 
                            {
                                if (((_bits & 33554432L) != 0))
                                {
                                    _headers._Cookie = AppendValue(_headers._Cookie, value);
                                }
                                else
                                {
                                    _bits |= 33554432L;
                                    _headers._Cookie = new StringValues(value);
                                }
                                return;
                            }
                        
                            if ((((pUI[0] & 3755991007u) == 1162893381u) && ((pUS[2] & 57311u) == 21571u))) 
                            {
                                if (((_bits & 67108864L) != 0))
                                {
                                    _headers._Expect = AppendValue(_headers._Expect, value);
                                }
                                else
                                {
                                    _bits |= 67108864L;
                                    _headers._Expect = new StringValues(value);
                                }
                                return;
                            }
                        
                            if ((((pUI[0] & 3755991007u) == 1195987535u) && ((pUS[2] & 57311u) == 20041u))) 
                            {
                                if (((_bits & 2199023255552L) != 0))
                                {
                                    _headers._Origin = AppendValue(_headers._Origin, value);
                                }
                                else
                                {
                                    _bits |= 2199023255552L;
                                    _headers._Origin = new StringValues(value);
                                }
                                return;
                            }
                        }
                        break;
                
                    case 7:
                        {
                            if ((((pUI[0] & 3755991007u) == 1229017684u) && ((pUS[2] & 57311u) == 17740u) && ((pUB[6] & 223u) == 82u))) 
                            {
                                if (((_bits & 32L) != 0))
                                {
                                    _headers._Trailer = AppendValue(_headers._Trailer, value);
                                }
                                else
                                {
                                    _bits |= 32L;
                                    _headers._Trailer = new StringValues(value);
                                }
                                return;
                            }
                        
                            if ((((pUI[0] & 3755991007u) == 1380405333u) && ((pUS[2] & 57311u) == 17473u) && ((pUB[6] & 223u) == 69u))) 
                            {
                                if (((_bits & 128L) != 0))
                                {
                                    _headers._Upgrade = AppendValue(_headers._Upgrade, value);
                                }
                                else
                                {
                                    _bits |= 128L;
                                    _headers._Upgrade = new StringValues(value);
                                }
                                return;
                            }
                        
                            if ((((pUI[0] & 3755991007u) == 1314013527u) && ((pUS[2] & 57311u) == 20041u) && ((pUB[6] & 223u) == 71u))) 
                            {
                                if (((_bits & 512L) != 0))
                                {
                                    _headers._Warning = AppendValue(_headers._Warning, value);
                                }
                                else
                                {
                                    _bits |= 512L;
                                    _headers._Warning = new StringValues(value);
                                }
                                return;
                            }
                        
                            if ((((pUI[0] & 3755991007u) == 1230002245u) && ((pUS[2] & 57311u) == 17746u) && ((pUB[6] & 223u) == 83u))) 
                            {
                                if (((_bits & 262144L) != 0))
                                {
                                    _headers._Expires = AppendValue(_headers._Expires, value);
                                }
                                else
                                {
                                    _bits |= 262144L;
                                    _headers._Expires = new StringValues(value);
                                }
                                return;
                            }
                        
                            if ((((pUI[0] & 3755991007u) == 1162233170u) && ((pUS[2] & 57311u) == 17746u) && ((pUB[6] & 223u) == 82u))) 
                            {
                                if (((_bits & 68719476736L) != 0))
                                {
                                    _headers._Referer = AppendValue(_headers._Referer, value);
                                }
                                else
                                {
                                    _bits |= 68719476736L;
                                    _headers._Referer = new StringValues(value);
                                }
                                return;
                            }
                        }
                        break;
                
                    case 17:
                        {
                            if ((((pUL[0] & 16131858542891098079uL) == 5928221808112259668uL) && ((pUL[1] & 16131858542891098111uL) == 5641115115480565037uL) && ((pUB[16] & 223u) == 71u))) 
                            {
                                if (((_bits & 64L) != 0))
                                {
                                    _headers._TransferEncoding = AppendValue(_headers._TransferEncoding, value);
                                }
                                else
                                {
                                    _bits |= 64L;
                                    _headers._TransferEncoding = new StringValues(value);
                                }
                                return;
                            }
                        
                            if ((((pUL[0] & 16131858542893195231uL) == 5064654363342751305uL) && ((pUL[1] & 16131858543427968991uL) == 4849894470315165001uL) && ((pUB[16] & 223u) == 69u))) 
                            {
                                if (((_bits & 1073741824L) != 0))
                                {
                                    _headers._IfModifiedSince = AppendValue(_headers._IfModifiedSince, value);
                                }
                                else
                                {
                                    _bits |= 1073741824L;
                                    _headers._IfModifiedSince = new StringValues(value);
                                }
                                return;
                            }
                        }
                        break;
                
                    case 3:
                        {
                            if ((((pUS[0] & 57311u) == 18774u) && ((pUB[2] & 223u) == 65u))) 
                            {
                                if (((_bits & 256L) != 0))
                                {
                                    _headers._Via = AppendValue(_headers._Via, value);
                                }
                                else
                                {
                                    _bits |= 256L;
                                    _headers._Via = new StringValues(value);
                                }
                                return;
                            }
                        }
                        break;
                
                    case 5:
                        {
                            if ((((pUI[0] & 3755991007u) == 1330400321u) && ((pUB[4] & 223u) == 87u))) 
                            {
                                if (((_bits & 1024L) != 0))
                                {
                                    _headers._Allow = AppendValue(_headers._Allow, value);
                                }
                                else
                                {
                                    _bits |= 1024L;
                                    _headers._Allow = new StringValues(value);
                                }
                                return;
                            }
                        
                            if ((((pUI[0] & 3755991007u) == 1196310866u) && ((pUB[4] & 223u) == 69u))) 
                            {
                                if (((_bits & 137438953472L) != 0))
                                {
                                    _headers._Range = AppendValue(_headers._Range, value);
                                }
                                else
                                {
                                    _bits |= 137438953472L;
                                    _headers._Range = new StringValues(value);
                                }
                                return;
                            }
                        }
                        break;
                
                    case 14:
                        {
                            if ((((pUL[0] & 18437701552104792031uL) == 3266321689424580419uL) && ((pUI[2] & 3755991007u) == 1196311884u) && ((pUS[6] & 57311u) == 18516u))) 
                            {
                                if (((_bits & 2048L) != 0))
                                {
                                    _headers._ContentLength = AppendValue(_headers._ContentLength, value);
                                }
                                else
                                {
                                    _bits |= 2048L;
                                    _headers._ContentLength = new StringValues(value);
                                }
                                return;
                            }
                        
                            if ((((pUL[0] & 16140865742145839071uL) == 4840617878229304129uL) && ((pUI[2] & 3755991007u) == 1397899592u) && ((pUS[6] & 57311u) == 21573u))) 
                            {
                                if (((_bits & 2097152L) != 0))
                                {
                                    _headers._AcceptCharset = AppendValue(_headers._AcceptCharset, value);
                                }
                                else
                                {
                                    _bits |= 2097152L;
                                    _headers._AcceptCharset = new StringValues(value);
                                }
                                return;
                            }
                        }
                        break;
                
                    case 12:
                        {
                            if ((((pUL[0] & 18437701552104792031uL) == 3266321689424580419uL) && ((pUI[2] & 3755991007u) == 1162893652u))) 
                            {
                                if (((_bits & 4096L) != 0))
                                {
                                    _headers._ContentType = AppendValue(_headers._ContentType, value);
                                }
                                else
                                {
                                    _bits |= 4096L;
                                    _headers._ContentType = new StringValues(value);
                                }
                                return;
                            }
                        
                            if ((((pUL[0] & 16131858543427968991uL) == 6292178792217067853uL) && ((pUI[2] & 3755991007u) == 1396986433u))) 
                            {
                                if (((_bits & 17179869184L) != 0))
                                {
                                    _headers._MaxForwards = AppendValue(_headers._MaxForwards, value);
                                }
                                else
                                {
                                    _bits |= 17179869184L;
                                    _headers._MaxForwards = new StringValues(value);
                                }
                                return;
                            }
                        }
                        break;
                
                    case 16:
                        {
                            if ((((pUL[0] & 18437701552104792031uL) == 3266321689424580419uL) && ((pUL[1] & 16131858542891098079uL) == 5138124782612729413uL))) 
                            {
                                if (((_bits & 8192L) != 0))
                                {
                                    _headers._ContentEncoding = AppendValue(_headers._ContentEncoding, value);
                                }
                                else
                                {
                                    _bits |= 8192L;
                                    _headers._ContentEncoding = new StringValues(value);
                                }
                                return;
                            }
                        
                            if ((((pUL[0] & 18437701552104792031uL) == 3266321689424580419uL) && ((pUL[1] & 16131858542891098079uL) == 4992030546487820620uL))) 
                            {
                                if (((_bits & 16384L) != 0))
                                {
                                    _headers._ContentLanguage = AppendValue(_headers._ContentLanguage, value);
                                }
                                else
                                {
                                    _bits |= 16384L;
                                    _headers._ContentLanguage = new StringValues(value);
                                }
                                return;
                            }
                        
                            if ((((pUL[0] & 18437701552104792031uL) == 3266321689424580419uL) && ((pUL[1] & 16131858542891098079uL) == 5642809484339531596uL))) 
                            {
                                if (((_bits & 32768L) != 0))
                                {
                                    _headers._ContentLocation = AppendValue(_headers._ContentLocation, value);
                                }
                                else
                                {
                                    _bits |= 32768L;
                                    _headers._ContentLocation = new StringValues(value);
                                }
                                return;
                            }
                        }
                        break;
                
                    case 11:
                        {
                            if ((((pUL[0] & 18437701552104792031uL) == 3266321689424580419uL) && ((pUS[4] & 57311u) == 17485u) && ((pUB[10] & 255u) == 53u))) 
                            {
                                if (((_bits & 65536L) != 0))
                                {
                                    _headers._ContentMD5 = AppendValue(_headers._ContentMD5, value);
                                }
                                else
                                {
                                    _bits |= 65536L;
                                    _headers._ContentMD5 = new StringValues(value);
                                }
                                return;
                            }
                        }
                        break;
                
                    case 15:
                        {
                            if ((((pUL[0] & 16140865742145839071uL) == 4984733066305160001uL) && ((pUI[2] & 3755991007u) == 1146045262u) && ((pUS[6] & 57311u) == 20041u) && ((pUB[14] & 223u) == 71u))) 
                            {
                                if (((_bits & 4194304L) != 0))
                                {
                                    _headers._AcceptEncoding = AppendValue(_headers._AcceptEncoding, value);
                                }
                                else
                                {
                                    _bits |= 4194304L;
                                    _headers._AcceptEncoding = new StringValues(value);
                                }
                                return;
                            }
                        
                            if ((((pUL[0] & 16140865742145839071uL) == 5489136224570655553uL) && ((pUI[2] & 3755991007u) == 1430736449u) && ((pUS[6] & 57311u) == 18241u) && ((pUB[14] & 223u) == 69u))) 
                            {
                                if (((_bits & 8388608L) != 0))
                                {
                                    _headers._AcceptLanguage = AppendValue(_headers._AcceptLanguage, value);
                                }
                                else
                                {
                                    _bits |= 8388608L;
                                    _headers._AcceptLanguage = new StringValues(value);
                                }
                                return;
                            }
                        }
                        break;
                
                    case 8:
                        {
                            if ((((pUL[0] & 16131858542893195231uL) == 5207098233614845513uL))) 
                            {
                                if (((_bits & 536870912L) != 0))
                                {
                                    _headers._IfMatch = AppendValue(_headers._IfMatch, value);
                                }
                                else
                                {
                                    _bits |= 536870912L;
                                    _headers._IfMatch = new StringValues(value);
                                }
                                return;
                            }
                        
                            if ((((pUL[0] & 16131858542893195231uL) == 4992044754422023753uL))) 
                            {
                                if (((_bits & 4294967296L) != 0))
                                {
                                    _headers._IfRange = AppendValue(_headers._IfRange, value);
                                }
                                else
                                {
                                    _bits |= 4294967296L;
                                    _headers._IfRange = new StringValues(value);
                                }
                                return;
                            }
                        }
                        break;
                
                    case 19:
                        {
                            if ((((pUL[0] & 16131858542893195231uL) == 4922237916571059785uL) && ((pUL[1] & 16131893727263186911uL) == 5283616559079179849uL) && ((pUS[8] & 57311u) == 17230u) && ((pUB[18] & 223u) == 69u))) 
                            {
                                if (((_bits & 8589934592L) != 0))
                                {
                                    _headers._IfUnmodifiedSince = AppendValue(_headers._IfUnmodifiedSince, value);
                                }
                                else
                                {
                                    _bits |= 8589934592L;
                                    _headers._IfUnmodifiedSince = new StringValues(value);
                                }
                                return;
                            }
                        
                            if ((((pUL[0] & 16131893727263186911uL) == 6143241228466999888uL) && ((pUL[1] & 16131858542891098079uL) == 6071233043632179284uL) && ((pUS[8] & 57311u) == 20297u) && ((pUB[18] & 223u) == 78u))) 
                            {
                                if (((_bits & 34359738368L) != 0))
                                {
                                    _headers._ProxyAuthorization = AppendValue(_headers._ProxyAuthorization, value);
                                }
                                else
                                {
                                    _bits |= 34359738368L;
                                    _headers._ProxyAuthorization = new StringValues(value);
                                }
                                return;
                            }
                        }
                        break;
                
                    case 2:
                        {
                            if ((((pUS[0] & 57311u) == 17748u))) 
                            {
                                if (((_bits & 274877906944L) != 0))
                                {
                                    _headers._TE = AppendValue(_headers._TE, value);
                                }
                                else
                                {
                                    _bits |= 274877906944L;
                                    _headers._TE = new StringValues(value);
                                }
                                return;
                            }
                        }
                        break;
                
                    case 9:
                        {
                            if ((((pUL[0] & 16131858542891098079uL) == 6071217693351039572uL) && ((pUB[8] & 223u) == 69u))) 
                            {
                                if (((_bits & 549755813888L) != 0))
                                {
                                    _headers._Translate = AppendValue(_headers._Translate, value);
                                }
                                else
                                {
                                    _bits |= 549755813888L;
                                    _headers._Translate = new StringValues(value);
                                }
                                return;
                            }
                        }
                        break;
                
                    case 29:
                        {
                            if ((((pUL[0] & 16140865742145839071uL) == 4840616791602578241uL) && ((pUL[1] & 16140865742145839071uL) == 5921472988629454415uL) && ((pUL[2] & 16140865742145839071uL) == 5561193831494668613uL) && ((pUI[6] & 3755991007u) == 1330140229u) && ((pUB[28] & 223u) == 68u))) 
                            {
                                if (((_bits & 4398046511104L) != 0))
                                {
                                    _headers._AccessControlRequestMethod = AppendValue(_headers._AccessControlRequestMethod, value);
                                }
                                else
                                {
                                    _bits |= 4398046511104L;
                                    _headers._AccessControlRequestMethod = new StringValues(value);
                                }
                                return;
                            }
                        }
                        break;
                
                    case 30:
                        {
                            if ((((pUL[0] & 16140865742145839071uL) == 4840616791602578241uL) && ((pUL[1] & 16140865742145839071uL) == 5921472988629454415uL) && ((pUL[2] & 16140865742145839071uL) == 5200905861305028933uL) && ((pUI[6] & 3755991007u) == 1162101061u) && ((pUS[14] & 57311u) == 21330u))) 
                            {
                                if (((_bits & 8796093022208L) != 0))
                                {
                                    _headers._AccessControlRequestHeaders = AppendValue(_headers._AccessControlRequestHeaders, value);
                                }
                                else
                                {
                                    _bits |= 8796093022208L;
                                    _headers._AccessControlRequestHeaders = new StringValues(value);
                                }
                                return;
                            }
                        }
                        break;
                }
            }
            var key = System.Text.Encoding.ASCII.GetString(keyBytes, keyOffset, keyLength);
            StringValues existing;
            Unknown.TryGetValue(key, out existing);
            Unknown[key] = AppendValue(existing, value);
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
            public StringValues _ContentLength;
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
                    
                        case 43:
                            goto state43;
                    
                    default:
                        goto state_default;
                }
                
                state0:
                    if (((_bits & 1L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Cache-Control", _collection._headers._CacheControl);
                        _state = 1;
                        return true;
                    }
                
                state1:
                    if (((_bits & 2L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Connection", _collection._headers._Connection);
                        _state = 2;
                        return true;
                    }
                
                state2:
                    if (((_bits & 4L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Date", _collection._headers._Date);
                        _state = 3;
                        return true;
                    }
                
                state3:
                    if (((_bits & 8L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Keep-Alive", _collection._headers._KeepAlive);
                        _state = 4;
                        return true;
                    }
                
                state4:
                    if (((_bits & 16L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Pragma", _collection._headers._Pragma);
                        _state = 5;
                        return true;
                    }
                
                state5:
                    if (((_bits & 32L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Trailer", _collection._headers._Trailer);
                        _state = 6;
                        return true;
                    }
                
                state6:
                    if (((_bits & 64L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Transfer-Encoding", _collection._headers._TransferEncoding);
                        _state = 7;
                        return true;
                    }
                
                state7:
                    if (((_bits & 128L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Upgrade", _collection._headers._Upgrade);
                        _state = 8;
                        return true;
                    }
                
                state8:
                    if (((_bits & 256L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Via", _collection._headers._Via);
                        _state = 9;
                        return true;
                    }
                
                state9:
                    if (((_bits & 512L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Warning", _collection._headers._Warning);
                        _state = 10;
                        return true;
                    }
                
                state10:
                    if (((_bits & 1024L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Allow", _collection._headers._Allow);
                        _state = 11;
                        return true;
                    }
                
                state11:
                    if (((_bits & 2048L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Length", _collection._headers._ContentLength);
                        _state = 12;
                        return true;
                    }
                
                state12:
                    if (((_bits & 4096L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Type", _collection._headers._ContentType);
                        _state = 13;
                        return true;
                    }
                
                state13:
                    if (((_bits & 8192L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Encoding", _collection._headers._ContentEncoding);
                        _state = 14;
                        return true;
                    }
                
                state14:
                    if (((_bits & 16384L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Language", _collection._headers._ContentLanguage);
                        _state = 15;
                        return true;
                    }
                
                state15:
                    if (((_bits & 32768L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Location", _collection._headers._ContentLocation);
                        _state = 16;
                        return true;
                    }
                
                state16:
                    if (((_bits & 65536L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-MD5", _collection._headers._ContentMD5);
                        _state = 17;
                        return true;
                    }
                
                state17:
                    if (((_bits & 131072L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Range", _collection._headers._ContentRange);
                        _state = 18;
                        return true;
                    }
                
                state18:
                    if (((_bits & 262144L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Expires", _collection._headers._Expires);
                        _state = 19;
                        return true;
                    }
                
                state19:
                    if (((_bits & 524288L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Last-Modified", _collection._headers._LastModified);
                        _state = 20;
                        return true;
                    }
                
                state20:
                    if (((_bits & 1048576L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Accept", _collection._headers._Accept);
                        _state = 21;
                        return true;
                    }
                
                state21:
                    if (((_bits & 2097152L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Accept-Charset", _collection._headers._AcceptCharset);
                        _state = 22;
                        return true;
                    }
                
                state22:
                    if (((_bits & 4194304L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Accept-Encoding", _collection._headers._AcceptEncoding);
                        _state = 23;
                        return true;
                    }
                
                state23:
                    if (((_bits & 8388608L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Accept-Language", _collection._headers._AcceptLanguage);
                        _state = 24;
                        return true;
                    }
                
                state24:
                    if (((_bits & 16777216L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Authorization", _collection._headers._Authorization);
                        _state = 25;
                        return true;
                    }
                
                state25:
                    if (((_bits & 33554432L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Cookie", _collection._headers._Cookie);
                        _state = 26;
                        return true;
                    }
                
                state26:
                    if (((_bits & 67108864L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Expect", _collection._headers._Expect);
                        _state = 27;
                        return true;
                    }
                
                state27:
                    if (((_bits & 134217728L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("From", _collection._headers._From);
                        _state = 28;
                        return true;
                    }
                
                state28:
                    if (((_bits & 268435456L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Host", _collection._headers._Host);
                        _state = 29;
                        return true;
                    }
                
                state29:
                    if (((_bits & 536870912L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("If-Match", _collection._headers._IfMatch);
                        _state = 30;
                        return true;
                    }
                
                state30:
                    if (((_bits & 1073741824L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("If-Modified-Since", _collection._headers._IfModifiedSince);
                        _state = 31;
                        return true;
                    }
                
                state31:
                    if (((_bits & 2147483648L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("If-None-Match", _collection._headers._IfNoneMatch);
                        _state = 32;
                        return true;
                    }
                
                state32:
                    if (((_bits & 4294967296L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("If-Range", _collection._headers._IfRange);
                        _state = 33;
                        return true;
                    }
                
                state33:
                    if (((_bits & 8589934592L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("If-Unmodified-Since", _collection._headers._IfUnmodifiedSince);
                        _state = 34;
                        return true;
                    }
                
                state34:
                    if (((_bits & 17179869184L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Max-Forwards", _collection._headers._MaxForwards);
                        _state = 35;
                        return true;
                    }
                
                state35:
                    if (((_bits & 34359738368L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Proxy-Authorization", _collection._headers._ProxyAuthorization);
                        _state = 36;
                        return true;
                    }
                
                state36:
                    if (((_bits & 68719476736L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Referer", _collection._headers._Referer);
                        _state = 37;
                        return true;
                    }
                
                state37:
                    if (((_bits & 137438953472L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Range", _collection._headers._Range);
                        _state = 38;
                        return true;
                    }
                
                state38:
                    if (((_bits & 274877906944L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("TE", _collection._headers._TE);
                        _state = 39;
                        return true;
                    }
                
                state39:
                    if (((_bits & 549755813888L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Translate", _collection._headers._Translate);
                        _state = 40;
                        return true;
                    }
                
                state40:
                    if (((_bits & 1099511627776L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("User-Agent", _collection._headers._UserAgent);
                        _state = 41;
                        return true;
                    }
                
                state41:
                    if (((_bits & 2199023255552L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Origin", _collection._headers._Origin);
                        _state = 42;
                        return true;
                    }
                
                state42:
                    if (((_bits & 4398046511104L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Request-Method", _collection._headers._AccessControlRequestMethod);
                        _state = 43;
                        return true;
                    }
                
                state43:
                    if (((_bits & 8796093022208L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Request-Headers", _collection._headers._AccessControlRequestHeaders);
                        _state = 44;
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

    public partial class FrameResponseHeaders
    {
        private static byte[] _headerBytes = new byte[]
        {
            13,10,67,97,99,104,101,45,67,111,110,116,114,111,108,58,32,13,10,67,111,110,110,101,99,116,105,111,110,58,32,13,10,68,97,116,101,58,32,13,10,75,101,101,112,45,65,108,105,118,101,58,32,13,10,80,114,97,103,109,97,58,32,13,10,84,114,97,105,108,101,114,58,32,13,10,84,114,97,110,115,102,101,114,45,69,110,99,111,100,105,110,103,58,32,13,10,85,112,103,114,97,100,101,58,32,13,10,86,105,97,58,32,13,10,87,97,114,110,105,110,103,58,32,13,10,65,108,108,111,119,58,32,13,10,67,111,110,116,101,110,116,45,76,101,110,103,116,104,58,32,13,10,67,111,110,116,101,110,116,45,84,121,112,101,58,32,13,10,67,111,110,116,101,110,116,45,69,110,99,111,100,105,110,103,58,32,13,10,67,111,110,116,101,110,116,45,76,97,110,103,117,97,103,101,58,32,13,10,67,111,110,116,101,110,116,45,76,111,99,97,116,105,111,110,58,32,13,10,67,111,110,116,101,110,116,45,77,68,53,58,32,13,10,67,111,110,116,101,110,116,45,82,97,110,103,101,58,32,13,10,69,120,112,105,114,101,115,58,32,13,10,76,97,115,116,45,77,111,100,105,102,105,101,100,58,32,13,10,65,99,99,101,112,116,45,82,97,110,103,101,115,58,32,13,10,65,103,101,58,32,13,10,69,84,97,103,58,32,13,10,76,111,99,97,116,105,111,110,58,32,13,10,80,114,111,120,121,45,65,117,116,104,101,116,105,99,97,116,101,58,32,13,10,82,101,116,114,121,45,65,102,116,101,114,58,32,13,10,83,101,114,118,101,114,58,32,13,10,83,101,116,45,67,111,111,107,105,101,58,32,13,10,86,97,114,121,58,32,13,10,87,87,87,45,65,117,116,104,101,110,116,105,99,97,116,101,58,32,13,10,65,99,99,101,115,115,45,67,111,110,116,114,111,108,45,65,108,108,111,119,45,67,114,101,100,101,110,116,105,97,108,115,58,32,13,10,65,99,99,101,115,115,45,67,111,110,116,114,111,108,45,65,108,108,111,119,45,72,101,97,100,101,114,115,58,32,13,10,65,99,99,101,115,115,45,67,111,110,116,114,111,108,45,65,108,108,111,119,45,77,101,116,104,111,100,115,58,32,13,10,65,99,99,101,115,115,45,67,111,110,116,114,111,108,45,65,108,108,111,119,45,79,114,105,103,105,110,58,32,13,10,65,99,99,101,115,115,45,67,111,110,116,114,111,108,45,69,120,112,111,115,101,45,72,101,97,100,101,114,115,58,32,13,10,65,99,99,101,115,115,45,67,111,110,116,114,111,108,45,77,97,120,45,65,103,101,58,32,
        };
        
        private long _bits = 0;
        private HeaderReferences _headers;
        
        public StringValues HeaderCacheControl
        {
            get
            {
                if (((_bits & 1L) != 0))
                {
                    return _headers._CacheControl;
                }
                return StringValues.Empty;
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
                if (((_bits & 2L) != 0))
                {
                    return _headers._Connection;
                }
                return StringValues.Empty;
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
                if (((_bits & 4L) != 0))
                {
                    return _headers._Date;
                }
                return StringValues.Empty;
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
                if (((_bits & 8L) != 0))
                {
                    return _headers._KeepAlive;
                }
                return StringValues.Empty;
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
                if (((_bits & 16L) != 0))
                {
                    return _headers._Pragma;
                }
                return StringValues.Empty;
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
                if (((_bits & 32L) != 0))
                {
                    return _headers._Trailer;
                }
                return StringValues.Empty;
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
                if (((_bits & 64L) != 0))
                {
                    return _headers._TransferEncoding;
                }
                return StringValues.Empty;
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
                if (((_bits & 128L) != 0))
                {
                    return _headers._Upgrade;
                }
                return StringValues.Empty;
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
                if (((_bits & 256L) != 0))
                {
                    return _headers._Via;
                }
                return StringValues.Empty;
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
                if (((_bits & 512L) != 0))
                {
                    return _headers._Warning;
                }
                return StringValues.Empty;
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
                if (((_bits & 1024L) != 0))
                {
                    return _headers._Allow;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 1024L;
                _headers._Allow = value; 
            }
        }
        public StringValues HeaderContentLength
        {
            get
            {
                if (((_bits & 2048L) != 0))
                {
                    return _headers._ContentLength;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 2048L;
                _headers._ContentLength = value; 
                _headers._rawContentLength = null;
            }
        }
        public StringValues HeaderContentType
        {
            get
            {
                if (((_bits & 4096L) != 0))
                {
                    return _headers._ContentType;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 4096L;
                _headers._ContentType = value; 
            }
        }
        public StringValues HeaderContentEncoding
        {
            get
            {
                if (((_bits & 8192L) != 0))
                {
                    return _headers._ContentEncoding;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 8192L;
                _headers._ContentEncoding = value; 
            }
        }
        public StringValues HeaderContentLanguage
        {
            get
            {
                if (((_bits & 16384L) != 0))
                {
                    return _headers._ContentLanguage;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 16384L;
                _headers._ContentLanguage = value; 
            }
        }
        public StringValues HeaderContentLocation
        {
            get
            {
                if (((_bits & 32768L) != 0))
                {
                    return _headers._ContentLocation;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 32768L;
                _headers._ContentLocation = value; 
            }
        }
        public StringValues HeaderContentMD5
        {
            get
            {
                if (((_bits & 65536L) != 0))
                {
                    return _headers._ContentMD5;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 65536L;
                _headers._ContentMD5 = value; 
            }
        }
        public StringValues HeaderContentRange
        {
            get
            {
                if (((_bits & 131072L) != 0))
                {
                    return _headers._ContentRange;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 131072L;
                _headers._ContentRange = value; 
            }
        }
        public StringValues HeaderExpires
        {
            get
            {
                if (((_bits & 262144L) != 0))
                {
                    return _headers._Expires;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 262144L;
                _headers._Expires = value; 
            }
        }
        public StringValues HeaderLastModified
        {
            get
            {
                if (((_bits & 524288L) != 0))
                {
                    return _headers._LastModified;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 524288L;
                _headers._LastModified = value; 
            }
        }
        public StringValues HeaderAcceptRanges
        {
            get
            {
                if (((_bits & 1048576L) != 0))
                {
                    return _headers._AcceptRanges;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 1048576L;
                _headers._AcceptRanges = value; 
            }
        }
        public StringValues HeaderAge
        {
            get
            {
                if (((_bits & 2097152L) != 0))
                {
                    return _headers._Age;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 2097152L;
                _headers._Age = value; 
            }
        }
        public StringValues HeaderETag
        {
            get
            {
                if (((_bits & 4194304L) != 0))
                {
                    return _headers._ETag;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 4194304L;
                _headers._ETag = value; 
            }
        }
        public StringValues HeaderLocation
        {
            get
            {
                if (((_bits & 8388608L) != 0))
                {
                    return _headers._Location;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 8388608L;
                _headers._Location = value; 
            }
        }
        public StringValues HeaderProxyAutheticate
        {
            get
            {
                if (((_bits & 16777216L) != 0))
                {
                    return _headers._ProxyAutheticate;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 16777216L;
                _headers._ProxyAutheticate = value; 
            }
        }
        public StringValues HeaderRetryAfter
        {
            get
            {
                if (((_bits & 33554432L) != 0))
                {
                    return _headers._RetryAfter;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 33554432L;
                _headers._RetryAfter = value; 
            }
        }
        public StringValues HeaderServer
        {
            get
            {
                if (((_bits & 67108864L) != 0))
                {
                    return _headers._Server;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 67108864L;
                _headers._Server = value; 
                _headers._rawServer = null;
            }
        }
        public StringValues HeaderSetCookie
        {
            get
            {
                if (((_bits & 134217728L) != 0))
                {
                    return _headers._SetCookie;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 134217728L;
                _headers._SetCookie = value; 
            }
        }
        public StringValues HeaderVary
        {
            get
            {
                if (((_bits & 268435456L) != 0))
                {
                    return _headers._Vary;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 268435456L;
                _headers._Vary = value; 
            }
        }
        public StringValues HeaderWWWAuthenticate
        {
            get
            {
                if (((_bits & 536870912L) != 0))
                {
                    return _headers._WWWAuthenticate;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 536870912L;
                _headers._WWWAuthenticate = value; 
            }
        }
        public StringValues HeaderAccessControlAllowCredentials
        {
            get
            {
                if (((_bits & 1073741824L) != 0))
                {
                    return _headers._AccessControlAllowCredentials;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 1073741824L;
                _headers._AccessControlAllowCredentials = value; 
            }
        }
        public StringValues HeaderAccessControlAllowHeaders
        {
            get
            {
                if (((_bits & 2147483648L) != 0))
                {
                    return _headers._AccessControlAllowHeaders;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 2147483648L;
                _headers._AccessControlAllowHeaders = value; 
            }
        }
        public StringValues HeaderAccessControlAllowMethods
        {
            get
            {
                if (((_bits & 4294967296L) != 0))
                {
                    return _headers._AccessControlAllowMethods;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 4294967296L;
                _headers._AccessControlAllowMethods = value; 
            }
        }
        public StringValues HeaderAccessControlAllowOrigin
        {
            get
            {
                if (((_bits & 8589934592L) != 0))
                {
                    return _headers._AccessControlAllowOrigin;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 8589934592L;
                _headers._AccessControlAllowOrigin = value; 
            }
        }
        public StringValues HeaderAccessControlExposeHeaders
        {
            get
            {
                if (((_bits & 17179869184L) != 0))
                {
                    return _headers._AccessControlExposeHeaders;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 17179869184L;
                _headers._AccessControlExposeHeaders = value; 
            }
        }
        public StringValues HeaderAccessControlMaxAge
        {
            get
            {
                if (((_bits & 34359738368L) != 0))
                {
                    return _headers._AccessControlMaxAge;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 34359738368L;
                _headers._AccessControlMaxAge = value; 
            }
        }
        
        public void SetRawConnection(StringValues value, byte[] raw)
        {
            _bits |= 2L;
            _headers._Connection = value; 
            _headers._rawConnection = raw;
        }
        public void SetRawDate(StringValues value, byte[] raw)
        {
            _bits |= 4L;
            _headers._Date = value; 
            _headers._rawDate = raw;
        }
        public void SetRawTransferEncoding(StringValues value, byte[] raw)
        {
            _bits |= 64L;
            _headers._TransferEncoding = value; 
            _headers._rawTransferEncoding = raw;
        }
        public void SetRawContentLength(StringValues value, byte[] raw)
        {
            _bits |= 2048L;
            _headers._ContentLength = value; 
            _headers._rawContentLength = raw;
        }
        public void SetRawServer(StringValues value, byte[] raw)
        {
            _bits |= 67108864L;
            _headers._Server = value; 
            _headers._rawServer = raw;
        }
        protected override int GetCountFast()
        {
            return BitCount(_bits) + (MaybeUnknown?.Count ?? 0);
        }
        protected override StringValues GetValueFast(string key)
        {
            switch (key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 1L) != 0))
                            {
                                return _headers._CacheControl;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 131072L) != 0))
                            {
                                return _headers._ContentRange;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 524288L) != 0))
                            {
                                return _headers._LastModified;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Accept-Ranges".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 1048576L) != 0))
                            {
                                return _headers._AcceptRanges;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 10:
                    {
                        if ("Connection".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 2L) != 0))
                            {
                                return _headers._Connection;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Keep-Alive".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 8L) != 0))
                            {
                                return _headers._KeepAlive;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Set-Cookie".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 134217728L) != 0))
                            {
                                return _headers._SetCookie;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 4:
                    {
                        if ("Date".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 4L) != 0))
                            {
                                return _headers._Date;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("ETag".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 4194304L) != 0))
                            {
                                return _headers._ETag;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Vary".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 268435456L) != 0))
                            {
                                return _headers._Vary;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 6:
                    {
                        if ("Pragma".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 16L) != 0))
                            {
                                return _headers._Pragma;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Server".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 67108864L) != 0))
                            {
                                return _headers._Server;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 7:
                    {
                        if ("Trailer".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 32L) != 0))
                            {
                                return _headers._Trailer;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Upgrade".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 128L) != 0))
                            {
                                return _headers._Upgrade;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Warning".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 512L) != 0))
                            {
                                return _headers._Warning;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Expires".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 262144L) != 0))
                            {
                                return _headers._Expires;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 17:
                    {
                        if ("Transfer-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 64L) != 0))
                            {
                                return _headers._TransferEncoding;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Proxy-Autheticate".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 16777216L) != 0))
                            {
                                return _headers._ProxyAutheticate;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 3:
                    {
                        if ("Via".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 256L) != 0))
                            {
                                return _headers._Via;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Age".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 2097152L) != 0))
                            {
                                return _headers._Age;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 5:
                    {
                        if ("Allow".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 1024L) != 0))
                            {
                                return _headers._Allow;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 14:
                    {
                        if ("Content-Length".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 2048L) != 0))
                            {
                                return _headers._ContentLength;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 12:
                    {
                        if ("Content-Type".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 4096L) != 0))
                            {
                                return _headers._ContentType;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 16:
                    {
                        if ("Content-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 8192L) != 0))
                            {
                                return _headers._ContentEncoding;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 16384L) != 0))
                            {
                                return _headers._ContentLanguage;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 32768L) != 0))
                            {
                                return _headers._ContentLocation;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("WWW-Authenticate".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 536870912L) != 0))
                            {
                                return _headers._WWWAuthenticate;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 11:
                    {
                        if ("Content-MD5".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 65536L) != 0))
                            {
                                return _headers._ContentMD5;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Retry-After".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 33554432L) != 0))
                            {
                                return _headers._RetryAfter;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 8:
                    {
                        if ("Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 8388608L) != 0))
                            {
                                return _headers._Location;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 32:
                    {
                        if ("Access-Control-Allow-Credentials".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 1073741824L) != 0))
                            {
                                return _headers._AccessControlAllowCredentials;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 28:
                    {
                        if ("Access-Control-Allow-Headers".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 2147483648L) != 0))
                            {
                                return _headers._AccessControlAllowHeaders;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    
                        if ("Access-Control-Allow-Methods".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 4294967296L) != 0))
                            {
                                return _headers._AccessControlAllowMethods;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 27:
                    {
                        if ("Access-Control-Allow-Origin".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 8589934592L) != 0))
                            {
                                return _headers._AccessControlAllowOrigin;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 29:
                    {
                        if ("Access-Control-Expose-Headers".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 17179869184L) != 0))
                            {
                                return _headers._AccessControlExposeHeaders;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;

                case 22:
                    {
                        if ("Access-Control-Max-Age".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 34359738368L) != 0))
                            {
                                return _headers._AccessControlMaxAge;
                            }
                            else
                            {
                                ThrowKeyNotFoundException();
                            }
                        }
                    }
                    break;
}
            if (MaybeUnknown == null) 
            {
                ThrowKeyNotFoundException();
            }
            return MaybeUnknown[key];
        }
        protected override bool TryGetValueFast(string key, out StringValues value)
        {
            switch (key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 1L) != 0))
                            {
                                value = _headers._CacheControl;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 131072L) != 0))
                            {
                                value = _headers._ContentRange;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 524288L) != 0))
                            {
                                value = _headers._LastModified;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Accept-Ranges".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 1048576L) != 0))
                            {
                                value = _headers._AcceptRanges;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 10:
                    {
                        if ("Connection".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 2L) != 0))
                            {
                                value = _headers._Connection;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Keep-Alive".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 8L) != 0))
                            {
                                value = _headers._KeepAlive;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Set-Cookie".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 134217728L) != 0))
                            {
                                value = _headers._SetCookie;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 4:
                    {
                        if ("Date".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 4L) != 0))
                            {
                                value = _headers._Date;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("ETag".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 4194304L) != 0))
                            {
                                value = _headers._ETag;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Vary".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 268435456L) != 0))
                            {
                                value = _headers._Vary;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 6:
                    {
                        if ("Pragma".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 16L) != 0))
                            {
                                value = _headers._Pragma;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Server".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 67108864L) != 0))
                            {
                                value = _headers._Server;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 7:
                    {
                        if ("Trailer".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 32L) != 0))
                            {
                                value = _headers._Trailer;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Upgrade".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 128L) != 0))
                            {
                                value = _headers._Upgrade;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Warning".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 512L) != 0))
                            {
                                value = _headers._Warning;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Expires".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 262144L) != 0))
                            {
                                value = _headers._Expires;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 17:
                    {
                        if ("Transfer-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 64L) != 0))
                            {
                                value = _headers._TransferEncoding;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Proxy-Autheticate".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 16777216L) != 0))
                            {
                                value = _headers._ProxyAutheticate;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 3:
                    {
                        if ("Via".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 256L) != 0))
                            {
                                value = _headers._Via;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Age".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 2097152L) != 0))
                            {
                                value = _headers._Age;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 5:
                    {
                        if ("Allow".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 1024L) != 0))
                            {
                                value = _headers._Allow;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 14:
                    {
                        if ("Content-Length".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 2048L) != 0))
                            {
                                value = _headers._ContentLength;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 12:
                    {
                        if ("Content-Type".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 4096L) != 0))
                            {
                                value = _headers._ContentType;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 16:
                    {
                        if ("Content-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 8192L) != 0))
                            {
                                value = _headers._ContentEncoding;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 16384L) != 0))
                            {
                                value = _headers._ContentLanguage;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 32768L) != 0))
                            {
                                value = _headers._ContentLocation;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("WWW-Authenticate".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 536870912L) != 0))
                            {
                                value = _headers._WWWAuthenticate;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 11:
                    {
                        if ("Content-MD5".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 65536L) != 0))
                            {
                                value = _headers._ContentMD5;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Retry-After".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 33554432L) != 0))
                            {
                                value = _headers._RetryAfter;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 8:
                    {
                        if ("Location".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 8388608L) != 0))
                            {
                                value = _headers._Location;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 32:
                    {
                        if ("Access-Control-Allow-Credentials".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 1073741824L) != 0))
                            {
                                value = _headers._AccessControlAllowCredentials;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 28:
                    {
                        if ("Access-Control-Allow-Headers".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 2147483648L) != 0))
                            {
                                value = _headers._AccessControlAllowHeaders;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    
                        if ("Access-Control-Allow-Methods".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 4294967296L) != 0))
                            {
                                value = _headers._AccessControlAllowMethods;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 27:
                    {
                        if ("Access-Control-Allow-Origin".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 8589934592L) != 0))
                            {
                                value = _headers._AccessControlAllowOrigin;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 29:
                    {
                        if ("Access-Control-Expose-Headers".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 17179869184L) != 0))
                            {
                                value = _headers._AccessControlExposeHeaders;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;

                case 22:
                    {
                        if ("Access-Control-Max-Age".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 34359738368L) != 0))
                            {
                                value = _headers._AccessControlMaxAge;
                                return true;
                            }
                            else
                            {
                                value = StringValues.Empty;
                                return false;
                            }
                        }
                    }
                    break;
}
            value = StringValues.Empty;
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
                            _bits |= 1L;
                            _headers._CacheControl = value;
                            return;
                        }
                    
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 131072L;
                            _headers._ContentRange = value;
                            return;
                        }
                    
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 524288L;
                            _headers._LastModified = value;
                            return;
                        }
                    
                        if ("Accept-Ranges".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 1048576L;
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
                            _bits |= 134217728L;
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
                            _bits |= 4194304L;
                            _headers._ETag = value;
                            return;
                        }
                    
                        if ("Vary".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 268435456L;
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
                            _bits |= 67108864L;
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
                            _bits |= 262144L;
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
                    
                        if ("Proxy-Autheticate".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 16777216L;
                            _headers._ProxyAutheticate = value;
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
                            _bits |= 2097152L;
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

                case 14:
                    {
                        if ("Content-Length".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 2048L;
                            _headers._ContentLength = value;
                            _headers._rawContentLength = null;
                            return;
                        }
                    }
                    break;

                case 12:
                    {
                        if ("Content-Type".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 4096L;
                            _headers._ContentType = value;
                            return;
                        }
                    }
                    break;

                case 16:
                    {
                        if ("Content-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 8192L;
                            _headers._ContentEncoding = value;
                            return;
                        }
                    
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 16384L;
                            _headers._ContentLanguage = value;
                            return;
                        }
                    
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 32768L;
                            _headers._ContentLocation = value;
                            return;
                        }
                    
                        if ("WWW-Authenticate".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 536870912L;
                            _headers._WWWAuthenticate = value;
                            return;
                        }
                    }
                    break;

                case 11:
                    {
                        if ("Content-MD5".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 65536L;
                            _headers._ContentMD5 = value;
                            return;
                        }
                    
                        if ("Retry-After".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 33554432L;
                            _headers._RetryAfter = value;
                            return;
                        }
                    }
                    break;

                case 8:
                    {
                        if ("Location".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 8388608L;
                            _headers._Location = value;
                            return;
                        }
                    }
                    break;

                case 32:
                    {
                        if ("Access-Control-Allow-Credentials".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 1073741824L;
                            _headers._AccessControlAllowCredentials = value;
                            return;
                        }
                    }
                    break;

                case 28:
                    {
                        if ("Access-Control-Allow-Headers".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 2147483648L;
                            _headers._AccessControlAllowHeaders = value;
                            return;
                        }
                    
                        if ("Access-Control-Allow-Methods".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 4294967296L;
                            _headers._AccessControlAllowMethods = value;
                            return;
                        }
                    }
                    break;

                case 27:
                    {
                        if ("Access-Control-Allow-Origin".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 8589934592L;
                            _headers._AccessControlAllowOrigin = value;
                            return;
                        }
                    }
                    break;

                case 29:
                    {
                        if ("Access-Control-Expose-Headers".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 17179869184L;
                            _headers._AccessControlExposeHeaders = value;
                            return;
                        }
                    }
                    break;

                case 22:
                    {
                        if ("Access-Control-Max-Age".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 34359738368L;
                            _headers._AccessControlMaxAge = value;
                            return;
                        }
                    }
                    break;
}
            Unknown[key] = value;
        }
        protected override void AddValueFast(string key, StringValues value)
        {
            switch (key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 1L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 1L;
                            _headers._CacheControl = value;
                            return;
                        }
                    
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 131072L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 131072L;
                            _headers._ContentRange = value;
                            return;
                        }
                    
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 524288L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 524288L;
                            _headers._LastModified = value;
                            return;
                        }
                    
                        if ("Accept-Ranges".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 1048576L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 1048576L;
                            _headers._AcceptRanges = value;
                            return;
                        }
                    }
                    break;
            
                case 10:
                    {
                        if ("Connection".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 2L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 2L;
                            _headers._Connection = value;
                            _headers._rawConnection = null;
                            return;
                        }
                    
                        if ("Keep-Alive".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 8L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 8L;
                            _headers._KeepAlive = value;
                            return;
                        }
                    
                        if ("Set-Cookie".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 134217728L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 134217728L;
                            _headers._SetCookie = value;
                            return;
                        }
                    }
                    break;
            
                case 4:
                    {
                        if ("Date".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 4L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 4L;
                            _headers._Date = value;
                            _headers._rawDate = null;
                            return;
                        }
                    
                        if ("ETag".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 4194304L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 4194304L;
                            _headers._ETag = value;
                            return;
                        }
                    
                        if ("Vary".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 268435456L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 268435456L;
                            _headers._Vary = value;
                            return;
                        }
                    }
                    break;
            
                case 6:
                    {
                        if ("Pragma".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 16L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 16L;
                            _headers._Pragma = value;
                            return;
                        }
                    
                        if ("Server".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 67108864L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 67108864L;
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
                            if (((_bits & 32L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 32L;
                            _headers._Trailer = value;
                            return;
                        }
                    
                        if ("Upgrade".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 128L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 128L;
                            _headers._Upgrade = value;
                            return;
                        }
                    
                        if ("Warning".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 512L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 512L;
                            _headers._Warning = value;
                            return;
                        }
                    
                        if ("Expires".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 262144L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 262144L;
                            _headers._Expires = value;
                            return;
                        }
                    }
                    break;
            
                case 17:
                    {
                        if ("Transfer-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 64L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 64L;
                            _headers._TransferEncoding = value;
                            _headers._rawTransferEncoding = null;
                            return;
                        }
                    
                        if ("Proxy-Autheticate".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 16777216L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 16777216L;
                            _headers._ProxyAutheticate = value;
                            return;
                        }
                    }
                    break;
            
                case 3:
                    {
                        if ("Via".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 256L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 256L;
                            _headers._Via = value;
                            return;
                        }
                    
                        if ("Age".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 2097152L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 2097152L;
                            _headers._Age = value;
                            return;
                        }
                    }
                    break;
            
                case 5:
                    {
                        if ("Allow".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 1024L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 1024L;
                            _headers._Allow = value;
                            return;
                        }
                    }
                    break;
            
                case 14:
                    {
                        if ("Content-Length".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 2048L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 2048L;
                            _headers._ContentLength = value;
                            _headers._rawContentLength = null;
                            return;
                        }
                    }
                    break;
            
                case 12:
                    {
                        if ("Content-Type".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 4096L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 4096L;
                            _headers._ContentType = value;
                            return;
                        }
                    }
                    break;
            
                case 16:
                    {
                        if ("Content-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 8192L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 8192L;
                            _headers._ContentEncoding = value;
                            return;
                        }
                    
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 16384L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 16384L;
                            _headers._ContentLanguage = value;
                            return;
                        }
                    
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 32768L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 32768L;
                            _headers._ContentLocation = value;
                            return;
                        }
                    
                        if ("WWW-Authenticate".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 536870912L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 536870912L;
                            _headers._WWWAuthenticate = value;
                            return;
                        }
                    }
                    break;
            
                case 11:
                    {
                        if ("Content-MD5".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 65536L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 65536L;
                            _headers._ContentMD5 = value;
                            return;
                        }
                    
                        if ("Retry-After".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 33554432L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 33554432L;
                            _headers._RetryAfter = value;
                            return;
                        }
                    }
                    break;
            
                case 8:
                    {
                        if ("Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 8388608L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 8388608L;
                            _headers._Location = value;
                            return;
                        }
                    }
                    break;
            
                case 32:
                    {
                        if ("Access-Control-Allow-Credentials".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 1073741824L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 1073741824L;
                            _headers._AccessControlAllowCredentials = value;
                            return;
                        }
                    }
                    break;
            
                case 28:
                    {
                        if ("Access-Control-Allow-Headers".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 2147483648L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 2147483648L;
                            _headers._AccessControlAllowHeaders = value;
                            return;
                        }
                    
                        if ("Access-Control-Allow-Methods".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 4294967296L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 4294967296L;
                            _headers._AccessControlAllowMethods = value;
                            return;
                        }
                    }
                    break;
            
                case 27:
                    {
                        if ("Access-Control-Allow-Origin".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 8589934592L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 8589934592L;
                            _headers._AccessControlAllowOrigin = value;
                            return;
                        }
                    }
                    break;
            
                case 29:
                    {
                        if ("Access-Control-Expose-Headers".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 17179869184L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 17179869184L;
                            _headers._AccessControlExposeHeaders = value;
                            return;
                        }
                    }
                    break;
            
                case 22:
                    {
                        if ("Access-Control-Max-Age".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 34359738368L) != 0))
                            {
                                ThrowDuplicateKeyException();
                            }
                            _bits |= 34359738368L;
                            _headers._AccessControlMaxAge = value;
                            return;
                        }
                    }
                    break;
            }
            Unknown.Add(key, value);
        }
        protected override bool RemoveFast(string key)
        {
            switch (key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 1L) != 0))
                            {
                                _bits &= ~1L;
                                _headers._CacheControl = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 131072L) != 0))
                            {
                                _bits &= ~131072L;
                                _headers._ContentRange = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 524288L) != 0))
                            {
                                _bits &= ~524288L;
                                _headers._LastModified = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Accept-Ranges".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 1048576L) != 0))
                            {
                                _bits &= ~1048576L;
                                _headers._AcceptRanges = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 10:
                    {
                        if ("Connection".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 2L) != 0))
                            {
                                _bits &= ~2L;
                                _headers._Connection = StringValues.Empty;
                                _headers._rawConnection = null;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Keep-Alive".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 8L) != 0))
                            {
                                _bits &= ~8L;
                                _headers._KeepAlive = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Set-Cookie".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 134217728L) != 0))
                            {
                                _bits &= ~134217728L;
                                _headers._SetCookie = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 4:
                    {
                        if ("Date".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 4L) != 0))
                            {
                                _bits &= ~4L;
                                _headers._Date = StringValues.Empty;
                                _headers._rawDate = null;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("ETag".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 4194304L) != 0))
                            {
                                _bits &= ~4194304L;
                                _headers._ETag = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Vary".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 268435456L) != 0))
                            {
                                _bits &= ~268435456L;
                                _headers._Vary = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 6:
                    {
                        if ("Pragma".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 16L) != 0))
                            {
                                _bits &= ~16L;
                                _headers._Pragma = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Server".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 67108864L) != 0))
                            {
                                _bits &= ~67108864L;
                                _headers._Server = StringValues.Empty;
                                _headers._rawServer = null;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 7:
                    {
                        if ("Trailer".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 32L) != 0))
                            {
                                _bits &= ~32L;
                                _headers._Trailer = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Upgrade".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 128L) != 0))
                            {
                                _bits &= ~128L;
                                _headers._Upgrade = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Warning".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 512L) != 0))
                            {
                                _bits &= ~512L;
                                _headers._Warning = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Expires".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 262144L) != 0))
                            {
                                _bits &= ~262144L;
                                _headers._Expires = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 17:
                    {
                        if ("Transfer-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 64L) != 0))
                            {
                                _bits &= ~64L;
                                _headers._TransferEncoding = StringValues.Empty;
                                _headers._rawTransferEncoding = null;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Proxy-Autheticate".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 16777216L) != 0))
                            {
                                _bits &= ~16777216L;
                                _headers._ProxyAutheticate = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 3:
                    {
                        if ("Via".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 256L) != 0))
                            {
                                _bits &= ~256L;
                                _headers._Via = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Age".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 2097152L) != 0))
                            {
                                _bits &= ~2097152L;
                                _headers._Age = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 5:
                    {
                        if ("Allow".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 1024L) != 0))
                            {
                                _bits &= ~1024L;
                                _headers._Allow = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 14:
                    {
                        if ("Content-Length".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 2048L) != 0))
                            {
                                _bits &= ~2048L;
                                _headers._ContentLength = StringValues.Empty;
                                _headers._rawContentLength = null;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 12:
                    {
                        if ("Content-Type".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 4096L) != 0))
                            {
                                _bits &= ~4096L;
                                _headers._ContentType = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 16:
                    {
                        if ("Content-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 8192L) != 0))
                            {
                                _bits &= ~8192L;
                                _headers._ContentEncoding = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 16384L) != 0))
                            {
                                _bits &= ~16384L;
                                _headers._ContentLanguage = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 32768L) != 0))
                            {
                                _bits &= ~32768L;
                                _headers._ContentLocation = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("WWW-Authenticate".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 536870912L) != 0))
                            {
                                _bits &= ~536870912L;
                                _headers._WWWAuthenticate = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 11:
                    {
                        if ("Content-MD5".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 65536L) != 0))
                            {
                                _bits &= ~65536L;
                                _headers._ContentMD5 = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Retry-After".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 33554432L) != 0))
                            {
                                _bits &= ~33554432L;
                                _headers._RetryAfter = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 8:
                    {
                        if ("Location".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 8388608L) != 0))
                            {
                                _bits &= ~8388608L;
                                _headers._Location = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 32:
                    {
                        if ("Access-Control-Allow-Credentials".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 1073741824L) != 0))
                            {
                                _bits &= ~1073741824L;
                                _headers._AccessControlAllowCredentials = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 28:
                    {
                        if ("Access-Control-Allow-Headers".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 2147483648L) != 0))
                            {
                                _bits &= ~2147483648L;
                                _headers._AccessControlAllowHeaders = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    
                        if ("Access-Control-Allow-Methods".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 4294967296L) != 0))
                            {
                                _bits &= ~4294967296L;
                                _headers._AccessControlAllowMethods = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 27:
                    {
                        if ("Access-Control-Allow-Origin".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 8589934592L) != 0))
                            {
                                _bits &= ~8589934592L;
                                _headers._AccessControlAllowOrigin = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 29:
                    {
                        if ("Access-Control-Expose-Headers".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 17179869184L) != 0))
                            {
                                _bits &= ~17179869184L;
                                _headers._AccessControlExposeHeaders = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            
                case 22:
                    {
                        if ("Access-Control-Max-Age".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 34359738368L) != 0))
                            {
                                _bits &= ~34359738368L;
                                _headers._AccessControlMaxAge = StringValues.Empty;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    break;
            }
            return MaybeUnknown?.Remove(key) ?? false;
        }
        protected override void ClearFast()
        {
            _bits = 0;
            _headers = default(HeaderReferences);
            MaybeUnknown?.Clear();
        }
        
        protected override void CopyToFast(KeyValuePair<string, StringValues>[] array, int arrayIndex)
        {
            if (arrayIndex < 0)
            {
                ThrowArgumentException();
            }
            
                if (((_bits & 1L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Cache-Control", _headers._CacheControl);
                    ++arrayIndex;
                }
            
                if (((_bits & 2L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Connection", _headers._Connection);
                    ++arrayIndex;
                }
            
                if (((_bits & 4L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Date", _headers._Date);
                    ++arrayIndex;
                }
            
                if (((_bits & 8L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Keep-Alive", _headers._KeepAlive);
                    ++arrayIndex;
                }
            
                if (((_bits & 16L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Pragma", _headers._Pragma);
                    ++arrayIndex;
                }
            
                if (((_bits & 32L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Trailer", _headers._Trailer);
                    ++arrayIndex;
                }
            
                if (((_bits & 64L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Transfer-Encoding", _headers._TransferEncoding);
                    ++arrayIndex;
                }
            
                if (((_bits & 128L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Upgrade", _headers._Upgrade);
                    ++arrayIndex;
                }
            
                if (((_bits & 256L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Via", _headers._Via);
                    ++arrayIndex;
                }
            
                if (((_bits & 512L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Warning", _headers._Warning);
                    ++arrayIndex;
                }
            
                if (((_bits & 1024L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Allow", _headers._Allow);
                    ++arrayIndex;
                }
            
                if (((_bits & 2048L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Length", _headers._ContentLength);
                    ++arrayIndex;
                }
            
                if (((_bits & 4096L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Type", _headers._ContentType);
                    ++arrayIndex;
                }
            
                if (((_bits & 8192L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Encoding", _headers._ContentEncoding);
                    ++arrayIndex;
                }
            
                if (((_bits & 16384L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Language", _headers._ContentLanguage);
                    ++arrayIndex;
                }
            
                if (((_bits & 32768L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Location", _headers._ContentLocation);
                    ++arrayIndex;
                }
            
                if (((_bits & 65536L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-MD5", _headers._ContentMD5);
                    ++arrayIndex;
                }
            
                if (((_bits & 131072L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Range", _headers._ContentRange);
                    ++arrayIndex;
                }
            
                if (((_bits & 262144L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Expires", _headers._Expires);
                    ++arrayIndex;
                }
            
                if (((_bits & 524288L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Last-Modified", _headers._LastModified);
                    ++arrayIndex;
                }
            
                if (((_bits & 1048576L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Accept-Ranges", _headers._AcceptRanges);
                    ++arrayIndex;
                }
            
                if (((_bits & 2097152L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Age", _headers._Age);
                    ++arrayIndex;
                }
            
                if (((_bits & 4194304L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("ETag", _headers._ETag);
                    ++arrayIndex;
                }
            
                if (((_bits & 8388608L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Location", _headers._Location);
                    ++arrayIndex;
                }
            
                if (((_bits & 16777216L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Proxy-Autheticate", _headers._ProxyAutheticate);
                    ++arrayIndex;
                }
            
                if (((_bits & 33554432L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Retry-After", _headers._RetryAfter);
                    ++arrayIndex;
                }
            
                if (((_bits & 67108864L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Server", _headers._Server);
                    ++arrayIndex;
                }
            
                if (((_bits & 134217728L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Set-Cookie", _headers._SetCookie);
                    ++arrayIndex;
                }
            
                if (((_bits & 268435456L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Vary", _headers._Vary);
                    ++arrayIndex;
                }
            
                if (((_bits & 536870912L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("WWW-Authenticate", _headers._WWWAuthenticate);
                    ++arrayIndex;
                }
            
                if (((_bits & 1073741824L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Access-Control-Allow-Credentials", _headers._AccessControlAllowCredentials);
                    ++arrayIndex;
                }
            
                if (((_bits & 2147483648L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Access-Control-Allow-Headers", _headers._AccessControlAllowHeaders);
                    ++arrayIndex;
                }
            
                if (((_bits & 4294967296L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Access-Control-Allow-Methods", _headers._AccessControlAllowMethods);
                    ++arrayIndex;
                }
            
                if (((_bits & 8589934592L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Access-Control-Allow-Origin", _headers._AccessControlAllowOrigin);
                    ++arrayIndex;
                }
            
                if (((_bits & 17179869184L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Access-Control-Expose-Headers", _headers._AccessControlExposeHeaders);
                    ++arrayIndex;
                }
            
                if (((_bits & 34359738368L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        ThrowArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Access-Control-Max-Age", _headers._AccessControlMaxAge);
                    ++arrayIndex;
                }
            
            ((ICollection<KeyValuePair<string, StringValues>>)MaybeUnknown)?.CopyTo(array, arrayIndex);
        }
        
        protected void CopyToFast(ref MemoryPoolIterator output)
        {
            
                if (((_bits & 1L) != 0)) 
                { 
                        foreach (var value in _headers._CacheControl)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 0, 17);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 2L) != 0)) 
                { 
                    if (_headers._rawConnection != null) 
                    {
                        output.CopyFrom(_headers._rawConnection, 0, _headers._rawConnection.Length);
                    } 
                    else 
                        foreach (var value in _headers._Connection)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 17, 14);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 4L) != 0)) 
                { 
                    if (_headers._rawDate != null) 
                    {
                        output.CopyFrom(_headers._rawDate, 0, _headers._rawDate.Length);
                    } 
                    else 
                        foreach (var value in _headers._Date)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 31, 8);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 8L) != 0)) 
                { 
                        foreach (var value in _headers._KeepAlive)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 39, 14);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 16L) != 0)) 
                { 
                        foreach (var value in _headers._Pragma)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 53, 10);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 32L) != 0)) 
                { 
                        foreach (var value in _headers._Trailer)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 63, 11);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 64L) != 0)) 
                { 
                    if (_headers._rawTransferEncoding != null) 
                    {
                        output.CopyFrom(_headers._rawTransferEncoding, 0, _headers._rawTransferEncoding.Length);
                    } 
                    else 
                        foreach (var value in _headers._TransferEncoding)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 74, 21);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 128L) != 0)) 
                { 
                        foreach (var value in _headers._Upgrade)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 95, 11);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 256L) != 0)) 
                { 
                        foreach (var value in _headers._Via)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 106, 7);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 512L) != 0)) 
                { 
                        foreach (var value in _headers._Warning)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 113, 11);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 1024L) != 0)) 
                { 
                        foreach (var value in _headers._Allow)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 124, 9);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 2048L) != 0)) 
                { 
                    if (_headers._rawContentLength != null) 
                    {
                        output.CopyFrom(_headers._rawContentLength, 0, _headers._rawContentLength.Length);
                    } 
                    else 
                        foreach (var value in _headers._ContentLength)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 133, 18);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 4096L) != 0)) 
                { 
                        foreach (var value in _headers._ContentType)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 151, 16);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 8192L) != 0)) 
                { 
                        foreach (var value in _headers._ContentEncoding)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 167, 20);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 16384L) != 0)) 
                { 
                        foreach (var value in _headers._ContentLanguage)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 187, 20);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 32768L) != 0)) 
                { 
                        foreach (var value in _headers._ContentLocation)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 207, 20);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 65536L) != 0)) 
                { 
                        foreach (var value in _headers._ContentMD5)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 227, 15);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 131072L) != 0)) 
                { 
                        foreach (var value in _headers._ContentRange)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 242, 17);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 262144L) != 0)) 
                { 
                        foreach (var value in _headers._Expires)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 259, 11);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 524288L) != 0)) 
                { 
                        foreach (var value in _headers._LastModified)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 270, 17);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 1048576L) != 0)) 
                { 
                        foreach (var value in _headers._AcceptRanges)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 287, 17);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 2097152L) != 0)) 
                { 
                        foreach (var value in _headers._Age)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 304, 7);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 4194304L) != 0)) 
                { 
                        foreach (var value in _headers._ETag)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 311, 8);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 8388608L) != 0)) 
                { 
                        foreach (var value in _headers._Location)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 319, 12);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 16777216L) != 0)) 
                { 
                        foreach (var value in _headers._ProxyAutheticate)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 331, 21);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 33554432L) != 0)) 
                { 
                        foreach (var value in _headers._RetryAfter)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 352, 15);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 67108864L) != 0)) 
                { 
                    if (_headers._rawServer != null) 
                    {
                        output.CopyFrom(_headers._rawServer, 0, _headers._rawServer.Length);
                    } 
                    else 
                        foreach (var value in _headers._Server)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 367, 10);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 134217728L) != 0)) 
                { 
                        foreach (var value in _headers._SetCookie)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 377, 14);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 268435456L) != 0)) 
                { 
                        foreach (var value in _headers._Vary)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 391, 8);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 536870912L) != 0)) 
                { 
                        foreach (var value in _headers._WWWAuthenticate)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 399, 20);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 1073741824L) != 0)) 
                { 
                        foreach (var value in _headers._AccessControlAllowCredentials)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 419, 36);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 2147483648L) != 0)) 
                { 
                        foreach (var value in _headers._AccessControlAllowHeaders)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 455, 32);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 4294967296L) != 0)) 
                { 
                        foreach (var value in _headers._AccessControlAllowMethods)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 487, 32);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 8589934592L) != 0)) 
                { 
                        foreach (var value in _headers._AccessControlAllowOrigin)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 519, 31);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 17179869184L) != 0)) 
                { 
                        foreach (var value in _headers._AccessControlExposeHeaders)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 550, 33);
                                output.CopyFromAscii(value);
                            }
                        }
                }
            
                if (((_bits & 34359738368L) != 0)) 
                { 
                        foreach (var value in _headers._AccessControlMaxAge)
                        {
                            if (value != null)
                            {
                                output.CopyFrom(_headerBytes, 583, 26);
                                output.CopyFromAscii(value);
                            }
                        }
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
            public StringValues _ContentLength;
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
            public StringValues _ProxyAutheticate;
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
            public byte[] _rawContentLength;
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
                    
                        case 35:
                            goto state35;
                    
                    default:
                        goto state_default;
                }
                
                state0:
                    if (((_bits & 1L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Cache-Control", _collection._headers._CacheControl);
                        _state = 1;
                        return true;
                    }
                
                state1:
                    if (((_bits & 2L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Connection", _collection._headers._Connection);
                        _state = 2;
                        return true;
                    }
                
                state2:
                    if (((_bits & 4L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Date", _collection._headers._Date);
                        _state = 3;
                        return true;
                    }
                
                state3:
                    if (((_bits & 8L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Keep-Alive", _collection._headers._KeepAlive);
                        _state = 4;
                        return true;
                    }
                
                state4:
                    if (((_bits & 16L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Pragma", _collection._headers._Pragma);
                        _state = 5;
                        return true;
                    }
                
                state5:
                    if (((_bits & 32L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Trailer", _collection._headers._Trailer);
                        _state = 6;
                        return true;
                    }
                
                state6:
                    if (((_bits & 64L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Transfer-Encoding", _collection._headers._TransferEncoding);
                        _state = 7;
                        return true;
                    }
                
                state7:
                    if (((_bits & 128L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Upgrade", _collection._headers._Upgrade);
                        _state = 8;
                        return true;
                    }
                
                state8:
                    if (((_bits & 256L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Via", _collection._headers._Via);
                        _state = 9;
                        return true;
                    }
                
                state9:
                    if (((_bits & 512L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Warning", _collection._headers._Warning);
                        _state = 10;
                        return true;
                    }
                
                state10:
                    if (((_bits & 1024L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Allow", _collection._headers._Allow);
                        _state = 11;
                        return true;
                    }
                
                state11:
                    if (((_bits & 2048L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Length", _collection._headers._ContentLength);
                        _state = 12;
                        return true;
                    }
                
                state12:
                    if (((_bits & 4096L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Type", _collection._headers._ContentType);
                        _state = 13;
                        return true;
                    }
                
                state13:
                    if (((_bits & 8192L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Encoding", _collection._headers._ContentEncoding);
                        _state = 14;
                        return true;
                    }
                
                state14:
                    if (((_bits & 16384L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Language", _collection._headers._ContentLanguage);
                        _state = 15;
                        return true;
                    }
                
                state15:
                    if (((_bits & 32768L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Location", _collection._headers._ContentLocation);
                        _state = 16;
                        return true;
                    }
                
                state16:
                    if (((_bits & 65536L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-MD5", _collection._headers._ContentMD5);
                        _state = 17;
                        return true;
                    }
                
                state17:
                    if (((_bits & 131072L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Range", _collection._headers._ContentRange);
                        _state = 18;
                        return true;
                    }
                
                state18:
                    if (((_bits & 262144L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Expires", _collection._headers._Expires);
                        _state = 19;
                        return true;
                    }
                
                state19:
                    if (((_bits & 524288L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Last-Modified", _collection._headers._LastModified);
                        _state = 20;
                        return true;
                    }
                
                state20:
                    if (((_bits & 1048576L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Accept-Ranges", _collection._headers._AcceptRanges);
                        _state = 21;
                        return true;
                    }
                
                state21:
                    if (((_bits & 2097152L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Age", _collection._headers._Age);
                        _state = 22;
                        return true;
                    }
                
                state22:
                    if (((_bits & 4194304L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("ETag", _collection._headers._ETag);
                        _state = 23;
                        return true;
                    }
                
                state23:
                    if (((_bits & 8388608L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Location", _collection._headers._Location);
                        _state = 24;
                        return true;
                    }
                
                state24:
                    if (((_bits & 16777216L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Proxy-Autheticate", _collection._headers._ProxyAutheticate);
                        _state = 25;
                        return true;
                    }
                
                state25:
                    if (((_bits & 33554432L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Retry-After", _collection._headers._RetryAfter);
                        _state = 26;
                        return true;
                    }
                
                state26:
                    if (((_bits & 67108864L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Server", _collection._headers._Server);
                        _state = 27;
                        return true;
                    }
                
                state27:
                    if (((_bits & 134217728L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Set-Cookie", _collection._headers._SetCookie);
                        _state = 28;
                        return true;
                    }
                
                state28:
                    if (((_bits & 268435456L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Vary", _collection._headers._Vary);
                        _state = 29;
                        return true;
                    }
                
                state29:
                    if (((_bits & 536870912L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("WWW-Authenticate", _collection._headers._WWWAuthenticate);
                        _state = 30;
                        return true;
                    }
                
                state30:
                    if (((_bits & 1073741824L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Allow-Credentials", _collection._headers._AccessControlAllowCredentials);
                        _state = 31;
                        return true;
                    }
                
                state31:
                    if (((_bits & 2147483648L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Allow-Headers", _collection._headers._AccessControlAllowHeaders);
                        _state = 32;
                        return true;
                    }
                
                state32:
                    if (((_bits & 4294967296L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Allow-Methods", _collection._headers._AccessControlAllowMethods);
                        _state = 33;
                        return true;
                    }
                
                state33:
                    if (((_bits & 8589934592L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Allow-Origin", _collection._headers._AccessControlAllowOrigin);
                        _state = 34;
                        return true;
                    }
                
                state34:
                    if (((_bits & 17179869184L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Expose-Headers", _collection._headers._AccessControlExposeHeaders);
                        _state = 35;
                        return true;
                    }
                
                state35:
                    if (((_bits & 34359738368L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Max-Age", _collection._headers._AccessControlMaxAge);
                        _state = 36;
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