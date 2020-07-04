// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.E2ETesting;
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
            if (E2ETestOptions.Instance.SauceTest)
            {
                host = E2ETestOptions.Instance.Sauce.HostName;
            }

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
