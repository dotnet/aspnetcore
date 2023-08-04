// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.AspNetCore.Http.Connections.Client.Internal;

internal sealed class ServerSentEventsMessageParser
{
    private const byte ByteCR = (byte)'\r';
    private const byte ByteLF = (byte)'\n';
    private const byte ByteColon = (byte)':';

    // This uses C# compiler's ability to refer to static data directly. For more information see https://vcsjones.dev/2019/02/01/csharp-readonly-span-bytes-static
    private static ReadOnlySpan<byte> DataPrefix => "data: "u8;
    private static ReadOnlySpan<byte> SseLineEnding => "\r\n"u8;
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
                // Partial message. We need to read more.
                return ParseResult.Incomplete;
            }

            // buffer, and thus line should atleast contain \n at this point.
            lineEnd = buffer.GetPosition(1, lineEnd);
            var line = ConvertBufferToSpan(buffer.Slice(start, lineEnd));
            buffer = buffer.Slice(line.Length);

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
                    var payloadLength = line.Length - DataPrefix.Length;
                    var lineWithEnding = line.Slice(DataPrefix.Length, payloadLength);
                    var newData = TrimEnding(lineWithEnding).ToArray();
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

    private static ReadOnlySpan<byte> TrimEnding(ReadOnlySpan<byte> lineWithEnding)
    {
        // Up above we ensure that we will have a line ending.
        // that can be either CRLF or LF
        return lineWithEnding.EndsWith(SseLineEnding)
            ? lineWithEnding.Slice(0, lineWithEnding.Length - 2) // CRLF
            : lineWithEnding.Slice(0, lineWithEnding.Length - 1); // LF
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<byte> ConvertBufferToSpan(in ReadOnlySequence<byte> buffer)
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

    private static void EnsureStartsWithDataPrefix(ReadOnlySpan<byte> line)
    {
        if (!line.StartsWith(DataPrefix))
        {
            throw new FormatException("Expected the message prefix 'data: '");
        }
    }

    private static bool IsMessageEnd(ReadOnlySpan<byte> line)
    {
        if (line.Length == 2)
        {
            return line[0] == ByteCR && line[1] == ByteLF;
        }
        else if (line.Length == 1)
        {
            return line[0] == ByteLF;
        }
        return false;
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
