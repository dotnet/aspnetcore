// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class DefaultRazorProjectEngineTest
    {
        [Fact]
        public void ConvertToSourceDocument_ConvertsNormalImports()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("Index.cshtml");

            // Act
            var sourceDocument = DefaultRazorProjectEngine.ConvertToSourceDocument(projectItem);

            // Assert
            Assert.NotNull(sourceDocument);
        }

        [Fact]
        public void ConvertToSourceDocument_ConvertsMarkerImports()
        {
            // Arrange
            var projectItem = Mock.Of<RazorProjectItem>(item => item.FilePath == "Index.cshtml" && item.Exists == false);

            // Act
            var sourceDocument = DefaultRazorProjectEngine.ConvertToSourceDocument(projectItem);

            // Assert
            Assert.NotNull(sourceDocument);
        }

        [Fact]
        public void ConvertToSourceDocument_UnreadableItem_Throws()
        {
            // Arrange
            var projectItem = new Mock<RazorProjectItem>(MockBehavior.Strict);
            projectItem.SetupGet(p => p.Exists).Returns(true);
            projectItem.SetupGet(p => p.PhysicalPath).Returns("path/to/file.cshtml");
            projectItem.Setup(p => p.Read()).Throws(new IOException("Couldn't read file."));

            // Act & Assert
            var exception = Assert.Throws<IOException>(() => DefaultRazorProjectEngine.ConvertToSourceDocument(projectItem.Object));
            Assert.Equal("Couldn't read file.", exception.Message);
        }

        [Fact]
        public void ConvertToSourceDocument_WithSuppressExceptions_UnreadableItem_DoesNotThrow()
        {
            // Arrange
            var projectItem = new Mock<RazorProjectItem>(MockBehavior.Strict);
            projectItem.SetupGet(p => p.Exists).Returns(true);
            projectItem.SetupGet(p => p.PhysicalPath).Returns("path/to/file.cshtml");
            projectItem.SetupGet(p => p.FilePath).Returns("path/to/file.cshtml");
            projectItem.SetupGet(p => p.RelativePhysicalPath).Returns("path/to/file.cshtml");
            projectItem.Setup(p => p.Read()).Throws(new IOException("Couldn't read file."));

            // Act
            var sourceDocument = DefaultRazorProjectEngine.ConvertToSourceDocument(projectItem.Object, suppressExceptions: true);

            // Assert - Does not throw
            Assert.NotNull(sourceDocument);
            Assert.Equal(0, sourceDocument.Length);
        }
    }
}
