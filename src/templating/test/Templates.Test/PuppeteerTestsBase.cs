// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class PuppeteerTestsBase : TemplateTestBase
    {
        protected const string IndividualAuth = "Individual";

        public PuppeteerTestsBase(ITestOutputHelper output) : base(output)
        {
        }

        private static readonly string TestDir = Path.Join(TestPathUtilities.GetSolutionRootDirectory("Templating"), "test", "Templates.Test");
        protected static readonly string PuppeteerDir = Path.Join(TestDir, "PuppeteerTests");

        protected async Task<ProcessResult> RunTest(string test)
        {
            ProcessStartInfo processStartInfo;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                processStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd",
                    WorkingDirectory = PuppeteerDir,
                    Arguments = $"/c npm run test-{test}"
                };
            }
            else
            {
                processStartInfo = new ProcessStartInfo
                {
                    FileName = "npm",
                    WorkingDirectory = PuppeteerDir,
                    Arguments = $"run test-{test}"
                };
            }

            // Act
            return await ProcessManager.RunProcessAsync(processStartInfo);
        }

        protected async Task TemplateBase(
            string templateName,
            string targetFrameworkOverride,
            int httpPort,
            int httpsPort,
            string languageOverride = default,
            string auth = null,
            bool noHttps = false,
            bool useLocalDb = false)
        {
            using (StartLog(out var loggerFactory))
            {
                RunDotNetNew(templateName, targetFrameworkOverride, auth, language: languageOverride, useLocalDb, noHttps);

                var projectExtension = languageOverride == "F#" ? "fsproj" : "csproj";
                var projectFileContents = ReadFile($"{ProjectName}.{projectExtension}");

                Assert.DoesNotContain("Microsoft.VisualStudio.Web.CodeGeneration.Design", projectFileContents);
                Assert.DoesNotContain("Microsoft.EntityFrameworkCore.Tools.DotNet", projectFileContents);

                if (auth == IndividualAuth)
                {
                    if (targetFrameworkOverride != null)
                    {
                        Assert.Contains("Microsoft.EntityFrameworkCore.Tools", projectFileContents);
                    }

                    if (!useLocalDb)
                    {
                        Assert.Contains(".db", projectFileContents);
                    }
                    else
                    {
                        Assert.DoesNotContain(".db", projectFileContents);
                    }
                }
                else
                {
                    Assert.DoesNotContain("Microsoft.EntityFrameworkCore.Tools", projectFileContents);
                }

                if (targetFrameworkOverride != null && !noHttps && templateName != "web")
                {
                    Assert.Contains("Microsoft.AspNetCore.HttpsPolicy", projectFileContents);
                }

                await RunPuppeteerTests(templateName, targetFrameworkOverride, httpPort, httpsPort);
            }
        }

        protected async Task RunPuppeteerTests(string templateName, string targetFrameworkOverride, int httpPort, int httpsPort)
        {
            foreach (var publish in new[] { false, true })
            {
                // Arrange
                using (var aspNetProcess = StartAspNetProcess(targetFrameworkOverride, publish, httpPort, httpsPort))
                {
                    // Act
                    var testResult = await RunTest(templateName);

                    // Assert
                    AssertNpmTest.Success(testResult);
                    Assert.Contains("Test Suites: 1 passed, 1 total", testResult.Output);
                }
            }
        }
    }
}
