// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNet.Razor.CodeGenerators
{
    public class CSharpLineMappingWriterTest
    {
        [Fact]
        public void WriterConstructedWithContentLength_AddsLineMappings_OnDispose()
        {
            // Arrange
            var location = new SourceLocation(10, 15, 20);
            var expected = new LineMapping(
                                new MappingLocation(location, 30),
                                new MappingLocation(new SourceLocation(0, 0, 0), 11));
            var writer = new CSharpCodeWriter();

            // Act
            using (var mappingWriter = new CSharpLineMappingWriter(writer, location, 30))
            {
                writer.Write("Hello world");
            }

            // Assert
            Assert.Equal("Hello world", writer.GenerateCode());
            var mapping = Assert.Single(writer.LineMappingManager.Mappings);
            Assert.Equal(expected, mapping);
        }

        [Fact]
        public void WriterConstructedWithContentLengthAndSourceFile_AddsLineMappingsAndLinePragmas_OnDispose()
        {
            // Arrange
            var location = new SourceLocation(10, 1, 20);
            var expected = string.Join(Environment.NewLine,
                                       @"#line 2 ""myfile""",
                                       "Hello world",
                                       "",
                                       "#line default",
                                       "#line hidden",
                                       "");
            var expectedMappings = new LineMapping(
                                new MappingLocation(location, 30),
                                new MappingLocation(new SourceLocation(16 + Environment.NewLine.Length, 1, 0), 11));
            var writer = new CSharpCodeWriter();

            // Act
            using (var mappingWriter = new CSharpLineMappingWriter(writer, location, 30, "myfile"))
            {
                writer.Write("Hello world");
            }

            // Assert
            Assert.Equal(expected, writer.GenerateCode());
            var mapping = Assert.Single(writer.LineMappingManager.Mappings);
            Assert.Equal(expectedMappings, mapping);
        }

        [Fact]
        public void WriterConstructedWithoutContentLengthAndSourceFile_AddsLinePragmas_OnDispose()
        {
            // Arrange
            var location = new SourceLocation(10, 1, 20);
            var expected = string.Join(Environment.NewLine,
                                       @"#line 2 ""myfile""",
                                       "Hello world",
                                       "",
                                       "#line default",
                                       "#line hidden",
                                       "");
            var expectedMappings = new LineMapping(
                                new MappingLocation(location, 30),
                                new MappingLocation(new SourceLocation(18, 1, 0), 11));
            var writer = new CSharpCodeWriter();

            // Act
            using (var mappingWriter = new CSharpLineMappingWriter(writer, location, "myfile"))
            {
                writer.Write("Hello world");
            }

            // Assert
            Assert.Equal(expected, writer.GenerateCode());
            Assert.Empty(writer.LineMappingManager.Mappings);
        }
    }
}