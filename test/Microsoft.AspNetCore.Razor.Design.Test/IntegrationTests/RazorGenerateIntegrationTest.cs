// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class RazorGenerateIntegrationTest : MSBuildIntegrationTestBase
    {
        private const string RazorGenerateTarget = "RazorGenerate";

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorGenerate_Success_GeneratesFilesOnDisk()
        {
            var result = await DotnetMSBuild(RazorGenerateTarget);

            Assert.BuildPassed(result);

            // RazorGenerate should compile the assembly, but not the views.
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.dll");
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "SimpleMvc.Views.dll");

            // RazorGenerate should generate correct TagHelper caches
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.TagHelpers.input.cache");
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.TagHelpers.output.cache");
            Assert.FileContains(
                result,
                Path.Combine(IntermediateOutputPath, "SimpleMvc.TagHelpers.output.cache"),
                @"""Name"":""SimpleMvc.SimpleTagHelper""");

            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "_ViewImports.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "_ViewStart.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Home", "About.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Home", "Contact.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Home", "Index.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Shared", "_Layout.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Shared", "_ValidationScriptsPartial.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Shared", "Error.cs");
            Assert.FileCountEquals(result, 8, RazorIntermediateOutputPath, "*.cs");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorGenerate_ErrorInRazorFile_ReportsMSBuildError()
        {
            // Introducing a syntax error, an unclosed brace
            ReplaceContent("@{", "Views", "Home", "Index.cshtml");

            var result = await DotnetMSBuild(RazorGenerateTarget);

            Assert.BuildFailed(result);

            // Looks like C:\...\Views\Home\Index.cshtml(1,2): error RZ1006: The code block is missi... [C:\Users\rynowak\AppData\Local\Temp\rwnv03ll.wb0\SimpleMvc.csproj]
            Assert.BuildError(result, "RZ1006");

            // RazorGenerate should compile the assembly, but not the views.
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.dll");
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "SimpleMvc.Views.dll");

            // The file should still be generated even if we had a Razor syntax error.
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Home", "Index.cs");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "aspnet/Razor#1888")]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorGenerate_BuildsIncrementally()
        {
            // Act - 1
            var result = await DotnetMSBuild(RazorGenerateTarget);
            var generatedFile = Path.Combine(Project.DirectoryPath, RazorIntermediateOutputPath, "Views", "Home", "About.cs");

            // Assert - 1
            Assert.BuildPassed(result);
            Assert.FileExists(result, generatedFile);
            var thumbPrint = GetThumbPrint(generatedFile);

            // Act - 2
            using (var razorGenDirectoryLock = LockDirectory(RazorIntermediateOutputPath))
            {
                result = await DotnetMSBuild(RazorGenerateTarget);
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
            var result = await DotnetMSBuild(RazorGenerateTarget);
            var file = Path.Combine(Project.DirectoryPath, "Views", "Home", "Contact.cshtml");
            var generatedFile = Path.Combine(RazorIntermediateOutputPath, "Views", "Home", "Contact.cs");

            // Assert - 1
            Assert.BuildPassed(result);
            var fileThumbPrint = GetThumbPrint(generatedFile);

            // Act - 2
            // Update the source content and build. We should expect the outputs to be regenerated.
            ReplaceContent("Uodated content", file);
            result = await DotnetMSBuild(RazorGenerateTarget);

            // Assert - 2
            Assert.BuildPassed(result);
            var newThumbPrint = GetThumbPrint(generatedFile);
            Assert.NotEqual(fileThumbPrint, newThumbPrint);
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorGenerate_Rebuilds_IfOutputFilesAreMissing()
        {
            // Act - 1
            var result = await DotnetMSBuild(RazorGenerateTarget);
            var file = Path.Combine(Project.DirectoryPath, RazorIntermediateOutputPath, "Views", "Home", "About.cs");

            // Assert - 1
            Assert.BuildPassed(result);
            Assert.FileExists(result, file);

            // Act - 2
            File.Delete(file);
            result = await DotnetMSBuild(RazorGenerateTarget);

            // Assert - 2
            Assert.BuildPassed(result);
            Assert.FileExists(result, file);
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorGenerate_Rebuilds_IfInputFilesAreRenamed()
        {
            // Act - 1
            var result = await DotnetMSBuild(RazorGenerateTarget);
            var file = Path.Combine(Project.DirectoryPath, "Views", "Home", "Index.cshtml");
            var renamed = Path.Combine(Project.DirectoryPath, "Views", "Home", "NewIndex.cshtml");
            var generated = Path.Combine(RazorIntermediateOutputPath, "Views", "Home", "Index.cs");

            // Assert - 1
            Assert.BuildPassed(result);
            Assert.FileExists(result, file);
            Assert.FileExists(result, generated);

            // Act - 2
            File.Move(file, renamed);
            result = await DotnetMSBuild(RazorGenerateTarget);

            // Assert - 2
            Assert.BuildPassed(result);
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Home", "NewIndex.cs");
            Assert.FileDoesNotExist(result, generated);
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorGenerate_Rebuilds_IfInputFilesAreDeleted()
        {
            // Act - 1
            var result = await DotnetMSBuild(RazorGenerateTarget);
            var file = Path.Combine(Project.DirectoryPath, "Views", "Home", "Index.cshtml");
            var generatedFile = Path.Combine(RazorIntermediateOutputPath, "Views", "Home", "Index.cs");

            // Assert - 1
            Assert.BuildPassed(result);
            Assert.FileExists(result, generatedFile);

            // Act - 2
            File.Delete(file);
            result = await DotnetMSBuild(RazorGenerateTarget);

            // Assert - 2
            Assert.BuildPassed(result);
            Assert.FileDoesNotExist(result, generatedFile);
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorGenerate_Noops_WithNoFiles()
        {
            Directory.Delete(Path.Combine(Project.DirectoryPath, "Views"), recursive: true);

            var result = await DotnetMSBuild(RazorGenerateTarget);

            Assert.BuildPassed(result);

            // We shouldn't need to look for tag helpers
            Assert.FileDoesNotExist(result, Path.Combine(IntermediateOutputPath, "SimpleMvc.TagHelpers.input.cache"));
            Assert.FileDoesNotExist(result, Path.Combine(IntermediateOutputPath, "SimpleMvc.TagHelpers.output.cache"));

            // We shouldn't need to hash the files
            Assert.FileDoesNotExist(result, Path.Combine(IntermediateOutputPath, "SimpleMvc.RazorCoreGenerate.cache"));

            Assert.FileCountEquals(result, 0, RazorIntermediateOutputPath, "*.cs");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorGenerate_MvcRazorFilesToCompile_OverridesDefaultItems()
        {
            var projectContent = @"
<ItemGroup>
  <MvcRazorFilesToCompile Include=""Views/Home/About.cshtml"" />
</ItemGroup>
";
            AddProjectFileContent(projectContent);

            var result = await DotnetMSBuild(RazorGenerateTarget);

            Assert.BuildPassed(result);

            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Home", "About.cs");
            Assert.FileCountEquals(result, 1, RazorIntermediateOutputPath, "*.cs");
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

            var result = await DotnetMSBuild(RazorGenerateTarget);

            Assert.BuildPassed(result);

            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Home", "About.cs");
            Assert.FileCountEquals(result, 1, RazorIntermediateOutputPath, "*.cs");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc", "LinkedDir")]
        public async Task RazorGenerate_WorksWithLinkedFiles()
        {
            // Arrange
            var projectContent = @"
<ItemGroup>
  <Content Include=""..\LinkedDir\LinkedFile.cshtml"" />
  <Content Include=""..\LinkedDir\LinkedFile2.cshtml"" Link=""LinkedFileOut\LinkedFile2.cshtml"" />
  <Content Include=""..\LinkedDir\LinkedFile3.cshtml"" Link=""LinkedFileOut\LinkedFileWithRename.cshtml"" />
</ItemGroup>
";
            AddProjectFileContent(projectContent);

            var result = await DotnetMSBuild(RazorGenerateTarget, "/t:_IntrospectRazorGenerateWithTargetPath");

            Assert.BuildPassed(result);

            Assert.FileExists(result, RazorIntermediateOutputPath, "LinkedFile.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "LinkedFileOut", "LinkedFile2.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "LinkedFileOut", "LinkedFileWithRename.cs");

            Assert.BuildOutputContainsLine(result, $@"RazorGenerateWithTargetPath: {Path.Combine("..", "LinkedDir", "LinkedFile.cshtml")} LinkedFile.cshtml {Path.Combine(RazorIntermediateOutputPath, "LinkedFile.cs")}");
            Assert.BuildOutputContainsLine(result, $@"RazorGenerateWithTargetPath: {Path.Combine("..", "LinkedDir", "LinkedFile2.cshtml")} LinkedFileOut\LinkedFile2.cshtml {Path.Combine(RazorIntermediateOutputPath, "LinkedFileOut", "LinkedFile2.cs")}");
            Assert.BuildOutputContainsLine(result, $@"RazorGenerateWithTargetPath: {Path.Combine("..", "LinkedDir", "LinkedFile3.cshtml")} LinkedFileOut\LinkedFileWithRename.cshtml {Path.Combine(RazorIntermediateOutputPath, "LinkedFileOut", "LinkedFileWithRename.cs")}");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc", "LinkedDir")]
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

            var result = await DotnetMSBuild(RazorGenerateTarget);

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

            var result = await DotnetMSBuild(RazorGenerateTarget, "/t:_IntrospectRazorGenerateWithTargetPath");

            Assert.BuildPassed(result);

            // RazorGenerate should compile the assembly, but not the views.
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.dll");
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "SimpleMvc.Views.dll");

            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "_ViewImports.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "_ViewStart.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Home", "About.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Home", "Contact.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Home", "Index.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Shared", "_Layout.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Shared", "_ValidationScriptsPartial.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Shared", "Error.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "temp.cs");
            Assert.FileCountEquals(result, 9, RazorIntermediateOutputPath, "*.cs");
            Assert.BuildOutputContainsLine(result, $@"RazorGenerateWithTargetPath: {filePath} temp.cshtml {Path.Combine(RazorIntermediateOutputPath, "temp.cs")}");
        }
    }
}
