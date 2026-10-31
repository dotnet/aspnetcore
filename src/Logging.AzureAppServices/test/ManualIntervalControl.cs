// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace Microsoft.Extensions.Logging.AzureAppServices.Test;

internal class ManualIntervalControl
{

    private TaskCompletionSource<object> _pauseCompletionSource = new TaskCompletionSource<object>();
    private TaskCompletionSource<object> _resumeCompletionSource;

    public Task Pause => _pauseCompletionSource.Task;

    public void Resume()
    {
        _pauseCompletionSource = new TaskCompletionSource<object>();
        _resumeCompletionSource.SetResult(null);
    }

    public async Task IntervalAsync()
    {
        _resumeCompletionSource = new TaskCompletionSource<object>();
        _pauseCompletionSource.SetResult(null);

        await _resumeCompletionSource.Task;
    }
}
