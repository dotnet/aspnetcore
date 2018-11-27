// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Editor.Razor;

namespace Microsoft.VisualStudio.Mac.LanguageServices.Razor
{
    internal class DefaultFileChangeTrackerFactory : FileChangeTrackerFactory
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;

        public DefaultFileChangeTrackerFactory(ForegroundDispatcher foregroundDispatcher)
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

            var fileChangeTracker = new DefaultFileChangeTracker(filePath, _foregroundDispatcher);
            return fileChangeTracker;
        }
    }
}
