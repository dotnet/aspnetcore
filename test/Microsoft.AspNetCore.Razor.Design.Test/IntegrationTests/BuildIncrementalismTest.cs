// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class BuildIncrementalismTest : MSBuildIntegrationTestBase, IClassFixture<BuildServerTestFixture>
    {
        public BuildIncrementalismTest(BuildServerTestFixture buildServer)
            : base(buildServer)
        {
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task BuildIncremental_SimpleMvc_PersistsTargetInputFile()
        {
            // Arrange
            var thumbprintLookup = new Dictionary<string, FileThumbPrint>();

            // Act 1
            var result = await DotnetMSBuild("Build");

            var directoryPath = Path.Combine(result.Project.DirectoryPath, IntermediateOutputPath);
            var filesToIgnore = new[]
            {
                // These files are generated on every build.
                Path.Combine(directoryPath, "SimpleMvc.csproj.CopyComplete"),
                Path.Combine(directoryPath, "SimpleMvc.csproj.FileListAbsolute.txt"),
            };
            var files = Directory.GetFiles(directoryPath).Where(p => !filesToIgnore.Contains(p));
            foreach (var file in files)
            {
                var thumbprint = GetThumbPrint(file);
                thumbprintLookup[file] = thumbprint;
            }

            // Assert 1
            Assert.BuildPassed(result);

            // Act & Assert 2
            for (var i = 0; i < 2; i++)
            {
                // We want to make sure nothing changed between multiple incremental builds.
                using (var razorGenDirectoryLock = LockDirectory(RazorIntermediateOutputPath))
                {
                    result = await DotnetMSBuild("Build");
                }

                Assert.BuildPassed(result);
                foreach (var file in files)
                {
                    var thumbprint = GetThumbPrint(file);
                    Assert.Equal(thumbprintLookup[file], thumbprint);
                }
            }
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorGenerate_RegeneratesTagHelperInputs_IfFileChanges()
        {
            // Act - 1
            var expectedTagHelperCacheContent = @"""Name"":""SimpleMvc.SimpleTagHelper""";
            var result = await DotnetMSBuild("Build");
            var file = Path.Combine(Project.DirectoryPath, "SimpleTagHelper.cs");
            var tagHelperOutputCache = Path.Combine(IntermediateOutputPath, "SimpleMvc.TagHelpers.output.cache");
            var generatedFile = Path.Combine(RazorIntermediateOutputPath, "Views", "Home", "Index.cs");

            // Assert - 1
            Assert.BuildPassed(result);
            Assert.FileContains(result, tagHelperOutputCache, expectedTagHelperCacheContent);
            var fileThumbPrint = GetThumbPrint(generatedFile);

            // Act - 2
            // Update the source content and build. We should expect the outputs to be regenerated.
            ReplaceContent(string.Empty, file);
            result = await DotnetMSBuild("Build");

            // Assert - 2
            Assert.BuildPassed(result);
            Assert.FileContentEquals(result, tagHelperOutputCache, "[]");
            var newThumbPrint = GetThumbPrint(generatedFile);
            Assert.NotEqual(fileThumbPrint, newThumbPrint);
        }

    }
}
