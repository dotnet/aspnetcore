// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor
{
    internal class RazorDocumentExcerptService : IDocumentExcerptService
    {
        private readonly DocumentSnapshot _document;
        private readonly ISpanMappingService _mapper;

        public RazorDocumentExcerptService(DocumentSnapshot document, ISpanMappingService mapper)
        {
            if (mapper == null)
            {
                throw new ArgumentNullException(nameof(mapper));
            }

            _document = document;
            _mapper = mapper;
        }

        public async Task<ExcerptResult?> TryExcerptAsync(
            Document document,
            TextSpan span,
            ExcerptMode mode,
            CancellationToken cancellationToken)
        {
            var result = await TryGetExcerptInternalAsync(document, span, (ExcerptModeInternal)mode, cancellationToken).ConfigureAwait(false);
            return result?.ToExcerptResult();
        }

        public async Task<ExcerptResultInternal?> TryGetExcerptInternalAsync(
            Document document,
            TextSpan span,
            ExcerptModeInternal mode,
            CancellationToken cancellationToken)
        { 
            if (_document == null)
            {
                return null;
            }

            var mapped = await _mapper.MapSpansAsync(document, new[] { span }, cancellationToken).ConfigureAwait(false);
            if (mapped.Length == 0 || mapped[0].Equals(default(MappedSpanResult)))
            {
                return null;
            }

            var project = _document.Project;
            var primaryDocument = project.GetDocument(mapped[0].FilePath);
            if (primaryDocument == null)
            {
                return null;
            }

            var primaryText = await primaryDocument.GetTextAsync().ConfigureAwait(false);
            var primarySpan = primaryText.Lines.GetTextSpan(mapped[0].LinePositionSpan);

            var secondaryDocument = document;
            var secondarySpan = span;

            // First compute the range of text we want to we to display relative to the primary document.
            var excerptSpan = ChooseExcerptSpan(primaryText, primarySpan, mode);

            // Then we'll classify the spans based on the primary document, since that's the coordinate
            // space that our output mappings use.
            var output = await _document.GetGeneratedOutputAsync().ConfigureAwait(false);
            var mappings = output.GetCSharpDocument().SourceMappings;
            var classifiedSpans = await ClassifyPreviewAsync(
                primaryText, 
                excerptSpan, 
                secondaryDocument, 
                mappings,
                cancellationToken).ConfigureAwait(false);

            // Now translate everything to be relative to the excerpt
            var offset = 0 - excerptSpan.Start;
            var excerptText = primaryText.GetSubText(excerptSpan);
            excerptSpan = new TextSpan(0, excerptSpan.Length);
            primarySpan = new TextSpan(primarySpan.Start + offset, primarySpan.Length);

            for (var i = 0; i < classifiedSpans.Count; i++)
            {
                var classifiedSpan = classifiedSpans[i];
                var updated = new TextSpan(classifiedSpan.TextSpan.Start + offset, classifiedSpan.TextSpan.Length);
                Debug.Assert(excerptSpan.Contains(updated));

                classifiedSpans[i] = new ClassifiedSpan(classifiedSpan.ClassificationType, updated);
            }

            return new ExcerptResultInternal(excerptText, primarySpan, classifiedSpans.ToImmutable(), document, span);
        }
        
        private TextSpan ChooseExcerptSpan(SourceText primaryText, TextSpan primarySpan, ExcerptModeInternal mode)
        {
            var startLine = primaryText.Lines.GetLineFromPosition(primarySpan.Start);
            var endLine = primaryText.Lines.GetLineFromPosition(primarySpan.End);

            // If we're showing a single line then this will do. Otherwise expand the range by 3 in
            // each direction (if possible).
            if (mode == ExcerptModeInternal.Tooltip)
            {
                var index = Math.Max(startLine.LineNumber - 3, 0);
                startLine = primaryText.Lines[index];
            }

            if (mode == ExcerptModeInternal.Tooltip)
            {
                var index = Math.Min(endLine.LineNumber + 3, primaryText.Lines.Count - 1);
                endLine = primaryText.Lines[index];
            }

            return new TextSpan(startLine.Start, endLine.End - startLine.Start);
        }

        private async Task<ImmutableArray<ClassifiedSpan>.Builder> ClassifyPreviewAsync(
            SourceText primaryText,
            TextSpan excerptSpan,
            Document secondaryDocument,
            IReadOnlyList<SourceMapping> mappings,
            CancellationToken cancellationToken)
        {
            var builder = ImmutableArray.CreateBuilder<ClassifiedSpan>();

            var sorted = new List<SourceMapping>(mappings);
            sorted.Sort((x, y) => x.OriginalSpan.AbsoluteIndex.CompareTo(y.OriginalSpan.AbsoluteIndex));

            // The algorithm here is to iterate through the source mappings (sorted) and use the C# classifier
            // on the spans that are known to the C#. For the spans that are not known to be C# then 
            // we just treat them as text since we'd don't currently have our own classifications.

            var remainingSpan = excerptSpan;
            for (var i = 0; i < sorted.Count && excerptSpan.Length > 0; i++)
            {
                var primarySpan = sorted[i].OriginalSpan.AsTextSpan();
                var intersection = primarySpan.Intersection(remainingSpan);
                if (intersection == null)
                {
                    // This span is outside the area we're interested in.
                    continue;
                }

                // OK this span intersects with the excerpt span, so we will process it. Let's compute
                // the secondary span that matches the intersection.
                var secondarySpan = sorted[i].GeneratedSpan.AsTextSpan();
                secondarySpan = new TextSpan(secondarySpan.Start + intersection.Value.Start - primarySpan.Start, intersection.Value.Length);
                primarySpan = intersection.Value;
                
                if (remainingSpan.Start < primarySpan.Start)
                {
                    // The position is before the next C# span. Classify everything up to the C# start
                    // as text.
                    builder.Add(new ClassifiedSpan(ClassificationTypeNames.Text, new TextSpan(remainingSpan.Start, primarySpan.Start - remainingSpan.Start)));

                    // Advance to the start of the C# span.
                    remainingSpan = new TextSpan(primarySpan.Start, remainingSpan.Length - (primarySpan.Start - remainingSpan.Start));
                }

                // We should be able to process this whole span as C#, so classify it.
                //
                // However, we'll have to translate it to the the secondary document's coordinates to do that.
                Debug.Assert(remainingSpan.Contains(primarySpan) && remainingSpan.Start == primarySpan.Start);
                var classifiedSecondarySpans = await Classifier.GetClassifiedSpansAsync(
                    secondaryDocument, 
                    secondarySpan, 
                    cancellationToken);

                // NOTE: The Classifier will only returns spans for things that it understands. That means
                // that whitespace is not classified. The preview expects us to provide contiguous spans, 
                // so we are going to have to fill in the gaps.
                
                // Now we have to translate back to the primary document's coordinates.
                var offset = primarySpan.Start - secondarySpan.Start;
                foreach (var classifiedSecondarySpan in classifiedSecondarySpans)
                {
                    Debug.Assert(secondarySpan.Contains(classifiedSecondarySpan.TextSpan));
                    
                    var updated = new TextSpan(classifiedSecondarySpan.TextSpan.Start + offset, classifiedSecondarySpan.TextSpan.Length);
                    Debug.Assert(primarySpan.Contains(updated));

                    // Make sure that we're not introducing a gap. Remember, we need to fill in the whitespace.
                    if (remainingSpan.Start < updated.Start)
                    {
                        builder.Add(new ClassifiedSpan(
                            ClassificationTypeNames.Text,
                            new TextSpan(remainingSpan.Start, updated.Start - remainingSpan.Start)));
                        remainingSpan = new TextSpan(updated.Start, remainingSpan.Length - (updated.Start - remainingSpan.Start));
                    }
                    
                    builder.Add(new ClassifiedSpan(classifiedSecondarySpan.ClassificationType, updated));
                    remainingSpan = new TextSpan(updated.End, remainingSpan.Length - (updated.End - remainingSpan.Start));
                }

                // Make sure that we're not introducing a gap. Remember, we need to fill in the whitespace.
                if (remainingSpan.Start < primarySpan.End)
                {
                    builder.Add(new ClassifiedSpan(
                        ClassificationTypeNames.Text,
                        new TextSpan(remainingSpan.Start, primarySpan.End - remainingSpan.Start)));
                    remainingSpan = new TextSpan(primarySpan.End, remainingSpan.Length - (primarySpan.End - remainingSpan.Start));
                }

            }

            // Deal with residue
            if (remainingSpan.Length > 0)
            {
                // Trailing Razor/markup text.
                builder.Add(new ClassifiedSpan(ClassificationTypeNames.Text, remainingSpan));
            }

            return builder;
        }

        // We have IVT access to the Roslyn APIs for product code, but not for testing.
        public enum ExcerptModeInternal
        {
            SingleLine = ExcerptMode.SingleLine,
            Tooltip = ExcerptMode.Tooltip,
        }

        // We have IVT access to the Roslyn APIs for product code, but not for testing.
        public readonly struct ExcerptResultInternal
        {
            public readonly SourceText Content;

            public readonly TextSpan MappedSpan;

            public readonly ImmutableArray<ClassifiedSpan> ClassifiedSpans;

            public readonly Document Document;

            public readonly TextSpan Span;

            public ExcerptResultInternal(
                SourceText content,
                TextSpan mappedSpan,
                ImmutableArray<ClassifiedSpan> classifiedSpans,
                Document document,
                TextSpan span)
            {
                Content = content;
                MappedSpan = mappedSpan;
                ClassifiedSpans = classifiedSpans;
                Document = document;
                Span = span;
            }

            public ExcerptResult ToExcerptResult()
            {
                return new ExcerptResult(Content, MappedSpan, ClassifiedSpans, Document, Span);
            }
        }
    }
}