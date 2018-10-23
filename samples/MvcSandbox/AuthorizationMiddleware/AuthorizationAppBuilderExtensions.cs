// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using MvcSandbox.AuthorizationMiddleware;

namespace Microsoft.AspNetCore.Builder
{
    public static class AuthorizationAppBuilderExtensions
    {
        public static IApplicationBuilder UseAuthorization(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<AuthorizationMiddleware>();
        }
    }
}