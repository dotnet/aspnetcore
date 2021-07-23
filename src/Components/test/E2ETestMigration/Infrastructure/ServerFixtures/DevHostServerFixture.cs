// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using DevHostServerProgram = Microsoft.AspNetCore.Components.WebAssembly.DevServer.Server.Program;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures
{
    public class DevHostServerFixture<TProgram> : WebHostServerFixture
    {
        public string Environment { get; set; }
        public string PathBase { get; set; }
        public string ContentRoot { get; private set; }

        protected override IHost CreateWebHost()
        {
            ContentRoot = FindSampleOrTestSitePath(
                typeof(TProgram).Assembly.FullName);

            var host = "127.0.0.1";

            var args = new List<string>
            {
                "--urls", $"http://{host}:0",
                "--contentroot", ContentRoot,
                "--pathbase", PathBase,
                "--applicationpath", typeof(TProgram).Assembly.Location,
            };

            if (!string.IsNullOrEmpty(Environment))
            {
                args.Add("--environment");
                args.Add(Environment);
            }

            return DevHostServerProgram.BuildWebHost(args.ToArray());
        }
    }
}
