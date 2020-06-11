// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
