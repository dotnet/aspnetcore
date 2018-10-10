// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MessagePack;

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis.Internal
{
    internal static class MessagePackUtil
    {
        public static int ReadArrayHeader(ref ReadOnlyMemory<byte> data)
        {
            var arr = GetArray(data);
            var val = MessagePackBinary.ReadArrayHeader(arr.Array, arr.Offset, out var readSize);
            data = data.Slice(readSize);
            return val;
        }

        public static int ReadMapHeader(ref ReadOnlyMemory<byte> data)
        {
            var arr = GetArray(data);
            var val = MessagePackBinary.ReadMapHeader(arr.Array, arr.Offset, out var readSize);
            data = data.Slice(readSize);
            return val;
        }

        public static string ReadString(ref ReadOnlyMemory<byte> data)
        {
            var arr = GetArray(data);
            var val = MessagePackBinary.ReadString(arr.Array, arr.Offset, out var readSize);
            data = data.Slice(readSize);
            return val;
        }

        public static byte[] ReadBytes(ref ReadOnlyMemory<byte> data)
        {
            var arr = GetArray(data);
            var val = MessagePackBinary.ReadBytes(arr.Array, arr.Offset, out var readSize);
            data = data.Slice(readSize);
            return val;
        }

        public static int ReadInt32(ref ReadOnlyMemory<byte> data)
        {
            var arr = GetArray(data);
            var val = MessagePackBinary.ReadInt32(arr.Array, arr.Offset, out var readSize);
            data = data.Slice(readSize);
            return val;
        }

        public static byte ReadByte(ref ReadOnlyMemory<byte> data)
        {
            var arr = GetArray(data);
            var val = MessagePackBinary.ReadByte(arr.Array, arr.Offset, out var readSize);
            data = data.Slice(readSize);
            return val;
        }

        private static ArraySegment<byte> GetArray(ReadOnlyMemory<byte> data)
        {
            var isArray = MemoryMarshal.TryGetArray(data, out var array);
            Debug.Assert(isArray);
            return array;
        }
    }
}
