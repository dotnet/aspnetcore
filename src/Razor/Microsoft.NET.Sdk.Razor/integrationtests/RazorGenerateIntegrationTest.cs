// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class RazorGenerateIntegrationTest : MSBuildIntegrationTestBase, IClassFixture<BuildServerTestFixture>
    {
        public RazorGenerateIntegrationTest(BuildServerTestFixture buildServer)
            : base(buildServer)
        {
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorGenerate_Success_GeneratesFilesOnDisk()
        {
            var result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);

            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "_ViewImports.cshtml.g.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "_ViewStart.cshtml.g.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Home", "About.cshtml.g.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Home", "Contact.cshtml.g.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Home", "Index.cshtml.g.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Shared", "_Layout.cshtml.g.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Shared", "_ValidationScriptsPartial.cshtml.g.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Shared", "Error.cshtml.g.cs");
            Assert.FileCountEquals(result, 8, RazorIntermediateOutputPath, "*.cshtml.g.cs");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorGenerate_ErrorInRazorFile_ReportsMSBuildError()
        {
            // Introducing a syntax error, an unclosed brace
            ReplaceContent("@{", "Views", "Home", "Index.cshtml");

            var result = await DotnetMSBuild("Build");

            Assert.BuildFailed(result);

            // Looks like C:\...\Views\Home\Index.cshtml(1,2): error RZ1006: The code block is missi... [C:\Users\rynowak\AppData\Local\Temp\rwnv03ll.wb0\SimpleMvc.csproj]
            Assert.BuildError(result, "RZ1006");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorGenerate_BuildsIncrementally()
        {
            // Act - 1
            var result = await DotnetMSBuild("Build");
            var generatedFile = Path.Combine(Project.DirectoryPath, RazorIntermediateOutputPath, "Views", "Home", "About.cshtml.g.cs");

            // Assert - 1
            Assert.BuildPassed(result);
            Assert.FileExists(result, generatedFile);
            var thumbPrint = GetThumbPrint(generatedFile);

            // Act - 2
            using (var razorGenDirectoryLock = LockDirectory(RazorIntermediateOutputPath))
            {
                result = await DotnetMSBuild("Build");
            }

            // Assert - 2
            Assert.BuildPassed(result);
            Assert.FileExists(result, generatedFile);
            var currentThumbPrint = GetThumbPrint(generatedFile);
            Assert.Equal(thumbPrint, currentThumbPrint);
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorGenerate_Rebuilds_IfSourcesAreUpdated()
        {
            // Act - 1
            var result = await DotnetMSBuild("Build");
            var file = Path.Combine(Project.DirectoryPath, "Views", "Home", "Contact.cshtml");
            var generatedFile = Path.Combine(RazorIntermediateOutputPath, "Views", "Home", "Contact.cshtml.g.cs");

            // Assert - 1
            Assert.BuildPassed(result);
            var fileThumbPrint = GetThumbPrint(generatedFile);

            // Act - 2
            // Update the source content and build. We should expect the outputs to be regenerated.
            ReplaceContent("Uodated content", file);
            result = await DotnetMSBuild("Build");

            // Assert - 2
            Assert.BuildPassed(result);
            var newThumbPrint = GetThumbPrint(generatedFile);
            Assert.NotEqual(fileThumbPrint, newThumbPrint);
        }

        [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/28780")]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorGenerate_Rebuilds_IfOutputFilesAreMissing()
        {
            // Act - 1
            var result = await DotnetMSBuild("Build");
            var file = Path.Combine(Project.DirectoryPath, RazorIntermediateOutputPath, "Views", "Home", "About.cshtml.g.cs");

            // Assert - 1
            Assert.BuildPassed(result);
            Assert.FileExists(result, file);

            // Act - 2
            File.Delete(file);
            result = await DotnetMSBuild("Build");

            // Assert - 2
            Assert.BuildPassed(result);
            Assert.FileExists(result, file);
        }

        [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/28780")]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorGenerate_Rebuilds_IfInputFilesAreRenamed()
        {
            // Act - 1
            var result = await DotnetMSBuild("Build");
            var file = Path.Combine(Project.DirectoryPath, "Views", "Home", "Index.cshtml");
            var renamed = Path.Combine(Project.DirectoryPath, "Views", "Home", "NewIndex.cshtml");
            var generated = Path.Combine(RazorIntermediateOutputPath, "Views", "Home", "Index.cshtml.g.cs");

            // Assert - 1
            Assert.BuildPassed(result);
            Assert.FileExists(result, file);
            Assert.FileExists(result, generated);

            // Act - 2
            File.Move(file, renamed);
            result = await DotnetMSBuild("Build");

            // Assert - 2
            Assert.BuildPassed(result);
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Home", "NewIndex.cshtml.g.cs");
            Assert.FileDoesNotExist(result, generated);
        }

        [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/28780")]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorGenerate_Rebuilds_IfInputFilesAreDeleted()
        {
            // Act - 1
            var result = await DotnetMSBuild("Build");
            var file = Path.Combine(Project.DirectoryPath, "Views", "Home", "Index.cshtml");
            var generatedFile = Path.Combine(RazorIntermediateOutputPath, "Views", "Home", "Index.cshtml.g.cs");

            // Assert - 1
            Assert.BuildPassed(result);
            Assert.FileExists(result, generatedFile);

            // Act - 2
            File.Delete(file);
            result = await DotnetMSBuild("Build");

            // Assert - 2
            Assert.BuildPassed(result);
            Assert.FileDoesNotExist(result, generatedFile);
        }

        [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/28780")]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorGenerate_Noops_WithNoFiles()
        {
            Directory.Delete(Path.Combine(Project.DirectoryPath, "Views"), recursive: true);

            var result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);

            // We shouldn't need to look for tag helpers
            Assert.FileDoesNotExist(result, Path.Combine(IntermediateOutputPath, "SimpleMvc.TagHelpers.input.cache"));
            Assert.FileDoesNotExist(result, Path.Combine(IntermediateOutputPath, "SimpleMvc.TagHelpers.output.cache"));

            // We shouldn't need to hash the files
            Assert.FileDoesNotExist(result, Path.Combine(IntermediateOutputPath, "SimpleMvc.RazorCoreGenerate.cache"));

            Assert.FileCountEquals(result, 0, RazorIntermediateOutputPath, "*.cshtml.g.cs");
        }

        [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/28780")]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorGenerate_MvcRazorFilesToCompile_OverridesDefaultItems()
        {
            var projectContent = @"
<ItemGroup>
  <MvcRazorFilesToCompile Include=""Views/Home/About.cshtml"" />
</ItemGroup>
";
            AddProjectFileContent(projectContent);

            var result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);

            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Home", "About.cshtml.g.cs");
            Assert.FileCountEquals(result, 1, RazorIntermediateOutputPath, "*.cshtml.g.cs");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorGenerate_EnableDefaultRazorGenerateItems_False_OverridesDefaultItems()
        {
            var projectContent = @"
 <PropertyGroup>
   <EnableDefaultRazorGenerateItems>false</EnableDefaultRazorGenerateItems>
 </PropertyGroup>
 <ItemGroup>
   <RazorGenerate Include=""Views/Home/About.cshtml"" />
 </ItemGroup>
";
            AddProjectFileContent(projectContent);

            var result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);

            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Home", "About.cshtml.g.cs");
            Assert.FileCountEquals(result, 1, RazorIntermediateOutputPath, "*.cshtml.g.cs");
        }

        [Fact(Skip = "Blocked until https://github.com/dotnet/roslyn/issues/50090 is resolved.")]
        [InitializeTestProject("SimpleMvc", additionalProjects: "LinkedDir")]
        public async Task RazorGenerate_PrintsErrorsFromLinkedFiles()
        {
            // Arrange
            var file = @"..\LinkedDir\LinkedErrorFile.cshtml";
            var projectContent = $@"
<ItemGroup>
  <Content Include=""{file}"" Link=""LinkedFileOut\LinkedFile.cshtml"" />
</ItemGroup>
";
            AddProjectFileContent(projectContent);

            var result = await DotnetMSBuild("Build");

            Assert.BuildFailed(result);
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // GetFullPath on OSX doesn't work well in travis. We end up computing a different path than will
                // end up in the MSBuild logs.
                var errorLocation = Path.GetFullPath(Path.Combine(Project.DirectoryPath, "..", "LinkedDir", "LinkedErrorFile.cshtml")) + "(1,2)";
                Assert.BuildError(result, "RZ1006", errorLocation);
            }
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorGenerate_FileWithAbsolutePath()
        {
            // In preview1 we totally ignore files that are specified with an absolute path
            var filePath = Path.Combine(Project.SolutionPath, "temp.cshtml");
            File.WriteAllText(filePath, string.Empty);

            AddProjectFileContent($@"
<ItemGroup>
  <Content Include=""{filePath}""/>
</ItemGroup>");

            var result = await DotnetMSBuild("Build", "/t:_IntrospectRazorGenerateWithTargetPath");

            Assert.BuildPassed(result);

            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "_ViewImports.cshtml.g.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "_ViewStart.cshtml.g.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Home", "About.cshtml.g.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Home", "Contact.cshtml.g.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Home", "Index.cshtml.g.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Shared", "_Layout.cshtml.g.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Shared", "_ValidationScriptsPartial.cshtml.g.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Shared", "Error.cshtml.g.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "temp.cshtml.g.cs");
            Assert.FileCountEquals(result, 9, RazorIntermediateOutputPath, "*.cshtml.g.cs");
            Assert.BuildOutputContainsLine(result, $@"RazorGenerateWithTargetPath: {filePath} temp.cshtml {Path.Combine(RazorIntermediateOutputPath, "temp.cshtml.g.cs")}");
        }
    }
}
