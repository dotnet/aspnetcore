// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using DevHostServerProgram = Microsoft.AspNetCore.Blazor.Cli.Server.Program;

namespace Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure.ServerFixtures
{
    public class DevHostServerFixture<TProgram> : WebHostServerFixture
    {
        public string PathBase { get; set; }

        protected override IWebHost CreateWebHost()
        {
            var sampleSitePath = FindSampleOrTestSitePath(
                typeof(TProgram).Assembly.GetName().Name);

            return DevHostServerProgram.BuildWebHost(new string[]
            {
                "--urls", "http://127.0.0.1:0",
                "--contentroot", sampleSitePath,
                "--pathbase", PathBase
            });
        }
    }
}
