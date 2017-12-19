// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public class DefaultRazorEditorFactoryServiceTest
    {
        private IContentType RazorCoreContentType { get; } = Mock.Of<IContentType>(c => c.IsOfType(RazorLanguage.CoreContentType) == true);

        private IContentType NonRazorCoreContentType { get; } = Mock.Of<IContentType>(c => c.IsOfType(It.IsAny<string>()) == false);

        [Fact]
        public void TryGetDocumentTracker_ForRazorTextBuffer_ReturnsTrue()
        {
            // Arrange
            var expectedDocumentTracker = Mock.Of<VisualStudioDocumentTracker>();
            var factoryService = CreateFactoryService(expectedDocumentTracker);
            var textBuffer = Mock.Of<ITextBuffer>(b => b.ContentType == RazorCoreContentType && b.Properties == new PropertyCollection());

            // Act
            var result = factoryService.TryGetDocumentTracker(textBuffer, out var documentTracker);

            // Assert
            Assert.True(result);
            Assert.Same(expectedDocumentTracker, documentTracker);
        }

        [Fact]
        public void TryGetDocumentTracker_NonRazorBuffer_ReturnsFalse()
        {
            // Arrange
            var factoryService = CreateFactoryService();
            var textBuffer = Mock.Of<ITextBuffer>(b => b.ContentType == NonRazorCoreContentType && b.Properties == new PropertyCollection());

            // Act
            var result = factoryService.TryGetDocumentTracker(textBuffer, out var documentTracker);

            // Assert
            Assert.False(result);
            Assert.Null(documentTracker);
        }

        [Fact]
        public void EnsureTextBufferInitialized_StoresTracker()
        {
            // Arrange
            var expectedDocumentTracker = Mock.Of<VisualStudioDocumentTracker>();
            var factoryService = CreateFactoryService(expectedDocumentTracker);
            var textBuffer = Mock.Of<ITextBuffer>(b => b.ContentType == RazorCoreContentType && b.Properties == new PropertyCollection());

            // Act
            factoryService.EnsureTextBufferInitialized(textBuffer);

            // Assert
            Assert.True(textBuffer.Properties.TryGetProperty(typeof(VisualStudioDocumentTracker), out VisualStudioDocumentTracker documentTracker));
            Assert.Same(expectedDocumentTracker, documentTracker);
        }

        [Fact]
        public void EnsureTextBufferInitialized_OnlyStoresTrackerOnTextBufferOnce()
        {
            // Arrange
            var factoryService = CreateFactoryService();
            var textBuffer = Mock.Of<ITextBuffer>(b => b.ContentType == RazorCoreContentType && b.Properties == new PropertyCollection());
            factoryService.EnsureTextBufferInitialized(textBuffer);
            var expectedDocumentTracker = textBuffer.Properties[typeof(VisualStudioDocumentTracker)];

            // Create a second factory service so it generates a different tracker
            factoryService = CreateFactoryService();

            // Act
            factoryService.EnsureTextBufferInitialized(textBuffer);

            // Assert
            Assert.True(textBuffer.Properties.TryGetProperty(typeof(VisualStudioDocumentTracker), out VisualStudioDocumentTracker documentTracker));
            Assert.Same(expectedDocumentTracker, documentTracker);
        }

        [Fact]
        public void TryGetParser_ForRazorTextBuffer_ReturnsTrue()
        {
            // Arrange
            var expectedParser = Mock.Of<VisualStudioRazorParser>();
            var factoryService = CreateFactoryService(parser: expectedParser);
            var textBuffer = Mock.Of<ITextBuffer>(b => b.ContentType == RazorCoreContentType && b.Properties == new PropertyCollection());

            // Act
            var result = factoryService.TryGetParser(textBuffer, out var parser);

            // Assert
            Assert.True(result);
            Assert.Same(expectedParser, parser);
        }

        [Fact]
        public void TryGetParser_NonRazorBuffer_ReturnsFalse()
        {
            // Arrange
            var factoryService = CreateFactoryService();
            var textBuffer = Mock.Of<ITextBuffer>(b => b.ContentType == NonRazorCoreContentType && b.Properties == new PropertyCollection());

            // Act
            var result = factoryService.TryGetParser(textBuffer, out var parser);

            // Assert
            Assert.False(result);
            Assert.Null(parser);
        }

        [Fact]
        public void EnsureTextBufferInitialized_StoresParser()
        {
            // Arrange
            var expectedParser = Mock.Of<VisualStudioRazorParser>();
            var factoryService = CreateFactoryService(parser: expectedParser);
            var textBuffer = Mock.Of<ITextBuffer>(b => b.ContentType == RazorCoreContentType && b.Properties == new PropertyCollection());

            // Act
            factoryService.EnsureTextBufferInitialized(textBuffer);

            // Assert
            Assert.True(textBuffer.Properties.TryGetProperty(typeof(VisualStudioRazorParser), out VisualStudioRazorParser parser));
            Assert.Same(expectedParser, parser);
        }

        [Fact]
        public void EnsureTextBufferInitialized_OnlyStoresParserOnTextBufferOnce()
        {
            // Arrange
            var factoryService = CreateFactoryService();
            var textBuffer = Mock.Of<ITextBuffer>(b => b.ContentType == RazorCoreContentType && b.Properties == new PropertyCollection());
            factoryService.EnsureTextBufferInitialized(textBuffer);
            var expectedParser = textBuffer.Properties[typeof(VisualStudioRazorParser)];

            // Create a second factory service so it generates a different parser
            factoryService = CreateFactoryService();

            // Act
            factoryService.EnsureTextBufferInitialized(textBuffer);

            // Assert
            Assert.True(textBuffer.Properties.TryGetProperty(typeof(VisualStudioRazorParser), out VisualStudioRazorParser parser));
            Assert.Same(expectedParser, parser);
        }

        [Fact]
        public void TryGetSmartIndenter_ForRazorTextBuffer_ReturnsTrue()
        {
            // Arrange
            var expectedSmartIndenter = Mock.Of<BraceSmartIndenter>();
            var factoryService = CreateFactoryService(smartIndenter: expectedSmartIndenter);
            var textBuffer = Mock.Of<ITextBuffer>(b => b.ContentType == RazorCoreContentType && b.Properties == new PropertyCollection());

            // Act
            var result = factoryService.TryGetSmartIndenter(textBuffer, out var smartIndenter);

            // Assert
            Assert.True(result);
            Assert.Same(expectedSmartIndenter, smartIndenter);
        }

        [Fact]
        public void TryGetSmartIndenter_NonRazorBuffer_ReturnsFalse()
        {
            // Arrange
            var factoryService = CreateFactoryService();
            var textBuffer = Mock.Of<ITextBuffer>(b => b.ContentType == NonRazorCoreContentType && b.Properties == new PropertyCollection());

            // Act
            var result = factoryService.TryGetSmartIndenter(textBuffer, out var smartIndenter);

            // Assert
            Assert.False(result);
            Assert.Null(smartIndenter);
        }

        [Fact]
        public void EnsureTextBufferInitialized_StoresSmartIndenter()
        {
            // Arrange
            var expectedSmartIndenter = Mock.Of<BraceSmartIndenter>();
            var factoryService = CreateFactoryService(smartIndenter: expectedSmartIndenter);
            var textBuffer = Mock.Of<ITextBuffer>(b => b.ContentType == RazorCoreContentType && b.Properties == new PropertyCollection());

            // Act
            factoryService.EnsureTextBufferInitialized(textBuffer);

            // Assert
            Assert.True(textBuffer.Properties.TryGetProperty(typeof(BraceSmartIndenter), out BraceSmartIndenter smartIndenter));
            Assert.Same(expectedSmartIndenter, smartIndenter);
        }

        [Fact]
        public void EnsureTextBufferInitialized_OnlyStoresSmartIndenterOnTextBufferOnce()
        {
            // Arrange
            var factoryService = CreateFactoryService();
            var textBuffer = Mock.Of<ITextBuffer>(b => b.ContentType == RazorCoreContentType && b.Properties == new PropertyCollection());
            factoryService.EnsureTextBufferInitialized(textBuffer);
            var expectedSmartIndenter = textBuffer.Properties[typeof(BraceSmartIndenter)];

            // Create a second factory service so it generates a different smart indenter
            factoryService = CreateFactoryService();

            // Act
            factoryService.EnsureTextBufferInitialized(textBuffer);

            // Assert
            Assert.True(textBuffer.Properties.TryGetProperty(typeof(BraceSmartIndenter), out BraceSmartIndenter smartIndenter));
            Assert.Same(expectedSmartIndenter, smartIndenter);
        }

        private static DefaultRazorEditorFactoryService CreateFactoryService(
            VisualStudioDocumentTracker documentTracker = null,
            VisualStudioRazorParser parser = null,
            BraceSmartIndenter smartIndenter = null)
        {
            documentTracker = documentTracker ?? Mock.Of<VisualStudioDocumentTracker>();
            parser = parser ?? Mock.Of<VisualStudioRazorParser>();
            smartIndenter = smartIndenter ?? Mock.Of<BraceSmartIndenter>();

            var documentTrackerFactory = Mock.Of<VisualStudioDocumentTrackerFactory>(f => f.Create(It.IsAny<ITextBuffer>()) == documentTracker);
            var parserFactory = Mock.Of<VisualStudioRazorParserFactory>(f => f.Create(It.IsAny<VisualStudioDocumentTracker>()) == parser);
            var smartIndenterFactory = Mock.Of<BraceSmartIndenterFactory>(f => f.Create(It.IsAny<VisualStudioDocumentTracker>()) == smartIndenter);
            var factoryService = new DefaultRazorEditorFactoryService(documentTrackerFactory, parserFactory, smartIndenterFactory);

            return factoryService;
        }
    }
}
