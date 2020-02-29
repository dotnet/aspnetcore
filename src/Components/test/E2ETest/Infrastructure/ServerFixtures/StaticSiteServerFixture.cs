// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures
{
    // Although this is not used for anything meaningful related to Blazor yet, it
    // will be used later when there's a mechanism for publishing standalone Blazor
    // apps as a set of purely static files and we need E2E testing on the result.

    public class StaticSiteServerFixture : WebHostServerFixture
    {
        public string SampleSiteName { get; set; }

        protected override IHost CreateWebHost()
        {
            if (string.IsNullOrEmpty(SampleSiteName))
            {
                throw new InvalidOperationException($"No value was provided for {nameof(SampleSiteName)}");
            }

            var sampleSitePath = FindSampleOrTestSitePath(SampleSiteName);

            return new HostBuilder()
                .ConfigureWebHost(webHostBuilder => webHostBuilder
                    .UseKestrel()
                    .UseContentRoot(sampleSitePath)
                    .UseWebRoot(string.Empty)
                    .UseStartup<StaticSiteStartup>()
                    .UseUrls("http://127.0.0.1:0"))
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
