// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.Blazor.E2ETest.Infrastructure
{
    public class StaticServerFixture : ServerFixture
    {
        public void Start(string sampleSiteName)
        {
            var sampleSitePath = Path.Combine(
                FindSolutionDir(),
                "samples",
                sampleSiteName);

            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(sampleSitePath)
                .UseWebRoot(string.Empty)
                .UseStartup<Startup>()
                .UseUrls("http://127.0.0.1:0")
                .Build();

            Start(host);
        }

        private class Startup
        {
            public void Configure(IApplicationBuilder app)
            {
                app.UseFileServer();
            }
        }
    }
}
