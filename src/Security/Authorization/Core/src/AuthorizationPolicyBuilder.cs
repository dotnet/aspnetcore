// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// Used for building policies.
/// </summary>
public class AuthorizationPolicyBuilder
{
    private static readonly DenyAnonymousAuthorizationRequirement _denyAnonymousAuthorizationRequirement = new();

    /// <summary>
    /// Creates a new instance of <see cref="AuthorizationPolicyBuilder"/>
    /// </summary>
    /// <param name="authenticationSchemes">An array of authentication schemes the policy should be evaluated against.</param>
    public AuthorizationPolicyBuilder(params string[] authenticationSchemes)
    {
        AddAuthenticationSchemes(authenticationSchemes);
    }

    /// <summary>
    /// Creates a new instance of <see cref="AuthorizationPolicyBuilder"/>.
    /// </summary>
    /// <param name="policy">The <see cref="AuthorizationPolicy"/> to copy.</param>
    public AuthorizationPolicyBuilder(AuthorizationPolicy policy)
    {
        Combine(policy);
    }

    /// <summary>
    /// Gets or sets a list of <see cref="IAuthorizationRequirement"/>s which must succeed for
    /// this policy to be successful.
    /// </summary>
    public IList<IAuthorizationRequirement> Requirements { get; set; } = new List<IAuthorizationRequirement>();

    /// <summary>
    /// Gets or sets a list authentication schemes the <see cref="AuthorizationPolicyBuilder.Requirements"/>
    /// are evaluated against.
    /// <para>
    /// When not specified, the requirements are evaluated against default schemes.
    /// </para>
    /// </summary>
    public IList<string> AuthenticationSchemes { get; set; } = new List<string>();

    /// <summary>
    /// Adds the specified authentication <paramref name="schemes"/> to the
    /// <see cref="AuthorizationPolicyBuilder.AuthenticationSchemes"/> for this instance.
    /// </summary>
    /// <param name="schemes">The schemes to add.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AuthorizationPolicyBuilder AddAuthenticationSchemes(params string[] schemes) => AddAuthenticationSchemesCore(schemes);

    private AuthorizationPolicyBuilder AddAuthenticationSchemesCore(IEnumerable<string> schemes)
    {
        foreach (var authType in schemes)
        {
            AuthenticationSchemes.Add(authType);
        }
        return this;
    }

    /// <summary>
    /// Adds the specified <paramref name="requirements"/> to the
    /// <see cref="AuthorizationPolicyBuilder.Requirements"/> for this instance.
    /// </summary>
    /// <param name="requirements">The authorization requirements to add.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AuthorizationPolicyBuilder AddRequirements(params IAuthorizationRequirement[] requirements) => AddRequirementsCore(requirements);

    private AuthorizationPolicyBuilder AddRequirementsCore(IEnumerable<IAuthorizationRequirement> requirements)
    {
        foreach (var req in requirements)
        {
            Requirements.Add(req);
        }
        return this;
    }

    /// <summary>
    /// Combines the specified <paramref name="policy"/> into the current instance.
    /// </summary>
    /// <param name="policy">The <see cref="AuthorizationPolicy"/> to combine.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AuthorizationPolicyBuilder Combine(AuthorizationPolicy policy)
    {
        ArgumentNullThrowHelper.ThrowIfNull(policy);

        AddAuthenticationSchemesCore(policy.AuthenticationSchemes);
        AddRequirementsCore(policy.Requirements);
        return this;
    }

    /// <summary>
    /// Adds a <see cref="ClaimsAuthorizationRequirement"/> to the current instance which requires
    /// that the current user has the specified claim and that the claim value must be one of the allowed values.
    /// </summary>
    /// <param name="claimType">The claim type required.</param>
    /// <param name="allowedValues">Optional list of claim values. If specified, the claim must match one or more of these values.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AuthorizationPolicyBuilder RequireClaim(string claimType, params string[] allowedValues)
    {
        ArgumentNullThrowHelper.ThrowIfNull(claimType);

        return RequireClaim(claimType, (IEnumerable<string>)allowedValues);
    }

