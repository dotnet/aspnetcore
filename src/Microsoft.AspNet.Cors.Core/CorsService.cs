// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.Primitives;

namespace Microsoft.AspNet.Cors.Core
{
    /// <summary>
    /// Default implementation of <see cref="ICorsService"/>.
    /// </summary>
    public class CorsService : ICorsService
    {
        private readonly CorsOptions _options;

        /// <summary>
        /// Creates a new instance of the <see cref="CorsService"/>.
        /// </summary>
        /// <param name="options">The option model representing <see cref="CorsOptions"/>.</param>
        public CorsService([NotNull] IOptions<CorsOptions> options)
        {
            _options = options.Value;
        }

        /// <summary>
        /// Looks up a policy using the <paramref name="policyName"/> and then evaluates the policy using the passed in
        /// <paramref name="context"/>.
        /// </summary>
        /// <param name="requestContext"></param>
        /// <param name="policyName"></param>
        /// <returns>A <see cref="CorsResult"/> which contains the result of policy evaluation and can be
        /// used by the caller to set appropriate response headers.</returns>
        public CorsResult EvaluatePolicy([NotNull] HttpContext context, string policyName)
        {
            var policy = _options.GetPolicy(policyName);
            return EvaluatePolicy(context, policy);
        }

        /// <inheritdoc />
        public CorsResult EvaluatePolicy([NotNull] HttpContext context, [NotNull] CorsPolicy policy)
        {
            var corsResult = new CorsResult();
            var accessControlRequestMethod = context.Request.Headers[CorsConstants.AccessControlRequestMethod];
            if (string.Equals(context.Request.Method, CorsConstants.PreflightHttpMethod, StringComparison.Ordinal) &&
                !StringValues.IsNullOrEmpty(accessControlRequestMethod))
            {
                EvaluatePreflightRequest(context, policy, corsResult);
            }
            else
            {
                EvaluateRequest(context, policy, corsResult);
            }

            return corsResult;
        }

        public virtual void EvaluateRequest(HttpContext context, CorsPolicy policy, CorsResult result)
        {
            var origin = context.Request.Headers[CorsConstants.Origin];
            if (StringValues.IsNullOrEmpty(origin) || !policy.AllowAnyOrigin && !policy.Origins.Contains(origin))
            {
                return;
            }

            AddOriginToResult(origin, policy, result);
            result.SupportsCredentials = policy.SupportsCredentials;
            AddHeaderValues(result.AllowedExposedHeaders, policy.ExposedHeaders);
        }

        public virtual void EvaluatePreflightRequest(HttpContext context, CorsPolicy policy, CorsResult result)
        {
            var origin = context.Request.Headers[CorsConstants.Origin];
            if (StringValues.IsNullOrEmpty(origin) || !policy.AllowAnyOrigin && !policy.Origins.Contains(origin))
            {
                return;
            }

            var accessControlRequestMethod = context.Request.Headers[CorsConstants.AccessControlRequestMethod];
            if (StringValues.IsNullOrEmpty(accessControlRequestMethod))
            {
                return;
            }

            var requestHeaders =
                context.Request.Headers.GetCommaSeparatedValues(CorsConstants.AccessControlRequestHeaders);

            if (!policy.AllowAnyMethod && !policy.Methods.Contains(accessControlRequestMethod))
            {
                return;
            }

            if (!policy.AllowAnyHeader &&
                requestHeaders != null &&
                !requestHeaders.All(header => CorsConstants.SimpleRequestHeaders.Contains(header, StringComparer.OrdinalIgnoreCase) ||
                                              policy.Headers.Contains(header, StringComparer.OrdinalIgnoreCase)))
            {
                return;
            }

            AddOriginToResult(origin, policy, result);
            result.SupportsCredentials = policy.SupportsCredentials;
            result.PreflightMaxAge = policy.PreflightMaxAge;
            result.AllowedMethods.Add(accessControlRequestMethod);
            AddHeaderValues(result.AllowedHeaders, requestHeaders);
        }

        /// <inheritdoc />
        public virtual void ApplyResult(CorsResult result, HttpResponse response)
        {
            var headers = response.Headers;

            if (result.AllowedOrigin != null)
            {
                headers[CorsConstants.AccessControlAllowOrigin] = result.AllowedOrigin;
            }

            if (result.VaryByOrigin)
            {
                headers["Vary"] = "Origin";
            }

            if (result.SupportsCredentials)
            {
                headers[CorsConstants.AccessControlAllowCredentials] = "true";
            }

            if (result.AllowedMethods.Count > 0)
            {
                // Filter out simple methods
                var nonSimpleAllowMethods = result.AllowedMethods
                    .Where(m =>
                        !CorsConstants.SimpleMethods.Contains(m, StringComparer.OrdinalIgnoreCase))
                    .ToArray();

                if (nonSimpleAllowMethods.Length > 0)
                {
                    headers.SetCommaSeparatedValues(
                        CorsConstants.AccessControlAllowMethods,
                        nonSimpleAllowMethods);
                }
            }

            if (result.AllowedHeaders.Count > 0)
            {
                // Filter out simple request headers
                var nonSimpleAllowRequestHeaders = result.AllowedHeaders
                    .Where(header =>
                        !CorsConstants.SimpleRequestHeaders.Contains(header, StringComparer.OrdinalIgnoreCase))
                    .ToArray();

                if (nonSimpleAllowRequestHeaders.Length > 0)
                {
                    headers.SetCommaSeparatedValues(
                        CorsConstants.AccessControlAllowHeaders,
                        nonSimpleAllowRequestHeaders);
                }
            }

            if (result.AllowedExposedHeaders.Count > 0)
            {
                // Filter out simple response headers
                var nonSimpleAllowResponseHeaders = result.AllowedExposedHeaders
                    .Where(header =>
                        !CorsConstants.SimpleResponseHeaders.Contains(header, StringComparer.OrdinalIgnoreCase))
                    .ToArray();

                if (nonSimpleAllowResponseHeaders.Length > 0)
                {
                    headers.SetCommaSeparatedValues(
                        CorsConstants.AccessControlExposeHeaders,
                        nonSimpleAllowResponseHeaders);
                }
            }

            if (result.PreflightMaxAge.HasValue)
            {
                headers[CorsConstants.AccessControlMaxAge]
                    = result.PreflightMaxAge.Value.TotalSeconds.ToString(CultureInfo.InvariantCulture);
            }
        }

        private void AddOriginToResult(string origin, CorsPolicy policy, CorsResult result)
        {
            if (policy.AllowAnyOrigin)
            {
                if (policy.SupportsCredentials)
                {
                    result.AllowedOrigin = origin;
                    result.VaryByOrigin = true;
                }
                else
                {
                    result.AllowedOrigin = CorsConstants.AnyOrigin;
                }
            }
            else if (policy.Origins.Contains(origin))
            {
                result.AllowedOrigin = origin;
            }
        }

        private static void AddHeaderValues(IList<string> target, IEnumerable<string> headerValues)
        {
            if (headerValues == null)
            {
                return;
            }

            foreach (var current in headerValues)
            {
                target.Add(current);
            }
        }
    }
}