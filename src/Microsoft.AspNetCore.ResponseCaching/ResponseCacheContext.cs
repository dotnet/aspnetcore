// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.ResponseCaching.Internal;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.ResponseCaching
{
    public class ResponseCacheContext
    {
        private static readonly CacheControlHeaderValue EmptyCacheControl = new CacheControlHeaderValue();

        private RequestHeaders _requestHeaders;
        private ResponseHeaders _responseHeaders;
        private CacheControlHeaderValue _requestCacheControl;
        private CacheControlHeaderValue _responseCacheControl;
        private DateTimeOffset? _responseDate;
        private bool _parsedResponseDate;
        private DateTimeOffset? _responseExpires;
        private bool _parsedResponseExpires;

        internal ResponseCacheContext(
            HttpContext httpContext)
        {
            HttpContext = httpContext;
        }

        public HttpContext HttpContext { get; }

        public DateTimeOffset? ResponseTime { get; internal set; }

        public TimeSpan? CachedEntryAge { get; internal set; }

        public CachedVaryByRules CachedVaryByRules { get; internal set; }

        internal bool ShouldCacheResponse { get;  set; }

        internal string BaseKey { get;  set; }

        internal string StorageVaryKey { get;  set; }

        internal TimeSpan CachedResponseValidFor { get;  set; }

        internal CachedResponse CachedResponse { get;  set; }

        internal bool ResponseStarted { get; set; }

        internal Stream OriginalResponseStream { get; set; }

        internal ResponseCacheStream ResponseCacheStream { get; set; }

        internal IHttpSendFileFeature OriginalSendFileFeature { get; set; }

        internal ResponseHeaders CachedResponseHeaders { get; set; }

        internal RequestHeaders TypedRequestHeaders
        {
            get
            {
                if (_requestHeaders == null)
                {
                    _requestHeaders = HttpContext.Request.GetTypedHeaders();
                }
                return _requestHeaders;
            }
        }

        internal ResponseHeaders TypedResponseHeaders
        {
            get
            {
                if (_responseHeaders == null)
                {
                    _responseHeaders = HttpContext.Response.GetTypedHeaders();
                }
                return _responseHeaders;
            }
        }

        internal CacheControlHeaderValue RequestCacheControlHeaderValue
        {
            get
            {
                if (_requestCacheControl == null)
                {
                    _requestCacheControl = TypedRequestHeaders.CacheControl ?? EmptyCacheControl;
                }
                return _requestCacheControl;
            }
        }

        internal CacheControlHeaderValue ResponseCacheControlHeaderValue
        {
            get
            {
                if (_responseCacheControl == null)
                {
                    _responseCacheControl = TypedResponseHeaders.CacheControl ?? EmptyCacheControl;
                }
                return _responseCacheControl;
            }
        }

        internal DateTimeOffset? ResponseDate
        {
            get
            {
                if (!_parsedResponseDate)
                {
                    _parsedResponseDate = true;
                    _responseDate = TypedResponseHeaders.Date;
                }
                return _responseDate;
            }
            set
            {
                // Don't reparse the response date again if it's explicitly set
                _parsedResponseDate = true;
                _responseDate = value;
            }
        }

        internal DateTimeOffset? ResponseExpires
        {
            get
            {
                if (!_parsedResponseExpires)
                {
                    _parsedResponseExpires = true;
                    _responseExpires = TypedResponseHeaders.Expires;
                }
                return _responseExpires;
            }
        }
    }
}
