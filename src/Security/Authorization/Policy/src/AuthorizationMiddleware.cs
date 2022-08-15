// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// A middleware that enables authorization capabilities.
/// </summary>
public class AuthorizationMiddleware
{
    // AppContext switch used to control whether HttpContext or endpoint is passed as a resource to AuthZ
    private const string SuppressUseHttpContextAsAuthorizationResource = "Microsoft.AspNetCore.Authorization.SuppressUseHttpContextAsAuthorizationResource";

    // Property key is used by Endpoint routing to determine if Authorization has run
    private const string AuthorizationMiddlewareInvokedWithEndpointKey = "__AuthorizationMiddlewareWithEndpointInvoked";
    private static readonly object AuthorizationMiddlewareWithEndpointInvokedValue = new object();

    private readonly RequestDelegate _next;
    private readonly IAuthorizationPolicyProvider _policyProvider;

    // Caches AuthorizationPolicy instances
    private readonly DataSourceDependentCache<ConcurrentDictionary<Endpoint, AuthorizationPolicy>>? _policyCache;

    /// <summary>
    /// Initializes a new instance of <see cref="AuthorizationMiddleware"/>.
    /// </summary>
    /// <param name="next">The next middleware in the application middleware pipeline.</param>
    /// <param name="policyProvider">The <see cref="IAuthorizationPolicyProvider"/>.</param>
    public AuthorizationMiddleware(RequestDelegate next, IAuthorizationPolicyProvider policyProvider)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _policyProvider = policyProvider ?? throw new ArgumentNullException(nameof(policyProvider));
    }

    /// <summary>
    /// Initializes a new instance of <see cref="AuthorizationMiddleware"/>.
    /// </summary>
    /// <param name="next">The next middleware in the application middleware pipeline.</param>
    /// <param name="policyProvider">The <see cref="IAuthorizationPolicyProvider"/>.</param>
    /// <param name="dataSource">The <see cref="EndpointDataSource"/>.</param>
    public AuthorizationMiddleware(RequestDelegate next, IAuthorizationPolicyProvider policyProvider, EndpointDataSource dataSource) : this(next, policyProvider)
    {
        if (dataSource != null)
        {
            // We cache AuthorizationPolicy instances per-Endpoint for performance, but we want to wipe out
            // that cache if the endpoints change so that we don't allow unbounded memory growth.
            _policyCache = new DataSourceDependentCache<ConcurrentDictionary<Endpoint, AuthorizationPolicy>>(dataSource, (_) =>
            {
                // We don't eagerly fill this cache because there's no real reason to. Unlike URL matching, we don't
                // need to build a big data structure up front to be correct.
                return new ConcurrentDictionary<Endpoint, AuthorizationPolicy>();
            });
        }
    }

    /// <summary>
    /// Invokes the middleware performing authorization.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
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

        // Use the computed policy for this endpoint if we can
        AuthorizationPolicy? policy = null;

        var canCachePolicy = _policyProvider.CanCachePolicy && _policyCache != null && endpoint != null;
        if (canCachePolicy)
        {
            _policyCache!.EnsureInitialized();
            _policyCache?.Value?.TryGetValue(endpoint!, out policy);
        }

        if (policy == null)
        {
            // IMPORTANT: Changes to authorization logic should be mirrored in MVC's AuthorizeFilter
            var authorizeData = endpoint?.Metadata.GetOrderedMetadata<IAuthorizeData>() ?? Array.Empty<IAuthorizeData>();

            var policies = endpoint?.Metadata.GetOrderedMetadata<AuthorizationPolicy>() ?? Array.Empty<AuthorizationPolicy>();

            policy = await AuthorizationPolicy.CombineAsync(_policyProvider, authorizeData, policies);

            // Cache the computed policy
            if (policy != null && canCachePolicy)
            {
                _policyCache!.Value![endpoint!] = policy;
            }
        }

        if (policy == null)
        {
            await _next(context);
            return;
        }

        // Policy evaluator has transient lifetime so it's fetched from request services instead of injecting in constructor
        var policyEvaluator = context.RequestServices.GetRequiredService<IPolicyEvaluator>();

        var authenticateResult = await policyEvaluator.AuthenticateAsync(policy, context);

        if (authenticateResult?.Succeeded ?? false)
        {
            if (context.Features.Get<IAuthenticateResultFeature>() is IAuthenticateResultFeature authenticateResultFeature)
            {
                authenticateResultFeature.AuthenticateResult = authenticateResult;
            }
            else
            {
                var authFeatures = new AuthenticationFeatures(authenticateResult);
                context.Features.Set<IHttpAuthenticationFeature>(authFeatures);
                context.Features.Set<IAuthenticateResultFeature>(authFeatures);
            }
        }

        // Allow Anonymous still wants to run authorization to populate the User but skips any failure/challenge handling
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null)
        {
            await _next(context);
            return;
        }

        object? resource;
        if (AppContext.TryGetSwitch(SuppressUseHttpContextAsAuthorizationResource, out var useEndpointAsResource) && useEndpointAsResource)
        {
            resource = endpoint;
        }
        else
        {
            resource = context;
        }

        var authorizeResult = await policyEvaluator.AuthorizeAsync(policy, authenticateResult!, context, resource);
        var authorizationMiddlewareResultHandler = context.RequestServices.GetRequiredService<IAuthorizationMiddlewareResultHandler>();
        await authorizationMiddlewareResultHandler.HandleAsync(_next, context, policy, authorizeResult);
    }
}
