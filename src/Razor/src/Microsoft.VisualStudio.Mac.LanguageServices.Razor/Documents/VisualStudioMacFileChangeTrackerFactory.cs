// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.VisualStudio.Editor.Razor.Documents
{
    internal class VisualStudioMacFileChangeTrackerFactory : FileChangeTrackerFactory
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;

        public VisualStudioMacFileChangeTrackerFactory(ForegroundDispatcher foregroundDispatcher)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            _foregroundDispatcher = foregroundDispatcher;
        }

        public override FileChangeTracker Create(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(filePath));
            }

            var fileChangeTracker = new VisualStudioMacFileChangeTracker(filePath, _foregroundDispatcher);
            return fileChangeTracker;
        }
    }
}
