// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Cors.Infrastructure
{
    /// <summary>
    /// A middleware for handling CORS.
    /// </summary>
    public class CorsMiddleware
    {
        // Property key is used by other systems, e.g. MVC, to check if CORS middleware has run
        private const string CorsMiddlewareWithEndpointInvokedKey = "__CorsMiddlewareWithEndpointInvoked";
        private static readonly object CorsMiddlewareWithEndpointInvokedValue = new object();

        private readonly Func<object, Task> OnResponseStartingDelegate = OnResponseStarting;
        private readonly RequestDelegate _next;
        private readonly CorsPolicy _policy;
        private readonly string _corsPolicyName;

        /// <summary>
        /// Instantiates a new <see cref="CorsMiddleware"/>.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="corsService">An instance of <see cref="ICorsService"/>.</param>
        /// <param name="loggerFactory">An instance of <see cref="ILoggerFactory"/>.</param>
        public CorsMiddleware(
            RequestDelegate next,
            ICorsService corsService,
            ILoggerFactory loggerFactory)
            : this(next, corsService, loggerFactory, policyName: null)
        {
        }

        /// <summary>
        /// Instantiates a new <see cref="CorsMiddleware"/>.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="corsService">An instance of <see cref="ICorsService"/>.</param>
        /// <param name="loggerFactory">An instance of <see cref="ILoggerFactory"/>.</param>
        /// <param name="policyName">An optional name of the policy to be fetched.</param>
        public CorsMiddleware(
            RequestDelegate next,
            ICorsService corsService,
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

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _next = next;
            CorsService = corsService;
            _corsPolicyName = policyName;
            Logger = loggerFactory.CreateLogger<CorsMiddleware>();
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
            CorsService = corsService;
            _policy = policy;
            Logger = loggerFactory.CreateLogger<CorsMiddleware>();
        }

        private ICorsService CorsService { get; }

        private ILogger Logger { get; }

        /// <inheritdoc />
        public Task Invoke(HttpContext context, ICorsPolicyProvider corsPolicyProvider)
        {
            // CORS policy resolution rules:
            //
            // 1. If there is an endpoint with IDisableCorsAttribute then CORS is not run
            // 2. If there is an endpoint with ICorsPolicyMetadata then use its policy or if
            //    there is an endpoint with IEnableCorsAttribute that has a policy name then
            //    fetch policy by name, prioritizing it above policy on middleware
            // 3. If there is no policy on middleware then use name on middleware
            var endpoint = context.GetEndpoint();

            if (endpoint != null)
            {
                // EndpointRoutingMiddleware uses this flag to check if the CORS middleware processed CORS metadata on the endpoint.
                // The CORS middleware can only make this claim if it observes an actual endpoint.
                context.Items[CorsMiddlewareWithEndpointInvokedKey] = CorsMiddlewareWithEndpointInvokedValue;
            }

            if (!context.Request.Headers.ContainsKey(CorsConstants.Origin))
            {
                return _next(context);
            }

            // Get the most significant CORS metadata for the endpoint
            // For backwards compatibility reasons this is then downcast to Enable/Disable metadata
            var corsMetadata = endpoint?.Metadata.GetMetadata<ICorsMetadata>();

            if (corsMetadata is IDisableCorsAttribute)
            {
                var isOptionsRequest = HttpMethods.IsOptions(context.Request.Method);

                var isCorsPreflightRequest = isOptionsRequest && context.Request.Headers.ContainsKey(CorsConstants.AccessControlRequestMethod);

                if (isCorsPreflightRequest)
                {
                    // If this is a preflight request, and we disallow CORS, complete the request
                    context.Response.StatusCode = StatusCodes.Status204NoContent;
                    return Task.CompletedTask;
                }

                return _next(context);
            }

            var corsPolicy = _policy;
            var policyName = _corsPolicyName;
            if (corsMetadata is ICorsPolicyMetadata corsPolicyMetadata)
            {
                policyName = null;
                corsPolicy = corsPolicyMetadata.Policy;
            }
            else if (corsMetadata is IEnableCorsAttribute enableCorsAttribute &&
                enableCorsAttribute.PolicyName != null)
            {
                // If a policy name has been provided on the endpoint metadata then prioritizing it above the static middleware policy
                policyName = enableCorsAttribute.PolicyName;
                corsPolicy = null;
            }

            if (corsPolicy == null)
            {
                // Resolve policy by name if the local policy is not being used
                var policyTask = corsPolicyProvider.GetPolicyAsync(context, policyName);
                if (!policyTask.IsCompletedSuccessfully)
                {
                    return InvokeCoreAwaited(context, policyTask);
                }

                corsPolicy = policyTask.Result;
            }

            return EvaluateAndApplyPolicy(context, corsPolicy);

            async Task InvokeCoreAwaited(HttpContext context, Task<CorsPolicy> policyTask)
            {
                var corsPolicy = await policyTask;
                await EvaluateAndApplyPolicy(context, corsPolicy);
            }
        }

        private Task EvaluateAndApplyPolicy(HttpContext context, CorsPolicy corsPolicy)
        {
            if (corsPolicy == null)
            {
                Logger.NoCorsPolicyFound();
                return _next(context);
            }

            var corsResult = CorsService.EvaluatePolicy(context, corsPolicy);
            if (corsResult.IsPreflightRequest)
            {
                CorsService.ApplyResult(corsResult, context.Response);

                // Since there is a policy which was identified,
                // always respond to preflight requests.
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return Task.CompletedTask;
            }
            else
            {
                context.Response.OnStarting(OnResponseStartingDelegate, Tuple.Create(this, context, corsResult));
                return _next(context);
            }
        }

        private static Task OnResponseStarting(object state)
        {
            var (middleware, context, result) = (Tuple<CorsMiddleware, HttpContext, CorsResult>)state;
            try
            {
                middleware.CorsService.ApplyResult(result, context.Response);
            }
            catch (Exception exception)
            {
                middleware.Logger.FailedToSetCorsHeaders(exception);
            }
            return Task.CompletedTask;
        }
    }
}
