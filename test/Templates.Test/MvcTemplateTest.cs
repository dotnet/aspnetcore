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

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public void MvcTemplate_NoAuth_Works_NetFramework_ForDefaultTemplate()
            => MvcTemplate_NoAuthImpl("net461", languageOverride: default);

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public void MvcTemplate_NoAuth_Works_NetFramework_ForFSharpTemplate()
            => MvcTemplate_NoAuthImpl("net461", languageOverride: "F#");

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public void MvcTemplate_NoAuth_NoHttps_Works_NetFramework_ForDefaultTemplate()
            => MvcTemplate_NoAuthImpl("net461", languageOverride: default, true);

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public void MvcTemplate_NoAuth_NoHttps_Works_NetFramework_ForFSharpTemplate()
            => MvcTemplate_NoAuthImpl("net461", languageOverride: "F#", true);

        [Fact]
        public void MvcTemplate_NoAuth_Works_NetCore_ForDefaultTemplate()
            => MvcTemplate_NoAuthImpl(null, languageOverride: default);

        [Fact]
        public void MvcTemplate_NoAuth_Works_NetCore_ForFSharpTemplate()
            => MvcTemplate_NoAuthImpl(null, languageOverride: "F#");

        private void MvcTemplate_NoAuthImpl(string targetFrameworkOverride, string languageOverride, bool noHttps = false)
        {
            RunDotNetNew("mvc", targetFrameworkOverride, language: languageOverride, noHttps: noHttps);

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

            if (targetFrameworkOverride != null)
            {
                if (noHttps)
                {
                    Assert.DoesNotContain("Microsoft.AspNetCore.HttpsPolicy", projectFileContents);
                }
                else
                {
                    Assert.Contains("Microsoft.AspNetCore.HttpsPolicy", projectFileContents);
                }
            }

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

        [ConditionalFact(Skip = "https://github.com/aspnet/templating/issues/378")]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public void MvcTemplate_IndividualAuth_Works_NetFramework()
            => MvcTemplate_IndividualAuthImpl("net461");

        [ConditionalFact(Skip = "https://github.com/aspnet/templating/issues/378")]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public void MvcTemplate_WithIndividualAuth_NoHttpsSetToTrue_UsesHttps_NetFramework()
            => MvcTemplate_IndividualAuthImpl("net461", false, true);

        [Fact(Skip = "https://github.com/aspnet/templating/issues/378")]
        public void MvcTemplate_IndividualAuth_Works_NetCore()
            => MvcTemplate_IndividualAuthImpl(null);

        [Fact(Skip = "https://github.com/aspnet/templating/issues/378")]
        public void MvcTemplate_IndividualAuth_UsingLocalDB_Works_NetCore()
            => MvcTemplate_IndividualAuthImpl(null, true);

        private void MvcTemplate_IndividualAuthImpl(string targetFrameworkOverride, bool useLocalDB = false, bool noHttps = false)
        {
            RunDotNetNew("mvc", targetFrameworkOverride, auth: "Individual", useLocalDB: useLocalDB);

            AssertDirectoryExists("Extensions", false);
            AssertFileExists("urlRewrite.config", false);
            AssertFileExists("Controllers/AccountController.cs", false);

            var projectFileContents = ReadFile($"{ProjectName}.csproj");
            if (!useLocalDB)
            {
                Assert.Contains(".db", projectFileContents);
            }
            Assert.Contains("Microsoft.VisualStudio.Web.CodeGeneration.Design", projectFileContents);

            if (targetFrameworkOverride != null)
            {
                Assert.Contains("Microsoft.AspNetCore.HttpsPolicy", projectFileContents);
            }

            RunDotNetEfCreateMigration("mvc");

            AssertEmptyMigration("mvc");

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
