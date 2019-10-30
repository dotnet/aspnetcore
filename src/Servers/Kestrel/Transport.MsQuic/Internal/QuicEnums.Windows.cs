// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if WINDOWS
namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    /// <summary>
    /// Represents the status of an operation by MSQuic
    /// This isn't an exhaustive list of HRESULT that can be returned.
    /// </summary>
    internal enum QUIC_STATUS : uint
    {
        SUCCESS = 0,
        PENDING = 0x703E5,
        CONTINUE = 0x704DE,
        OUT_OF_MEMORY = 0x8007000E,
        INVALID_PARAMETER = 0x80070057,
        INVALID_STATE = 0x8007139F,
        NOT_SUPPORTED = 0x80004002,
        NOT_FOUND = 0x80070490,
        BUFFER_TOO_SMALL = 0x8007007A,
        HANDSHAKE_FAILURE = 0x80410000,
        ABORTED = 0x80004004,
        ADDRESS_IN_USE = 0x80072740,
        CONNECTION_TIMEOUT = 0x800704CF,
        CONNECTION_IDLE = 0x800704D4,
        INTERNAL_ERROR = 0x80004005,
        SERVER_BUSY = 0x800704C9,
        PROTOCOL_ERROR = 0x800704CD,
        VER_NEG_ERROR = 0x80410001
    }

    internal static class StatusEx
    {
        internal static bool Succeeded(this QUIC_STATUS status)
        {
            return status >= (QUIC_STATUS)0x80000000;
        }
    }
}
#else
#endif
