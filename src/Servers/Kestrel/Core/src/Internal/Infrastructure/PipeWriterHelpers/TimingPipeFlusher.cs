// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.IO.Pipelines;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.PipeWriterHelpers;

/// <summary>
/// This wraps PipeWriter.FlushAsync() in a way that allows multiple awaiters making it safe to call from publicly
/// exposed Stream implementations while also tracking response data rate.
/// </summary>
internal sealed class TimingPipeFlusher
{
    private PipeWriter _writer = default!;
    private readonly ITimeoutControl? _timeoutControl;
    private readonly KestrelTrace _log;

    public TimingPipeFlusher(
        ITimeoutControl? timeoutControl,
        KestrelTrace log)
    {
        _timeoutControl = timeoutControl;
        _log = log;
    }

    public void Initialize(PipeWriter output)
    {
        _writer = output;
    }

    public ValueTask<FlushResult> FlushAsync()
    {
        return FlushAsync(outputAborter: null, cancellationToken: default);
    }

    public ValueTask<FlushResult> FlushAsync(IHttpOutputAborter? outputAborter, CancellationToken cancellationToken)
    {
        return FlushAsync(minRate: null, count: 0, outputAborter: outputAborter, cancellationToken: cancellationToken);
    }

    public ValueTask<FlushResult> FlushAsync(MinDataRate? minRate, long count)
    {
        return FlushAsync(minRate, count, outputAborter: null, cancellationToken: default);
    }

    public ValueTask<FlushResult> FlushAsync(MinDataRate? minRate, long count, IHttpOutputAborter? outputAborter, CancellationToken cancellationToken)
    {
        if (minRate is object)
        {
            // Call BytesWrittenToBuffer before FlushAsync() to make testing easier, otherwise the Flush can cause test code to run before the timeout
            // control updates and if the test checks for a timeout it can fail
            _timeoutControl!.BytesWrittenToBuffer(minRate, count);
        }

        var pipeFlushTask = _writer.FlushAsync(cancellationToken);

        if (pipeFlushTask.IsCompletedSuccessfully)
        {
            var flushResult = pipeFlushTask.Result;

            if (flushResult.IsCompleted && outputAborter is object)
            {
                outputAborter.OnInputOrOutputCompleted();
            }

            return new ValueTask<FlushResult>(flushResult);
        }

        return TimeFlushAsyncAwaited(pipeFlushTask, minRate, outputAborter, cancellationToken);
    }

    private async ValueTask<FlushResult> TimeFlushAsyncAwaited(ValueTask<FlushResult> pipeFlushTask, MinDataRate? minRate, IHttpOutputAborter? outputAborter, CancellationToken cancellationToken)
    {
        if (minRate is object)
        {
            _timeoutControl!.StartTimingWrite();
        }

        try
        {
            var flushResult = await pipeFlushTask;

            if (flushResult.IsCompleted && outputAborter is object)
            {
                outputAborter.OnInputOrOutputCompleted();
            }
        }
        catch (OperationCanceledException ex) when (outputAborter is object)
        {
            outputAborter.Abort(new ConnectionAbortedException(CoreStrings.ConnectionOrStreamAbortedByCancellationToken, ex), ConnectionEndReason.WriteCanceled);
        }
        catch (Exception ex)
        {
            // A canceled token is the only reason flush should ever throw.
            _log.LogError(0, ex, $"Unexpected exception in {nameof(TimingPipeFlusher)}.{nameof(FlushAsync)}.");
        }
        finally
        {
            if (minRate is object)
            {
                _timeoutControl!.StopTimingWrite();
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        return default;
    }
}
