// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Hosting;
using DevHostServerProgram = Microsoft.Blazor.DevHost.Server.Program;

namespace Microsoft.Blazor.E2ETest.Infrastructure.ServerFixtures
{
    public class DevHostServerFixture<TProgram> : WebHostServerFixture
    {
        protected override IWebHost CreateWebHost()
        {
            var sampleSitePath = Path.Combine(
                FindSolutionDir(),
                "samples",
                typeof(TProgram).Assembly.GetName().Name);

            return DevHostServerProgram.BuildWebHost(new string[]
            {
                "--urls", "http://127.0.0.1:0",
                "--contentroot", sampleSitePath
            });
        }
    }
}
