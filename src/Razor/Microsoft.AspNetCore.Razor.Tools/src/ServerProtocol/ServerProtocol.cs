// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal static class ServerProtocol
    {
        /// <summary>
        /// The version number for this protocol.
        /// </summary>
        public static readonly uint ProtocolVersion = 2;

        /// <summary>
        /// Read a string from the Reader where the string is encoded
        /// as a length prefix (signed 32-bit integer) followed by
        /// a sequence of characters.
        /// </summary>
        public static string ReadLengthPrefixedString(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            return new string(reader.ReadChars(length));
        }

        /// <summary>
        /// Write a string to the Writer where the string is encoded
        /// as a length prefix (signed 32-bit integer) follows by
        /// a sequence of characters.
        /// </summary>
        public static void WriteLengthPrefixedString(BinaryWriter writer, string value)
        {
            writer.Write(value.Length);
            writer.Write(value.ToCharArray());
        }

        /// <summary>
        /// This task does not complete until we are completely done reading.
        /// </summary>
        internal static async Task ReadAllAsync(
            Stream stream,
            byte[] buffer,
            int count,
            CancellationToken cancellationToken)
        {
            var totalBytesRead = 0;
            do
            {
                ServerLogger.Log("Attempting to read {0} bytes from the stream", count - totalBytesRead);
                var bytesRead = await stream.ReadAsync(
                    buffer,
                    totalBytesRead,
                    count - totalBytesRead,
                    cancellationToken)
                    .ConfigureAwait(false);

                if (bytesRead == 0)
                {
                    ServerLogger.Log("Unexpected -- read 0 bytes from the stream.");
                    throw new EndOfStreamException("Reached end of stream before end of read.");
                }
                ServerLogger.Log("Read {0} bytes", bytesRead);
                totalBytesRead += bytesRead;
            } while (totalBytesRead < count);
            ServerLogger.Log("Finished read");
        }
    }
}
