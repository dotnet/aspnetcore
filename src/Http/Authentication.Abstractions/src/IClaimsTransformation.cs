// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Used by the <see cref="IAuthenticationService"/> for claims transformation.
    /// </summary>
    public interface IClaimsTransformation
    {
        /// <summary>
        /// Provides a central transformation point to change the specified principal. 
        /// Note: this will be run on each AuthenticateAsync call, so its safer to
        /// return a new ClaimsPrincipal if your transformation is not idempotent.
        /// </summary>
        /// <param name="principal">The <see cref="ClaimsPrincipal"/> to transform.</param>
        /// <returns>The transformed principal.</returns>
        Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal);
    }
}