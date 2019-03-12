// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures
{
    public class AspNetSiteServerFixture : WebHostServerFixture
    {
        public delegate IWebHost BuildWebHost(string[] args);

        public BuildWebHost BuildWebHostMethod { get; set; }

        public AspNetEnvironment Environment { get; set; } = AspNetEnvironment.Production;

        protected override IWebHost CreateWebHost()
        {
            if (BuildWebHostMethod == null)
            {
                throw new InvalidOperationException(
                    $"No value was provided for {nameof(BuildWebHostMethod)}");
            }

            var sampleSitePath = FindSampleOrTestSitePath(
                BuildWebHostMethod.Method.DeclaringType.Assembly.FullName);

            return BuildWebHostMethod(new[]
            {
                "--urls", "http://127.0.0.1:0",
                "--contentroot", sampleSitePath,
                "--environment", Environment.ToString(),
            });
        }
    }
}
