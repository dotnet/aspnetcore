// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public class DefaultTextBufferCodeDocumentProviderTest
    {
        [Fact]
        public void TryGetFromBuffer_UsesVisualStudioRazorParserIfAvailable()
        {
            // Arrange
            var expectedCodeDocument = TestRazorCodeDocument.Create("Hello World");
            var parser = new VisualStudioRazorParser(expectedCodeDocument);
            var properties = new PropertyCollection();
            properties.AddProperty(typeof(VisualStudioRazorParser), parser);
            var textBuffer = new Mock<ITextBuffer>();
            textBuffer.Setup(buffer => buffer.Properties)
                .Returns(properties);
            var provider = new DefaultTextBufferCodeDocumentProvider();

            // Act
            var result = provider.TryGetFromBuffer(textBuffer.Object, out var codeDocument);

            // Assert
            Assert.True(result);
            Assert.Same(expectedCodeDocument, codeDocument);
        }

        [Fact]
        public void TryGetFromBuffer_FailsIfNoParserIsAvailable()
        {
            // Arrange
            var properties = new PropertyCollection();
            var textBuffer = new Mock<ITextBuffer>();
            textBuffer.Setup(buffer => buffer.Properties)
                .Returns(properties);
            var provider = new DefaultTextBufferCodeDocumentProvider();

            // Act
            var result = provider.TryGetFromBuffer(textBuffer.Object, out var codeDocument);

            // Assert
            Assert.False(result);
            Assert.Null(codeDocument);
        }
    }
}
