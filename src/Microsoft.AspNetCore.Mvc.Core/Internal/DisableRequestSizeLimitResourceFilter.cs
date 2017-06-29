// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    /// <summary>
    /// A filter that sets <see cref="IHttpMaxRequestBodySizeFeature.MaxRequestBodySize"/>
    /// to <c>null</c>.
    /// </summary>
    public class DisableRequestSizeLimitResourceFilter : IResourceFilter, IRequestSizePolicy
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new instance of <see cref="DisableRequestSizeLimitResourceFilter"/>.
        /// </summary>
        public DisableRequestSizeLimitResourceFilter(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DisableRequestSizeLimitResourceFilter>();
        }

        /// <inheritdoc />
        public int Order { get; set; }

        /// <inheritdoc />
        public void OnResourceExecuted(ResourceExecutedContext context)
        {
        }

        /// <summary>
        /// Sets the <see cref="IHttpMaxRequestBodySizeFeature.MaxRequestBodySize"/>
        /// to <c>null</c>.
        /// </summary>
        /// <param name="context">The <see cref="ResourceExecutingContext"/>.</param>
        /// <remarks>If <see cref="IHttpMaxRequestBodySizeFeature"/> is not enabled or is read-only, 
        /// the <see cref="DisableRequestSizeLimitAttribute"/> is not applied.</remarks> 
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (IsClosestRequestSizePolicy(context.Filters))
            {
                var maxRequestBodySizeFeature = context.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();

                if (maxRequestBodySizeFeature == null)
                {
                    _logger.FeatureNotFound();
                }
                else if (maxRequestBodySizeFeature.IsReadOnly)
                {
                    _logger.FeatureIsReadOnly();
                }
                else
                {
                    maxRequestBodySizeFeature.MaxRequestBodySize = null;
                    _logger.RequestBodySizeLimitDisabled();
                }
            }
        }

        private bool IsClosestRequestSizePolicy(IList<IFilterMetadata> filters)
        {
            // Determine if this instance is the 'effective' request size policy.
            for (var i = filters.Count - 1; i >= 0; i--)
            {
                var filter = filters[i];
                if (filter is IRequestSizePolicy)
                {
                    return ReferenceEquals(this, filter);
                }
            }

            Debug.Fail("The current instance should be in the list of filters.");
            return false;
        }
    }
}
