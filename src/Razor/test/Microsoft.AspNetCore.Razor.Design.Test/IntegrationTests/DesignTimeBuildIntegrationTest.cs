// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class DesignTimeBuildIntegrationTest : MSBuildIntegrationTestBase, IClassFixture<BuildServerTestFixture>
    {
        public DesignTimeBuildIntegrationTest(BuildServerTestFixture buildServer)
            : base(buildServer)
        {
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task DesignTimeBuild_DoesNotRunRazorTargets()
        {
            // Using Compile here instead of CompileDesignTime because the latter is only defined when using
            // the VS targets. This is a close enough simulation for an SDK project
            var result = await DotnetMSBuild("Compile", "/p:DesignTimeBuild=true /clp:PerformanceSummary");

            Assert.BuildPassed(result);
            Assert.FileDoesNotExist(result, OutputPath, "SimpleMvc.dll");
            Assert.FileDoesNotExist(result, OutputPath, "SimpleMvc.pdb");
            Assert.FileDoesNotExist(result, OutputPath, "SimpleMvc.Views.dll");
            Assert.FileDoesNotExist(result, OutputPath, "SimpleMvc.Views.pdb");
            
            // This target should be part of the design time build.
            Assert.Contains("RazorGetAssemblyAttributes", result.Output);

            // We don't want to see the expensive Razor targets in the performance summary, since they shouldn't run
            // during a design time build.
            Assert.DoesNotContain("RazorCoreGenerate", result.Output);
            Assert.DoesNotContain("RazorCoreCompile", result.Output);
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorGenerateDesignTime_ReturnsRazorGenerateWithTargetPath()
        {
            var result = await DotnetMSBuild("RazorGenerateDesignTime;_IntrospectRazorGenerateWithTargetPath");

            Assert.BuildPassed(result);

            var filePaths = new string[]
            {
                Path.Combine("Views", "Home", "About.cshtml"),
                Path.Combine("Views", "Home", "Contact.cshtml"),
                Path.Combine("Views", "Home", "Index.cshtml"),
                Path.Combine("Views", "Shared", "Error.cshtml"),
                Path.Combine("Views", "Shared", "_Layout.cshtml"),
                Path.Combine("Views", "Shared", "_ValidationScriptsPartial.cshtml"),
                Path.Combine("Views", "_ViewImports.cshtml"),
                Path.Combine("Views", "_ViewStart.cshtml"),
            };
            
            foreach (var filePath in filePaths)
            {
                Assert.BuildOutputContainsLine(
                    result, 
                    $@"RazorGenerateWithTargetPath: {filePath} {filePath} {Path.Combine(RazorIntermediateOutputPath, Path.ChangeExtension(filePath, ".g.cshtml.cs"))}");
            }
        }
    }
}
