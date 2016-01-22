// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.TestConfiguration;

namespace Microsoft.AspNetCore.Builder
{
    public static class BuilderExtensions
    {
        // Should be added to the pipeline as early as possible.
        public static IApplicationBuilder UseCultureReplacer(this IApplicationBuilder app)
        {
            return app.UseMiddleware<CultureReplacerMiddleware>();
        }
    }
}