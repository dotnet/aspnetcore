// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Provides methods to create middleware.
    /// </summary>
    public interface IMiddlewareFactory
    {
        /// <summary>
        /// Creates a middleware instance for each request.
        /// </summary>
        /// <param name="middlewareType">The concrete <see cref="Type"/> of the <see cref="IMiddleware"/>.</param>
        /// <returns>The <see cref="IMiddleware"/> instance.</returns>
        IMiddleware Create(Type middlewareType);

        /// <summary>
        /// Releases a <see cref="IMiddleware"/> instance at the end of each request.
        /// </summary>
        /// <param name="middleware">The <see cref="IMiddleware"/> instance to release.</param>
        void Release(IMiddleware middleware);
    }
}
