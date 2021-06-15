// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http.Features.Authentication;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Used to capture the <see cref="AuthenticateResult"/> from the authorization middleware.
    /// </summary>
    public interface IAuthenticateResultFeature
    {
        /// <summary>
        /// The <see cref="AuthenticateResult"/> from the authorization middleware.
        /// Set to null if the <see cref="IHttpAuthenticationFeature.User"/> property is set after the authorization middleware.
        /// </summary>
        AuthenticateResult? Result { get; set; }
    }
}
