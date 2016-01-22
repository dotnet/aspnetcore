// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNet.Razor.CodeGenerators
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

            // Act
            writer.WriteLineNumberDirective(sourceLocation, filePath);
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

            // Act
            writer.WriteLineNumberDirective(sourceLocation, filePath);
            var code = writer.GenerateCode();

            // Assert
            Assert.Equal(expected, code);
        }
    }
}
