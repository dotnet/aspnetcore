// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    public class Http2FrameWriter : IHttp2FrameWriter
    {
        // Literal Header Field without Indexing - Indexed Name (Index 8 - :status)
        private static readonly byte[] _continueBytes = new byte[] { 0x08, 0x03, (byte)'1', (byte)'0', (byte)'0' };

        private readonly Http2Frame _outgoingFrame = new Http2Frame();
        private readonly object _writeLock = new object();
        private readonly HPackEncoder _hpackEncoder = new HPackEncoder();
        private readonly PipeWriter _outputWriter;
        private readonly PipeReader _outputReader;

        private bool _completed;

        public Http2FrameWriter(PipeWriter outputPipeWriter, PipeReader outputPipeReader)
        {
            _outputWriter = outputPipeWriter;
            _outputReader = outputPipeReader;
        }

        public void Complete()
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return;
                }

                _completed = true;
                _outputWriter.Complete();
            }
        }

        public void Abort(Exception ex)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return;
                }

                _completed = true;
                _outputReader.CancelPendingRead();
                _outputWriter.Complete(ex);
            }
        }

        public Task FlushAsync(CancellationToken cancellationToken)
        {
            lock (_writeLock)
            {
                return WriteAsync(Constants.EmptyData);
            }
        }

        public Task Write100ContinueAsync(int streamId)
        {
            lock (_writeLock)
            {
                _outgoingFrame.PrepareHeaders(Http2HeadersFrameFlags.END_HEADERS, streamId);
                _outgoingFrame.Length = _continueBytes.Length;
                _continueBytes.CopyTo(_outgoingFrame.HeadersPayload);

                return WriteAsync(_outgoingFrame.Raw);
            }
        }

        public void WriteResponseHeaders(int streamId, int statusCode, IHeaderDictionary headers)
        {
            lock (_writeLock)
            {
                _outgoingFrame.PrepareHeaders(Http2HeadersFrameFlags.NONE, streamId);

                var done = _hpackEncoder.BeginEncode(statusCode, EnumerateHeaders(headers), _outgoingFrame.Payload, out var payloadLength);
                _outgoingFrame.Length = payloadLength;

                if (done)
                {
                    _outgoingFrame.HeadersFlags = Http2HeadersFrameFlags.END_HEADERS;
                }

                Append(_outgoingFrame.Raw);

                while (!done)
                {
                    _outgoingFrame.PrepareContinuation(Http2ContinuationFrameFlags.NONE, streamId);

                    done = _hpackEncoder.Encode(_outgoingFrame.Payload, out var length);
                    _outgoingFrame.Length = length;

                    if (done)
                    {
                        _outgoingFrame.ContinuationFlags = Http2ContinuationFrameFlags.END_HEADERS;
                    }

                    Append(_outgoingFrame.Raw);
                }
            }
        }

        public Task WriteDataAsync(int streamId, ReadOnlySpan<byte> data, CancellationToken cancellationToken)
            => WriteDataAsync(streamId, data, endStream: false, cancellationToken: cancellationToken);

        public Task WriteDataAsync(int streamId, ReadOnlySpan<byte> data, bool endStream, CancellationToken cancellationToken)
        {
            var tasks = new List<Task>();

            lock (_writeLock)
            {
                _outgoingFrame.PrepareData(streamId);

                while (data.Length > _outgoingFrame.Length)
                {
                    data.Slice(0, _outgoingFrame.Length).CopyTo(_outgoingFrame.Payload);
                    data = data.Slice(_outgoingFrame.Length);

                    tasks.Add(WriteAsync(_outgoingFrame.Raw, cancellationToken));
                }

                _outgoingFrame.Length = data.Length;

                if (endStream)
                {
                    _outgoingFrame.DataFlags = Http2DataFrameFlags.END_STREAM;
                }

                data.CopyTo(_outgoingFrame.Payload);

                tasks.Add(WriteAsync(_outgoingFrame.Raw, cancellationToken));

                return Task.WhenAll(tasks);
            }
        }

        public Task WriteRstStreamAsync(int streamId, Http2ErrorCode errorCode)
        {
            lock (_writeLock)
            {
                _outgoingFrame.PrepareRstStream(streamId, errorCode);
                return WriteAsync(_outgoingFrame.Raw);
            }
        }

        public Task WriteSettingsAsync(Http2PeerSettings settings)
        {
            lock (_writeLock)
            {
                // TODO: actually send settings
                _outgoingFrame.PrepareSettings(Http2SettingsFrameFlags.NONE);
                return WriteAsync(_outgoingFrame.Raw);
            }
        }

        public Task WriteSettingsAckAsync()
        {
            lock (_writeLock)
            {
                _outgoingFrame.PrepareSettings(Http2SettingsFrameFlags.ACK);
                return WriteAsync(_outgoingFrame.Raw);
            }
        }

        public Task WritePingAsync(Http2PingFrameFlags flags, ReadOnlySpan<byte> payload)
        {
            lock (_writeLock)
            {
                _outgoingFrame.PreparePing(Http2PingFrameFlags.ACK);
                payload.CopyTo(_outgoingFrame.Payload);
                return WriteAsync(_outgoingFrame.Raw);
            }
        }

        public Task WriteGoAwayAsync(int lastStreamId, Http2ErrorCode errorCode)
        {
            lock (_writeLock)
            {
                _outgoingFrame.PrepareGoAway(lastStreamId, errorCode);
                return WriteAsync(_outgoingFrame.Raw);
            }
        }

        // Must be called with _writeLock
        private void Append(ReadOnlySpan<byte> data)
        {
            if (_completed)
            {
                return;
            }

            _outputWriter.Write(data);
        }

        // Must be called with _writeLock
        private Task WriteAsync(ReadOnlySpan<byte> data, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_completed)
            {
                return Task.CompletedTask;
            }

            _outputWriter.Write(data);
            return FlushAsync(_outputWriter, cancellationToken);
        }

        private async Task FlushAsync(PipeWriter outputWriter, CancellationToken cancellationToken)
        {
            await outputWriter.FlushAsync(cancellationToken);
        }

        private static IEnumerable<KeyValuePair<string, string>> EnumerateHeaders(IHeaderDictionary headers)
        {
            foreach (var header in headers)
            {
                foreach (var value in header.Value)
                {
                    yield return new KeyValuePair<string, string>(header.Key, value);
                }
            }
        }
    }
}
