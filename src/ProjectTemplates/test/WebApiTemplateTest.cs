// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Templates.Test.Helpers;
using Microsoft.AspNetCore.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class WebApiTemplateTest
    {
        public WebApiTemplateTest(ProjectFactoryFixture factoryFixture, ITestOutputHelper output)
        {
            FactoryFixture = factoryFixture;
            Output = output;
        }

        public ProjectFactoryFixture FactoryFixture { get; }

        public ITestOutputHelper Output { get; }

        public Project Project { get; set; }

        [Fact]
        public async Task WebApiTemplateFSharp() => await WebApiTemplateCore(languageOverride: "F#");

        [ConditionalFact]
        [SkipOnHelix("Cert failures", Queues = "OSX.1014.Amd64;OSX.1014.Amd64.Open")]
        public async Task WebApiTemplateCSharp() => await WebApiTemplateCore(languageOverride: null);

        private async Task WebApiTemplateCore(string languageOverride)
        {
            Project = await FactoryFixture.GetOrCreateProject("webapi" + (languageOverride == "F#" ? "fsharp" : "csharp"), Output);

            var createResult = await Project.RunDotNetNewAsync("webapi", language: languageOverride);
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", Project, createResult));

            // Avoid the F# compiler. See https://github.com/dotnet/aspnetcore/issues/14022
            if (languageOverride != null)
            {
                return;
            }

            var publishResult = await Project.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", Project, publishResult));

            // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
            // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
            // later, while the opposite is not true.

            var buildResult = await Project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", Project, buildResult));

            using (var aspNetProcess = Project.StartBuiltProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", Project, aspNetProcess.Process));

                await aspNetProcess.AssertOk("weatherforecast");
                await aspNetProcess.AssertNotFound("/");
            }

            using (var aspNetProcess = Project.StartPublishedProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", Project, aspNetProcess.Process));


                await aspNetProcess.AssertOk("weatherforecast");
                await aspNetProcess.AssertNotFound("/");
            }
        }
    }
}
