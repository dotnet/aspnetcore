// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.AspNetCore.SignalR.Internal.Protocol
{
    internal class Utf8BufferTextReader : TextReader
    {
        private ReadOnlyMemory<byte> _utf8Buffer;

        public Utf8BufferTextReader(ReadOnlyMemory<byte> utf8Buffer)
        {
            _utf8Buffer = utf8Buffer;
        }

        public override int Read(char[] buffer, int index, int count)
        {
            if (_utf8Buffer.IsEmpty)
            {
                return 0;
            }

            var source = _utf8Buffer.Span;
            var destination = new Span<char>(buffer, index, count);
            var destinationBytesCount = Encoding.UTF8.GetByteCount(buffer, index, count);

            // We have then the destination
            if (source.Length > destinationBytesCount)
            {
                source = source.Slice(0, destinationBytesCount);

                _utf8Buffer = _utf8Buffer.Slice(destinationBytesCount);
            }
            else
            {
                _utf8Buffer = ReadOnlyMemory<byte>.Empty;
            }

#if NETCOREAPP2_1
            return Encoding.UTF8.GetChars(source, destination);
#else
            unsafe
            {
                fixed (char* destinationChars = &MemoryMarshal.GetReference(destination))
                fixed (byte* sourceBytes = &MemoryMarshal.GetReference(source))
                {
                    return Encoding.UTF8.GetChars(sourceBytes, source.Length, destinationChars, destination.Length);
                }
            }
#endif
        }
    }
}
