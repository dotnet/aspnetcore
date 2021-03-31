// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    // Integration tests focused on file path handling for class/namespace names
    public class ComponentFilePathIntegrationTest : RazorIntegrationTestBase
    {
        internal override string FileKind => FileKinds.Component; 

        [Fact]
        public void FileNameIsInvalidClassName_SanitizesInvalidClassName()
        {
            // Arrange

            // Act
            var result = CompileToAssembly("Filename with spaces.cshtml", "");

            // Assert
            Assert.Empty(result.Diagnostics);

            var type = Assert.Single(result.Assembly.GetTypes());
            Assert.Equal(DefaultRootNamespace, type.Namespace);
            Assert.Equal("Filename_with_spaces", type.Name);
        }

        [Theory]
        [InlineData("ItemAtRoot.cs", "Test", "ItemAtRoot")]
        [InlineData("Dir1\\MyFile.cs", "Test.Dir1", "MyFile")]
        [InlineData("Dir1\\Dir2\\MyFile.cs", "Test.Dir1.Dir2", "MyFile")]
        public void CreatesClassWithCorrectNameAndNamespace(string relativePath, string expectedNamespace, string expectedClassName)
        {
            // Arrange
            relativePath = relativePath.Replace('\\', Path.DirectorySeparatorChar);

            // Act
            var result = CompileToAssembly(relativePath, "");

            // Assert
            Assert.Empty(result.Diagnostics);

            var type = Assert.Single(result.Assembly.GetTypes());
            Assert.Equal(expectedNamespace, type.Namespace);
            Assert.Equal(expectedClassName, type.Name);
        }
    }
}
