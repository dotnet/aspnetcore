// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authorization
{
    public class AuthorizationMiddleware
    {
        // Property key is used by Endpoint routing to determine if Authorization has run
        private const string AuthorizationMiddlewareInvokedWithEndpointKey = "__AuthorizationMiddlewareWithEndpointInvoked";
        private static readonly object AuthorizationMiddlewareWithEndpointInvokedValue = new object();

        private readonly RequestDelegate _next;
        private readonly IAuthorizationPolicyProvider _policyProvider;
        private readonly AuthorizationMiddlewareOptions _options;

        public AuthorizationMiddleware(RequestDelegate next, IAuthorizationPolicyProvider policyProvider, IOptions<AuthorizationMiddlewareOptions> options)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _policyProvider = policyProvider ?? throw new ArgumentNullException(nameof(policyProvider));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var endpoint = context.GetEndpoint();

            if (endpoint != null)
            {
                // EndpointRoutingMiddleware uses this flag to check if the Authorization middleware processed auth metadata on the endpoint.
                // The Authorization middleware can only make this claim if it observes an actual endpoint.
                context.Items[AuthorizationMiddlewareInvokedWithEndpointKey] = AuthorizationMiddlewareWithEndpointInvokedValue;
            }

            // IMPORTANT: Changes to authorization logic should be mirrored in MVC's AuthorizeFilter
            var authorizeData = endpoint?.Metadata.GetOrderedMetadata<IAuthorizeData>() ?? Array.Empty<IAuthorizeData>();
            var policy = await AuthorizationPolicy.CombineAsync(_policyProvider, authorizeData);
            if (policy == null)
            {
                await _next(context);
                return;
            }

            // Policy evaluator has transient lifetime so it fetched from request services instead of injecting in constructor
            var policyEvaluator = context.RequestServices.GetRequiredService<IPolicyEvaluator>();

            var authenticateResult = await policyEvaluator.AuthenticateAsync(policy, context);

            // Allow Anonymous skips all authorization
            if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null)
            {
                await _next(context);
                return;
            }

            object resource;
            if (_options.UseHttpContextAsResource)
            {
                resource = context;
            }
            else
            {
                resource = endpoint;
            }
            
            var authorizeResult = await policyEvaluator.AuthorizeAsync(policy, authenticateResult, context, resource);
            var authorizationMiddlewareResultHandler = context.RequestServices.GetRequiredService<IAuthorizationMiddlewareResultHandler>();
            await authorizationMiddlewareResultHandler.HandleAsync(_next, context, policy, authorizeResult);
        }
    }
}
