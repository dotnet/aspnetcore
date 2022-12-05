// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

/// <summary>
/// Represents an access token for a given user and scopes.
/// </summary>
public class AccessToken
{
    /// <summary>
    /// Gets or sets the list of granted scopes for the token.
    /// </summary>
    public IReadOnlyList<string> GrantedScopes { get; set; } = default!;

    /// <summary>
    /// Gets the expiration time of the token.
    /// </summary>
    public DateTimeOffset Expires { get; set; }

    /// <summary>
    /// Gets the serialized representation of the token.
    /// </summary>
    public string Value { get; set; } = default!;
}
