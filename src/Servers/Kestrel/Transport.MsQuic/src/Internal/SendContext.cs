// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal.NativeMethods;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    public sealed class SendContext
    {
        private object _clientSendContext;
        private QuicBuffer[] _bufs;
        private MemoryHandle[] _bufferArrays;
        private GCHandle _gchBufs;
        private GCHandle _gchThis;

        private TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        internal unsafe SendContext(
            ReadOnlySequence<byte> sequence,
            object clientSendContext)
        {
            TcsTask = _tcs.Task;
            _clientSendContext = clientSendContext;
            foreach (var memory in sequence)
            {
                BufferCount++;
            }
            _bufferArrays = new MemoryHandle[BufferCount];
            _bufs = new QuicBuffer[BufferCount];
            var i = 0;
            foreach (var memory in sequence)
            {
                var handle = memory.Pin();
                _bufferArrays[i] = handle;
                _bufs[i].Length = (uint)memory.Length;
                _bufs[i].Buffer = (byte*)handle.Pointer;
                i++;
            }

            _gchBufs = GCHandle.Alloc(_bufs, GCHandleType.Pinned);

            Buffers = (QuicBuffer*)Marshal.UnsafeAddrOfPinnedArrayElement(_bufs, 0);

            _gchThis = GCHandle.Alloc(this);
        }

        internal unsafe QuicBuffer* Buffers { get; }

        internal uint BufferCount { get; }

        internal IntPtr NativeClientSendContext { get => (IntPtr)_gchThis; }

        internal Task TcsTask { get; }

        internal static object HandleNativeCallback(IntPtr nativeClientSendContext)
        {
            var gch = GCHandle.FromIntPtr(nativeClientSendContext);
            var sendContext = (SendContext)gch.Target;
            return sendContext.HandleCallback();
        }

        internal object HandleCallback()
        {
            _tcs.SetResult(null);
            _gchBufs.Free();
            foreach (var gchBufferArray in _bufferArrays)
            {
                gchBufferArray.Dispose();
            }
            _gchThis.Free();
            return _clientSendContext;
        }
    }
}
