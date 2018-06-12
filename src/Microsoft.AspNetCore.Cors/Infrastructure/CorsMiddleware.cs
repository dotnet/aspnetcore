// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Cors.Infrastructure
{
    /// <summary>
    /// An ASP.NET middleware for handling CORS.
    /// </summary>
    public class CorsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ICorsService _corsService;
        private readonly ICorsPolicyProvider _corsPolicyProvider;
        private readonly CorsPolicy _policy;
        private readonly string _corsPolicyName;
        private readonly ILogger _logger;

        /// <summary>
        /// Instantiates a new <see cref="CorsMiddleware"/>.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="corsService">An instance of <see cref="ICorsService"/>.</param>
        /// <param name="policyProvider">A policy provider which can get an <see cref="CorsPolicy"/>.</param>
        [Obsolete("This constructor has been replaced with an equivalent constructor which requires an ILoggerFactory")]
        public CorsMiddleware(
            RequestDelegate next,
            ICorsService corsService,
            ICorsPolicyProvider policyProvider)
            : this(next, corsService, policyProvider, NullLoggerFactory.Instance, policyName: null)
        {
        }

        /// <summary>
        /// Instantiates a new <see cref="CorsMiddleware"/>.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="corsService">An instance of <see cref="ICorsService"/>.</param>
        /// <param name="policyProvider">A policy provider which can get an <see cref="CorsPolicy"/>.</param>
        /// <param name="policyName">An optional name of the policy to be fetched.</param>
        [Obsolete("This constructor has been replaced with an equivalent constructor which requires an ILoggerFactory")]
        public CorsMiddleware(
            RequestDelegate next,
            ICorsService corsService,
            ICorsPolicyProvider policyProvider,
            string policyName)
            : this(next, corsService, policyProvider, NullLoggerFactory.Instance, policyName)
        {
        }

        /// <summary>
        /// Instantiates a new <see cref="CorsMiddleware"/>.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="corsService">An instance of <see cref="ICorsService"/>.</param>
        /// <param name="policy">An instance of the <see cref="CorsPolicy"/> which can be applied.</param>
        [Obsolete("This constructor has been replaced with an equivalent constructor which requires an ILoggerFactory")]
        public CorsMiddleware(
            RequestDelegate next,
            ICorsService corsService,
            CorsPolicy policy)
            : this(next, corsService, policy, NullLoggerFactory.Instance)
        {
        }

        /// <summary>
        /// Instantiates a new <see cref="CorsMiddleware"/>.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="corsService">An instance of <see cref="ICorsService"/>.</param>
        /// <param name="policyProvider">A policy provider which can get an <see cref="CorsPolicy"/>.</param>
        /// <param name="loggerFactory">An instance of <see cref="ILoggerFactory"/>.</param>
        public CorsMiddleware(
            RequestDelegate next,
            ICorsService corsService,
            ICorsPolicyProvider policyProvider,
            ILoggerFactory loggerFactory)
            : this(next, corsService, policyProvider, loggerFactory, policyName: null)
        {
        }

        /// <summary>
        /// Instantiates a new <see cref="CorsMiddleware"/>.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="corsService">An instance of <see cref="ICorsService"/>.</param>
        /// <param name="policyProvider">A policy provider which can get an <see cref="CorsPolicy"/>.</param>
        /// <param name="loggerFactory">An instance of <see cref="ILoggerFactory"/>.</param>
        /// <param name="policyName">An optional name of the policy to be fetched.</param>
        public CorsMiddleware(
            RequestDelegate next,
            ICorsService corsService,
            ICorsPolicyProvider policyProvider,
            ILoggerFactory loggerFactory,
            string policyName)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (corsService == null)
            {
                throw new ArgumentNullException(nameof(corsService));
            }

            if (policyProvider == null)
            {
                throw new ArgumentNullException(nameof(policyProvider));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _next = next;
            _corsService = corsService;
            _corsPolicyProvider = policyProvider;
            _corsPolicyName = policyName;
            _logger = loggerFactory.CreateLogger<CorsMiddleware>();
        }

        /// <summary>
        /// Instantiates a new <see cref="CorsMiddleware"/>.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="corsService">An instance of <see cref="ICorsService"/>.</param>
        /// <param name="policy">An instance of the <see cref="CorsPolicy"/> which can be applied.</param>
        /// <param name="loggerFactory">An instance of <see cref="ILoggerFactory"/>.</param>
        public CorsMiddleware(
            RequestDelegate next,
            ICorsService corsService,
            CorsPolicy policy,
            ILoggerFactory loggerFactory)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (corsService == null)
            {
                throw new ArgumentNullException(nameof(corsService));
            }

            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _next = next;
            _corsService = corsService;
            _policy = policy;
            _logger = loggerFactory.CreateLogger<CorsMiddleware>();
        }

        /// <inheritdoc />
        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Headers.ContainsKey(CorsConstants.Origin))
            {
                var corsPolicy = _policy ?? await _corsPolicyProvider?.GetPolicyAsync(context, _corsPolicyName);
                if (corsPolicy != null)
                {
                    var accessControlRequestMethod =
                        context.Request.Headers[CorsConstants.AccessControlRequestMethod];
                    if (string.Equals(
                            context.Request.Method,
                            CorsConstants.PreflightHttpMethod,
                            StringComparison.OrdinalIgnoreCase) &&
                            !StringValues.IsNullOrEmpty(accessControlRequestMethod))
                    {
                        ApplyCorsHeaders(context, corsPolicy);

                        // Since there is a policy which was identified,
                        // always respond to preflight requests.
                        context.Response.StatusCode = StatusCodes.Status204NoContent;
                        return;
                    }
                    else
                    {
                        context.Response.OnStarting(state =>
                        {
                            var (httpContext, policy) = (Tuple<HttpContext, CorsPolicy>)state;
                            try
                            {
                                ApplyCorsHeaders(httpContext, policy);
                            }
                            catch (Exception exception)
                            {
                                _logger.FailedToSetCorsHeaders(exception);
                            }
                            return Task.CompletedTask;
                        }, Tuple.Create(context, corsPolicy));
                    }
                }
            }

            await _next(context);
        }

        private void ApplyCorsHeaders(HttpContext context, CorsPolicy corsPolicy)
        {
            var corsResult = _corsService.EvaluatePolicy(context, corsPolicy);
            _corsService.ApplyResult(corsResult, context.Response);
        }
    }
}
