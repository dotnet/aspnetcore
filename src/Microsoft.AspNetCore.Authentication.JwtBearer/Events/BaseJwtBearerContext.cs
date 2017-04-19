// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.JwtBearer
{
    public class BaseJwtBearerContext : BaseControlContext
    {
        public BaseJwtBearerContext(HttpContext context, AuthenticationScheme scheme, JwtBearerOptions options)
            : base(context)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (scheme == null)
            {
                throw new ArgumentNullException(nameof(scheme));
            }

            Options = options;
            Scheme = scheme;
        }

        public JwtBearerOptions Options { get; }

        public AuthenticationScheme Scheme { get; }
    }
}