// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;

namespace Microsoft.AspNetCore.Sockets.Internal.Formatters
{
    public static class ServerSentEventsMessageFormatter
    {
        private static readonly byte[] DataPrefix = new byte[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a', (byte)':', (byte)' ' };
        private static readonly byte[] Newline = new byte[] { (byte)'\r', (byte)'\n' };

        private const byte LineFeed = (byte)'\n';

        public static void WriteMessage(ReadOnlySpan<byte> payload, MemoryStream output)
        {
            // Write the payload
            WritePayload(payload, output);

            // Write new \r\n
            output.Write(Newline, 0, Newline.Length);
        }

        private static void WritePayload(ReadOnlySpan<byte> payload, Stream output)
        {
            // Short-cut for empty payload
            if (payload.Length == 0)
            {
                return;
            }

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
                    payload = ReadOnlySpan<byte>.Empty;
                }
                else
                {
                    payload = payload.Slice(nextSliceStart);
                }

                WriteLine(slice, output);
            }
        }

        private static void WriteLine(ReadOnlySpan<byte> payload, Stream output)
        {
            output.Write(DataPrefix, 0, DataPrefix.Length);

            var buffer = ArrayPool<byte>.Shared.Rent(payload.Length);
            payload.CopyTo(buffer);
            output.Write(buffer, 0, payload.Length);
            ArrayPool<byte>.Shared.Return(buffer);

            output.Write(Newline, 0, Newline.Length);
        }
    }
}
