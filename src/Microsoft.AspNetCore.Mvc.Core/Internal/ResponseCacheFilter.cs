// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.ResponseCaching;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    /// <summary>
    /// An <see cref="IActionFilter"/> which sets the appropriate headers related to response caching.
    /// </summary>
    public class ResponseCacheFilter : IResponseCacheFilter
    {
        private readonly CacheProfile _cacheProfile;
        private int? _cacheDuration;
        private ResponseCacheLocation? _cacheLocation;
        private bool? _cacheNoStore;
        private string _cacheVaryByHeader;
        private string[] _cacheVaryByQueryKeys;

        /// <summary>
        /// Creates a new instance of <see cref="ResponseCacheFilter"/>
        /// </summary>
        /// <param name="cacheProfile">The profile which contains the settings for
        /// <see cref="ResponseCacheFilter"/>.</param>
        public ResponseCacheFilter(CacheProfile cacheProfile)
        {
            _cacheProfile = cacheProfile;
        }

        /// <summary>
        /// Gets or sets the duration in seconds for which the response is cached.
        /// This is a required parameter.
        /// This sets "max-age" in "Cache-control" header.
        /// </summary>
        public int Duration
        {
            get { return (_cacheDuration ?? _cacheProfile.Duration) ?? 0; }
            set { _cacheDuration = value; }
        }

        /// <summary>
        /// Gets or sets the location where the data from a particular URL must be cached.
        /// </summary>
        public ResponseCacheLocation Location
        {
            get { return (_cacheLocation ?? _cacheProfile.Location) ?? ResponseCacheLocation.Any; }
            set { _cacheLocation = value; }
        }

        /// <summary>
        /// Gets or sets the value which determines whether the data should be stored or not.
        /// When set to <see langword="true"/>, it sets "Cache-control" header to "no-store".
        /// Ignores the "Location" parameter for values other than "None".
        /// Ignores the "duration" parameter.
        /// </summary>
        public bool NoStore
        {
            get { return (_cacheNoStore ?? _cacheProfile.NoStore) ?? false; }
            set { _cacheNoStore = value; }
        }

        /// <summary>
        /// Gets or sets the value for the Vary response header.
        /// </summary>
        public string VaryByHeader
        {
            get { return _cacheVaryByHeader ?? _cacheProfile.VaryByHeader; }
            set { _cacheVaryByHeader = value; }
        }

        /// <summary>
        /// Gets or sets the query keys to vary by.
        /// </summary>
        /// <remarks>
        /// <see cref="VaryByQueryKeys"/> requires the response cache middleware.
        /// </remarks>
        public string[] VaryByQueryKeys
        {
            get { return _cacheVaryByQueryKeys ?? _cacheProfile.VaryByQueryKeys; }
            set { _cacheVaryByQueryKeys = value; }
        }

        /// <inheritdoc />
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // If there are more filters which can override the values written by this filter,
            // then skip execution of this filter.
            if (IsOverridden(context))
            {
                return;
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
                    throw new InvalidOperationException(Resources.FormatVaryByQueryKeys_Requires_ResponseCachingMiddleware(nameof(VaryByQueryKeys)));
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
                string cacheControlValue = null;
                switch (Location)
                {
                    case ResponseCacheLocation.Any:
                        cacheControlValue = "public";
                        break;
                    case ResponseCacheLocation.Client:
                        cacheControlValue = "private";
                        break;
                    case ResponseCacheLocation.None:
                        cacheControlValue = "no-cache";
                        headers[HeaderNames.Pragma] = "no-cache";
                        break;
                }

                cacheControlValue = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}{1}max-age={2}",
                    cacheControlValue,
                    cacheControlValue != null ? "," : null,
                    Duration);

                if (cacheControlValue != null)
                {
                    headers[HeaderNames.CacheControl] = cacheControlValue;
                }
            }
        }

        /// <inheritdoc />
        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        // internal for Unit Testing purposes.
        internal bool IsOverridden(ActionExecutingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Return true if there are any filters which are after the current filter. In which case the current
            // filter should be skipped.
            return context.Filters.OfType<IResponseCacheFilter>().Last() != this;
        }
    }
}