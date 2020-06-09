// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking
{
    internal static class PlatformApis
    {
        public static bool IsWindows { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static bool IsDarwin { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long VolatileRead(ref long value)
        {
            if (IntPtr.Size == 8)
            {
                return Volatile.Read(ref value);
            }
            else
            {
                // Avoid torn long reads on 32-bit
                return Interlocked.Read(ref value);
            }
        }
    }
}
