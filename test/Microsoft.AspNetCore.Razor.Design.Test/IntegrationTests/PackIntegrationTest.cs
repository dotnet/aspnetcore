// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class PackIntegrationTest : MSBuildIntegrationTestBase
    {
        [Fact]
        [InitializeTestProject("ClassLibrary")]
        public async Task Pack_Works_IncludesRazorAssembly()
        {
            var result = await DotnetMSBuild("Pack", "/p:RazorCompileOnBuild=true");

            Assert.BuildPassed(result);

            Assert.FileExists(result, OutputPath, "ClassLibrary.dll");
            Assert.FileExists(result, OutputPath, "ClassLibrary.PrecompiledViews.dll");
            
            Assert.NuspecContains(
                result,
                Path.Combine("obj", Configuration, "ClassLibrary.1.0.0.nuspec"),
                $"<file src=\"{Path.Combine("bin", Configuration, "netcoreapp2.0", "ClassLibrary.PrecompiledViews.dll")}\" " +
                $"target=\"{Path.Combine("lib", "netcoreapp2.0", "ClassLibrary.PrecompiledViews.dll")}\" />");

            Assert.NupkgContains(
                result,
                Path.Combine("bin", Configuration, "ClassLibrary.1.0.0.nupkg"),
                Path.Combine("lib", "netcoreapp2.0", "ClassLibrary.PrecompiledViews.dll"));
        }
    }
}
