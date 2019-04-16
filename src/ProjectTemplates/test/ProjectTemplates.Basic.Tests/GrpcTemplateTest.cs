// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using ProjectTemplates.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace ProjectTemplates.Basic.Tests
{
    public class GrpcTemplateTest
    {
        public GrpcTemplateTest(ProjectFactoryFixture projectFactory, ITestOutputHelper output)
        {
            ProjectFactory = projectFactory;
            Output = output;
        }

        public Project Project { get; set; }

        public ProjectFactoryFixture ProjectFactory { get; }
        public ITestOutputHelper Output { get; }

        [Fact]
        public async Task GrpcTemplate()
        {
            Project = await ProjectFactory.GetOrCreateProject("grpc", Output);

            var createResult = await Project.RunDotNetNewAsync("grpc");
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", Project, createResult));

            var publishResult = await Project.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", Project, publishResult));

            var buildResult = await Project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", Project, buildResult));

            using (var serverProcess = Project.StartBuiltProjectAsync())
            {
                Assert.False(
                    serverProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run built service", Project, serverProcess.Process));
            }

            using (var aspNetProcess = Project.StartPublishedProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run published service", Project, aspNetProcess.Process));
            }
        }
    }
}
