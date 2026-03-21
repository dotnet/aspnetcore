// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

internal sealed class Http1ContentLengthMessageBody : Http1MessageBody
{
    private ReadResult _readResult;
    private readonly long _contentLength;
    private long _unexaminedInputLength;
    private bool _readCompleted;
    private bool _isReading;
    private int _userCanceled;
    private bool _finalAdvanceCalled;
    private bool _cannotResetInputPipe;

    public Http1ContentLengthMessageBody(Http1Connection context, long contentLength, bool keepAlive)
        : base(context, keepAlive)
    {
        _contentLength = contentLength;
        _unexaminedInputLength = contentLength;
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    public override async ValueTask<ReadResult> ReadAsyncInternal(CancellationToken cancellationToken = default)
    {
        VerifyIsNotReading();

        if (_readCompleted)
        {
            _isReading = true;
            return new ReadResult(_readResult.Buffer, Interlocked.Exchange(ref _userCanceled, 0) == 1, isCompleted: true);
        }

        // The issue is that TryRead can get a canceled read result
        // which is unknown to StartTimingReadAsync.
        if (_context.RequestTimedOut)
        {
            KestrelBadHttpRequestException.Throw(RequestRejectionReason.RequestBodyTimeout);
        }

        await TryStartAsync();

        // The while(true) loop is required because the Http1 connection calls CancelPendingRead to unblock
        // the call to StartTimingReadAsync to check if the request timed out.
        // However, if the user called CancelPendingRead, we want that to return a canceled ReadResult
        // We internally track an int for that.
        while (true)
        {
            try
            {
                var readAwaitable = _context.Input.ReadAsync(cancellationToken);

                _isReading = true;
                _readResult = await StartTimingReadAsync(readAwaitable, cancellationToken);
            }
            catch (ConnectionAbortedException ex)
            {
                _isReading = false;
                throw new TaskCanceledException("The request was aborted", ex);
            }

            void ResetReadingState()
            {
                // Reset the timing read here for the next call to read.
                StopTimingRead(0);

                if (!_cannotResetInputPipe)
                {
                    _isReading = false;
                    _context.Input.AdvanceTo(_readResult.Buffer.Start);
                }
            }

            if (_context.RequestTimedOut)
            {
                ResetReadingState();
                KestrelBadHttpRequestException.Throw(RequestRejectionReason.RequestBodyTimeout);
            }

            if (_readResult.IsCompleted)
            {
                ResetReadingState();
                ThrowUnexpectedEndOfRequestContent();
            }

            // Ignore the canceled readResult if it wasn't canceled by the user.
            // Normally we do not return a canceled ReadResult unless CancelPendingRead was called on the request body PipeReader itself,
            // but if the last call to AdvanceTo examined data it did not consume, we cannot reset the state of the Input pipe.
            // https://github.com/dotnet/aspnetcore/issues/19476
            if (!_readResult.IsCanceled || Interlocked.Exchange(ref _userCanceled, 0) == 1 || _cannotResetInputPipe)
            {
                var returnedReadResultLength = CreateReadResultFromConnectionReadResult();

                // Don't count bytes belonging to the next request, since read rate timeouts are done on a per-request basis.
                StopTimingRead(returnedReadResultLength);

                if (_readResult.IsCompleted)
                {
                    TryStop();
                }

                break;
            }

            ResetReadingState();
        }

        return _readResult;
    }

    public override bool TryReadInternal(out ReadResult readResult)
    {
        VerifyIsNotReading();

        if (_readCompleted)
        {
            _isReading = true;
            readResult = new ReadResult(_readResult.Buffer, Interlocked.Exchange(ref _userCanceled, 0) == 1, isCompleted: true);
            return true;
        }

        if (_context.RequestTimedOut)
        {
            KestrelBadHttpRequestException.Throw(RequestRejectionReason.RequestBodyTimeout);
        }

        TryStartAsync();

        // The while(true) because we don't want to return a canceled ReadResult if the user themselves didn't cancel it.
        while (true)
        {
            if (!_context.Input.TryRead(out _readResult))
            {
                readResult = default;
                return false;
            }

            if (!_readResult.IsCanceled || Interlocked.Exchange(ref _userCanceled, 0) == 1 || _cannotResetInputPipe)
            {
                break;
            }

            _context.Input.AdvanceTo(_readResult.Buffer.Start);
        }

        if (_readResult.IsCompleted)
        {
            if (_cannotResetInputPipe)
            {
                _isReading = true;
            }
            else
            {
                _context.Input.AdvanceTo(_readResult.Buffer.Start);
            }

            ThrowUnexpectedEndOfRequestContent();
        }

        var returnedReadResultLength = CreateReadResultFromConnectionReadResult();

        // Don't count bytes belonging to the next request, since read rate timeouts are done on a per-request basis.
        CountBytesRead(returnedReadResultLength);

        // Only set _isReading if we are returning true.
        _isReading = true;
        readResult = _readResult;

        if (readResult.IsCompleted)
        {
            TryStop();
        }

        return true;
    }

    private long CreateReadResultFromConnectionReadResult()
    {
        var initialLength = _readResult.Buffer.Length;
        var maxLength = _unexaminedInputLength + _examinedUnconsumedBytes;

        if (initialLength < maxLength)
        {
            return initialLength;
        }

        _readCompleted = true;
        _readResult = new ReadResult(
            _readResult.Buffer.Slice(0, maxLength),
            _readResult.IsCanceled,
            isCompleted: true);

        return maxLength;
    }

    public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
    {
        if (!_isReading)
        {
            throw new InvalidOperationException("No reading operation to complete.");
        }

        _isReading = false;

        // The current body is read, though there might be more bytes to read on the stream with pipelining
        if (_readCompleted)
        {
            var buffer = _readResult.Buffer.Slice(consumed, _readResult.Buffer.End);

            if (!_finalAdvanceCalled && buffer.IsEmpty)
            {
                // Don't reference the old buffer as it will be released by the pipe after calling AdvanceTo
                _readResult = new ReadResult(new ReadOnlySequence<byte>(), isCanceled: false, isCompleted: true);

                _context.Input.AdvanceTo(consumed);
                _finalAdvanceCalled = true;
                _context.OnTrailersComplete();
            }
            else
            {
                // If the old stored _readResult was canceled, it's already been observed. Do not store a canceled read result permanently.
                _readResult = new ReadResult(buffer, isCanceled: false, isCompleted: true);
            }

            return;
        }

        // If consumed != examined, we cannot reset _context.Input back to a non-reading state after the next call to ReadAsync
        // simply by calling _context.Input.AdvanceTo(_readResult.Buffer.Start) because the DefaultPipeReader will complain that
        // "The examined position cannot be less than the previously examined position."
        _cannotResetInputPipe = !consumed.Equals(examined);
        _unexaminedInputLength -= TrackConsumedAndExaminedBytes(_readResult, consumed, examined);
        _context.Input.AdvanceTo(consumed, examined);
    }

    protected override void OnReadStarting()
    {
        var maxRequestBodySize = _context.MaxRequestBodySize;
        if (_contentLength > maxRequestBodySize)
        {
            _context.DisableKeepAlive(ConnectionEndReason.MaxRequestBodySizeExceeded);
            KestrelBadHttpRequestException.Throw(RequestRejectionReason.RequestBodyTooLarge, maxRequestBodySize.GetValueOrDefault().ToString(CultureInfo.InvariantCulture));
        }
    }

    public override void CancelPendingRead()
    {
        Interlocked.Exchange(ref _userCanceled, 1);
        _context.Input.CancelPendingRead();
    }

    [StackTraceHidden]
    private void VerifyIsNotReading()
    {
        if (!_isReading)
        {
            return;
        }

        if (_cannotResetInputPipe)
        {
            if (_readResult.IsCompleted)
            {
                KestrelBadHttpRequestException.Throw(RequestRejectionReason.UnexpectedEndOfRequestContent);
            }

            if (_context.RequestTimedOut)
            {
                KestrelBadHttpRequestException.Throw(RequestRejectionReason.RequestBodyTimeout);
            }
        }

        throw new InvalidOperationException("Reading is already in progress.");
    }
}
