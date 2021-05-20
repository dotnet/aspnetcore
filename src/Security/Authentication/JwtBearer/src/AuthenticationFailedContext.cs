// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.JwtBearer
{
    /// <summary>
    /// A <see cref="ResultContext{TOptions}"/> when authentication has failed.
    /// </summary>
    public class AuthenticationFailedContext : ResultContext<JwtBearerOptions>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="AuthenticationFailedContext"/>.
        /// </summary>
        /// <inheritdoc />
        public AuthenticationFailedContext(
            HttpContext context,
            AuthenticationScheme scheme,
            JwtBearerOptions options)
            : base(context, scheme, options) { }

        /// <summary>
        /// Gets or sets the exception associated with the authentication failure.
        /// </summary>
        public Exception Exception { get; set; } = default!;
    }
}
