// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test.SpaTemplateTest
{
    public class SpaTemplateTestBase : PuppeteerTestsBase
    {
        public SpaTemplateTestBase(ITestOutputHelper output) : base(output)
        {
        }

        // Rather than using [Theory] to pass each of the different values for 'template',
        // it's important to distribute the SPA template tests over different test classes
        // so they can be run in parallel. Xunit doesn't parallelize within a test class.
        protected async Task SpaTemplateImpl(string template, int httpPort, int httpsPort, bool noHttps = false)
        {
            RunDotNetNew(template, noHttps: noHttps);

            var clientAppSubdirPath = Path.Combine(TemplateOutputDir, "ClientApp");
            Assert.True(File.Exists(Path.Combine(clientAppSubdirPath, "package.json")), "Missing a package.json");

            await RunPuppeteerTests(template, httpPort, httpsPort);

            Npm.Test(Output, clientAppSubdirPath);
        }
    }
}
