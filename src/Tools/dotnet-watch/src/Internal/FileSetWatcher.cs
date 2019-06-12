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
        private readonly IFileSet _fileSet;

        public FileSetWatcher(IFileSet fileSet, IReporter reporter)
        {
            Ensure.NotNull(fileSet, nameof(fileSet));

            _fileSet = fileSet;
            _fileWatcher = new FileWatcher(reporter);
        }

        public async Task<string> GetChangedFileAsync(CancellationToken cancellationToken, Action startedWatching)
        {
            foreach (var file in _fileSet)
            {
                _fileWatcher.WatchDirectory(Path.GetDirectoryName(file));
            }

            var tcs = new TaskCompletionSource<string>();
            cancellationToken.Register(() => tcs.TrySetResult(null));

            Action<string> callback = path =>
            {
                if (_fileSet.Contains(path))
                {
                    tcs.TrySetResult(path);
                }
            };

            _fileWatcher.OnFileChange += callback;
            startedWatching();
            var changedFile = await tcs.Task;
            _fileWatcher.OnFileChange -= callback;

            return changedFile;
        }


        public Task<string> GetChangedFileAsync(CancellationToken cancellationToken)
        {
            return GetChangedFileAsync(cancellationToken, () => {});
        }

        public void Dispose()
        {
            _fileWatcher.Dispose();
        }
    }
}
