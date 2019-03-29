// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Templates.Test.Helpers;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class MvcTemplateTest
    {
        public MvcTemplateTest(ProjectFactoryFixture projectFactory, ITestOutputHelper output)
        {
            ProjectFactory = projectFactory;
            Output = output;
        }

        public Project Project { get; set; }

        public ProjectFactoryFixture ProjectFactory { get; }
        public ITestOutputHelper Output { get; }

        [Theory]
        [InlineData(null)]
        [InlineData("F#")]
        public async Task MvcTemplate_NoAuthImplAsync(string languageOverride)
        {
            Project = await ProjectFactory.GetOrCreateProject("mvcnoauth" + (languageOverride == "F#" ? "fsharp" : "csharp"), Output);

            var createResult = await Project.RunDotNetNewAsync("mvc", language: languageOverride);
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", Project, createResult));

            AssertDirectoryExists(Project.TemplateOutputDir, "Areas", false);
            AssertDirectoryExists(Project.TemplateOutputDir, "Extensions", false);
            AssertFileExists(Project.TemplateOutputDir, "urlRewrite.config", false);
            AssertFileExists(Project.TemplateOutputDir, "Controllers/AccountController.cs", false);

            var projectExtension = languageOverride == "F#" ? "fsproj" : "csproj";
            var projectFileContents = ReadFile(Project.TemplateOutputDir, $"{Project.ProjectName}.{projectExtension}");
            Assert.DoesNotContain(".db", projectFileContents);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore.Tools", projectFileContents);
            Assert.DoesNotContain("Microsoft.VisualStudio.Web.CodeGeneration.Design", projectFileContents);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore.Tools.DotNet", projectFileContents);
            Assert.DoesNotContain("Microsoft.Extensions.SecretManager.Tools", projectFileContents);

            var publishResult = await Project.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", Project, publishResult));

            // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
            // The output from publish will go into bin/Release/netcoreapp3.0/publish and won't be affected by calling build
            // later, while the opposite is not true.

            var buildResult = await Project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", Project, buildResult));

            IEnumerable<string> menuLinks = new List<string> {
                PageUrls.HomeUrl,
                PageUrls.HomeUrl,
                PageUrls.PrivacyFullUrl
            };

            if(languageOverride == null)
            {
                menuLinks = menuLinks.Append(PageUrls.PrivacyFullUrl);
            }
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

            using (var aspNetProcess = Project.StartBuiltProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", Project, aspNetProcess.Process));

                await aspNetProcess.AssertPagesOk(pages);
            }

            using (var aspNetProcess = Project.StartPublishedProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", Project, aspNetProcess.Process));

                await aspNetProcess.AssertPagesOk(pages);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task MvcTemplate_IndividualAuthImplAsync(bool useLocalDB)
        {
            Project = await ProjectFactory.GetOrCreateProject("mvcindividual" + (useLocalDB ? "uld" : ""), Output);

            var createResult = await Project.RunDotNetNewAsync("mvc", auth: "Individual", useLocalDB: useLocalDB);
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", Project, createResult));

            AssertDirectoryExists(Project.TemplateOutputDir, "Extensions", false);
            AssertFileExists(Project.TemplateOutputDir, "urlRewrite.config", false);
            AssertFileExists(Project.TemplateOutputDir, "Controllers/AccountController.cs", false);

            var projectFileContents = ReadFile(Project.TemplateOutputDir, $"{Project.ProjectName}.csproj");
            if (!useLocalDB)
            {
                Assert.Contains(".db", projectFileContents);
            }

            var publishResult = await Project.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", Project, publishResult));

            // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
            // The output from publish will go into bin/Release/netcoreapp3.0/publish and won't be affected by calling build
            // later, while the opposite is not true.

            var buildResult = await Project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", Project, buildResult));

            var migrationsResult = await Project.RunDotNetEfCreateMigrationAsync("mvc");
            Assert.True(0 == migrationsResult.ExitCode, ErrorMessages.GetFailedProcessMessage("run EF migrations", Project, migrationsResult));
            Project.AssertEmptyMigration("mvc");

            var pages = new List<Page> {
                new Page
                {
                    Url = PageUrls.ForgotPassword,
                    Links = new string [] {
                        PageUrls.HomeUrl,
                        PageUrls.RegisterUrl,
                        PageUrls.LoginUrl,
                        PageUrls.HomeUrl,
                        PageUrls.PrivacyUrl,
                        PageUrls.PrivacyUrl,
                        PageUrls.PrivacyUrl
                    }
                },
                new Page
                {
                    Url = PageUrls.HomeUrl,
                    Links = new string[] {
                        PageUrls.HomeUrl,
                        PageUrls.RegisterUrl,
                        PageUrls.LoginUrl,
                        PageUrls.HomeUrl,
                        PageUrls.PrivacyUrl,
                        PageUrls.PrivacyUrl,
                        PageUrls.DocsUrl,
                        PageUrls.PrivacyUrl
                    }
                },
                new Page
                {
                    Url = PageUrls.PrivacyFullUrl,
                    Links = new string[] {
                        PageUrls.HomeUrl,
                        PageUrls.RegisterUrl,
                        PageUrls.LoginUrl,
                        PageUrls.HomeUrl,
                        PageUrls.PrivacyUrl,
                        PageUrls.PrivacyUrl,
                        PageUrls.PrivacyUrl
                    }
                },
                new Page
                {
                    Url = PageUrls.LoginUrl,
                    Links = new string[] {
                        PageUrls.HomeUrl,
                        PageUrls.RegisterUrl,
                        PageUrls.LoginUrl,
                        PageUrls.HomeUrl,
                        PageUrls.PrivacyUrl,
                        PageUrls.PrivacyUrl,
                        PageUrls.ForgotPassword,
                        PageUrls.RegisterUrl,
                        PageUrls.ExternalArticle,
                        PageUrls.PrivacyUrl }
                },
                new Page
                {
                    Url = PageUrls.RegisterUrl,
                    Links = new string [] {
                        PageUrls.HomeUrl,
                        PageUrls.RegisterUrl,
                        PageUrls.LoginUrl,
                        PageUrls.HomeUrl,
                        PageUrls.PrivacyUrl,
                        PageUrls.PrivacyUrl,
                        PageUrls.ExternalArticle,
                        PageUrls.PrivacyUrl
                    }
                }
            };

            using (var aspNetProcess = Project.StartBuiltProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", Project, aspNetProcess.Process));

                await aspNetProcess.AssertPagesOk(pages);
            }

            using (var aspNetProcess = Project.StartPublishedProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", Project, aspNetProcess.Process));

                await aspNetProcess.AssertPagesOk(pages);
            }
        }

        private void AssertDirectoryExists(string basePath, string path, bool shouldExist)
        {
            var fullPath = Path.Combine(basePath, path);
            var doesExist = Directory.Exists(fullPath);

            if (shouldExist)
            {
                Assert.True(doesExist, "Expected directory to exist, but it doesn't: " + path);
            }
            else
            {
                Assert.False(doesExist, "Expected directory not to exist, but it does: " + path);
            }
        }

        private void AssertFileExists(string basePath, string path, bool shouldExist)
        {
            var fullPath = Path.Combine(basePath, path);
            var doesExist = File.Exists(fullPath);

            if (shouldExist)
            {
                Assert.True(doesExist, "Expected file to exist, but it doesn't: " + path);
            }
            else
            {
                Assert.False(doesExist, "Expected file not to exist, but it does: " + path);
            }
        }

        private string ReadFile(string basePath, string path)
        {
            AssertFileExists(basePath, path, shouldExist: true);
            return File.ReadAllText(Path.Combine(basePath, path));
        }
    }
}
