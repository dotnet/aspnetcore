// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Authorization
{
    /// <summary>
    /// Encapsulates the result of <see cref="IAuthorizationService.AuthorizeAsync(ClaimsPrincipal, object, IEnumerable{IAuthorizationRequirement})"/>.
    /// </summary>
    public class AuthorizationResult
    {
        private AuthorizationResult() { }

        /// <summary>
        /// True if authorization was successful.
        /// </summary>
        public bool Succeeded { get; private set; }

        /// <summary>
        /// Contains information about why authorization failed.
        /// </summary>
        public AuthorizationFailure Failure { get; private set; }

        /// <summary>
        /// Returns a successful result.
        /// </summary>
        /// <returns>A successful result.</returns>
        public static AuthorizationResult Success() => new AuthorizationResult { Succeeded = true };

        public static AuthorizationResult Failed(AuthorizationFailure failure) => new AuthorizationResult { Failure = failure };

        public static AuthorizationResult Failed() => new AuthorizationResult { Failure = AuthorizationFailure.ExplicitFail() };

    }
}
