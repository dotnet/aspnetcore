// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// A filter that configures <see cref="FormOptions"/> for the current request.
    /// </summary>
    internal class RequestFormLimitsFilter : IAuthorizationFilter, IRequestFormLimitsPolicy
    {
        private readonly ILogger _logger;

        public RequestFormLimitsFilter(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<RequestFormLimitsFilter>();
        }

        public FormOptions FormOptions { get; set; }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var effectivePolicy = context.FindEffectivePolicy<IRequestFormLimitsPolicy>();
            if (effectivePolicy != null && effectivePolicy != this)
            {
                _logger.NotMostEffectiveFilter(GetType(), effectivePolicy.GetType(), typeof(IRequestFormLimitsPolicy));
                return;
            }

            var features = context.HttpContext.Features;
            var formFeature = features.Get<IFormFeature>();

            if (formFeature == null || formFeature.Form == null)
            {
                // Request form has not been read yet, so set the limits
                features.Set<IFormFeature>(new FormFeature(context.HttpContext.Request, FormOptions));
                _logger.AppliedRequestFormLimits();
            }
            else
            {
                _logger.CannotApplyRequestFormLimits();
            }
        }
    }
}
