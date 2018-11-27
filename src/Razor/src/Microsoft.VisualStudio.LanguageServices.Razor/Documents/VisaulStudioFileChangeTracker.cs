// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.Editor.Razor.Documents
{
    internal class VisualStudioFileChangeTracker : FileChangeTracker, IVsFileChangeEvents
    {
        private const uint FileChangeFlags = (uint)(_VSFILECHANGEFLAGS.VSFILECHG_Time | _VSFILECHANGEFLAGS.VSFILECHG_Size | _VSFILECHANGEFLAGS.VSFILECHG_Del | _VSFILECHANGEFLAGS.VSFILECHG_Add);

        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly ErrorReporter _errorReporter;
        private readonly IVsFileChangeEx _fileChangeService;
        private uint _fileChangeCookie;

        public override event EventHandler<FileChangeEventArgs> Changed;

        public VisualStudioFileChangeTracker(
            string filePath,
            ForegroundDispatcher foregroundDispatcher,
            ErrorReporter errorReporter,
            IVsFileChangeEx fileChangeService)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(filePath));
            }

            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (errorReporter == null)
            {
                throw new ArgumentNullException(nameof(errorReporter));
            }

            if (fileChangeService == null)
            {
                throw new ArgumentNullException(nameof(fileChangeService));
            }

            FilePath = filePath;
            _foregroundDispatcher = foregroundDispatcher;
            _errorReporter = errorReporter;
            _fileChangeService = fileChangeService;
            _fileChangeCookie = VSConstants.VSCOOKIE_NIL;
        }

        public override string FilePath { get; }

        public override void StartListening()
        {
            _foregroundDispatcher.AssertForegroundThread();

            try
            {
                if (_fileChangeCookie == VSConstants.VSCOOKIE_NIL)
                {
                    var hr = _fileChangeService.AdviseFileChange(
                        FilePath,
                        FileChangeFlags,
                        this,
                        out _fileChangeCookie);

                    Marshal.ThrowExceptionForHR(hr);
                }
            }
            catch (Exception exception)
            {
                _errorReporter.ReportError(exception);
            }
        }

        public override void StopListening()
        {
            _foregroundDispatcher.AssertForegroundThread();

            try
            {
                if (_fileChangeCookie != VSConstants.VSCOOKIE_NIL)
                {
                    var hr = _fileChangeService.UnadviseFileChange(_fileChangeCookie);
                    Marshal.ThrowExceptionForHR(hr);
                    _fileChangeCookie = VSConstants.VSCOOKIE_NIL;
                }
            }
            catch (Exception exception)
            {
                _errorReporter.ReportError(exception);
            }
        }

        public int FilesChanged(uint fileCount, string[] filePaths, uint[] fileChangeFlags)
        {
            _foregroundDispatcher.AssertForegroundThread();

            foreach (var fileChangeFlag in fileChangeFlags)
            {
                var fileChangeKind = FileChangeKind.Changed;
                var changeFlag = (_VSFILECHANGEFLAGS)fileChangeFlag;
                if ((changeFlag & _VSFILECHANGEFLAGS.VSFILECHG_Del) == _VSFILECHANGEFLAGS.VSFILECHG_Del)
                {
                    fileChangeKind = FileChangeKind.Removed;
                }
                else if ((changeFlag & _VSFILECHANGEFLAGS.VSFILECHG_Add) == _VSFILECHANGEFLAGS.VSFILECHG_Add)
                {
                    fileChangeKind = FileChangeKind.Added;
                }

                // Purposefully not passing through the file paths here because we know this change has to do with this trackers FilePath.
                // We use that FilePath instead so any path normalization the file service did does not impact callers.
                OnChanged(fileChangeKind);
            }

            return VSConstants.S_OK;
        }

        public int DirectoryChanged(string pszDirectory) => VSConstants.S_OK;

        private void OnChanged(FileChangeKind changeKind)
        {
            _foregroundDispatcher.AssertForegroundThread();

            if (Changed == null)
            {
                return;
            }

            var args = new FileChangeEventArgs(FilePath, changeKind);
            Changed.Invoke(this, args);
        }
    }
}
