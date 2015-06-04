// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Cors.Core;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.WebUtilities;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// A filter which applies the given <see cref="CorsPolicy"/> and adds appropriate response headers.
    /// </summary>
    public class CorsAuthorizationFilter : ICorsAuthorizationFilter
    {
        private ICorsService _corsService;
        private ICorsPolicyProvider _corsPolicyProvider;

        /// <summary>
        /// Creates a new instace of <see cref="CorsAuthorizationFilter"/>.
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
                return DefaultOrder.DefaultCorsSortOrder;
            }
        }


        /// <inheritdoc />
        public async Task OnAuthorizationAsync([NotNull] AuthorizationContext context)
        {
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
                var result = _corsService.EvaluatePolicy(context.HttpContext, policy);
                _corsService.ApplyResult(result, context.HttpContext.Response);

                var accessControlRequestMethod = 
                        httpContext.Request.Headers.Get(CorsConstants.AccessControlRequestMethod);
                if (string.Equals(
                        request.Method,
                        CorsConstants.PreflightHttpMethod,
                        StringComparison.Ordinal) &&
                    accessControlRequestMethod != null)
                {
                    // If this was a preflight, there is no need to run anything else.
                    // Also the response is always 200 so that anyone after mvc can handle the pre flight request.
                    context.Result = new HttpStatusCodeResult(StatusCodes.Status200OK);
                    await Task.FromResult(true);
                }

                // Continue with other filters and action.
            }
        }

        private bool IsClosestToAction(IEnumerable<IFilter> filters)
        {
            // If there are multiple ICorsAuthorizationFilter which are defined at the class and
            // at the action level, the one closest to the action overrides the others. 
            // Since filterdescriptor collection is ordered (the last filter is the one closest to the action),
            // we apply this constraint only if there is no ICorsAuthorizationFilter after this.
            return filters.Last(filter => filter is ICorsAuthorizationFilter) == this;
        }
    }
}
