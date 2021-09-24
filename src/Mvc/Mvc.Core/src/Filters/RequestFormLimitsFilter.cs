// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        public FormOptions FormOptions { get; set; } = default!;

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
