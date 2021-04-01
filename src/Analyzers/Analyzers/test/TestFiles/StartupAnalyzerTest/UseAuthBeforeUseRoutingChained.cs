// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest {
    public class UseAuthBeforeUseRoutingChained
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseFileServer()
               .UseAuthorization()
               .UseRouting()
               .UseEndpoints(r => { });
        }
    }
}
