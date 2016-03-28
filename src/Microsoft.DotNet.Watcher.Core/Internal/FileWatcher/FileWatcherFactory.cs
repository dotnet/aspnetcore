// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.DotNet.Watcher.Core.Internal
{
    public static class FileWatcherFactory
    {
        public static IFileSystemWatcher CreateWatcher(string watchedDirectory)
        {
            var envVar = Environment.GetEnvironmentVariable("USE_POLLING_FILE_WATCHER");
            var usePollingWatcher =
                envVar != null &&
                (envVar.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                 envVar.Equals("true", StringComparison.OrdinalIgnoreCase));

            return CreateWatcher(watchedDirectory, usePollingWatcher);
        }

        public static IFileSystemWatcher CreateWatcher(string watchedDirectory, bool usePollingWatcher)
        {
            return usePollingWatcher ? 
                new PollingFileWatcher(watchedDirectory) : 
                new DotnetFileWatcher(watchedDirectory) as IFileSystemWatcher;
        }
    }
}
