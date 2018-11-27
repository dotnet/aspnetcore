// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Moq;
using Xunit;
using Span = Microsoft.VisualStudio.Text.Span;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public class RazorDirectiveCompletionSourceTest : ForegroundDispatcherTestBase
    {
        private static readonly IReadOnlyList<DirectiveDescriptor> DefaultDirectives = new[]
        {
            CSharpCodeParser.AddTagHelperDirectiveDescriptor,
            CSharpCodeParser.RemoveTagHelperDirectiveDescriptor,
            CSharpCodeParser.TagHelperPrefixDirectiveDescriptor,
        };

        private RazorCompletionFactsService CompletionFactsService { get; } = new DefaultRazorCompletionFactsService();

        [ForegroundFact]
        public async Task GetCompletionContextAsync_DoesNotProvideCompletionsPriorToParseResults()
        {
            // Arrange
            var text = "@validCompletion";
            var parser = Mock.Of<VisualStudioRazorParser>(); // CodeDocument will be null faking a parser without a parse.
            var completionSource = new RazorDirectiveCompletionSource(Dispatcher, parser, CompletionFactsService);
            var documentSnapshot = new StringTextSnapshot(text);
            var triggerLocation = new SnapshotPoint(documentSnapshot, 4);
            var applicableSpan = new SnapshotSpan(documentSnapshot, new Span(1, text.Length - 1 /* validCompletion */));

            // Act
            var completionContext = await Task.Run(
                async () => await completionSource.GetCompletionContextAsync(null, new CompletionTrigger(CompletionTriggerReason.Invoke, triggerLocation.Snapshot), triggerLocation, applicableSpan, CancellationToken.None));

            // Assert
            Assert.Empty(completionContext.Items);
        }

        [ForegroundFact]
        public async Task GetCompletionContextAsync_DoesNotProvideCompletionsWhenNotAtCompletionPoint()
        {
            // Arrange
            var text = "@(NotValidCompletionLocation)";
            var parser = CreateParser(text);
            var completionSource = new RazorDirectiveCompletionSource(Dispatcher, parser, CompletionFactsService);
            var documentSnapshot = new StringTextSnapshot(text);
            var triggerLocation = new SnapshotPoint(documentSnapshot, 4);
            var applicableSpan = new SnapshotSpan(documentSnapshot, new Span(2, text.Length - 3 /* @() */));

            // Act
            var completionContext = await Task.Run(
                async () => await completionSource.GetCompletionContextAsync(null, new CompletionTrigger(CompletionTriggerReason.Invoke, triggerLocation.Snapshot), triggerLocation, applicableSpan, CancellationToken.None));

            // Assert
            Assert.Empty(completionContext.Items);
        }

        // This is more of an integration level test validating the end-to-end completion flow.
        [ForegroundFact]
        public async Task GetCompletionContextAsync_ProvidesCompletionsWhenAtCompletionPoint()
        {
            // Arrange
            var text = "@addTag";
            var parser = CreateParser(text, SectionDirective.Directive);
            var completionSource = new RazorDirectiveCompletionSource(Dispatcher, parser, CompletionFactsService);
            var documentSnapshot = new StringTextSnapshot(text);
            var triggerLocation = new SnapshotPoint(documentSnapshot, 4);
            var applicableSpan = new SnapshotSpan(documentSnapshot, new Span(1, 6 /* addTag */));

            // Act
            var completionContext = await Task.Run(
                async () => await completionSource.GetCompletionContextAsync(null, new CompletionTrigger(CompletionTriggerReason.Invoke, triggerLocation.Snapshot), triggerLocation, applicableSpan, CancellationToken.None));

            // Assert
            Assert.Collection(
                completionContext.Items,
                item => AssertRazorCompletionItem(SectionDirective.Directive, item, completionSource),
                item => AssertRazorCompletionItem(DefaultDirectives[0], item, completionSource),
                item => AssertRazorCompletionItem(DefaultDirectives[1], item, completionSource),
                item => AssertRazorCompletionItem(DefaultDirectives[2], item, completionSource));
        }

        [Fact]
        public async Task GetDescriptionAsync_AddsDirectiveDescriptionIfPropertyExists()
        {
            // Arrange
            var completionItem = new CompletionItem("TestDirective", Mock.Of<IAsyncCompletionSource>());
            var expectedDescription = "The expected description";
            completionItem.Properties.AddProperty(RazorDirectiveCompletionSource.DescriptionKey, expectedDescription);
            var completionSource = new RazorDirectiveCompletionSource(Dispatcher, Mock.Of<VisualStudioRazorParser>(), CompletionFactsService);

            // Act
            var descriptionObject = await completionSource.GetDescriptionAsync(null, completionItem, CancellationToken.None);

            // Assert
            var description = Assert.IsType<string>(descriptionObject);
            Assert.Equal(expectedDescription, descriptionObject);
        }

        [Fact]
        public async Task GetDescriptionAsync_DoesNotAddDescriptionWhenPropertyAbsent()
        {
            // Arrange
            var completionItem = new CompletionItem("TestDirective", Mock.Of<IAsyncCompletionSource>());
            var completionSource = new RazorDirectiveCompletionSource(Dispatcher, Mock.Of<VisualStudioRazorParser>(), CompletionFactsService);

            // Act
            var descriptionObject = await completionSource.GetDescriptionAsync(null, completionItem, CancellationToken.None);

            // Assert
            var description = Assert.IsType<string>(descriptionObject);
            Assert.Equal(string.Empty, description);
        }

        private static void AssertRazorCompletionItem(string completionDisplayText, DirectiveDescriptor directive, CompletionItem item, IAsyncCompletionSource source)
        {
            Assert.Equal(item.DisplayText, completionDisplayText);
            Assert.Equal(item.FilterText, completionDisplayText);
            Assert.Equal(item.InsertText, directive.Directive);
            Assert.Same(item.Source, source);
            Assert.True(item.Properties.TryGetProperty<string>(RazorDirectiveCompletionSource.DescriptionKey, out var actualDescription));
            Assert.Equal(directive.Description, actualDescription);

            AssertRazorCompletionItemDefaults(item);
        }

        private static void AssertRazorCompletionItem(DirectiveDescriptor directive, CompletionItem item, IAsyncCompletionSource source) =>
            AssertRazorCompletionItem(directive.Directive, directive, item, source);

        private static void AssertRazorCompletionItemDefaults(CompletionItem item)
        {
            Assert.Equal(item.Icon.ImageId.Guid, RazorDirectiveCompletionSource.DirectiveImageGlyph.ImageId.Guid);
            var filter = Assert.Single(item.Filters);
            Assert.Same(RazorDirectiveCompletionSource.DirectiveCompletionFilters[0], filter);
            Assert.Equal(string.Empty, item.Suffix);
            Assert.Equal(item.DisplayText, item.SortText);
            Assert.Empty(item.AttributeIcons);
        }

        private static VisualStudioRazorParser CreateParser(string text, params DirectiveDescriptor[] directives)
        {
            var syntaxTree = CreateSyntaxTree(text, directives);
            var codeDocument = TestRazorCodeDocument.Create(text);
            codeDocument.SetSyntaxTree(syntaxTree);
            var parser = Mock.Of<VisualStudioRazorParser>(p => p.CodeDocument == codeDocument);

            return parser;
        }

        private static RazorSyntaxTree CreateSyntaxTree(string text, params DirectiveDescriptor[] directives)
        {
            var sourceDocument = TestRazorSourceDocument.Create(text);
            var options = RazorParserOptions.Create(builder =>
            {
                foreach (var directive in directives)
                {
                    builder.Directives.Add(directive);
                }
            });
            var syntaxTree = RazorSyntaxTree.Parse(sourceDocument, options);
            return syntaxTree;
        }
    }
}
