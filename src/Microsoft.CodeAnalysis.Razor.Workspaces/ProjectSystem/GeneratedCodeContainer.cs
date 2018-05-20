// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Experiment;
using Microsoft.AspNetCore.Razor.Language;
using System.Collections.Immutable;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class GeneratedCodeContainer : IDocumentServiceFactory, ISpanMapper
    {
        private readonly TextContainer _textContainer;

        public GeneratedCodeContainer()
        {
            _textContainer = new TextContainer();
        }

        public SourceText Source { get; private set; }

        public VersionStamp SourceVersion { get; private set; }

        public RazorCSharpDocument Output { get; private set; }

        public SourceTextContainer SourceTextContainer => _textContainer;

        public TService GetService<TService>()
        {
            if (this is TService service)
            {
                return service;
            }

            return default(TService);
        }

        public void SetOutput(SourceText source, RazorCodeDocument codeDocument)
        {
            Source = source;
            Output = codeDocument.GetCSharpDocument();

            _textContainer.SetText(SourceText.From(Output.GeneratedCode));
        }

        public Task<ImmutableArray<SpanMapResult>> MapSpansAsync(
                Document document,
                IEnumerable<TextSpan> spans,
                CancellationToken cancellationToken)
        {
            if (Output == null)
            {
                return Task.FromResult(ImmutableArray<SpanMapResult>.Empty);
            }

            var results = ImmutableArray.CreateBuilder<SpanMapResult>();
            foreach (var span in spans)
            {
                if (TryGetLinePositionSpan(span, out var linePositionSpan))
                {
                    results.Add(new SpanMapResult(document, linePositionSpan));
                }
            }

            return Task.FromResult(results.ToImmutable());
        }

        // Internal for testing.
        internal bool TryGetLinePositionSpan(TextSpan span, out LinePositionSpan linePositionSpan)
        {
            for (var i = 0; i < Output.SourceMappings.Count; i++)
            {
                var mapping = Output.SourceMappings[i];
                if (span.Length > mapping.GeneratedSpan.Length)
                {
                    // If the length of the generated span is smaller they can't match. A C# expression
                    // won't cover multiple generated spans.
                    //
                    // This heuristic is useful in the Razor context to filter out zero-length
                    // spans.
                    continue;
                }

                var original = mapping.OriginalSpan.AsTextSpan();
                var generated = mapping.GeneratedSpan.AsTextSpan();

                var leftOffset = span.Start - generated.Start;
                var rightOffset = span.End - generated.End;
                if (leftOffset >= 0 && rightOffset <= 0)
                {
                    // This span mapping contains the span.
                    var adjusted = new TextSpan(original.Start + leftOffset, (original.End + rightOffset) - (original.Start + leftOffset));
                    linePositionSpan = Source.Lines.GetLinePositionSpan(adjusted);
                    return true;
                }
            }

            linePositionSpan = default;
            return false;
        }

        private class TextContainer : SourceTextContainer
        {
            public override event EventHandler<TextChangeEventArgs> TextChanged;

            private SourceText _currentText;

            public TextContainer()
                : this(SourceText.From(string.Empty))
            {
            }

            public TextContainer(SourceText sourceText)
            {
                if (sourceText == null)
                {
                    throw new ArgumentNullException(nameof(sourceText));
                }

                _currentText = sourceText;
            }

            public override SourceText CurrentText => _currentText;

            public void SetText(SourceText sourceText)
            {
                if (sourceText == null)
                {
                    throw new ArgumentNullException(nameof(sourceText));
                }

                var e = new TextChangeEventArgs(_currentText, sourceText);
                _currentText = sourceText;

                TextChanged?.Invoke(this, e);
            }
        }
    }
}
