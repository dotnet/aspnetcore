// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.CompilationFeatureDetectorTest
{
    public class StartupWithUseSignalR
    {
        public void Configure(IApplicationBuilder app)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            app.UseSignalR(routes =>
            {

            });
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
