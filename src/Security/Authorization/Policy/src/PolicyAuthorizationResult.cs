// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authorization.Policy
{
    public class PolicyAuthorizationResult
    {
        private PolicyAuthorizationResult() { }

        /// <summary>
        /// If true, means the callee should challenge and try again.
        /// </summary>
        public bool Challenged { get; private set; }

        /// <summary>
        /// Authorization was forbidden.
        /// </summary>
        public bool Forbidden { get; private set; }

        /// <summary>
        /// Authorization was successful.
        /// </summary>
        public bool Succeeded { get; private set; }

        /// <summary>
        /// Contains information about why authorization failed.
        /// </summary>
        public AuthorizationFailure? AuthorizationFailure { get; private set; }

        public static PolicyAuthorizationResult Challenge()
            => new PolicyAuthorizationResult { Challenged = true };

        public static PolicyAuthorizationResult Forbid()
            => Forbid(null);

        public static PolicyAuthorizationResult Forbid(AuthorizationFailure? authorizationFailure)
            => new PolicyAuthorizationResult { Forbidden = true, AuthorizationFailure = authorizationFailure };

        public static PolicyAuthorizationResult Success()
            => new PolicyAuthorizationResult { Succeeded = true };

    }
}
