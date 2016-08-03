// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Cors.Internal;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Cors
{
    /// <summary>
    /// A filter that applies the given <see cref="CorsPolicy"/> and adds appropriate response headers.
    /// </summary>
    public class CorsAuthorizationFilter : ICorsAuthorizationFilter
    {
        private ICorsService _corsService;
        private ICorsPolicyProvider _corsPolicyProvider;

        /// <summary>
        /// Creates a new instance of <see cref="CorsAuthorizationFilter"/>.
        /// </summary>
        /// <param name="corsService">The <see cref="ICorsService"/>.</param>
        /// <param name="policyProvider">The <see cref="ICorsPolicyProvider"/>.</param>
        public CorsAuthorizationFilter(ICorsService corsService, ICorsPolicyProvider policyProvider)
        {
            _corsService = corsService;
            _corsPolicyProvider = policyProvider;
        }

        /// <summary>
        /// The policy name used to fetch a <see cref="CorsPolicy"/>.
        /// </summary>
        public string PolicyName { get; set; }

        /// <inheritdoc />
        public int Order
        {
            get
            {
                // Since clients' preflight requests would not have data to authenticate requests, this
                // filter must run before any other authorization filters.
                return int.MinValue + 100;
            }
        }


        /// <inheritdoc />
        public async Task OnAuthorizationAsync(Filters.AuthorizationFilterContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // If this filter is not closest to the action, it is not applicable.
            if (!IsClosestToAction(context.Filters))
            {
                return;
            }

            var httpContext = context.HttpContext;
            var request = httpContext.Request;
            if (request.Headers.ContainsKey(CorsConstants.Origin))
            {
                var policy = await _corsPolicyProvider.GetPolicyAsync(httpContext, PolicyName);

                if (policy == null)
                {
                    throw new InvalidOperationException(
                        Resources.FormatCorsAuthorizationFilter_MissingCorsPolicy(PolicyName));
                }

                var result = _corsService.EvaluatePolicy(context.HttpContext, policy);
                _corsService.ApplyResult(result, context.HttpContext.Response);

                var accessControlRequestMethod =
                        httpContext.Request.Headers[CorsConstants.AccessControlRequestMethod];
                if (string.Equals(
                        request.Method,
                        CorsConstants.PreflightHttpMethod,
                        StringComparison.OrdinalIgnoreCase) &&
                    !StringValues.IsNullOrEmpty(accessControlRequestMethod))
                {
                    // If this was a preflight, there is no need to run anything else.
                    // Also the response is always 200 so that anyone after mvc can handle the pre flight request.
                    context.Result = new StatusCodeResult(StatusCodes.Status200OK);
                }

                // Continue with other filters and action.
            }
        }

        private bool IsClosestToAction(IEnumerable<IFilterMetadata> filters)
        {
            // If there are multiple ICorsAuthorizationFilter that are defined at the class and
            // at the action level, the one closest to the action overrides the others.
            // Since filterdescriptor collection is ordered (the last filter is the one closest to the action),
            // we apply this constraint only if there is no ICorsAuthorizationFilter after this.
            return filters.Last(filter => filter is ICorsAuthorizationFilter) == this;
        }
    }
}
