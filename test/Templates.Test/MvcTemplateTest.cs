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

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InlineData(null)]
        [InlineData("F#")]
        public void MvcTemplate_NoAuth_Works_NetFramework(string languageOverride)
            => MvcTemplate_NoAuthImpl("net461", languageOverride);

        [Theory]
        [InlineData(null)]
        [InlineData("F#")]
        public void MvcTemplate_NoAuth_Works_NetCore(string languageOverride)
            => MvcTemplate_NoAuthImpl(null, languageOverride);

        private void MvcTemplate_NoAuthImpl(string targetFrameworkOverride, string languageOverride)
        {
            RunDotNetNew("mvc", targetFrameworkOverride, language: languageOverride);

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
                using (var aspNetProcess = StartAspNetProcess(targetFrameworkOverride, publish))
                {
                    aspNetProcess.AssertOk("/");
                    aspNetProcess.AssertOk("/Home/About");
                    aspNetProcess.AssertOk("/Home/Contact");
                }
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public void MvcTemplate_IndividualAuth_Works_NetFramework()
            => MvcTemplate_IndividualAuthImpl("net461");

        [Fact]
        public void MvcTemplate_IndividualAuth_Works_NetCore()
            => MvcTemplate_IndividualAuthImpl(null);

        private void MvcTemplate_IndividualAuthImpl(string targetFrameworkOverride)
        {
            RunDotNetNew("mvc", targetFrameworkOverride, auth: "Individual");

            AssertDirectoryExists("Extensions", true);
            AssertFileExists("urlRewrite.config", false);
            AssertFileExists("Controllers/AccountController.cs", true);

            var projectFileContents = ReadFile($"{ProjectName}.csproj");
            Assert.Contains(".db", projectFileContents);
            Assert.Contains("Microsoft.EntityFrameworkCore.Tools", projectFileContents);
            Assert.Contains("Microsoft.VisualStudio.Web.CodeGeneration.Design", projectFileContents);
            Assert.Contains("Microsoft.EntityFrameworkCore.Tools.DotNet", projectFileContents);
            Assert.Contains("Microsoft.Extensions.SecretManager.Tools", projectFileContents);

            foreach (var publish in new[] { false, true })
            {
                using (var aspNetProcess = StartAspNetProcess(targetFrameworkOverride, publish))
                {
                    aspNetProcess.AssertOk("/");
                    aspNetProcess.AssertOk("/Home/About");
                    aspNetProcess.AssertOk("/Home/Contact");
                }
            }
        }
    }
}
