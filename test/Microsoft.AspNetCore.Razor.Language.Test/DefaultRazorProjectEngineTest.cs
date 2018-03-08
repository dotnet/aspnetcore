// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
    }
}
