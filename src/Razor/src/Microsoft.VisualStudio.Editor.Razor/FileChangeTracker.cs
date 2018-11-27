// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Editor.Razor
{
    internal abstract class FileChangeTracker
    {
        public abstract event EventHandler<FileChangeEventArgs> Changed;

        public abstract string FilePath { get; }

        public abstract void StartListening();

        public abstract void StopListening();
    }
}
