// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.DotNet.Watcher.Internal
{
    public interface IFileSystemWatcher : IDisposable
    {
        event EventHandler<string> OnFileChange;

        event EventHandler OnError;

        bool EnableRaisingEvents { get; set; }
    }
}
