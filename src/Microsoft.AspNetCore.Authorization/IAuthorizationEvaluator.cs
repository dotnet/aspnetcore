// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authorization
{
    /// <summary>
    /// Determines whether an authorization request was successful or not.
    /// </summary>
    public interface IAuthorizationEvaluator
    {
        /// <summary>
        /// Returns true, if authorization has failed.
        /// </summary>
        /// <param name="context">The authorization information.</param>
        /// <returns>True if authorization has failed.</returns>
        bool HasFailed(AuthorizationHandlerContext context);

        /// <summary>
        /// Returns true, if authorization has succeeded.
        /// </summary>
        /// <param name="context">The authorization information.</param>
        /// <returns>True if authorization has succeeded.</returns>
        bool HasSucceeded(AuthorizationHandlerContext context);
    }
}
