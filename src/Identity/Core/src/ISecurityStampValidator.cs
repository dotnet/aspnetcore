// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Provides an abstraction for a validating a security stamp of an incoming identity, and regenerating or rejecting the
/// identity based on the validation result.
/// </summary>
public interface ISecurityStampValidator
{
    /// <summary>
    /// Validates a security stamp of an identity as an asynchronous operation, and rebuilds the identity if the validation succeeds, otherwise rejects
    /// the identity.
    /// </summary>
    /// <param name="context">The context containing the <see cref="System.Security.Claims.ClaimsPrincipal"/>
    /// and <see cref="AuthenticationProperties"/> to validate.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous validation operation.</returns>
    Task ValidateAsync(CookieValidatePrincipalContext context);
}

