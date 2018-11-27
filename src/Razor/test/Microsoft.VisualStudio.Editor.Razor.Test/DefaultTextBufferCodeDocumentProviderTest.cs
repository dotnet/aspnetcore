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
        public void TryGetFromBuffer_SucceedsIfParserHasCodeDocument()
        {
            // Arrange
            var expectedCodeDocument = TestRazorCodeDocument.Create("Hello World");
            VisualStudioRazorParser parser = new DefaultVisualStudioRazorParser(expectedCodeDocument);
            var properties = new PropertyCollection()
            {
                [typeof(VisualStudioRazorParser)] = parser
            };
            var textBuffer = Mock.Of<ITextBuffer>(buffer => buffer.Properties == properties);
            var provider = new DefaultTextBufferCodeDocumentProvider();

            // Act
            var result = provider.TryGetFromBuffer(textBuffer, out var codeDocument);

            // Assert
            Assert.True(result);
            Assert.Same(expectedCodeDocument, codeDocument);
        }

        [Fact]
        public void TryGetFromBuffer_FailsIfParserMissingCodeDocument()
        {
            // Arrange
            VisualStudioRazorParser parser = new DefaultVisualStudioRazorParser(codeDocument: null);
            var properties = new PropertyCollection()
            {
                [typeof(VisualStudioRazorParser)] = parser
            };
            var textBuffer = Mock.Of<ITextBuffer>(buffer => buffer.Properties == properties);
            var provider = new DefaultTextBufferCodeDocumentProvider();

            // Act
            var result = provider.TryGetFromBuffer(textBuffer, out var codeDocument);

            // Assert
            Assert.False(result);
            Assert.Null(codeDocument);
        }

        [Fact]
        public void TryGetFromBuffer_FailsIfNoParserIsAvailable()
        {
            // Arrange
            var textBuffer = Mock.Of<ITextBuffer>(buffer => buffer.Properties == new PropertyCollection());
            var provider = new DefaultTextBufferCodeDocumentProvider();

            // Act
            var result = provider.TryGetFromBuffer(textBuffer, out var codeDocument);

            // Assert
            Assert.False(result);
            Assert.Null(codeDocument);
        }
    }
}
