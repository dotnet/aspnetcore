// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Editor
{
    public class DefaultCodeDocumentProviderTest
    {
        [Fact]
        public void TryGetFromDocument_ReturnsFalseIfBufferProviderCanNotGetAssociatedBuffer()
        {
            // Arrange
            ITextBuffer textBuffer;
            RazorCodeDocument codeDocument;
            var bufferProvider = new Mock<RazorTextBufferProvider>();
            bufferProvider.Setup(provider => provider.TryGetFromDocument(It.IsAny<TextDocument>(), out textBuffer))
                .Returns(false);
            var vsCodeDocumentProvider = new Mock<VisualStudioCodeDocumentProvider>();
            vsCodeDocumentProvider.Setup(provider => provider.TryGetFromBuffer(It.IsAny<ITextBuffer>(), out codeDocument))
                .Returns(true);
            var codeDocumentProvider = new DefaultCodeDocumentProvider(bufferProvider.Object, vsCodeDocumentProvider.Object);
            var document = new Mock<TextDocument>();

            // Act
            var result = codeDocumentProvider.TryGetFromDocument(document.Object, out codeDocument);

            // Assert
            Assert.False(result);
            Assert.Null(codeDocument);
        }

        [Fact]
        public void TryGetFromDocument_ReturnsFalseIfVSProviderCanNotGetCodeDocument()
        {
            // Arrange
            var textBuffer = new Mock<ITextBuffer>().Object;
            RazorCodeDocument codeDocument;
            var bufferProvider = new Mock<RazorTextBufferProvider>();
            bufferProvider.Setup(provider => provider.TryGetFromDocument(It.IsAny<TextDocument>(), out textBuffer))
                .Returns(true);
            var vsCodeDocumentProvider = new Mock<VisualStudioCodeDocumentProvider>();
            vsCodeDocumentProvider.Setup(provider => provider.TryGetFromBuffer(It.Is<ITextBuffer>(val => val == textBuffer), out codeDocument))
                .Returns(false);
            var codeDocumentProvider = new DefaultCodeDocumentProvider(bufferProvider.Object, vsCodeDocumentProvider.Object);
            var document = new Mock<TextDocument>();

            // Act
            var result = codeDocumentProvider.TryGetFromDocument(document.Object, out codeDocument);

            // Assert
            Assert.False(result);
            Assert.Null(codeDocument);
        }

        [Fact]
        public void TryGetFromDocument_ReturnsTrueIfBothBufferAndVSProviderReturnTrue()
        {
            // Arrange
            var textBuffer = new Mock<ITextBuffer>().Object;
            var expectedCodeDocument = new Mock<RazorCodeDocument>().Object;
            var bufferProvider = new Mock<RazorTextBufferProvider>();
            bufferProvider.Setup(provider => provider.TryGetFromDocument(It.IsAny<TextDocument>(), out textBuffer))
                .Returns(true);
            var vsCodeDocumentProvider = new Mock<VisualStudioCodeDocumentProvider>();
            vsCodeDocumentProvider.Setup(provider => provider.TryGetFromBuffer(It.Is<ITextBuffer>(val => val == textBuffer), out expectedCodeDocument))
                .Returns(true);
            var codeDocumentProvider = new DefaultCodeDocumentProvider(bufferProvider.Object, vsCodeDocumentProvider.Object);
            var document = new Mock<TextDocument>();

            // Act
            var result = codeDocumentProvider.TryGetFromDocument(document.Object, out var codeDocument);

            // Assert
            Assert.True(result);
            Assert.Same(expectedCodeDocument, codeDocument);
        }
    }
}
