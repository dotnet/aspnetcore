// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
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
}
