// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Session;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Builder
{
    public static class SessionMiddlewareExtensions
    {
        public static IApplicationBuilder UseSession([NotNull] this IApplicationBuilder app)
        {
            return app.UseMiddleware<SessionMiddleware>();
        }
    }
}