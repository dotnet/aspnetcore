// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Projection;

namespace Microsoft.VisualStudio.Editor.Razor
{
    [System.Composition.Shared]
    [Export(typeof(CompletionProvider))]
    [ExportMetadata("Language", LanguageNames.CSharp)]
    internal class RazorDirectiveCompletionProvider : CompletionProvider
    {
        // Internal for testing
        internal static readonly string DescriptionKey = "Razor.Description";

        private static readonly IEnumerable<DirectiveDescriptor> DefaultDirectives = new[]
        {
            CSharpCodeParser.AddTagHelperDirectiveDescriptor,
            CSharpCodeParser.RemoveTagHelperDirectiveDescriptor,
            CSharpCodeParser.TagHelperPrefixDirectiveDescriptor,
        };
        private readonly Lazy<RazorCodeDocumentProvider> _codeDocumentProvider;
        private readonly Lazy<CompletionProviderDependencies> _dependencies;
        private readonly RazorTextBufferProvider _textBufferProvider;

        [ImportingConstructor]
        public RazorDirectiveCompletionProvider(
            [Import(typeof(RazorCodeDocumentProvider))] Lazy<RazorCodeDocumentProvider> codeDocumentProvider,
            [Import(typeof(CompletionProviderDependencies))] Lazy<CompletionProviderDependencies> dependencies,
            RazorTextBufferProvider textBufferProvider)
        {
            if (codeDocumentProvider == null)
            {
                throw new ArgumentNullException(nameof(codeDocumentProvider));
            }

            if (dependencies == null)
            {
                throw new ArgumentNullException(nameof(dependencies));
            }

            if (textBufferProvider == null)
            {
                throw new ArgumentNullException(nameof(textBufferProvider));
            }

            _codeDocumentProvider = codeDocumentProvider;
            _dependencies = dependencies;
            _textBufferProvider = textBufferProvider;
        }

        public override Task<CompletionDescription> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var descriptionContent = new List<TaggedText>();
            if (item.Properties.TryGetValue(DescriptionKey, out var directiveDescription))
            {
                var descriptionText = new TaggedText(TextTags.Text, directiveDescription);
                descriptionContent.Add(descriptionText);
            }

            var completionDescription = CompletionDescription.Create(descriptionContent.ToImmutableArray());
            return Task.FromResult(completionDescription);
        }

        public override Task ProvideCompletionsAsync(CompletionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // FilePath will be null when the editor is open for cases where we don't have a file on disk (C# interactive window and others).
            if (context.Document?.FilePath == null ||
                !context.Document.FilePath.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
            {
                // Not a Razor file.
                return Task.CompletedTask;
            }

            var result = AddCompletionItems(context);

            return result;
        }

        // We do not want this inlined because the work done in this method requires Razor.Workspaces and Razor.Language assemblies.
        // If those two assemblies were to load you'd have them load in every C# editor completion scenario.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private Task AddCompletionItems(CompletionContext context)
        {
            if (!_textBufferProvider.TryGetFromDocument(context.Document, out var textBuffer) ||
                _dependencies.Value.AsyncCompletionBroker.IsCompletionSupported(textBuffer.ContentType))
            {
                // Async completion is supported that code path will handle completion.
                return Task.CompletedTask;
            }

            if (!_codeDocumentProvider.Value.TryGetFromDocument(context.Document, out var codeDocument))
            {
                // A Razor code document has not yet been associated with the document.
                return Task.CompletedTask;
            }

            var syntaxTree = codeDocument.GetSyntaxTree();
            if (syntaxTree == null)
            {
                // No syntax tree has been computed for the current document.
                return Task.CompletedTask;
            }

            if (!TryGetRazorSnapshotPoint(context, out var razorSnapshotPoint))
            {
                // Could not find associated Razor location.
                return Task.CompletedTask;
            }

            var location = new SourceSpan(razorSnapshotPoint.Position, 0);
            var razorCompletionItems = _dependencies.Value.CompletionFactsService.GetCompletionItems(syntaxTree, location);

            foreach (var razorCompletionItem in razorCompletionItems)
            {
                if (razorCompletionItem.Kind != RazorCompletionItemKind.Directive)
                {
                    // Don't support any other types of completion kinds other than directives.
                    continue;
                }

                var propertyDictionary = new Dictionary<string, string>(StringComparer.Ordinal);
                if (!string.IsNullOrEmpty(razorCompletionItem.Description))
                {
                    propertyDictionary[DescriptionKey] = razorCompletionItem.Description;
                }

                var completionItem = CompletionItem.Create(
                    razorCompletionItem.InsertText,
                    // This groups all Razor directives together
                    sortText: "_RazorDirective_",
                    rules: CompletionItemRules.Create(formatOnCommit: false),
                    tags: ImmutableArray.Create(WellKnownTags.Intrinsic),
                    properties: propertyDictionary.ToImmutableDictionary());

                context.AddItem(completionItem);
            }

            return Task.CompletedTask;
        }

        protected virtual bool TryGetRazorSnapshotPoint(CompletionContext context, out SnapshotPoint snapshotPoint)
        {
            snapshotPoint = default(SnapshotPoint);

            if (context.Document.TryGetText(out var sourceText))
            {
                var textSnapshot = sourceText.FindCorrespondingEditorTextSnapshot();
                var projectionSnapshot = textSnapshot as IProjectionSnapshot;

                if (projectionSnapshot == null)
                {
                    return false;
                }

                var mappedPoints = projectionSnapshot.MapToSourceSnapshots(context.CompletionListSpan.Start);
                var htmlSnapshotPoints = mappedPoints.Where(p => p.Snapshot.TextBuffer.IsRazorBuffer());

                if (!htmlSnapshotPoints.Any())
                {
                    return false;
                }

                snapshotPoint = htmlSnapshotPoints.First();
                return true;
            }

            return false;
        }
    }

    // These types are only for this class to provide indirection for assembly loads.
    internal abstract class CompletionProviderDependencies
    {
        public abstract RazorCompletionFactsService CompletionFactsService { get; }

        public abstract IAsyncCompletionBroker AsyncCompletionBroker { get; }
    }

    [System.Composition.Shared]
    [Export(typeof(CompletionProviderDependencies))]
    internal class DefaultCompletionProviderDependencies : CompletionProviderDependencies
    {
        [ImportingConstructor]
        public DefaultCompletionProviderDependencies(
            RazorCompletionFactsService completionFactsService,
            IAsyncCompletionBroker asyncCompletionBroker)
        {
            if (completionFactsService == null)
            {
                throw new ArgumentNullException(nameof(completionFactsService));
            }

            if (asyncCompletionBroker == null)
            {
                throw new ArgumentNullException(nameof(asyncCompletionBroker));
            }

            CompletionFactsService = completionFactsService;
            AsyncCompletionBroker = asyncCompletionBroker;
        }

        public override RazorCompletionFactsService CompletionFactsService { get; }

        public override IAsyncCompletionBroker AsyncCompletionBroker { get; }
    }
}
