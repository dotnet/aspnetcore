// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Timers;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using ITextBuffer = Microsoft.VisualStudio.Text.ITextBuffer;
using Timer = System.Timers.Timer;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Editor
{
    internal class VisualStudioRazorParser : IDisposable
    {
        // Internal for testing.
        internal readonly ITextBuffer _textBuffer;
        internal readonly Timer _idleTimer;

        private const int IdleDelay = 3000;
        private readonly ICompletionBroker _completionBroker;
        private readonly BackgroundParser _parser;
        private readonly ForegroundDispatcher _dispatcher;
        private RazorSyntaxTreePartialParser _partialParser;

        // For testing only
        internal VisualStudioRazorParser(RazorCodeDocument codeDocument)
        {
            CodeDocument = codeDocument;
        }

        public VisualStudioRazorParser(ForegroundDispatcher dispatcher, ITextBuffer buffer, RazorTemplateEngine templateEngine, string filePath, ICompletionBroker completionBroker)
        {
            if (dispatcher == null)
            {
                throw new ArgumentNullException(nameof(dispatcher));
            }

            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (templateEngine == null)
            {
                throw new ArgumentNullException(nameof(templateEngine));
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(filePath));
            }

            if (completionBroker == null)
            {
                throw new ArgumentNullException(nameof(completionBroker));
            }

            _dispatcher = dispatcher;
            TemplateEngine = templateEngine;
            FilePath = filePath;
            _textBuffer = buffer;
            _completionBroker = completionBroker;
            _textBuffer.Changed += TextBuffer_OnChanged;
            _parser = new BackgroundParser(templateEngine, filePath);
            _idleTimer = new Timer(IdleDelay);
            _idleTimer.Elapsed += Onidle;
            _parser.ResultsReady += OnResultsReady;

            _parser.Start();
        }

        public event EventHandler<DocumentStructureChangedEventArgs> DocumentStructureChanged;

        public RazorTemplateEngine TemplateEngine { get; }

        public string FilePath { get; }

        public RazorCodeDocument CodeDocument { get; private set; }

        public ITextSnapshot Snapshot { get; private set; }

        public void Reparse()
        {
            // Can be called from any thread
            var snapshot = _textBuffer.CurrentSnapshot;
            _parser.QueueChange(null, snapshot);
        }

        public void Dispose()
        {
            _dispatcher.AssertForegroundThread();

            _textBuffer.Changed -= TextBuffer_OnChanged;
            _parser.Dispose();
            _idleTimer.Dispose();
        }

        private void TextBuffer_OnChanged(object sender, TextContentChangedEventArgs contentChange)
        {
            _dispatcher.AssertForegroundThread();

            if (contentChange.Changes.Count > 0)
            {
                // Idle timers are used to track provisional changes. Provisional changes only last for a single text change. After that normal
                // partial parsing rules apply (stop the timer).
                _idleTimer.Stop();


                var firstChange = contentChange.Changes[0];
                var lastChange = contentChange.Changes[contentChange.Changes.Count - 1];

                var oldLen = lastChange.OldEnd - firstChange.OldPosition;
                var newLen = lastChange.NewEnd - firstChange.NewPosition;

                var wasChanged = true;
                if (oldLen == newLen)
                {
                    var oldText = contentChange.Before.GetText(firstChange.OldPosition, oldLen);
                    var newText = contentChange.After.GetText(firstChange.NewPosition, newLen);
                    wasChanged = !string.Equals(oldText, newText, StringComparison.Ordinal);
                }

                if (wasChanged)
                {
                    var newText = contentChange.After.GetText(firstChange.NewPosition, newLen);
                    var change = new SourceChange(firstChange.OldPosition, oldLen, newText);
                    var snapshot = contentChange.After;
                    var result = PartialParseResultInternal.Rejected;

                    using (_parser.SynchronizeMainThreadState())
                    {
                        // Check if we can partial-parse
                        if (_partialParser != null && _parser.IsIdle)
                        {
                            result = _partialParser.Parse(change);
                        }
                    }

                    // If partial parsing failed or there were outstanding parser tasks, start a full reparse
                    if ((result & PartialParseResultInternal.Rejected) == PartialParseResultInternal.Rejected)
                    {
                        _parser.QueueChange(change, snapshot);
                    }

                    if ((result & PartialParseResultInternal.Provisional) == PartialParseResultInternal.Provisional)
                    {
                        _idleTimer.Start();
                    }
                }
            }
        }

        private void Onidle(object sender, ElapsedEventArgs e)
        {
            _dispatcher.AssertBackgroundThread();

            var textViews = Array.Empty<ITextView>();

            foreach (var textView in textViews)
            {
                if (_completionBroker.IsCompletionActive(textView))
                {
                    return;
                }
            }

            _idleTimer.Stop();
            Reparse();
        }

        private void OnResultsReady(object sender, DocumentStructureChangedEventArgs args)
        {
            _dispatcher.AssertBackgroundThread();

            if (DocumentStructureChanged != null)
            {
                if (args.Snapshot != _textBuffer.CurrentSnapshot)
                {
                    // A different text change is being parsed.
                    return;
                }

                CodeDocument = args.CodeDocument;
                Snapshot = args.Snapshot;
                _partialParser = new RazorSyntaxTreePartialParser(CodeDocument.GetSyntaxTree());
                DocumentStructureChanged(this, args);
            }
        }
    }
}
