// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

/// <summary>
/// Represents a contract for services capable of provisioning access tokens for an application.
/// </summary>
public interface IAccessTokenProvider
{
    /// <summary>
    /// Tries to get an access token for the current user with the default set of permissions.
    /// </summary>
    /// <returns>A <see cref="ValueTask{AccessTokenResult}"/> that will contain the <see cref="AccessTokenResult"/> when completed.</returns>
    ValueTask<AccessTokenResult> RequestAccessToken();

    /// <summary>
    /// Tries to get an access token with the options specified in <see cref="AccessTokenRequestOptions"/>.
    /// </summary>
    /// <param name="options">The <see cref="AccessTokenRequestOptions"/> for provisioning the access token.</param>
    /// <returns>A <see cref="ValueTask{AccessTokenResult}"/> that will contain the <see cref="AccessTokenResult"/> when completed.</returns>
    ValueTask<AccessTokenResult> RequestAccessToken(AccessTokenRequestOptions options);
}
