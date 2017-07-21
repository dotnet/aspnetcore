// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Internal;
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

        /// <inheritdoc />
        public virtual async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var effectivePolicy = Policy;
            if (effectivePolicy == null)
            {
                if (PolicyProvider == null)
                {
                    throw new InvalidOperationException(
                        Resources.FormatAuthorizeFilter_AuthorizationPolicyCannotBeCreated(
                            nameof(AuthorizationPolicy),
                            nameof(IAuthorizationPolicyProvider)));
                }

                effectivePolicy = await AuthorizationPolicy.CombineAsync(PolicyProvider, AuthorizeData);
            }

            if (effectivePolicy == null)
            {
                return;
            }

            var policyEvaluator = context.HttpContext.RequestServices.GetRequiredService<IPolicyEvaluator>();

            var authenticateResult = await policyEvaluator.AuthenticateAsync(effectivePolicy, context.HttpContext);

            // Allow Anonymous skips all authorization
            if (context.Filters.Any(item => item is IAllowAnonymousFilter))
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
    }
}
