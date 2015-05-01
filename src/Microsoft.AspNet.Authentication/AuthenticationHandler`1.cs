// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Logging;
using Microsoft.Framework.WebEncoders;

namespace Microsoft.AspNet.Authentication
{
    /// <summary>
    /// Base class for the per-request work performed by most authentication middleware.
    /// </summary>
    /// <typeparam name="TOptions">Specifies which type for of AuthenticationOptions property</typeparam>
    public abstract class AuthenticationHandler<TOptions> : AuthenticationHandler where TOptions : AuthenticationOptions
    {
        protected TOptions Options { get; private set; }

        /// <summary>
        /// Initialize is called once per request to contextualize this instance with appropriate state.
        /// </summary>
        /// <param name="options">The original options passed by the application control behavior</param>
        /// <param name="context">The utility object to observe the current request and response</param>
        /// <param name="logger">The logging factory used to create loggers</param>
        /// <returns>async completion</returns>
        public Task Initialize(TOptions options, HttpContext context, ILogger logger, IUrlEncoder encoder)
        {
            Options = options;
            return BaseInitializeAsync(options, context, logger, encoder);
        }
    }
}
