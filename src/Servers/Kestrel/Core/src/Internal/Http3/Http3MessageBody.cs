// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

internal sealed class Http3MessageBody : MessageBody
{
    private readonly Http3Stream _context;
    private ReadResult _readResult;

    public Http3MessageBody(Http3Stream context)
        : base(context)
    {
        _context = context;
        ExtendedConnect = _context.IsExtendedConnectRequest;
    }

    protected override void OnReadStarting()
    {
        // Note ContentLength or MaxRequestBodySize may be null
        var maxRequestBodySize = _context.MaxRequestBodySize;

        if (_context.RequestHeaders.ContentLength > maxRequestBodySize)
        {
            KestrelBadHttpRequestException.Throw(RequestRejectionReason.RequestBodyTooLarge, maxRequestBodySize.GetValueOrDefault().ToString(CultureInfo.InvariantCulture));
        }
    }

    public override void Reset()
    {
        base.Reset();
        _readResult = default;
        ExtendedConnect = _context.IsExtendedConnectRequest;
    }

    public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
    {
        var newlyExaminedBytes = TrackConsumedAndExaminedBytes(_readResult, consumed, examined);

        _context.RequestBodyPipe.Reader.AdvanceTo(consumed, examined);

        AddAndCheckObservedBytes(newlyExaminedBytes);
    }

    public override bool TryRead(out ReadResult readResult)
    {
        TryStartAsync();

        var hasResult = _context.RequestBodyPipe.Reader.TryRead(out readResult);

        if (hasResult)
        {
            _readResult = readResult;

            CountBytesRead(readResult.Buffer.Length);

            if (readResult.IsCompleted)
            {
                TryStop();
            }
        }

        return hasResult;
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    public override async ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
    {
        await TryStartAsync();

        try
        {
            var readAwaitable = _context.RequestBodyPipe.Reader.ReadAsync(cancellationToken);

            _readResult = await StartTimingReadAsync(readAwaitable, cancellationToken);
        }
        catch (ConnectionAbortedException ex)
        {
            throw new TaskCanceledException("The request was aborted", ex);
        }

        StopTimingRead(_readResult.Buffer.Length);

        if (_readResult.IsCompleted)
        {
            TryStop();
        }

        return _readResult;
    }

    public override void Complete(Exception? exception)
    {
        _context.ReportApplicationError(exception);
        _context.RequestBodyPipe.Reader.Complete();
    }

    public override ValueTask CompleteAsync(Exception? exception)
    {
        _context.ReportApplicationError(exception);
        return _context.RequestBodyPipe.Reader.CompleteAsync();
    }

    public override void CancelPendingRead()
    {
        _context.RequestBodyPipe.Reader.CancelPendingRead();
    }

    protected override ValueTask OnStopAsync()
    {
        if (!_context.HasStartedConsumingRequestBody)
        {
            return default;
        }

        _context.RequestBodyPipe.Reader.Complete();

        return default;
    }
}
