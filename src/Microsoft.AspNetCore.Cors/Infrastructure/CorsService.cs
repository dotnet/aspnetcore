// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Cors.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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
        public CorsService(IOptions<CorsOptions> options)
            : this(options, loggerFactory: null)
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

            _options = options.Value;
            _logger = loggerFactory?.CreateLogger<CorsService>();
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

            var corsResult = new CorsResult();
            var accessControlRequestMethod = context.Request.Headers[CorsConstants.AccessControlRequestMethod];
            if (string.Equals(context.Request.Method, CorsConstants.PreflightHttpMethod, StringComparison.OrdinalIgnoreCase) &&
                !StringValues.IsNullOrEmpty(accessControlRequestMethod))
            {
                _logger?.IsPreflightRequest();
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
            if (!IsOriginAllowed(policy, origin))
            {
                return;
            }

            AddOriginToResult(origin, policy, result);
            result.SupportsCredentials = policy.SupportsCredentials;
            AddHeaderValues(result.AllowedExposedHeaders, policy.ExposedHeaders);
            _logger?.PolicySuccess();
        }

        public virtual void EvaluatePreflightRequest(HttpContext context, CorsPolicy policy, CorsResult result)
        {
            var origin = context.Request.Headers[CorsConstants.Origin];
            if (!IsOriginAllowed(policy, origin))
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

            if (!policy.AllowAnyMethod)
            {
                var found = false;
                for (var i = 0; i < policy.Methods.Count; i++)
                {
                    var method = policy.Methods[i];
                    if (string.Equals(method, accessControlRequestMethod, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    _logger?.PolicyFailure();
                    _logger?.AccessControlMethodNotAllowed(accessControlRequestMethod);
                    return;
                }
            }

            if (!policy.AllowAnyHeader &&
                requestHeaders != null)
            {
                foreach (var requestHeader in requestHeaders)
                {
                    if (!policy.Headers.Contains(requestHeader, StringComparer.OrdinalIgnoreCase))
                    {
                        _logger?.PolicyFailure();
                        _logger?.RequestHeaderNotAllowed(requestHeader);
                        return;
                    }
                }
            }

            AddOriginToResult(origin, policy, result);
            result.SupportsCredentials = policy.SupportsCredentials;
            result.PreflightMaxAge = policy.PreflightMaxAge;
            result.AllowedMethods.Add(accessControlRequestMethod);
            AddHeaderValues(result.AllowedHeaders, requestHeaders);
            _logger?.PolicySuccess();
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
                headers.SetCommaSeparatedValues(
                    CorsConstants.AccessControlAllowMethods,
                    result.AllowedMethods.ToArray());
            }

            if (result.AllowedHeaders.Count > 0)
            {
                headers.SetCommaSeparatedValues(
                    CorsConstants.AccessControlAllowHeaders,
                    result.AllowedHeaders.ToArray());
            }

            if (result.AllowedExposedHeaders.Count > 0)
            {
                headers.SetCommaSeparatedValues(
                    CorsConstants.AccessControlExposeHeaders,
                    result.AllowedExposedHeaders.ToArray());
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
            else if (policy.IsOriginAllowed(origin))
            {
                result.AllowedOrigin = origin;

                if(policy.Origins.Count > 1)
                {
                    result.VaryByOrigin = true;
                }
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

        private bool IsOriginAllowed(CorsPolicy policy, StringValues origin)
        {
            if (StringValues.IsNullOrEmpty(origin))
            {
                _logger?.RequestDoesNotHaveOriginHeader();
                return false;
            }

            _logger?.RequestHasOriginHeader(origin);
            if (policy.AllowAnyOrigin || policy.IsOriginAllowed(origin))
            {
                return true;
            }
            _logger?.PolicyFailure();
            _logger?.OriginNotAllowed(origin);
            return false;
        }
    }
}
