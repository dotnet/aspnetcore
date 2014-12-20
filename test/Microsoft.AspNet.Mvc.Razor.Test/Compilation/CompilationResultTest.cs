// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using Microsoft.AspNet.FileSystems;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor.Test
{
    public class CompilationResultTest
    {
        [Fact]
        public void FailedResult_ThrowsWhenAccessingCompiledType()
        {
            // Arrange
            var expected = @"Error compiling page at 'myfile'.";
            var originalContent = "Original file content";
            var fileInfo = new Mock<IFileInfo>();
            fileInfo.SetupGet(f => f.PhysicalPath)
                    .Returns("myfile");
            var contentBytes = Encoding.UTF8.GetBytes(originalContent);
            fileInfo.Setup(f => f.CreateReadStream())
                    .Returns(new MemoryStream(contentBytes));
            var messages = new[]
            {
                new CompilationMessage("hello", 1, 1, 2, 2),
                new CompilationMessage("world", 3, 3, 4, 3)
            };
            var result = CompilationResult.Failed(fileInfo.Object,
                                                 "<h1>hello world</h1>",
                                                 messages);

            // Act and Assert
            var ex = Assert.Throws<CompilationFailedException>(() => result.CompiledType);
            Assert.Equal(expected, ex.Message);
            var compilationFailure = Assert.Single(ex.CompilationFailures);
            Assert.Equal(originalContent, compilationFailure.SourceFileContent);
            Assert.Equal(messages, compilationFailure.Messages);
        }
    }
}