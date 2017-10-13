// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.VisualStudio.Text;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public class DefaultTextBufferCodeDocumentProviderTest
    {
        [Fact]
        public void TryGetFromBuffer_SucceedsIfParserFromProviderHasCodeDocument()
        {
            // Arrange
            var expectedCodeDocument = TestRazorCodeDocument.Create("Hello World");
            VisualStudioRazorParser parser = new DefaultVisualStudioRazorParser(expectedCodeDocument);
            var parserProvider = Mock.Of<RazorEditorFactoryService>(p => p.TryGetParser(It.IsAny<ITextBuffer>(), out parser) == true);
            var textBuffer = Mock.Of<ITextBuffer>();
            var provider = new DefaultTextBufferCodeDocumentProvider(parserProvider);

            // Act
            var result = provider.TryGetFromBuffer(textBuffer, out var codeDocument);

            // Assert
            Assert.True(result);
            Assert.Same(expectedCodeDocument, codeDocument);
        }

        [Fact]
        public void TryGetFromBuffer_FailsIfParserFromProviderMissingCodeDocument()
        {
            // Arrange
            VisualStudioRazorParser parser = new DefaultVisualStudioRazorParser(codeDocument: null);
            var parserProvider = Mock.Of<RazorEditorFactoryService>(p => p.TryGetParser(It.IsAny<ITextBuffer>(), out parser) == true);
            var textBuffer = Mock.Of<ITextBuffer>();
            var provider = new DefaultTextBufferCodeDocumentProvider(parserProvider);

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
            VisualStudioRazorParser parser = null;
            var parserProvider = Mock.Of<RazorEditorFactoryService>(p => p.TryGetParser(It.IsAny<ITextBuffer>(), out parser) == false);
            var textBuffer = Mock.Of<ITextBuffer>();
            var provider = new DefaultTextBufferCodeDocumentProvider(parserProvider);

            // Act
            var result = provider.TryGetFromBuffer(textBuffer, out var codeDocument);

            // Assert
            Assert.False(result);
            Assert.Null(codeDocument);
        }
    }
}
