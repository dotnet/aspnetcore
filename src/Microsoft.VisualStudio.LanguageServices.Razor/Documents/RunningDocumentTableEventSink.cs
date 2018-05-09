// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.Editor.Razor.Documents
{
    internal class RunningDocumentTableEventSink : IVsRunningDocTableEvents3
    {
        private readonly VisualStudioEditorDocumentManager _documentManager;

        public RunningDocumentTableEventSink(VisualStudioEditorDocumentManager documentManager)
        {
            if (documentManager == null)
            {
                throw new ArgumentNullException(nameof(documentManager));
            }

            _documentManager = documentManager;
        }

        public int OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld, string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
        {
            // Document has been initialized.
            if ((grfAttribs & (uint)__VSRDTATTRIB3.RDTA_DocumentInitialized) != 0)
            {
                _documentManager.DocumentOpened(docCookie);
            }

            if ((grfAttribs & (uint)__VSRDTATTRIB.RDTA_MkDocument) != 0)
            {
                _documentManager.DocumentRenamed(docCookie, pszMkDocumentOld, pszMkDocumentNew);
            }

            return VSConstants.S_OK;
        }

        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            // Document is being closed
            if (dwReadLocksRemaining + dwEditLocksRemaining == 0)
            {
                _documentManager.DocumentClosed(docCookie);
            }

            return VSConstants.S_OK;
        }

        public int OnBeforeSave(uint docCookie) => VSConstants.S_OK;

        public int OnAfterSave(uint docCookie) => VSConstants.S_OK;

        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs) => VSConstants.S_OK;

        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame) => VSConstants.S_OK;

        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame) => VSConstants.S_OK;

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;
    }
}
