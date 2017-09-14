// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.ResponseCaching;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ResponseCacheFilterExecutor
    {
        private readonly CacheProfile _cacheProfile;
        private int? _cacheDuration;
        private ResponseCacheLocation? _cacheLocation;
        private bool? _cacheNoStore;
        private string _cacheVaryByHeader;
        private string[] _cacheVaryByQueryKeys;

        public ResponseCacheFilterExecutor(CacheProfile cacheProfile)
        {
            _cacheProfile = cacheProfile ?? throw new ArgumentNullException(nameof(cacheProfile));
        }

        public int Duration
        {
            get => _cacheDuration ?? _cacheProfile.Duration ?? 0;
            set => _cacheDuration = value;
        }

        public ResponseCacheLocation Location
        {
            get => _cacheLocation ?? _cacheProfile.Location ?? ResponseCacheLocation.Any;
            set => _cacheLocation = value;
        }

        public bool NoStore
        {
            get => _cacheNoStore ?? _cacheProfile.NoStore ?? false;
            set => _cacheNoStore = value;
        }

        public string VaryByHeader
        {
            get => _cacheVaryByHeader ?? _cacheProfile.VaryByHeader;
            set => _cacheVaryByHeader = value;
        }

        public string[] VaryByQueryKeys
        {
            get => _cacheVaryByQueryKeys ?? _cacheProfile.VaryByQueryKeys;
            set => _cacheVaryByQueryKeys = value;
        }

        public void Execute(FilterContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!NoStore)
            {
                // Duration MUST be set (either in the cache profile or in this filter) unless NoStore is true.
                if (_cacheProfile.Duration == null && _cacheDuration == null)
                {
                    throw new InvalidOperationException(
                        Resources.FormatResponseCache_SpecifyDuration(nameof(NoStore), nameof(Duration)));
                }
            }

            var headers = context.HttpContext.Response.Headers;

            // Clear all headers
            headers.Remove(HeaderNames.Vary);
            headers.Remove(HeaderNames.CacheControl);
            headers.Remove(HeaderNames.Pragma);

            if (!string.IsNullOrEmpty(VaryByHeader))
            {
                headers[HeaderNames.Vary] = VaryByHeader;
            }

            if (VaryByQueryKeys != null)
            {
                var responseCachingFeature = context.HttpContext.Features.Get<IResponseCachingFeature>();
                if (responseCachingFeature == null)
                {
                    throw new InvalidOperationException(
                        Resources.FormatVaryByQueryKeys_Requires_ResponseCachingMiddleware(nameof(VaryByQueryKeys)));
                }
                responseCachingFeature.VaryByQueryKeys = VaryByQueryKeys;
            }

            if (NoStore)
            {
                headers[HeaderNames.CacheControl] = "no-store";

                // Cache-control: no-store, no-cache is valid.
                if (Location == ResponseCacheLocation.None)
                {
                    headers.AppendCommaSeparatedValues(HeaderNames.CacheControl, "no-cache");
                    headers[HeaderNames.Pragma] = "no-cache";
                }
            }
            else
            {
                string cacheControlValue;
                switch (Location)
                {
                    case ResponseCacheLocation.Any:
                        cacheControlValue = "public,";
                        break;
                    case ResponseCacheLocation.Client:
                        cacheControlValue = "private,";
                        break;
                    case ResponseCacheLocation.None:
                        cacheControlValue = "no-cache,";
                        headers[HeaderNames.Pragma] = "no-cache";
                        break;
                    default:
                        cacheControlValue = null;
                        break;
                }

                cacheControlValue = $"{cacheControlValue}max-age={Duration}";
                headers[HeaderNames.CacheControl] = cacheControlValue;
            }
        }
    }
}
