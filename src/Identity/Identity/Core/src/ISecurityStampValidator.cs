// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Provides an abstraction for a validating a security stamp of an incoming identity, and regenerating or rejecting the 
    /// identity based on the validation result.
    /// </summary>
    public interface ISecurityStampValidator
    {
        /// <summary>
        /// Validates a security stamp of an identity as an asynchronous operation, and rebuilds the identity if the validation succeeds, otherwise rejects
        /// the identity.
        /// </summary>
        /// <param name="context">The context containing the <see cref="System.Security.Claims.ClaimsPrincipal"/>
        /// and <see cref="Http.Authentication.AuthenticationProperties"/> to validate.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous validation operation.</returns>
        Task ValidateAsync(CookieValidatePrincipalContext context);
    }
}

