// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using static System.Net.Quic.Implementations.MsQuic.Internal.MsQuicNativeMethods;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal static class MsQuicParameterHelpers
    {
        internal static unsafe SOCKADDR_INET GetINetParam(MsQuicApi api, IntPtr nativeObject, uint level, uint param)
        {
            byte* ptr = stackalloc byte[sizeof(SOCKADDR_INET)];
            QuicBuffer buffer = new QuicBuffer
            {
                Length = (uint)sizeof(SOCKADDR_INET),
                Buffer = ptr
            };

            QuicExceptionHelpers.ThrowIfFailed(
                api.UnsafeGetParam(nativeObject, level, param, ref buffer),
                "Could not get SOCKADDR_INET.");

            return *(SOCKADDR_INET*)ptr;
        }

        internal static unsafe ushort GetUShortParam(MsQuicApi api, IntPtr nativeObject, uint level, uint param)
        {
            byte* ptr = stackalloc byte[sizeof(ushort)];
            QuicBuffer buffer = new QuicBuffer()
            {
                Length = sizeof(ushort),
                Buffer = ptr
            };

            QuicExceptionHelpers.ThrowIfFailed(
                api.UnsafeGetParam(nativeObject, level, param, ref buffer),
                "Could not get ushort.");

            return *(ushort*)ptr;
        }

        internal static unsafe void SetUshortParam(MsQuicApi api, IntPtr nativeObject, uint level, uint param, ushort value)
        {
            QuicBuffer buffer = new QuicBuffer()
            {
                Length = sizeof(ushort),
                Buffer = (byte*)&value
            };

            QuicExceptionHelpers.ThrowIfFailed(
                api.UnsafeSetParam(nativeObject, level, param, buffer),
                "Could not set ushort.");
        }

        internal static unsafe ulong GetULongParam(MsQuicApi api, IntPtr nativeObject, uint level, uint param)
        {
            byte* ptr = stackalloc byte[sizeof(ulong)];
            QuicBuffer buffer = new QuicBuffer()
            {
                Length = sizeof(ulong),
                Buffer = ptr
            };

            QuicExceptionHelpers.ThrowIfFailed(
                api.UnsafeGetParam(nativeObject, level, param, ref buffer),
                "Could not get ulong.");

            return *(ulong*)ptr;
        }

        internal static unsafe void SetULongParam(MsQuicApi api, IntPtr nativeObject, uint level, uint param, ulong value)
        {
            QuicBuffer buffer = new QuicBuffer()
            {
                Length = sizeof(ulong),
                Buffer = (byte*)&value
            };

            QuicExceptionHelpers.ThrowIfFailed(
                api.UnsafeGetParam(nativeObject, level, param, ref buffer),
                "Could not set ulong.");
        }

        internal static unsafe void SetSecurityConfig(MsQuicApi api, IntPtr nativeObject, uint level, uint param, IntPtr value)
        {
            QuicBuffer buffer = new QuicBuffer()
            {
                Length = (uint)sizeof(void*),
                Buffer = (byte*)&value
            };

            QuicExceptionHelpers.ThrowIfFailed(
                api.UnsafeSetParam(nativeObject, level, param, buffer),
                "Could not set security configuration.");
        }
    }
}
