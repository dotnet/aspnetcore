// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Runtime.InteropServices;
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
            TargetFramework = "netstandard2.0";
            var result = await DotnetMSBuild("Pack", "/p:RazorCompileOnBuild=true");

            Assert.BuildPassed(result);

            Assert.FileExists(result, OutputPath, "ClassLibrary.dll");
            Assert.FileExists(result, OutputPath, "ClassLibrary.Views.dll");
            
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Travis on OSX produces different full paths in C# and MSBuild
                Assert.NuspecContains(
                    result,
                    Path.Combine("obj", Configuration, "ClassLibrary.1.0.0.nuspec"),
                    $"<file src=\"{Path.Combine(Project.DirectoryPath, "bin", Configuration, "netstandard2.0", "ClassLibrary.Views.dll")}\" " +
                    $"target=\"{Path.Combine("lib", "netstandard2.0", "ClassLibrary.Views.dll")}\" />");

                Assert.NuspecDoesNotContain(
                    result,
                    Path.Combine("obj", Configuration, "ClassLibrary.1.0.0.nuspec"),
                    $"<file src=\"{Path.Combine(Project.DirectoryPath, "bin", Configuration, "netstandard2.0", "ClassLibrary.Views.pdb")}\" " +
                    $"target=\"{Path.Combine("lib", "netstandard2.0", "ClassLibrary.Views.pdb")}\" />");
            }

            Assert.NuspecDoesNotContain(
                result,
                Path.Combine("obj", Configuration, "ClassLibrary.1.0.0.nuspec"),
                @"<files include=""any/netstandard2.0/Views/Shared/_Layout.cshtml"" buildAction=""Content"" />");

            Assert.NupkgContains(
                result,
                Path.Combine("bin", Configuration, "ClassLibrary.1.0.0.nupkg"),
                Path.Combine("lib", "netstandard2.0", "ClassLibrary.Views.dll"));
        }

        [Fact]
        [InitializeTestProject("ClassLibrary")]
        public async Task Pack_WithIncludeSymbols_IncludesRazorPdb()
        {
            var result = await DotnetMSBuild("Pack", "/p:RazorCompileOnBuild=true /p:IncludeSymbols=true");

            Assert.BuildPassed(result);

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Travis on OSX produces different full paths in C# and MSBuild
                Assert.NuspecContains(
                    result,
                    Path.Combine("obj", Configuration, "ClassLibrary.1.0.0.symbols.nuspec"),
                    $"<file src=\"{Path.Combine(Project.DirectoryPath, "bin", Configuration, "netstandard2.0", "ClassLibrary.Views.dll")}\" " +
                    $"target=\"{Path.Combine("lib", "netstandard2.0", "ClassLibrary.Views.dll")}\" />");

                Assert.NuspecContains(
                    result,
                    Path.Combine("obj", Configuration, "ClassLibrary.1.0.0.symbols.nuspec"),
                    $"<file src=\"{Path.Combine(Project.DirectoryPath, "bin", Configuration, "netstandard2.0", "ClassLibrary.Views.pdb")}\" " +
                    $"target=\"{Path.Combine("lib", "netstandard2.0", "ClassLibrary.Views.pdb")}\" />");
            }

            Assert.NupkgContains(
                result,
                Path.Combine("bin", Configuration, "ClassLibrary.1.0.0.symbols.nupkg"),
                Path.Combine("lib", "netstandard2.0", "ClassLibrary.Views.dll"),
                Path.Combine("lib", "netstandard2.0", "ClassLibrary.Views.pdb"));
        }

        [Fact]
        [InitializeTestProject("ClassLibrary")]
        public async Task Pack_IncludesRazorFilesAsContent_WhenIncludeRazorContentInPack_IsSet()
        {
            TargetFramework = "netstandard2.0";
            var result = await DotnetMSBuild("Pack", "/p:RazorCompileOnBuild=true /p:IncludeRazorContentInPack=true");

            Assert.BuildPassed(result);

            Assert.FileExists(result, OutputPath, "ClassLibrary.dll");
            Assert.FileExists(result, OutputPath, "ClassLibrary.Views.dll");

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Travis on OSX produces different full paths in C# and MSBuild
                Assert.NuspecContains(
                    result,
                    Path.Combine("obj", Configuration, "ClassLibrary.1.0.0.nuspec"),
                    $"<file src=\"{Path.Combine(Project.DirectoryPath, "bin", Configuration, "netstandard2.0", "ClassLibrary.Views.dll")}\" " +
                    $"target=\"{Path.Combine("lib", "netstandard2.0", "ClassLibrary.Views.dll")}\" />");

                Assert.NuspecContains(
                    result,
                    Path.Combine("obj", Configuration, "ClassLibrary.1.0.0.nuspec"),
                    @"<files include=""any/netstandard2.0/Views/Shared/_Layout.cshtml"" buildAction=""Content"" />");
            }

            Assert.NupkgContains(
                result,
                Path.Combine("bin", Configuration, "ClassLibrary.1.0.0.nupkg"),
                Path.Combine("lib", "netstandard2.0", "ClassLibrary.Views.dll"));
        }
    }
}
