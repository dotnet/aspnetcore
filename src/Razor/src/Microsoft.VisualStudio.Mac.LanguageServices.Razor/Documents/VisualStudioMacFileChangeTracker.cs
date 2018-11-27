// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor;
using MonoDevelop.Core;

namespace Microsoft.VisualStudio.Editor.Razor.Documents
{
    internal class VisualStudioMacFileChangeTracker : FileChangeTracker
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly string _normalizedFilePath;
        private bool _listening;

        public override event EventHandler<FileChangeEventArgs> Changed;

        public VisualStudioMacFileChangeTracker(
            string filePath,
            ForegroundDispatcher foregroundDispatcher)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(filePath));
            }

            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            FilePath = filePath;
            _normalizedFilePath = NormalizePath(FilePath);
            _foregroundDispatcher = foregroundDispatcher;
        }

        public override string FilePath { get; }

        public override void StartListening()
        {
            _foregroundDispatcher.AssertForegroundThread();

            if (_listening)
            {
                return;
            }

            AttachToFileServiceEvents();

            _listening = true;
        }

        public override void StopListening()
        {
            _foregroundDispatcher.AssertForegroundThread();

            if (!_listening)
            {
                return;
            }

            DetachFromFileServiceEvents();

            _listening = false;
        }

        // Virtual for testing
        protected virtual void AttachToFileServiceEvents()
        {
            FileService.FileChanged += FileService_FileChanged;
            FileService.FileCreated += FileService_FileCreated;
            FileService.FileRemoved += FileService_FileRemoved;
        }

        // Virtual for testing
        protected virtual void DetachFromFileServiceEvents()
        {
            FileService.FileChanged -= FileService_FileChanged;
            FileService.FileCreated -= FileService_FileCreated;
            FileService.FileRemoved -= FileService_FileRemoved;
        }

        private void FileService_FileChanged(object sender, FileEventArgs args) => HandleFileChangeEvent(FileChangeKind.Changed, args);

        private void FileService_FileCreated(object sender, FileEventArgs args) => HandleFileChangeEvent(FileChangeKind.Added, args);

        private void FileService_FileRemoved(object sender, FileEventArgs args) => HandleFileChangeEvent(FileChangeKind.Removed, args);

        private void HandleFileChangeEvent(FileChangeKind changeKind, FileEventArgs args)
        {
            _foregroundDispatcher.AssertForegroundThread();

            if (Changed == null)
            {
                return;
            }

            foreach (var fileEvent in args)
            {
                if (fileEvent.IsDirectory)
                {
                    continue;
                }

                var normalizedEventPath = NormalizePath(fileEvent.FileName.FullPath);
                if (string.Equals(_normalizedFilePath, normalizedEventPath, StringComparison.OrdinalIgnoreCase))
                {
                    OnChanged(changeKind);
                    return;
                }
            }
        }

        private void OnChanged(FileChangeKind changeKind)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var args = new FileChangeEventArgs(FilePath, changeKind);
            Changed?.Invoke(this, args);
        }

        private static string NormalizePath(string path)
        {
            path = path.Replace('\\', '/');

            return path;
        }
    }
}
