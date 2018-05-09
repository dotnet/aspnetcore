// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Editor.Razor.Documents
{
    // Similar to the DocumentProvider in dotnet/Roslyn - but simplified quite a bit to remove
    // concepts that we don't need. Responsible for providing data about text changes for documents
    // and editor open/closed state.
    internal abstract class EditorDocumentManagerBase : EditorDocumentManager
    {
        private readonly FileChangeTrackerFactory _fileChangeTrackerFactory;
        private readonly ForegroundDispatcher _foregroundDispatcher;

        private readonly Dictionary<DocumentKey, EditorDocument> _documents;
        private readonly Dictionary<string, List<DocumentKey>> _documentsByFilePath;
        protected readonly object _lock;

        public EditorDocumentManagerBase(
            ForegroundDispatcher foregroundDispatcher,
            FileChangeTrackerFactory fileChangeTrackerFactory)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (fileChangeTrackerFactory == null)
            {
                throw new ArgumentNullException(nameof(fileChangeTrackerFactory));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _fileChangeTrackerFactory = fileChangeTrackerFactory;

            _documents = new Dictionary<DocumentKey, EditorDocument>();
            _documentsByFilePath = new Dictionary<string, List<DocumentKey>>(FilePathComparer.Instance);
            _lock = new object();
        }

        protected ForegroundDispatcher ForegroundDispatcher => _foregroundDispatcher;

        protected abstract ITextBuffer GetTextBufferForOpenDocument(string filePath);

        protected abstract void OnDocumentOpened(EditorDocument document);

        protected abstract void OnDocumentClosed(EditorDocument document);

        public sealed override bool TryGetDocument(DocumentKey key, out EditorDocument document)
        {
            _foregroundDispatcher.AssertForegroundThread();

            lock (_lock)
            {
                return _documents.TryGetValue(key, out document);
            }
        }

        public sealed override bool TryGetMatchingDocuments(string filePath, out EditorDocument[] documents)
        {
            _foregroundDispatcher.AssertForegroundThread();

            lock (_lock)
            {
                if (!_documentsByFilePath.TryGetValue(filePath, out var keys))
                {
                    documents = null;
                    return false;
                }

                documents = new EditorDocument[keys.Count];
                for (var i = 0; i < keys.Count; i++)
                {
                    documents[i] = _documents[keys[i]];
                }

                return true;
            }
        }

        public sealed override EditorDocument GetOrCreateDocument(
            DocumentKey key,
            EventHandler changedOnDisk,
            EventHandler changedInEditor,
            EventHandler opened,
            EventHandler closed)
        {
            _foregroundDispatcher.AssertForegroundThread();

            EditorDocument document;

            lock (_lock)
            {
                if (TryGetDocument(key, out document))
                {
                    return document;
                }

                // Check if the document is already open and initialized, and associate a buffer if possible.
                var textBuffer = GetTextBufferForOpenDocument(key.DocumentFilePath);
                document = new EditorDocument(
                    this,
                    key.ProjectFilePath,
                    key.DocumentFilePath,
                    new FileTextLoader(key.DocumentFilePath, defaultEncoding: null),
                    _fileChangeTrackerFactory.Create(key.DocumentFilePath),
                    textBuffer,
                    changedOnDisk,
                    changedInEditor,
                    opened,
                    closed);

                _documents.Add(key, document);

                if (!_documentsByFilePath.TryGetValue(key.DocumentFilePath, out var documents))
                {
                    documents = new List<DocumentKey>();
                    _documentsByFilePath.Add(key.DocumentFilePath, documents);
                }

                if (!documents.Contains(key))
                {
                    documents.Add(key);
                }

                if (document.IsOpenInEditor)
                {
                    OnDocumentOpened(document);
                }

                return document;
            }
        }

        protected void DocumentOpened(string filePath, ITextBuffer textBuffer)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (textBuffer == null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            _foregroundDispatcher.AssertForegroundThread();

            lock (_lock)
            {
                if (TryGetMatchingDocuments(filePath, out var documents))
                {
                    for (var i = 0; i < documents.Length; i++)
                    {
                        var document = documents[i];

                        document.ProcessOpen(textBuffer);
                        OnDocumentOpened(document);
                    }
                }
            }
        }

        protected void DocumentClosed(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            _foregroundDispatcher.AssertForegroundThread();

            lock (_lock)
            {
                if (TryGetMatchingDocuments(filePath, out var documents))
                {
                    for (var i = 0; i < documents.Length; i++)
                    {
                        var document = documents[i];

                        document.ProcessClose();
                        OnDocumentClosed(document);
                    }
                }
            }
        }

        public sealed override void RemoveDocument(EditorDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            _foregroundDispatcher.AssertForegroundThread();

            var key = new DocumentKey(document.ProjectFilePath, document.DocumentFilePath);
            if (_documentsByFilePath.TryGetValue(document.DocumentFilePath, out var documents))
            {
                documents.Remove(key);

                if (documents.Count == 0)
                {
                    _documentsByFilePath.Remove(document.DocumentFilePath);
                }
            }

            _documents.Remove(key);

            if (document.IsOpenInEditor)
            {
                OnDocumentClosed(document);
            }
        }
    }
}
