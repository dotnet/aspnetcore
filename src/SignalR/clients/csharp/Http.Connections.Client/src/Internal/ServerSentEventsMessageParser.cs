// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.AspNetCore.Http.Connections.Client.Internal
{
    internal class ServerSentEventsMessageParser
    {
        private const byte ByteCR = (byte)'\r';
        private const byte ByteLF = (byte)'\n';
        private const byte ByteColon = (byte)':';

        // This uses C# compiler's ability to refer to static data directly. For more information see https://vcsjones.dev/2019/02/01/csharp-readonly-span-bytes-static
        private static ReadOnlySpan<byte> DataPrefix => new byte[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a', (byte)':', (byte)' ' };
        private static ReadOnlySpan<byte> SseLineEnding => new byte[] { (byte)'\r', (byte)'\n' };
        private static readonly byte[] _newLine = Encoding.UTF8.GetBytes(Environment.NewLine);

        private InternalParseState _internalParserState = InternalParseState.ReadMessagePayload;
        private readonly List<byte[]> _data = new List<byte[]>();

        public ParseResult ParseMessage(ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined, out byte[]? message)
        {
            consumed = buffer.Start;
            examined = buffer.End;
            message = null;

            var start = consumed;

            while (buffer.Length > 0)
            {
                if (!(buffer.PositionOf(ByteLF) is SequencePosition lineEnd))
                {
                    // For the case of data: Foo\r\n\r\<Anything except \n>
                    if (_internalParserState == InternalParseState.ReadEndOfMessage)
                    {
                        if (ConvertBufferToSpan(buffer.Slice(start, buffer.End)).Length > 1)
                        {
                            throw new FormatException("Expected a \\r\\n frame ending");
                        }
                    }

                    // Partial message. We need to read more.
                    return ParseResult.Incomplete;
                }

                lineEnd = buffer.GetPosition(1, lineEnd);
                var line = ConvertBufferToSpan(buffer.Slice(start, lineEnd));
                buffer = buffer.Slice(line.Length);

                if (line.Length <= 1)
                {
                    throw new FormatException("There was an error in the frame format");
                }

                // Skip comments
                if (line[0] == ByteColon)
                {
                    start = lineEnd;
                    consumed = lineEnd;
                    continue;
                }

                if (IsMessageEnd(line))
                {
                    _internalParserState = InternalParseState.ReadEndOfMessage;
                }

                // To ensure that the \n was preceded by a \r
                // since messages can't contain \n.
                // data: foo\n\bar should be encoded as
                // data: foo\r\n
                // data: bar\r\n
                else if (line[line.Length - SseLineEnding.Length] != ByteCR)
                {
                    throw new FormatException("Unexpected '\\n' in message. A '\\n' character can only be used as part of the newline sequence '\\r\\n'");
                }
                else
                {
                    EnsureStartsWithDataPrefix(line);
                }

                var payload = Array.Empty<byte>();
                switch (_internalParserState)
                {
                    case InternalParseState.ReadMessagePayload:
                        EnsureStartsWithDataPrefix(line);

                        // Slice away the 'data: '
                        var payloadLength = line.Length - (DataPrefix.Length + SseLineEnding.Length);
                        var newData = line.Slice(DataPrefix.Length, payloadLength).ToArray();
                        _data.Add(newData);

                        start = lineEnd;
                        consumed = lineEnd;
                        break;
                    case InternalParseState.ReadEndOfMessage:
                        if (_data.Count == 1)
                        {
                            payload = _data[0];
                        }
                        else if (_data.Count > 1)
                        {
                            // Find the final size of the payload
                            var payloadSize = 0;
                            foreach (var dataLine in _data)
                            {
                                payloadSize += dataLine.Length;
                            }

                            payloadSize += _newLine.Length * _data.Count;

                            // Allocate space in the payload buffer for the data and the new lines.
                            // Subtract newLine length because we don't want a trailing newline.
                            payload = new byte[payloadSize - _newLine.Length];

                            var offset = 0;
                            foreach (var dataLine in _data)
                            {
                                dataLine.CopyTo(payload, offset);
                                offset += dataLine.Length;
                                if (offset < payload.Length)
                                {
                                    _newLine.CopyTo(payload, offset);
                                    offset += _newLine.Length;
                                }
                            }
                        }

                        message = payload;
                        consumed = lineEnd;
                        examined = consumed;
                        return ParseResult.Completed;
                }

                if (buffer.Length > 0 && buffer.First.Span[0] == ByteCR)
                {
                    _internalParserState = InternalParseState.ReadEndOfMessage;
                }
            }
            return ParseResult.Incomplete;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlySpan<byte> ConvertBufferToSpan(in ReadOnlySequence<byte> buffer)
        {
            if (buffer.IsSingleSegment)
            {
                return buffer.First.Span;
            }
            return buffer.ToArray();
        }

        public void Reset()
        {
            _internalParserState = InternalParseState.ReadMessagePayload;
            _data.Clear();
        }

        private void EnsureStartsWithDataPrefix(ReadOnlySpan<byte> line)
        {
            if (!line.StartsWith(DataPrefix))
            {
                throw new FormatException("Expected the message prefix 'data: '");
            }
        }

        private bool IsMessageEnd(ReadOnlySpan<byte> line)
        {
            return line.Length == SseLineEnding.Length && line.SequenceEqual(SseLineEnding);
        }

        public enum ParseResult
        {
            Completed,
            Incomplete,
        }

        private enum InternalParseState
        {
            ReadMessagePayload,
            ReadEndOfMessage,
            Error
        }
    }
}