    /// <summary>
    /// Adds a <see cref="ClaimsAuthorizationRequirement"/> to the current instance which requires
    /// that the current user has the specified claim and that the claim value must be one of the allowed values.
    /// </summary>
    /// <param name="claimType">The claim type required.</param>
    /// <param name="allowedValues">Optional list of claim values. If specified, the claim must match one or more of these values.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AuthorizationPolicyBuilder RequireClaim(string claimType, IEnumerable<string> allowedValues)
    {
        ArgumentNullThrowHelper.ThrowIfNull(claimType);

        Requirements.Add(new ClaimsAuthorizationRequirement(claimType, allowedValues));
        return this;
    }

    /// <summary>
    /// Adds a <see cref="ClaimsAuthorizationRequirement"/> to the current instance which requires
    /// that the current user has the specified claim.
    /// </summary>
    /// <param name="claimType">The claim type required, with no restrictions on claim value.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AuthorizationPolicyBuilder RequireClaim(string claimType)
    {
        ArgumentNullThrowHelper.ThrowIfNull(claimType);

        Requirements.Add(new ClaimsAuthorizationRequirement(claimType, allowedValues: null));
        return this;
    }

    /// <summary>
    /// Adds a <see cref="RolesAuthorizationRequirement"/> to the current instance which enforces that the current user
    /// must have at least one of the specified roles.
    /// </summary>
    /// <param name="roles">The allowed roles.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AuthorizationPolicyBuilder RequireRole(params string[] roles)
    {
        ArgumentNullThrowHelper.ThrowIfNull(roles);

        return RequireRole((IEnumerable<string>)roles);
    }

    /// <summary>
    /// Adds a <see cref="RolesAuthorizationRequirement"/> to the current instance which enforces that the current user
    /// must have at least one of the specified roles.
    /// </summary>
    /// <param name="roles">The allowed roles.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AuthorizationPolicyBuilder RequireRole(IEnumerable<string> roles)
    {
        ArgumentNullThrowHelper.ThrowIfNull(roles);

        Requirements.Add(new RolesAuthorizationRequirement(roles));
        return this;
    }

    /// <summary>
    /// Adds a <see cref="NameAuthorizationRequirement"/> to the current instance which enforces that the current user matches the specified name.
    /// </summary>
    /// <param name="userName">The user name the current user must have.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AuthorizationPolicyBuilder RequireUserName(string userName)
    {
        ArgumentNullThrowHelper.ThrowIfNull(userName);

        Requirements.Add(new NameAuthorizationRequirement(userName));
        return this;
    }

    /// <summary>
    /// Adds <see cref="DenyAnonymousAuthorizationRequirement"/> to the current instance which enforces that the current user is authenticated.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AuthorizationPolicyBuilder RequireAuthenticatedUser()
    {
        Requirements.Add(_denyAnonymousAuthorizationRequirement);
        return this;
    }

    /// <summary>
    /// Adds an <see cref="AssertionRequirement"/> to the current instance.
    /// </summary>
    /// <param name="handler">The handler to evaluate during authorization.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AuthorizationPolicyBuilder RequireAssertion(Func<AuthorizationHandlerContext, bool> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(handler);

        Requirements.Add(new AssertionRequirement(handler));
        return this;
    }

    /// <summary>
    /// Adds an <see cref="AssertionRequirement"/> to the current instance.
    /// </summary>
    /// <param name="handler">The handler to evaluate during authorization.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AuthorizationPolicyBuilder RequireAssertion(Func<AuthorizationHandlerContext, Task<bool>> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(handler);

        Requirements.Add(new AssertionRequirement(handler));
        return this;
    }

    /// <summary>
    /// Builds a new <see cref="AuthorizationPolicy"/> from the requirements
    /// in this instance.
    /// </summary>
    /// <returns>
    /// A new <see cref="AuthorizationPolicy"/> built from the requirements in this instance.
    /// </returns>
    public AuthorizationPolicy Build()
    {
        return new AuthorizationPolicy(Requirements, AuthenticationSchemes.Distinct());
    }
}
