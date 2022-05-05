// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class RazorPagesTemplateTest : LoggedTest
    {
        public RazorPagesTemplateTest(ProjectFactoryFixture projectFactory)
        {
            ProjectFactory = projectFactory;
        }

        public ProjectFactoryFixture ProjectFactory { get; set; }

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
        [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
        [InlineData(true)]
        [InlineData(false)]
        public async Task RazorPagesTemplate_NoAuth(bool useProgramMain)
        {
            var project = await ProjectFactory.GetOrCreateProject("razorpagesnoauth", Output);

            var args = useProgramMain ? new [] { ArgConstants.UseProgramMain } : null;
            var createResult = await project.RunDotNetNewAsync("razor", args: args);
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("razor", project, createResult));

            var projectFileContents = ReadFile(project.TemplateOutputDir, $"{project.ProjectName}.csproj");
            Assert.DoesNotContain(".db", projectFileContents);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore.Tools", projectFileContents);
            Assert.DoesNotContain("Microsoft.VisualStudio.Web.CodeGeneration.Design", projectFileContents);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore.Tools.DotNet", projectFileContents);
            Assert.DoesNotContain("Microsoft.Extensions.SecretManager.Tools", projectFileContents);

            var publishResult = await project.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", project, createResult));

            // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
            // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
            // later, while the opposite is not true.

            var buildResult = await project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", project, createResult));

            var pages = new List<Page>
            {
                new Page
                {
                    Url = PageUrls.HomeUrl,
                    Links = new string[] {
                        PageUrls.HomeUrl,
                        PageUrls.HomeUrl,
                        PageUrls.PrivacyUrl,
                        PageUrls.DocsUrl,
                        PageUrls.PrivacyUrl
                    }
                },
                new Page
                {
                    Url = PageUrls.PrivacyUrl,
                    Links = new string[] {
                        PageUrls.HomeUrl,
                        PageUrls.HomeUrl,
                        PageUrls.PrivacyUrl,
                        PageUrls.PrivacyUrl }
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
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
        public async Task RazorPagesTemplate_IndividualAuth(bool useLocalDB, bool useProgramMain)
        {
            var project = await ProjectFactory.GetOrCreateProject("razorpagesindividual" + (useLocalDB ? "uld" : ""), Output);

            var args = useProgramMain ? new [] { ArgConstants.UseProgramMain } : null;
            var createResult = await project.RunDotNetNewAsync("razor", auth: "Individual", useLocalDB: useLocalDB, args: args);
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", project, createResult));

            var projectFileContents = ReadFile(project.TemplateOutputDir, $"{project.ProjectName}.csproj");
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

            var migrationsResult = await project.RunDotNetEfCreateMigrationAsync("razorpages");
            Assert.True(0 == migrationsResult.ExitCode, ErrorMessages.GetFailedProcessMessage("run EF migrations", project, migrationsResult));
            project.AssertEmptyMigration("razorpages");

            // Note: if any links are updated here, MvcTemplateTest.cs should be updated as well
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
                    Url = PageUrls.PrivacyUrl,
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
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

                await aspNetProcess.AssertPagesOk(pages);
            }
        }

        [ConditionalTheory]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/28090", Queues = HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
        [InlineData("IndividualB2C", null)]
        [InlineData("IndividualB2C", new[] { ArgConstants.UseProgramMain })]
        [InlineData("IndividualB2C", new[] { ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
        [InlineData("IndividualB2C", new[] { ArgConstants.UseProgramMain, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
        [InlineData("SingleOrg", null)]
        [InlineData("SingleOrg", new[] { ArgConstants.UseProgramMain })]
        [InlineData("SingleOrg", new[] { ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
        [InlineData("SingleOrg", new[] { ArgConstants.UseProgramMain, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
        public Task RazorPagesTemplate_IdentityWeb_BuildsAndPublishes(string auth, string[] args) => BuildAndPublishRazorPagesTemplate(auth: auth, args: args);

        [ConditionalTheory]
        [InlineData("SingleOrg", new[] { ArgConstants.CallsGraph })]
        [InlineData("SingleOrg", new[] { ArgConstants.UseProgramMain, ArgConstants.CallsGraph })]
        public Task RazorPagesTemplate_IdentityWeb_BuildsAndPublishes_WithSingleOrg(string auth, string[] args) => BuildAndPublishRazorPagesTemplate(auth: auth, args: args);

        private async Task<Project> BuildAndPublishRazorPagesTemplate(string auth, string[] args)
        {
            var project = await ProjectFactory.GetOrCreateProject("razorpages" + Guid.NewGuid().ToString().Substring(0, 10).ToLowerInvariant(), Output);

            var createResult = await project.RunDotNetNewAsync("razor", auth: auth, args: args);
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", project, createResult));

            // Verify building in debug works
            var buildResult = await project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", project, buildResult));

            // Publish builds in "release" configuration. Running publish should ensure we can compile in release and that we can publish without issues.
            buildResult = await project.RunDotNetPublishAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", project, buildResult));

            return project;
       }


        private string ReadFile(string basePath, string path)
        {
            var fullPath = Path.Combine(basePath, path);
            var doesExist = File.Exists(fullPath);

            Assert.True(doesExist, $"Expected file to exist, but it doesn't: {path}");
            return File.ReadAllText(Path.Combine(basePath, path));
        }
    }
}
