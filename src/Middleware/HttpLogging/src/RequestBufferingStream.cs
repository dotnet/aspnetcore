// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.HttpLogging;

internal sealed class RequestBufferingStream : BufferingStream
{
    private readonly Encoding _encoding;
    private readonly bool _logOnFinish;
    private readonly int _limit;
    private BodyStatus _status = BodyStatus.None;

    public bool HasLogged { get; private set; }

    public RequestBufferingStream(Stream innerStream, int limit, ILogger logger, Encoding encoding, bool logOnFinish)
        : base(innerStream, logger)
    {
        _logger = logger;
        _limit = limit;
        _innerStream = innerStream;
        _encoding = encoding;
        _logOnFinish = logOnFinish;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
    {
        var res = await _innerStream.ReadAsync(destination, cancellationToken);

        // Zero-byte reads (where the passed in buffer has 0 length) can occur when using PipeReader, we don't want to accidentally complete the RequestBody logging in this case
        if (destination.IsEmpty)
        {
            return res;
        }

        WriteToBuffer(destination.Slice(0, res).Span);

        return res;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var res = await _innerStream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);

        // Zero-byte reads (where the passed in buffer has 0 length) can occur when using PipeReader, we don't want to accidentally complete the RequestBody logging in this case
        if (count == 0)
        {
            return res;
        }

        WriteToBuffer(buffer.AsSpan(offset, res));

        return res;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var res = _innerStream.Read(buffer, offset, count);

        // Zero-byte reads (where the passed in buffer has 0 length) can occur when using PipeReader, we don't want to accidentally complete the RequestBody logging in this case
        if (count == 0)
        {
            return res;
        }

        WriteToBuffer(buffer.AsSpan(offset, res));

        return res;
    }

    private void WriteToBuffer(ReadOnlySpan<byte> span)
    {
        // get what was read into the buffer
        var remaining = _limit - _bytesBuffered;

        if (remaining == 0 || _status > BodyStatus.Incomplete)
        {
            return;
        }

        if (span.Length == 0)
        {
            // Done reading, log the string.
            _status = BodyStatus.Complete;
            LogRequestBody();
            return;
        }

        _status = BodyStatus.Incomplete;

        var innerCount = Math.Min(remaining, span.Length);

        if (span.Slice(0, innerCount).TryCopyTo(_tailMemory.Span))
        {
            _tailBytesBuffered += innerCount;
            _bytesBuffered += innerCount;
            _tailMemory = _tailMemory.Slice(innerCount);
        }
        else
        {
            BuffersExtensions.Write(this, span.Slice(0, innerCount));
        }

        if (_limit - _bytesBuffered == 0)
        {
            _status = BodyStatus.Truncated;
            LogRequestBody();
        }
    }

    public void LogRequestBody()
    {
        if (!HasLogged && _logOnFinish)
        {
            HasLogged = true;
            _logger.RequestBody(GetString(_encoding), GetStatus(showCompleted: false));
        }
    }

    public void LogRequestBody(HttpLoggingInterceptorContext logContext)
    {
        if (logContext.IsAnyEnabled(HttpLoggingFields.RequestBody))
        {
            logContext.AddParameter("RequestBody", GetString(_encoding));
            logContext.AddParameter("RequestBodyStatus", GetStatus(showCompleted: true));
        }
    }

    private string GetStatus(bool showCompleted) => _status switch
    {
        BodyStatus.None => "[Not consumed by app]",
        BodyStatus.Incomplete => "[Only partially consumed by app]",
        BodyStatus.Complete => showCompleted ? "[Completed]" : "",
        BodyStatus.Truncated => "[Truncated by RequestBodyLogLimit]",
        _ => throw new NotImplementedException(_status.ToString()),
    };

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return TaskToApm.Begin(ReadAsync(buffer, offset, count), callback, state);
    }

    public override int EndRead(IAsyncResult asyncResult)
    {
        return TaskToApm.End<int>(asyncResult);
    }

    private enum BodyStatus
    {
        /// <summary>
        /// The body was not read.
        /// </summary>
        None,

        /// <summary>
        /// The body was partially read.
        /// </summary>
        Incomplete,

        /// <summary>
        /// The body was completely read.
        /// </summary>
        Complete,

        /// <summary>
        /// The body was read and truncated.
        /// </summary>
        Truncated,
    }
}
