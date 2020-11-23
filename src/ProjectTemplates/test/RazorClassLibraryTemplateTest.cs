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
        }

        public ProjectFactoryFixture ProjectFactory { get; }
        private ITestOutputHelper _output;
        public ITestOutputHelper Output
        {
            get
            {
                if (_output == null)
                {
                    _output = new TestOutputLogger(Logger);
                }
                return _output;
            }
        }

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

        [ConditionalFact]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/28090", Queues = "Windows.10.Arm64v8.Open;(Debian.9.Arm64.Open)Ubuntu.1804.Armarch.Open@mcr.microsoft.com/dotnet-buildtools/prereqs:debian-9-helix-arm64v8-a12566d-20190807161036")]
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
