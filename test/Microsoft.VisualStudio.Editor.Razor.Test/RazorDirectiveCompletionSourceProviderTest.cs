// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public class RazorDirectiveCompletionSourceProviderTest : ForegroundDispatcherTestBase
    {
        private IContentType RazorContentType { get; } = Mock.Of<IContentType>(c => c.IsOfType(RazorLanguage.ContentType) == true);

        private IContentType NonRazorContentType { get; } = Mock.Of<IContentType>(c => c.IsOfType(It.IsAny<string>()) == false);

        [Fact]
        public void CreateCompletionSource_ReturnsNullIfParserHasNotBeenAssocitedWithRazorBuffer()
        {
            // Arrange
            var expectedParser = Mock.Of<VisualStudioRazorParser>();
            var properties = new PropertyCollection();
            properties.AddProperty(typeof(VisualStudioRazorParser), expectedParser);
            var razorBuffer = Mock.Of<ITextBuffer>(buffer => buffer.ContentType == RazorContentType && buffer.Properties == properties);
            var completionSourceProvider = new RazorDirectiveCompletionSourceProvider(Dispatcher);

            // Act
            var completionSource = completionSourceProvider.CreateCompletionSource(razorBuffer);

            // Assert
            var completionSourceImpl = Assert.IsType<RazorDirectiveCompletionSource>(completionSource);
            Assert.Same(expectedParser, completionSourceImpl._parser);
        }

        [Fact]
        public void CreateCompletionSource_CreatesACompletionSourceWithTextBuffersParser()
        {
            // Arrange
            var razorBuffer = Mock.Of<ITextBuffer>(buffer => buffer.ContentType == RazorContentType && buffer.Properties == new PropertyCollection());
            var completionSourceProvider = new RazorDirectiveCompletionSourceProvider(Dispatcher);

            // Act
            var completionSource = completionSourceProvider.CreateCompletionSource(razorBuffer);

            // Assert
            Assert.Null(completionSource);
        }

        [Fact]
        public void GetOrCreate_ReturnsNullIfRazorBufferHasNotBeenAssociatedWithTextView()
        {
            // Arrange
            var textView = CreateTextView(NonRazorContentType, new PropertyCollection());
            var completionSourceProvider = new RazorDirectiveCompletionSourceProvider(Dispatcher);

            // Act
            var completionSource = completionSourceProvider.GetOrCreate(textView);

            // Assert
            Assert.Null(completionSource);
        }

        [Fact]
        public void GetOrCreate_CachesCompletionSource()
        {
            // Arrange
            var expectedParser = Mock.Of<VisualStudioRazorParser>();
            var properties = new PropertyCollection();
            properties.AddProperty(typeof(VisualStudioRazorParser), expectedParser);
            var textView = CreateTextView(RazorContentType, properties);
            var completionSourceProvider = new RazorDirectiveCompletionSourceProvider(Dispatcher);

            // Act
            var completionSource1 = completionSourceProvider.GetOrCreate(textView);
            var completionSource2 = completionSourceProvider.GetOrCreate(textView);

            // Assert
            Assert.Same(completionSource1, completionSource2);
        }

        private static ITextView CreateTextView(IContentType contentType, PropertyCollection properties)
        {
            var bufferGraph = new Mock<IBufferGraph>();
            bufferGraph.Setup(graph => graph.GetTextBuffers(It.IsAny<Predicate<ITextBuffer>>()))
                .Returns(new Collection<ITextBuffer>()
                {
                    Mock.Of<ITextBuffer>(buffer => buffer.ContentType == contentType && buffer.Properties == properties)
                });
            var textView = Mock.Of<ITextView>(view => view.BufferGraph == bufferGraph.Object);

            return textView;
        }
    }
}
