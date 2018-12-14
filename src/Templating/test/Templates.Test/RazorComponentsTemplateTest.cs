// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class RazorComponentsTemplateTest : PuppeteerTestsBase
    {
        public RazorComponentsTemplateTest(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task RazorComponentsTemplate_Works()
        {
            var template = "razorcomponents";
            RunDotNetNew(template);

            // We don't want the Directory.Build.props/targets interfering
            // File.WriteAllText(
            //     Path.Combine(TemplateOutputDir, "Directory.Build.props"),
            //     "<Project />");
            // File.WriteAllText(
            //     Path.Combine(TemplateOutputDir, "Directory.Build.targets"),
            //     "<Project />");

            // Run the "server" project
            ProjectName += ".Server";
            TemplateOutputDir = Path.Combine(TemplateOutputDir, ProjectName);

            await RunPuppeteerTests(template, 8000, 8001);
        }
    }
}
