// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Globalization;
using System.IO.Pipelines;
using System.Net.Http;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Core.WebTransport;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.WebTransport;

/// <summary>
/// Represents a base WebTransport stream. Do not use directly as it does not
/// contain logic for handling data.
/// </summary>
internal class WebTransportStream : ConnectionContext
{
    private readonly CancellationTokenRegistration _connectionClosedRegistration;
    private readonly bool _canWrite;
    private readonly bool _canRead;
    private readonly DuplexPipe _duplexPipe;
    private readonly IFeatureCollection _features;
    private readonly KestrelTrace _log;

    private bool _isClosed;

    internal readonly IStreamDirectionFeature _streamDirectionFeature = default!;

    public readonly long StreamId;

    /// <summary>
    /// Use StreamId instead of this whenever possible to avoid the cast from long to string
    /// </summary>
    public override string ConnectionId { get => StreamId.ToString(NumberFormatInfo.InvariantInfo); set => throw new NotSupportedException(); }

    public override IDuplexPipe Transport { get => _duplexPipe; set => throw new NotSupportedException(); }

    public override IFeatureCollection Features => _features;

    public override IDictionary<object, object?> Items { get; set; }

    internal WebTransportStream(Http3StreamContext context, WebTransportStreamType type)
    {
        _canRead = type != WebTransportStreamType.Output;
        _canWrite = type != WebTransportStreamType.Input;
        _streamDirectionFeature = context.ConnectionFeatures.GetRequiredFeature<IStreamDirectionFeature>();
        _log = context.ServiceContext.Log;

        var streamIdFeature = context.ConnectionFeatures.GetRequiredFeature<IStreamIdFeature>();
        StreamId = streamIdFeature!.StreamId;

        _features = new FeatureCollection(1);
        _features.Set(_streamDirectionFeature);

        _duplexPipe = new DuplexPipe(context.Transport.Input, context.Transport.Output);

        // will not trigger if closed only of of the directions of a stream. Stream must be fully
        // ended before this will be called. Then it will be considered an abort
        _connectionClosedRegistration = context.StreamContext.ConnectionClosed.Register(state =>
        {
            context.WebTransportSession?.TryRemoveStream(StreamId);
        }, null);

        ConnectionClosed = _connectionClosedRegistration.Token;
        Items = new Dictionary<object, object?>();
    }

    public override void Abort(ConnectionAbortedException abortReason)
    {
        if (_isClosed)
        {
            return;
        }

        _isClosed = true;

        _log.Http3StreamAbort(ConnectionId, Http3ErrorCode.InternalError, abortReason);

        if (_canRead)
        {
            _duplexPipe.Input.Complete(abortReason);
        }

        if (_canWrite)
        {
            _duplexPipe.Output.Complete(abortReason);
        }
    }

    /// <summary>
    /// Soft close the stream and end data transmission.
    /// </summary>
    public override async ValueTask DisposeAsync()
    {
        if (_isClosed)
        {
            return;
        }

        _isClosed = true;

        await _connectionClosedRegistration.DisposeAsync();

        if (_canRead)
        {
            await _duplexPipe.Input.CompleteAsync();
        }

        if (_canWrite)
        {
            await _duplexPipe.Output.CompleteAsync();
        }
    }
}

/// <summary>
/// Defines a helper function to make reading from a pipe more elegant.
/// </summary>
public static class PipeReaderExtension
{
    /// <summary>
    /// Read data from the input pipe.
    /// </summary>
    /// <param name="context">The WebTransport stream object.</param>
    /// <param name="buffer">buffer to read the data into.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The amounts of bytes read.</returns>
    public static async ValueTask<int> ReadAsync(this PipeReader input, Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await input.ReadAsync(cancellationToken);

            if (result.IsCanceled)
            {
                throw new OperationCanceledException(CoreStrings.WebTransportReadCancelled);
            }

            var resultBuffer = result.Buffer;
            var length = resultBuffer.Length;

            var consumed = resultBuffer.End;

            if (length != 0)
            {
                var actual = (int)Math.Min(length, buffer.Length);

                var slice = actual == length ? resultBuffer : resultBuffer.Slice(0, actual);
                consumed = slice.End;
                slice.CopyTo(buffer.Span);

                input.AdvanceTo(consumed);

                return actual;
            }
            else
            {
                input.AdvanceTo(consumed);
                return 0;
            }
        }
        catch (Exception)
        {
            return 0;
        }
    }
}
