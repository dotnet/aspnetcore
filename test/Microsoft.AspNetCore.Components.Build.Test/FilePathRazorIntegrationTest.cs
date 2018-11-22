// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Xunit;

namespace Microsoft.AspNetCore.Components.Build.Test
{
    // Integration tests focused on file path handling for class/namespace names
    public class FilePathRazorIntegrationTest : RazorIntegrationTestBase
    {
        [Fact]
        public void FileNameIsInvalidClassName_SanitizesInvalidClassName()
        {
            // Arrange

            // Act
            var result = CompileToAssembly("Filename with spaces.cshtml", "");

            // Assert
            Assert.Empty(result.Diagnostics);

            var type = Assert.Single(result.Assembly.GetTypes());
            Assert.Equal(DefaultBaseNamespace, type.Namespace);
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
