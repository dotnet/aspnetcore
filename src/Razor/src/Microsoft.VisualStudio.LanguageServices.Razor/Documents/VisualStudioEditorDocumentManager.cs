// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.Editor.Razor.Documents
{
    // Similar to the DocumentProvider in dotnet/Roslyn - but simplified quite a bit to remove
    // concepts that we don't need. Responsible for providing data about text changes for documents
    // and editor open/closed state.
    internal class VisualStudioEditorDocumentManager : EditorDocumentManagerBase
    {
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactory;

        private readonly IVsRunningDocumentTable4 _runningDocumentTable;
        private readonly uint _rdtCookie;

        private readonly Dictionary<uint, List<DocumentKey>> _documentsByCookie;
        private readonly Dictionary<DocumentKey, uint> _cookiesByDocument;

        public VisualStudioEditorDocumentManager(
            ForegroundDispatcher foregroundDispatcher,
            FileChangeTrackerFactory fileChangeTrackerFactory,
            IVsRunningDocumentTable runningDocumentTable,
            IVsEditorAdaptersFactoryService editorAdaptersFactory)
            : base(foregroundDispatcher, fileChangeTrackerFactory)
        {
            if (runningDocumentTable == null)
            {
                throw new ArgumentNullException(nameof(runningDocumentTable));
            }

            if (editorAdaptersFactory == null)
            {
                throw new ArgumentNullException(nameof(editorAdaptersFactory));
            }

            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (fileChangeTrackerFactory == null)
            {
                throw new ArgumentNullException(nameof(fileChangeTrackerFactory));
            }

            _runningDocumentTable = (IVsRunningDocumentTable4)runningDocumentTable;
            _editorAdaptersFactory = editorAdaptersFactory;
            
            var hr = runningDocumentTable.AdviseRunningDocTableEvents(new RunningDocumentTableEventSink(this), out _rdtCookie);
            Marshal.ThrowExceptionForHR(hr);

            _documentsByCookie = new Dictionary<uint, List<DocumentKey>>();
            _cookiesByDocument = new Dictionary<DocumentKey, uint>();
        }

        protected override ITextBuffer GetTextBufferForOpenDocument(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            // Check if the document is already open and initialized, and associate a buffer if possible.
            var cookie = VSConstants.VSCOOKIE_NIL;
            ITextBuffer textBuffer = null;
            if (_runningDocumentTable.IsMonikerValid(filePath) &&
                ((cookie = _runningDocumentTable.GetDocumentCookie(filePath)) != VSConstants.VSCOOKIE_NIL) &&
                (_runningDocumentTable.GetDocumentFlags(cookie) & (uint)_VSRDTFLAGS4.RDT_PendingInitialization) == 0)
            {
                var vsTextBuffer = ((object)_runningDocumentTable.GetDocumentData(cookie)) as VsTextBuffer;
                textBuffer = vsTextBuffer == null ? null : _editorAdaptersFactory.GetDocumentBuffer(vsTextBuffer);
                return textBuffer;
            }

            return null;
        }

        protected override void OnDocumentOpened(EditorDocument document)
        {
            var cookie = _runningDocumentTable.GetDocumentCookie(document.DocumentFilePath);
            if (cookie != VSConstants.VSCOOKIE_NIL)
            {
                TrackOpenDocument(cookie, new DocumentKey(document.ProjectFilePath, document.DocumentFilePath));
            }
        }

        protected override void OnDocumentClosed(EditorDocument document)
        {
            var key = new DocumentKey(document.ProjectFilePath, document.DocumentFilePath);
            if (_cookiesByDocument.TryGetValue(key, out var cookie))
            {
                UntrackOpenDocument(cookie, key);
            }
        }

        public void DocumentOpened(uint cookie)
        {
            ForegroundDispatcher.AssertForegroundThread();

            lock (_lock)
            {
                // Casts avoid dynamic
                if ((object)(_runningDocumentTable.GetDocumentData(cookie)) is IVsTextBuffer vsTextBuffer)
                {
                    var filePath = _runningDocumentTable.GetDocumentMoniker(cookie);
                    if (!TryGetMatchingDocuments(filePath, out var documents))
                    {
                        // This isn't a document that we're interesting in.
                        return;
                    }

                    var textBuffer = _editorAdaptersFactory.GetDataBuffer(vsTextBuffer);
                    if (textBuffer == null)
                    {
                        // The text buffer has not been created yet, register to be notified when it is.
                        VsTextBufferDataEventsSink.Subscribe(vsTextBuffer, () =>
                        {
                            BufferLoaded(vsTextBuffer, filePath);
                        });

                        return;
                    }

                    // It's possible that events could be fired out of order and that this is a rename.
                    if (_documentsByCookie.ContainsKey(cookie))
                    {
                        DocumentClosed(cookie, exceptFilePath: filePath);
                    }

                    BufferLoaded(textBuffer, filePath, documents);
                }
            }
        }

        public void BufferLoaded(IVsTextBuffer vsTextBuffer, string filePath)
        {
            ForegroundDispatcher.AssertForegroundThread();
            
            var textBuffer = _editorAdaptersFactory.GetDocumentBuffer(vsTextBuffer);
            if (textBuffer != null)
            {
                // We potentially waited for the editor to initialize on this code path, so requery
                // the documents.
                if (TryGetMatchingDocuments(filePath, out var documents))
                {
                    BufferLoaded(textBuffer, filePath, documents);
                }
            }
        }

        public void BufferLoaded(ITextBuffer textBuffer, string filePath, EditorDocument[] documents)
        {
            ForegroundDispatcher.AssertForegroundThread();

            lock (_lock)
            {
                for (var i = 0; i < documents.Length; i++)
                {
                    DocumentOpened(filePath, textBuffer);
                }
            }
        }

        public void DocumentClosed(uint cookie, string exceptFilePath = null)
        {
            ForegroundDispatcher.AssertForegroundThread();

            lock (_lock)
            {
                if (!_documentsByCookie.TryGetValue(cookie, out var documents))
                {
                    return;
                }

                // We have to deal with some complications here due to renames and event ordering and such.
                // We we might see multiple documents open for a cookie (due to linked files), but only one of them 
                // has been renamed. In that case, we just process the change that we know about.
                var filePaths = new HashSet<string>(documents.Select(d => d.DocumentFilePath));
                filePaths.Remove(exceptFilePath);

                foreach (var filePath in filePaths)
                {
                    DocumentClosed(filePath);
                }
            }
        }

        public void DocumentRenamed(uint cookie, string fromFilePath, string toFilePath)
        {
            ForegroundDispatcher.AssertForegroundThread();

            // Ignore changes is casing
            if (FilePathComparer.Instance.Equals(fromFilePath, toFilePath))
            {
                return;
            }

            lock (_lock)
            {
                // Treat a rename as a close + reopen.
                //
                // Due to ordering issues, we could see a partial rename. This is why we need to pass the new
                // file path here.
                DocumentClosed(cookie, exceptFilePath: toFilePath);
            }

            // Try to open any existing documents that match the new name.
            if ((_runningDocumentTable.GetDocumentFlags(cookie) & (uint)_VSRDTFLAGS4.RDT_PendingInitialization) == 0)
            {
                DocumentOpened(cookie);
            }
        }

        private void TrackOpenDocument(uint cookie, DocumentKey key)
        {
            if (!_documentsByCookie.TryGetValue(cookie, out var documents))
            {
                documents = new List<DocumentKey>();
                _documentsByCookie.Add(cookie, documents);
            }

            if (!documents.Contains(key))
            {
                documents.Add(key);
            }

            _cookiesByDocument[key] = cookie;
        }

        private void UntrackOpenDocument(uint cookie, DocumentKey key)
        {
            if (_documentsByCookie.TryGetValue(cookie, out var documents))
            {
                documents.Remove(key);

                if (documents.Count == 0)
                {
                    _documentsByCookie.Remove(cookie);
                }
            }

            _cookiesByDocument.Remove(key);
        }
    }
}
