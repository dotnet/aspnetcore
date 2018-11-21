// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class MvcTemplateTest : TemplateTestBase
    {
        public MvcTemplateTest(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [InlineData(null)]
        [InlineData("F#", Skip = "https://github.com/aspnet/Templating/issues/673")]
        private void MvcTemplate_NoAuthImpl(string languageOverride)
        {
            RunDotNetNew("mvc", language: languageOverride);

            AssertDirectoryExists("Areas", false);
            AssertDirectoryExists("Extensions", false);
            AssertFileExists("urlRewrite.config", false);
            AssertFileExists("Controllers/AccountController.cs", false);

            var projectExtension = languageOverride == "F#" ? "fsproj" : "csproj";
            var projectFileContents = ReadFile($"{ProjectName}.{projectExtension}");
            Assert.DoesNotContain(".db", projectFileContents);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore.Tools", projectFileContents);
            Assert.DoesNotContain("Microsoft.VisualStudio.Web.CodeGeneration.Design", projectFileContents);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore.Tools.DotNet", projectFileContents);
            Assert.DoesNotContain("Microsoft.Extensions.SecretManager.Tools", projectFileContents);

            foreach (var publish in new[] { false, true })
            {
                using (var aspNetProcess = StartAspNetProcess(publish))
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
            RunDotNetNew("mvc", auth: "Individual", useLocalDB: useLocalDB);

            AssertDirectoryExists("Extensions", false);
            AssertFileExists("urlRewrite.config", false);
            AssertFileExists("Controllers/AccountController.cs", false);

            var projectFileContents = ReadFile($"{ProjectName}.csproj");
            if (!useLocalDB)
            {
                Assert.Contains(".db", projectFileContents);
            }

            RunDotNetEfCreateMigration("mvc");

            AssertEmptyMigration("mvc");

            foreach (var publish in new[] { false, true })
            {
                using (var aspNetProcess = StartAspNetProcess(publish))
                {
                    aspNetProcess.AssertOk("/");
                    aspNetProcess.AssertOk("/Home/Privacy");
                }
            }
        }
    }
}
