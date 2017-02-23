// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Binary;

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
            if (!TryFormatPayload(message.Payload, message.Type, buffer, out var writtenForPayload))
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

        private static bool TryFormatPayload(ReadOnlySpan<byte> payload, MessageType type, Span<byte> buffer, out int bytesWritten)
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
                var encodedSize = DataPrefix.Length + Base64.ComputeEncodedLength(payload.Length) + Newline.Length;
                if (buffer.Length < encodedSize)
                {
                    bytesWritten = 0;
                    return false;
                }
                DataPrefix.CopyTo(buffer);
                buffer = buffer.Slice(DataPrefix.Length);

                var encodedLength = Base64.Encode(payload, buffer);
                buffer = buffer.Slice(encodedLength);

                Newline.CopyTo(buffer);
                writtenSoFar += encodedSize;
                buffer.Slice(Newline.Length);
            }
            else
            {
                // We can't just use while(payload.Length > 0) because we need to write a blank final "data: " line
                // if the payload ends in a newline. For example, consider the following payload:
                //   "Hello\n"
                // It needs to be written as:
                //   data: Hello\r\n
                //   data: \r\n
                //   \r\n
                // Since we slice past the newline when we find it, after writing "Hello" in the previous example, we'll
                // end up with an empty payload buffer, BUT we need to write it as an empty 'data:' line, so we need
                // to use a condition that ensure the only time we stop writing is when we write the slice after the final
                // newline.
                var keepWriting = true;
                while (keepWriting)
                {
                    // Seek to the end of buffer or newline
                    var sliceEnd = payload.IndexOf(LineFeed);
                    var nextSliceStart = sliceEnd + 1;
                    if (sliceEnd < 0)
                    {
                        sliceEnd = payload.Length;
                        nextSliceStart = sliceEnd + 1;

                        // This is the last span
                        keepWriting = false;
                    }
                    if (sliceEnd > 0 && payload[sliceEnd - 1] == '\r')
                    {
                        sliceEnd--;
                    }

                    var slice = payload.Slice(0, sliceEnd);

                    if (nextSliceStart >= payload.Length)
                    {
                        payload = Span<byte>.Empty;
                    }
                    else
                    {
                        payload = payload.Slice(nextSliceStart);
                    }

                    if (!TryFormatLine(slice, buffer, out var writtenByLine))
                    {
                        bytesWritten = 0;
                        return false;
                    }
                    buffer = buffer.Slice(writtenByLine);
                    writtenSoFar += writtenByLine;
                }
            }

            bytesWritten = writtenSoFar;
            return true;
        }

        private static bool TryFormatLine(ReadOnlySpan<byte> line, Span<byte> buffer, out int bytesWritten)
        {
            // We're going to write the whole thing. HOWEVER, if the last byte is a '\r', we want to truncate it
            // because it was the '\r' in a '\r\n' newline sequence
            // This won't require an additional byte in the buffer because after this line we have to write a newline sequence anyway.
            var writtenSoFar = 0;
            if (buffer.Length < DataPrefix.Length + line.Length)
            {
                bytesWritten = 0;
                return false;
            }
            DataPrefix.CopyTo(buffer);
            writtenSoFar += DataPrefix.Length;
            buffer = buffer.Slice(DataPrefix.Length);

            line.CopyTo(buffer);
            var sliceTo = line.Length;
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
