// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Text;

namespace Microsoft.AspNetCore.Sockets.Formatters
{
    public static class ServerSentEventsMessageFormatter
    {
        private static readonly Span<byte> DataPrefix = new byte[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a', (byte)':', (byte)' ' };
        private static readonly Span<byte> Newline = new byte[] { (byte)'\r', (byte)'\n' };

        private const byte LineFeed = (byte)'\n';
        private const byte TextTypeFlag = (byte)'T';
        private const byte BinaryTypeFlag = (byte)'B';
        private const byte CloseTypeFlag = (byte)'C';
        private const byte ErrorTypeFlag = (byte)'E';

        public static bool TryFormatMessage(Message message, Span<byte> buffer, out int bytesWritten)
        {
            if (!message.EndOfMessage)
            {
                // This is a truely exceptional condition since we EXPECT callers to have already
                // buffered incomplete messages and synthesized the correct, complete message before
                // giving it to us. Hence we throw, instead of returning false.
                throw new InvalidOperationException("Cannot format message where endOfMessage is false using this format");
            }

            // Need at least: Length of 'data: ', one character type, one \r\n, and the trailing \r\n
            if (buffer.Length < DataPrefix.Length + 1 + Newline.Length + Newline.Length)
            {
                bytesWritten = 0;
                return false;
            }
            DataPrefix.CopyTo(buffer);
            buffer = buffer.Slice(DataPrefix.Length);
            if (!TryFormatType(buffer, message.Type))
            {
                bytesWritten = 0;
                return false;
            }
            buffer = buffer.Slice(1);

            Newline.CopyTo(buffer);
            buffer = buffer.Slice(Newline.Length);

            // Write the payload
            if (!TryFormatPayload(message.Payload.Buffer, message.Type, buffer, out var writtenForPayload))
            {
                bytesWritten = 0;
                return false;
            }
            buffer = buffer.Slice(writtenForPayload);

            if (buffer.Length < Newline.Length)
            {
                bytesWritten = 0;
                return false;
            }
            Newline.CopyTo(buffer);

            bytesWritten = DataPrefix.Length + Newline.Length + 1 + writtenForPayload + Newline.Length;
            return true;
        }

        private static bool TryFormatPayload(ReadableBuffer payload, MessageType type, Span<byte> buffer, out int bytesWritten)
        {
            // Short-cut for empty payload
            if (payload.Length == 0)
            {
                bytesWritten = 0;
                return true;
            }

            var writtenSoFar = 0;
            if (type == MessageType.Binary)
            {
                // TODO: We're going to need to fix this as part of https://github.com/aspnet/SignalR/issues/192
                var message = Convert.ToBase64String(payload.ToArray());
                var encodedSize = DataPrefix.Length + Encoding.UTF8.GetByteCount(message) + Newline.Length;
                if (buffer.Length < encodedSize)
                {
                    bytesWritten = 0;
                    return false;
                }
                DataPrefix.CopyTo(buffer);
                buffer = buffer.Slice(DataPrefix.Length);

                var array = Encoding.UTF8.GetBytes(message);
                array.CopyTo(buffer);
                buffer = buffer.Slice(array.Length);

                Newline.CopyTo(buffer);
                writtenSoFar += encodedSize;
                buffer.Slice(Newline.Length);
            }
            else
            {
                while (true)
                {
                    // Seek to the end of buffer or newline
                    var sliced = payload.TrySliceTo(LineFeed, out var slice, out var cursor);

                    if (!TryFormatLine(sliced ? slice : payload, buffer, out var writtenByLine))
                    {
                        bytesWritten = 0;
                        return false;
                    }
                    buffer = buffer.Slice(writtenByLine);
                    writtenSoFar += writtenByLine;

                    if (sliced)
                    {
                        payload = payload.Slice(payload.Move(cursor, 1));
                    }
                    else
                    {
                        break;
                    }
                }
            }

            bytesWritten = writtenSoFar;
            return true;
        }

        private static bool TryFormatLine(ReadableBuffer slice, Span<byte> buffer, out int bytesWritten)
        {
            // We're going to write the whole thing. HOWEVER, if the last byte is a '\r', we want to truncate it
            // because it was the '\r' in a '\r\n' newline sequence
            // This won't require an additional byte in the buffer because after this line we have to write a newline sequence anyway.
            var writtenSoFar = 0;
            if (buffer.Length < DataPrefix.Length + slice.Length)
            {
                bytesWritten = 0;
                return false;
            }
            DataPrefix.CopyTo(buffer);
            writtenSoFar += DataPrefix.Length;
            buffer = buffer.Slice(DataPrefix.Length);

            slice.CopyTo(buffer);
            var sliceTo = slice.Length;
            if (sliceTo > 0 && buffer[sliceTo - 1] == '\r')
            {
                sliceTo -= 1;
            }
            writtenSoFar += sliceTo;
            buffer = buffer.Slice(sliceTo);

            if (buffer.Length < Newline.Length)
            {
                bytesWritten = 0;
                return false;
            }
            writtenSoFar += Newline.Length;
            Newline.CopyTo(buffer);

            bytesWritten = writtenSoFar;
            return true;
        }

        private static bool TryFormatType(Span<byte> buffer, MessageType type)
        {
            switch (type)
            {
                case MessageType.Text:
                    buffer[0] = TextTypeFlag;
                    return true;
                case MessageType.Binary:
                    buffer[0] = BinaryTypeFlag;
                    return true;
                case MessageType.Close:
                    buffer[0] = CloseTypeFlag;
                    return true;
                case MessageType.Error:
                    buffer[0] = ErrorTypeFlag;
                    return true;
                default:
                    return false;
            }
        }
    }
}
