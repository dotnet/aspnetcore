// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Templates.Test
{
    public class MvcTemplateTest : LoggedTest
    {
        public MvcTemplateTest(ProjectFactoryFixture projectFactory)
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
        public async Task MvcTemplate_NoAuthFSharp() => await MvcTemplateCore(languageOverride: "F#");

        [ConditionalFact]
        [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
        public async Task MvcTemplate_NoAuthCSharp() => await MvcTemplateCore(languageOverride: null);

        [ConditionalFact]
        [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
        public async Task MvcTemplate_ProgramMainNoAuthCSharp() => await MvcTemplateCore(languageOverride: null, new [] { ArgConstants.UseProgramMain });

        private async Task MvcTemplateCore(string languageOverride, string[] args = null)
        {
            var project = await ProjectFactory.CreateProject(Output);

            var createResult = await project.RunDotNetNewAsync("mvc", language: languageOverride, args: args);
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", project, createResult));

            var projectExtension = languageOverride == "F#" ? "fsproj" : "csproj";
            var projectFileContents = project.ReadFile($"{project.ProjectName}.{projectExtension}");
            Assert.DoesNotContain(".db", projectFileContents);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore.Tools", projectFileContents);
            Assert.DoesNotContain("Microsoft.VisualStudio.Web.CodeGeneration.Design", projectFileContents);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore.Tools.DotNet", projectFileContents);
            Assert.DoesNotContain("Microsoft.Extensions.SecretManager.Tools", projectFileContents);

            // Avoid the F# compiler. See https://github.com/dotnet/aspnetcore/issues/14022
            if (languageOverride != null)
            {
                return;
            }

            var publishResult = await project.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", project, publishResult));

            // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
            // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
            // later, while the opposite is not true.

            var buildResult = await project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", project, buildResult));

            IEnumerable<string> menuLinks = new List<string> {
                    PageUrls.HomeUrl,
                    PageUrls.HomeUrl,
                    PageUrls.PrivacyFullUrl
                };

            var footerLinks = new string[] { PageUrls.PrivacyFullUrl };

            var pages = new List<Page>
            {
                new Page
                {
                    Url = PageUrls.HomeUrl,
                    Links = menuLinks.Append(PageUrls.DocsUrl).Concat(footerLinks)
                },
                new Page
                {
                    Url = PageUrls.PrivacyFullUrl,
                    Links = menuLinks.Concat(footerLinks)
                }
            };

            using (var aspNetProcess = project.StartBuiltProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

                await aspNetProcess.AssertPagesOk(pages);
            }

            using (var aspNetProcess = project.StartPublishedProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", project, aspNetProcess.Process));

                await aspNetProcess.AssertPagesOk(pages);
            }
        }

        [ConditionalTheory]
        [InlineData(false)]
        [InlineData(true)]
        [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX, SkipReason = "No LocalDb on non-Windows")]
        public Task MvcTemplate_IndividualAuth_LocalDb(bool useProgramMain) => MvcTemplate_IndividualAuth_Core(useLocalDB: true, useProgramMain);

        [ConditionalTheory]
        [InlineData(false)]
        [InlineData(true)]
        [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64 + HelixConstants.DebianAmd64)]
        public Task MvcTemplate_IndividualAuth(bool useProgramMain) => MvcTemplate_IndividualAuth_Core(useLocalDB: false, useProgramMain);

        private async Task MvcTemplate_IndividualAuth_Core(bool useLocalDB, bool useProgramMain)
        {
            var project = await ProjectFactory.CreateProject(Output);

            var args = useProgramMain ? new [] { ArgConstants.UseProgramMain } : null;
            var createResult = await project.RunDotNetNewAsync("mvc", auth: "Individual", useLocalDB: useLocalDB, args: args);
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", project, createResult));

            var projectFileContents = project.ReadFile($"{project.ProjectName}.csproj");
            if (!useLocalDB)
            {
                Assert.Contains(".db", projectFileContents);
            }

            var publishResult = await project.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", project, publishResult));

            // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
            // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
            // later, while the opposite is not true.

            var buildResult = await project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", project, buildResult));

            var migrationsResult = await project.RunDotNetEfCreateMigrationAsync("mvc");
            Assert.True(0 == migrationsResult.ExitCode, ErrorMessages.GetFailedProcessMessage("run EF migrations", project, migrationsResult));
            project.AssertEmptyMigration("mvc");

            // Note: if any links are updated here, RazorPagesTemplateTest.cs should be updated as well
            var pages = new List<Page> {
                    new Page
                    {
                        Url = PageUrls.ForgotPassword,
                        Links = new string [] {
                            PageUrls.HomeUrl,
                            PageUrls.HomeUrl,
                            PageUrls.PrivacyUrl,
                            PageUrls.RegisterUrl,
                            PageUrls.LoginUrl,
                            PageUrls.PrivacyUrl
                        }
                    },
                    new Page
                    {
                        Url = PageUrls.HomeUrl,
                        Links = new string[] {
                            PageUrls.HomeUrl,
                            PageUrls.HomeUrl,
                            PageUrls.PrivacyUrl,
                            PageUrls.RegisterUrl,
                            PageUrls.LoginUrl,
                            PageUrls.DocsUrl,
                            PageUrls.PrivacyUrl
                        }
                    },
                    new Page
                    {
                        Url = PageUrls.PrivacyFullUrl,
                        Links = new string[] {
                            PageUrls.HomeUrl,
                            PageUrls.HomeUrl,
                            PageUrls.PrivacyUrl,
                            PageUrls.RegisterUrl,
                            PageUrls.LoginUrl,
                            PageUrls.PrivacyUrl
                        }
                    },
                    new Page
                    {
                        Url = PageUrls.LoginUrl,
                        Links = new string[] {
                            PageUrls.HomeUrl,
                            PageUrls.HomeUrl,
                            PageUrls.PrivacyUrl,
                            PageUrls.RegisterUrl,
                            PageUrls.LoginUrl,
                            PageUrls.ForgotPassword,
                            PageUrls.RegisterUrl,
                            PageUrls.ResendEmailConfirmation,
                            PageUrls.ExternalArticle,
                            PageUrls.PrivacyUrl }
                    },
                    new Page
                    {
                        Url = PageUrls.RegisterUrl,
                        Links = new string [] {
                            PageUrls.HomeUrl,
                            PageUrls.HomeUrl,
                            PageUrls.PrivacyUrl,
                            PageUrls.RegisterUrl,
                            PageUrls.LoginUrl,
                            PageUrls.ExternalArticle,
                            PageUrls.PrivacyUrl
                        }
                    }
                };

            using (var aspNetProcess = project.StartBuiltProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

                await aspNetProcess.AssertPagesOk(pages);
            }

            using (var aspNetProcess = project.StartPublishedProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", project, aspNetProcess.Process));

                await aspNetProcess.AssertPagesOk(pages);
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)] // Running these requires the rid-specific runtime pack to be available which is not consistent in all our platform builds.
        [SkipOnHelix("cert failure", Queues = "All.OSX;" + HelixConstants.Windows10Arm64)]
        public async Task MvcTemplate_SingleFileExe()
        {
            // This test verifies publishing an MVC app as a single file exe works. We'll limit testing
            // this to a few operating systems to make our lives easier.
            var runtimeIdentifer = "win-x64";
            var project = await ProjectFactory.CreateProject(Output);
            project.RuntimeIdentifier = runtimeIdentifer;

            var createResult = await project.RunDotNetNewAsync("mvc");
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", project, createResult));

            var publishResult = await project.RunDotNetPublishAsync(additionalArgs: $"/p:PublishSingleFile=true -r {runtimeIdentifer} --self-contained", noRestore: false);
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", project, publishResult));

            var menuLinks = new[]
            {
                PageUrls.HomeUrl,
                PageUrls.HomeUrl,            
                PageUrls.PrivacyFullUrl
            };

            var footerLinks = new[] { PageUrls.PrivacyFullUrl };

            var pages = new List<Page>
            {
                new Page
                {
                    Url = PageUrls.HomeUrl,
                    Links = menuLinks.Append(PageUrls.DocsUrl).Concat(footerLinks),
                },
                new Page
                {
                    Url = PageUrls.PrivacyFullUrl,
                    Links = menuLinks.Concat(footerLinks),
                }
            };

            using var aspNetProcess = project.StartPublishedProjectAsync(usePublishedAppHost: true);
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", project, aspNetProcess.Process));

            await aspNetProcess.AssertPagesOk(pages);
        }

        [ConditionalTheory]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/28090", Queues = HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
        [InlineData("IndividualB2C", null)]
        [InlineData("IndividualB2C", new[] { ArgConstants.UseProgramMain })]
        [InlineData("IndividualB2C", new[] { ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
        [InlineData("IndividualB2C", new[] { ArgConstants.UseProgramMain, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
        public Task MvcTemplate_IdentityWeb_IndividualB2C_BuildsAndPublishes(string auth, string[] args) => MvcTemplateBuildsAndPublishes(auth: auth, args: args);
        
        [ConditionalTheory]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/28090", Queues = HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
        [InlineData("SingleOrg", null)]
        [InlineData("SingleOrg", new[] { ArgConstants.UseProgramMain })]
        [InlineData("SingleOrg", new[] { ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
        [InlineData("SingleOrg", new[] { ArgConstants.UseProgramMain, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
        [InlineData("SingleOrg", new[] { ArgConstants.CallsGraph })]
        [InlineData("SingleOrg", new[] { ArgConstants.UseProgramMain, ArgConstants.CallsGraph })]
        public Task MvcTemplate_IdentityWeb_SingleOrg_BuildsAndPublishes(string auth, string[] args) => MvcTemplateBuildsAndPublishes(auth: auth, args: args);

        private async Task<Project> MvcTemplateBuildsAndPublishes(string auth, string[] args)
        {
            var project = await ProjectFactory.CreateProject(Output);

            var createResult = await project.RunDotNetNewAsync("mvc", auth: auth, args: args);
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", project, createResult));

            // Verify building in debug works
            var buildResult = await project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", project, buildResult));

            // Publish builds in "release" configuration. Running publish should ensure we can compile in release and that we can publish without issues.
            buildResult = await project.RunDotNetPublishAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", project, buildResult));

            return project;
        }
    }
}
