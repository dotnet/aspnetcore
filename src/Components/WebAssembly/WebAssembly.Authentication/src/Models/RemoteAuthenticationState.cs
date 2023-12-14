// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

/// <summary>
/// Represents the minimal amount of authentication state to be preserved during authentication operations.
/// </summary>
public class RemoteAuthenticationState
{
    /// <summary>
    /// Gets or sets the URL to which the application should redirect after a successful authentication operation.
    /// It must be a url within the page.
    /// </summary>
    public string? ReturnUrl { get; set; }
}
