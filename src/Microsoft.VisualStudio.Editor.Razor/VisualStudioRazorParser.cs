// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using ITextBuffer = Microsoft.VisualStudio.Text.ITextBuffer;
using Timer = System.Threading.Timer;

namespace Microsoft.VisualStudio.Editor.Razor
{
    internal class VisualStudioRazorParser : IDisposable
    {
        // Internal for testing.
        internal readonly ITextBuffer _textBuffer;
        internal TimeSpan IdleDelay = TimeSpan.FromSeconds(3);
        internal Timer _idleTimer;

        private readonly object IdleLock = new object();
        private readonly ICompletionBroker _completionBroker;
        private readonly VisualStudioDocumentTrackerFactory _documentTrackerFactory;
        private readonly BackgroundParser _parser;
        private readonly ForegroundDispatcher _dispatcher;
        private readonly ErrorReporter _errorReporter;
        private RazorSyntaxTreePartialParser _partialParser;
        private BraceSmartIndenter _braceSmartIndenter;

        // For testing only
        internal VisualStudioRazorParser(RazorCodeDocument codeDocument)
        {
            CodeDocument = codeDocument;
        }

        public VisualStudioRazorParser(
            ForegroundDispatcher dispatcher,
            ITextBuffer buffer,
            RazorTemplateEngine templateEngine,
            string filePath,
            ErrorReporter errorReporter,
            ICompletionBroker completionBroker,
            VisualStudioDocumentTrackerFactory documentTrackerFactory,
            IEditorOperationsFactoryService editorOperationsFactory)
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

            if (errorReporter == null)
            {
                throw new ArgumentNullException(nameof(errorReporter));
            }

            if (completionBroker == null)
            {
                throw new ArgumentNullException(nameof(completionBroker));
            }

            if (documentTrackerFactory == null)
            {
                throw new ArgumentNullException(nameof(documentTrackerFactory));
            }

            if (editorOperationsFactory == null)
            {
                throw new ArgumentNullException(nameof(editorOperationsFactory));
            }

            _dispatcher = dispatcher;
            TemplateEngine = templateEngine;
            FilePath = filePath;
            _errorReporter = errorReporter;
            _textBuffer = buffer;
            _completionBroker = completionBroker;
            _documentTrackerFactory = documentTrackerFactory;
            _textBuffer.Changed += TextBuffer_OnChanged;
            _braceSmartIndenter = new BraceSmartIndenter(_dispatcher, _textBuffer, _documentTrackerFactory, editorOperationsFactory);
            _parser = new BackgroundParser(templateEngine, filePath);
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
            _braceSmartIndenter.Dispose();
            _parser.Dispose();

            StopIdleTimer();
        }

        // Internal for testing
        internal void StartIdleTimer()
        {
            _dispatcher.AssertForegroundThread();

            lock (IdleLock)
            {
                if (_idleTimer == null)
                {
                    // Timer will fire after a fixed delay, but only once.
                    _idleTimer = new Timer(Timer_Tick, null, IdleDelay, Timeout.InfiniteTimeSpan);
                }
            }
        }

        // Internal for testing
        internal void StopIdleTimer()
        {
            // Can be called from any thread.

            lock (IdleLock)
            {
                if (_idleTimer != null)
                {
                    _idleTimer.Dispose();
                    _idleTimer = null;
                }
            }
        }

        private void TextBuffer_OnChanged(object sender, TextContentChangedEventArgs args)
        {
            _dispatcher.AssertForegroundThread();

            if (args.Changes.Count > 0)
            {
                // Idle timers are used to track provisional changes. Provisional changes only last for a single text change. After that normal
                // partial parsing rules apply (stop the timer).
                StopIdleTimer();
            }

            if (!args.TextChangeOccurred(out var changeInformation))
            {
                return;
            }

            var change = new SourceChange(changeInformation.firstChange.OldPosition, changeInformation.oldText.Length, changeInformation.newText);
            var snapshot = args.After;
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
                StartIdleTimer();
            }
        }

        private void OnIdle(object state)
        {
            _dispatcher.AssertForegroundThread();

            var documentTracker = _documentTrackerFactory.GetTracker(_textBuffer);

            if (documentTracker == null)
            {
                Debug.Fail("Document tracker should never be null when checking idle state.");
                return;
            }

            foreach (var textView in documentTracker.TextViews)
            {
                if (_completionBroker.IsCompletionActive(textView))
                {
                    // Completion list is still active, need to re-start timer.
                    StartIdleTimer();
                    return;
                }
            }

            Reparse();
        }

        private async void Timer_Tick(object state)
        {
            try
            {
                _dispatcher.AssertBackgroundThread();

                StopIdleTimer();

                // We need to get back to the UI thread to properly check if a completion is active.
                await Task.Factory.StartNew(OnIdle, null, CancellationToken.None, TaskCreationOptions.None, _dispatcher.ForegroundScheduler);
            }
            catch (Exception ex)
            {
                // This is something totally unexpected, let's just send it over to the workspace.
                await Task.Factory.StartNew(() => _errorReporter.ReportError(ex), CancellationToken.None, TaskCreationOptions.None, _dispatcher.ForegroundScheduler);
            }
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
