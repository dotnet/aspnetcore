// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Editor.Razor.Documents
{
    // Tracks the mutable state associated with a document - in contrast to DocumentSnapshot
    // which tracks the state at a point in time.
    internal sealed class EditorDocument : IDisposable
    {
        private readonly EditorDocumentManager _documentManager;
        private readonly FileChangeTracker _fileTracker;
        private readonly SnapshotChangeTracker _snapshotTracker;
        private readonly EventHandler _changedOnDisk;
        private readonly EventHandler _changedInEditor;
        private readonly EventHandler _opened;
        private readonly EventHandler _closed;

        private bool _disposed;

        public EditorDocument(
            EditorDocumentManager documentManager,
            string projectFilePath,
            string documentFilePath,
            TextLoader textLoader,
            FileChangeTracker fileTracker,
            ITextBuffer textBuffer,
            EventHandler changedOnDisk,
            EventHandler changedInEditor,
            EventHandler opened,
            EventHandler closed)
        {
            if (documentManager == null)
            {
                throw new ArgumentNullException(nameof(documentManager));
            }

            if (projectFilePath == null)
            {
                throw new ArgumentNullException(nameof(projectFilePath));
            }

            if (documentFilePath == null)
            {
                throw new ArgumentNullException(nameof(documentFilePath));
            }

            if (textLoader == null)
            {
                throw new ArgumentNullException(nameof(textLoader));
            }

            if (fileTracker == null)
            {
                throw new ArgumentNullException(nameof(fileTracker));
            }

            _documentManager = documentManager;
            ProjectFilePath = projectFilePath;
            DocumentFilePath = documentFilePath;
            TextLoader = textLoader;
            _fileTracker = fileTracker;
            _changedOnDisk = changedOnDisk;
            _changedInEditor = changedInEditor;
            _opened = opened;
            _closed = closed;

            _snapshotTracker = new SnapshotChangeTracker();
            _fileTracker.Changed += ChangeTracker_Changed;

            // Only one of these should be active at a time.
            if (textBuffer == null)
            {
                _fileTracker.StartListening();
            }
            else
            {
                _snapshotTracker.StartTracking(textBuffer);

                EditorTextBuffer = textBuffer;
                EditorTextContainer = textBuffer.AsTextContainer();
                EditorTextContainer.TextChanged += TextContainer_Changed;
            }
        }

        public string ProjectFilePath { get; }

        public string DocumentFilePath { get; }

        public bool IsOpenInEditor => EditorTextBuffer != null;

        public SourceTextContainer EditorTextContainer { get; private set; }

        public ITextBuffer EditorTextBuffer { get; private set; }

        public TextLoader TextLoader { get; }

        public void ProcessOpen(ITextBuffer textBuffer)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            _fileTracker.StopListening();

            _snapshotTracker.StartTracking(textBuffer);
            EditorTextBuffer = textBuffer;
            EditorTextContainer = textBuffer.AsTextContainer();
            EditorTextContainer.TextChanged += TextContainer_Changed;

            _opened?.Invoke(this, EventArgs.Empty);
        }

        public void ProcessClose()
        {
            _closed?.Invoke(this, EventArgs.Empty);

            _snapshotTracker.StopTracking(EditorTextBuffer);

            EditorTextContainer.TextChanged -= TextContainer_Changed;
            EditorTextContainer = null;
            EditorTextBuffer = null;
            
            _fileTracker.StartListening();
        }

        private void ChangeTracker_Changed(object sender, FileChangeEventArgs e)
        {
            if (e.Kind == FileChangeKind.Changed)
            {
                _changedOnDisk?.Invoke(this, EventArgs.Empty);
            }
        }

        private void TextContainer_Changed(object sender, TextChangeEventArgs e)
        {
            _changedInEditor?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _fileTracker.Changed -= ChangeTracker_Changed;
                _fileTracker.StopListening();

                if (EditorTextBuffer != null)
                {
                    _snapshotTracker.StopTracking(EditorTextBuffer);
                    EditorTextBuffer = null;
                }

                if (EditorTextContainer != null)
                {
                    EditorTextContainer.TextChanged -= TextContainer_Changed;
                    EditorTextContainer = null;
                }

                _documentManager.RemoveDocument(this);

                _disposed = true;
            }
        }
    }
}
