// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    /// <summary>
    /// Contains all native delegates and structs that are used with MsQuic.
    /// </summary>
    internal static unsafe class MsQuicNativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct NativeApi
        {
            internal uint Version;

            internal IntPtr SetContext;
            internal IntPtr GetContext;
            internal IntPtr SetCallbackHandler;

            internal IntPtr SetParam;
            internal IntPtr GetParam;

            internal IntPtr RegistrationOpen;
            internal IntPtr RegistrationClose;

            internal IntPtr SecConfigCreate;
            internal IntPtr SecConfigDelete;

            internal IntPtr SessionOpen;
            internal IntPtr SessionClose;
            internal IntPtr SessionShutdown;

            internal IntPtr ListenerOpen;
            internal IntPtr ListenerClose;
            internal IntPtr ListenerStart;
            internal IntPtr ListenerStop;

            internal IntPtr ConnectionOpen;
            internal IntPtr ConnectionClose;
            internal IntPtr ConnectionShutdown;
            internal IntPtr ConnectionStart;

            internal IntPtr StreamOpen;
            internal IntPtr StreamClose;
            internal IntPtr StreamStart;
            internal IntPtr StreamShutdown;
            internal IntPtr StreamSend;
            internal IntPtr StreamReceiveComplete;
            internal IntPtr StreamReceiveSetEnabled;
        }

        internal delegate uint SetContextDelegate(
            IntPtr handle,
            IntPtr context);

        internal delegate IntPtr GetContextDelegate(
            IntPtr handle);

        internal delegate void SetCallbackHandlerDelegate(
            IntPtr handle,
            Delegate del,
            IntPtr context);

        internal delegate uint SetParamDelegate(
            IntPtr handle,
            uint level,
            uint param,
            uint bufferLength,
            byte* buffer);

        internal delegate uint GetParamDelegate(
            IntPtr handle,
            uint level,
            uint param,
            uint* bufferLength,
            byte* buffer);

        internal delegate uint RegistrationOpenDelegate(byte[] appName, out IntPtr registrationContext);

        internal delegate void RegistrationCloseDelegate(IntPtr registrationContext);

        internal delegate void SecConfigCreateCompleteDelegate(IntPtr context, uint status, IntPtr securityConfig);

        internal delegate uint SecConfigCreateDelegate(
            IntPtr registrationContext,
            uint flags,
            IntPtr certificate,
            [MarshalAs(UnmanagedType.LPStr)]string? principal,
            IntPtr context,
            SecConfigCreateCompleteDelegate completionHandler);

        internal delegate void SecConfigDeleteDelegate(
            IntPtr securityConfig);

        internal delegate uint SessionOpenDelegate(
            IntPtr registrationContext,
            byte[] utf8String,
            IntPtr context,
            ref IntPtr session);

        internal delegate void SessionCloseDelegate(
            IntPtr session);

        internal delegate void SessionShutdownDelegate(
            IntPtr session,
            uint flags,
            ushort errorCode);

        [StructLayout(LayoutKind.Sequential)]
        internal struct ListenerEvent
        {
            internal QUIC_LISTENER_EVENT Type;
            internal ListenerEventDataUnion Data;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct ListenerEventDataUnion
        {
            [FieldOffset(0)]
            internal ListenerEventDataNewConnection NewConnection;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ListenerEventDataNewConnection
        {
            internal IntPtr Info;
            internal IntPtr Connection;
            internal IntPtr SecurityConfig;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NewConnectionInfo
        {
            internal uint QuicVersion;
            internal IntPtr LocalAddress;
            internal IntPtr RemoteAddress;
            internal ushort CryptoBufferLength;
            internal ushort AlpnListLength;
            internal ushort ServerNameLength;
            internal IntPtr CryptoBuffer;
            internal IntPtr AlpnList;
            internal IntPtr ServerName;
        }

        internal delegate uint ListenerCallbackDelegate(
            IntPtr listener,
            IntPtr context,
            ref ListenerEvent evt);

        internal delegate uint ListenerOpenDelegate(
           IntPtr session,
           ListenerCallbackDelegate handler,
           IntPtr context,
           out IntPtr listener);

        internal delegate uint ListenerCloseDelegate(
            IntPtr listener);

        internal delegate uint ListenerStartDelegate(
            IntPtr listener,
            ref SOCKADDR_INET localAddress);

        internal delegate uint ListenerStopDelegate(
            IntPtr listener);

        [StructLayout(LayoutKind.Sequential)]
        internal struct ConnectionEventDataConnected
        {
            internal bool EarlyDataAccepted;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ConnectionEventDataShutdownBegin
        {
            internal uint Status;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ConnectionEventDataShutdownBeginPeer
        {
            internal long ErrorCode;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ConnectionEventDataShutdownComplete
        {
            internal bool TimedOut;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ConnectionEventDataLocalAddrChanged
        {
            internal IntPtr Address;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ConnectionEventDataPeerAddrChanged
        {
            internal IntPtr Address;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ConnectionEventDataNewStream
        {
            internal IntPtr Stream;
            internal QUIC_STREAM_OPEN_FLAG Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ConnectionEventDataStreamsAvailable
        {
            internal ushort BiDirectionalCount;
            internal ushort UniDirectionalCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ConnectionEventDataIdealSendBuffer
        {
            internal ulong NumBytes;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct ConnectionEventDataUnion
        {
            [FieldOffset(0)]
            internal ConnectionEventDataConnected Connected;

            [FieldOffset(0)]
            internal ConnectionEventDataShutdownBegin ShutdownBegin;

            [FieldOffset(0)]
            internal ConnectionEventDataShutdownBeginPeer ShutdownBeginPeer;

            [FieldOffset(0)]
            internal ConnectionEventDataShutdownComplete ShutdownComplete;

            [FieldOffset(0)]
            internal ConnectionEventDataLocalAddrChanged LocalAddrChanged;

            [FieldOffset(0)]
            internal ConnectionEventDataPeerAddrChanged PeerAddrChanged;

            [FieldOffset(0)]
            internal ConnectionEventDataNewStream NewStream;

            [FieldOffset(0)]
            internal ConnectionEventDataStreamsAvailable StreamsAvailable;

            [FieldOffset(0)]
            internal ConnectionEventDataIdealSendBuffer IdealSendBuffer;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ConnectionEvent
        {
            internal QUIC_CONNECTION_EVENT Type;
            internal ConnectionEventDataUnion Data;

            internal bool EarlyDataAccepted => Data.Connected.EarlyDataAccepted;
            internal ulong NumBytes => Data.IdealSendBuffer.NumBytes;
            internal uint ShutdownBeginStatus => Data.ShutdownBegin.Status;
            internal long ShutdownBeginPeerStatus => Data.ShutdownBeginPeer.ErrorCode;
            internal bool ShutdownTimedOut => Data.ShutdownComplete.TimedOut;
            internal ushort BiDirectionalCount => Data.StreamsAvailable.BiDirectionalCount;
            internal ushort UniDirectionalCount => Data.StreamsAvailable.UniDirectionalCount;
            internal QUIC_STREAM_OPEN_FLAG StreamFlags => Data.NewStream.Flags;
        }

        internal delegate uint ConnectionCallbackDelegate(
            IntPtr connection,
            IntPtr context,
            ref ConnectionEvent connectionEvent);

        internal delegate uint ConnectionOpenDelegate(
            IntPtr session,
            ConnectionCallbackDelegate handler,
            IntPtr context,
            out IntPtr connection);

        internal delegate uint ConnectionCloseDelegate(
            IntPtr connection);

        internal delegate uint ConnectionStartDelegate(
            IntPtr connection,
            ushort family,
            [MarshalAs(UnmanagedType.LPStr)]
            string serverName,
            ushort serverPort);

        internal delegate uint ConnectionShutdownDelegate(
            IntPtr connection,
            uint flags,
            long errorCode);

        [StructLayout(LayoutKind.Sequential)]
        internal struct StreamEventDataRecv
        {
            internal ulong AbsoluteOffset;
            internal ulong TotalBufferLength;
            internal QuicBuffer* Buffers;
            internal uint BufferCount;
            internal uint Flags;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct StreamEventDataSendComplete
        {
            [FieldOffset(0)]
            internal byte Canceled;
            [FieldOffset(1)]
            internal IntPtr ClientContext;

            internal bool IsCanceled()
            {
                return Canceled != 0;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct StreamEventDataPeerSendAbort
        {
            internal long ErrorCode;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct StreamEventDataPeerRecvAbort
        {
            internal long ErrorCode;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct StreamEventDataSendShutdownComplete
        {
            internal byte Graceful;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct StreamEventDataUnion
        {
            [FieldOffset(0)]
            internal StreamEventDataRecv Recv;

            [FieldOffset(0)]
            internal StreamEventDataSendComplete SendComplete;

            [FieldOffset(0)]
            internal StreamEventDataPeerSendAbort PeerSendAbort;

            [FieldOffset(0)]
            internal StreamEventDataPeerRecvAbort PeerRecvAbort;

            [FieldOffset(0)]
            internal StreamEventDataSendShutdownComplete SendShutdownComplete;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct StreamEvent
        {
            internal QUIC_STREAM_EVENT Type;
            internal StreamEventDataUnion Data;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SOCKADDR_IN
        {
            internal ushort sin_family;
            internal ushort sin_port;
            internal byte sin_addr0;
            internal byte sin_addr1;
            internal byte sin_addr2;
            internal byte sin_addr3;

            internal byte[] Address
            {
                get
                {
                    return new byte[] { sin_addr0, sin_addr1, sin_addr2, sin_addr3 };
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SOCKADDR_IN6
        {
            internal ushort _family;
            internal ushort _port;
            internal uint _flowinfo;
            internal byte _addr0;
            internal byte _addr1;
            internal byte _addr2;
            internal byte _addr3;
            internal byte _addr4;
            internal byte _addr5;
            internal byte _addr6;
            internal byte _addr7;
            internal byte _addr8;
            internal byte _addr9;
            internal byte _addr10;
            internal byte _addr11;
            internal byte _addr12;
            internal byte _addr13;
            internal byte _addr14;
            internal byte _addr15;
            internal uint _scope_id;

            internal byte[] Address
            {
                get
                {
                    return new byte[] {
                    _addr0, _addr1, _addr2, _addr3,
                    _addr4, _addr5, _addr6, _addr7,
                    _addr8, _addr9, _addr10, _addr11,
                    _addr12, _addr13, _addr14, _addr15 };
                }
            }
        }

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi)]
        internal struct SOCKADDR_INET
        {
            [FieldOffset(0)]
            internal SOCKADDR_IN Ipv4;
            [FieldOffset(0)]
            internal SOCKADDR_IN6 Ipv6;
            [FieldOffset(0)]
            internal ushort si_family;
        }

        internal delegate uint StreamCallbackDelegate(
            IntPtr stream,
            IntPtr context,
            ref StreamEvent streamEvent);

        internal delegate uint StreamOpenDelegate(
            IntPtr connection,
            uint flags,
            StreamCallbackDelegate handler,
            IntPtr context,
            out IntPtr stream);

        internal delegate uint StreamStartDelegate(
            IntPtr stream,
            uint flags);

        internal delegate uint StreamCloseDelegate(
            IntPtr stream);

        internal delegate uint StreamShutdownDelegate(
            IntPtr stream,
            uint flags,
            long errorCode);

        internal delegate uint StreamSendDelegate(
            IntPtr stream,
            QuicBuffer* buffers,
            uint bufferCount,
            uint flags,
            IntPtr clientSendContext);

        internal delegate uint StreamReceiveCompleteDelegate(
            IntPtr stream,
            ulong bufferLength);

        internal delegate uint StreamReceiveSetEnabledDelegate(
            IntPtr stream,
            bool enabled);

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct QuicBuffer
        {
            internal uint Length;
            internal byte* Buffer;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CertFileParams
        {
            internal IntPtr PrivateKeyFilePath;
            internal IntPtr CertificateFilePath;
        }
    }
}
