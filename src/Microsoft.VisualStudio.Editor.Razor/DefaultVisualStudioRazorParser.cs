// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Editor;
using Microsoft.VisualStudio.Text;
using static Microsoft.VisualStudio.Editor.Razor.BackgroundParser;
using ITextBuffer = Microsoft.VisualStudio.Text.ITextBuffer;
using Timer = System.Threading.Timer;

namespace Microsoft.VisualStudio.Editor.Razor
{
    internal class DefaultVisualStudioRazorParser : VisualStudioRazorParser, IDisposable
    {
        public override event EventHandler<DocumentStructureChangedEventArgs> DocumentStructureChanged;

        // Internal for testing.
        internal TimeSpan IdleDelay = TimeSpan.FromSeconds(3);
        internal Timer _idleTimer;
        internal BackgroundParser _parser;
        internal ChangeReference _latestChangeReference;
        internal RazorSyntaxTreePartialParser _partialParser;

        private readonly object IdleLock = new object();
        private readonly VisualStudioCompletionBroker _completionBroker;
        private readonly VisualStudioDocumentTracker _documentTracker;
        private readonly ForegroundDispatcher _dispatcher;
        private readonly ProjectSnapshotProjectEngineFactory _projectEngineFactory;
        private readonly ErrorReporter _errorReporter;
        private RazorProjectEngine _projectEngine;
        private RazorCodeDocument _codeDocument;
        private ITextSnapshot _snapshot;
        private bool _disposed;

        // For testing only
        internal DefaultVisualStudioRazorParser(RazorCodeDocument codeDocument)
        {
            _codeDocument = codeDocument;
        }

        public DefaultVisualStudioRazorParser(
            ForegroundDispatcher dispatcher,
            VisualStudioDocumentTracker documentTracker,
            ProjectSnapshotProjectEngineFactory projectEngineFactory,
            ErrorReporter errorReporter,
            VisualStudioCompletionBroker completionBroker)
        {
            if (dispatcher == null)
            {
                throw new ArgumentNullException(nameof(dispatcher));
            }

            if (documentTracker == null)
            {
                throw new ArgumentNullException(nameof(documentTracker));
            }

            if (projectEngineFactory == null)
            {
                throw new ArgumentNullException(nameof(projectEngineFactory));
            }

            if (errorReporter == null)
            {
                throw new ArgumentNullException(nameof(errorReporter));
            }

            if (completionBroker == null)
            {
                throw new ArgumentNullException(nameof(completionBroker));
            }

            _dispatcher = dispatcher;
            _projectEngineFactory = projectEngineFactory;
            _errorReporter = errorReporter;
            _completionBroker = completionBroker;
            _documentTracker = documentTracker;

            _documentTracker.ContextChanged += DocumentTracker_ContextChanged;
        }

        public override string FilePath => _documentTracker.FilePath;

        public override RazorCodeDocument CodeDocument => _codeDocument;

        public override ITextSnapshot Snapshot => _snapshot;

        public override ITextBuffer TextBuffer => _documentTracker.TextBuffer;

        public override bool HasPendingChanges => _latestChangeReference != null;

        // Used in unit tests to ensure we can be notified when idle starts.
        internal ManualResetEventSlim NotifyForegroundIdleStart { get; set; }

        // Used in unit tests to ensure we can block background idle work.
        internal ManualResetEventSlim BlockBackgroundIdleWork { get; set; }

        public override void QueueReparse()
        {
            // Can be called from any thread

            if (_dispatcher.IsForegroundThread)
            {
                ReparseOnForeground(null);
            }
            else
            {
                Task.Factory.StartNew(ReparseOnForeground, null, CancellationToken.None, TaskCreationOptions.None, _dispatcher.ForegroundScheduler);
            }
        }

        public void Dispose()
        {
            _dispatcher.AssertForegroundThread();

            StopParser();

            _documentTracker.ContextChanged -= DocumentTracker_ContextChanged;

            StopIdleTimer();

            _disposed = true;
        }

        // Internal for testing
        internal void DocumentTracker_ContextChanged(object sender, ContextChangeEventArgs args)
        {
            _dispatcher.AssertForegroundThread();

            if (!TryReinitializeParser())
            {
                return;
            }

            // We have a new parser, force a reparse to generate new document information. Note that this
            // only blocks until the reparse change has been queued.
            QueueReparse();
        }

        // Internal for testing
        internal bool TryReinitializeParser()
        {
            _dispatcher.AssertForegroundThread();

            StopParser();

            if (!_documentTracker.IsSupportedProject)
            {
                // Tracker is either starting up, tearing down or wrongfully instantiated.
                // Either way, the tracker can't act on its associated project, neither can we.
                return false;
            }

            StartParser();

            return true;
        }

        // Internal for testing
        internal void StartParser()
        {
            _dispatcher.AssertForegroundThread();

            // Make sure any tests use the real thing or a good mock. These tests can cause failures
            // that are hard to understand when this throws.
            Debug.Assert(_documentTracker.IsSupportedProject);
            Debug.Assert(_documentTracker.ProjectSnapshot != null);

            _projectEngine = _projectEngineFactory.Create(_documentTracker.ProjectSnapshot, ConfigureProjectEngine);

            Debug.Assert(_projectEngine != null); 
            Debug.Assert(_projectEngine.Engine != null);
            Debug.Assert(_projectEngine.FileSystem != null);

            var projectDirectory = Path.GetDirectoryName(_documentTracker.ProjectPath);
            _parser = new BackgroundParser(_projectEngine, FilePath, projectDirectory);
            _parser.ResultsReady += OnResultsReady;
            _parser.Start();

            TextBuffer.Changed += TextBuffer_OnChanged;
        }

