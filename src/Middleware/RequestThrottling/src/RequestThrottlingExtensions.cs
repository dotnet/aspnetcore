// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Aspnetcore.RequestThrottling;

namespace Microsoft.AspNetCore.Builder
{
    public static class RequestThrottlingExtensions
    {
        public static IApplicationBuilder UseRequestThrottling(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<RequestThrottlingMiddleware>();
        }
    }
}
