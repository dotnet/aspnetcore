// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.Editor.Razor.Documents
{
    internal class VisualStudioFileChangeTrackerFactory : FileChangeTrackerFactory
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly ErrorReporter _errorReporter;
        private readonly IVsFileChangeEx _fileChangeService;

        public VisualStudioFileChangeTrackerFactory(
            ForegroundDispatcher foregroundDispatcher,
            ErrorReporter errorReporter, 
            IVsFileChangeEx fileChangeService)
        {
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

            _foregroundDispatcher = foregroundDispatcher;
            _errorReporter = errorReporter;
            _fileChangeService = fileChangeService;
        }

        public override FileChangeTracker Create(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(filePath));
            }

            var fileChangeTracker = new VisualStudioFileChangeTracker(filePath, _foregroundDispatcher, _errorReporter, _fileChangeService);
            return fileChangeTracker;
        }
    }
}
