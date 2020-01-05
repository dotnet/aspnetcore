// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MessagePack;

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis.Internal
{
    //REVIEW : pretty sure this class can be deleted once the solution build since it's the same Api as MessagePackReader
    internal static class MessagePackUtil
    {
        public static int ReadArrayHeader(ref MessagePackReader reader) =>
            reader.ReadArrayHeader();

        public static int ReadMapHeader(ref MessagePackReader reader) =>
            reader.ReadMapHeader();

        public static string ReadString(ref MessagePackReader reader) =>
            reader.ReadString();

        public static byte[] ReadBytes(ref MessagePackReader reader) =>
            // REVIEW : is that ToArray necessary ?
            // REVIEW : Returning a "ReadOnlySequence<byte>?" have significant code change accross the repository, so for now i limited the impact here
            // REVIEW : As my understanding is limited, it will allocate only if multiple sequence are not one next to another in memory ?
            reader.ReadBytes()?.ToArray() ?? Array.Empty<byte>();

        public static int ReadInt32(ref MessagePackReader reader) =>
            reader.ReadInt32();

        public static byte ReadByte(ref MessagePackReader reader) =>
            reader.ReadByte();

    }
}
