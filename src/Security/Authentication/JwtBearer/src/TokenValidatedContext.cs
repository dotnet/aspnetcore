// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Authentication.JwtBearer
{
    /// <summary>
    /// A context for <see cref="JwtBearerEvents.OnTokenValidated"/>.
    /// </summary>
    public class TokenValidatedContext : ResultContext<JwtBearerOptions>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="TokenValidatedContext"/>.
        /// </summary>
        /// <inheritdoc />
        public TokenValidatedContext(
            HttpContext context,
            AuthenticationScheme scheme,
            JwtBearerOptions options)
            : base(context, scheme, options) { }

        /// <summary>
        /// Gets or sets the validated security token.
        /// </summary>
        public SecurityToken SecurityToken { get; set; } = default!;
    }
}
