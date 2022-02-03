// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authorization.Policy;

/// <summary>
/// The result of <see cref="IPolicyEvaluator.AuthorizeAsync(AuthorizationPolicy, Authentication.AuthenticateResult, Http.HttpContext, object?)"/>.
/// </summary>
public class PolicyAuthorizationResult
{
    private PolicyAuthorizationResult() { }

    /// <summary>
    /// If true, means the callee should challenge and try again.
    /// </summary>
    public bool Challenged { get; private set; }

    /// <summary>
    /// Authorization was forbidden.
    /// </summary>
    public bool Forbidden { get; private set; }

    /// <summary>
    /// Authorization was successful.
    /// </summary>
    public bool Succeeded { get; private set; }

    /// <summary>
    /// Contains information about why authorization failed.
    /// </summary>
    public AuthorizationFailure? AuthorizationFailure { get; private set; }

    /// <summary>
    ///Indicates that an unauthenticated user requested access to an endpoint that requires authentication.
    /// </summary>
    /// <returns>The <see cref="PolicyAuthorizationResult"/>.</returns>
    public static PolicyAuthorizationResult Challenge()
        => new PolicyAuthorizationResult { Challenged = true };

    /// <summary>
    /// Indicates that the access to a resource was forbidden.
    /// </summary>
    /// <returns>The <see cref="PolicyAuthorizationResult"/>.</returns>
    public static PolicyAuthorizationResult Forbid()
        => Forbid(null);

    /// <summary>
    /// Indicates that the access to a resource was forbidden.
    /// </summary>
    /// <param name="authorizationFailure">Specifies the reason the authorization failed.s</param>
    /// <returns>The <see cref="PolicyAuthorizationResult"/>.</returns>
    public static PolicyAuthorizationResult Forbid(AuthorizationFailure? authorizationFailure)
        => new PolicyAuthorizationResult { Forbidden = true, AuthorizationFailure = authorizationFailure };

    /// <summary>
    /// Indicates a successful authorization.
    /// </summary>
    /// <returns>The <see cref="PolicyAuthorizationResult"/>.</returns>
    public static PolicyAuthorizationResult Success()
        => new PolicyAuthorizationResult { Succeeded = true };
}
