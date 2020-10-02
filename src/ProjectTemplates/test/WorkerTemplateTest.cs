// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;
using Microsoft.AspNetCore.Testing;

namespace Templates.Test
{
    public class WorkerTemplateTest : LoggedTest
    {
        public WorkerTemplateTest(ProjectFactoryFixture projectFactory)
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

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "https://github.com/dotnet/sdk/issues/12831")]
        [InlineData("C#")]
        [InlineData("F#")]
        public async Task WorkerTemplateAsync(string language)
        {
            var project = await ProjectFactory.GetOrCreateProject(
                $"worker-{ language.ToLowerInvariant()[0] }sharp",
                Output);

            var createResult = await project.RunDotNetNewAsync("worker", language: language);
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", project, createResult));

            var publishResult = await project.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", project, publishResult));

            // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
            // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
            // later, while the opposite is not true.

            var buildResult = await project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", project, buildResult));

            using (var aspNetProcess = project.StartBuiltProjectAsync(hasListeningUri: false))
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));
            }

            using (var aspNetProcess = project.StartPublishedProjectAsync(hasListeningUri: false))
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", project, aspNetProcess.Process));
            }
        }
    }
}
