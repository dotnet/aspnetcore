// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class RazorClassLibraryTemplateTest : LoggedTest
    {
        public RazorClassLibraryTemplateTest(ProjectFactoryFixture projectFactory)
        {
            ProjectFactory = projectFactory;
            Output = new TestOutputLogger(Logger);
        }

        public ProjectFactoryFixture ProjectFactory { get; }
        public ITestOutputHelper Output { get; }

        [Fact]
        public async Task RazorClassLibraryTemplate_WithViews_Async()
        {
            var project = await ProjectFactory.GetOrCreateProject("razorclasslibwithviews", Output);

            var createResult = await project.RunDotNetNewAsync("razorclasslib", args: new[] { "--support-pages-and-views", "true" });
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", project, createResult));

            var publishResult = await project.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", project, publishResult));

            // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
            // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
            // later, while the opposite is not true.

            var buildResult = await project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", project, buildResult));
        }

        [Fact]
        public async Task RazorClassLibraryTemplateAsync()
        {
            var project = await ProjectFactory.GetOrCreateProject("razorclasslib", Output);

            var createResult = await project.RunDotNetNewAsync("razorclasslib");
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", project, createResult));

            var publishResult = await project.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", project, publishResult));

            // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
            // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
            // later, while the opposite is not true.

            var buildResult = await project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", project, buildResult));
        }
    }
}
