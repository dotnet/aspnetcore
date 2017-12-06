// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;

namespace Microsoft.Blazor.E2ETest.Infrastructure
{
    public class AspNetServerFixture : ServerFixture
    {
        public string StartAndGetUrl(Type startupType)
        {
            var sampleSitePath = Path.Combine(
                FindSolutionDir(),
                "samples",
                startupType.Assembly.GetName().Name);

            var host = WebHost.CreateDefaultBuilder()
                .UseStartup(startupType)
                .UseContentRoot(sampleSitePath)
                .UseUrls("http://127.0.0.1:0")
                .Build();

            return StartAndGetUrl(host);
        }
    }
}