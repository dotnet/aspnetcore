// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public class RazorDirectiveCompletionProviderTest
    {
        private static readonly IReadOnlyList<DirectiveDescriptor> DefaultDirectives = new[]
        {
            CSharpCodeParser.AddTagHelperDirectiveDescriptor,
            CSharpCodeParser.RemoveTagHelperDirectiveDescriptor,
            CSharpCodeParser.TagHelperPrefixDirectiveDescriptor,
        };

        [Fact]
        public void AtDirectiveCompletionPoint_ReturnsFalseIfChangeHasNoOwner()
        {
            // Arrange
            var codeDocumentProvider = CreateCodeDocumentProvider("@", Enumerable.Empty<DirectiveDescriptor>());
            var completionProvider = new FailOnGetCompletionsProvider(codeDocumentProvider);
            var document = CreateDocument();
            codeDocumentProvider.Value.TryGetFromDocument(document, out var codeDocument);
            var syntaxTree = codeDocument.GetSyntaxTree();
            var completionContext = CreateContext(2, completionProvider, document);

            // Act
            var result = completionProvider.AtDirectiveCompletionPoint(syntaxTree, completionContext);

            // Assert
            Assert.False(result);
        }

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
            var completionProvider = new RazorDirectiveCompletionProvider(new Lazy<RazorCodeDocumentProvider>(() => codeDocumentProvider.Object));

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
            var completionProvider = new RazorDirectiveCompletionProvider(new Lazy<RazorCodeDocumentProvider>(() => codeDocumentProvider.Object));

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
            var completionProvider = new FailOnGetCompletionsProvider(new Lazy<RazorCodeDocumentProvider>(() => codeDocumentProvider.Object));
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
            var completionProvider = new FailOnGetCompletionsProvider(new Lazy<RazorCodeDocumentProvider>(() => codeDocumentProvider.Object));
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
            var completionProvider = new FailOnGetCompletionsProvider(new Lazy<RazorCodeDocumentProvider>(() => codeDocumentProvider.Object));
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
            var completionProvider = new FailOnGetCompletionsProvider(codeDocumentProvider, false);
            var document = CreateDocument();
            var context = CreateContext(0, completionProvider, document);

            // Act & Assert
            await completionProvider.ProvideCompletionsAsync(context);
        }

        [Fact]
        public async Task ProvideCompletionAsync_DoesNotProvideCompletionsWhenNotAtCompletionPoint()
        {
            // Arrange
            var codeDocumentProvider = CreateCodeDocumentProvider("@", Enumerable.Empty<DirectiveDescriptor>());
            var completionProvider = new FailOnGetCompletionsProvider(codeDocumentProvider);
            var document = CreateDocument();
            var context = CreateContext(0, completionProvider, document);

            // Act & Assert
            await completionProvider.ProvideCompletionsAsync(context);
        }

        [Theory]
        [InlineData("DateTime.Now")]
        [InlineData("SomeMethod()")]
        public async Task ProvideCompletionAsync_DoesNotProvideCompletionsWhenAtComplexExpressions(string content)
        {
            // Arrange
            var codeDocumentProvider = CreateCodeDocumentProvider("@" + content, Enumerable.Empty<DirectiveDescriptor>());
            var completionProvider = new FailOnGetCompletionsProvider(codeDocumentProvider);
            var document = CreateDocument();
            var context = CreateContext(1, completionProvider, document);

            // Act & Assert
            await completionProvider.ProvideCompletionsAsync(context);
        }

        [Fact]
        public async Task ProvideCompletionAsync_DoesNotProvideCompletionsForExplicitExpressions()
        {
            // Arrange
            var codeDocumentProvider = CreateCodeDocumentProvider("@()", Enumerable.Empty<DirectiveDescriptor>());
            var completionProvider = new FailOnGetCompletionsProvider(codeDocumentProvider);
            var document = CreateDocument();
            var context = CreateContext(2, completionProvider, document);

            // Act & Assert
            await completionProvider.ProvideCompletionsAsync(context);
        }

        [Fact]
        public async Task ProvideCompletionAsync_DoesNotProvideCompletionsForCodeDocumentWithoutSyntaxTree()
        {
            // Arrange
            var codeDocumentProvider = new Mock<RazorCodeDocumentProvider>();
            var codeDocument = TestRazorCodeDocument.CreateEmpty();
            codeDocumentProvider.Setup(provider => provider.TryGetFromDocument(It.IsAny<TextDocument>(), out codeDocument))
                .Returns(true);
            var completionProvider = new FailOnGetCompletionsProvider(new Lazy<RazorCodeDocumentProvider>(() => codeDocumentProvider.Object));
            var document = CreateDocument();
            var context = CreateContext(2, completionProvider, document);

            // Act & Assert
            await completionProvider.ProvideCompletionsAsync(context);
        }

        [Fact]
        public void GetCompletionItems_ProvidesCompletionsForDefaultDirectives()
        {
            // Arrange
            var codeDocumentProvider = CreateCodeDocumentProvider("@", Enumerable.Empty<DirectiveDescriptor>());
            var completionProvider = new RazorDirectiveCompletionProvider(codeDocumentProvider);
            var document = CreateDocument();
            codeDocumentProvider.Value.TryGetFromDocument(document, out var codeDocument);
            var syntaxTree = codeDocument.GetSyntaxTree();

            // Act
            var completionItems = completionProvider.GetCompletionItems(syntaxTree);

            // Assert
            Assert.Collection(
                completionItems,
                item => AssertRazorCompletionItem(DefaultDirectives[0].Description, item),
                item => AssertRazorCompletionItem(DefaultDirectives[1].Description, item),
                item => AssertRazorCompletionItem(DefaultDirectives[2].Description, item));
        }

        [Fact]
        public void GetCompletionItems_ProvidesCompletionsForDefaultAndExtensibleDirectives()
        {
            // Arrange
            var codeDocumentProvider = CreateCodeDocumentProvider("@", new[] { SectionDirective.Directive });
            var completionProvider = new RazorDirectiveCompletionProvider(codeDocumentProvider);
            var document = CreateDocument();
            codeDocumentProvider.Value.TryGetFromDocument(document, out var codeDocument);
            var syntaxTree = codeDocument.GetSyntaxTree();

            // Act
            var completionItems = completionProvider.GetCompletionItems(syntaxTree);

            // Assert
            Assert.Collection(
                completionItems,
                item => AssertRazorCompletionItem(SectionDirective.Directive.Description, item),
                item => AssertRazorCompletionItem(DefaultDirectives[0].Description, item),
                item => AssertRazorCompletionItem(DefaultDirectives[1].Description, item),
                item => AssertRazorCompletionItem(DefaultDirectives[2].Description, item));
        }

        [Fact]
        public void GetCompletionItems_ProvidesCompletionsForDirectivesWithoutDescription()
        {
            // Arrange
            var customDirective = DirectiveDescriptor.CreateSingleLineDirective("custom");
            var codeDocumentProvider = CreateCodeDocumentProvider("@", new[] { customDirective });
            var completionProvider = new RazorDirectiveCompletionProvider(codeDocumentProvider);
            var document = CreateDocument();
            codeDocumentProvider.Value.TryGetFromDocument(document, out var codeDocument);
            var syntaxTree = codeDocument.GetSyntaxTree();

            // Act
            var completionItems = completionProvider.GetCompletionItems(syntaxTree);

            // Assert
            var customDirectiveCompletion = Assert.Single(completionItems, item => item.DisplayText == customDirective.Directive);
            AssertRazorCompletionItemDefaults(customDirectiveCompletion);
            Assert.DoesNotContain(customDirectiveCompletion.Properties, property => property.Key == RazorDirectiveCompletionProvider.DescriptionKey);
        }

        private static void AssertRazorCompletionItem(string expectedDescription, CompletionItem item)
        {
            Assert.True(item.Properties.TryGetValue(RazorDirectiveCompletionProvider.DescriptionKey, out var actualDescription));
            Assert.Equal(expectedDescription, actualDescription);

            AssertRazorCompletionItemDefaults(item);
        }

        private static void AssertRazorCompletionItemDefaults(CompletionItem item)
        {
            Assert.Equal("_RazorDirective_", item.SortText);
            Assert.False(item.Rules.FormatOnCommit);
            var tag = Assert.Single(item.Tags);
            Assert.Equal(CompletionTags.Intrinsic, tag);
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

            public FailOnGetCompletionsProvider(Lazy<RazorCodeDocumentProvider> codeDocumentProvider, bool canGetSnapshotPoint = true)
                : base(codeDocumentProvider)
            {
                _canGetSnapshotPoint = canGetSnapshotPoint;
            }

            internal override IEnumerable<CompletionItem> GetCompletionItems(RazorSyntaxTree syntaxTree)
            {
                Assert.False(true, "Completions should not have been attempted.");
                return null;
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
