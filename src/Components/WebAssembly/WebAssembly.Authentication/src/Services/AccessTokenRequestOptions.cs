// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

/// <summary>
/// Represents the options for provisioning an access token on behalf of a user.
/// </summary>
public class AccessTokenRequestOptions
{
    /// <summary>
    /// Gets or sets the list of scopes to request for the token.
    /// </summary>
    public IEnumerable<string>? Scopes { get; set; }

    /// <summary>
    /// Gets or sets a specific return url to use for returning the user back to the application if it needs to be
    /// redirected elsewhere in order to provision the token.
    /// </summary>
    public string? ReturnUrl { get; set; }
}
