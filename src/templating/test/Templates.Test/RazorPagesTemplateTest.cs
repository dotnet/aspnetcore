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

        [Fact]
        private void RazorPagesTemplate_NoAuthImpl()
        {
            RunDotNetNew("razor");

            AssertFileExists("Pages/Shared/_LoginPartial.cshtml", false);

            var projectFileContents = ReadFile($"{ProjectName}.csproj");
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
                    aspNetProcess.AssertOk("/Privacy");
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void RazorPagesTemplate_IndividualAuthImpl( bool useLocalDB)
        {
            RunDotNetNew("razor", auth: "Individual", useLocalDB: useLocalDB);

            AssertFileExists("Pages/Shared/_LoginPartial.cshtml", true);

            var projectFileContents = ReadFile($"{ProjectName}.csproj");
            if (!useLocalDB)
            {
                Assert.Contains(".db", projectFileContents);
            }

            RunDotNetEfCreateMigration("razorpages");

            AssertEmptyMigration("razorpages");

            foreach (var publish in new[] { false, true })
            {
                using (var aspNetProcess = StartAspNetProcess(publish))
                {
                    aspNetProcess.AssertOk("/");
                    aspNetProcess.AssertOk("/Privacy");
                }
            }
        }
    }
}
