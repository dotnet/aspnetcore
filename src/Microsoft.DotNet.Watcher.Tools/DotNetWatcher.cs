// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Watcher.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watcher
{
    public class DotNetWatcher
    {
        private readonly ILogger _logger;
        private readonly ProcessRunner _processRunner;

        public DotNetWatcher(ILogger logger)
        {
            Ensure.NotNull(logger, nameof(logger));

            _logger = logger;
            _processRunner = new ProcessRunner(logger);
        }

        public async Task WatchAsync(ProcessSpec processSpec, IFileSetFactory fileSetFactory, CancellationToken cancellationToken)
        {
            Ensure.NotNull(processSpec, nameof(processSpec));

            var cancelledTaskSource = new TaskCompletionSource<object>();
            cancellationToken.Register(state => ((TaskCompletionSource<object>)state).TrySetResult(null), cancelledTaskSource);

            while (true)
            {
                var fileSet = await fileSetFactory.CreateAsync(cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                using (var currentRunCancellationSource = new CancellationTokenSource())
                using (var combinedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    currentRunCancellationSource.Token))
                using (var fileSetWatcher = new FileSetWatcher(fileSet))
                {
                    var fileSetTask = fileSetWatcher.GetChangedFileAsync(combinedCancellationSource.Token);
                    var processTask = _processRunner.RunAsync(processSpec, combinedCancellationSource.Token);

                    var finishedTask = await Task.WhenAny(processTask, fileSetTask, cancelledTaskSource.Task);

                    // Regardless of the which task finished first, make sure everything is cancelled
                    // and wait for dotnet to exit. We don't want orphan processes
                    currentRunCancellationSource.Cancel();

                    await Task.WhenAll(processTask, fileSetTask);

                    if (finishedTask == cancelledTaskSource.Task || cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    if (finishedTask == processTask)
                    {
                        _logger.LogInformation("Waiting for a file to change before restarting dotnet...");

                        // Now wait for a file to change before restarting process
                        await fileSetWatcher.GetChangedFileAsync(cancellationToken);
                    }

                    if (!string.IsNullOrEmpty(fileSetTask.Result))
                    {
                        _logger.LogInformation($"File changed: {fileSetTask.Result}");
                    }
                }
            }
        }
    }
}
