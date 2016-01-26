// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    public class ValidateAntiforgeryTokenAuthorizationFilter : IAsyncAuthorizationFilter, IAntiforgeryPolicy
    {
        private readonly IAntiforgery _antiforgery;
        private readonly ILogger _logger;

        public ValidateAntiforgeryTokenAuthorizationFilter(IAntiforgery antiforgery, ILoggerFactory loggerFactory)
        {
            if (antiforgery == null)
            {
                throw new ArgumentNullException(nameof(antiforgery));
            }

            _antiforgery = antiforgery;
            _logger = loggerFactory.CreateLogger<ValidateAntiforgeryTokenAuthorizationFilter>();
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (IsClosestAntiforgeryPolicy(context.Filters) && ShouldValidate(context))
            {
                try
                {
                    await _antiforgery.ValidateRequestAsync(context.HttpContext);
                }
                catch (AntiforgeryValidationException exception)
                {
                    _logger.AntiforgeryTokenInvalid(exception.Message, exception);
                    context.Result = new BadRequestResult();
                }
            }
        }

        protected virtual bool ShouldValidate(AuthorizationFilterContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return true;
        }

        private bool IsClosestAntiforgeryPolicy(IList<IFilterMetadata> filters)
        {
            // Determine if this instance is the 'effective' antiforgery policy.
            for (var i = filters.Count - 1; i >= 0; i--)
            {
                var filter = filters[i];
                if (filter is IAntiforgeryPolicy)
                {
                    return object.ReferenceEquals(this, filter);
                }
            }

            Debug.Fail("The current instance should be in the list of filters.");
            return false;
        }
    }
}