// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Templates.Test.Helpers;
using Microsoft.AspNetCore.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class WebApiTemplateTest : LoggedTest
    {
        public WebApiTemplateTest(ProjectFactoryFixture factoryFixture)
        {
            FactoryFixture = factoryFixture;
        }

        public ProjectFactoryFixture FactoryFixture { get; }

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
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/28090", Queues = HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
        [InlineData("IndividualB2C", null)]
        [InlineData("IndividualB2C", new string[] { ArgConstants.UseMinimalApis })]
        [InlineData("IndividualB2C", new string[] { ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
        [InlineData("IndividualB2C", new string[] { ArgConstants.UseMinimalApis, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
        public Task WebApiTemplateCSharp_IdentityWeb_IndividualB2C_BuildsAndPublishes(string auth, string[] args) => PublishAndBuildWebApiTemplate(languageOverride: null, auth: auth, args: args);

        [ConditionalTheory]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/28090", Queues = HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
        [InlineData("IndividualB2C", null)]
        [InlineData("IndividualB2C", new string[] { ArgConstants.UseProgramMain })]
        [InlineData("IndividualB2C", new string[] { ArgConstants.UseProgramMain, ArgConstants.UseMinimalApis })]
        [InlineData("IndividualB2C", new string[] { ArgConstants.UseProgramMain, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
        [InlineData("IndividualB2C", new string[] { ArgConstants.UseProgramMain, ArgConstants.UseMinimalApis, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
        public Task WebApiTemplateCSharp_IdentityWeb_IndividualB2C_ProgramMain_BuildsAndPublishes(string auth, string[] args) => PublishAndBuildWebApiTemplate(languageOverride: null, auth: auth, args: args);

        [ConditionalTheory]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/28090", Queues = HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
        [InlineData("SingleOrg", null)]
        [InlineData("SingleOrg", new string[] { ArgConstants.UseMinimalApis })]
        [InlineData("SingleOrg", new string[] { ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
        [InlineData("SingleOrg", new string[] { ArgConstants.UseMinimalApis, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
        [InlineData("SingleOrg", new string[] { ArgConstants.CallsGraph })]
        [InlineData("SingleOrg", new string[] { ArgConstants.UseMinimalApis, ArgConstants.CallsGraph })]
        public Task WebApiTemplateCSharp_IdentityWeb_SingleOrg_BuildsAndPublishes(string auth, string[] args) => PublishAndBuildWebApiTemplate(languageOverride: null, auth: auth, args: args);

        [ConditionalTheory]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/28090", Queues = HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
        [InlineData("SingleOrg", null)]
        [InlineData("SingleOrg", new string[] { ArgConstants.UseProgramMain })]
        [InlineData("SingleOrg", new string[] { ArgConstants.UseProgramMain, ArgConstants.UseMinimalApis })]
        [InlineData("SingleOrg", new string[] { ArgConstants.UseProgramMain, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
        [InlineData("SingleOrg", new string[] { ArgConstants.UseProgramMain, ArgConstants.UseMinimalApis, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
        [InlineData("SingleOrg", new string[] { ArgConstants.UseProgramMain, ArgConstants.CallsGraph })]
        [InlineData("SingleOrg", new string[] { ArgConstants.UseProgramMain, ArgConstants.UseMinimalApis, ArgConstants.CallsGraph })]
        public Task WebApiTemplateCSharp_IdentityWeb_SingleOrg_ProgramMain_BuildsAndPublishes(string auth, string[] args) => PublishAndBuildWebApiTemplate(languageOverride: null, auth: auth, args: args);

        [Fact]
        public Task WebApiTemplateFSharp() => WebApiTemplateCore(languageOverride: "F#");

        [ConditionalFact]
        [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
        public Task WebApiTemplateCSharp() => WebApiTemplateCore(languageOverride: null);

        [ConditionalFact]
        [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
        public Task WebApiTemplateProgramMainCSharp() => WebApiTemplateCore(languageOverride: null, args: new [] { ArgConstants.UseProgramMain });

        [ConditionalFact]
        [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
        public Task WebApiTemplateMinimalApisCSharp() => WebApiTemplateCore(languageOverride: null, args: new[] { ArgConstants.UseMinimalApis });

        [ConditionalFact]
        [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
        public Task WebApiTemplateProgramMainMinimalApisCSharp() => WebApiTemplateCore(languageOverride: null, args: new[] { ArgConstants.UseProgramMain, ArgConstants.UseMinimalApis });

        [ConditionalTheory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
        public async Task WebApiTemplateCSharp_WithoutOpenAPI(bool useProgramMain, bool useMinimalApis)
        {
            var project = await FactoryFixture.CreateProject(Output);

            var args = useProgramMain
            ? useMinimalApis
                ? new[] { ArgConstants.UseProgramMain, ArgConstants.UseMinimalApis, ArgConstants.NoOpenApi }
                : new[] { ArgConstants.UseProgramMain, ArgConstants.NoOpenApi }
            : useMinimalApis
                ? new[] { ArgConstants.UseMinimalApis, ArgConstants.NoOpenApi }
                : new[] { ArgConstants.NoOpenApi };
            var createResult = await project.RunDotNetNewAsync("webapi", args: args);
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", project, createResult));

            var buildResult = await project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", project, buildResult));

            using var aspNetProcess = project.StartBuiltProjectAsync();
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

            await aspNetProcess.AssertNotFound("swagger");
        }

        private async Task<Project> PublishAndBuildWebApiTemplate(string languageOverride, string auth, string[] args = null)
        {
            var project = await FactoryFixture.GetOrCreateProject("webapi" + (languageOverride == "F#" ? "fsharp" : "csharp") + Guid.NewGuid().ToString().Substring(0, 10).ToLowerInvariant(), Output);

            var createResult = await project.RunDotNetNewAsync("webapi", language: languageOverride, auth: auth, args: args);
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", project, createResult));

            // Avoid the F# compiler. See https://github.com/dotnet/aspnetcore/issues/14022
            if (languageOverride != null)
            {
                return project;
            }

            var publishResult = await project.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", project, publishResult));

            // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
            // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
            // later, while the opposite is not true.

            var buildResult = await project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", project, buildResult));

            return project;
        }

        private async Task WebApiTemplateCore(string languageOverride, string[] args = null)
        {
            var project = await PublishAndBuildWebApiTemplate(languageOverride, null, args);

            // Avoid the F# compiler. See https://github.com/dotnet/aspnetcore/issues/14022
            if (languageOverride != null)
            {
                return;
            }

            using (var aspNetProcess = project.StartBuiltProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

                await aspNetProcess.AssertOk("weatherforecast");
                await aspNetProcess.AssertOk("swagger");
                await aspNetProcess.AssertNotFound("/");
            }

            using (var aspNetProcess = project.StartPublishedProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", project, aspNetProcess.Process));


                await aspNetProcess.AssertOk("weatherforecast");
                // Swagger is only available in Development
                await aspNetProcess.AssertNotFound("swagger");
                await aspNetProcess.AssertNotFound("/");
            }
        }
    }
}
