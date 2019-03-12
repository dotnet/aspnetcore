// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;

namespace System.IO.Pipelines
{
    internal static class ThrowHelper
    {
        public static void ThrowInvalidOperationException_NoReadingAllowed() => throw CreateInvalidOperationException_NoReadingAllowed();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateInvalidOperationException_NoReadingAllowed() => new InvalidOperationException("Reading is not allowed after reader was completed.");

        public static void ThrowInvalidOperationException_NoArrayFromMemory() => throw CreateInvalidOperationException_NoArrayFromMemory();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateInvalidOperationException_NoArrayFromMemory() => new InvalidOperationException("Could not get byte[] from Memory.");

        public static void ThrowInvalidOperationException_NoDataRead() => throw CreateInvalidOperationException_NoDataRead();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateInvalidOperationException_NoDataRead() => new InvalidOperationException("No data has been read into the StreamPipeReader.");

        public static void ThrowInvalidOperationException_SynchronousReadsDisallowed() => throw CreateInvalidOperationException_SynchronousReadsDisallowed();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateInvalidOperationException_SynchronousReadsDisallowed() => new InvalidOperationException("Synchronous operations are disallowed. Call ReadAsync or set allowSynchronousIO to true instead.");
        
        public static void ThrowInvalidOperationException_SynchronousWritesDisallowed() => throw CreateInvalidOperationException_SynchronousWritesDisallowed();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateInvalidOperationException_SynchronousWritesDisallowed() => new InvalidOperationException("Synchronous operations are disallowed. Call WriteAsync or set allowSynchronousIO to true instead.");
        
        public static void ThrowInvalidOperationException_SynchronousFlushesDisallowed() => throw CreateInvalidOperationException_SynchronousFlushesDisallowed();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateInvalidOperationException_SynchronousFlushesDisallowed() => new InvalidOperationException("Synchronous operations are disallowed. Call FlushAsync or set allowSynchronousIO to true instead.");

        public static void ThrowInvalidOperationException_DataNotAllFlushed() => throw CreateInvalidOperationException_DataNotAllFlushed();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateInvalidOperationException_DataNotAllFlushed() => new InvalidOperationException("Complete called without flushing the StreamPipeWriter. Call FlushAsync() before calling Complete().");

        public static void ThrowInvalidOperationException_NoWritingAllowed() => throw CreateInvalidOperationException_NoWritingAllowed();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateInvalidOperationException_NoWritingAllowed() => new InvalidOperationException("Writing is not allowed after writer was completed.");

        public static void ThrowArgumentOutOfRangeException(string argument) => throw CreateArgumentOutOfRangeException(argument);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateArgumentOutOfRangeException(string argument) => new ArgumentOutOfRangeException(argument);

        public static void ThrowOperationCanceledException_ReadCanceled() => throw CreateOperationCanceledException_ReadCanceled();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateOperationCanceledException_ReadCanceled() => new OperationCanceledException("Read was canceled on underlying PipeReader.");
    }
}
