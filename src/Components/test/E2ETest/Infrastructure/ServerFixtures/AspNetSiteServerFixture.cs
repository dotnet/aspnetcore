// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures
{
    public class AspNetSiteServerFixture : WebHostServerFixture
    {
        public delegate IHost BuildWebHost(string[] args);

        public Assembly ApplicationAssembly { get; set; }

        public BuildWebHost BuildWebHostMethod { get; set; }

        public AspNetEnvironment Environment { get; set; } = AspNetEnvironment.Production;

        public List<string> AdditionalArguments { get; set; } = new List<string> { "--test-execution-mode", "server" };

        protected override IHost CreateWebHost()
        {
            if (BuildWebHostMethod == null)
            {
                throw new InvalidOperationException(
                    $"No value was provided for {nameof(BuildWebHostMethod)}");
            }

            var assembly = ApplicationAssembly ?? BuildWebHostMethod.Method.DeclaringType.Assembly;
            var sampleSitePath = FindSampleOrTestSitePath(assembly.FullName);

            return BuildWebHostMethod(new[]
            {
                "--urls", "http://127.0.0.1:0",
                "--contentroot", sampleSitePath,
                "--environment", Environment.ToString(),
            }.Concat(AdditionalArguments).ToArray());
        }
    }
}
