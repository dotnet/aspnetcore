// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Threading.Tasks;
using ProjectTemplates.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace ProjectTemplates.Basic.Tests
{
    public class RazorComponentsTemplateTest
    {
        public RazorComponentsTemplateTest(ProjectFactoryFixture projectFactory, ITestOutputHelper output)
        {
            ProjectFactory = projectFactory;
            Output = output;
        }

        public ProjectFactoryFixture ProjectFactory { get; set; }

        public Project Project { get; private set; }

        public ITestOutputHelper Output { get; }

        [Fact]
        public async Task RazorComponentsTemplateWorks()
        {
            Project = await ProjectFactory.GetOrCreateProject("blazorserverside", Output);

            var createResult = await Project.RunDotNetNewAsync("blazorserverside");
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", Project, createResult));

            var publishResult = await Project.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", Project, publishResult));

            // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
            // The output from publish will go into bin/Release/netcoreapp3.0/publish and won't be affected by calling build
            // later, while the opposite is not true.

            var buildResult = await Project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", Project, buildResult));

            using (var aspNetProcess = Project.StartBuiltProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", Project, aspNetProcess.Process));

                await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
            }

            using (var aspNetProcess = Project.StartPublishedProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", Project, aspNetProcess.Process));

                await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
            }
        }
    }
}
