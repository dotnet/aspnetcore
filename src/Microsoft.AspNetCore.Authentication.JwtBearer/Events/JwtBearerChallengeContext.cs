// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;

namespace Microsoft.AspNetCore.Authentication.JwtBearer
{
    public class JwtBearerChallengeContext : BaseJwtBearerContext
    {
        public JwtBearerChallengeContext(HttpContext context, JwtBearerOptions options, AuthenticationProperties properties)
            : base(context, options)
        {
            Properties = properties;
        }

        public AuthenticationProperties Properties { get; }
    }
}
