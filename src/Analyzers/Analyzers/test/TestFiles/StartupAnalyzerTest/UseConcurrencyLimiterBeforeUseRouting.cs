// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class UseConcurrencyLimiterBeforeUseRouting
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseFileServer();
            /*MM*/app.UseConcurrencyLimiter();
            app.UseRouting();
            app.UseEndpoints(r => { });
        }
    }
}
