// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    public static partial class NativeMethods
    {
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct StreamEvent
        {
            private int _dummyPrimitive;
            public Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal.QUIC_STREAM_EVENT Type;
            public ulong AbsoluteOffset { get { throw null; } }
            public bool Canceled { get { throw null; } }
            public System.IntPtr ClientContext { get { throw null; } }
            public bool GracefulShutdown { get { throw null; } }
            public uint ReceiveAbortError { get { throw null; } }
            public uint SendAbortError { get { throw null; } }
            public ulong TotalBufferLength { get { throw null; } }
            public void CopyToBuffer(System.Span<byte> buffer) { }
        }
    }
    public partial class QuicStatusException : System.Exception
    {
        internal QuicStatusException() { }
        public override string Message { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal.QUIC_STATUS Status { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    [System.FlagsAttribute]
    public enum QUIC_CERT_HASH_STORE_FLAG : uint
    {
        NONE = (uint)0,
        MACHINE_CERT = (uint)1,
    }
    public enum QUIC_CONNECTION_EVENT : byte
    {
        CONNECTED = (byte)0,
        SHUTDOWN_BEGIN = (byte)1,
        SHUTDOWN_BEGIN_PEER = (byte)2,
        SHUTDOWN_COMPLETE = (byte)3,
        LOCAL_ADDR_CHANGED = (byte)4,
        PEER_ADDR_CHANGED = (byte)5,
        NEW_STREAM = (byte)6,
        STREAMS_AVAILABLE = (byte)7,
        PEER_NEEDS_STREAMS = (byte)8,
        IDEAL_SEND_BUFFER = (byte)9,
    }
    [System.FlagsAttribute]
    public enum QUIC_CONNECTION_SHUTDOWN : uint
    {
        NONE = (uint)0,
        SILENT = (uint)1,
    }
    public enum QUIC_LISTENER_EVENT : byte
    {
        NEW_CONNECTION = (byte)0,
    }
    [System.FlagsAttribute]
    public enum QUIC_NEW_STREAM_FLAG : uint
    {
        NONE = (uint)0,
        UNIDIRECTIONAL = (uint)1,
        FAIL_BLOCKED = (uint)2,
    }
    public enum QUIC_PARAM_CONN : uint
    {
        QUIC_VERSION = (uint)0,
        LOCAL_ADDRESS = (uint)1,
        REMOTE_ADDRESS = (uint)2,
        IDLE_TIMEOUT = (uint)3,
        PEER_BIDI_STREAM_COUNT = (uint)4,
        PEER_UNIDI_STREAM_COUNT = (uint)5,
        LOCAL_BIDI_STREAM_COUNT = (uint)6,
        LOCAL_UNIDI_STREAM_COUNT = (uint)7,
        CLOSE_REASON_PHRASE = (uint)8,
        STATISTICS = (uint)9,
        STATISTICS_PLAT = (uint)10,
        CERT_VALIDATION_FLAGS = (uint)11,
        KEEP_ALIVE_ENABLED = (uint)12,
        KEEP_ALIVE_INTERVAL = (uint)13,
        DISCONNECT_TIMEOUT = (uint)14,
        SEC_CONFIG = (uint)15,
        USE_SEND_BUFFER = (uint)16,
        USE_PACING = (uint)17,
        SHARE_UDP_BINDING = (uint)18,
        IDEAL_PROCESSOR = (uint)19,
        MAX_STREAM_IDS = (uint)20,
    }
    public enum QUIC_PARAM_LEVEL : uint
    {
        REGISTRATION = (uint)0,
        SESSION = (uint)1,
        LISTENER = (uint)2,
        CONNECTION = (uint)3,
        TLS = (uint)4,
        STREAM = (uint)5,
    }
    public enum QUIC_PARAM_LISTENER : uint
    {
        LOCAL_ADDRESS = (uint)0,
    }
    public enum QUIC_PARAM_REGISTRATION : uint
    {
        RETRY_MEMORY_PERCENT = (uint)0,
        CID_PREFIX = (uint)1,
    }
    public enum QUIC_PARAM_SESSION : uint
    {
        TLS_TICKET_KEY = (uint)0,
        PEER_BIDI_STREAM_COUNT = (uint)1,
        PEER_UNIDI_STREAM_COUNT = (uint)2,
        IDLE_TIMEOUT = (uint)3,
        DISCONNECT_TIMEOUT = (uint)4,
        MAX_BYTES_PER_KEY = (uint)5,
    }
    public enum QUIC_PARAM_STREAM : uint
    {
        ID = (uint)0,
        RECEIVE_ENABLED = (uint)1,
        ZERORTT_LENGTH = (uint)2,
        IDEAL_SEND_BUFFER = (uint)3,
    }
    [System.FlagsAttribute]
    public enum QUIC_RECV_FLAG : byte
    {
        NONE = (byte)0,
        ZERO_RTT = (byte)1,
    }
    [System.FlagsAttribute]
    public enum QUIC_SEC_CONFIG_FLAG : uint
    {
        NONE = (uint)0,
        CERT_HASH = (uint)1,
        CERT_HASH_STORE = (uint)2,
        CERT_CONTEXT = (uint)4,
        CERT_FILE = (uint)8,
        ENABL_OCSP = (uint)16,
        CERT_NULL = (uint)4026531840,
    }
    [System.FlagsAttribute]
    public enum QUIC_SEND_FLAG : uint
    {
        NONE = (uint)0,
        ALLOW_0_RTT = (uint)1,
        FIN = (uint)2,
    }
    public enum QUIC_STATUS : uint
    {
        SUCCESS = (uint)0,
        PENDING = (uint)459749,
        NOT_SUPPORTED = (uint)2147500034,
        ABORTED = (uint)2147500036,
        INTERNAL_ERROR = (uint)2147500037,
        OUT_OF_MEMORY = (uint)2147942414,
        INVALID_PARAMETER = (uint)2147942487,
        BUFFER_TOO_SMALL = (uint)2147942522,
        NOT_FOUND = (uint)2147943568,
        SERVER_BUSY = (uint)2147943625,
        PROTOCOL_ERROR = (uint)2147943629,
        CONNECTION_TIMEOUT = (uint)2147943631,
        CONNECTION_IDLE = (uint)2147943636,
        CONTINUE = (uint)2147943646,
        INVALID_STATE = (uint)2147947423,
        ADDRESS_IN_USE = (uint)2147952448,
        HANDSHAKE_FAILURE = (uint)2151743488,
        VER_NEG_ERROR = (uint)2151743489,
    }
    public enum QUIC_STREAM_EVENT : byte
    {
        START_COMPLETE = (byte)0,
        RECV = (byte)1,
        SEND_COMPLETE = (byte)2,
        PEER_SEND_CLOSE = (byte)3,
        PEER_SEND_ABORT = (byte)4,
        PEER_RECV_ABORT = (byte)5,
        SEND_SHUTDOWN_COMPLETE = (byte)6,
        SHUTDOWN_COMPLETE = (byte)7,
        IDEAL_SEND_BUFFER_SIZE = (byte)8,
    }
    [System.FlagsAttribute]
    public enum QUIC_STREAM_SHUTDOWN : uint
    {
        NONE = (uint)0,
        GRACEFUL = (uint)1,
        ABORT_SEND = (uint)2,
        ABORT_RECV = (uint)4,
        ABORT = (uint)6,
        IMMEDIATE = (uint)8,
    }
    [System.FlagsAttribute]
    public enum QUIC_STREAM_START : uint
    {
        NONE = (uint)0,
        FAIL_BLOCKED = (uint)1,
        IMMEDIATE = (uint)2,
    }
    public static partial class StatusEx
    {
        public static bool HasSucceeded(this Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal.QUIC_STATUS status) { throw null; }
    }
}
