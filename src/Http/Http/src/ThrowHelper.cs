// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Http
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
    }
}
