// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.CompilationFeatureDetectorTest
{
    public class StartupWithUseSignalR
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseSignalR(routes =>
            {

            });
        }
    }
}
