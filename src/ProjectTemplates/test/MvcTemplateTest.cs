// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using ProjectTemplates.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class MvcTemplateTest
    {
        public MvcTemplateTest(ProjectFactoryFixture projectFactory, ITestOutputHelper output)
        {
            Project = projectFactory.CreateProject(output);
        }

        public Project Project { get; }

        [Theory]
        [InlineData(null)]
        [InlineData("F#", Skip = "https://github.com/aspnet/Templating/issues/673")]
        private void MvcTemplate_NoAuthImpl(string languageOverride)
        {
            Project.RunDotNetNew("mvc", language: languageOverride);

            Project.AssertDirectoryExists("Areas", false);
            Project.AssertDirectoryExists("Extensions", false);
            Project.AssertFileExists("urlRewrite.config", false);
            Project.AssertFileExists("Controllers/AccountController.cs", false);

            var projectExtension = languageOverride == "F#" ? "fsproj" : "csproj";
            var projectFileContents = Project.ReadFile($"{Project.ProjectName}.{projectExtension}");
            Assert.DoesNotContain(".db", projectFileContents);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore.Tools", projectFileContents);
            Assert.DoesNotContain("Microsoft.VisualStudio.Web.CodeGeneration.Design", projectFileContents);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore.Tools.DotNet", projectFileContents);
            Assert.DoesNotContain("Microsoft.Extensions.SecretManager.Tools", projectFileContents);

            foreach (var publish in new[] { false, true })
            {
                using (var aspNetProcess = Project.StartAspNetProcess(publish))
                {
                    aspNetProcess.AssertOk("/");
                    aspNetProcess.AssertOk("/Home/Privacy");
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void MvcTemplate_IndividualAuthImpl(bool useLocalDB)
        {
            Project.RunDotNetNew("mvc", auth: "Individual", useLocalDB: useLocalDB);

            Project.AssertDirectoryExists("Extensions", false);
            Project.AssertFileExists("urlRewrite.config", false);
            Project.AssertFileExists("Controllers/AccountController.cs", false);

            var projectFileContents = Project.ReadFile($"{Project.ProjectName}.csproj");
            if (!useLocalDB)
            {
                Assert.Contains(".db", projectFileContents);
            }

            Project.RunDotNetEfCreateMigration("mvc");

            Project.AssertEmptyMigration("mvc");

            foreach (var publish in new[] { false, true })
            {
                using (var aspNetProcess = Project.StartAspNetProcess(publish))
                {
                    aspNetProcess.AssertOk("/");
                    aspNetProcess.AssertOk("/Home/Privacy");
                }
            }
        }
    }
}
