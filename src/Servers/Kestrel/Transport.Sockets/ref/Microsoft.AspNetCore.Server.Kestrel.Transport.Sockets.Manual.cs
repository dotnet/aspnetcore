// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace System.Buffers
{
    internal partial class DiagnosticMemoryPool : System.Buffers.MemoryPool<byte>
    {        public DiagnosticMemoryPool(System.Buffers.MemoryPool<byte> pool, bool allowLateReturn = false, bool rentTracking = false) { }
        public bool IsDisposed { get { throw null; } }
        public override int MaxBufferSize { get { throw null; } }
        protected override void Dispose(bool disposing) { }
        public override System.Buffers.IMemoryOwner<byte> Rent(int size = -1) { throw null; }
        internal void ReportException(System.Exception exception) { }
        internal void Return(System.Buffers.DiagnosticPoolBlock block) { }
        public System.Threading.Tasks.Task WhenAllBlocksReturnedAsync(System.TimeSpan timeout) { throw null; }
    }
    internal sealed partial class DiagnosticPoolBlock : System.Buffers.MemoryManager<byte>
    {
        internal DiagnosticPoolBlock(System.Buffers.DiagnosticMemoryPool pool, System.Buffers.IMemoryOwner<byte> memoryOwner) { }
        public System.Diagnostics.StackTrace Leaser { get { throw null; } set { } }
        public override System.Memory<byte> Memory { get { throw null; } }
        protected override void Dispose(bool disposing) { }
        public override System.Span<byte> GetSpan() { throw null; }
        public override System.Buffers.MemoryHandle Pin(int byteOffset = 0) { throw null; }
        public void Track() { }
        protected override bool TryGetArray(out System.ArraySegment<byte> segment) { throw null; }
        public override void Unpin() { }
    }
    internal sealed partial class MemoryPoolBlock : System.Buffers.IMemoryOwner<byte>
    {
        internal MemoryPoolBlock(System.Buffers.SlabMemoryPool pool, System.Buffers.MemoryPoolSlab slab, int offset, int length) { }
        public System.Memory<byte> Memory { get { throw null; } }
        public System.Buffers.SlabMemoryPool Pool { get { throw null; } }
        public System.Buffers.MemoryPoolSlab Slab { get { throw null; } }
        public void Dispose() { }
        ~MemoryPoolBlock() { }
        public void Lease() { }
    }
    internal partial class MemoryPoolSlab : System.IDisposable
    {
        public MemoryPoolSlab(byte[] data) { }
        public byte[] Array { get { throw null; } }
        public bool IsActive { get { throw null; } }
        public System.IntPtr NativePointer { get { throw null; } }
        public static System.Buffers.MemoryPoolSlab Create(int length) { throw null; }
        public void Dispose() { }
        protected void Dispose(bool disposing) { }
        ~MemoryPoolSlab() { }
    }
    internal partial class MemoryPoolThrowHelper
    {
        public MemoryPoolThrowHelper() { }
        public static void ThrowArgumentOutOfRangeException(int sourceLength, int offset) { }
        public static void ThrowArgumentOutOfRangeException_BufferRequestTooLarge(int maxSize) { }
        public static void ThrowInvalidOperationException_BlockDoubleDispose(System.Buffers.DiagnosticPoolBlock block) { }
        public static void ThrowInvalidOperationException_BlockIsBackedByDisposedSlab(System.Buffers.DiagnosticPoolBlock block) { }
        public static void ThrowInvalidOperationException_BlockReturnedToDisposedPool(System.Buffers.DiagnosticPoolBlock block) { }
        public static void ThrowInvalidOperationException_BlocksWereNotReturnedInTime(int returned, int total, System.Buffers.DiagnosticPoolBlock[] blocks) { }
        public static void ThrowInvalidOperationException_DisposingPoolWithActiveBlocks(int returned, int total, System.Buffers.DiagnosticPoolBlock[] blocks) { }
        public static void ThrowInvalidOperationException_DoubleDispose() { }
        public static void ThrowInvalidOperationException_PinCountZero(System.Buffers.DiagnosticPoolBlock block) { }
        public static void ThrowInvalidOperationException_ReturningPinnedBlock(System.Buffers.DiagnosticPoolBlock block) { }
        public static void ThrowObjectDisposedException(System.Buffers.MemoryPoolThrowHelper.ExceptionArgument argument) { }
        internal enum ExceptionArgument
        {
            size = 0,
            offset = 1,
            length = 2,
            MemoryPoolBlock = 3,
            MemoryPool = 4,
        }
    }
    internal sealed partial class SlabMemoryPool : System.Buffers.MemoryPool<byte>
    {
        public SlabMemoryPool() { }
        public static int BlockSize { get { throw null; } }
        public override int MaxBufferSize { get { throw null; } }
        protected override void Dispose(bool disposing) { }
        internal void RefreshBlock(System.Buffers.MemoryPoolSlab slab, int offset, int length) { }
        public override System.Buffers.IMemoryOwner<byte> Rent(int size = -1) { throw null; }
        internal void Return(System.Buffers.MemoryPoolBlock block) { }
    }
    internal static partial class SlabMemoryPoolFactory
    {
        public static System.Buffers.MemoryPool<byte> Create() { throw null; }
        public static System.Buffers.MemoryPool<byte> CreateSlabMemoryPool() { throw null; }
    }
}
namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets
{
    public partial class SocketTransportOptions
    {
        internal System.Func<System.Buffers.MemoryPool<byte>> MemoryPoolFactory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}