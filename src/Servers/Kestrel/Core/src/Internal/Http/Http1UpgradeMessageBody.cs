// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

/// <summary>
/// The upgrade stream uses the raw connection stream instead of going through the RequestBodyPipe. This
/// removes the redundant copy from the transport pipe to the body pipe.
/// </summary>
internal sealed class Http1UpgradeMessageBody : Http1MessageBody
{
    private int _userCanceled;

    public Http1UpgradeMessageBody(Http1Connection context, bool keepAlive)
        : base(context, keepAlive)
    {
        RequestUpgrade = true;
    }

    // This returns IsEmpty so we can avoid draining the body (since it's basically an endless stream)
    public override bool IsEmpty => true;

    public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfReaderCompleted();
        return ReadAsyncInternal(cancellationToken);
    }

    public override bool TryRead(out ReadResult result)
    {
        ThrowIfReaderCompleted();
        return TryReadInternal(out result);
    }

    public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
    {
        _context.Input.AdvanceTo(consumed, examined);
    }

    public override void CancelPendingRead()
    {
        Interlocked.Exchange(ref _userCanceled, 1);
        _context.Input.CancelPendingRead();
    }

    public override Task ConsumeAsync()
    {
        return Task.CompletedTask;
    }

    public override ValueTask StopAsync()
    {
        return default;
    }

    public override bool TryReadInternal(out ReadResult readResult)
    {
        // Ignore the canceled readResult unless it was canceled by the user.
        do
        {
            if (!_context.Input.TryRead(out readResult))
            {
                return false;
            }
        } while (readResult.IsCanceled && Interlocked.Exchange(ref _userCanceled, 0) == 0);

        return true;
    }

    public override ValueTask<ReadResult> ReadAsyncInternal(CancellationToken cancellationToken = default)
    {
        ReadResult readResult;

        // Ignore the canceled readResult unless it was canceled by the user.
        do
        {
            var readTask = _context.Input.ReadAsync(cancellationToken);

            if (!readTask.IsCompletedSuccessfully)
            {
                return ReadAsyncInternalAwaited(readTask, cancellationToken);
            }

            readResult = readTask.GetAwaiter().GetResult();
        } while (readResult.IsCanceled && Interlocked.Exchange(ref _userCanceled, 0) == 0);

        return new ValueTask<ReadResult>(readResult);
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    private async ValueTask<ReadResult> ReadAsyncInternalAwaited(ValueTask<ReadResult> readTask, CancellationToken cancellationToken = default)
    {
        var readResult = await readTask;

        // Ignore the canceled readResult unless it was canceled by the user.
        while (readResult.IsCanceled && Interlocked.Exchange(ref _userCanceled, 0) == 0)
        {
            readResult = await _context.Input.ReadAsync(cancellationToken);
        }

        return readResult;
    }
}
