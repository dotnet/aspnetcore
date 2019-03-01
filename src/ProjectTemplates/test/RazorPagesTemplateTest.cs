// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Testing.xunit;
using ProjectTemplates.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class RazorPagesTemplateTest
    {
        public RazorPagesTemplateTest(ProjectFactoryFixture projectFactory, ITestOutputHelper output)
        {
            Project = projectFactory.CreateProject(output);
        }

        public Project Project { get; }

        [Fact]
        private void RazorPagesTemplate_NoAuthImpl()
        {
            Project.RunDotNetNew("razor");

            Project.AssertFileExists("Pages/Shared/_LoginPartial.cshtml", false);

            var projectFileContents = Project.ReadFile($"{Project.ProjectName}.csproj");
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
                    aspNetProcess.AssertOk("/Privacy");
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void RazorPagesTemplate_IndividualAuthImpl( bool useLocalDB)
        {
            Project.RunDotNetNew("razor", auth: "Individual", useLocalDB: useLocalDB);

            Project.AssertFileExists("Pages/Shared/_LoginPartial.cshtml", true);

            var projectFileContents = Project.ReadFile($"{Project.ProjectName}.csproj");
            if (!useLocalDB)
            {
                Assert.Contains(".db", projectFileContents);
            }

            Project.RunDotNetEfCreateMigration("razorpages");

            Project.AssertEmptyMigration("razorpages");

            foreach (var publish in new[] { false, true })
            {
                using (var aspNetProcess = Project.StartAspNetProcess(publish))
                {
                    aspNetProcess.AssertOk("/");
                    aspNetProcess.AssertOk("/Privacy");
                }
            }
        }
    }
}
