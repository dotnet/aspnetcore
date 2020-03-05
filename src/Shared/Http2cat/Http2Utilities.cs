// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Http;
using System.Net.Http.HPack;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using IHttpHeadersHandler = System.Net.Http.IHttpHeadersHandler;

namespace Microsoft.AspNetCore.Http2Cat
{
    internal class Http2Utilities : IHttpHeadersHandler
    {
        public static ReadOnlySpan<byte> ClientPreface => new byte[24] { (byte)'P', (byte)'R', (byte)'I', (byte)' ', (byte)'*', (byte)' ', (byte)'H', (byte)'T', (byte)'T', (byte)'P', (byte)'/', (byte)'2', (byte)'.', (byte)'0', (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n', (byte)'S', (byte)'M', (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };
        public const int MaxRequestHeaderFieldSize = 16 * 1024;
        public static readonly string FourKHeaderValue = new string('a', 4096);
        private static readonly Encoding HeaderValueEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        public static readonly IEnumerable<KeyValuePair<string, string>> BrowserRequestHeaders = new[]
        {
            new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(HeaderNames.Path, "/"),
            new KeyValuePair<string, string>(HeaderNames.Scheme, "https"),
            new KeyValuePair<string, string>(HeaderNames.Authority, "localhost:443"),
            new KeyValuePair<string, string>("user-agent", "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:54.0) Gecko/20100101 Firefox/54.0"),
            new KeyValuePair<string, string>("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8"),
            new KeyValuePair<string, string>("accept-language", "en-US,en;q=0.5"),
            new KeyValuePair<string, string>("accept-encoding", "gzip, deflate, br"),
            new KeyValuePair<string, string>("upgrade-insecure-requests", "1"),
        };

        public static readonly IEnumerable<KeyValuePair<string, string>> BrowserRequestHeadersHttp = new[]
        {
            new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(HeaderNames.Path, "/"),
            new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(HeaderNames.Authority, "localhost:80"),
            new KeyValuePair<string, string>("user-agent", "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:54.0) Gecko/20100101 Firefox/54.0"),
            new KeyValuePair<string, string>("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8"),
            new KeyValuePair<string, string>("accept-language", "en-US,en;q=0.5"),
            new KeyValuePair<string, string>("accept-encoding", "gzip, deflate, br"),
            new KeyValuePair<string, string>("upgrade-insecure-requests", "1"),
        };

        public static readonly IEnumerable<KeyValuePair<string, string>> PostRequestHeaders = new[]
        {
            new KeyValuePair<string, string>(HeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(HeaderNames.Path, "/"),
            new KeyValuePair<string, string>(HeaderNames.Scheme, "https"),
            new KeyValuePair<string, string>(HeaderNames.Authority, "localhost:80"),
        };

        public static readonly IEnumerable<KeyValuePair<string, string>> ExpectContinueRequestHeaders = new[]
        {
            new KeyValuePair<string, string>(HeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(HeaderNames.Path, "/"),
            new KeyValuePair<string, string>(HeaderNames.Authority, "127.0.0.1"),
            new KeyValuePair<string, string>(HeaderNames.Scheme, "https"),
            new KeyValuePair<string, string>("expect", "100-continue"),
        };

        public static readonly IEnumerable<KeyValuePair<string, string>> RequestTrailers = new[]
        {
            new KeyValuePair<string, string>("trailer-one", "1"),
            new KeyValuePair<string, string>("trailer-two", "2"),
        };

        public static readonly IEnumerable<KeyValuePair<string, string>> OneContinuationRequestHeaders = new[]
        {
            new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(HeaderNames.Path, "/"),
            new KeyValuePair<string, string>(HeaderNames.Scheme, "https"),
            new KeyValuePair<string, string>(HeaderNames.Authority, "localhost:80"),
            new KeyValuePair<string, string>("a", FourKHeaderValue),
            new KeyValuePair<string, string>("b", FourKHeaderValue),
            new KeyValuePair<string, string>("c", FourKHeaderValue),
            new KeyValuePair<string, string>("d", FourKHeaderValue)
        };

        public static readonly IEnumerable<KeyValuePair<string, string>> TwoContinuationsRequestHeaders = new[]
        {
            new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(HeaderNames.Path, "/"),
            new KeyValuePair<string, string>(HeaderNames.Scheme, "https"),
            new KeyValuePair<string, string>(HeaderNames.Authority, "localhost:80"),
            new KeyValuePair<string, string>("a", FourKHeaderValue),
            new KeyValuePair<string, string>("b", FourKHeaderValue),
            new KeyValuePair<string, string>("c", FourKHeaderValue),
            new KeyValuePair<string, string>("d", FourKHeaderValue),
            new KeyValuePair<string, string>("e", FourKHeaderValue),
            new KeyValuePair<string, string>("f", FourKHeaderValue),
            new KeyValuePair<string, string>("g", FourKHeaderValue),
        };

        public static IEnumerable<KeyValuePair<string, string>> ReadRateRequestHeaders(int expectedBytes) => new[]
        {
            new KeyValuePair<string, string>(HeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(HeaderNames.Path, "/" + expectedBytes),
            new KeyValuePair<string, string>(HeaderNames.Scheme, "https"),
            new KeyValuePair<string, string>(HeaderNames.Authority, "localhost:80"),
        };

        public static readonly byte[] _helloBytes = Encoding.ASCII.GetBytes("hello");
        public static readonly byte[] _worldBytes = Encoding.ASCII.GetBytes("world");
        public static readonly byte[] _helloWorldBytes = Encoding.ASCII.GetBytes("hello, world");
        public static readonly byte[] _noData = new byte[0];
        public static readonly byte[] _maxData = Encoding.ASCII.GetBytes(new string('a', Http2PeerSettings.MinAllowedMaxFrameSize));

        internal readonly Http2PeerSettings _clientSettings = new Http2PeerSettings();
        internal readonly HPackDecoder _hpackDecoder;
        private readonly byte[] _headerEncodingBuffer = new byte[Http2PeerSettings.MinAllowedMaxFrameSize];

        public readonly Dictionary<string, string> _decodedHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        internal DuplexPipe.DuplexPipePair _pair;
        public long _bytesReceived;

        public Http2Utilities(ConnectionContext clientConnectionContext, ILogger logger, CancellationToken stopToken)
        {
            _hpackDecoder = new HPackDecoder((int)_clientSettings.HeaderTableSize, MaxRequestHeaderFieldSize);
            _pair = new DuplexPipe.DuplexPipePair(transport: null, application: clientConnectionContext.Transport);
            Logger = logger;
            StopToken = stopToken;
        }

        public ILogger Logger { get; }
        public CancellationToken StopToken { get; }

        void IHttpHeadersHandler.OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
        {
            _decodedHeaders[GetAsciiStringNonNullCharacters(name)] = GetAsciiOrUTF8StringNonNullCharacters(value);
        }

        public unsafe string GetAsciiStringNonNullCharacters(ReadOnlySpan<byte> span)
        {
            if (span.IsEmpty)
            {
                return string.Empty;
            }

            var asciiString = new string('\0', span.Length);

            fixed (char* output = asciiString)
            fixed (byte* buffer = span)
            {
                // This version if AsciiUtilities returns null if there are any null (0 byte) characters
                // in the string
                if (!StringUtilities.TryGetAsciiString(buffer, output, span.Length))
                {
                    throw new InvalidOperationException();
                }
            }
            return asciiString;
        }

        public unsafe string GetAsciiOrUTF8StringNonNullCharacters(ReadOnlySpan<byte> span)
        {
            if (span.IsEmpty)
            {
                return string.Empty;
            }

            var resultString = new string('\0', span.Length);

            fixed (char* output = resultString)
            fixed (byte* buffer = span)
            {
                // This version if AsciiUtilities returns null if there are any null (0 byte) characters
                // in the string
                if (!StringUtilities.TryGetAsciiString(buffer, output, span.Length))
                {
                    // null characters are considered invalid
                    if (span.IndexOf((byte)0) != -1)
                    {
                        throw new InvalidOperationException();
                    }

                    try
                    {
                        resultString = HeaderValueEncoding.GetString(buffer, span.Length);
                    }
                    catch (DecoderFallbackException)
                    {
                        throw new InvalidOperationException();
                    }
                }
            }
            return resultString;
        }

        void IHttpHeadersHandler.OnHeadersComplete(bool endStream) { }

        public async Task InitializeConnectionAsync(int expectedSettingsCount = 3)
        {
            await SendPreambleAsync().ConfigureAwait(false);
            await SendSettingsAsync();

            await ExpectAsync(Http2FrameType.SETTINGS,
                withLength: expectedSettingsCount * Http2FrameReader.SettingSize,
                withFlags: 0,
                withStreamId: 0);

            await ExpectAsync(Http2FrameType.WINDOW_UPDATE,
                withLength: 4,
                withFlags: 0,
                withStreamId: 0);

            await ExpectAsync(Http2FrameType.SETTINGS,
                withLength: 0,
                withFlags: (byte)Http2SettingsFrameFlags.ACK,
                withStreamId: 0);
        }

        public Task StartStreamAsync(int streamId, IEnumerable<KeyValuePair<string, string>> headers, bool endStream)
        {
            var writableBuffer = _pair.Application.Output;
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            var frame = new Http2Frame();
            frame.PrepareHeaders(Http2HeadersFrameFlags.NONE, streamId);

            var buffer = _headerEncodingBuffer.AsSpan();
            var headersEnumerator = GetHeadersEnumerator(headers);
            var done = HPackHeaderWriter.BeginEncodeHeaders(headersEnumerator, buffer, out var length);
            frame.PayloadLength = length;

            if (done)
            {
                frame.HeadersFlags = Http2HeadersFrameFlags.END_HEADERS;
            }

            if (endStream)
            {
                frame.HeadersFlags |= Http2HeadersFrameFlags.END_STREAM;
            }

            WriteHeader(frame, writableBuffer);
            writableBuffer.Write(buffer.Slice(0, length));

            while (!done)
            {
                frame.PrepareContinuation(Http2ContinuationFrameFlags.NONE, streamId);

                done = HPackHeaderWriter.ContinueEncodeHeaders(headersEnumerator, buffer, out length);
                frame.PayloadLength = length;

                if (done)
                {
                    frame.ContinuationFlags = Http2ContinuationFrameFlags.END_HEADERS;
                }

                WriteHeader(frame, writableBuffer);
                writableBuffer.Write(buffer.Slice(0, length));
            }

            return FlushAsync(writableBuffer);
        }

        private static IEnumerator<KeyValuePair<string, string>> GetHeadersEnumerator(IEnumerable<KeyValuePair<string, string>> headers)
        {
            var headersEnumerator = headers.GetEnumerator();
            return headersEnumerator;
        }

        internal Dictionary<string, string> DecodeHeaders(Http2FrameWithPayload frame, bool endHeaders = false)
        {
            Assert.Equal(Http2FrameType.HEADERS, frame.Type);
            _hpackDecoder.Decode(frame.PayloadSequence, endHeaders, handler: this);
            return _decodedHeaders;
        }

        internal void ResetHeaders()
        {
            _decodedHeaders.Clear();
        }

        /* https://tools.ietf.org/html/rfc7540#section-4.1
            +-----------------------------------------------+
            |                 Length (24)                   |
            +---------------+---------------+---------------+
            |   Type (8)    |   Flags (8)   |
            +-+-------------+---------------+-------------------------------+
            |R|                 Stream Identifier (31)                      |
            +=+=============================================================+
            |                   Frame Payload (0...)                      ...
            +---------------------------------------------------------------+
        */
        internal static void WriteHeader(Http2Frame frame, PipeWriter output)
        {
            var buffer = output.GetSpan(Http2FrameReader.HeaderLength);

            Bitshifter.WriteUInt24BigEndian(buffer, (uint)frame.PayloadLength);
            buffer = buffer.Slice(3);

            buffer[0] = (byte)frame.Type;
            buffer[1] = frame.Flags;
            buffer = buffer.Slice(2);

            Bitshifter.WriteUInt31BigEndian(buffer, (uint)frame.StreamId, preserveHighestBit: false);

            output.Advance(Http2FrameReader.HeaderLength);
        }

        /* https://tools.ietf.org/html/rfc7540#section-6.2
            +---------------+
            |Pad Length? (8)|
            +-+-------------+-----------------------------------------------+
            |                   Header Block Fragment (*)                 ...
            +---------------------------------------------------------------+
            |                           Padding (*)                       ...
            +---------------------------------------------------------------+
        */
        public Task SendHeadersWithPaddingAsync(int streamId, IEnumerable<KeyValuePair<string, string>> headers, byte padLength, bool endStream)
        {
            var writableBuffer = _pair.Application.Output;
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            var frame = new Http2Frame();

            frame.PrepareHeaders(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.PADDED, streamId);
            frame.HeadersPadLength = padLength;

            var extendedHeaderLength = 1; // Padding length field
            var buffer = _headerEncodingBuffer.AsSpan();
            var extendedHeader = buffer.Slice(0, extendedHeaderLength);
            extendedHeader[0] = padLength;
            var payload = buffer.Slice(extendedHeaderLength, buffer.Length - padLength - extendedHeaderLength);

            HPackHeaderWriter.BeginEncodeHeaders(GetHeadersEnumerator(headers), payload, out var length);
            var padding = buffer.Slice(extendedHeaderLength + length, padLength);
            padding.Fill(0);

            frame.PayloadLength = extendedHeaderLength + length + padLength;

            if (endStream)
            {
                frame.HeadersFlags |= Http2HeadersFrameFlags.END_STREAM;
            }

            WriteHeader(frame, writableBuffer);
            writableBuffer.Write(buffer.Slice(0, frame.PayloadLength));
            return FlushAsync(writableBuffer);
        }

        /* https://tools.ietf.org/html/rfc7540#section-6.2
            +-+-------------+-----------------------------------------------+
            |E|                 Stream Dependency? (31)                     |
            +-+-------------+-----------------------------------------------+
            |  Weight? (8)  |
            +-+-------------+-----------------------------------------------+
            |                   Header Block Fragment (*)                 ...
            +---------------------------------------------------------------+
        */
        public Task SendHeadersWithPriorityAsync(int streamId, IEnumerable<KeyValuePair<string, string>> headers, byte priority, int streamDependency, bool endStream)
        {
            var writableBuffer = _pair.Application.Output;
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            var frame = new Http2Frame();
            frame.PrepareHeaders(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.PRIORITY, streamId);
            frame.HeadersPriorityWeight = priority;
            frame.HeadersStreamDependency = streamDependency;

            var extendedHeaderLength = 5; // stream dependency + weight
            var buffer = _headerEncodingBuffer.AsSpan();
            var extendedHeader = buffer.Slice(0, extendedHeaderLength);
            Bitshifter.WriteUInt31BigEndian(extendedHeader, (uint)streamDependency);
            extendedHeader[4] = priority;
            var payload = buffer.Slice(extendedHeaderLength);

            HPackHeaderWriter.BeginEncodeHeaders(GetHeadersEnumerator(headers), payload, out var length);

            frame.PayloadLength = extendedHeaderLength + length;

            if (endStream)
            {
                frame.HeadersFlags |= Http2HeadersFrameFlags.END_STREAM;
            }

            WriteHeader(frame, writableBuffer);
            writableBuffer.Write(buffer.Slice(0, frame.PayloadLength));
            return FlushAsync(writableBuffer);
        }

        /* https://tools.ietf.org/html/rfc7540#section-6.2
            +---------------+
            |Pad Length? (8)|
            +-+-------------+-----------------------------------------------+
            |E|                 Stream Dependency? (31)                     |
            +-+-------------+-----------------------------------------------+
            |  Weight? (8)  |
            +-+-------------+-----------------------------------------------+
            |                   Header Block Fragment (*)                 ...
            +---------------------------------------------------------------+
            |                           Padding (*)                       ...
            +---------------------------------------------------------------+
        */
        public Task SendHeadersWithPaddingAndPriorityAsync(int streamId, IEnumerable<KeyValuePair<string, string>> headers, byte padLength, byte priority, int streamDependency, bool endStream)
        {
            var writableBuffer = _pair.Application.Output;
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            var frame = new Http2Frame();
            frame.PrepareHeaders(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.PADDED | Http2HeadersFrameFlags.PRIORITY, streamId);
            frame.HeadersPadLength = padLength;
            frame.HeadersPriorityWeight = priority;
            frame.HeadersStreamDependency = streamDependency;

            var extendedHeaderLength = 6; // pad length + stream dependency + weight
            var buffer = _headerEncodingBuffer.AsSpan();
            var extendedHeader = buffer.Slice(0, extendedHeaderLength);
            extendedHeader[0] = padLength;
            Bitshifter.WriteUInt31BigEndian(extendedHeader.Slice(1), (uint)streamDependency);
            extendedHeader[5] = priority;
            var payload = buffer.Slice(extendedHeaderLength, buffer.Length - padLength - extendedHeaderLength);

            HPackHeaderWriter.BeginEncodeHeaders(GetHeadersEnumerator(headers), payload, out var length);
            var padding = buffer.Slice(extendedHeaderLength + length, padLength);
            padding.Fill(0);

            frame.PayloadLength = extendedHeaderLength + length + padLength;

            if (endStream)
            {
                frame.HeadersFlags |= Http2HeadersFrameFlags.END_STREAM;
            }

            WriteHeader(frame, writableBuffer);
            writableBuffer.Write(buffer.Slice(0, frame.PayloadLength));
            return FlushAsync(writableBuffer);
        }

        public Task SendAsync(ReadOnlySpan<byte> span)
        {
            var writableBuffer = _pair.Application.Output;
            writableBuffer.Write(span);
            return FlushAsync(writableBuffer);
        }

        public static async Task FlushAsync(PipeWriter writableBuffer)
        {
            await writableBuffer.FlushAsync().AsTask().DefaultTimeout();
        }

        public Task SendPreambleAsync() => SendAsync(ClientPreface);

        public async Task SendSettingsAsync()
        {
            var writableBuffer = _pair.Application.Output;
            var frame = new Http2Frame();
            frame.PrepareSettings(Http2SettingsFrameFlags.NONE);
            var settings = _clientSettings.GetNonProtocolDefaults();
            var payload = new byte[settings.Count * Http2FrameReader.SettingSize];
            frame.PayloadLength = payload.Length;
            WriteSettings(settings, payload);
            WriteHeader(frame, writableBuffer);
            await SendAsync(payload);
        }

        internal static void WriteSettings(IList<Http2PeerSetting> settings, Span<byte> destination)
        {
            foreach (var setting in settings)
            {
                BinaryPrimitives.WriteUInt16BigEndian(destination, (ushort)setting.Parameter);
                BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(2), setting.Value);
                destination = destination.Slice(Http2FrameReader.SettingSize);
            }
        }

        public async Task SendSettingsAckWithInvalidLengthAsync(int length)
        {
            var writableBuffer = _pair.Application.Output;
            var frame = new Http2Frame();
            frame.PrepareSettings(Http2SettingsFrameFlags.ACK);
            frame.PayloadLength = length;
            WriteHeader(frame, writableBuffer);
            await SendAsync(new byte[length]);
        }

        public async Task SendSettingsWithInvalidStreamIdAsync(int streamId)
        {
            var writableBuffer = _pair.Application.Output;
            var frame = new Http2Frame();
            frame.PrepareSettings(Http2SettingsFrameFlags.NONE);
            frame.StreamId = streamId;
            var settings = _clientSettings.GetNonProtocolDefaults();
            var payload = new byte[settings.Count * Http2FrameReader.SettingSize];
            frame.PayloadLength = payload.Length;
            WriteSettings(settings, payload);
            WriteHeader(frame, writableBuffer);
            await SendAsync(payload);
        }

        public async Task SendSettingsWithInvalidLengthAsync(int length)
        {
            var writableBuffer = _pair.Application.Output;
            var frame = new Http2Frame();
            frame.PrepareSettings(Http2SettingsFrameFlags.NONE);

            frame.PayloadLength = length;
            var payload = new byte[length];
            WriteHeader(frame, writableBuffer);
            await SendAsync(payload);
        }

        internal async Task SendSettingsWithInvalidParameterValueAsync(Http2SettingsParameter parameter, uint value)
        {
            var writableBuffer = _pair.Application.Output;
            var frame = new Http2Frame();
            frame.PrepareSettings(Http2SettingsFrameFlags.NONE);
            frame.PayloadLength = 6;
            var payload = new byte[Http2FrameReader.SettingSize];
            payload[0] = (byte)((ushort)parameter >> 8);
            payload[1] = (byte)(ushort)parameter;
            payload[2] = (byte)(value >> 24);
            payload[3] = (byte)(value >> 16);
            payload[4] = (byte)(value >> 8);
            payload[5] = (byte)value;

            WriteHeader(frame, writableBuffer);
            await SendAsync(payload);
        }

        public Task SendPushPromiseFrameAsync()
        {
            var writableBuffer = _pair.Application.Output;
            var frame = new Http2Frame();
            frame.PayloadLength = 0;
            frame.Type = Http2FrameType.PUSH_PROMISE;
            frame.StreamId = 1;

            WriteHeader(frame, writableBuffer);
            return FlushAsync(writableBuffer);
        }

        internal async Task<bool> SendHeadersAsync(int streamId, Http2HeadersFrameFlags flags, IEnumerable<KeyValuePair<string, string>> headers)
        {
            var outputWriter = _pair.Application.Output;
            var frame = new Http2Frame();

            frame.PrepareHeaders(flags, streamId);
            var buffer = _headerEncodingBuffer.AsMemory();
            var done = HPackHeaderWriter.BeginEncodeHeaders(GetHeadersEnumerator(headers), buffer.Span, out var length);
            frame.PayloadLength = length;

            WriteHeader(frame, outputWriter);
            await SendAsync(buffer.Span.Slice(0, length));

            return done;
        }

        internal async Task SendHeadersAsync(int streamId, Http2HeadersFrameFlags flags, byte[] headerBlock)
        {
            var outputWriter = _pair.Application.Output;
            var frame = new Http2Frame();

            frame.PrepareHeaders(flags, streamId);
            frame.PayloadLength = headerBlock.Length;

            WriteHeader(frame, outputWriter);
            await SendAsync(headerBlock);
        }

        public async Task SendInvalidHeadersFrameAsync(int streamId, int payloadLength, byte padLength)
        {
            Assert.True(padLength >= payloadLength, $"{nameof(padLength)} must be greater than or equal to {nameof(payloadLength)} to create an invalid frame.");

            var outputWriter = _pair.Application.Output;
            var frame = new Http2Frame();

            frame.PrepareHeaders(Http2HeadersFrameFlags.PADDED, streamId);
            frame.PayloadLength = payloadLength;
            var payload = new byte[payloadLength];
            if (payloadLength > 0)
            {
                payload[0] = padLength;
            }

            WriteHeader(frame, outputWriter);
            await SendAsync(payload);
        }

        public async Task SendIncompleteHeadersFrameAsync(int streamId)
        {
            var outputWriter = _pair.Application.Output;
            var frame = new Http2Frame();

            frame.PrepareHeaders(Http2HeadersFrameFlags.END_HEADERS, streamId);
            frame.PayloadLength = 3;
            var payload = new byte[3];
            // Set up an incomplete Literal Header Field w/ Incremental Indexing frame,
            // with an incomplete new name
            payload[0] = 0;
            payload[1] = 2;
            payload[2] = (byte)'a';

            WriteHeader(frame, outputWriter);
            await SendAsync(payload);
        }

        internal async Task<bool> SendContinuationAsync(int streamId, Http2ContinuationFrameFlags flags, IEnumerator<KeyValuePair<string, string>> headersEnumerator)
        {
            var outputWriter = _pair.Application.Output;
            var frame = new Http2Frame();

            frame.PrepareContinuation(flags, streamId);
            var buffer = _headerEncodingBuffer.AsMemory();
            var done = HPackHeaderWriter.ContinueEncodeHeaders(headersEnumerator, buffer.Span, out var length);
            frame.PayloadLength = length;

            WriteHeader(frame, outputWriter);
            await SendAsync(buffer.Span.Slice(0, length));

            return done;
        }

        internal async Task SendContinuationAsync(int streamId, Http2ContinuationFrameFlags flags, byte[] payload)
        {
            var outputWriter = _pair.Application.Output;
            var frame = new Http2Frame();

            frame.PrepareContinuation(flags, streamId);
            frame.PayloadLength = payload.Length;

            WriteHeader(frame, outputWriter);
            await SendAsync(payload);
        }

        internal async Task<bool> SendContinuationAsync(int streamId, Http2ContinuationFrameFlags flags, IEnumerable<KeyValuePair<string, string>> headers)
        {
            var outputWriter = _pair.Application.Output;
            var frame = new Http2Frame();

            frame.PrepareContinuation(flags, streamId);
            var buffer = _headerEncodingBuffer.AsMemory();
            var done = HPackHeaderWriter.BeginEncodeHeaders(GetHeadersEnumerator(headers), buffer.Span, out var length);
            frame.PayloadLength = length;

            WriteHeader(frame, outputWriter);
            await SendAsync(buffer.Span.Slice(0, length));

            return done;
        }

        internal Task SendEmptyContinuationFrameAsync(int streamId, Http2ContinuationFrameFlags flags)
        {
            var outputWriter = _pair.Application.Output;
            var frame = new Http2Frame();

            frame.PrepareContinuation(flags, streamId);
            frame.PayloadLength = 0;

            WriteHeader(frame, outputWriter);
            return FlushAsync(outputWriter);
        }

        public async Task SendIncompleteContinuationFrameAsync(int streamId)
        {
            var outputWriter = _pair.Application.Output;
            var frame = new Http2Frame();

            frame.PrepareContinuation(Http2ContinuationFrameFlags.END_HEADERS, streamId);
            frame.PayloadLength = 3;
            var payload = new byte[3];
            // Set up an incomplete Literal Header Field w/ Incremental Indexing frame,
            // with an incomplete new name
            payload[0] = 0;
            payload[1] = 2;
            payload[2] = (byte)'a';

            WriteHeader(frame, outputWriter);
            await SendAsync(payload);
        }

        public Task SendDataAsync(int streamId, Memory<byte> data, bool endStream)
        {
            var outputWriter = _pair.Application.Output;
            var frame = new Http2Frame();

            frame.PrepareData(streamId);
            frame.PayloadLength = data.Length;
            frame.DataFlags = endStream ? Http2DataFrameFlags.END_STREAM : Http2DataFrameFlags.NONE;

            WriteHeader(frame, outputWriter);
            return SendAsync(data.Span);
        }

        public async Task SendDataWithPaddingAsync(int streamId, Memory<byte> data, byte padLength, bool endStream)
        {
            var outputWriter = _pair.Application.Output;
            var frame = new Http2Frame();

            frame.PrepareData(streamId, padLength);
            frame.PayloadLength = data.Length + 1 + padLength;

            if (endStream)
            {
                frame.DataFlags |= Http2DataFrameFlags.END_STREAM;
            }

            WriteHeader(frame, outputWriter);
            outputWriter.GetSpan(1)[0] = padLength;
            outputWriter.Advance(1);
            await SendAsync(data.Span);
            await SendAsync(new byte[padLength]);
        }

        public Task SendInvalidDataFrameAsync(int streamId, int frameLength, byte padLength)
        {
            Assert.True(padLength >= frameLength, $"{nameof(padLength)} must be greater than or equal to {nameof(frameLength)} to create an invalid frame.");

            var outputWriter = _pair.Application.Output;
            var frame = new Http2Frame();

            frame.PrepareData(streamId);
            frame.DataFlags = Http2DataFrameFlags.PADDED;
            frame.PayloadLength = frameLength;
            var payload = new byte[frameLength];
            if (frameLength > 0)
            {
                payload[0] = padLength;
            }

            WriteHeader(frame, outputWriter);
            return SendAsync(payload);
        }

        internal Task SendPingAsync(Http2PingFrameFlags flags)
        {
            var outputWriter = _pair.Application.Output;
            var pingFrame = new Http2Frame();
            pingFrame.PreparePing(flags);
            WriteHeader(pingFrame, outputWriter);
            return SendAsync(new byte[8]); // Empty payload
        }

        public Task SendPingWithInvalidLengthAsync(int length)
        {
            var outputWriter = _pair.Application.Output;
            var pingFrame = new Http2Frame();
            pingFrame.PreparePing(Http2PingFrameFlags.NONE);
            pingFrame.PayloadLength = length;
            WriteHeader(pingFrame, outputWriter);
            return SendAsync(new byte[length]);
        }

        public Task SendPingWithInvalidStreamIdAsync(int streamId)
        {
            Assert.NotEqual(0, streamId);

            var outputWriter = _pair.Application.Output;
            var pingFrame = new Http2Frame();
            pingFrame.PreparePing(Http2PingFrameFlags.NONE);
            pingFrame.StreamId = streamId;
            WriteHeader(pingFrame, outputWriter);
            return SendAsync(new byte[pingFrame.PayloadLength]);
        }

        /* https://tools.ietf.org/html/rfc7540#section-6.3
            +-+-------------------------------------------------------------+
            |E|                  Stream Dependency (31)                     |
            +-+-------------+-----------------------------------------------+
            |   Weight (8)  |
            +-+-------------+
        */
        public Task SendPriorityAsync(int streamId, int streamDependency = 0)
        {
            var outputWriter = _pair.Application.Output;
            var priorityFrame = new Http2Frame();
            priorityFrame.PreparePriority(streamId, streamDependency: streamDependency, exclusive: false, weight: 0);

            var payload = new byte[priorityFrame.PayloadLength].AsSpan();
            Bitshifter.WriteUInt31BigEndian(payload, (uint)streamDependency);
            payload[4] = 0; // Weight

            WriteHeader(priorityFrame, outputWriter);
            return SendAsync(payload);
        }

        public Task SendInvalidPriorityFrameAsync(int streamId, int length)
        {
            var outputWriter = _pair.Application.Output;
            var priorityFrame = new Http2Frame();
            priorityFrame.PreparePriority(streamId, streamDependency: 0, exclusive: false, weight: 0);
            priorityFrame.PayloadLength = length;

            WriteHeader(priorityFrame, outputWriter);
            return SendAsync(new byte[length]);
        }

        /* https://tools.ietf.org/html/rfc7540#section-6.4
            +---------------------------------------------------------------+
            |                        Error Code (32)                        |
            +---------------------------------------------------------------+
        */
        public Task SendRstStreamAsync(int streamId)
        {
            var outputWriter = _pair.Application.Output;
            var rstStreamFrame = new Http2Frame();
            rstStreamFrame.PrepareRstStream(streamId, Http2ErrorCode.CANCEL);
            var payload = new byte[rstStreamFrame.PayloadLength];
            BinaryPrimitives.WriteUInt32BigEndian(payload, (uint)Http2ErrorCode.CANCEL);

            WriteHeader(rstStreamFrame, outputWriter);
            return SendAsync(payload);
        }

        public Task SendInvalidRstStreamFrameAsync(int streamId, int length)
        {
            var outputWriter = _pair.Application.Output;
            var frame = new Http2Frame();
            frame.PrepareRstStream(streamId, Http2ErrorCode.CANCEL);
            frame.PayloadLength = length;
            WriteHeader(frame, outputWriter);
            return SendAsync(new byte[length]);
        }

        public Task SendGoAwayAsync()
        {
            var outputWriter = _pair.Application.Output;
            var frame = new Http2Frame();
            frame.PrepareGoAway(0, Http2ErrorCode.NO_ERROR);
            WriteHeader(frame, outputWriter);
            return SendAsync(new byte[frame.PayloadLength]);
        }

        public Task SendInvalidGoAwayFrameAsync()
        {
            var outputWriter = _pair.Application.Output;
            var frame = new Http2Frame();
            frame.PrepareGoAway(0, Http2ErrorCode.NO_ERROR);
            frame.StreamId = 1;
            WriteHeader(frame, outputWriter);
            return SendAsync(new byte[frame.PayloadLength]);
        }

        public Task SendWindowUpdateAsync(int streamId, int sizeIncrement)
        {
            var outputWriter = _pair.Application.Output;
            var frame = new Http2Frame();
            frame.PrepareWindowUpdate(streamId, sizeIncrement);
            WriteHeader(frame, outputWriter);
            var buffer = outputWriter.GetSpan(4);
            BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)sizeIncrement);
            outputWriter.Advance(4);
            return FlushAsync(outputWriter);
        }

        public Task SendInvalidWindowUpdateAsync(int streamId, int sizeIncrement, int length)
        {
            var outputWriter = _pair.Application.Output;
            var frame = new Http2Frame();
            frame.PrepareWindowUpdate(streamId, sizeIncrement);
            frame.PayloadLength = length;
            WriteHeader(frame, outputWriter);
            return SendAsync(new byte[length]);
        }

        public Task SendUnknownFrameTypeAsync(int streamId, int frameType)
        {
            var outputWriter = _pair.Application.Output;
            var frame = new Http2Frame();
            frame.StreamId = streamId;
            frame.Type = (Http2FrameType)frameType;
            frame.PayloadLength = 0;
            WriteHeader(frame, outputWriter);
            return FlushAsync(outputWriter);
        }

        internal async Task<Http2FrameWithPayload> ReceiveFrameAsync(uint maxFrameSize = Http2PeerSettings.DefaultMaxFrameSize)
        {
            var frame = new Http2FrameWithPayload();

            while (true)
            {
                var result = await _pair.Application.Input.ReadAsync().AsTask().DefaultTimeout();
                var buffer = result.Buffer;
                var consumed = buffer.Start;
                var examined = buffer.Start;

                try
                {
                    Assert.True(buffer.Length > 0);

                    if (Http2FrameReader.TryReadFrame(ref buffer, frame, maxFrameSize, out var framePayload))
                    {
                        consumed = examined = framePayload.End;
                        frame.Payload = framePayload.ToArray();
                        return frame;
                    }
                    else
                    {
                        examined = buffer.End;
                    }

                    if (result.IsCompleted)
                    {
                        throw new IOException("The reader completed without returning a frame.");
                    }
                }
                finally
                {
                    _bytesReceived += buffer.Slice(buffer.Start, consumed).Length;
                    _pair.Application.Input.AdvanceTo(consumed, examined);
                }
            }
        }

        internal async Task<Http2FrameWithPayload> ExpectAsync(Http2FrameType type, int withLength, byte withFlags, int withStreamId)
        {
            var frame = await ReceiveFrameAsync((uint)withLength);

            Assert.Equal(type, frame.Type);
            Assert.Equal(withLength, frame.PayloadLength);
            Assert.Equal(withFlags, frame.Flags);
            Assert.Equal(withStreamId, frame.StreamId);

            return frame;
        }

        public async Task StopConnectionAsync(int expectedLastStreamId, bool ignoreNonGoAwayFrames)
        {
            await SendGoAwayAsync();
            await WaitForConnectionStopAsync(expectedLastStreamId, ignoreNonGoAwayFrames);

            _pair.Application.Output.Complete();
        }

        public Task WaitForConnectionStopAsync(int expectedLastStreamId, bool ignoreNonGoAwayFrames)
        {
            return WaitForConnectionErrorAsync<Exception>(ignoreNonGoAwayFrames, expectedLastStreamId, Http2ErrorCode.NO_ERROR);
        }

        internal Task ReceiveHeadersAsync(int expectedStreamId, Action<IDictionary<string, string>> verifyHeaders = null)
            => ReceiveHeadersAsync(expectedStreamId, endStream: false, verifyHeaders);

        internal async Task ReceiveHeadersAsync(int expectedStreamId, bool endStream = false, Action<IDictionary<string, string>> verifyHeaders = null)
        {
            var headersFrame = await ReceiveFrameAsync();
            Assert.Equal(Http2FrameType.HEADERS, headersFrame.Type);
            Assert.Equal(expectedStreamId, headersFrame.StreamId);
            Assert.True((headersFrame.Flags & (byte)Http2HeadersFrameFlags.END_HEADERS) != 0);
            Assert.Equal(endStream, (headersFrame.Flags & (byte)Http2HeadersFrameFlags.END_STREAM) != 0);
            Logger.LogInformation("Received headers in a single frame.");

            ResetHeaders();
            DecodeHeaders(headersFrame);
            verifyHeaders?.Invoke(_decodedHeaders);
        }

        internal static void VerifyDataFrame(Http2Frame frame, int expectedStreamId, bool endOfStream, int length)
        {
            Assert.Equal(Http2FrameType.DATA, frame.Type);
            Assert.Equal(expectedStreamId, frame.StreamId);
            Assert.Equal(endOfStream ? Http2DataFrameFlags.END_STREAM : Http2DataFrameFlags.NONE, frame.DataFlags);
            Assert.Equal(length, frame.PayloadLength);
        }

        internal void VerifyGoAway(Http2Frame frame, int expectedLastStreamId, Http2ErrorCode expectedErrorCode)
        {
            Assert.Equal(Http2FrameType.GOAWAY, frame.Type);
            Assert.Equal(8, frame.PayloadLength);
            Assert.Equal(0, frame.Flags);
            Assert.Equal(0, frame.StreamId);
            Assert.Equal(expectedLastStreamId, frame.GoAwayLastStreamId);
            Assert.Equal(expectedErrorCode, frame.GoAwayErrorCode);
        }

        internal static void VerifyResetFrame(Http2Frame frame, int expectedStreamId, Http2ErrorCode expectedErrorCode)
        {
            Assert.Equal(Http2FrameType.RST_STREAM, frame.Type);
            Assert.Equal(expectedStreamId, frame.StreamId);
            Assert.Equal(expectedErrorCode, frame.RstStreamErrorCode);
            Assert.Equal(4, frame.PayloadLength);
            Assert.Equal(0, frame.Flags);
        }

        internal async Task WaitForConnectionErrorAsync<TException>(bool ignoreNonGoAwayFrames, int expectedLastStreamId, Http2ErrorCode expectedErrorCode)
            where TException : Exception
        {
            await WaitForConnectionErrorAsyncDoNotCloseTransport<TException>(ignoreNonGoAwayFrames, expectedLastStreamId, expectedErrorCode);
            _pair.Application.Output.Complete();
        }

        internal async Task WaitForConnectionErrorAsyncDoNotCloseTransport<TException>(bool ignoreNonGoAwayFrames, int expectedLastStreamId, Http2ErrorCode expectedErrorCode)
            where TException : Exception
        {
            var frame = await ReceiveFrameAsync();

            if (ignoreNonGoAwayFrames)
            {
                while (frame.Type != Http2FrameType.GOAWAY)
                {
                    frame = await ReceiveFrameAsync();
                }
            }

            VerifyGoAway(frame, expectedLastStreamId, expectedErrorCode);
        }

        internal async Task WaitForStreamErrorAsync(int expectedStreamId, Http2ErrorCode expectedErrorCode)
        {
            var frame = await ReceiveFrameAsync();

            Assert.Equal(Http2FrameType.RST_STREAM, frame.Type);
            Assert.Equal(4, frame.PayloadLength);
            Assert.Equal(0, frame.Flags);
            Assert.Equal(expectedStreamId, frame.StreamId);
            Assert.Equal(expectedErrorCode, frame.RstStreamErrorCode);
        }

        public void OnStaticIndexedHeader(int index)
        {
            throw new NotImplementedException();
        }

        public void OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
        {
            throw new NotImplementedException();
        }

        internal class Http2FrameWithPayload : Http2Frame
        {
            public Http2FrameWithPayload() : base()
            {
            }

            // This does not contain extended headers
            public Memory<byte> Payload { get; set; }

            public ReadOnlySequence<byte> PayloadSequence => new ReadOnlySequence<byte>(Payload);
        }

        private static class Assert
        {
            public static void True(bool condition, string message = "")
            {
                if (!condition)
                {
                    throw new Exception($"Assert.True failed: '{message}'");
                }
            }

            public static void Equal<T>(T expected, T actual)
            {
                if (!expected.Equals(actual))
                {
                    throw new Exception($"Assert.Equal('{expected}', '{actual}') failed");
                }
            }

            public static void Equal(string expected, string actual, bool ignoreCase = false)
            {
                if (!expected.Equals(actual, ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture))
                {
                    throw new Exception($"Assert.Equal('{expected}', '{actual}') failed");
                }
            }

            public static void NotEqual<T>(T value1, T value2)
            {
                if (value1.Equals(value2))
                {
                    throw new Exception($"Assert.NotEqual('{value1}', '{value2}') failed");
                }
            }

            public static void Contains<T>(IEnumerable<T> collection, T value)
            {
                if (!collection.Contains(value))
                {
                    throw new Exception($"Assert.Contains(collection, '{value}') failed");
                }
            }
        }
    }
}
