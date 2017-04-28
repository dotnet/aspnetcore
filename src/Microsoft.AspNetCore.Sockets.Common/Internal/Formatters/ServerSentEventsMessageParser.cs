// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.AspNetCore.Sockets.Internal.Formatters
{
    public class ServerSentEventsMessageParser
    {
        private const byte ByteCR = (byte)'\r';
        private const byte ByteLF = (byte)'\n';
        private const byte ByteColon = (byte)':';
        private const byte ByteT = (byte)'T';
        private const byte ByteB = (byte)'B';
        private const byte ByteC = (byte)'C';
        private const byte ByteE = (byte)'E';

        private static byte[] _dataPrefix = Encoding.UTF8.GetBytes("data: ");
        private static byte[] _sseLineEnding = Encoding.UTF8.GetBytes("\r\n");
        private static byte[] _newLine = Encoding.UTF8.GetBytes(Environment.NewLine);

        private readonly static int _messageTypeLineLength = "data: X\r\n".Length;

        private InternalParseState _internalParserState = InternalParseState.ReadMessageType;
        private List<byte[]> _data = new List<byte[]>();
        private MessageType _messageType = MessageType.Text;

        public ParseResult ParseMessage(ReadableBuffer buffer, out ReadCursor consumed, out ReadCursor examined, out Message message)
        {
            consumed = buffer.Start;
            examined = buffer.End;
            message = new Message();
            var reader = new ReadableBufferReader(buffer);

            var start = consumed;
            var end = examined;

            while (!reader.End)
            {
                if (ReadCursorOperations.Seek(start, end, out var lineEnd, ByteLF) == -1)
                {
                    // For the case of  data: Foo\r\n\r\<Anytine except \n>
                    if (_internalParserState == InternalParseState.ReadEndOfMessage)
                    {
                        if(ConvertBufferToSpan(buffer.Slice(start, buffer.End)).Length > 1)
                        {
                            throw new FormatException("Expected a \\r\\n frame ending");
                        }
                    }

                    // Partial message. We need to read more.
                    return ParseResult.Incomplete;
                }

                lineEnd = buffer.Move(lineEnd, 1);
                var line = ConvertBufferToSpan(buffer.Slice(start, lineEnd));
                reader.Skip(line.Length);

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
                else if (line[line.Length - _sseLineEnding.Length] != ByteCR)
                {
                    throw new FormatException("Unexpected '\n' in message. A '\n' character can only be used as part of the newline sequence '\r\n'");
                }
                else
                {
                    EnsureStartsWithDataPrefix(line);
                }

                var payload = Array.Empty<byte>();
                switch (_internalParserState)
                {
                    case InternalParseState.ReadMessageType:
                        EnsureStartsWithDataPrefix(line);


                        _messageType = ParseMessageType(line);

                        _internalParserState = InternalParseState.ReadMessagePayload;

                        start = lineEnd;
                        consumed = lineEnd;
                        break;
                    case InternalParseState.ReadMessagePayload:
                        EnsureStartsWithDataPrefix(line);

                        // Slice away the 'data: '
                        var payloadLength = line.Length - (_dataPrefix.Length + _sseLineEnding.Length);
                        var newData = line.Slice(_dataPrefix.Length, payloadLength).ToArray();
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

                            if (_messageType != MessageType.Binary)
                            {
                                payloadSize += _newLine.Length*_data.Count;

                                // Allocate space in the paylod buffer for the data and the new lines.
                                // Subtract newLine length because we don't want a trailing newline.
                                payload = new byte[payloadSize - _newLine.Length];
                            }
                            else
                            {
                                payload = new byte[payloadSize];
                            }

                            var offset = 0;
                            foreach (var dataLine in _data)
                            {
                                dataLine.CopyTo(payload, offset);
                                offset += dataLine.Length;
                                if (offset < payload.Length && _messageType != MessageType.Binary)
                                {
                                    _newLine.CopyTo(payload, offset);
                                    offset += _newLine.Length;
                                }
                            }
                        }

                        if (_messageType == MessageType.Binary)
                        {
                            payload = MessageFormatUtils.DecodePayload(payload);
                        }

                        message = new Message(payload, _messageType);
                        consumed = lineEnd;
                        examined = consumed;
                        return ParseResult.Completed;
                }

                if (reader.Peek() == ByteCR)
                {
                    _internalParserState = InternalParseState.ReadEndOfMessage;
                }
            }
            return ParseResult.Incomplete;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Span<byte> ConvertBufferToSpan(ReadableBuffer buffer)
        {
            if (buffer.IsSingleSpan)
            {
                return buffer.First.Span;
            }
            return buffer.ToArray();
        }

        public void Reset()
        {
            _internalParserState = InternalParseState.ReadMessageType;
            _data.Clear();
        }

        private void EnsureStartsWithDataPrefix(ReadOnlySpan<byte> line)
        {
            if (!line.StartsWith(_dataPrefix))
            {
                throw new FormatException("Expected the message prefix 'data: '");
            }
        }

        private bool IsMessageEnd(ReadOnlySpan<byte> line)
        {
            return line.Length == _sseLineEnding.Length && line.SequenceEqual(_sseLineEnding);
        }

        private MessageType ParseMessageType(ReadOnlySpan<byte> line)
        {
            if (line.Length != _messageTypeLineLength)
            {
                throw new FormatException("Expected a data format message of the form 'data: <MesssageType>'");
            }

            // Skip the "data: " part of the line
            var type = line[_dataPrefix.Length];
            switch (type)
            {
                case ByteT:
                    return MessageType.Text;
                case ByteB:
                    return MessageType.Binary;
                case ByteC:
                    return MessageType.Close;
                case ByteE:
                    return MessageType.Error;
                default:
                    throw new FormatException($"Unknown message type: '{(char)type}'");
            }
        }

        public enum ParseResult
        {
            Completed,
            Incomplete,
        }

        private enum InternalParseState
        {
            ReadMessageType,
            ReadMessagePayload,
            ReadEndOfMessage,
            Error
        }
    }
}
