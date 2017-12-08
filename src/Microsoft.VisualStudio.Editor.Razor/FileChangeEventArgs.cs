// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Editor.Razor
{
    internal sealed class FileChangeEventArgs : EventArgs
    {
        public FileChangeEventArgs(string filePath, FileChangeKind kind)
        {
            FilePath = filePath;
            Kind = kind;
        }

        public string FilePath { get; }

        public FileChangeKind Kind { get; }
    }
}
