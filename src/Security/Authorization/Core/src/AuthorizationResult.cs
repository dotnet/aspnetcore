// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// Encapsulates the result of <see cref="IAuthorizationService.AuthorizeAsync(ClaimsPrincipal, object, IEnumerable{IAuthorizationRequirement})"/>.
/// </summary>
public class AuthorizationResult
{
    private static readonly AuthorizationResult _succeededResult = new() { Succeeded = true };
    private static readonly AuthorizationResult _failedResult = new() { Failure = AuthorizationFailure.ExplicitFail() };

    private AuthorizationResult() { }

    /// <summary>
    /// True if authorization was successful.
    /// </summary>
    [MemberNotNullWhen(false, nameof(Failure))]
    public bool Succeeded { get; private set; }

    /// <summary>
    /// Contains information about why authorization failed.
    /// </summary>
    public AuthorizationFailure? Failure { get; private set; }

    /// <summary>
    /// Returns a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static AuthorizationResult Success() => _succeededResult;

    /// <summary>
    /// Creates a failed authorization result.
    /// </summary>
    /// <param name="failure">Contains information about why authorization failed.</param>
    /// <returns>The <see cref="AuthorizationResult"/>.</returns>
    public static AuthorizationResult Failed(AuthorizationFailure failure) => new AuthorizationResult { Failure = failure };

    /// <summary>
    /// Creates a failed authorization result.
    /// </summary>
    /// <returns>The <see cref="AuthorizationResult"/>.</returns>
    public static AuthorizationResult Failed() => _failedResult;
}
