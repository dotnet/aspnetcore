// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

        [ConditionalFact]
        [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
        public async Task RazorPagesTemplate_NoAuth()
        {
            var project = await ProjectFactory.GetOrCreateProject("razorpagesnoauth", Output);

            var createResult = await project.RunDotNetNewAsync("razor");
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
        [InlineData(false)]
        [InlineData(true)]
        [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
        public async Task RazorPagesTemplate_IndividualAuth(bool useLocalDB)
        {
            var project = await ProjectFactory.GetOrCreateProject("razorpagesindividual" + (useLocalDB ? "uld" : ""), Output);

            var createResult = await project.RunDotNetNewAsync("razor", auth: "Individual", useLocalDB: useLocalDB);
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

        [ConditionalFact]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/28090", Queues = HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
        public async Task RazorPagesTemplate_RazorRuntimeCompilation_BuildsAndPublishes()
        {
            var project = await BuildAndPublishRazorPagesTemplate(auth: null, new[] { "--razor-runtime-compilation" });

            Assert.False(Directory.Exists(Path.Combine(project.TemplatePublishDir, "refs")), "The refs directory should not be published.");

            // Verify ref assemblies isn't published
            var refsDirectory = Path.Combine(project.TemplatePublishDir, "refs");
            Assert.False(Directory.Exists(refsDirectory), $"{refsDirectory} should not be in the publish output.");
        }

        [ConditionalTheory]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/28090", Queues = HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
        [InlineData("IndividualB2C", null)]
        [InlineData("IndividualB2C", new string[] { "--called-api-url \"https://graph.microsoft.com\"", "--called-api-scopes user.readwrite" })]
        [InlineData("SingleOrg", null)]
        [InlineData("SingleOrg", new string[] { "--called-api-url \"https://graph.microsoft.com\"", "--called-api-scopes user.readwrite" })]
        [InlineData("SingleOrg", new string[] { "--calls-graph" })]
        public Task RazorPagesTemplate_IdentityWeb_BuildsAndPublishes(string auth, string[] args) => BuildAndPublishRazorPagesTemplate(auth: auth, args: args);

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
