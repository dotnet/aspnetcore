// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// A filter that sets <see cref="IHttpMaxRequestBodySizeFeature.MaxRequestBodySize"/>
    /// to <c>null</c>.
    /// </summary>
    internal class DisableRequestSizeLimitFilter : IAuthorizationFilter, IRequestSizePolicy
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new instance of <see cref="DisableRequestSizeLimitFilter"/>.
        /// </summary>
        public DisableRequestSizeLimitFilter(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DisableRequestSizeLimitFilter>();
        }

        /// <summary>
        /// Sets the <see cref="IHttpMaxRequestBodySizeFeature.MaxRequestBodySize"/>
        /// to <c>null</c>.
        /// </summary>
        /// <param name="context">The <see cref="AuthorizationFilterContext"/>.</param>
        /// <remarks>If <see cref="IHttpMaxRequestBodySizeFeature"/> is not enabled or is read-only,
        /// the <see cref="DisableRequestSizeLimitAttribute"/> is not applied.</remarks>
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var effectivePolicy = context.FindEffectivePolicy<IRequestSizePolicy>();
            if (effectivePolicy != null && effectivePolicy != this)
            {
                _logger.NotMostEffectiveFilter(GetType(), effectivePolicy.GetType(), typeof(IRequestSizePolicy));
                return;
            }

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
}
