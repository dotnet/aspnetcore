// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.Twitter;

/// <summary>
/// The Twitter access token retrieved from the access token endpoint.
/// </summary>
public class AccessToken : RequestToken
{
    /// <summary>
    /// Gets or sets the Twitter User ID.
    /// </summary>
    public string UserId { get; set; } = default!;

    /// <summary>
    /// Gets or sets the Twitter screen name.
    /// </summary>
    public string ScreenName { get; set; } = default!;
}
