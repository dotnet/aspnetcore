// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore
{
    /// <summary>
    /// Processes requests to execute migrations operations. The middleware will listen for requests to the path configured in the supplied options.
    /// </summary>
    [Obsolete("This is obsolete and will be removed in a future version. Use the Package Manager Console in Visual Studio or dotnet-ef tool on the command line to apply migrations.", error:true)]
    public class MigrationsEndPointMiddleware
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MigrationsEndPointMiddleware"/> class
        /// </summary>
        /// <param name="next">Delegate to execute the next piece of middleware in the request pipeline.</param>
        /// <param name="logger">The <see cref="Logger{T}"/> to write messages to.</param>
        /// <param name="options">The options to control the behavior of the middleware.</param>
        public MigrationsEndPointMiddleware(
            RequestDelegate next,
            ILogger<MigrationsEndPointMiddleware> logger,
            IOptions<MigrationsEndPointOptions> options)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Process an individual request.
        /// </summary>
        /// <param name="context">The context for the current request.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public virtual Task Invoke(HttpContext context)
        {
            throw new NotImplementedException();
        }
    }
}
