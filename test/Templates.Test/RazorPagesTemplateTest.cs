// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class RazorPagesTemplateTest : TemplateTestBase
    {
        public RazorPagesTemplateTest(ITestOutputHelper output) : base(output)
        {
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public void RazorPagesTemplate_NoAuth_Works_NetFramework()
            => RazorPagesTemplate_NoAuthImpl("net461");

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public void RazorPagesTemplate_NoAuth_NoHttps_Works_NetFramework()
            => RazorPagesTemplate_NoAuthImpl("net471", true);

        [Fact]
        public void RazorPagesTemplate_NoAuth_Works_NetCore()
            => RazorPagesTemplate_NoAuthImpl(null);

        private void RazorPagesTemplate_NoAuthImpl(string targetFrameworkOverride, bool noHttps = false)
        {
            RunDotNetNew("razor", targetFrameworkOverride, noHttps: noHttps);

            AssertFileExists("Pages/Shared/_LoginPartial.cshtml", false);

            var projectFileContents = ReadFile($"{ProjectName}.csproj");
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
                    aspNetProcess.AssertOk("/About");
                    aspNetProcess.AssertOk("/Contact");
                }
            }
        }

        [ConditionalFact(Skip = "https://github.com/aspnet/templating/issues/378")]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public void RazorPagesTemplate_IndividualAuth_Works_NetFramework()
            => RazorPagesTemplate_IndividualAuthImpl("net461");

        [ConditionalFact(Skip = "https://github.com/aspnet/templating/issues/378")]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public void RazorPagesTemplate_WithIndividualAuth_NoHttpsSetToTrue_UsesHttps_NetFramework()
            => RazorPagesTemplate_IndividualAuthImpl("net471", false, true);

        [Fact(Skip = "https://github.com/aspnet/templating/issues/378")]
        public void RazorPagesTemplate_IndividualAuth_Works_NetCore()
            => RazorPagesTemplate_IndividualAuthImpl(null);

        [Fact(Skip = "https://github.com/aspnet/templating/issues/378")]
        public void RazorPagesTemplate_IndividualAuth_UsingLocalDB_Works_NetCore()
            => RazorPagesTemplate_IndividualAuthImpl(null, true);

        private void RazorPagesTemplate_IndividualAuthImpl(string targetFrameworkOverride, bool useLocalDB = false, bool noHttps = false)
        {
            RunDotNetNew("razor", targetFrameworkOverride, auth: "Individual", useLocalDB: useLocalDB);

            AssertFileExists("Pages/Shared/_LoginPartial.cshtml", true);

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

            RunDotNetEfCreateMigration("razorpages");

            AssertEmptyMigration("razorpages");

            foreach (var publish in new[] { false, true })
            {
                using (var aspNetProcess = StartAspNetProcess(targetFrameworkOverride, publish))
                {
                    aspNetProcess.AssertOk("/");
                    aspNetProcess.AssertOk("/About");
                    aspNetProcess.AssertOk("/Contact");
                }
            }
        }
    }
}
