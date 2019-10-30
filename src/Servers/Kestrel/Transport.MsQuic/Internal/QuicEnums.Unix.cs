using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if UNIX
namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    /// <summary>
    /// Represents the status of an operation by MSQuic
    /// This isn't an exhaustive list of HRESULT that can be returned.
    /// </summary>
    internal enum QUIC_STATUS : ulong
    {
        SUCCESS = 0,
        PENDING = -2,
        CONTINUE = -1,
        OUT_OF_MEMORY = 12,
        INVALID_PARAMETER = 22,
        INVALID_STATE = 200000002,
        NOT_SUPPORTED = 95,
        NOT_FOUND = 2,
        BUFFER_TOO_SMALL = 75,
        HANDSHAKE_FAILURE = 200000009,
        ABORTED = 200000008,
        ADDRESS_IN_USE = 98,
        CONNECTION_TIMEOUT = 110,
        CONNECTION_IDLE = 200000011,
        INTERNAL_ERROR = 200000012,
        SERVER_BUSY = 200000007,
        PROTOCOL_ERROR = 200000013,
        VER_NEG_ERROR = 200000014,
        UNREACHABLE = 113,
        PERMISSION_DENIED = 1,
        EPOLL_ERROR = 200000015,
        DNS_RESOLUTION_ERROR = 200000016,
        SOCKET_ERROR = 200000017,
        SSL_ERROR = 200000018
    }

    internal static class StatusEx
    {
        internal static bool HasSucceeded(this QUIC_STATUS status)
        {
            return (long)status <= 0;
        }
    }
}
#else
#endif
