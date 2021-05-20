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
        public void GetImportSourceDocuments_DoesNotIncludeNonExistentItems()
        {
            // Arrange
            var existingItem = new TestRazorProjectItem("Index.cshtml");
            var nonExistentItem = Mock.Of<RazorProjectItem>(item => item.Exists == false);
            var items = new[] { existingItem, nonExistentItem };

            // Act
            var sourceDocuments = DefaultRazorProjectEngine.GetImportSourceDocuments(items);

            // Assert
            var sourceDocument = Assert.Single(sourceDocuments);
            Assert.Equal(existingItem.FilePath, sourceDocument.FilePath);
        }

        [Fact]
        public void GetImportSourceDocuments_UnreadableItem_Throws()
        {
            // Arrange
            var projectItem = new Mock<RazorProjectItem>(MockBehavior.Strict);
            projectItem.SetupGet(p => p.Exists).Returns(true);
            projectItem.SetupGet(p => p.PhysicalPath).Returns("path/to/file.cshtml");
            projectItem.Setup(p => p.Read()).Throws(new IOException("Couldn't read file."));
            var items = new[] { projectItem.Object };

            // Act & Assert
            var exception = Assert.Throws<IOException>(() => DefaultRazorProjectEngine.GetImportSourceDocuments(items));
            Assert.Equal("Couldn't read file.", exception.Message);
        }

        [Fact]
        public void GetImportSourceDocuments_WithSuppressExceptions_UnreadableItem_DoesNotThrow()
        {
            // Arrange
            var projectItem = new Mock<RazorProjectItem>(MockBehavior.Strict);
            projectItem.SetupGet(p => p.Exists).Returns(true);
            projectItem.SetupGet(p => p.PhysicalPath).Returns("path/to/file.cshtml");
            projectItem.SetupGet(p => p.FilePath).Returns("path/to/file.cshtml");
            projectItem.SetupGet(p => p.RelativePhysicalPath).Returns("path/to/file.cshtml");
            projectItem.Setup(p => p.Read()).Throws(new IOException("Couldn't read file."));
            var items = new[] { projectItem.Object };

            // Act
            var sourceDocuments = DefaultRazorProjectEngine.GetImportSourceDocuments(items, suppressExceptions: true);

            // Assert - Does not throw
            Assert.Empty(sourceDocuments);
        }
    }
}
