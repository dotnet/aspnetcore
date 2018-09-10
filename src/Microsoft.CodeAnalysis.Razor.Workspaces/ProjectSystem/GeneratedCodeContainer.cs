// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Experiment;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class GeneratedCodeContainer : IDocumentServiceFactory, ISpanMapper
    {
        public event EventHandler<TextChangeEventArgs> GeneratedCodeChanged;

        private SourceText _source;
        private VersionStamp? _sourceVersion;
        private RazorCSharpDocument _output;
        private DocumentSnapshot _latestDocument;

        private readonly object _setOutputLock = new object();
        private readonly TextContainer _textContainer;

        public GeneratedCodeContainer()
        {
            _textContainer = new TextContainer(_setOutputLock);
            _textContainer.TextChanged += TextContainer_TextChanged;
        }

        public SourceText Source
        {
            get
            {
                lock (_setOutputLock)
                {
                    return _source;
                }
            }
        }

        public VersionStamp SourceVersion
        {
            get
            {
                lock (_setOutputLock)
                {
                    return _sourceVersion.Value;
                }
            }
        }

        public RazorCSharpDocument Output
        {
            get
            {
                lock (_setOutputLock)
                {
                    return _output;
                }
            }
        }

        public DocumentSnapshot LatestDocument
        {
            get
            {
                lock (_setOutputLock)
                {
                    return _latestDocument;
                }
            }
        }

        public SourceTextContainer SourceTextContainer
        {
            get
            {
                lock (_setOutputLock)
                {
                    return _textContainer;
                }
            }
        }

        public TService GetService<TService>()
        {
            if (this is TService service)
            {
                return service;
            }

            return default(TService);
        }

        public void SetOutput(RazorCSharpDocument csharpDocument, DefaultDocumentSnapshot document)
        {
            lock (_setOutputLock)
            {
                if (!document.TryGetTextVersion(out var version))
                {
                    Debug.Fail("The text version should have already been evaluated.");
                    return;
                }

                if (_sourceVersion.HasValue && _sourceVersion == SourceVersion.GetNewerVersion(version))
                {
                    // Latest document is newer than the provided document.
                    return;
                }

                if (!document.TryGetText(out var source))
                {
                    Debug.Fail("The text should have already been evaluated.");
                    return;
                }

                _source = source;
                _sourceVersion = version;
                _output = csharpDocument;
                _latestDocument = document;
                _textContainer.SetText(SourceText.From(Output.GeneratedCode));
            }
        }

        public Task<ImmutableArray<SpanMapResult>> MapSpansAsync(
            Document document,
            IEnumerable<TextSpan> spans,
            CancellationToken cancellationToken)
        {
            RazorCSharpDocument output;
            SourceText source;
            lock (_setOutputLock)
            {
                if (Output == null)
                {
                    return Task.FromResult(ImmutableArray<SpanMapResult>.Empty);
                }

                output = Output;
                source = Source;
            }

            var results = ImmutableArray.CreateBuilder<SpanMapResult>();
            foreach (var span in spans)
            {
                if (TryGetLinePositionSpan(span, source, output, out var linePositionSpan))
                {
                    results.Add(new SpanMapResult(document, linePositionSpan));
                }
            }

            return Task.FromResult(results.ToImmutable());
        }

        // Internal for testing.
        internal static bool TryGetLinePositionSpan(TextSpan span, SourceText source, RazorCSharpDocument output, out LinePositionSpan linePositionSpan)
        {
            for (var i = 0; i < output.SourceMappings.Count; i++)
            {
                var mapping = output.SourceMappings[i];
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
                    linePositionSpan = source.Lines.GetLinePositionSpan(adjusted);
                    return true;
                }
            }

            linePositionSpan = default;
            return false;
        }

        private void TextContainer_TextChanged(object sender, TextChangeEventArgs args)
        {
            GeneratedCodeChanged?.Invoke(this, args);
        }

        private class TextContainer : SourceTextContainer
        {
            public override event EventHandler<TextChangeEventArgs> TextChanged;

            private readonly object _outerLock;
            private SourceText _currentText;

            public TextContainer(object outerLock)
                : this(SourceText.From(string.Empty))
            {
                _outerLock = outerLock;
            }

            public TextContainer(SourceText sourceText)
            {
                if (sourceText == null)
                {
                    throw new ArgumentNullException(nameof(sourceText));
                }

                _currentText = sourceText;
            }

            public override SourceText CurrentText
            {
                get
                {
                    lock (_outerLock)
                    {
                        return _currentText;
                    }
                }
            }

            public void SetText(SourceText sourceText)
            {
                if (sourceText == null)
                {
                    throw new ArgumentNullException(nameof(sourceText));
                }

                lock (_outerLock)
                {

                    var e = new TextChangeEventArgs(_currentText, sourceText);
                    _currentText = sourceText;

                    TextChanged?.Invoke(this, e);
                }
            }
        }
    }
}
