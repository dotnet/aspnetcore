// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public class SocketInput
    {
        private readonly IMemoryPool _memory;
        private GCHandle _gcHandle;

        public SocketInput(IMemoryPool memory)
        {
            _memory = memory;
            Buffer = new ArraySegment<byte>(_memory.Empty, 0, 0);
        }

        public ArraySegment<byte> Buffer { get; set; }

        public bool RemoteIntakeFin { get; set; }


        public void Skip(int count)
        {
            Buffer = new ArraySegment<byte>(Buffer.Array, Buffer.Offset + count, Buffer.Count - count);
        }

        public ArraySegment<byte> Take(int count)
        {
            var taken = new ArraySegment<byte>(Buffer.Array, Buffer.Offset, count);
            Skip(count);
            return taken;
        }

        public void Free()
        {
            if (Buffer.Count == 0 && Buffer.Array.Length != 0)
            {
                _memory.FreeByte(Buffer.Array);
                Buffer = new ArraySegment<byte>(_memory.Empty, 0, 0);
            }
        }

        public ArraySegment<byte> Available(int minimumSize)
        {
            if (Buffer.Count == 0 && Buffer.Offset != 0)
            {
                Buffer = new ArraySegment<byte>(Buffer.Array, 0, 0);
            }

            var availableSize = Buffer.Array.Length - Buffer.Offset - Buffer.Count;

            if (availableSize < minimumSize)
            {
                if (availableSize + Buffer.Offset >= minimumSize)
                {
                    Array.Copy(Buffer.Array, Buffer.Offset, Buffer.Array, 0, Buffer.Count);
                    if (Buffer.Count != 0)
                    {
                        Buffer = new ArraySegment<byte>(Buffer.Array, 0, Buffer.Count);
                    }
                    availableSize = Buffer.Array.Length - Buffer.Offset - Buffer.Count;
                }
                else
                {
                    var largerSize = Buffer.Array.Length + Math.Max(Buffer.Array.Length, minimumSize);
                    var larger = new ArraySegment<byte>(_memory.AllocByte(largerSize), 0, Buffer.Count);
                    if (Buffer.Count != 0)
                    {
                        Array.Copy(Buffer.Array, Buffer.Offset, larger.Array, 0, Buffer.Count);
                    }
                    _memory.FreeByte(Buffer.Array);
                    Buffer = larger;
                    availableSize = Buffer.Array.Length - Buffer.Offset - Buffer.Count;
                }
            }
            return new ArraySegment<byte>(Buffer.Array, Buffer.Offset + Buffer.Count, availableSize);
        }

        public void Extend(int count)
        {
            Debug.Assert(count >= 0);
            Debug.Assert(Buffer.Offset >= 0);
            Debug.Assert(Buffer.Offset <= Buffer.Array.Length);
            Debug.Assert(Buffer.Offset + Buffer.Count <= Buffer.Array.Length);
            Debug.Assert(Buffer.Offset + Buffer.Count + count <= Buffer.Array.Length);

            Buffer = new ArraySegment<byte>(Buffer.Array, Buffer.Offset, Buffer.Count + count);
        }

        public IntPtr Pin(int minimumSize)
        {
            var segment = Available(minimumSize);
            _gcHandle = GCHandle.Alloc(segment.Array, GCHandleType.Pinned);
            return _gcHandle.AddrOfPinnedObject() + segment.Offset;
        }

        public void Unpin(int count)
        {
            // read_cb may called without an earlier alloc_cb 
            // this does not need to be thread-safe
            // IsAllocated is checked only because Unpin can be called redundantly
            if (_gcHandle.IsAllocated)
            {
                _gcHandle.Free();
                Extend(count);
            }
        }

    }
}
