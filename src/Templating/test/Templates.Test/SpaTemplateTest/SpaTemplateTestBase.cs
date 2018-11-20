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
        protected async Task SpaTemplateImpl(string targetFrameworkOverride, string template, int httpPort, int httpsPort, bool noHttps = false)
        {
            RunDotNetNew(template, targetFrameworkOverride, noHttps: noHttps);

            // For some SPA templates, the NPM root directory is './ClientApp'. In other
            // templates it's at the project root. Strictly speaking we shouldn't have
            // to do the NPM restore in tests because it should happen automatically at
            // build time, but by doing it up front we can avoid having multiple NPM
            // installs run concurrently which otherwise causes errors when tests run
            // in parallel.
            var clientAppSubdirPath = Path.Combine(TemplateOutputDir, "ClientApp");
            Assert.True(File.Exists(Path.Combine(clientAppSubdirPath, "package.json")), "Missing a package.json");
            
            await RunPuppeteerTests(template, targetFrameworkOverride, httpPort, httpsPort);
            
            Npm.Test(Output, clientAppSubdirPath);
        }
    }
}
