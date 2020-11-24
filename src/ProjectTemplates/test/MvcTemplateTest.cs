// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

        private async Task MvcTemplateCore(string languageOverride)
        {
            var project = await ProjectFactory.GetOrCreateProject("mvcnoauth" + (languageOverride == "F#" ? "fsharp" : "csharp"), Output);

            var createResult = await project.RunDotNetNewAsync("mvc", language: languageOverride);
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
        [InlineData(true)]
        [InlineData(false)]
        [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
        public async Task MvcTemplate_IndividualAuth(bool useLocalDB)
        {
            var project = await ProjectFactory.GetOrCreateProject("mvcindividual" + (useLocalDB ? "uld" : ""), Output);

            var createResult = await project.RunDotNetNewAsync("mvc", auth: "Individual", useLocalDB: useLocalDB);
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

        [ConditionalFact(Skip = "https://github.com/dotnet/aspnetcore/issues/25103")]
        [SkipOnHelix("cert failure", Queues = "All.OSX")]
        public async Task MvcTemplate_SingleFileExe()
        {
            // This test verifies publishing an MVC app as a single file exe works. We'll limit testing
            // this to a few operating systems to make our lives easier.
            string runtimeIdentifer;
            if (OperatingSystem.IsWindows())
            {
                runtimeIdentifer = "win-x64";
            }
            else if (OperatingSystem.IsLinux())
            {
                runtimeIdentifer = "linux-x64";
            }
            else
            {
                return;
            }

            var project = await ProjectFactory.GetOrCreateProject("mvcsinglefileexe", Output);
            project.RuntimeIdentifier = runtimeIdentifer;

            var createResult = await project.RunDotNetNewAsync("mvc", auth: "Individual", useLocalDB: true);
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", project, createResult));

            var publishResult = await project.RunDotNetPublishAsync(additionalArgs: $"/p:PublishSingleFile=true -r {runtimeIdentifer}", noRestore: false);
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", project, publishResult));

            var pages = new[]
            {
                new Page
                {
                    // Verify a view from the app works
                    Url = PageUrls.HomeUrl,
                    Links = new []
                    {
                        PageUrls.HomeUrl,
                        PageUrls.RegisterUrl,
                        PageUrls.LoginUrl,
                        PageUrls.HomeUrl,
                        PageUrls.PrivacyUrl,
                        PageUrls.DocsUrl,
                        PageUrls.PrivacyUrl
                    }
                },
                new Page
                {
                    // Verify a view from a RCL (in this case IdentityUI) works
                    Url = PageUrls.RegisterUrl,
                    Links = new []
                    {
                        PageUrls.HomeUrl,
                        PageUrls.RegisterUrl,
                        PageUrls.LoginUrl,
                        PageUrls.HomeUrl,
                        PageUrls.PrivacyUrl,
                        PageUrls.ExternalArticle,
                        PageUrls.PrivacyUrl
                    }
                },
            };

            using var aspNetProcess = project.StartPublishedProjectAsync(usePublishedAppHost: true);
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", project, aspNetProcess.Process));

            await aspNetProcess.AssertPagesOk(pages);
        }

        [ConditionalFact]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/28090", Queues = HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
        public async Task MvcTemplate_RazorRuntimeCompilation_BuildsAndPublishes()
        {
            var project = await MvcTemplateBuildsAndPublishes(auth: null, args: new[] { "--razor-runtime-compilation" });

            Assert.False(Directory.Exists(Path.Combine(project.TemplatePublishDir, "refs")), "The refs directory should not be published.");
        }

        [ConditionalTheory]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/28090", Queues = HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
        [InlineData("IndividualB2C", null)]
        [InlineData("IndividualB2C", new string[] { "--called-api-url \"https://graph.microsoft.com\"", "--called-api-scopes user.readwrite" })]
        [InlineData("SingleOrg", null)]
        [InlineData("SingleOrg", new string[] { "--called-api-url \"https://graph.microsoft.com\"", "--called-api-scopes user.readwrite" })]
        [InlineData("SingleOrg", new string[] { "--calls-graph" })]
        public Task MvcTemplate_IdentityWeb_BuildsAndPublishes(string auth, string[] args) => MvcTemplateBuildsAndPublishes(auth: auth, args: args);

        private async Task<Project> MvcTemplateBuildsAndPublishes(string auth, string[] args)
        {
            var project = await ProjectFactory.GetOrCreateProject("mvc" + Guid.NewGuid().ToString().Substring(0, 10).ToLowerInvariant(), Output);

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
