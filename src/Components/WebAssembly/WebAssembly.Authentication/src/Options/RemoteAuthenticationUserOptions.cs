// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics.CodeAnalysis;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

/// <summary>
/// Represents options to use when configuring the <see cref="System.Security.Claims.ClaimsPrincipal"/> for a user.
/// </summary>
[DynamicallyAccessedMembers(JsonSerialized)] 
public class RemoteAuthenticationUserOptions
{
    /// <summary>
    /// Gets or sets the claim type to use for the user name.
    /// </summary>
    public string NameClaim { get; set; } = "name";

    /// <summary>
    /// Gets or sets the claim type to use for the user roles.
    /// </summary>
    public string RoleClaim { get; set; }

    /// <summary>
    /// Gets or sets the claim type to use for the user scopes.
    /// </summary>
    public string ScopeClaim { get; set; }

    /// <summary>
    /// Gets or sets the value to use for the <see cref="System.Security.Claims.ClaimsIdentity.AuthenticationType"/>.
    /// </summary>
    public string AuthenticationType { get; set; }
}
