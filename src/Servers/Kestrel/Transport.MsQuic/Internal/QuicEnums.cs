// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    /// <summary>
    /// Represents the status of an operation by MSQuic
    /// </summary>
    public enum QUIC_STATUS : uint
    {
        SUCCESS = 0,
        PENDING = 459749,
        CONTINUE = 0x800704DE,
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

    public static class StatusEx
    {
        public static bool HasSucceeded(this QUIC_STATUS status)
        {
            return status == QUIC_STATUS.SUCCESS || status == QUIC_STATUS.PENDING;
        }
    }

    /// <summary>
    /// Flags to pass when creating a certificate hash store. 
    /// </summary>
    [Flags]
    public enum QUIC_CERT_HASH_STORE_FLAG : uint
    {
        NONE = 0,
        MACHINE_CERT = 0x0001,
    }

    /// <summary>
    /// Flags to pass when creating a security config.
    /// </summary>
    [Flags]
    public enum QUIC_SEC_CONFIG_FLAG : uint
    {
        NONE = 0,
        CERT_HASH = 0x00000001,
        CERT_HASH_STORE = 0x00000002,
        CERT_CONTEXT = 0x00000004,
        CERT_FILE = 0x00000008,
        ENABL_OCSP = 0x00000010,
        CERT_NULL = 0xF0000000,
    }

    /// <summary>
    /// Event types that are returned from the <see cref="QuicListener"/>
    /// </summary>
    public enum QUIC_LISTENER_EVENT : byte
    {
        NEW_CONNECTION = 0
    }

    /// <summary>
    /// Event types that are returned from the <see cref="QuicConnection"/>
    /// </summary>
    public enum QUIC_CONNECTION_EVENT : byte
    {
        CONNECTED = 0,
        SHUTDOWN_BEGIN = 1,
        SHUTDOWN_BEGIN_PEER = 2,
        SHUTDOWN_COMPLETE = 3,
        LOCAL_ADDR_CHANGED = 4,
        PEER_ADDR_CHANGED = 5,
        NEW_STREAM = 6,
        STREAMS_AVAILABLE = 7,
        PEER_NEEDS_STREAMS = 8,
        IDEAL_SEND_BUFFER = 9,
    }

    /// <summary>
    /// Flags to pass to <see cref="QuicConnection.Shutdown(QUIC_CONNECTION_SHUTDOWN, ushort)"/>
    /// </summary>
    [Flags]
    public enum QUIC_CONNECTION_SHUTDOWN : uint
    {
        NONE = 0x0,
        SILENT = 0x1
    }

    /// <summary>
    /// Flags to pass for creating a <see cref="QuicRegistration"/>
    /// </summary>
    public enum QUIC_PARAM_REGISTRATION : uint
    {
        RETRY_MEMORY_PERCENT = 0,
        CID_PREFIX = 1
    }

    /// <summary>
    /// Flags to pass to specify which type a parameter is specified for.
    /// </summary>
    public enum QUIC_PARAM_LEVEL : uint
    {
        REGISTRATION = 0,
        SESSION = 1,
        LISTENER = 2,
        CONNECTION = 3,
        TLS = 4,
        STREAM = 5,
    }

    /// <summary>
    /// Flags to pass for creating a <see cref="QuicListener"/>
    /// </summary>
    public enum QUIC_PARAM_LISTENER : uint
    {
        LOCAL_ADDRESS = 0
    }

    /// <summary>
    /// Flags to pass for creating a <see cref="QuicSession"/>
    /// </summary>
    public enum QUIC_PARAM_SESSION : uint
    {
        TLS_TICKET_KEY = 0,
        PEER_BIDI_STREAM_COUNT = 1,
        PEER_UNIDI_STREAM_COUNT = 2,
        IDLE_TIMEOUT = 3,
        DISCONNECT_TIMEOUT = 4,
        MAX_BYTES_PER_KEY = 5
    }

    /// <summary>
    /// Flags to pass for creating a <see cref="QuicConnection"/>
    /// </summary>
    public enum QUIC_PARAM_CONN : uint
    {
        QUIC_VERSION = 0,
        LOCAL_ADDRESS = 1,
        REMOTE_ADDRESS = 2,
        IDLE_TIMEOUT = 3,
        PEER_BIDI_STREAM_COUNT = 4,
        PEER_UNIDI_STREAM_COUNT = 5,
        LOCAL_BIDI_STREAM_COUNT = 6,
        LOCAL_UNIDI_STREAM_COUNT = 7,
        CLOSE_REASON_PHRASE = 8,
        STATISTICS = 9,
        STATISTICS_PLAT = 10,
        CERT_VALIDATION_FLAGS = 11,
        KEEP_ALIVE_ENABLED = 12,
        KEEP_ALIVE_INTERVAL = 13,
        DISCONNECT_TIMEOUT = 14,
        SEC_CONFIG = 15,
        USE_SEND_BUFFER = 16,
        USE_PACING = 17,
        SHARE_UDP_BINDING = 18,
        IDEAL_PROCESSOR = 19,
        MAX_STREAM_IDS = 20
    }

    /// <summary>
    /// Flags to pass for creating a <see cref="QuicStream"/>
    /// </summary>
    public enum QUIC_PARAM_STREAM : uint
    {
        ID = 0,
        RECEIVE_ENABLED = 1,
        ZERORTT_LENGTH = 2,
        IDEAL_SEND_BUFFER = 3
    }

    /// <summary>
    /// Range of values for stream priority
    /// </summary>
    public enum QUIC_STREAM_PRIORITY : uint
    {
        DEFAULT = 0,
        MIN = 0x80000000,
        MAX = 0x7fffffff,
    }

    /// <summary>
    /// Flags obtained from a receive event on a <see cref="QuicStream"/>
    /// </summary>
    [Flags]
    public enum QUIC_RECV_FLAG : byte
    {
        NONE = 0,
        ZERO_RTT = 0x00000001
    }

    /// <summary>
    /// Event types that are returned from the <see cref="QuicStream"/>
    /// </summary>
    public enum QUIC_STREAM_EVENT : byte
    {
        START_COMPLETE = 0,
        RECV = 1,
        SEND_COMPLETE = 2,
        PEER_SEND_CLOSE = 3,
        PEER_SEND_ABORT = 4,
        PEER_RECV_ABORT = 5,
        SEND_SHUTDOWN_COMPLETE = 6,
        SHUTDOWN_COMPLETE = 7,
        IDEAL_SEND_BUFFER_SIZE = 8,
    }

    /// <summary>
    /// Flags to pass when creating a new <see cref="QuicStream"/>
    /// </summary>
    [Flags]
    public enum QUIC_NEW_STREAM_FLAG : uint
    {
        NONE = 0,
        UNIDIRECTIONAL = 0x1,
        FAIL_BLOCKED = 0x2,
    }

    /// <summary>
    /// Flags to pass when starting a <see cref="QuicStream"/>
    /// </summary>
    [Flags]
    public enum QUIC_STREAM_START : uint
    {
        NONE = 0,
        FAIL_BLOCKED = 0x1,
        IMMEDIATE = 0x2
    }

    /// <summary>
    /// Flags to pass when shuting down a <see cref="QuicStream"/>
    /// </summary>
    [Flags]
    public enum QUIC_STREAM_SHUTDOWN : uint
    {
        NONE = 0,
        GRACEFUL = 0x1,
        ABORT_SEND = 0x2,
        ABORT_RECV = 0x4,
        ABORT = 0x6,
        IMMEDIATE = 0x8
    }

    /// <summary>
    /// Flags to pass when sending data on a <see cref="QuicStream"/>
    /// </summary>
    [Flags]
    public enum QUIC_SEND_FLAG : uint
    {
        NONE = 0,
        ALLOW_0_RTT = 0x00000001,
        FIN = 0x00000002,
    }
}
