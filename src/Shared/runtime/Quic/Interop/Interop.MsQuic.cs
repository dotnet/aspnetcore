// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static class MsQuic
    {
        [DllImport(Libraries.MsQuic)]
        internal static unsafe extern uint MsQuicOpen(int version, out MsQuicNativeMethods.NativeApi* registration);
    }
}
