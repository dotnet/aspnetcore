// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.DotNet.Watcher
{
    public readonly struct FileItem
    {
        public FileItem(string filePath, FileKind fileKind = FileKind.Default, string staticWebAssetPath = null)
        {
            FilePath = filePath;
            FileKind = fileKind;
            StaticWebAssetPath = staticWebAssetPath;
        }

        public string FilePath { get; }

        public FileKind FileKind { get; }

        public string StaticWebAssetPath { get; }
    }
}
