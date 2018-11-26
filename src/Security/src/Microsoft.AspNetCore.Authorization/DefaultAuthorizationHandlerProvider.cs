// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authorization
{
    /// <summary>
    /// The default implementation of a handler provider,
    /// which provides the <see cref="IAuthorizationHandler"/>s for an authorization request.
    /// </summary>
    public class DefaultAuthorizationHandlerProvider : IAuthorizationHandlerProvider
    {
        private readonly IEnumerable<IAuthorizationHandler> _handlers;

        /// <summary>
        /// Creates a new instance of <see cref="DefaultAuthorizationHandlerProvider"/>.
        /// </summary>
        /// <param name="handlers">The <see cref="IAuthorizationHandler"/>s.</param>
        public DefaultAuthorizationHandlerProvider(IEnumerable<IAuthorizationHandler> handlers)
        {
            if (handlers == null)
            {
                throw new ArgumentNullException(nameof(handlers));
            }

            _handlers = handlers;
        }

        public Task<IEnumerable<IAuthorizationHandler>> GetHandlersAsync(AuthorizationHandlerContext context)
            => Task.FromResult(_handlers);
    }
}
