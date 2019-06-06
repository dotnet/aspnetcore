// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.BlazorExtension
{
    internal static class StreamProtocolExtensions
    {
        public static async Task<string> ReadStringAsync(this Stream stream)
        {
            var length = BitConverter.ToInt32(await ReadBytesAsync(stream, 4), 0);
            var utf8Bytes = await ReadBytesAsync(stream, length);
            return Encoding.UTF8.GetString(utf8Bytes);
        }

        public static async Task<DateTime> ReadDateTimeAsync(this Stream stream)
        {
            var ticksBytes = await ReadBytesAsync(stream, 8);
            var ticks = BitConverter.ToInt64(ticksBytes, 0);
            return new DateTime(ticks);
        }

        public static async Task WriteBoolAsync(this Stream stream, bool value)
        {
            var byteVal = value ? (byte)1 : (byte)0;
            await stream.WriteAsync(new[] { byteVal }, 0, 1);
        }

        public static async Task WriteIntAsync(this Stream stream, int value)
        {
            await stream.WriteAsync(BitConverter.GetBytes(value), 0, 4);
        }

        private static async Task<byte[]> ReadBytesAsync(Stream stream, int exactLength)
        {
            var buf = new byte[exactLength];
            var bytesRead = 0;
            while (bytesRead < exactLength)
            {
                bytesRead += await stream.ReadAsync(buf, bytesRead, exactLength - bytesRead);
            }
            return buf;
        }
    }
}
