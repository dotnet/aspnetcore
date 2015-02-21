// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// An <see cref="ActionFilterAttribute"/> which sets the appropriate headers related to response caching.
    /// </summary>
    public class ResponseCacheFilter : IActionFilter, IResponseCacheFilter
    {
        /// <summary>
        /// Creates a new instance of <see cref="ResponseCacheFilter"/>
        /// </summary>
        /// <param name="cacheProfile">The profile which contains the settings for
        /// <see cref="ResponseCacheFilter"/>.</param>
        public ResponseCacheFilter(CacheProfile cacheProfile)
        {
            if (!(cacheProfile.NoStore ?? false))
            {
                // Duration MUST be set (either in the cache profile or in the attribute) unless NoStore is true.
                if (cacheProfile.Duration == null)
                {
                    throw new InvalidOperationException(
                            Resources.FormatResponseCache_SpecifyDuration(nameof(NoStore), nameof(Duration)));
                }
            }

            Duration = cacheProfile.Duration ?? 0;
            Location = cacheProfile.Location ?? ResponseCacheLocation.Any;
            NoStore = cacheProfile.NoStore ?? false;
            VaryByHeader = cacheProfile.VaryByHeader;
        }

        /// <summary>
        /// Gets or sets the duration in seconds for which the response is cached.
        /// This is a required parameter.
        /// This sets "max-age" in "Cache-control" header.
        /// </summary>
        public int Duration { get; set; }

        /// <summary>
        /// Gets or sets the location where the data from a particular URL must be cached.
        /// </summary>
        public ResponseCacheLocation Location { get; set; }

        /// <summary>
        /// Gets or sets the value which determines whether the data should be stored or not.
        /// When set to <see langword="true"/>, it sets "Cache-control" header to "no-store".
        /// Ignores the "Location" parameter for values other than "None".
        /// Ignores the "duration" parameter.
        /// </summary>
        public bool NoStore { get; set; }

        /// <summary>
        /// Gets or sets the value for the Vary response header.
        /// </summary>
        public string VaryByHeader { get; set; }
        
        // <inheritdoc />
        public void OnActionExecuting([NotNull] ActionExecutingContext context)
        {
            // If there are more filters which can override the values written by this filter,
            // then skip execution of this filter.
            if (IsOverridden(context))
            {
                return;
            }

            var headers = context.HttpContext.Response.Headers;

            // Clear all headers
            headers.Remove("Vary");
            headers.Remove("Cache-control");
            headers.Remove("Pragma");

            if (!string.IsNullOrEmpty(VaryByHeader))
            {
                headers.Set("Vary", VaryByHeader);
            }

            if (NoStore)
            {
                headers.Set("Cache-control", "no-store");

                // Cache-control: no-store, no-cache is valid.
                if (Location == ResponseCacheLocation.None)
                {
                    headers.Append("Cache-control", "no-cache");
                    headers.Set("Pragma", "no-cache");
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
                        headers.Set("Pragma", "no-cache");
                        break;
                }

                cacheControlValue = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}{1}max-age={2}",
                    cacheControlValue,
                    cacheControlValue != null? "," : null,
                    Duration);

                if (cacheControlValue != null)
                {
                    headers.Set("Cache-control", cacheControlValue);
                }
            }
        }

        // <inheritdoc />
        public void OnActionExecuted([NotNull]ActionExecutedContext context)
        {
        }

        // internal for Unit Testing purposes.
        internal bool IsOverridden([NotNull] ActionExecutingContext context)
        {
            // Return true if there are any filters which are after the current filter. In which case the current
            // filter should be skipped.
            return context.Filters.OfType<IResponseCacheFilter>().Last() != this;
        }
    }
}