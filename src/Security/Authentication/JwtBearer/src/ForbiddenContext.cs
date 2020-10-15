// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.JwtBearer
{
    /// <summary>
    /// A <see cref="ResultContext{TOptions}"/> when access to a resource is forbidden.
    /// </summary>
    public class ForbiddenContext : ResultContext<JwtBearerOptions>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ForbiddenContext"/>.
        /// </summary>
        /// <inheritdoc />
        public ForbiddenContext(
            HttpContext context,
            AuthenticationScheme scheme,
            JwtBearerOptions options)
            : base(context, scheme, options) { }
    }
}
