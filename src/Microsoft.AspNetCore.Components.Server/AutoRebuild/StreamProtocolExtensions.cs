// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Server.AutoRebuild
{
    internal static class StreamProtocolExtensions
    {
        public static async Task WriteStringAsync(this Stream stream, string str)
        {
            var utf8Bytes = Encoding.UTF8.GetBytes(str);
            await stream.WriteAsync(BitConverter.GetBytes(utf8Bytes.Length), 0, 4);
            await stream.WriteAsync(utf8Bytes, 0, utf8Bytes.Length);
        }

        public static async Task WriteDateTimeAsync(this Stream stream, DateTime value)
        {
            var ticksBytes = BitConverter.GetBytes(value.Ticks);
            await stream.WriteAsync(ticksBytes, 0, 8);
        }

        public static async Task<bool> ReadBoolAsync(this Stream stream)
        {
            var responseBuf = new byte[1];
            await stream.ReadAsync(responseBuf, 0, 1);
            return responseBuf[0] == 1;
        }

        public static async Task<int> ReadIntAsync(this Stream stream)
        {
            var responseBuf = new byte[4];
            await stream.ReadAsync(responseBuf, 0, 4);
            return BitConverter.ToInt32(responseBuf, 0);
        }
    }
}
