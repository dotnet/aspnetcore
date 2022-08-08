// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Authorization extension methods for <see cref="IEndpointConventionBuilder"/>.
/// </summary>
public static class AuthorizationEndpointConventionBuilderExtensions
{
    private static readonly IAllowAnonymous _allowAnonymousMetadata = new AllowAnonymousAttribute();

    /// <summary>
    /// Adds the default authorization policy to the endpoint(s).
    /// </summary>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <returns>The original convention builder parameter.</returns>
    public static TBuilder RequireAuthorization<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.RequireAuthorization(new AuthorizeAttribute());
    }

    /// <summary>
    /// Adds authorization policies with the specified names to the endpoint(s).
    /// </summary>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <param name="policyNames">A collection of policy names. If empty, the default authorization policy will be used.</param>
    /// <returns>The original convention builder parameter.</returns>
    public static TBuilder RequireAuthorization<TBuilder>(this TBuilder builder, params string[] policyNames) where TBuilder : IEndpointConventionBuilder
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (policyNames == null)
        {
            throw new ArgumentNullException(nameof(policyNames));
        }

        return builder.RequireAuthorization(policyNames.Select(n => new AuthorizeAttribute(n)).ToArray());
    }

    /// <summary>
    /// Adds authorization policies with the specified <see cref="IAuthorizeData"/> to the endpoint(s).
    /// </summary>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <param name="authorizeData">
    /// A collection of <paramref name="authorizeData"/>. If empty, the default authorization policy will be used.
    /// </param>
    /// <returns>The original convention builder parameter.</returns>
    public static TBuilder RequireAuthorization<TBuilder>(this TBuilder builder, params IAuthorizeData[] authorizeData)
        where TBuilder : IEndpointConventionBuilder
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (authorizeData == null)
        {
            throw new ArgumentNullException(nameof(authorizeData));
        }

        if (authorizeData.Length == 0)
        {
            authorizeData = new IAuthorizeData[] { new AuthorizeAttribute(), };
        }

        RequireAuthorizationCore(builder, authorizeData);
        return builder;
    }

    /// <summary>
    /// Adds an authorization policy to the endpoint(s).
    /// </summary>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <param name="policy">The <see cref="AuthorizationPolicy"/> policy.</param>
    /// <returns>The original convention builder parameter.</returns>
    public static TBuilder RequireAuthorization<TBuilder>(this TBuilder builder, AuthorizationPolicy policy)
        where TBuilder : IEndpointConventionBuilder
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (policy == null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        RequirePolicyCore(builder, policy);
        return builder;
    }

    /// <summary>
    /// Adds an new authorization policy configured by a callback to the endpoint(s).
    /// </summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <param name="configurePolicy">The callback used to configure the policy.</param>
    /// <returns>The original convention builder parameter.</returns>
    public static TBuilder RequireAuthorization<TBuilder>(this TBuilder builder, Action<AuthorizationPolicyBuilder> configurePolicy)
        where TBuilder : IEndpointConventionBuilder
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configurePolicy == null)
        {
            throw new ArgumentNullException(nameof(configurePolicy));
        }

        var policyBuilder = new AuthorizationPolicyBuilder();
        configurePolicy(policyBuilder);
        RequirePolicyCore(builder, policyBuilder.Build());
        return builder;
    }

    /// <summary>
    /// Allows anonymous access to the endpoint by adding <see cref="AllowAnonymousAttribute" /> to the endpoint metadata. This will bypass
    /// all authorization checks for the endpoint including the default authorization policy and fallback authorization policy.
    /// </summary>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <returns>The original convention builder parameter.</returns>
    public static TBuilder AllowAnonymous<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        builder.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(_allowAnonymousMetadata);
        });
        return builder;
    }

    private static void RequirePolicyCore<TBuilder>(TBuilder builder, AuthorizationPolicy policy)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.Add(endpointBuilder =>
        {
            // Only add an authorize attribute if there isn't one
            if (!endpointBuilder.Metadata.Any(meta => meta is IAuthorizeData))
            {
                endpointBuilder.Metadata.Add(new AuthorizeAttribute());
            }
            // Only add a policy cache if there isn't one
            if (!endpointBuilder.Metadata.Any(meta => meta is AuthorizationPolicyCache))
            {
                endpointBuilder.Metadata.Add(new AuthorizationPolicyCache());
            }
            endpointBuilder.Metadata.Add(policy);
        });
    }

    private static void RequireAuthorizationCore<TBuilder>(TBuilder builder, IEnumerable<IAuthorizeData> authorizeData)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.Add(endpointBuilder =>
        {
            foreach (var data in authorizeData)
            {
                endpointBuilder.Metadata.Add(data);
            }
            endpointBuilder.Metadata.Add(new AuthorizationPolicyCache());
        });
    }

}