        // Internal for testing
        internal void StopParser()
        {
            _dispatcher.AssertForegroundThread();

            if (_parser != null)
            {
                // Detatch from the text buffer until we have a new parser to handle changes.
                TextBuffer.Changed -= TextBuffer_OnChanged;

                _parser.ResultsReady -= OnResultsReady;
                _parser.Dispose();
                _parser = null;
            }
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
                QueueChange(change, snapshot);
            }

            if ((result & PartialParseResultInternal.Provisional) == PartialParseResultInternal.Provisional)
            {
                StartIdleTimer();
            }
        }

        // Internal for testing
        internal void OnIdle(object state)
        {
            _dispatcher.AssertForegroundThread();

            if (_disposed)
            {
                return;
            }

            OnNotifyForegroundIdle();

            foreach (var textView in _documentTracker.TextViews)
            {
                if (_completionBroker.IsCompletionActive(textView))
                {
                    // Completion list is still active, need to re-start timer.
                    StartIdleTimer();
                    return;
                }
            }

            QueueReparse();
        }

        // Internal for testing
        internal void ReparseOnForeground(object state)
        {
            _dispatcher.AssertForegroundThread();

            if (_disposed)
            {
                return;
            }

            var snapshot = TextBuffer.CurrentSnapshot;
            QueueChange(null, snapshot);
        }

        private void QueueChange(SourceChange change, ITextSnapshot snapshot)
        {
            _dispatcher.AssertForegroundThread();

            _latestChangeReference = _parser.QueueChange(change, snapshot);
        }

        private void OnNotifyForegroundIdle()
        {
            if (NotifyForegroundIdleStart != null)
            {
                NotifyForegroundIdleStart.Set();
            }
        }

        private void OnStartingBackgroundIdleWork()
        {
            if (BlockBackgroundIdleWork != null)
            {
                BlockBackgroundIdleWork.Wait();
            }
        }

        private void Timer_Tick(object state)
        {
            try
            {
                _dispatcher.AssertBackgroundThread();

                OnStartingBackgroundIdleWork();

                StopIdleTimer();

                // We need to get back to the UI thread to properly check if a completion is active.
                Task.Factory.StartNew(OnIdle, null, CancellationToken.None, TaskCreationOptions.None, _dispatcher.ForegroundScheduler);
            }
            catch (Exception ex)
            {
                // This is something totally unexpected, let's just send it over to the workspace.
                Task.Factory.StartNew(() => _errorReporter.ReportError(ex), CancellationToken.None, TaskCreationOptions.None, _dispatcher.ForegroundScheduler);
            }
        }

        private void OnResultsReady(object sender, BackgroundParserResultsReadyEventArgs args)
        {
            _dispatcher.AssertBackgroundThread();

            // Jump back to UI thread to notify structure changes.
            Task.Factory.StartNew(OnDocumentStructureChanged, args, CancellationToken.None, TaskCreationOptions.None, _dispatcher.ForegroundScheduler);
        }

        // Internal for testing
        internal void OnDocumentStructureChanged(object state)
        {
            _dispatcher.AssertForegroundThread();

            if (_disposed)
            {
                return;
            }

            var backgroundParserArgs = (BackgroundParserResultsReadyEventArgs)state;
            if (_latestChangeReference == null || // extra hardening
                _latestChangeReference != backgroundParserArgs.ChangeReference)
            {
                // In the middle of parsing a newer change or about to parse a newer change.
                return;
            }

            if (backgroundParserArgs.ChangeReference.Snapshot != TextBuffer.CurrentSnapshot)
            {
                // Changes have impacted the snapshot after our we recorded our last change reference.
                // This can happen for a multitude of reasons, usually because of a user auto-completing
                // C# statements (causes multiple edits in quick succession). This ensures that our latest
                // parse corresponds to the current snapshot.
                QueueReparse();
                return;
            }

            _latestChangeReference = null;
            _codeDocument = backgroundParserArgs.CodeDocument;
            _snapshot = backgroundParserArgs.ChangeReference.Snapshot;
            _partialParser = new RazorSyntaxTreePartialParser(CodeDocument.GetSyntaxTree());

            var documentStructureChangedArgs = new DocumentStructureChangedEventArgs(
                backgroundParserArgs.ChangeReference.Change, 
                backgroundParserArgs.ChangeReference.Snapshot, 
                backgroundParserArgs.CodeDocument);
            DocumentStructureChanged?.Invoke(this, documentStructureChangedArgs);
        }

        private void ConfigureProjectEngine(RazorProjectEngineBuilder builder)
        {
            builder.Features.Add(new VisualStudioParserOptionsFeature(_documentTracker.EditorSettings));
            builder.Features.Add(new VisualStudioTagHelperFeature(_documentTracker.TagHelpers));
        }

        private class VisualStudioParserOptionsFeature : RazorEngineFeatureBase, IConfigureRazorCodeGenerationOptionsFeature
        {
            private readonly EditorSettings _settings;

            public VisualStudioParserOptionsFeature(EditorSettings settings)
            {
                _settings = settings;
            }

            public int Order { get; set; }

            public void Configure(RazorCodeGenerationOptionsBuilder options)
            {
                options.IndentSize = _settings.IndentSize;
                options.IndentWithTabs = _settings.IndentWithTabs;
            }
        }

        private class VisualStudioTagHelperFeature : ITagHelperFeature
        {
            private readonly IReadOnlyList<TagHelperDescriptor> _tagHelpers;

            public VisualStudioTagHelperFeature(IReadOnlyList<TagHelperDescriptor> tagHelpers)
            {
                _tagHelpers = tagHelpers;
            }

            public RazorEngine Engine { get; set; }

            public IReadOnlyList<TagHelperDescriptor> GetDescriptors()
            {
                return _tagHelpers;
            }
        }
    }
}
