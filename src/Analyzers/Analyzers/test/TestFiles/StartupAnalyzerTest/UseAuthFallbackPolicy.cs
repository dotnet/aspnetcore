// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class UseAuthFallbackPolicy
    {
        public void Configure(IApplicationBuilder app)
        {
            // This sort of setup would be useful if the user wants to use Auth for non-endpoint content to be handled using the Fallback policy, while
            // using the second instance for regular endpoint routing based auth. We do not want to produce a warning in this case.
            app.UseAuthorization();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(r => { });
        }
    }
}
