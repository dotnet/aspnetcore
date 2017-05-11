// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpCodeWriterTest
    {
        [Fact]
        public void WriteLineNumberDirective_UsesFilePath_WhenFileInSourceLocationIsNull()
        {
            // Arrange
            var filePath = "some-path";
            var writer = new CSharpCodeWriter();
            var expected = $"#line 5 \"{filePath}\"" + writer.NewLine;
            var sourceLocation = new SourceLocation(10, 4, 3);
            var mappingLocation = new SourceSpan(sourceLocation, 9);

            // Act
            writer.WriteLineNumberDirective(mappingLocation, filePath);
            var code = writer.GenerateCode();

            // Assert
            Assert.Equal(expected, code);
        }

        [Theory]
        [InlineData("")]
        [InlineData("source-location-file-path")]
        public void WriteLineNumberDirective_UsesSourceLocationFilePath_IfAvailable(
            string sourceLocationFilePath)
        {
            // Arrange
            var filePath = "some-path";
            var writer = new CSharpCodeWriter();
            var expected = $"#line 5 \"{sourceLocationFilePath}\"" + writer.NewLine;
            var sourceLocation = new SourceLocation(sourceLocationFilePath, 10, 4, 3);
            var mappingLocation = new SourceSpan(sourceLocation, 9);

            // Act
            writer.WriteLineNumberDirective(mappingLocation, filePath);
            var code = writer.GenerateCode();

            // Assert
            Assert.Equal(expected, code);
        }

        [Fact]
        public void WriteField_WritesFieldDeclaration()
        {
            // Arrange
            var writer = new CSharpCodeWriter();

            // Act
            writer.WriteField("private", "global::System.String", "_myString");

            // Assert
            var output = writer.GenerateCode();
            Assert.Equal("private global::System.String _myString;" + Environment.NewLine, output);
        }

        [Fact]
        public void WriteField_WithModifiers_WritesFieldDeclaration()
        {
            // Arrange
            var writer = new CSharpCodeWriter();

            // Act
            writer.WriteField("private", new[] { "readonly", "static" }, "global::System.String", "_myString");

            // Assert
            var output = writer.GenerateCode();
            Assert.Equal("private readonly static global::System.String _myString;" + Environment.NewLine, output);
        }

        [Fact]
        public void WriteAutoPropertyDeclaration_WritesPropertyDeclaration()
        {
            // Arrange
            var writer = new CSharpCodeWriter();

            // Act
            writer.WriteAutoPropertyDeclaration("public", "global::System.String", "MyString");

            // Assert
            var output = writer.GenerateCode();
            Assert.Equal("public global::System.String MyString { get; set; }" + Environment.NewLine, output);
        }

        [Fact]
        public void WriteAutoPropertyDeclaration_WithModifiers_WritesPropertyDeclaration()
        {
            // Arrange
            var writer = new CSharpCodeWriter();

            // Act
            writer.WriteAutoPropertyDeclaration("public", new[] { "static" }, "global::System.String", "MyString");

            // Assert
            var output = writer.GenerateCode();
            Assert.Equal("public static global::System.String MyString { get; set; }" + Environment.NewLine, output);
        }
    }
}
