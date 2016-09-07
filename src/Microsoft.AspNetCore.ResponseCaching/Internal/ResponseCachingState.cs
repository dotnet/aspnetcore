// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.ResponseCaching.Internal
{
    public class ResponseCachingState
    {
        private static readonly CacheControlHeaderValue EmptyCacheControl = new CacheControlHeaderValue();

        private readonly HttpContext _httpContext;

        private RequestHeaders _requestHeaders;
        private ResponseHeaders _responseHeaders;
        private CacheControlHeaderValue _requestCacheControl;
        private CacheControlHeaderValue _responseCacheControl;

        internal ResponseCachingState(HttpContext httpContext)
        {
            _httpContext = httpContext;
        }

        public bool ShouldCacheResponse { get; internal set; }

        public string BaseKey { get; internal set; }

        public string VaryKey { get; internal set; }

        public DateTimeOffset ResponseTime { get; internal set; }

        public TimeSpan CachedEntryAge { get; internal set; }

        public TimeSpan CachedResponseValidFor { get; internal set; }

        internal CachedResponse CachedResponse { get;  set; }

        internal CachedVaryRules CachedVaryRules { get; set; }

        public RequestHeaders RequestHeaders
        {
            get
            {
                if (_requestHeaders == null)
                {
                    _requestHeaders = _httpContext.Request.GetTypedHeaders();
                }
                return _requestHeaders;
            }
        }

        public ResponseHeaders ResponseHeaders
        {
            get
            {
                if (_responseHeaders == null)
                {
                    _responseHeaders = _httpContext.Response.GetTypedHeaders();
                }
                return _responseHeaders;
            }
        }

        public CacheControlHeaderValue RequestCacheControl
        {
            get
            {
                if (_requestCacheControl == null)
                {
                    _requestCacheControl = RequestHeaders.CacheControl ?? EmptyCacheControl;
                }
                return _requestCacheControl;
            }
        }

        public CacheControlHeaderValue ResponseCacheControl
        {
            get
            {
                if (_responseCacheControl == null)
                {
                    _responseCacheControl = ResponseHeaders.CacheControl ?? EmptyCacheControl;
                }
                return _responseCacheControl;
            }
        }
    }
}
