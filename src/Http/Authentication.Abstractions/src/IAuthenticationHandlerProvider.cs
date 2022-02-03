// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Provides the appropriate IAuthenticationHandler instance for the authenticationScheme and request.
/// </summary>
public interface IAuthenticationHandlerProvider
{
    /// <summary>
    /// Returns the handler instance that will be used.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <param name="authenticationScheme">The name of the authentication scheme being handled.</param>
    /// <returns>The handler instance.</returns>
    Task<IAuthenticationHandler?> GetHandlerAsync(HttpContext context, string authenticationScheme);
}
