// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Base context for authentication events which deal with a ClaimsPrincipal.
/// </summary>
public abstract class PrincipalContext<TOptions> : PropertiesContext<TOptions> where TOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="scheme">The authentication scheme.</param>
    /// <param name="options">The authentication options associated with the scheme.</param>
    /// <param name="properties">The authentication properties.</param>
    protected PrincipalContext(HttpContext context, AuthenticationScheme scheme, TOptions options, AuthenticationProperties? properties)
        : base(context, scheme, options, properties) { }

    /// <summary>
    /// Gets the <see cref="ClaimsPrincipal"/> containing the user claims.
    /// </summary>
    public virtual ClaimsPrincipal? Principal { get; set; }
}
