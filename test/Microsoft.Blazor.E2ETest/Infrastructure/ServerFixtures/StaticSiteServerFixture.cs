// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.Blazor.E2ETest.Infrastructure.ServerFixtures
{
    public class StaticSiteServerFixture : WebHostServerFixture
    {
        public string SampleSiteName { get; set; }

        protected override IWebHost CreateWebHost()
        {
            if (string.IsNullOrEmpty(SampleSiteName))
            {
                throw new InvalidOperationException($"No value was provided for {nameof(SampleSiteName)}");
            }

            var sampleSitePath = Path.Combine(
                    FindSolutionDir(),
                    "samples",
                    SampleSiteName);

            return new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(sampleSitePath)
                .UseWebRoot(string.Empty)
                .UseStartup<StaticSiteStartup>()
                .UseUrls("http://127.0.0.1:0")
                .Build();
        }

        private class StaticSiteStartup
        {
            public void Configure(IApplicationBuilder app)
            {
                app.UseFileServer();
            }
        }
    }
}
