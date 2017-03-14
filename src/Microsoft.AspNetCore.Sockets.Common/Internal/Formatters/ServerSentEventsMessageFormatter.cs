// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Binary;
using System.Buffers;
using System.Text;
using System.Text.Formatting;

namespace Microsoft.AspNetCore.Sockets.Internal.Formatters
{
    public static class ServerSentEventsMessageFormatter
    {
        private static readonly Span<byte> DataPrefix = new byte[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a', (byte)':', (byte)' ' };
        private static readonly Span<byte> Newline = new byte[] { (byte)'\r', (byte)'\n' };

        private const byte LineFeed = (byte)'\n';
        private const char TextTypeFlag = 'T';
        private const char BinaryTypeFlag = 'B';
        private const char CloseTypeFlag = 'C';
        private const char ErrorTypeFlag = 'E';

        public static bool TryWriteMessage(Message message, IOutput output)
        {
            if (!message.EndOfMessage)
            {
                // This is a truely exceptional condition since we EXPECT callers to have already
                // buffered incomplete messages and synthesized the correct, complete message before
                // giving it to us. Hence we throw, instead of returning false.
                throw new InvalidOperationException("Cannot format message where endOfMessage is false using this format");
            }

            var typeIndicator = GetTypeIndicator(message.Type);

            // Write the Data Prefix
            if (!output.TryWrite(DataPrefix))
            {
                return false;
            }

            // Write the type indicator
            output.Append(typeIndicator, TextEncoder.Utf8);

            if (!output.TryWrite(Newline))
            {
                return false;
            }

            // Write the payload
            if (!TryWritePayload(message.Payload, message.Type, output))
            {
                return false;
            }

            if (!output.TryWrite(Newline))
            {
                return false;
            }

            return true;
        }

        private static bool TryWritePayload(ReadOnlySpan<byte> payload, MessageType type, IOutput output)
        {
            // Short-cut for empty payload
            if (payload.Length == 0)
            {
                return true;
            }

            if (type == MessageType.Binary)
            {
                // TODO: Base64 writer that works with IOutput would be amazing!
                var arr = new byte[Base64.ComputeEncodedLength(payload.Length)];
                Base64.Encode(payload, arr);
                return TryWriteLine(arr, output);
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

                    if (!TryWriteLine(slice, output))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool TryWriteLine(ReadOnlySpan<byte> line, IOutput output)
        {
            if (!output.TryWrite(DataPrefix))
            {
                return false;
            }

            if (!output.TryWrite(line))
            {
                return false;
            }

            if (!output.TryWrite(Newline))
            {
                return false;
            }

            return true;
        }

        private static char GetTypeIndicator(MessageType type)
        {
            switch (type)
            {
                case MessageType.Text: return TextTypeFlag;
                case MessageType.Binary: return BinaryTypeFlag;
                case MessageType.Close: return CloseTypeFlag;
                case MessageType.Error: return ErrorTypeFlag;
                default: throw new FormatException($"Invalid Message Type: {type}");
            }
        }
    }
}