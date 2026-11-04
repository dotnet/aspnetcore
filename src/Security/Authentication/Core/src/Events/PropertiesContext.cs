// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Base context for authentication events which contain <see cref="AuthenticationProperties"/>.
/// </summary>
public abstract class PropertiesContext<TOptions> : BaseContext<TOptions> where TOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="scheme">The authentication scheme.</param>
    /// <param name="options">The authentication options associated with the scheme.</param>
    /// <param name="properties">The authentication properties.</param>
    protected PropertiesContext(HttpContext context, AuthenticationScheme scheme, TOptions options, AuthenticationProperties? properties)
        : base(context, scheme, options)
    {
        Properties = properties ?? new AuthenticationProperties();
    }

    /// <summary>
    /// Gets or sets the <see cref="AuthenticationProperties"/>.
    /// </summary>
    public virtual AuthenticationProperties Properties { get; protected set; }
}
