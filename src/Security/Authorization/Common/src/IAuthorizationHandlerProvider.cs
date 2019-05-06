// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authorization
{
    /// <summary>
    /// A type which can provide the <see cref="IAuthorizationHandler"/>s for an authorization request.
    /// </summary>
    public interface IAuthorizationHandlerProvider
    {
        /// <summary>
        /// Return the handlers that will be called for the authorization request.
        /// </summary>
        /// <param name="context">The <see cref="AuthorizationHandlerContext"/>.</param>
        /// <returns>The list of handlers.</returns>
        Task<IEnumerable<IAuthorizationHandler>> GetHandlersAsync(AuthorizationHandlerContext context);
    }
}
