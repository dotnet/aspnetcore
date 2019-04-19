// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Negotiate
{
    public class TokenValidatedContext : ResultContext<NegotiateOptions>
    {
        public TokenValidatedContext(
            HttpContext context,
            AuthenticationScheme scheme,
            NegotiateOptions options)
            : base(context, scheme, options) { }
    }
}
