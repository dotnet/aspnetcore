// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Cors.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Cors.Infrastructure
{
    /// <summary>
    /// Default implementation of <see cref="ICorsService"/>.
    /// </summary>
    public class CorsService : ICorsService
    {
        private readonly CorsOptions _options;
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new instance of the <see cref="CorsService"/>.
        /// </summary>
        /// <param name="options">The option model representing <see cref="CorsOptions"/>.</param>
        [Obsolete("This constructor is obsolete and will be removed in a future version.")]
        public CorsService(IOptions<CorsOptions> options)
            : this(options, loggerFactory: NullLoggerFactory.Instance)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="CorsService"/>.
        /// </summary>
        /// <param name="options">The option model representing <see cref="CorsOptions"/>.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public CorsService(IOptions<CorsOptions> options, ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _options = options.Value;
            _logger = loggerFactory.CreateLogger<CorsService>();
        }

        /// <summary>
        /// Looks up a policy using the <paramref name="policyName"/> and then evaluates the policy using the passed in
        /// <paramref name="context"/>.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="policyName"></param>
        /// <returns>A <see cref="CorsResult"/> which contains the result of policy evaluation and can be
        /// used by the caller to set appropriate response headers.</returns>
        public CorsResult EvaluatePolicy(HttpContext context, string policyName)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var policy = _options.GetPolicy(policyName);
            return EvaluatePolicy(context, policy);
        }

        /// <inheritdoc />
        public CorsResult EvaluatePolicy(HttpContext context, CorsPolicy policy)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            if (policy.AllowAnyOrigin && policy.SupportsCredentials)
            {
                _logger.InsecureConfiguration();
            }

            var origin = context.Request.Headers[CorsConstants.Origin];
            var requestHeaders = context.Request.Headers;

            var isOptionsRequest = string.Equals(context.Request.Method, CorsConstants.PreflightHttpMethod, StringComparison.OrdinalIgnoreCase);
            var isPreflightRequest = isOptionsRequest && requestHeaders.ContainsKey(CorsConstants.AccessControlRequestMethod);

            if (isOptionsRequest && !isPreflightRequest)
            {
                _logger.IsNotPreflightRequest();
            }

            var corsResult = new CorsResult
            {
                IsPreflightRequest = isPreflightRequest,
                IsOriginAllowed = IsOriginAllowed(policy, origin),
            };

            if (isPreflightRequest)
            {
                EvaluatePreflightRequest(context, policy, corsResult);
            }
            else
            {
                EvaluateRequest(context, policy, corsResult);
            }

            return corsResult;
        }

        private static void PopulateResult(HttpContext context, CorsPolicy policy, CorsResult result)
        {
            if (policy.AllowAnyOrigin)
            {
                result.AllowedOrigin = CorsConstants.AnyOrigin;
                result.VaryByOrigin = policy.SupportsCredentials;
            }
            else
            {
                var origin = context.Request.Headers[CorsConstants.Origin];
                result.AllowedOrigin = origin;
                result.VaryByOrigin = policy.Origins.Count > 1;
            }

            result.SupportsCredentials = policy.SupportsCredentials;
            result.PreflightMaxAge = policy.PreflightMaxAge;

            // https://fetch.spec.whatwg.org/#http-new-header-syntax
            AddHeaderValues(result.AllowedExposedHeaders, policy.ExposedHeaders);

            var allowedMethods = policy.AllowAnyMethod ?
                new[] { result.IsPreflightRequest ? (string)context.Request.Headers[CorsConstants.AccessControlRequestMethod] : context.Request.Method } :
                policy.Methods;
            AddHeaderValues(result.AllowedMethods, allowedMethods);

            var allowedHeaders = policy.AllowAnyHeader ?
                context.Request.Headers.GetCommaSeparatedValues(CorsConstants.AccessControlRequestHeaders) :
                policy.Headers;
            AddHeaderValues(result.AllowedHeaders, allowedHeaders);
        }

        public virtual void EvaluateRequest(HttpContext context, CorsPolicy policy, CorsResult result)
        {
            PopulateResult(context, policy, result);
        }

        public virtual void EvaluatePreflightRequest(HttpContext context, CorsPolicy policy, CorsResult result)
        {
            PopulateResult(context, policy, result);
        }

        /// <inheritdoc />
        public virtual void ApplyResult(CorsResult result, HttpResponse response)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (!result.IsOriginAllowed)
            {
                // In case a server does not wish to participate in the CORS protocol, its HTTP response to the
                // CORS or CORS-preflight request must not include any of the above headers.
                return;
            }

            response.Headers[CorsConstants.AccessControlAllowOrigin] = result.AllowedOrigin;

            if (result.SupportsCredentials)
            {
                response.Headers[CorsConstants.AccessControlAllowCredentials] = "true";
            }

            if (result.IsPreflightRequest)
            {
                _logger.IsPreflightRequest();

                // An HTTP response to a CORS-preflight request can include the following headers:
                // `Access-Control-Allow-Methods`, `Access-Control-Allow-Headers`, `Access-Control-Max-Age`
                if (result.AllowedHeaders.Count > 0)
                {
                    response.Headers.SetCommaSeparatedValues(CorsConstants.AccessControlAllowHeaders, result.AllowedHeaders.ToArray());
                }

                if (result.AllowedMethods.Count > 0)
                {
                    response.Headers.SetCommaSeparatedValues(CorsConstants.AccessControlAllowMethods, result.AllowedMethods.ToArray());
                }

                if (result.PreflightMaxAge.HasValue)
                {
                    response.Headers[CorsConstants.AccessControlMaxAge] = result.PreflightMaxAge.Value.TotalSeconds.ToString(CultureInfo.InvariantCulture);
                }
            }
            else
            {
                // An HTTP response to a CORS request that is not a CORS-preflight request can also include the following header:
                // `Access-Control-Expose-Headers`
                if (result.AllowedExposedHeaders.Count > 0)
                {
                    response.Headers.SetCommaSeparatedValues(CorsConstants.AccessControlExposeHeaders, result.AllowedExposedHeaders.ToArray());
                }
            }

            if (result.VaryByOrigin)
            {
                response.Headers.Append("Vary", "Origin");
            }
        }

        private static void AddHeaderValues(IList<string> target, IList<string> headerValues)
        {
            if (headerValues == null)
            {
                return;
            }

            for (var i = 0; i < headerValues.Count; i++)
            {
                target.Add(headerValues[i]);
            }
        }

        private bool IsOriginAllowed(CorsPolicy policy, StringValues origin)
        {
            if (StringValues.IsNullOrEmpty(origin))
            {
                _logger.RequestDoesNotHaveOriginHeader();
                return false;
            }

            _logger.RequestHasOriginHeader(origin);
            if (policy.AllowAnyOrigin || policy.IsOriginAllowed(origin))
            {
                _logger.PolicySuccess();
                return true;
            }
            _logger.PolicyFailure();
            _logger.OriginNotAllowed(origin);
            return false;
        }
    }
}
