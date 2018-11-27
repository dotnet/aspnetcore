// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.Editor.Razor
{
    internal abstract class ImportDocumentManager : ILanguageService
    {
        public abstract event EventHandler<ImportChangedEventArgs> Changed;

        public abstract void OnSubscribed(VisualStudioDocumentTracker tracker);

        public abstract void OnUnsubscribed(VisualStudioDocumentTracker tracker);
    }
}
