// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.Twitter;

/// <summary>
/// The Twitter request token obtained from the request token endpoint.
/// </summary>
public class RequestToken
{
    /// <summary>
    /// Gets or sets the Twitter request token.
    /// </summary>
    public string Token { get; set; } = default!;

    /// <summary>
    /// Gets or sets the Twitter token secret.
    /// </summary>
    public string TokenSecret { get; set; } = default!;

    /// <summary>
    /// Gets or sets whether the callback was confirmed.
    /// </summary>
    public bool CallbackConfirmed { get; set; }

    /// <summary>
    /// Gets or sets a property bag for common authentication properties.
    /// </summary>
    public AuthenticationProperties Properties { get; set; } = default!;
}
