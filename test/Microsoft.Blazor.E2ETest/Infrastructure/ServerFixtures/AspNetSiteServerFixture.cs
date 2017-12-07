// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.Blazor.E2ETest.Infrastructure.ServerFixtures
{
    public class AspNetSiteServerFixture<TStartup> : WebHostServerFixture
        where TStartup: class
    {
        protected override IWebHost CreateWebHost()
        {
            var sampleSitePath = Path.Combine(
                    FindSolutionDir(),
                    "samples",
                    typeof(TStartup).Assembly.GetName().Name);

            return WebHost.CreateDefaultBuilder()
                .UseStartup<TStartup>()
                .UseContentRoot(sampleSitePath)
                .UseUrls("http://127.0.0.1:0")
                .Build();
        }
    }
}
