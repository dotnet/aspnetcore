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

        internal ResponseCacheContext(
            HttpContext httpContext)
        {
            HttpContext = httpContext;
        }

        public HttpContext HttpContext { get; }

        public bool ShouldCacheResponse { get; internal set; }

        public string BaseKey { get; internal set; }

        public string StorageVaryKey { get; internal set; }

        public DateTimeOffset ResponseTime { get; internal set; }

        public TimeSpan CachedEntryAge { get; internal set; }

        public TimeSpan CachedResponseValidFor { get; internal set; }

        public CachedResponse CachedResponse { get; internal set; }

        public CachedVaryByRules CachedVaryByRules { get; internal set; }

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
    }
}
