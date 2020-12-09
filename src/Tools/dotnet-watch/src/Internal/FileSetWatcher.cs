// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.DotNet.Watcher.Internal
{
    public class FileSetWatcher : IDisposable
    {
        private readonly FileWatcher _fileWatcher;
        private readonly FileSet _fileSet;

        public FileSetWatcher(FileSet fileSet, IReporter reporter)
        {
            Ensure.NotNull(fileSet, nameof(fileSet));

            _fileSet = fileSet;
            _fileWatcher = new FileWatcher(reporter);
        }

        public async Task<FileItem?> GetChangedFileAsync(CancellationToken cancellationToken, Action startedWatching)
        {
            foreach (var file in _fileSet)
            {
                _fileWatcher.WatchDirectory(Path.GetDirectoryName(file.FilePath));
            }

            var tcs = new TaskCompletionSource<FileItem?>();
            cancellationToken.Register(() => tcs.TrySetResult(null));

            void callback(string path)
            {
                if (_fileSet.TryGetValue(path, out var fileItem))
                {
                    tcs.TrySetResult(fileItem);
                }
            }

            _fileWatcher.OnFileChange += callback;
            startedWatching();
            var changedFile = await tcs.Task;
            _fileWatcher.OnFileChange -= callback;

            return changedFile;
        }

        public Task<FileItem?> GetChangedFileAsync(CancellationToken cancellationToken)
        {
            return GetChangedFileAsync(cancellationToken, () => {});
        }

        public void Dispose()
        {
            _fileWatcher.Dispose();
        }
    }
}
