// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// Determines whether an authorization request was successful or not.
/// </summary>
public interface IAuthorizationEvaluator
{
    /// <summary>
    /// Determines whether the authorization result was successful or not.
    /// </summary>
    /// <param name="context">The authorization information.</param>
    /// <returns>The <see cref="AuthorizationResult"/>.</returns>
    AuthorizationResult Evaluate(AuthorizationHandlerContext context);
}
