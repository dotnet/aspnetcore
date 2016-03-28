// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features.Authentication;

namespace Microsoft.AspNetCore.Authentication
{
    internal static class HttpContextExtensions
    {
        internal static IHttpAuthenticationFeature GetAuthentication(this HttpContext context)
        {
            var auth = context.Features.Get<IHttpAuthenticationFeature>();
            if (auth == null)
            {
                auth = new HttpAuthenticationFeature();
                context.Features.Set(auth);
            }
            return auth;
        }
    }
}
