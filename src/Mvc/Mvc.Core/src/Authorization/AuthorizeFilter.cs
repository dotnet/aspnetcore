// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Authorization
{
    /// <summary>
    /// An implementation of <see cref="IAsyncAuthorizationFilter"/> which applies a specific
    /// <see cref="AuthorizationPolicy"/>. MVC recognizes the <see cref="AuthorizeAttribute"/> and adds an instance of
    /// this filter to the associated action or controller.
    /// </summary>
    public class AuthorizeFilter : IAsyncAuthorizationFilter, IFilterFactory
    {
        /// <summary>
        /// Initializes a new <see cref="AuthorizeFilter"/> instance.
        /// </summary>
        public AuthorizeFilter()
            : this(authorizeData: new[] { new AuthorizeAttribute() })
        {
        }

        /// <summary>
        /// Initialize a new <see cref="AuthorizeFilter"/> instance.
        /// </summary>
        /// <param name="policy">Authorization policy to be used.</param>
        public AuthorizeFilter(AuthorizationPolicy policy)
        {
            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            Policy = policy;
        }

        /// <summary>
        /// Initialize a new <see cref="AuthorizeFilter"/> instance.
        /// </summary>
        /// <param name="policyProvider">The <see cref="IAuthorizationPolicyProvider"/> to use to resolve policy names.</param>
        /// <param name="authorizeData">The <see cref="IAuthorizeData"/> to combine into an <see cref="IAuthorizeData"/>.</param>
        public AuthorizeFilter(IAuthorizationPolicyProvider policyProvider, IEnumerable<IAuthorizeData> authorizeData)
            : this(authorizeData)
        {
            if (policyProvider == null)
            {
                throw new ArgumentNullException(nameof(policyProvider));
            }

            PolicyProvider = policyProvider;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="AuthorizeFilter"/>.
        /// </summary>
        /// <param name="authorizeData">The <see cref="IAuthorizeData"/> to combine into an <see cref="IAuthorizeData"/>.</param>
        public AuthorizeFilter(IEnumerable<IAuthorizeData> authorizeData)
        {
            if (authorizeData == null)
            {
                throw new ArgumentNullException(nameof(authorizeData));
            }

            AuthorizeData = authorizeData;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="AuthorizeFilter"/>.
        /// </summary>
        /// <param name="policy">The name of the policy to require for authorization.</param>
        public AuthorizeFilter(string policy)
            : this(new[] { new AuthorizeAttribute(policy) })
        {
        }

        /// <summary>
        /// The <see cref="IAuthorizationPolicyProvider"/> to use to resolve policy names.
        /// </summary>
        public IAuthorizationPolicyProvider PolicyProvider { get; }

        /// <summary>
        /// The <see cref="IAuthorizeData"/> to combine into an <see cref="IAuthorizeData"/>.
        /// </summary>
        public IEnumerable<IAuthorizeData> AuthorizeData { get; }

        /// <summary>
        /// Gets the authorization policy to be used.
        /// </summary>
        /// <remarks>
        /// If<c>null</c>, the policy will be constructed using
        /// <see cref="AuthorizationPolicy.CombineAsync(IAuthorizationPolicyProvider, IEnumerable{IAuthorizeData})"/>.
        /// </remarks>
        public AuthorizationPolicy Policy { get; }

        bool IFilterFactory.IsReusable => true;

        // Computes the actual policy for this filter using either Policy or PolicyProvider + AuthorizeData
        private Task<AuthorizationPolicy> ComputePolicyAsync()
        {
            if (Policy != null)
            {
                return Task.FromResult(Policy);
            }

            if (PolicyProvider == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatAuthorizeFilter_AuthorizationPolicyCannotBeCreated(
                        nameof(AuthorizationPolicy),
                        nameof(IAuthorizationPolicyProvider)));
            }

            return AuthorizationPolicy.CombineAsync(PolicyProvider, AuthorizeData);
        }

        internal async Task<AuthorizationPolicy> GetEffectivePolicyAsync(AuthorizationFilterContext context)
        {
            // Combine all authorize filters into single effective policy that's only run on the closest filter
            var builder = new AuthorizationPolicyBuilder(await ComputePolicyAsync());
            for (var i = 0; i < context.Filters.Count; i++)
            {
                if (ReferenceEquals(this, context.Filters[i]))
                {
                    continue;
                }

                if (context.Filters[i] is AuthorizeFilter authorizeFilter)
                {
                    // Combine using the explicit policy, or the dynamic policy provider
                    builder.Combine(await authorizeFilter.ComputePolicyAsync());
                }
            }

            var endpoint = context.HttpContext.GetEndpoint();
            if (endpoint != null)
            {
                // When doing endpoint routing, MVC does not create filters for any authorization specific metadata i.e [Authorize] does not
                // get translated into AuthorizeFilter. Consequently, there are some rough edges when an application uses a mix of AuthorizeFilter
                // explicilty configured by the user (e.g. global auth filter), and uses endpoint metadata.
                // To keep the behavior of AuthFilter identical to pre-endpoint routing, we will gather auth data from endpoint metadata
                // and produce a policy using this. This would mean we would have effectively run some auth twice, but it maintains compat.
                var policyProvider = PolicyProvider ?? context.HttpContext.RequestServices.GetRequiredService<IAuthorizationPolicyProvider>();
                var endpointAuthorizeData = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>() ?? Array.Empty<IAuthorizeData>();

                var endpointPolicy = await AuthorizationPolicy.CombineAsync(policyProvider, endpointAuthorizeData);
                if (endpointPolicy != null)
                {
                    builder.Combine(endpointPolicy);
                }
            }

            return builder.Build();
        }

        /// <inheritdoc />
        public virtual async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!context.IsEffectivePolicy(this))
            {
                return;
            }

            // IMPORTANT: Changes to authorization logic should be mirrored in security's AuthorizationMiddleware
            var effectivePolicy = await GetEffectivePolicyAsync(context);
            if (effectivePolicy == null)
            {
                return;
            }

            var policyEvaluator = context.HttpContext.RequestServices.GetRequiredService<IPolicyEvaluator>();

            var authenticateResult = await policyEvaluator.AuthenticateAsync(effectivePolicy, context.HttpContext);

            // Allow Anonymous skips all authorization
            if (HasAllowAnonymous(context))
            {
                return;
            }

            var authorizeResult = await policyEvaluator.AuthorizeAsync(effectivePolicy, authenticateResult, context.HttpContext, context);

            if (authorizeResult.Challenged)
            {
                context.Result = new ChallengeResult(effectivePolicy.AuthenticationSchemes.ToArray());
            }
            else if (authorizeResult.Forbidden)
            {
                context.Result = new ForbidResult(effectivePolicy.AuthenticationSchemes.ToArray());
            }
        }

        IFilterMetadata IFilterFactory.CreateInstance(IServiceProvider serviceProvider)
        {
            if (Policy != null || PolicyProvider != null)
            {
                // The filter is fully constructed. Use the current instance to authorize.
                return this;
            }

            Debug.Assert(AuthorizeData != null);
            var policyProvider = serviceProvider.GetRequiredService<IAuthorizationPolicyProvider>();
            return AuthorizationApplicationModelProvider.GetFilter(policyProvider, AuthorizeData);
        }

        private static bool HasAllowAnonymous(AuthorizationFilterContext context)
        {
            var filters = context.Filters;
            for (var i = 0; i < filters.Count; i++)
            {
                if (filters[i] is IAllowAnonymousFilter)
                {
                    return true;
                }
            }

            // When doing endpoint routing, MVC does not add AllowAnonymousFilters for AllowAnonymousAttributes that
            // were discovered on controllers and actions. To maintain compat with 2.x,
            // we'll check for the presence of IAllowAnonymous in endpoint metadata.
            var endpoint = context.HttpContext.GetEndpoint();
            if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
            {
                return true;
            }

            return false;
        }
    }
}
