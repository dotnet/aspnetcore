// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
    }
}
