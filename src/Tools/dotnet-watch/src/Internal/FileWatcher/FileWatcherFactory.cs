// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.DotNet.Watcher.Internal
{
    public static class FileWatcherFactory
    {
        public static IFileSystemWatcher CreateWatcher(string watchedDirectory)
            => CreateWatcher(watchedDirectory, CommandLineOptions.IsPollingEnabled);

        public static IFileSystemWatcher CreateWatcher(string watchedDirectory, bool usePollingWatcher)
        {
            return usePollingWatcher ?
                new PollingFileWatcher(watchedDirectory) :
                new DotnetFileWatcher(watchedDirectory) as IFileSystemWatcher;
        }
    }
}
