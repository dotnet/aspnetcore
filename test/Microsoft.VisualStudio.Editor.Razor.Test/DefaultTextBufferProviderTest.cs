// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public class DefaultTextBufferProviderTest
    {
        [Fact]
        public void TryGetFromDocument_ReturnsFalseIfCannotExtractSourceText()
        {
            // Arrange
            var textBuffer = CreateTextBuffer();
            var bufferGraphService = CreateBufferGraphService(textBuffer);
            var document = CreateDocumentWithoutText();
            var bufferProvider = new DefaultTextBufferProvider(bufferGraphService);

            // Act
            var result = bufferProvider.TryGetFromDocument(document, out var buffer);

            // Assert
            Assert.False(result);
            Assert.Null(buffer);
        }

        [Fact]
        public void TryGetFromDocument_ReturnsFalseIfSourceContainerNotConstructedFromTextBuffer()
        {
            // Arrange
            var bufferGraphService = CreateBufferGraphService(null);
            var text = SourceText.From("Hello World");
            var document = CreateDocumentWithoutText();
            document = document.WithText(text);
            var bufferProvider = new DefaultTextBufferProvider(bufferGraphService);

            // Act
            var result = bufferProvider.TryGetFromDocument(document, out var buffer);

            // Assert
            Assert.False(result);
            Assert.Null(buffer);
        }

        [Fact]
        public void TryGetFromDocument_ReturnsFalseIfBufferGraphCanNotFindRazorBuffer()
        {
            // Arrange
            var textBuffer = CreateTextBuffer();
            var bufferGraph = new Mock<IBufferGraph>();
            bufferGraph.Setup(graph => graph.GetTextBuffers(It.IsAny<Predicate<ITextBuffer>>()))
                .Returns(new Collection<ITextBuffer>());
            var bufferGraphService = new Mock<IBufferGraphFactoryService>();
            bufferGraphService.Setup(service => service.CreateBufferGraph(textBuffer))
                .Returns(bufferGraph.Object);
            var document = CreateDocument(textBuffer);
            var bufferProvider = new DefaultTextBufferProvider(bufferGraphService.Object);

            // Act
            var result = bufferProvider.TryGetFromDocument(document, out var buffer);

            // Assert
            Assert.False(result);
            Assert.Null(buffer);
        }

        [Fact]
        public void TryGetFromDocument_ReturnsTrueForValidDocuments()
        {
            // Arrange
            var textBuffer = CreateTextBuffer();
            var bufferGraphService = CreateBufferGraphService(textBuffer);
            var document = CreateDocument(textBuffer);
            var bufferProvider = new DefaultTextBufferProvider(bufferGraphService);

            // Act
            var result = bufferProvider.TryGetFromDocument(document, out var buffer);

            // Assert
            Assert.True(result);
            Assert.Same(textBuffer, buffer);
        }

        private static Document CreateDocumentWithoutText()
        {
            var project = ProjectInfo
                .Create(ProjectId.CreateNewId(), VersionStamp.Default, "TestProject", "TestAssembly", LanguageNames.CSharp)
                .WithFilePath("/TestProject.csproj");
            var workspace = new AdhocWorkspace();
            workspace.AddProject(project);
            var documentInfo = DocumentInfo.Create(DocumentId.CreateNewId(project.Id), "Test.cshtml");
            var document = workspace.AddDocument(documentInfo);

            return document;
        }

        private static Document CreateDocument(ITextBuffer buffer)
        {
            var document = CreateDocumentWithoutText();
            var container = buffer.AsTextContainer();
            document = document.WithText(container.CurrentText);
            return document;
        }

        private static ITextBuffer CreateTextBuffer()
        {
            var textBuffer = new Mock<ITextBuffer>();
            textBuffer.Setup(buffer => buffer.Properties)
                .Returns(new PropertyCollection());

            var textImage = new Mock<ITextImage>();
            var textVersion = new Mock<ITextVersion>();
            var textBufferSnapshot = new Mock<ITextSnapshot2>();
            textBufferSnapshot.Setup(snapshot => snapshot.TextImage)
                .Returns(textImage.Object);
            textBufferSnapshot.Setup(snapshot => snapshot.Length)
                .Returns(0);
            textBufferSnapshot.Setup(snapshot => snapshot.Version)
                .Returns(textVersion.Object);
            textBufferSnapshot.Setup(snapshot => snapshot.TextBuffer)
                .Returns(() => textBuffer.Object);

            textBuffer.Setup(buffer => buffer.CurrentSnapshot)
                .Returns(() => textBufferSnapshot.Object);

            var contentType = new Mock<IContentType>();
            contentType.Setup(type => type.IsOfType(It.IsAny<string>()))
                .Returns<string>(val => val == RazorLanguage.ContentType);
            textBuffer.Setup(buffer => buffer.ContentType)
                .Returns(contentType.Object);

            return textBuffer.Object;
        }

        private static IBufferGraphFactoryService CreateBufferGraphService(ITextBuffer buffer)
        {
            var bufferGraph = new Mock<IBufferGraph>();
            bufferGraph.Setup(graph => graph.GetTextBuffers(It.IsAny<Predicate<ITextBuffer>>()))
                .Returns<Predicate<ITextBuffer>>(predicate => predicate(buffer) ? new Collection<ITextBuffer>() { buffer } : new Collection<ITextBuffer>());
            var bufferGraphService = new Mock<IBufferGraphFactoryService>();
            bufferGraphService.Setup(service => service.CreateBufferGraph(buffer))
                .Returns(bufferGraph.Object);

            return bufferGraphService.Object;
        }
    }
}
