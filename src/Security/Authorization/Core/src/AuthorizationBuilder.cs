// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// Used to configure authorization
/// </summary>
public class AuthorizationBuilder
{
    /// <summary>
    /// Initializes a new instance of <see cref="AuthorizationBuilder"/>.
    /// </summary>
    /// <param name="services">The services being configured.</param>
    public AuthorizationBuilder(IServiceCollection services)
        => Services = services;

    /// <summary>
    /// The services being configured.
    /// </summary>
    public virtual IServiceCollection Services { get; }

    /// <summary>
    /// Determines whether authorization handlers should be invoked after <see cref="AuthorizationHandlerContext.HasFailed"/>.
    /// Defaults to true.
    /// </summary>
    /// <returns>The builder.</returns>
    public virtual AuthorizationBuilder SetInvokeHandlersAfterFailure(bool invoke)
    {
        Services.Configure<AuthorizationOptions>(o => o.InvokeHandlersAfterFailure = invoke);
        return this;
    }

    /// <summary>
    /// Sets the default authorization policy. Defaults to require authenticated users.
    /// </summary>
    /// <remarks>
    /// The default policy used when evaluating <see cref="IAuthorizeData"/> with no policy name specified.
    /// </remarks>
    /// <returns>The builder.</returns>
    public virtual AuthorizationBuilder SetDefaultPolicy(AuthorizationPolicy policy)
    {
        Services.Configure<AuthorizationOptions>(o => o.DefaultPolicy = policy);
        return this;
    }

    /// <summary>
    /// Sets the fallback authorization policy used by <see cref="AuthorizationPolicy.CombineAsync(IAuthorizationPolicyProvider, IEnumerable{IAuthorizeData})"/>
    /// when no IAuthorizeData have been provided. As a result, the AuthorizationMiddleware uses the fallback policy
    /// if there are no <see cref="IAuthorizeData"/> instances for a resource. If a resource has any <see cref="IAuthorizeData"/>
    /// then they are evaluated instead of the fallback policy. By default the fallback policy is null, and usually will have no
    /// effect unless you have the AuthorizationMiddleware in your pipeline. It is not used in any way by the
    /// default <see cref="IAuthorizationService"/>.
    /// </summary>
    /// <returns>The builder.</returns>
    public virtual AuthorizationBuilder SetFallbackPolicy(AuthorizationPolicy? policy)
    {
        Services.Configure<AuthorizationOptions>(o => o.FallbackPolicy = policy);
        return this;
    }

    /// <summary>
    /// Adds a <see cref="AuthorizationPolicy"/> which can be used by <see cref="IAuthorizationService"/>.
    /// </summary>
    /// <param name="name">The name of this policy.</param>
    /// <param name="policy">The <see cref="AuthorizationPolicy"/>.></param>
    /// <returns>The builder.</returns>
    public virtual AuthorizationBuilder AddPolicy(string name, AuthorizationPolicy policy)
    {
        Services.Configure<AuthorizationOptions>(o => o.AddPolicy(name, policy));
        return this;
    }

    /// <summary>
    /// Add a policy that is built from a delegate with the provided name.
    /// </summary>
    /// <param name="name">The name of the policy.</param>
    /// <param name="configurePolicy">The delegate that will be used to build the policy.</param>
    /// <returns>The builder.</returns>
    public virtual AuthorizationBuilder AddPolicy(string name, Action<AuthorizationPolicyBuilder> configurePolicy)
    {
        Services.Configure<AuthorizationOptions>(o => o.AddPolicy(name, configurePolicy));
        return this;
    }

    /// <summary>
    /// Add a policy that is built from a delegate with the provided name and used as the default policy.
    /// </summary>
    /// <param name="name">The name of the default policy.</param>
    /// <param name="policy">The default <see cref="AuthorizationPolicy"/>.></param>
    /// <returns>The builder.</returns>
    public virtual AuthorizationBuilder AddDefaultPolicy(string name, AuthorizationPolicy policy)
    {
        SetDefaultPolicy(policy);
        return AddPolicy(name, policy);
    }

    /// <summary>
    /// Add a policy that is built from a delegate with the provided name and used as the DefaultPolicy.
    /// </summary>
    /// <param name="name">The name of the DefaultPolicy.</param>
    /// <param name="configurePolicy">The delegate that will be used to build the DefaultPolicy.</param>
    /// <returns>The builder.</returns>
    public virtual AuthorizationBuilder AddDefaultPolicy(string name, Action<AuthorizationPolicyBuilder> configurePolicy)
    {
        ArgumentNullThrowHelper.ThrowIfNull(configurePolicy);

        var policyBuilder = new AuthorizationPolicyBuilder();
        configurePolicy(policyBuilder);
        return AddDefaultPolicy(name, policyBuilder.Build());
    }

    /// <summary>
    /// Add a policy that is built from a delegate with the provided name and used as the FallbackPolicy.
    /// </summary>
    /// <param name="name">The name of the FallbackPolicy.</param>
    /// <param name="policy">The Fallback <see cref="AuthorizationPolicy"/>.></param>
    /// <returns>The builder.</returns>
    public virtual AuthorizationBuilder AddFallbackPolicy(string name, AuthorizationPolicy policy)
    {
        SetFallbackPolicy(policy);
        return AddPolicy(name, policy);
    }

    /// <summary>
    /// Add a policy that is built from a delegate with the provided name and used as the FallbackPolicy.
    /// </summary>
    /// <param name="name">The name of the Fallback policy.</param>
    /// <param name="configurePolicy">The delegate that will be used to build the Fallback policy.</param>
    /// <returns>The builder.</returns>
    public virtual AuthorizationBuilder AddFallbackPolicy(string name, Action<AuthorizationPolicyBuilder> configurePolicy)
    {
        ArgumentNullThrowHelper.ThrowIfNull(configurePolicy);

        var policyBuilder = new AuthorizationPolicyBuilder();
        configurePolicy(policyBuilder);
        return AddFallbackPolicy(name, policyBuilder.Build());
    }
}
