// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Xunit;
using ITextBuffer = Microsoft.VisualStudio.Text.ITextBuffer;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public class RazorDirectiveCompletionProviderTest
    {
        public RazorDirectiveCompletionProviderTest()
        {
            CompletionBroker = Mock.Of<IAsyncCompletionBroker>(broker => broker.IsCompletionSupported(It.IsAny<IContentType>()) == true);
            var razorBuffer = Mock.Of<ITextBuffer>(buffer => buffer.ContentType == Mock.Of<IContentType>());
            TextBufferProvider = Mock.Of<RazorTextBufferProvider>(provider => provider.TryGetFromDocument(It.IsAny<TextDocument>(), out razorBuffer) == true);
            CompletionFactsService = new DefaultRazorCompletionFactsService();
            CompletionProviderDependencies = new Lazy<CompletionProviderDependencies>(() => new DefaultCompletionProviderDependencies(CompletionFactsService, CompletionBroker));
        }

        private Lazy<CompletionProviderDependencies> CompletionProviderDependencies { get; }

        private IAsyncCompletionBroker CompletionBroker { get; }

        private RazorTextBufferProvider TextBufferProvider { get; }

        private RazorCompletionFactsService CompletionFactsService { get; }

        [Fact]
        public async Task GetDescriptionAsync_AddsDirectiveDescriptionIfPropertyExists()
        {
            // Arrange
            var document = CreateDocument();
            var expectedDescription = "The expected description";
            var item = CompletionItem.Create("TestDirective")
                .WithProperties((new Dictionary<string, string>()
                {
                    [RazorDirectiveCompletionProvider.DescriptionKey] = expectedDescription,
                }).ToImmutableDictionary());
            var codeDocumentProvider = new Mock<RazorCodeDocumentProvider>();
            var completionProvider = new RazorDirectiveCompletionProvider(
                new Lazy<RazorCodeDocumentProvider>(() => codeDocumentProvider.Object),
                CompletionProviderDependencies,
                TextBufferProvider);

            // Act
            var description = await completionProvider.GetDescriptionAsync(document, item, CancellationToken.None);

            // Assert
            var part = Assert.Single(description.TaggedParts);
            Assert.Equal(TextTags.Text, part.Tag);
            Assert.Equal(expectedDescription, part.Text);
            Assert.Equal(expectedDescription, description.Text);
        }

        [Fact]
        public async Task GetDescriptionAsync_DoesNotAddDescriptionWhenPropertyAbsent()
        {
            // Arrange
            var document = CreateDocument();
            var item = CompletionItem.Create("TestDirective");
            var codeDocumentProvider = new Mock<RazorCodeDocumentProvider>();
            var completionProvider = new RazorDirectiveCompletionProvider(
                new Lazy<RazorCodeDocumentProvider>(() => codeDocumentProvider.Object),
                CompletionProviderDependencies,
                TextBufferProvider);

            // Act
            var description = await completionProvider.GetDescriptionAsync(document, item, CancellationToken.None);

            // Assert
            Assert.Empty(description.TaggedParts);
            Assert.Equal(string.Empty, description.Text);
        }

        [Fact]
        public async Task ProvideCompletionAsync_DoesNotProvideCompletionsForNonRazorFiles()
        {
            // Arrange
            var codeDocumentProvider = new Mock<RazorCodeDocumentProvider>(MockBehavior.Strict);
            var completionProvider = new FailOnGetCompletionsProvider(
                new Lazy<RazorCodeDocumentProvider>(() => codeDocumentProvider.Object),
                CompletionProviderDependencies,
                TextBufferProvider);
            var document = CreateDocument();
            document = document.WithFilePath("NotRazor.cs");
            var context = CreateContext(1, completionProvider, document);

            // Act & Assert
            await completionProvider.ProvideCompletionsAsync(context);
        }

        [Fact]
        public async Task ProvideCompletionAsync_DoesNotProvideCompletionsForDocumentWithoutPath()
        {
            // Arrange
            Document document = null;
            TestWorkspace.Create(workspace =>
            {
                var project = ProjectInfo
                .Create(ProjectId.CreateNewId(), VersionStamp.Default, "TestProject", "TestAssembly", LanguageNames.CSharp)
                .WithFilePath("/TestProject.csproj");
                workspace.AddProject(project);
                var documentInfo = DocumentInfo.Create(DocumentId.CreateNewId(project.Id), "Test.cshtml");
                document = workspace.AddDocument(documentInfo);
            });

            var codeDocumentProvider = new Mock<RazorCodeDocumentProvider>(MockBehavior.Strict);
            var completionProvider = new FailOnGetCompletionsProvider(
                new Lazy<RazorCodeDocumentProvider>(() => codeDocumentProvider.Object),
                CompletionProviderDependencies,
                TextBufferProvider);
            var context = CreateContext(1, completionProvider, document);

            // Act & Assert
            await completionProvider.ProvideCompletionsAsync(context);
        }

        [Fact]
        public async Task ProvideCompletionAsync_DoesNotProvideCompletionsWhenDocumentProviderCanNotGetDocument()
        {
            // Arrange
            RazorCodeDocument codeDocument;
            var codeDocumentProvider = new Mock<RazorCodeDocumentProvider>();
            codeDocumentProvider.Setup(provider => provider.TryGetFromDocument(It.IsAny<TextDocument>(), out codeDocument))
                .Returns(false);
            var completionProvider = new FailOnGetCompletionsProvider(
                new Lazy<RazorCodeDocumentProvider>(() => codeDocumentProvider.Object),
                CompletionProviderDependencies,
                TextBufferProvider);
            var document = CreateDocument();
            var context = CreateContext(1, completionProvider, document);

            // Act & Assert
            await completionProvider.ProvideCompletionsAsync(context);
        }

        [Fact]
        public async Task ProvideCompletionAsync_DoesNotProvideCompletionsCanNotFindSnapshotPoint()
        {
            // Arrange
            var codeDocumentProvider = CreateCodeDocumentProvider("@", Enumerable.Empty<DirectiveDescriptor>());
            var completionProvider = new FailOnGetCompletionsProvider(
                codeDocumentProvider,
                CompletionProviderDependencies,
                TextBufferProvider,
                canGetSnapshotPoint: false);
            var document = CreateDocument();
            var context = CreateContext(0, completionProvider, document);

            // Act & Assert
            await completionProvider.ProvideCompletionsAsync(context);
        }

        private static Lazy<RazorCodeDocumentProvider> CreateCodeDocumentProvider(string text, IEnumerable<DirectiveDescriptor> directives)
        {
            var codeDocumentProvider = new Mock<RazorCodeDocumentProvider>();
            var codeDocument = TestRazorCodeDocument.CreateEmpty();
            var sourceDocument = TestRazorSourceDocument.Create(text);
            var options = RazorParserOptions.Create(builder =>
            {
                foreach (var directive in directives)
                {
                    builder.Directives.Add(directive);
                }
            });
            var syntaxTree = RazorSyntaxTree.Parse(sourceDocument, options);
            codeDocument.SetSyntaxTree(syntaxTree);
            codeDocumentProvider.Setup(provider => provider.TryGetFromDocument(It.IsAny<TextDocument>(), out codeDocument))
                .Returns(true);

            return new Lazy<RazorCodeDocumentProvider>(() => codeDocumentProvider.Object);
        }

        private static CompletionContext CreateContext(int position, RazorDirectiveCompletionProvider completionProvider, Document document)
        {
            var context = new CompletionContext(
                completionProvider,
                document,
                position,
                TextSpan.FromBounds(position, position),
                CompletionTrigger.Invoke,
                new Mock<OptionSet>().Object,
                CancellationToken.None);

            return context;
        }

        private static Document CreateDocument()
        {
            Document document = null;
            TestWorkspace.Create(workspace =>
            {
                var project = ProjectInfo
                .Create(ProjectId.CreateNewId(), VersionStamp.Default, "TestProject", "TestAssembly", LanguageNames.CSharp)
                .WithFilePath("/TestProject.csproj");
                workspace.AddProject(project);
                var documentInfo = DocumentInfo.Create(DocumentId.CreateNewId(project.Id), "Test.cshtml");
                document = workspace.AddDocument(documentInfo);
                document = document.WithFilePath("Test.cshtml");
            });

            return document;
        }

        private class FailOnGetCompletionsProvider : RazorDirectiveCompletionProvider
        {
            private readonly bool _canGetSnapshotPoint;

            public FailOnGetCompletionsProvider(
                Lazy<RazorCodeDocumentProvider> codeDocumentProvider,
                Lazy<CompletionProviderDependencies> completionProviderDependencies,
                RazorTextBufferProvider textBufferProvider,
                bool canGetSnapshotPoint = true)
                : base(codeDocumentProvider, completionProviderDependencies, textBufferProvider)
            {
                _canGetSnapshotPoint = canGetSnapshotPoint;
            }

            protected override bool TryGetRazorSnapshotPoint(CompletionContext context, out SnapshotPoint snapshotPoint)
            {
                if (!_canGetSnapshotPoint)
                {
                    snapshotPoint = default(SnapshotPoint);
                    return false;
                }

                var snapshot = new Mock<ITextSnapshot>(MockBehavior.Strict);
                snapshot.Setup(s => s.Length)
                    .Returns(context.CompletionListSpan.End);
                snapshotPoint = new SnapshotPoint(snapshot.Object, context.CompletionListSpan.Start);
                return true;
            }
        }
    }
}
