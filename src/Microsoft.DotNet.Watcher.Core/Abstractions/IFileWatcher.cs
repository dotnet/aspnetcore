// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.DotNet.Watcher.Core
{
    public interface IFileWatcher : IDisposable
    {
        event Action<string> OnChanged;

        void WatchDirectory(string path, string extension);

        bool WatchFile(string path);

        void WatchProject(string path);
    }
}
