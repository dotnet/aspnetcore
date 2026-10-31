// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Used to build <see cref="AuthenticationScheme"/>s.
/// </summary>
public class AuthenticationSchemeBuilder
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">The name of the scheme being built.</param>
    public AuthenticationSchemeBuilder(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the name of the scheme being built.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the display name for the scheme being built.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="IAuthenticationHandler"/> type responsible for this scheme.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type? HandlerType { get; set; }

    /// <summary>
    /// Builds the <see cref="AuthenticationScheme"/> instance.
    /// </summary>
    /// <returns>The <see cref="AuthenticationScheme"/>.</returns>
    public AuthenticationScheme Build()
    {
        if (HandlerType is null)
        {
            throw new InvalidOperationException($"{nameof(HandlerType)} must be configured to build an {nameof(AuthenticationScheme)}.");
        }

        return new AuthenticationScheme(Name, DisplayName, HandlerType);
    }
}
