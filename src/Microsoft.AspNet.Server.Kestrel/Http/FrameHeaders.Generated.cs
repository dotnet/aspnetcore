
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNet.Server.Kestrel.Http 
{

    public partial class FrameRequestHeaders
    {
        
        private long _bits = 0;
        
        private StringValues _CacheControl;
        private StringValues _Connection;
        private StringValues _Date;
        private StringValues _KeepAlive;
        private StringValues _Pragma;
        private StringValues _Trailer;
        private StringValues _TransferEncoding;
        private StringValues _Upgrade;
        private StringValues _Via;
        private StringValues _Warning;
        private StringValues _Allow;
        private StringValues _ContentLength;
        private StringValues _ContentType;
        private StringValues _ContentEncoding;
        private StringValues _ContentLanguage;
        private StringValues _ContentLocation;
        private StringValues _ContentMD5;
        private StringValues _ContentRange;
        private StringValues _Expires;
        private StringValues _LastModified;
        private StringValues _Accept;
        private StringValues _AcceptCharset;
        private StringValues _AcceptEncoding;
        private StringValues _AcceptLanguage;
        private StringValues _Authorization;
        private StringValues _Cookie;
        private StringValues _Expect;
        private StringValues _From;
        private StringValues _Host;
        private StringValues _IfMatch;
        private StringValues _IfModifiedSince;
        private StringValues _IfNoneMatch;
        private StringValues _IfRange;
        private StringValues _IfUnmodifiedSince;
        private StringValues _MaxForwards;
        private StringValues _ProxyAuthorization;
        private StringValues _Referer;
        private StringValues _Range;
        private StringValues _TE;
        private StringValues _Translate;
        private StringValues _UserAgent;
        private StringValues _Origin;
        private StringValues _AccessControlRequestMethod;
        private StringValues _AccessControlRequestHeaders;
        
        
        public StringValues HeaderCacheControl
        {
            get
            {
                if (((_bits & 1L) != 0))
                {
                    return _CacheControl;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 1L;
                _CacheControl = value; 
            }
        }
        public StringValues HeaderConnection
        {
            get
            {
                if (((_bits & 2L) != 0))
                {
                    return _Connection;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 2L;
                _Connection = value; 
            }
        }
        public StringValues HeaderDate
        {
            get
            {
                if (((_bits & 4L) != 0))
                {
                    return _Date;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 4L;
                _Date = value; 
            }
        }
        public StringValues HeaderKeepAlive
        {
            get
            {
                if (((_bits & 8L) != 0))
                {
                    return _KeepAlive;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 8L;
                _KeepAlive = value; 
            }
        }
        public StringValues HeaderPragma
        {
            get
            {
                if (((_bits & 16L) != 0))
                {
                    return _Pragma;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 16L;
                _Pragma = value; 
            }
        }
        public StringValues HeaderTrailer
        {
            get
            {
                if (((_bits & 32L) != 0))
                {
                    return _Trailer;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 32L;
                _Trailer = value; 
            }
        }
        public StringValues HeaderTransferEncoding
        {
            get
            {
                if (((_bits & 64L) != 0))
                {
                    return _TransferEncoding;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 64L;
                _TransferEncoding = value; 
            }
        }
        public StringValues HeaderUpgrade
        {
            get
            {
                if (((_bits & 128L) != 0))
                {
                    return _Upgrade;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 128L;
                _Upgrade = value; 
            }
        }
        public StringValues HeaderVia
        {
            get
            {
                if (((_bits & 256L) != 0))
                {
                    return _Via;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 256L;
                _Via = value; 
            }
        }
        public StringValues HeaderWarning
        {
            get
            {
                if (((_bits & 512L) != 0))
                {
                    return _Warning;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 512L;
                _Warning = value; 
            }
        }
        public StringValues HeaderAllow
        {
            get
            {
                if (((_bits & 1024L) != 0))
                {
                    return _Allow;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 1024L;
                _Allow = value; 
            }
        }
        public StringValues HeaderContentLength
        {
            get
            {
                if (((_bits & 2048L) != 0))
                {
                    return _ContentLength;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 2048L;
                _ContentLength = value; 
            }
        }
        public StringValues HeaderContentType
        {
            get
            {
                if (((_bits & 4096L) != 0))
                {
                    return _ContentType;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 4096L;
                _ContentType = value; 
            }
        }
        public StringValues HeaderContentEncoding
        {
            get
            {
                if (((_bits & 8192L) != 0))
                {
                    return _ContentEncoding;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 8192L;
                _ContentEncoding = value; 
            }
        }
        public StringValues HeaderContentLanguage
        {
            get
            {
                if (((_bits & 16384L) != 0))
                {
                    return _ContentLanguage;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 16384L;
                _ContentLanguage = value; 
            }
        }
        public StringValues HeaderContentLocation
        {
            get
            {
                if (((_bits & 32768L) != 0))
                {
                    return _ContentLocation;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 32768L;
                _ContentLocation = value; 
            }
        }
        public StringValues HeaderContentMD5
        {
            get
            {
                if (((_bits & 65536L) != 0))
                {
                    return _ContentMD5;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 65536L;
                _ContentMD5 = value; 
            }
        }
        public StringValues HeaderContentRange
        {
            get
            {
                if (((_bits & 131072L) != 0))
                {
                    return _ContentRange;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 131072L;
                _ContentRange = value; 
            }
        }
        public StringValues HeaderExpires
        {
            get
            {
                if (((_bits & 262144L) != 0))
                {
                    return _Expires;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 262144L;
                _Expires = value; 
            }
        }
        public StringValues HeaderLastModified
        {
            get
            {
                if (((_bits & 524288L) != 0))
                {
                    return _LastModified;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 524288L;
                _LastModified = value; 
            }
        }
        public StringValues HeaderAccept
        {
            get
            {
                if (((_bits & 1048576L) != 0))
                {
                    return _Accept;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 1048576L;
                _Accept = value; 
            }
        }
        public StringValues HeaderAcceptCharset
        {
            get
            {
                if (((_bits & 2097152L) != 0))
                {
                    return _AcceptCharset;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 2097152L;
                _AcceptCharset = value; 
            }
        }
        public StringValues HeaderAcceptEncoding
        {
            get
            {
                if (((_bits & 4194304L) != 0))
                {
                    return _AcceptEncoding;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 4194304L;
                _AcceptEncoding = value; 
            }
        }
        public StringValues HeaderAcceptLanguage
        {
            get
            {
                if (((_bits & 8388608L) != 0))
                {
                    return _AcceptLanguage;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 8388608L;
                _AcceptLanguage = value; 
            }
        }
        public StringValues HeaderAuthorization
        {
            get
            {
                if (((_bits & 16777216L) != 0))
                {
                    return _Authorization;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 16777216L;
                _Authorization = value; 
            }
        }
        public StringValues HeaderCookie
        {
            get
            {
                if (((_bits & 33554432L) != 0))
                {
                    return _Cookie;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 33554432L;
                _Cookie = value; 
            }
        }
        public StringValues HeaderExpect
        {
            get
            {
                if (((_bits & 67108864L) != 0))
                {
                    return _Expect;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 67108864L;
                _Expect = value; 
            }
        }
        public StringValues HeaderFrom
        {
            get
            {
                if (((_bits & 134217728L) != 0))
                {
                    return _From;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 134217728L;
                _From = value; 
            }
        }
        public StringValues HeaderHost
        {
            get
            {
                if (((_bits & 268435456L) != 0))
                {
                    return _Host;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 268435456L;
                _Host = value; 
            }
        }
        public StringValues HeaderIfMatch
        {
            get
            {
                if (((_bits & 536870912L) != 0))
                {
                    return _IfMatch;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 536870912L;
                _IfMatch = value; 
            }
        }
        public StringValues HeaderIfModifiedSince
        {
            get
            {
                if (((_bits & 1073741824L) != 0))
                {
                    return _IfModifiedSince;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 1073741824L;
                _IfModifiedSince = value; 
            }
        }
        public StringValues HeaderIfNoneMatch
        {
            get
            {
                if (((_bits & 2147483648L) != 0))
                {
                    return _IfNoneMatch;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 2147483648L;
                _IfNoneMatch = value; 
            }
        }
        public StringValues HeaderIfRange
        {
            get
            {
                if (((_bits & 4294967296L) != 0))
                {
                    return _IfRange;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 4294967296L;
                _IfRange = value; 
            }
        }
        public StringValues HeaderIfUnmodifiedSince
        {
            get
            {
                if (((_bits & 8589934592L) != 0))
                {
                    return _IfUnmodifiedSince;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 8589934592L;
                _IfUnmodifiedSince = value; 
            }
        }
        public StringValues HeaderMaxForwards
        {
            get
            {
                if (((_bits & 17179869184L) != 0))
                {
                    return _MaxForwards;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 17179869184L;
                _MaxForwards = value; 
            }
        }
        public StringValues HeaderProxyAuthorization
        {
            get
            {
                if (((_bits & 34359738368L) != 0))
                {
                    return _ProxyAuthorization;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 34359738368L;
                _ProxyAuthorization = value; 
            }
        }
        public StringValues HeaderReferer
        {
            get
            {
                if (((_bits & 68719476736L) != 0))
                {
                    return _Referer;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 68719476736L;
                _Referer = value; 
            }
        }
        public StringValues HeaderRange
        {
            get
            {
                if (((_bits & 137438953472L) != 0))
                {
                    return _Range;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 137438953472L;
                _Range = value; 
            }
        }
        public StringValues HeaderTE
        {
            get
            {
                if (((_bits & 274877906944L) != 0))
                {
                    return _TE;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 274877906944L;
                _TE = value; 
            }
        }
        public StringValues HeaderTranslate
        {
            get
            {
                if (((_bits & 549755813888L) != 0))
                {
                    return _Translate;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 549755813888L;
                _Translate = value; 
            }
        }
        public StringValues HeaderUserAgent
        {
            get
            {
                if (((_bits & 1099511627776L) != 0))
                {
                    return _UserAgent;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 1099511627776L;
                _UserAgent = value; 
            }
        }
        public StringValues HeaderOrigin
        {
            get
            {
                if (((_bits & 2199023255552L) != 0))
                {
                    return _Origin;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 2199023255552L;
                _Origin = value; 
            }
        }
        public StringValues HeaderAccessControlRequestMethod
        {
            get
            {
                if (((_bits & 4398046511104L) != 0))
                {
                    return _AccessControlRequestMethod;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 4398046511104L;
                _AccessControlRequestMethod = value; 
            }
        }
        public StringValues HeaderAccessControlRequestHeaders
        {
            get
            {
                if (((_bits & 8796093022208L) != 0))
                {
                    return _AccessControlRequestHeaders;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 8796093022208L;
                _AccessControlRequestHeaders = value; 
            }
        }
        
        protected override int GetCountFast()
        {
            return BitCount(_bits) + (MaybeUnknown?.Count ?? 0);
        }
        protected override StringValues GetValueFast(string key)
        {
            switch(key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 1L) != 0))
                            {
                                return _CacheControl;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 131072L) != 0))
                            {
                                return _ContentRange;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 524288L) != 0))
                            {
                                return _LastModified;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Authorization".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 16777216L) != 0))
                            {
                                return _Authorization;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("If-None-Match".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 2147483648L) != 0))
                            {
                                return _IfNoneMatch;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _Connection;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Keep-Alive".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 8L) != 0))
                            {
                                return _KeepAlive;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("User-Agent".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 1099511627776L) != 0))
                            {
                                return _UserAgent;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _Date;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("From".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 134217728L) != 0))
                            {
                                return _From;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Host".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 268435456L) != 0))
                            {
                                return _Host;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _Pragma;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Accept".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 1048576L) != 0))
                            {
                                return _Accept;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Cookie".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 33554432L) != 0))
                            {
                                return _Cookie;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Expect".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 67108864L) != 0))
                            {
                                return _Expect;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Origin".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 2199023255552L) != 0))
                            {
                                return _Origin;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _Trailer;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Upgrade".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 128L) != 0))
                            {
                                return _Upgrade;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Warning".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 512L) != 0))
                            {
                                return _Warning;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Expires".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 262144L) != 0))
                            {
                                return _Expires;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Referer".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 68719476736L) != 0))
                            {
                                return _Referer;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _TransferEncoding;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("If-Modified-Since".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 1073741824L) != 0))
                            {
                                return _IfModifiedSince;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _Via;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _Allow;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 137438953472L) != 0))
                            {
                                return _Range;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _ContentLength;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Accept-Charset".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 2097152L) != 0))
                            {
                                return _AcceptCharset;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _ContentType;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Max-Forwards".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 17179869184L) != 0))
                            {
                                return _MaxForwards;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _ContentEncoding;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 16384L) != 0))
                            {
                                return _ContentLanguage;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 32768L) != 0))
                            {
                                return _ContentLocation;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _ContentMD5;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _AcceptEncoding;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Accept-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 8388608L) != 0))
                            {
                                return _AcceptLanguage;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _IfMatch;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("If-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 4294967296L) != 0))
                            {
                                return _IfRange;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _IfUnmodifiedSince;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Proxy-Authorization".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 34359738368L) != 0))
                            {
                                return _ProxyAuthorization;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _TE;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _Translate;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _AccessControlRequestMethod;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _AccessControlRequestHeaders;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    }
                    break;
}
            if (MaybeUnknown == null) 
            {
                throw new System.Collections.Generic.KeyNotFoundException();
            }
            return MaybeUnknown[key];
        }
        protected override bool TryGetValueFast(string key, out StringValues value)
        {
            switch(key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 1L) != 0))
                            {
                                value = _CacheControl;
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
                                value = _ContentRange;
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
                                value = _LastModified;
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
                                value = _Authorization;
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
                                value = _IfNoneMatch;
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
                                value = _Connection;
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
                                value = _KeepAlive;
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
                                value = _UserAgent;
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
                                value = _Date;
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
                                value = _From;
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
                                value = _Host;
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
                                value = _Pragma;
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
                                value = _Accept;
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
                                value = _Cookie;
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
                                value = _Expect;
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
                                value = _Origin;
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
                                value = _Trailer;
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
                                value = _Upgrade;
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
                                value = _Warning;
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
                                value = _Expires;
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
                                value = _Referer;
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
                                value = _TransferEncoding;
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
                                value = _IfModifiedSince;
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
                                value = _Via;
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
                                value = _Allow;
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
                                value = _Range;
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
                                value = _ContentLength;
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
                                value = _AcceptCharset;
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
                                value = _ContentType;
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
                                value = _MaxForwards;
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
                                value = _ContentEncoding;
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
                                value = _ContentLanguage;
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
                                value = _ContentLocation;
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
                                value = _ContentMD5;
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
                                value = _AcceptEncoding;
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
                                value = _AcceptLanguage;
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
                                value = _IfMatch;
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
                                value = _IfRange;
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
                                value = _IfUnmodifiedSince;
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
                                value = _ProxyAuthorization;
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
                                value = _TE;
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
                                value = _Translate;
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
                                value = _AccessControlRequestMethod;
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
                                value = _AccessControlRequestHeaders;
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
            switch(key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 1L;
                            _CacheControl = value;
                            return;
                        }
                    
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 131072L;
                            _ContentRange = value;
                            return;
                        }
                    
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 524288L;
                            _LastModified = value;
                            return;
                        }
                    
                        if ("Authorization".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 16777216L;
                            _Authorization = value;
                            return;
                        }
                    
                        if ("If-None-Match".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 2147483648L;
                            _IfNoneMatch = value;
                            return;
                        }
                    }
                    break;

                case 10:
                    {
                        if ("Connection".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 2L;
                            _Connection = value;
                            return;
                        }
                    
                        if ("Keep-Alive".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 8L;
                            _KeepAlive = value;
                            return;
                        }
                    
                        if ("User-Agent".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 1099511627776L;
                            _UserAgent = value;
                            return;
                        }
                    }
                    break;

                case 4:
                    {
                        if ("Date".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 4L;
                            _Date = value;
                            return;
                        }
                    
                        if ("From".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 134217728L;
                            _From = value;
                            return;
                        }
                    
                        if ("Host".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 268435456L;
                            _Host = value;
                            return;
                        }
                    }
                    break;

                case 6:
                    {
                        if ("Pragma".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 16L;
                            _Pragma = value;
                            return;
                        }
                    
                        if ("Accept".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 1048576L;
                            _Accept = value;
                            return;
                        }
                    
                        if ("Cookie".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 33554432L;
                            _Cookie = value;
                            return;
                        }
                    
                        if ("Expect".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 67108864L;
                            _Expect = value;
                            return;
                        }
                    
                        if ("Origin".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 2199023255552L;
                            _Origin = value;
                            return;
                        }
                    }
                    break;

                case 7:
                    {
                        if ("Trailer".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 32L;
                            _Trailer = value;
                            return;
                        }
                    
                        if ("Upgrade".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 128L;
                            _Upgrade = value;
                            return;
                        }
                    
                        if ("Warning".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 512L;
                            _Warning = value;
                            return;
                        }
                    
                        if ("Expires".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 262144L;
                            _Expires = value;
                            return;
                        }
                    
                        if ("Referer".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 68719476736L;
                            _Referer = value;
                            return;
                        }
                    }
                    break;

                case 17:
                    {
                        if ("Transfer-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 64L;
                            _TransferEncoding = value;
                            return;
                        }
                    
                        if ("If-Modified-Since".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 1073741824L;
                            _IfModifiedSince = value;
                            return;
                        }
                    }
                    break;

                case 3:
                    {
                        if ("Via".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 256L;
                            _Via = value;
                            return;
                        }
                    }
                    break;

                case 5:
                    {
                        if ("Allow".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 1024L;
                            _Allow = value;
                            return;
                        }
                    
                        if ("Range".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 137438953472L;
                            _Range = value;
                            return;
                        }
                    }
                    break;

                case 14:
                    {
                        if ("Content-Length".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 2048L;
                            _ContentLength = value;
                            return;
                        }
                    
                        if ("Accept-Charset".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 2097152L;
                            _AcceptCharset = value;
                            return;
                        }
                    }
                    break;

                case 12:
                    {
                        if ("Content-Type".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 4096L;
                            _ContentType = value;
                            return;
                        }
                    
                        if ("Max-Forwards".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 17179869184L;
                            _MaxForwards = value;
                            return;
                        }
                    }
                    break;

                case 16:
                    {
                        if ("Content-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 8192L;
                            _ContentEncoding = value;
                            return;
                        }
                    
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 16384L;
                            _ContentLanguage = value;
                            return;
                        }
                    
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 32768L;
                            _ContentLocation = value;
                            return;
                        }
                    }
                    break;

                case 11:
                    {
                        if ("Content-MD5".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 65536L;
                            _ContentMD5 = value;
                            return;
                        }
                    }
                    break;

                case 15:
                    {
                        if ("Accept-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 4194304L;
                            _AcceptEncoding = value;
                            return;
                        }
                    
                        if ("Accept-Language".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 8388608L;
                            _AcceptLanguage = value;
                            return;
                        }
                    }
                    break;

                case 8:
                    {
                        if ("If-Match".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 536870912L;
                            _IfMatch = value;
                            return;
                        }
                    
                        if ("If-Range".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 4294967296L;
                            _IfRange = value;
                            return;
                        }
                    }
                    break;

                case 19:
                    {
                        if ("If-Unmodified-Since".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 8589934592L;
                            _IfUnmodifiedSince = value;
                            return;
                        }
                    
                        if ("Proxy-Authorization".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 34359738368L;
                            _ProxyAuthorization = value;
                            return;
                        }
                    }
                    break;

                case 2:
                    {
                        if ("TE".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 274877906944L;
                            _TE = value;
                            return;
                        }
                    }
                    break;

                case 9:
                    {
                        if ("Translate".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 549755813888L;
                            _Translate = value;
                            return;
                        }
                    }
                    break;

                case 29:
                    {
                        if ("Access-Control-Request-Method".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 4398046511104L;
                            _AccessControlRequestMethod = value;
                            return;
                        }
                    }
                    break;

                case 30:
                    {
                        if ("Access-Control-Request-Headers".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 8796093022208L;
                            _AccessControlRequestHeaders = value;
                            return;
                        }
                    }
                    break;
}
            Unknown[key] = value;
        }
        protected override void AddValueFast(string key, StringValues value)
        {
            switch(key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 1L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 1L;
                            _CacheControl = value;
                            return;
                        }
                    
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 131072L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 131072L;
                            _ContentRange = value;
                            return;
                        }
                    
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 524288L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 524288L;
                            _LastModified = value;
                            return;
                        }
                    
                        if ("Authorization".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 16777216L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 16777216L;
                            _Authorization = value;
                            return;
                        }
                    
                        if ("If-None-Match".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 2147483648L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 2147483648L;
                            _IfNoneMatch = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 2L;
                            _Connection = value;
                            return;
                        }
                    
                        if ("Keep-Alive".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 8L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 8L;
                            _KeepAlive = value;
                            return;
                        }
                    
                        if ("User-Agent".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 1099511627776L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 1099511627776L;
                            _UserAgent = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 4L;
                            _Date = value;
                            return;
                        }
                    
                        if ("From".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 134217728L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 134217728L;
                            _From = value;
                            return;
                        }
                    
                        if ("Host".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 268435456L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 268435456L;
                            _Host = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 16L;
                            _Pragma = value;
                            return;
                        }
                    
                        if ("Accept".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 1048576L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 1048576L;
                            _Accept = value;
                            return;
                        }
                    
                        if ("Cookie".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 33554432L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 33554432L;
                            _Cookie = value;
                            return;
                        }
                    
                        if ("Expect".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 67108864L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 67108864L;
                            _Expect = value;
                            return;
                        }
                    
                        if ("Origin".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 2199023255552L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 2199023255552L;
                            _Origin = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 32L;
                            _Trailer = value;
                            return;
                        }
                    
                        if ("Upgrade".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 128L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 128L;
                            _Upgrade = value;
                            return;
                        }
                    
                        if ("Warning".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 512L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 512L;
                            _Warning = value;
                            return;
                        }
                    
                        if ("Expires".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 262144L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 262144L;
                            _Expires = value;
                            return;
                        }
                    
                        if ("Referer".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 68719476736L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 68719476736L;
                            _Referer = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 64L;
                            _TransferEncoding = value;
                            return;
                        }
                    
                        if ("If-Modified-Since".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 1073741824L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 1073741824L;
                            _IfModifiedSince = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 256L;
                            _Via = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 1024L;
                            _Allow = value;
                            return;
                        }
                    
                        if ("Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 137438953472L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 137438953472L;
                            _Range = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 2048L;
                            _ContentLength = value;
                            return;
                        }
                    
                        if ("Accept-Charset".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 2097152L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 2097152L;
                            _AcceptCharset = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 4096L;
                            _ContentType = value;
                            return;
                        }
                    
                        if ("Max-Forwards".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 17179869184L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 17179869184L;
                            _MaxForwards = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 8192L;
                            _ContentEncoding = value;
                            return;
                        }
                    
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 16384L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 16384L;
                            _ContentLanguage = value;
                            return;
                        }
                    
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 32768L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 32768L;
                            _ContentLocation = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 65536L;
                            _ContentMD5 = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 4194304L;
                            _AcceptEncoding = value;
                            return;
                        }
                    
                        if ("Accept-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 8388608L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 8388608L;
                            _AcceptLanguage = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 536870912L;
                            _IfMatch = value;
                            return;
                        }
                    
                        if ("If-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 4294967296L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 4294967296L;
                            _IfRange = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 8589934592L;
                            _IfUnmodifiedSince = value;
                            return;
                        }
                    
                        if ("Proxy-Authorization".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 34359738368L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 34359738368L;
                            _ProxyAuthorization = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 274877906944L;
                            _TE = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 549755813888L;
                            _Translate = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 4398046511104L;
                            _AccessControlRequestMethod = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 8796093022208L;
                            _AccessControlRequestHeaders = value;
                            return;
                        }
                    }
                    break;
            }
            Unknown.Add(key, value);
        }
        protected override bool RemoveFast(string key)
        {
            switch(key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 1L) != 0))
                            {
                                _bits &= ~1L;
                                _CacheControl = StringValues.Empty;
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
                                _ContentRange = StringValues.Empty;
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
                                _LastModified = StringValues.Empty;
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
                                _Authorization = StringValues.Empty;
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
                                _IfNoneMatch = StringValues.Empty;
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
                                _Connection = StringValues.Empty;
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
                                _KeepAlive = StringValues.Empty;
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
                                _UserAgent = StringValues.Empty;
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
                                _Date = StringValues.Empty;
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
                                _From = StringValues.Empty;
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
                                _Host = StringValues.Empty;
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
                                _Pragma = StringValues.Empty;
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
                                _Accept = StringValues.Empty;
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
                                _Cookie = StringValues.Empty;
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
                                _Expect = StringValues.Empty;
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
                                _Origin = StringValues.Empty;
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
                                _Trailer = StringValues.Empty;
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
                                _Upgrade = StringValues.Empty;
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
                                _Warning = StringValues.Empty;
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
                                _Expires = StringValues.Empty;
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
                                _Referer = StringValues.Empty;
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
                                _TransferEncoding = StringValues.Empty;
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
                                _IfModifiedSince = StringValues.Empty;
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
                                _Via = StringValues.Empty;
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
                                _Allow = StringValues.Empty;
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
                                _Range = StringValues.Empty;
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
                                _ContentLength = StringValues.Empty;
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
                                _AcceptCharset = StringValues.Empty;
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
                                _ContentType = StringValues.Empty;
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
                                _MaxForwards = StringValues.Empty;
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
                                _ContentEncoding = StringValues.Empty;
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
                                _ContentLanguage = StringValues.Empty;
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
                                _ContentLocation = StringValues.Empty;
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
                                _ContentMD5 = StringValues.Empty;
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
                                _AcceptEncoding = StringValues.Empty;
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
                                _AcceptLanguage = StringValues.Empty;
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
                                _IfMatch = StringValues.Empty;
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
                                _IfRange = StringValues.Empty;
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
                                _IfUnmodifiedSince = StringValues.Empty;
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
                                _ProxyAuthorization = StringValues.Empty;
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
                                _TE = StringValues.Empty;
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
                                _Translate = StringValues.Empty;
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
                                _AccessControlRequestMethod = StringValues.Empty;
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
                                _AccessControlRequestHeaders = StringValues.Empty;
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
            MaybeUnknown?.Clear();
        }
        
        protected override void CopyToFast(KeyValuePair<string, StringValues>[] array, int arrayIndex)
        {
            if (arrayIndex < 0)
            {
                throw new ArgumentException();
            }
            
                if (((_bits & 1L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Cache-Control", _CacheControl);
                    ++arrayIndex;
                }
            
                if (((_bits & 2L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Connection", _Connection);
                    ++arrayIndex;
                }
            
                if (((_bits & 4L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Date", _Date);
                    ++arrayIndex;
                }
            
                if (((_bits & 8L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Keep-Alive", _KeepAlive);
                    ++arrayIndex;
                }
            
                if (((_bits & 16L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Pragma", _Pragma);
                    ++arrayIndex;
                }
            
                if (((_bits & 32L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Trailer", _Trailer);
                    ++arrayIndex;
                }
            
                if (((_bits & 64L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Transfer-Encoding", _TransferEncoding);
                    ++arrayIndex;
                }
            
                if (((_bits & 128L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Upgrade", _Upgrade);
                    ++arrayIndex;
                }
            
                if (((_bits & 256L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Via", _Via);
                    ++arrayIndex;
                }
            
                if (((_bits & 512L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Warning", _Warning);
                    ++arrayIndex;
                }
            
                if (((_bits & 1024L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Allow", _Allow);
                    ++arrayIndex;
                }
            
                if (((_bits & 2048L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Length", _ContentLength);
                    ++arrayIndex;
                }
            
                if (((_bits & 4096L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Type", _ContentType);
                    ++arrayIndex;
                }
            
                if (((_bits & 8192L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Encoding", _ContentEncoding);
                    ++arrayIndex;
                }
            
                if (((_bits & 16384L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Language", _ContentLanguage);
                    ++arrayIndex;
                }
            
                if (((_bits & 32768L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Location", _ContentLocation);
                    ++arrayIndex;
                }
            
                if (((_bits & 65536L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-MD5", _ContentMD5);
                    ++arrayIndex;
                }
            
                if (((_bits & 131072L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Range", _ContentRange);
                    ++arrayIndex;
                }
            
                if (((_bits & 262144L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Expires", _Expires);
                    ++arrayIndex;
                }
            
                if (((_bits & 524288L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Last-Modified", _LastModified);
                    ++arrayIndex;
                }
            
                if (((_bits & 1048576L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Accept", _Accept);
                    ++arrayIndex;
                }
            
                if (((_bits & 2097152L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Accept-Charset", _AcceptCharset);
                    ++arrayIndex;
                }
            
                if (((_bits & 4194304L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Accept-Encoding", _AcceptEncoding);
                    ++arrayIndex;
                }
            
                if (((_bits & 8388608L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Accept-Language", _AcceptLanguage);
                    ++arrayIndex;
                }
            
                if (((_bits & 16777216L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Authorization", _Authorization);
                    ++arrayIndex;
                }
            
                if (((_bits & 33554432L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Cookie", _Cookie);
                    ++arrayIndex;
                }
            
                if (((_bits & 67108864L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Expect", _Expect);
                    ++arrayIndex;
                }
            
                if (((_bits & 134217728L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("From", _From);
                    ++arrayIndex;
                }
            
                if (((_bits & 268435456L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Host", _Host);
                    ++arrayIndex;
                }
            
                if (((_bits & 536870912L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("If-Match", _IfMatch);
                    ++arrayIndex;
                }
            
                if (((_bits & 1073741824L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("If-Modified-Since", _IfModifiedSince);
                    ++arrayIndex;
                }
            
                if (((_bits & 2147483648L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("If-None-Match", _IfNoneMatch);
                    ++arrayIndex;
                }
            
                if (((_bits & 4294967296L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("If-Range", _IfRange);
                    ++arrayIndex;
                }
            
                if (((_bits & 8589934592L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("If-Unmodified-Since", _IfUnmodifiedSince);
                    ++arrayIndex;
                }
            
                if (((_bits & 17179869184L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Max-Forwards", _MaxForwards);
                    ++arrayIndex;
                }
            
                if (((_bits & 34359738368L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Proxy-Authorization", _ProxyAuthorization);
                    ++arrayIndex;
                }
            
                if (((_bits & 68719476736L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Referer", _Referer);
                    ++arrayIndex;
                }
            
                if (((_bits & 137438953472L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Range", _Range);
                    ++arrayIndex;
                }
            
                if (((_bits & 274877906944L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("TE", _TE);
                    ++arrayIndex;
                }
            
                if (((_bits & 549755813888L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Translate", _Translate);
                    ++arrayIndex;
                }
            
                if (((_bits & 1099511627776L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("User-Agent", _UserAgent);
                    ++arrayIndex;
                }
            
                if (((_bits & 2199023255552L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Origin", _Origin);
                    ++arrayIndex;
                }
            
                if (((_bits & 4398046511104L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Access-Control-Request-Method", _AccessControlRequestMethod);
                    ++arrayIndex;
                }
            
                if (((_bits & 8796093022208L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Access-Control-Request-Headers", _AccessControlRequestHeaders);
                    ++arrayIndex;
                }
            
            ((ICollection<KeyValuePair<string, StringValues>>)MaybeUnknown)?.CopyTo(array, arrayIndex);
        }
        
        public unsafe void Append(byte[] keyBytes, int keyOffset, int keyLength, string value)
        {
            fixed(byte* ptr = keyBytes) { var pUB = ptr + keyOffset; var pUL = (ulong*)pUB; var pUI = (uint*)pUB; var pUS = (ushort*)pUB;
            switch(keyLength)
            {
                case 13:
                    {
                        if ((((pUL[0] & 16131893727263186911uL) == 5711458528024281411uL) && ((pUI[2] & 3755991007u) == 1330795598u) && ((pUB[12] & 223u) == 76u))) 
                        {
                            if (((_bits & 1L) != 0))
                            {
                                _CacheControl = AppendValue(_CacheControl, value);
                            }
                            else
                            {
                                _bits |= 1L;
                                _CacheControl = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUL[0] & 18437701552104792031uL) == 3266321689424580419uL) && ((pUI[2] & 3755991007u) == 1196310866u) && ((pUB[12] & 223u) == 69u))) 
                        {
                            if (((_bits & 131072L) != 0))
                            {
                                _ContentRange = AppendValue(_ContentRange, value);
                            }
                            else
                            {
                                _bits |= 131072L;
                                _ContentRange = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUL[0] & 16131858680330051551uL) == 4922237774822850892uL) && ((pUI[2] & 3755991007u) == 1162430025u) && ((pUB[12] & 223u) == 68u))) 
                        {
                            if (((_bits & 524288L) != 0))
                            {
                                _LastModified = AppendValue(_LastModified, value);
                            }
                            else
                            {
                                _bits |= 524288L;
                                _LastModified = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUL[0] & 16131858542891098079uL) == 6505821637182772545uL) && ((pUI[2] & 3755991007u) == 1330205761u) && ((pUB[12] & 223u) == 78u))) 
                        {
                            if (((_bits & 16777216L) != 0))
                            {
                                _Authorization = AppendValue(_Authorization, value);
                            }
                            else
                            {
                                _bits |= 16777216L;
                                _Authorization = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUL[0] & 18437701552106889183uL) == 3262099607620765257uL) && ((pUI[2] & 3755991007u) == 1129595213u) && ((pUB[12] & 223u) == 72u))) 
                        {
                            if (((_bits & 2147483648L) != 0))
                            {
                                _IfNoneMatch = AppendValue(_IfNoneMatch, value);
                            }
                            else
                            {
                                _bits |= 2147483648L;
                                _IfNoneMatch = new StringValues(value);
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
                                _Connection = AppendValue(_Connection, value);
                            }
                            else
                            {
                                _bits |= 2L;
                                _Connection = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUL[0] & 16131858680330051551uL) == 5281668125874799947uL) && ((pUS[4] & 57311u) == 17750u))) 
                        {
                            if (((_bits & 8L) != 0))
                            {
                                _KeepAlive = AppendValue(_KeepAlive, value);
                            }
                            else
                            {
                                _bits |= 8L;
                                _KeepAlive = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUL[0] & 16131858680330051551uL) == 4992030374873092949uL) && ((pUS[4] & 57311u) == 21582u))) 
                        {
                            if (((_bits & 1099511627776L) != 0))
                            {
                                _UserAgent = AppendValue(_UserAgent, value);
                            }
                            else
                            {
                                _bits |= 1099511627776L;
                                _UserAgent = new StringValues(value);
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
                                _Date = AppendValue(_Date, value);
                            }
                            else
                            {
                                _bits |= 4L;
                                _Date = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUI[0] & 3755991007u) == 1297044038u))) 
                        {
                            if (((_bits & 134217728L) != 0))
                            {
                                _From = AppendValue(_From, value);
                            }
                            else
                            {
                                _bits |= 134217728L;
                                _From = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUI[0] & 3755991007u) == 1414745928u))) 
                        {
                            if (((_bits & 268435456L) != 0))
                            {
                                _Host = AppendValue(_Host, value);
                            }
                            else
                            {
                                _bits |= 268435456L;
                                _Host = new StringValues(value);
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
                                _Pragma = AppendValue(_Pragma, value);
                            }
                            else
                            {
                                _bits |= 16L;
                                _Pragma = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUI[0] & 3755991007u) == 1162036033u) && ((pUS[2] & 57311u) == 21584u))) 
                        {
                            if (((_bits & 1048576L) != 0))
                            {
                                _Accept = AppendValue(_Accept, value);
                            }
                            else
                            {
                                _bits |= 1048576L;
                                _Accept = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUI[0] & 3755991007u) == 1263488835u) && ((pUS[2] & 57311u) == 17737u))) 
                        {
                            if (((_bits & 33554432L) != 0))
                            {
                                _Cookie = AppendValue(_Cookie, value);
                            }
                            else
                            {
                                _bits |= 33554432L;
                                _Cookie = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUI[0] & 3755991007u) == 1162893381u) && ((pUS[2] & 57311u) == 21571u))) 
                        {
                            if (((_bits & 67108864L) != 0))
                            {
                                _Expect = AppendValue(_Expect, value);
                            }
                            else
                            {
                                _bits |= 67108864L;
                                _Expect = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUI[0] & 3755991007u) == 1195987535u) && ((pUS[2] & 57311u) == 20041u))) 
                        {
                            if (((_bits & 2199023255552L) != 0))
                            {
                                _Origin = AppendValue(_Origin, value);
                            }
                            else
                            {
                                _bits |= 2199023255552L;
                                _Origin = new StringValues(value);
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
                                _Trailer = AppendValue(_Trailer, value);
                            }
                            else
                            {
                                _bits |= 32L;
                                _Trailer = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUI[0] & 3755991007u) == 1380405333u) && ((pUS[2] & 57311u) == 17473u) && ((pUB[6] & 223u) == 69u))) 
                        {
                            if (((_bits & 128L) != 0))
                            {
                                _Upgrade = AppendValue(_Upgrade, value);
                            }
                            else
                            {
                                _bits |= 128L;
                                _Upgrade = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUI[0] & 3755991007u) == 1314013527u) && ((pUS[2] & 57311u) == 20041u) && ((pUB[6] & 223u) == 71u))) 
                        {
                            if (((_bits & 512L) != 0))
                            {
                                _Warning = AppendValue(_Warning, value);
                            }
                            else
                            {
                                _bits |= 512L;
                                _Warning = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUI[0] & 3755991007u) == 1230002245u) && ((pUS[2] & 57311u) == 17746u) && ((pUB[6] & 223u) == 83u))) 
                        {
                            if (((_bits & 262144L) != 0))
                            {
                                _Expires = AppendValue(_Expires, value);
                            }
                            else
                            {
                                _bits |= 262144L;
                                _Expires = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUI[0] & 3755991007u) == 1162233170u) && ((pUS[2] & 57311u) == 17746u) && ((pUB[6] & 223u) == 82u))) 
                        {
                            if (((_bits & 68719476736L) != 0))
                            {
                                _Referer = AppendValue(_Referer, value);
                            }
                            else
                            {
                                _bits |= 68719476736L;
                                _Referer = new StringValues(value);
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
                                _TransferEncoding = AppendValue(_TransferEncoding, value);
                            }
                            else
                            {
                                _bits |= 64L;
                                _TransferEncoding = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUL[0] & 16131858542893195231uL) == 5064654363342751305uL) && ((pUL[1] & 16131858543427968991uL) == 4849894470315165001uL) && ((pUB[16] & 223u) == 69u))) 
                        {
                            if (((_bits & 1073741824L) != 0))
                            {
                                _IfModifiedSince = AppendValue(_IfModifiedSince, value);
                            }
                            else
                            {
                                _bits |= 1073741824L;
                                _IfModifiedSince = new StringValues(value);
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
                                _Via = AppendValue(_Via, value);
                            }
                            else
                            {
                                _bits |= 256L;
                                _Via = new StringValues(value);
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
                                _Allow = AppendValue(_Allow, value);
                            }
                            else
                            {
                                _bits |= 1024L;
                                _Allow = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUI[0] & 3755991007u) == 1196310866u) && ((pUB[4] & 223u) == 69u))) 
                        {
                            if (((_bits & 137438953472L) != 0))
                            {
                                _Range = AppendValue(_Range, value);
                            }
                            else
                            {
                                _bits |= 137438953472L;
                                _Range = new StringValues(value);
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
                                _ContentLength = AppendValue(_ContentLength, value);
                            }
                            else
                            {
                                _bits |= 2048L;
                                _ContentLength = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUL[0] & 16140865742145839071uL) == 4840617878229304129uL) && ((pUI[2] & 3755991007u) == 1397899592u) && ((pUS[6] & 57311u) == 21573u))) 
                        {
                            if (((_bits & 2097152L) != 0))
                            {
                                _AcceptCharset = AppendValue(_AcceptCharset, value);
                            }
                            else
                            {
                                _bits |= 2097152L;
                                _AcceptCharset = new StringValues(value);
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
                                _ContentType = AppendValue(_ContentType, value);
                            }
                            else
                            {
                                _bits |= 4096L;
                                _ContentType = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUL[0] & 16131858543427968991uL) == 6292178792217067853uL) && ((pUI[2] & 3755991007u) == 1396986433u))) 
                        {
                            if (((_bits & 17179869184L) != 0))
                            {
                                _MaxForwards = AppendValue(_MaxForwards, value);
                            }
                            else
                            {
                                _bits |= 17179869184L;
                                _MaxForwards = new StringValues(value);
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
                                _ContentEncoding = AppendValue(_ContentEncoding, value);
                            }
                            else
                            {
                                _bits |= 8192L;
                                _ContentEncoding = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUL[0] & 18437701552104792031uL) == 3266321689424580419uL) && ((pUL[1] & 16131858542891098079uL) == 4992030546487820620uL))) 
                        {
                            if (((_bits & 16384L) != 0))
                            {
                                _ContentLanguage = AppendValue(_ContentLanguage, value);
                            }
                            else
                            {
                                _bits |= 16384L;
                                _ContentLanguage = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUL[0] & 18437701552104792031uL) == 3266321689424580419uL) && ((pUL[1] & 16131858542891098079uL) == 5642809484339531596uL))) 
                        {
                            if (((_bits & 32768L) != 0))
                            {
                                _ContentLocation = AppendValue(_ContentLocation, value);
                            }
                            else
                            {
                                _bits |= 32768L;
                                _ContentLocation = new StringValues(value);
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
                                _ContentMD5 = AppendValue(_ContentMD5, value);
                            }
                            else
                            {
                                _bits |= 65536L;
                                _ContentMD5 = new StringValues(value);
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
                                _AcceptEncoding = AppendValue(_AcceptEncoding, value);
                            }
                            else
                            {
                                _bits |= 4194304L;
                                _AcceptEncoding = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUL[0] & 16140865742145839071uL) == 5489136224570655553uL) && ((pUI[2] & 3755991007u) == 1430736449u) && ((pUS[6] & 57311u) == 18241u) && ((pUB[14] & 223u) == 69u))) 
                        {
                            if (((_bits & 8388608L) != 0))
                            {
                                _AcceptLanguage = AppendValue(_AcceptLanguage, value);
                            }
                            else
                            {
                                _bits |= 8388608L;
                                _AcceptLanguage = new StringValues(value);
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
                                _IfMatch = AppendValue(_IfMatch, value);
                            }
                            else
                            {
                                _bits |= 536870912L;
                                _IfMatch = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUL[0] & 16131858542893195231uL) == 4992044754422023753uL))) 
                        {
                            if (((_bits & 4294967296L) != 0))
                            {
                                _IfRange = AppendValue(_IfRange, value);
                            }
                            else
                            {
                                _bits |= 4294967296L;
                                _IfRange = new StringValues(value);
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
                                _IfUnmodifiedSince = AppendValue(_IfUnmodifiedSince, value);
                            }
                            else
                            {
                                _bits |= 8589934592L;
                                _IfUnmodifiedSince = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUL[0] & 16131893727263186911uL) == 6143241228466999888uL) && ((pUL[1] & 16131858542891098079uL) == 6071233043632179284uL) && ((pUS[8] & 57311u) == 20297u) && ((pUB[18] & 223u) == 78u))) 
                        {
                            if (((_bits & 34359738368L) != 0))
                            {
                                _ProxyAuthorization = AppendValue(_ProxyAuthorization, value);
                            }
                            else
                            {
                                _bits |= 34359738368L;
                                _ProxyAuthorization = new StringValues(value);
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
                                _TE = AppendValue(_TE, value);
                            }
                            else
                            {
                                _bits |= 274877906944L;
                                _TE = new StringValues(value);
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
                                _Translate = AppendValue(_Translate, value);
                            }
                            else
                            {
                                _bits |= 549755813888L;
                                _Translate = new StringValues(value);
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
                                _AccessControlRequestMethod = AppendValue(_AccessControlRequestMethod, value);
                            }
                            else
                            {
                                _bits |= 4398046511104L;
                                _AccessControlRequestMethod = new StringValues(value);
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
                                _AccessControlRequestHeaders = AppendValue(_AccessControlRequestHeaders, value);
                            }
                            else
                            {
                                _bits |= 8796093022208L;
                                _AccessControlRequestHeaders = new StringValues(value);
                            }
                            return;
                        }
                    }
                    break;
            }}
            var key = System.Text.Encoding.ASCII.GetString(keyBytes, keyOffset, keyLength);
            StringValues existing;
            Unknown.TryGetValue(key, out existing);
            Unknown[key] = AppendValue(existing, value);
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
                        _current = new KeyValuePair<string, StringValues>("Cache-Control", _collection._CacheControl);
                        _state = 1;
                        return true;
                    }
                
                state1:
                    if (((_bits & 2L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Connection", _collection._Connection);
                        _state = 2;
                        return true;
                    }
                
                state2:
                    if (((_bits & 4L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Date", _collection._Date);
                        _state = 3;
                        return true;
                    }
                
                state3:
                    if (((_bits & 8L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Keep-Alive", _collection._KeepAlive);
                        _state = 4;
                        return true;
                    }
                
                state4:
                    if (((_bits & 16L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Pragma", _collection._Pragma);
                        _state = 5;
                        return true;
                    }
                
                state5:
                    if (((_bits & 32L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Trailer", _collection._Trailer);
                        _state = 6;
                        return true;
                    }
                
                state6:
                    if (((_bits & 64L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Transfer-Encoding", _collection._TransferEncoding);
                        _state = 7;
                        return true;
                    }
                
                state7:
                    if (((_bits & 128L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Upgrade", _collection._Upgrade);
                        _state = 8;
                        return true;
                    }
                
                state8:
                    if (((_bits & 256L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Via", _collection._Via);
                        _state = 9;
                        return true;
                    }
                
                state9:
                    if (((_bits & 512L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Warning", _collection._Warning);
                        _state = 10;
                        return true;
                    }
                
                state10:
                    if (((_bits & 1024L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Allow", _collection._Allow);
                        _state = 11;
                        return true;
                    }
                
                state11:
                    if (((_bits & 2048L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Length", _collection._ContentLength);
                        _state = 12;
                        return true;
                    }
                
                state12:
                    if (((_bits & 4096L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Type", _collection._ContentType);
                        _state = 13;
                        return true;
                    }
                
                state13:
                    if (((_bits & 8192L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Encoding", _collection._ContentEncoding);
                        _state = 14;
                        return true;
                    }
                
                state14:
                    if (((_bits & 16384L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Language", _collection._ContentLanguage);
                        _state = 15;
                        return true;
                    }
                
                state15:
                    if (((_bits & 32768L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Location", _collection._ContentLocation);
                        _state = 16;
                        return true;
                    }
                
                state16:
                    if (((_bits & 65536L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-MD5", _collection._ContentMD5);
                        _state = 17;
                        return true;
                    }
                
                state17:
                    if (((_bits & 131072L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Range", _collection._ContentRange);
                        _state = 18;
                        return true;
                    }
                
                state18:
                    if (((_bits & 262144L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Expires", _collection._Expires);
                        _state = 19;
                        return true;
                    }
                
                state19:
                    if (((_bits & 524288L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Last-Modified", _collection._LastModified);
                        _state = 20;
                        return true;
                    }
                
                state20:
                    if (((_bits & 1048576L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Accept", _collection._Accept);
                        _state = 21;
                        return true;
                    }
                
                state21:
                    if (((_bits & 2097152L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Accept-Charset", _collection._AcceptCharset);
                        _state = 22;
                        return true;
                    }
                
                state22:
                    if (((_bits & 4194304L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Accept-Encoding", _collection._AcceptEncoding);
                        _state = 23;
                        return true;
                    }
                
                state23:
                    if (((_bits & 8388608L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Accept-Language", _collection._AcceptLanguage);
                        _state = 24;
                        return true;
                    }
                
                state24:
                    if (((_bits & 16777216L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Authorization", _collection._Authorization);
                        _state = 25;
                        return true;
                    }
                
                state25:
                    if (((_bits & 33554432L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Cookie", _collection._Cookie);
                        _state = 26;
                        return true;
                    }
                
                state26:
                    if (((_bits & 67108864L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Expect", _collection._Expect);
                        _state = 27;
                        return true;
                    }
                
                state27:
                    if (((_bits & 134217728L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("From", _collection._From);
                        _state = 28;
                        return true;
                    }
                
                state28:
                    if (((_bits & 268435456L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Host", _collection._Host);
                        _state = 29;
                        return true;
                    }
                
                state29:
                    if (((_bits & 536870912L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("If-Match", _collection._IfMatch);
                        _state = 30;
                        return true;
                    }
                
                state30:
                    if (((_bits & 1073741824L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("If-Modified-Since", _collection._IfModifiedSince);
                        _state = 31;
                        return true;
                    }
                
                state31:
                    if (((_bits & 2147483648L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("If-None-Match", _collection._IfNoneMatch);
                        _state = 32;
                        return true;
                    }
                
                state32:
                    if (((_bits & 4294967296L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("If-Range", _collection._IfRange);
                        _state = 33;
                        return true;
                    }
                
                state33:
                    if (((_bits & 8589934592L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("If-Unmodified-Since", _collection._IfUnmodifiedSince);
                        _state = 34;
                        return true;
                    }
                
                state34:
                    if (((_bits & 17179869184L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Max-Forwards", _collection._MaxForwards);
                        _state = 35;
                        return true;
                    }
                
                state35:
                    if (((_bits & 34359738368L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Proxy-Authorization", _collection._ProxyAuthorization);
                        _state = 36;
                        return true;
                    }
                
                state36:
                    if (((_bits & 68719476736L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Referer", _collection._Referer);
                        _state = 37;
                        return true;
                    }
                
                state37:
                    if (((_bits & 137438953472L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Range", _collection._Range);
                        _state = 38;
                        return true;
                    }
                
                state38:
                    if (((_bits & 274877906944L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("TE", _collection._TE);
                        _state = 39;
                        return true;
                    }
                
                state39:
                    if (((_bits & 549755813888L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Translate", _collection._Translate);
                        _state = 40;
                        return true;
                    }
                
                state40:
                    if (((_bits & 1099511627776L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("User-Agent", _collection._UserAgent);
                        _state = 41;
                        return true;
                    }
                
                state41:
                    if (((_bits & 2199023255552L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Origin", _collection._Origin);
                        _state = 42;
                        return true;
                    }
                
                state42:
                    if (((_bits & 4398046511104L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Request-Method", _collection._AccessControlRequestMethod);
                        _state = 43;
                        return true;
                    }
                
                state43:
                    if (((_bits & 8796093022208L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Request-Headers", _collection._AccessControlRequestHeaders);
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
        
        private StringValues _CacheControl;
        private StringValues _Connection;
        private StringValues _Date;
        private StringValues _KeepAlive;
        private StringValues _Pragma;
        private StringValues _Trailer;
        private StringValues _TransferEncoding;
        private StringValues _Upgrade;
        private StringValues _Via;
        private StringValues _Warning;
        private StringValues _Allow;
        private StringValues _ContentLength;
        private StringValues _ContentType;
        private StringValues _ContentEncoding;
        private StringValues _ContentLanguage;
        private StringValues _ContentLocation;
        private StringValues _ContentMD5;
        private StringValues _ContentRange;
        private StringValues _Expires;
        private StringValues _LastModified;
        private StringValues _AcceptRanges;
        private StringValues _Age;
        private StringValues _ETag;
        private StringValues _Location;
        private StringValues _ProxyAutheticate;
        private StringValues _RetryAfter;
        private StringValues _Server;
        private StringValues _SetCookie;
        private StringValues _Vary;
        private StringValues _WWWAuthenticate;
        private StringValues _AccessControlAllowCredentials;
        private StringValues _AccessControlAllowHeaders;
        private StringValues _AccessControlAllowMethods;
        private StringValues _AccessControlAllowOrigin;
        private StringValues _AccessControlExposeHeaders;
        private StringValues _AccessControlMaxAge;
        
        private byte[] _rawConnection;
        private byte[] _rawDate;
        private byte[] _rawTransferEncoding;
        private byte[] _rawContentLength;
        private byte[] _rawServer;
        
        public StringValues HeaderCacheControl
        {
            get
            {
                if (((_bits & 1L) != 0))
                {
                    return _CacheControl;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 1L;
                _CacheControl = value; 
            }
        }
        public StringValues HeaderConnection
        {
            get
            {
                if (((_bits & 2L) != 0))
                {
                    return _Connection;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 2L;
                _Connection = value; 
                _rawConnection = null;
            }
        }
        public StringValues HeaderDate
        {
            get
            {
                if (((_bits & 4L) != 0))
                {
                    return _Date;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 4L;
                _Date = value; 
                _rawDate = null;
            }
        }
        public StringValues HeaderKeepAlive
        {
            get
            {
                if (((_bits & 8L) != 0))
                {
                    return _KeepAlive;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 8L;
                _KeepAlive = value; 
            }
        }
        public StringValues HeaderPragma
        {
            get
            {
                if (((_bits & 16L) != 0))
                {
                    return _Pragma;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 16L;
                _Pragma = value; 
            }
        }
        public StringValues HeaderTrailer
        {
            get
            {
                if (((_bits & 32L) != 0))
                {
                    return _Trailer;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 32L;
                _Trailer = value; 
            }
        }
        public StringValues HeaderTransferEncoding
        {
            get
            {
                if (((_bits & 64L) != 0))
                {
                    return _TransferEncoding;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 64L;
                _TransferEncoding = value; 
                _rawTransferEncoding = null;
            }
        }
        public StringValues HeaderUpgrade
        {
            get
            {
                if (((_bits & 128L) != 0))
                {
                    return _Upgrade;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 128L;
                _Upgrade = value; 
            }
        }
        public StringValues HeaderVia
        {
            get
            {
                if (((_bits & 256L) != 0))
                {
                    return _Via;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 256L;
                _Via = value; 
            }
        }
        public StringValues HeaderWarning
        {
            get
            {
                if (((_bits & 512L) != 0))
                {
                    return _Warning;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 512L;
                _Warning = value; 
            }
        }
        public StringValues HeaderAllow
        {
            get
            {
                if (((_bits & 1024L) != 0))
                {
                    return _Allow;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 1024L;
                _Allow = value; 
            }
        }
        public StringValues HeaderContentLength
        {
            get
            {
                if (((_bits & 2048L) != 0))
                {
                    return _ContentLength;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 2048L;
                _ContentLength = value; 
                _rawContentLength = null;
            }
        }
        public StringValues HeaderContentType
        {
            get
            {
                if (((_bits & 4096L) != 0))
                {
                    return _ContentType;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 4096L;
                _ContentType = value; 
            }
        }
        public StringValues HeaderContentEncoding
        {
            get
            {
                if (((_bits & 8192L) != 0))
                {
                    return _ContentEncoding;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 8192L;
                _ContentEncoding = value; 
            }
        }
        public StringValues HeaderContentLanguage
        {
            get
            {
                if (((_bits & 16384L) != 0))
                {
                    return _ContentLanguage;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 16384L;
                _ContentLanguage = value; 
            }
        }
        public StringValues HeaderContentLocation
        {
            get
            {
                if (((_bits & 32768L) != 0))
                {
                    return _ContentLocation;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 32768L;
                _ContentLocation = value; 
            }
        }
        public StringValues HeaderContentMD5
        {
            get
            {
                if (((_bits & 65536L) != 0))
                {
                    return _ContentMD5;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 65536L;
                _ContentMD5 = value; 
            }
        }
        public StringValues HeaderContentRange
        {
            get
            {
                if (((_bits & 131072L) != 0))
                {
                    return _ContentRange;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 131072L;
                _ContentRange = value; 
            }
        }
        public StringValues HeaderExpires
        {
            get
            {
                if (((_bits & 262144L) != 0))
                {
                    return _Expires;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 262144L;
                _Expires = value; 
            }
        }
        public StringValues HeaderLastModified
        {
            get
            {
                if (((_bits & 524288L) != 0))
                {
                    return _LastModified;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 524288L;
                _LastModified = value; 
            }
        }
        public StringValues HeaderAcceptRanges
        {
            get
            {
                if (((_bits & 1048576L) != 0))
                {
                    return _AcceptRanges;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 1048576L;
                _AcceptRanges = value; 
            }
        }
        public StringValues HeaderAge
        {
            get
            {
                if (((_bits & 2097152L) != 0))
                {
                    return _Age;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 2097152L;
                _Age = value; 
            }
        }
        public StringValues HeaderETag
        {
            get
            {
                if (((_bits & 4194304L) != 0))
                {
                    return _ETag;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 4194304L;
                _ETag = value; 
            }
        }
        public StringValues HeaderLocation
        {
            get
            {
                if (((_bits & 8388608L) != 0))
                {
                    return _Location;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 8388608L;
                _Location = value; 
            }
        }
        public StringValues HeaderProxyAutheticate
        {
            get
            {
                if (((_bits & 16777216L) != 0))
                {
                    return _ProxyAutheticate;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 16777216L;
                _ProxyAutheticate = value; 
            }
        }
        public StringValues HeaderRetryAfter
        {
            get
            {
                if (((_bits & 33554432L) != 0))
                {
                    return _RetryAfter;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 33554432L;
                _RetryAfter = value; 
            }
        }
        public StringValues HeaderServer
        {
            get
            {
                if (((_bits & 67108864L) != 0))
                {
                    return _Server;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 67108864L;
                _Server = value; 
                _rawServer = null;
            }
        }
        public StringValues HeaderSetCookie
        {
            get
            {
                if (((_bits & 134217728L) != 0))
                {
                    return _SetCookie;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 134217728L;
                _SetCookie = value; 
            }
        }
        public StringValues HeaderVary
        {
            get
            {
                if (((_bits & 268435456L) != 0))
                {
                    return _Vary;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 268435456L;
                _Vary = value; 
            }
        }
        public StringValues HeaderWWWAuthenticate
        {
            get
            {
                if (((_bits & 536870912L) != 0))
                {
                    return _WWWAuthenticate;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 536870912L;
                _WWWAuthenticate = value; 
            }
        }
        public StringValues HeaderAccessControlAllowCredentials
        {
            get
            {
                if (((_bits & 1073741824L) != 0))
                {
                    return _AccessControlAllowCredentials;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 1073741824L;
                _AccessControlAllowCredentials = value; 
            }
        }
        public StringValues HeaderAccessControlAllowHeaders
        {
            get
            {
                if (((_bits & 2147483648L) != 0))
                {
                    return _AccessControlAllowHeaders;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 2147483648L;
                _AccessControlAllowHeaders = value; 
            }
        }
        public StringValues HeaderAccessControlAllowMethods
        {
            get
            {
                if (((_bits & 4294967296L) != 0))
                {
                    return _AccessControlAllowMethods;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 4294967296L;
                _AccessControlAllowMethods = value; 
            }
        }
        public StringValues HeaderAccessControlAllowOrigin
        {
            get
            {
                if (((_bits & 8589934592L) != 0))
                {
                    return _AccessControlAllowOrigin;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 8589934592L;
                _AccessControlAllowOrigin = value; 
            }
        }
        public StringValues HeaderAccessControlExposeHeaders
        {
            get
            {
                if (((_bits & 17179869184L) != 0))
                {
                    return _AccessControlExposeHeaders;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 17179869184L;
                _AccessControlExposeHeaders = value; 
            }
        }
        public StringValues HeaderAccessControlMaxAge
        {
            get
            {
                if (((_bits & 34359738368L) != 0))
                {
                    return _AccessControlMaxAge;
                }
                return StringValues.Empty;
            }
            set
            {
                _bits |= 34359738368L;
                _AccessControlMaxAge = value; 
            }
        }
        
        public void SetRawConnection(StringValues value, byte[] raw)
        {
            _bits |= 2L;
            _Connection = value; 
            _rawConnection = raw;
        }
        public void SetRawDate(StringValues value, byte[] raw)
        {
            _bits |= 4L;
            _Date = value; 
            _rawDate = raw;
        }
        public void SetRawTransferEncoding(StringValues value, byte[] raw)
        {
            _bits |= 64L;
            _TransferEncoding = value; 
            _rawTransferEncoding = raw;
        }
        public void SetRawContentLength(StringValues value, byte[] raw)
        {
            _bits |= 2048L;
            _ContentLength = value; 
            _rawContentLength = raw;
        }
        public void SetRawServer(StringValues value, byte[] raw)
        {
            _bits |= 67108864L;
            _Server = value; 
            _rawServer = raw;
        }
        protected override int GetCountFast()
        {
            return BitCount(_bits) + (MaybeUnknown?.Count ?? 0);
        }
        protected override StringValues GetValueFast(string key)
        {
            switch(key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 1L) != 0))
                            {
                                return _CacheControl;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 131072L) != 0))
                            {
                                return _ContentRange;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 524288L) != 0))
                            {
                                return _LastModified;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Accept-Ranges".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 1048576L) != 0))
                            {
                                return _AcceptRanges;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _Connection;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Keep-Alive".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 8L) != 0))
                            {
                                return _KeepAlive;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Set-Cookie".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 134217728L) != 0))
                            {
                                return _SetCookie;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _Date;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("ETag".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 4194304L) != 0))
                            {
                                return _ETag;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Vary".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 268435456L) != 0))
                            {
                                return _Vary;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _Pragma;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Server".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 67108864L) != 0))
                            {
                                return _Server;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _Trailer;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Upgrade".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 128L) != 0))
                            {
                                return _Upgrade;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Warning".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 512L) != 0))
                            {
                                return _Warning;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Expires".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 262144L) != 0))
                            {
                                return _Expires;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _TransferEncoding;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Proxy-Autheticate".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 16777216L) != 0))
                            {
                                return _ProxyAutheticate;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _Via;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Age".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 2097152L) != 0))
                            {
                                return _Age;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _Allow;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _ContentLength;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _ContentType;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _ContentEncoding;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 16384L) != 0))
                            {
                                return _ContentLanguage;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 32768L) != 0))
                            {
                                return _ContentLocation;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("WWW-Authenticate".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 536870912L) != 0))
                            {
                                return _WWWAuthenticate;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _ContentMD5;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Retry-After".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 33554432L) != 0))
                            {
                                return _RetryAfter;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _Location;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _AccessControlAllowCredentials;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _AccessControlAllowHeaders;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    
                        if ("Access-Control-Allow-Methods".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 4294967296L) != 0))
                            {
                                return _AccessControlAllowMethods;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _AccessControlAllowOrigin;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _AccessControlExposeHeaders;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
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
                                return _AccessControlMaxAge;
                            }
                            else
                            {
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }
                        }
                    }
                    break;
}
            if (MaybeUnknown == null) 
            {
                throw new System.Collections.Generic.KeyNotFoundException();
            }
            return MaybeUnknown[key];
        }
        protected override bool TryGetValueFast(string key, out StringValues value)
        {
            switch(key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 1L) != 0))
                            {
                                value = _CacheControl;
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
                                value = _ContentRange;
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
                                value = _LastModified;
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
                                value = _AcceptRanges;
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
                                value = _Connection;
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
                                value = _KeepAlive;
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
                                value = _SetCookie;
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
                                value = _Date;
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
                                value = _ETag;
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
                                value = _Vary;
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
                                value = _Pragma;
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
                                value = _Server;
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
                                value = _Trailer;
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
                                value = _Upgrade;
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
                                value = _Warning;
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
                                value = _Expires;
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
                                value = _TransferEncoding;
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
                                value = _ProxyAutheticate;
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
                                value = _Via;
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
                                value = _Age;
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
                                value = _Allow;
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
                                value = _ContentLength;
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
                                value = _ContentType;
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
                                value = _ContentEncoding;
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
                                value = _ContentLanguage;
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
                                value = _ContentLocation;
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
                                value = _WWWAuthenticate;
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
                                value = _ContentMD5;
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
                                value = _RetryAfter;
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
                                value = _Location;
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
                                value = _AccessControlAllowCredentials;
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
                                value = _AccessControlAllowHeaders;
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
                                value = _AccessControlAllowMethods;
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
                                value = _AccessControlAllowOrigin;
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
                                value = _AccessControlExposeHeaders;
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
                                value = _AccessControlMaxAge;
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
            switch(key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 1L;
                            _CacheControl = value;
                            return;
                        }
                    
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 131072L;
                            _ContentRange = value;
                            return;
                        }
                    
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 524288L;
                            _LastModified = value;
                            return;
                        }
                    
                        if ("Accept-Ranges".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 1048576L;
                            _AcceptRanges = value;
                            return;
                        }
                    }
                    break;

                case 10:
                    {
                        if ("Connection".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 2L;
                            _Connection = value;
                            _rawConnection = null;
                            return;
                        }
                    
                        if ("Keep-Alive".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 8L;
                            _KeepAlive = value;
                            return;
                        }
                    
                        if ("Set-Cookie".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 134217728L;
                            _SetCookie = value;
                            return;
                        }
                    }
                    break;

                case 4:
                    {
                        if ("Date".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 4L;
                            _Date = value;
                            _rawDate = null;
                            return;
                        }
                    
                        if ("ETag".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 4194304L;
                            _ETag = value;
                            return;
                        }
                    
                        if ("Vary".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 268435456L;
                            _Vary = value;
                            return;
                        }
                    }
                    break;

                case 6:
                    {
                        if ("Pragma".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 16L;
                            _Pragma = value;
                            return;
                        }
                    
                        if ("Server".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 67108864L;
                            _Server = value;
                            _rawServer = null;
                            return;
                        }
                    }
                    break;

                case 7:
                    {
                        if ("Trailer".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 32L;
                            _Trailer = value;
                            return;
                        }
                    
                        if ("Upgrade".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 128L;
                            _Upgrade = value;
                            return;
                        }
                    
                        if ("Warning".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 512L;
                            _Warning = value;
                            return;
                        }
                    
                        if ("Expires".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 262144L;
                            _Expires = value;
                            return;
                        }
                    }
                    break;

                case 17:
                    {
                        if ("Transfer-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 64L;
                            _TransferEncoding = value;
                            _rawTransferEncoding = null;
                            return;
                        }
                    
                        if ("Proxy-Autheticate".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 16777216L;
                            _ProxyAutheticate = value;
                            return;
                        }
                    }
                    break;

                case 3:
                    {
                        if ("Via".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 256L;
                            _Via = value;
                            return;
                        }
                    
                        if ("Age".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 2097152L;
                            _Age = value;
                            return;
                        }
                    }
                    break;

                case 5:
                    {
                        if ("Allow".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 1024L;
                            _Allow = value;
                            return;
                        }
                    }
                    break;

                case 14:
                    {
                        if ("Content-Length".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 2048L;
                            _ContentLength = value;
                            _rawContentLength = null;
                            return;
                        }
                    }
                    break;

                case 12:
                    {
                        if ("Content-Type".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 4096L;
                            _ContentType = value;
                            return;
                        }
                    }
                    break;

                case 16:
                    {
                        if ("Content-Encoding".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 8192L;
                            _ContentEncoding = value;
                            return;
                        }
                    
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 16384L;
                            _ContentLanguage = value;
                            return;
                        }
                    
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 32768L;
                            _ContentLocation = value;
                            return;
                        }
                    
                        if ("WWW-Authenticate".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 536870912L;
                            _WWWAuthenticate = value;
                            return;
                        }
                    }
                    break;

                case 11:
                    {
                        if ("Content-MD5".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 65536L;
                            _ContentMD5 = value;
                            return;
                        }
                    
                        if ("Retry-After".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 33554432L;
                            _RetryAfter = value;
                            return;
                        }
                    }
                    break;

                case 8:
                    {
                        if ("Location".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 8388608L;
                            _Location = value;
                            return;
                        }
                    }
                    break;

                case 32:
                    {
                        if ("Access-Control-Allow-Credentials".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 1073741824L;
                            _AccessControlAllowCredentials = value;
                            return;
                        }
                    }
                    break;

                case 28:
                    {
                        if ("Access-Control-Allow-Headers".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 2147483648L;
                            _AccessControlAllowHeaders = value;
                            return;
                        }
                    
                        if ("Access-Control-Allow-Methods".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 4294967296L;
                            _AccessControlAllowMethods = value;
                            return;
                        }
                    }
                    break;

                case 27:
                    {
                        if ("Access-Control-Allow-Origin".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 8589934592L;
                            _AccessControlAllowOrigin = value;
                            return;
                        }
                    }
                    break;

                case 29:
                    {
                        if ("Access-Control-Expose-Headers".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 17179869184L;
                            _AccessControlExposeHeaders = value;
                            return;
                        }
                    }
                    break;

                case 22:
                    {
                        if ("Access-Control-Max-Age".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            _bits |= 34359738368L;
                            _AccessControlMaxAge = value;
                            return;
                        }
                    }
                    break;
}
            Unknown[key] = value;
        }
        protected override void AddValueFast(string key, StringValues value)
        {
            switch(key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 1L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 1L;
                            _CacheControl = value;
                            return;
                        }
                    
                        if ("Content-Range".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 131072L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 131072L;
                            _ContentRange = value;
                            return;
                        }
                    
                        if ("Last-Modified".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 524288L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 524288L;
                            _LastModified = value;
                            return;
                        }
                    
                        if ("Accept-Ranges".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 1048576L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 1048576L;
                            _AcceptRanges = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 2L;
                            _Connection = value;
                            _rawConnection = null;
                            return;
                        }
                    
                        if ("Keep-Alive".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 8L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 8L;
                            _KeepAlive = value;
                            return;
                        }
                    
                        if ("Set-Cookie".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 134217728L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 134217728L;
                            _SetCookie = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 4L;
                            _Date = value;
                            _rawDate = null;
                            return;
                        }
                    
                        if ("ETag".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 4194304L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 4194304L;
                            _ETag = value;
                            return;
                        }
                    
                        if ("Vary".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 268435456L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 268435456L;
                            _Vary = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 16L;
                            _Pragma = value;
                            return;
                        }
                    
                        if ("Server".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 67108864L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 67108864L;
                            _Server = value;
                            _rawServer = null;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 32L;
                            _Trailer = value;
                            return;
                        }
                    
                        if ("Upgrade".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 128L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 128L;
                            _Upgrade = value;
                            return;
                        }
                    
                        if ("Warning".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 512L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 512L;
                            _Warning = value;
                            return;
                        }
                    
                        if ("Expires".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 262144L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 262144L;
                            _Expires = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 64L;
                            _TransferEncoding = value;
                            _rawTransferEncoding = null;
                            return;
                        }
                    
                        if ("Proxy-Autheticate".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 16777216L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 16777216L;
                            _ProxyAutheticate = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 256L;
                            _Via = value;
                            return;
                        }
                    
                        if ("Age".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 2097152L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 2097152L;
                            _Age = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 1024L;
                            _Allow = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 2048L;
                            _ContentLength = value;
                            _rawContentLength = null;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 4096L;
                            _ContentType = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 8192L;
                            _ContentEncoding = value;
                            return;
                        }
                    
                        if ("Content-Language".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 16384L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 16384L;
                            _ContentLanguage = value;
                            return;
                        }
                    
                        if ("Content-Location".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 32768L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 32768L;
                            _ContentLocation = value;
                            return;
                        }
                    
                        if ("WWW-Authenticate".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 536870912L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 536870912L;
                            _WWWAuthenticate = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 65536L;
                            _ContentMD5 = value;
                            return;
                        }
                    
                        if ("Retry-After".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 33554432L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 33554432L;
                            _RetryAfter = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 8388608L;
                            _Location = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 1073741824L;
                            _AccessControlAllowCredentials = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 2147483648L;
                            _AccessControlAllowHeaders = value;
                            return;
                        }
                    
                        if ("Access-Control-Allow-Methods".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (((_bits & 4294967296L) != 0))
                            {
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 4294967296L;
                            _AccessControlAllowMethods = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 8589934592L;
                            _AccessControlAllowOrigin = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 17179869184L;
                            _AccessControlExposeHeaders = value;
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
                                throw new ArgumentException("An item with the same key has already been added.");
                            }
                            _bits |= 34359738368L;
                            _AccessControlMaxAge = value;
                            return;
                        }
                    }
                    break;
            }
            Unknown.Add(key, value);
        }
        protected override bool RemoveFast(string key)
        {
            switch(key.Length)
            {
                case 13:
                    {
                        if ("Cache-Control".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {
                            if (((_bits & 1L) != 0))
                            {
                                _bits &= ~1L;
                                _CacheControl = StringValues.Empty;
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
                                _ContentRange = StringValues.Empty;
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
                                _LastModified = StringValues.Empty;
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
                                _AcceptRanges = StringValues.Empty;
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
                                _Connection = StringValues.Empty;
                                _rawConnection = null;
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
                                _KeepAlive = StringValues.Empty;
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
                                _SetCookie = StringValues.Empty;
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
                                _Date = StringValues.Empty;
                                _rawDate = null;
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
                                _ETag = StringValues.Empty;
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
                                _Vary = StringValues.Empty;
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
                                _Pragma = StringValues.Empty;
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
                                _Server = StringValues.Empty;
                                _rawServer = null;
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
                                _Trailer = StringValues.Empty;
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
                                _Upgrade = StringValues.Empty;
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
                                _Warning = StringValues.Empty;
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
                                _Expires = StringValues.Empty;
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
                                _TransferEncoding = StringValues.Empty;
                                _rawTransferEncoding = null;
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
                                _ProxyAutheticate = StringValues.Empty;
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
                                _Via = StringValues.Empty;
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
                                _Age = StringValues.Empty;
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
                                _Allow = StringValues.Empty;
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
                                _ContentLength = StringValues.Empty;
                                _rawContentLength = null;
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
                                _ContentType = StringValues.Empty;
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
                                _ContentEncoding = StringValues.Empty;
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
                                _ContentLanguage = StringValues.Empty;
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
                                _ContentLocation = StringValues.Empty;
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
                                _WWWAuthenticate = StringValues.Empty;
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
                                _ContentMD5 = StringValues.Empty;
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
                                _RetryAfter = StringValues.Empty;
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
                                _Location = StringValues.Empty;
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
                                _AccessControlAllowCredentials = StringValues.Empty;
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
                                _AccessControlAllowHeaders = StringValues.Empty;
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
                                _AccessControlAllowMethods = StringValues.Empty;
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
                                _AccessControlAllowOrigin = StringValues.Empty;
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
                                _AccessControlExposeHeaders = StringValues.Empty;
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
                                _AccessControlMaxAge = StringValues.Empty;
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
            MaybeUnknown?.Clear();
        }
        
        protected override void CopyToFast(KeyValuePair<string, StringValues>[] array, int arrayIndex)
        {
            if (arrayIndex < 0)
            {
                throw new ArgumentException();
            }
            
                if (((_bits & 1L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Cache-Control", _CacheControl);
                    ++arrayIndex;
                }
            
                if (((_bits & 2L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Connection", _Connection);
                    ++arrayIndex;
                }
            
                if (((_bits & 4L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Date", _Date);
                    ++arrayIndex;
                }
            
                if (((_bits & 8L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Keep-Alive", _KeepAlive);
                    ++arrayIndex;
                }
            
                if (((_bits & 16L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Pragma", _Pragma);
                    ++arrayIndex;
                }
            
                if (((_bits & 32L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Trailer", _Trailer);
                    ++arrayIndex;
                }
            
                if (((_bits & 64L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Transfer-Encoding", _TransferEncoding);
                    ++arrayIndex;
                }
            
                if (((_bits & 128L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Upgrade", _Upgrade);
                    ++arrayIndex;
                }
            
                if (((_bits & 256L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Via", _Via);
                    ++arrayIndex;
                }
            
                if (((_bits & 512L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Warning", _Warning);
                    ++arrayIndex;
                }
            
                if (((_bits & 1024L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Allow", _Allow);
                    ++arrayIndex;
                }
            
                if (((_bits & 2048L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Length", _ContentLength);
                    ++arrayIndex;
                }
            
                if (((_bits & 4096L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Type", _ContentType);
                    ++arrayIndex;
                }
            
                if (((_bits & 8192L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Encoding", _ContentEncoding);
                    ++arrayIndex;
                }
            
                if (((_bits & 16384L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Language", _ContentLanguage);
                    ++arrayIndex;
                }
            
                if (((_bits & 32768L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Location", _ContentLocation);
                    ++arrayIndex;
                }
            
                if (((_bits & 65536L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-MD5", _ContentMD5);
                    ++arrayIndex;
                }
            
                if (((_bits & 131072L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Content-Range", _ContentRange);
                    ++arrayIndex;
                }
            
                if (((_bits & 262144L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Expires", _Expires);
                    ++arrayIndex;
                }
            
                if (((_bits & 524288L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Last-Modified", _LastModified);
                    ++arrayIndex;
                }
            
                if (((_bits & 1048576L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Accept-Ranges", _AcceptRanges);
                    ++arrayIndex;
                }
            
                if (((_bits & 2097152L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Age", _Age);
                    ++arrayIndex;
                }
            
                if (((_bits & 4194304L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("ETag", _ETag);
                    ++arrayIndex;
                }
            
                if (((_bits & 8388608L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Location", _Location);
                    ++arrayIndex;
                }
            
                if (((_bits & 16777216L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Proxy-Autheticate", _ProxyAutheticate);
                    ++arrayIndex;
                }
            
                if (((_bits & 33554432L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Retry-After", _RetryAfter);
                    ++arrayIndex;
                }
            
                if (((_bits & 67108864L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Server", _Server);
                    ++arrayIndex;
                }
            
                if (((_bits & 134217728L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Set-Cookie", _SetCookie);
                    ++arrayIndex;
                }
            
                if (((_bits & 268435456L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Vary", _Vary);
                    ++arrayIndex;
                }
            
                if (((_bits & 536870912L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("WWW-Authenticate", _WWWAuthenticate);
                    ++arrayIndex;
                }
            
                if (((_bits & 1073741824L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Access-Control-Allow-Credentials", _AccessControlAllowCredentials);
                    ++arrayIndex;
                }
            
                if (((_bits & 2147483648L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Access-Control-Allow-Headers", _AccessControlAllowHeaders);
                    ++arrayIndex;
                }
            
                if (((_bits & 4294967296L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Access-Control-Allow-Methods", _AccessControlAllowMethods);
                    ++arrayIndex;
                }
            
                if (((_bits & 8589934592L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Access-Control-Allow-Origin", _AccessControlAllowOrigin);
                    ++arrayIndex;
                }
            
                if (((_bits & 17179869184L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Access-Control-Expose-Headers", _AccessControlExposeHeaders);
                    ++arrayIndex;
                }
            
                if (((_bits & 34359738368L) != 0)) 
                {
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException();
                    }

                    array[arrayIndex] = new KeyValuePair<string, StringValues>("Access-Control-Max-Age", _AccessControlMaxAge);
                    ++arrayIndex;
                }
            
            ((ICollection<KeyValuePair<string, StringValues>>)MaybeUnknown)?.CopyTo(array, arrayIndex);
        }
        
        protected void CopyToFast(ref MemoryPoolIterator2 output)
        {
            
                if (((_bits & 1L) != 0)) 
                { 
                    foreach(var value in _CacheControl)
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
                    if (_rawConnection != null) 
                    {
                        output.CopyFrom(_rawConnection, 0, _rawConnection.Length);
                    } else 
                    foreach(var value in _Connection)
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
                    if (_rawDate != null) 
                    {
                        output.CopyFrom(_rawDate, 0, _rawDate.Length);
                    } else 
                    foreach(var value in _Date)
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
                    foreach(var value in _KeepAlive)
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
                    foreach(var value in _Pragma)
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
                    foreach(var value in _Trailer)
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
                    if (_rawTransferEncoding != null) 
                    {
                        output.CopyFrom(_rawTransferEncoding, 0, _rawTransferEncoding.Length);
                    } else 
                    foreach(var value in _TransferEncoding)
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
                    foreach(var value in _Upgrade)
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
                    foreach(var value in _Via)
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
                    foreach(var value in _Warning)
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
                    foreach(var value in _Allow)
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
                    if (_rawContentLength != null) 
                    {
                        output.CopyFrom(_rawContentLength, 0, _rawContentLength.Length);
                    } else 
                    foreach(var value in _ContentLength)
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
                    foreach(var value in _ContentType)
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
                    foreach(var value in _ContentEncoding)
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
                    foreach(var value in _ContentLanguage)
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
                    foreach(var value in _ContentLocation)
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
                    foreach(var value in _ContentMD5)
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
                    foreach(var value in _ContentRange)
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
                    foreach(var value in _Expires)
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
                    foreach(var value in _LastModified)
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
                    foreach(var value in _AcceptRanges)
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
                    foreach(var value in _Age)
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
                    foreach(var value in _ETag)
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
                    foreach(var value in _Location)
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
                    foreach(var value in _ProxyAutheticate)
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
                    foreach(var value in _RetryAfter)
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
                    if (_rawServer != null) 
                    {
                        output.CopyFrom(_rawServer, 0, _rawServer.Length);
                    } else 
                    foreach(var value in _Server)
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
                    foreach(var value in _SetCookie)
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
                    foreach(var value in _Vary)
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
                    foreach(var value in _WWWAuthenticate)
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
                    foreach(var value in _AccessControlAllowCredentials)
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
                    foreach(var value in _AccessControlAllowHeaders)
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
                    foreach(var value in _AccessControlAllowMethods)
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
                    foreach(var value in _AccessControlAllowOrigin)
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
                    foreach(var value in _AccessControlExposeHeaders)
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
                    foreach(var value in _AccessControlMaxAge)
                    {
                        if (value != null)
                        {
                            output.CopyFrom(_headerBytes, 583, 26);
                            output.CopyFromAscii(value);
                        }
                    }
                }
            
        }
        public unsafe void Append(byte[] keyBytes, int keyOffset, int keyLength, string value)
        {
            fixed(byte* ptr = keyBytes) { var pUB = ptr + keyOffset; var pUL = (ulong*)pUB; var pUI = (uint*)pUB; var pUS = (ushort*)pUB;
            switch(keyLength)
            {
                case 13:
                    {
                        if ((((pUL[0] & 16131893727263186911uL) == 5711458528024281411uL) && ((pUI[2] & 3755991007u) == 1330795598u) && ((pUB[12] & 223u) == 76u))) 
                        {
                            if (((_bits & 1L) != 0))
                            {
                                _CacheControl = AppendValue(_CacheControl, value);
                            }
                            else
                            {
                                _bits |= 1L;
                                _CacheControl = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUL[0] & 18437701552104792031uL) == 3266321689424580419uL) && ((pUI[2] & 3755991007u) == 1196310866u) && ((pUB[12] & 223u) == 69u))) 
                        {
                            if (((_bits & 131072L) != 0))
                            {
                                _ContentRange = AppendValue(_ContentRange, value);
                            }
                            else
                            {
                                _bits |= 131072L;
                                _ContentRange = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUL[0] & 16131858680330051551uL) == 4922237774822850892uL) && ((pUI[2] & 3755991007u) == 1162430025u) && ((pUB[12] & 223u) == 68u))) 
                        {
                            if (((_bits & 524288L) != 0))
                            {
                                _LastModified = AppendValue(_LastModified, value);
                            }
                            else
                            {
                                _bits |= 524288L;
                                _LastModified = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUL[0] & 16140865742145839071uL) == 5921481788798223169uL) && ((pUI[2] & 3755991007u) == 1162300993u) && ((pUB[12] & 223u) == 83u))) 
                        {
                            if (((_bits & 1048576L) != 0))
                            {
                                _AcceptRanges = AppendValue(_AcceptRanges, value);
                            }
                            else
                            {
                                _bits |= 1048576L;
                                _AcceptRanges = new StringValues(value);
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
                                _Connection = AppendValue(_Connection, value);
                            }
                            else
                            {
                                _bits |= 2L;
                                _Connection = new StringValues(value);
                                _rawConnection = null;
                            }
                            return;
                        }
                    
                        if ((((pUL[0] & 16131858680330051551uL) == 5281668125874799947uL) && ((pUS[4] & 57311u) == 17750u))) 
                        {
                            if (((_bits & 8L) != 0))
                            {
                                _KeepAlive = AppendValue(_KeepAlive, value);
                            }
                            else
                            {
                                _bits |= 8L;
                                _KeepAlive = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUL[0] & 16131858543427968991uL) == 5426643225946637651uL) && ((pUS[4] & 57311u) == 17737u))) 
                        {
                            if (((_bits & 134217728L) != 0))
                            {
                                _SetCookie = AppendValue(_SetCookie, value);
                            }
                            else
                            {
                                _bits |= 134217728L;
                                _SetCookie = new StringValues(value);
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
                                _Date = AppendValue(_Date, value);
                            }
                            else
                            {
                                _bits |= 4L;
                                _Date = new StringValues(value);
                                _rawDate = null;
                            }
                            return;
                        }
                    
                        if ((((pUI[0] & 3755991007u) == 1195463749u))) 
                        {
                            if (((_bits & 4194304L) != 0))
                            {
                                _ETag = AppendValue(_ETag, value);
                            }
                            else
                            {
                                _bits |= 4194304L;
                                _ETag = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUI[0] & 3755991007u) == 1498562902u))) 
                        {
                            if (((_bits & 268435456L) != 0))
                            {
                                _Vary = AppendValue(_Vary, value);
                            }
                            else
                            {
                                _bits |= 268435456L;
                                _Vary = new StringValues(value);
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
                                _Pragma = AppendValue(_Pragma, value);
                            }
                            else
                            {
                                _bits |= 16L;
                                _Pragma = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUI[0] & 3755991007u) == 1448232275u) && ((pUS[2] & 57311u) == 21061u))) 
                        {
                            if (((_bits & 67108864L) != 0))
                            {
                                _Server = AppendValue(_Server, value);
                            }
                            else
                            {
                                _bits |= 67108864L;
                                _Server = new StringValues(value);
                                _rawServer = null;
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
                                _Trailer = AppendValue(_Trailer, value);
                            }
                            else
                            {
                                _bits |= 32L;
                                _Trailer = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUI[0] & 3755991007u) == 1380405333u) && ((pUS[2] & 57311u) == 17473u) && ((pUB[6] & 223u) == 69u))) 
                        {
                            if (((_bits & 128L) != 0))
                            {
                                _Upgrade = AppendValue(_Upgrade, value);
                            }
                            else
                            {
                                _bits |= 128L;
                                _Upgrade = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUI[0] & 3755991007u) == 1314013527u) && ((pUS[2] & 57311u) == 20041u) && ((pUB[6] & 223u) == 71u))) 
                        {
                            if (((_bits & 512L) != 0))
                            {
                                _Warning = AppendValue(_Warning, value);
                            }
                            else
                            {
                                _bits |= 512L;
                                _Warning = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUI[0] & 3755991007u) == 1230002245u) && ((pUS[2] & 57311u) == 17746u) && ((pUB[6] & 223u) == 83u))) 
                        {
                            if (((_bits & 262144L) != 0))
                            {
                                _Expires = AppendValue(_Expires, value);
                            }
                            else
                            {
                                _bits |= 262144L;
                                _Expires = new StringValues(value);
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
                                _TransferEncoding = AppendValue(_TransferEncoding, value);
                            }
                            else
                            {
                                _bits |= 64L;
                                _TransferEncoding = new StringValues(value);
                                _rawTransferEncoding = null;
                            }
                            return;
                        }
                    
                        if ((((pUL[0] & 16131893727263186911uL) == 6143241228466999888uL) && ((pUL[1] & 16131858542891098079uL) == 6071207754897639508uL) && ((pUB[16] & 223u) == 69u))) 
                        {
                            if (((_bits & 16777216L) != 0))
                            {
                                _ProxyAutheticate = AppendValue(_ProxyAutheticate, value);
                            }
                            else
                            {
                                _bits |= 16777216L;
                                _ProxyAutheticate = new StringValues(value);
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
                                _Via = AppendValue(_Via, value);
                            }
                            else
                            {
                                _bits |= 256L;
                                _Via = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUS[0] & 57311u) == 18241u) && ((pUB[2] & 223u) == 69u))) 
                        {
                            if (((_bits & 2097152L) != 0))
                            {
                                _Age = AppendValue(_Age, value);
                            }
                            else
                            {
                                _bits |= 2097152L;
                                _Age = new StringValues(value);
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
                                _Allow = AppendValue(_Allow, value);
                            }
                            else
                            {
                                _bits |= 1024L;
                                _Allow = new StringValues(value);
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
                                _ContentLength = AppendValue(_ContentLength, value);
                            }
                            else
                            {
                                _bits |= 2048L;
                                _ContentLength = new StringValues(value);
                                _rawContentLength = null;
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
                                _ContentType = AppendValue(_ContentType, value);
                            }
                            else
                            {
                                _bits |= 4096L;
                                _ContentType = new StringValues(value);
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
                                _ContentEncoding = AppendValue(_ContentEncoding, value);
                            }
                            else
                            {
                                _bits |= 8192L;
                                _ContentEncoding = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUL[0] & 18437701552104792031uL) == 3266321689424580419uL) && ((pUL[1] & 16131858542891098079uL) == 4992030546487820620uL))) 
                        {
                            if (((_bits & 16384L) != 0))
                            {
                                _ContentLanguage = AppendValue(_ContentLanguage, value);
                            }
                            else
                            {
                                _bits |= 16384L;
                                _ContentLanguage = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUL[0] & 18437701552104792031uL) == 3266321689424580419uL) && ((pUL[1] & 16131858542891098079uL) == 5642809484339531596uL))) 
                        {
                            if (((_bits & 32768L) != 0))
                            {
                                _ContentLocation = AppendValue(_ContentLocation, value);
                            }
                            else
                            {
                                _bits |= 32768L;
                                _ContentLocation = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUL[0] & 16131858543427968991uL) == 5211884407196440407uL) && ((pUL[1] & 16131858542891098079uL) == 4995689643909598789uL))) 
                        {
                            if (((_bits & 536870912L) != 0))
                            {
                                _WWWAuthenticate = AppendValue(_WWWAuthenticate, value);
                            }
                            else
                            {
                                _bits |= 536870912L;
                                _WWWAuthenticate = new StringValues(value);
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
                                _ContentMD5 = AppendValue(_ContentMD5, value);
                            }
                            else
                            {
                                _bits |= 65536L;
                                _ContentMD5 = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUL[0] & 16131893727263186911uL) == 5062377317797741906uL) && ((pUS[4] & 57311u) == 17748u) && ((pUB[10] & 223u) == 82u))) 
                        {
                            if (((_bits & 33554432L) != 0))
                            {
                                _RetryAfter = AppendValue(_RetryAfter, value);
                            }
                            else
                            {
                                _bits |= 33554432L;
                                _RetryAfter = new StringValues(value);
                            }
                            return;
                        }
                    }
                    break;
            
                case 8:
                    {
                        if ((((pUL[0] & 16131858542891098079uL) == 5642809484339531596uL))) 
                        {
                            if (((_bits & 8388608L) != 0))
                            {
                                _Location = AppendValue(_Location, value);
                            }
                            else
                            {
                                _bits |= 8388608L;
                                _Location = new StringValues(value);
                            }
                            return;
                        }
                    }
                    break;
            
                case 32:
                    {
                        if ((((pUL[0] & 16140865742145839071uL) == 4840616791602578241uL) && ((pUL[1] & 16140865742145839071uL) == 4696493889984679503uL) && ((pUL[2] & 16131858680330051551uL) == 4995128798724705356uL) && ((pUL[3] & 16131858542891098079uL) == 6002244186580862276uL))) 
                        {
                            if (((_bits & 1073741824L) != 0))
                            {
                                _AccessControlAllowCredentials = AppendValue(_AccessControlAllowCredentials, value);
                            }
                            else
                            {
                                _bits |= 1073741824L;
                                _AccessControlAllowCredentials = new StringValues(value);
                            }
                            return;
                        }
                    }
                    break;
            
                case 28:
                    {
                        if ((((pUL[0] & 16140865742145839071uL) == 4840616791602578241uL) && ((pUL[1] & 16140865742145839071uL) == 4696493889984679503uL) && ((pUL[2] & 16131858680330051551uL) == 4703244745433893964uL) && ((pUI[6] & 3755991007u) == 1397900612u))) 
                        {
                            if (((_bits & 2147483648L) != 0))
                            {
                                _AccessControlAllowHeaders = AppendValue(_AccessControlAllowHeaders, value);
                            }
                            else
                            {
                                _bits |= 2147483648L;
                                _AccessControlAllowHeaders = new StringValues(value);
                            }
                            return;
                        }
                    
                        if ((((pUL[0] & 16140865742145839071uL) == 4840616791602578241uL) && ((pUL[1] & 16140865742145839071uL) == 4696493889984679503uL) && ((pUL[2] & 16131858680330051551uL) == 6072344529712663628uL) && ((pUI[6] & 3755991007u) == 1396985672u))) 
                        {
                            if (((_bits & 4294967296L) != 0))
                            {
                                _AccessControlAllowMethods = AppendValue(_AccessControlAllowMethods, value);
                            }
                            else
                            {
                                _bits |= 4294967296L;
                                _AccessControlAllowMethods = new StringValues(value);
                            }
                            return;
                        }
                    }
                    break;
            
                case 27:
                    {
                        if ((((pUL[0] & 16140865742145839071uL) == 4840616791602578241uL) && ((pUL[1] & 16140865742145839071uL) == 4696493889984679503uL) && ((pUL[2] & 16131858680330051551uL) == 5283372369015950412uL) && ((pUS[12] & 57311u) == 18759u) && ((pUB[26] & 223u) == 78u))) 
                        {
                            if (((_bits & 8589934592L) != 0))
                            {
                                _AccessControlAllowOrigin = AppendValue(_AccessControlAllowOrigin, value);
                            }
                            else
                            {
                                _bits |= 8589934592L;
                                _AccessControlAllowOrigin = new StringValues(value);
                            }
                            return;
                        }
                    }
                    break;
            
                case 29:
                    {
                        if ((((pUL[0] & 16140865742145839071uL) == 4840616791602578241uL) && ((pUL[1] & 16140865742145839071uL) == 4984724266136391247uL) && ((pUL[2] & 16131893727263186911uL) == 4992289962713895000uL) && ((pUI[6] & 3755991007u) == 1380271169u) && ((pUB[28] & 223u) == 83u))) 
                        {
                            if (((_bits & 17179869184L) != 0))
                            {
                                _AccessControlExposeHeaders = AppendValue(_AccessControlExposeHeaders, value);
                            }
                            else
                            {
                                _bits |= 17179869184L;
                                _AccessControlExposeHeaders = new StringValues(value);
                            }
                            return;
                        }
                    }
                    break;
            
                case 22:
                    {
                        if ((((pUL[0] & 16140865742145839071uL) == 4840616791602578241uL) && ((pUL[1] & 16140865742145839071uL) == 5561185018439814735uL) && ((pUI[4] & 3758088159u) == 1093490753u) && ((pUS[10] & 57311u) == 17735u))) 
                        {
                            if (((_bits & 34359738368L) != 0))
                            {
                                _AccessControlMaxAge = AppendValue(_AccessControlMaxAge, value);
                            }
                            else
                            {
                                _bits |= 34359738368L;
                                _AccessControlMaxAge = new StringValues(value);
                            }
                            return;
                        }
                    }
                    break;
            }}
            var key = System.Text.Encoding.ASCII.GetString(keyBytes, keyOffset, keyLength);
            StringValues existing;
            Unknown.TryGetValue(key, out existing);
            Unknown[key] = AppendValue(existing, value);
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
                        _current = new KeyValuePair<string, StringValues>("Cache-Control", _collection._CacheControl);
                        _state = 1;
                        return true;
                    }
                
                state1:
                    if (((_bits & 2L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Connection", _collection._Connection);
                        _state = 2;
                        return true;
                    }
                
                state2:
                    if (((_bits & 4L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Date", _collection._Date);
                        _state = 3;
                        return true;
                    }
                
                state3:
                    if (((_bits & 8L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Keep-Alive", _collection._KeepAlive);
                        _state = 4;
                        return true;
                    }
                
                state4:
                    if (((_bits & 16L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Pragma", _collection._Pragma);
                        _state = 5;
                        return true;
                    }
                
                state5:
                    if (((_bits & 32L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Trailer", _collection._Trailer);
                        _state = 6;
                        return true;
                    }
                
                state6:
                    if (((_bits & 64L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Transfer-Encoding", _collection._TransferEncoding);
                        _state = 7;
                        return true;
                    }
                
                state7:
                    if (((_bits & 128L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Upgrade", _collection._Upgrade);
                        _state = 8;
                        return true;
                    }
                
                state8:
                    if (((_bits & 256L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Via", _collection._Via);
                        _state = 9;
                        return true;
                    }
                
                state9:
                    if (((_bits & 512L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Warning", _collection._Warning);
                        _state = 10;
                        return true;
                    }
                
                state10:
                    if (((_bits & 1024L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Allow", _collection._Allow);
                        _state = 11;
                        return true;
                    }
                
                state11:
                    if (((_bits & 2048L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Length", _collection._ContentLength);
                        _state = 12;
                        return true;
                    }
                
                state12:
                    if (((_bits & 4096L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Type", _collection._ContentType);
                        _state = 13;
                        return true;
                    }
                
                state13:
                    if (((_bits & 8192L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Encoding", _collection._ContentEncoding);
                        _state = 14;
                        return true;
                    }
                
                state14:
                    if (((_bits & 16384L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Language", _collection._ContentLanguage);
                        _state = 15;
                        return true;
                    }
                
                state15:
                    if (((_bits & 32768L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Location", _collection._ContentLocation);
                        _state = 16;
                        return true;
                    }
                
                state16:
                    if (((_bits & 65536L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-MD5", _collection._ContentMD5);
                        _state = 17;
                        return true;
                    }
                
                state17:
                    if (((_bits & 131072L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Content-Range", _collection._ContentRange);
                        _state = 18;
                        return true;
                    }
                
                state18:
                    if (((_bits & 262144L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Expires", _collection._Expires);
                        _state = 19;
                        return true;
                    }
                
                state19:
                    if (((_bits & 524288L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Last-Modified", _collection._LastModified);
                        _state = 20;
                        return true;
                    }
                
                state20:
                    if (((_bits & 1048576L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Accept-Ranges", _collection._AcceptRanges);
                        _state = 21;
                        return true;
                    }
                
                state21:
                    if (((_bits & 2097152L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Age", _collection._Age);
                        _state = 22;
                        return true;
                    }
                
                state22:
                    if (((_bits & 4194304L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("ETag", _collection._ETag);
                        _state = 23;
                        return true;
                    }
                
                state23:
                    if (((_bits & 8388608L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Location", _collection._Location);
                        _state = 24;
                        return true;
                    }
                
                state24:
                    if (((_bits & 16777216L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Proxy-Autheticate", _collection._ProxyAutheticate);
                        _state = 25;
                        return true;
                    }
                
                state25:
                    if (((_bits & 33554432L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Retry-After", _collection._RetryAfter);
                        _state = 26;
                        return true;
                    }
                
                state26:
                    if (((_bits & 67108864L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Server", _collection._Server);
                        _state = 27;
                        return true;
                    }
                
                state27:
                    if (((_bits & 134217728L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Set-Cookie", _collection._SetCookie);
                        _state = 28;
                        return true;
                    }
                
                state28:
                    if (((_bits & 268435456L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Vary", _collection._Vary);
                        _state = 29;
                        return true;
                    }
                
                state29:
                    if (((_bits & 536870912L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("WWW-Authenticate", _collection._WWWAuthenticate);
                        _state = 30;
                        return true;
                    }
                
                state30:
                    if (((_bits & 1073741824L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Allow-Credentials", _collection._AccessControlAllowCredentials);
                        _state = 31;
                        return true;
                    }
                
                state31:
                    if (((_bits & 2147483648L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Allow-Headers", _collection._AccessControlAllowHeaders);
                        _state = 32;
                        return true;
                    }
                
                state32:
                    if (((_bits & 4294967296L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Allow-Methods", _collection._AccessControlAllowMethods);
                        _state = 33;
                        return true;
                    }
                
                state33:
                    if (((_bits & 8589934592L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Allow-Origin", _collection._AccessControlAllowOrigin);
                        _state = 34;
                        return true;
                    }
                
                state34:
                    if (((_bits & 17179869184L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Expose-Headers", _collection._AccessControlExposeHeaders);
                        _state = 35;
                        return true;
                    }
                
                state35:
                    if (((_bits & 34359738368L) != 0))
                    {
                        _current = new KeyValuePair<string, StringValues>("Access-Control-Max-Age", _collection._AccessControlMaxAge);
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
