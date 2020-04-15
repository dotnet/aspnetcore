// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Used to store the AuthenticateResult for the HttpContext.
    /// </summary>
    public interface IHttpAuthenticationResultFeature
    {
        /// <summary>
        /// The <see cref="AuthenticateResult"/> for the request.
        /// </summary>
        AuthenticateResult Result { get; set; }
    }
}
