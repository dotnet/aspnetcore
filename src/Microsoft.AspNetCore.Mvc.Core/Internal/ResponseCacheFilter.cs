// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    /// <summary>
    /// An <see cref="IActionFilter"/> which sets the appropriate headers related to response caching.
    /// </summary>
    public class ResponseCacheFilter : IActionFilter, IResponseCacheFilter
    {
        private readonly ResponseCacheFilterExecutor _executor;

        /// <summary>
        /// Creates a new instance of <see cref="ResponseCacheFilter"/>
        /// </summary>
        /// <param name="cacheProfile">The profile which contains the settings for
        /// <see cref="ResponseCacheFilter"/>.</param>
        public ResponseCacheFilter(CacheProfile cacheProfile)
        {
            _executor = new ResponseCacheFilterExecutor(cacheProfile);
        }

        /// <summary>
        /// Gets or sets the duration in seconds for which the response is cached.
        /// This is a required parameter.
        /// This sets "max-age" in "Cache-control" header.
        /// </summary>
        public int Duration
        {
            get => _executor.Duration;
            set => _executor.Duration = value;
        }

        /// <summary>
        /// Gets or sets the location where the data from a particular URL must be cached.
        /// </summary>
        public ResponseCacheLocation Location
        {
            get => _executor.Location;
            set => _executor.Location = value;
        }

        /// <summary>
        /// Gets or sets the value which determines whether the data should be stored or not.
        /// When set to <see langword="true"/>, it sets "Cache-control" header to "no-store".
        /// Ignores the "Location" parameter for values other than "None".
        /// Ignores the "duration" parameter.
        /// </summary>
        public bool NoStore
        {
            get => _executor.NoStore;
            set => _executor.NoStore = value;
        }

        /// <summary>
        /// Gets or sets the value for the Vary response header.
        /// </summary>
        public string VaryByHeader
        {
            get => _executor.VaryByHeader;
            set => _executor.VaryByHeader = value;
        }

        /// <summary>
        /// Gets or sets the query keys to vary by.
        /// </summary>
        /// <remarks>
        /// <see cref="VaryByQueryKeys"/> requires the response cache middleware.
        /// </remarks>
        public string[] VaryByQueryKeys
        {
            get => _executor.VaryByQueryKeys;
            set => _executor.VaryByQueryKeys = value;
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
            if (!context.IsEffectivePolicy<IResponseCacheFilter>(this))
            {
                return;
            }

            _executor.Execute(context);
        }

        /// <inheritdoc />
        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}