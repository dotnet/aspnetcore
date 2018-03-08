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
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public void RazorPagesTemplate_NoAuth_Works_NetFramework()
            => RazorPagesTemplate_NoAuthImpl("net461");

        [Fact]
        public void RazorPagesTemplate_NoAuth_Works_NetCore()
            => RazorPagesTemplate_NoAuthImpl(null);

        private void RazorPagesTemplate_NoAuthImpl(string targetFrameworkOverride)
        {
            RunDotNetNew("razor", targetFrameworkOverride);

            AssertFileExists("Pages/Shared/_LoginPartial.cshtml", false);

            var projectFileContents = ReadFile($"{ProjectName}.csproj");
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
                    aspNetProcess.AssertOk("/About");
                    aspNetProcess.AssertOk("/Contact");
                }
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public void RazorPagesTemplate_IndividualAuth_Works_NetFramework()
            => RazorPagesTemplate_IndividualAuthImpl("net461");

        [Fact]
        public void RazorPagesTemplate_IndividualAuth_Works_NetCore()
            => RazorPagesTemplate_IndividualAuthImpl(null);
        [Fact]
        public void RazorPagesTemplate_IndividualAuth_UsingLocalDB_Works_NetCore()
            => RazorPagesTemplate_IndividualAuthImpl(null, true);

        private void RazorPagesTemplate_IndividualAuthImpl(string targetFrameworkOverride, bool useLocalDB = false)
        {
            RunDotNetNew("razor", targetFrameworkOverride, auth: "Individual", useLocalDB: useLocalDB);

            AssertFileExists("Pages/Shared/_LoginPartial.cshtml", true);

            var projectFileContents = ReadFile($"{ProjectName}.csproj");
            if (!useLocalDB)
            {
                Assert.Contains(".db", projectFileContents);
            }
            Assert.Contains("Microsoft.EntityFrameworkCore.Tools", projectFileContents);
            Assert.Contains("Microsoft.VisualStudio.Web.CodeGeneration.Design", projectFileContents);
            Assert.Contains("Microsoft.EntityFrameworkCore.Tools.DotNet", projectFileContents);

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
