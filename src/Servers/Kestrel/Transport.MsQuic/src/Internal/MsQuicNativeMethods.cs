// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    /// <summary>
    /// Contains all native delegates and structs that are used with MsQuic.
    /// </summary>
    internal unsafe static class MsQuicNativeMethods
    {
        internal const string dllName = "msquic.dll";

        [DllImport(dllName)]
        internal static extern int MsQuicOpen(int version, out NativeApi* registration);

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
        }

        internal delegate uint SetContextDelegate(
            IntPtr Handle,
            IntPtr Context);

        internal delegate IntPtr GetContextDelegate(
            IntPtr Handle);

        internal delegate void SetCallbackHandlerDelegate(
            IntPtr Handle,
            IntPtr Handler,
            IntPtr Context);

        internal delegate uint SetParamDelegate(
            IntPtr Handle,
            uint Level,
            uint Param,
            uint BufferLength,
            byte* Buffer);

        internal delegate uint GetParamDelegate(
            IntPtr Handle,
            uint Level,
            uint Param,
            IntPtr BufferLength,
            IntPtr Buffer);

        internal delegate uint RegistrationOpenDelegate(byte[] appName, out IntPtr RegistrationContext);

        internal delegate void RegistrationCloseDelegate(IntPtr RegistrationContext);

        [StructLayout(LayoutKind.Sequential)]
        internal struct CertHash
        {
            internal const int ShaHashLength = 20;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = ShaHashLength)]
            internal byte[] ShaHash;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CertHashStore
        {
            internal const int ShaHashLength = 20;
            internal const int StoreNameLength = 128;

            internal uint Flags;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = ShaHashLength)]
            internal byte[] ShaHash;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = StoreNameLength)]
            internal byte[] StoreName;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CertFile
        {
            [MarshalAs(UnmanagedType.ByValArray)]
            internal byte[] ShaHashUtf8;
            [MarshalAs(UnmanagedType.ByValArray)]
            internal byte[] StoreNameUtf8;
        }

        internal delegate void SecConfigCreateCompleteDelegate(IntPtr Context, uint Status, IntPtr SecurityConfig);

        internal delegate uint SecConfigCreateDelegate(
            IntPtr RegistrationContext,
            uint Flags,
            IntPtr Certificate,
            [MarshalAs(UnmanagedType.LPStr)]string Principal,
            IntPtr Context,
            SecConfigCreateCompleteDelegate CompletionHandler);

        internal delegate void SecConfigDeleteDelegate(
            IntPtr SecurityConfig);

        internal delegate uint SessionOpenDelegate(
            IntPtr RegistrationContext,
            byte[] utf8String,
            IntPtr Context,
            ref IntPtr Session);

        internal delegate void SessionCloseDelegate(
            IntPtr Session);

        internal delegate void SessionShutdownDelegate(
            IntPtr Session,
            uint Flags,
            ushort ErrorCode);

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

            internal static string BufferToString(IntPtr buffer, ushort bufferLength)
            {
                if (bufferLength == 0)
                {
                    return "";
                }

                var utf8Bytes = new byte[bufferLength]; // TODO: Avoid extra alloc and copy.
                Marshal.Copy(buffer, utf8Bytes, 0, bufferLength);
                var str = Encoding.UTF8.GetString(utf8Bytes);
                return str;
            }
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
            internal ushort ErrorCode;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ConnectionEventDataShutdownComplete
        {
            internal bool TimedOut;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ConnectionEventDataLocalAddrChanged
        {
            internal IntPtr Address; // TODO this needs to be IPV4 and IPV6
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ConnectionEventDataPeerAddrChanged
        {
            internal IntPtr Address; // TODO this needs to be IPV4 and IPV6
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
            internal IPEndPoint LocalAddress => null; // TODO
            internal IPEndPoint PeerAddress => null; // TODO
            internal uint ShutdownBeginStatus => Data.ShutdownBegin.Status;
            internal ushort ShutdownBeginPeerStatus => Data.ShutdownBeginPeer.ErrorCode;
            internal bool ShutdownTimedOut => Data.ShutdownComplete.TimedOut;
            internal ushort BiDirectionalCount => Data.StreamsAvailable.BiDirectionalCount;
            internal ushort UniDirectionalCount => Data.StreamsAvailable.UniDirectionalCount;
            internal QUIC_STREAM_OPEN_FLAG StreamFlags => Data.NewStream.Flags;
        }

        internal delegate uint ConnectionCallbackDelegate(
         IntPtr Connection,
         IntPtr Context,
         ref ConnectionEvent Event);

        internal delegate uint ConnectionOpenDelegate(
            IntPtr Session,
            ConnectionCallbackDelegate Handler,
            IntPtr Context,
            out IntPtr Connection);

        internal delegate uint ConnectionCloseDelegate(
            IntPtr Connection);

        internal delegate uint ConnectionStartDelegate(
            IntPtr Connection,
            ushort Family,
            [MarshalAs(UnmanagedType.LPStr)]
            string ServerName,
            ushort ServerPort);

        internal delegate uint ConnectionShutdownDelegate(
            IntPtr Connection,
            uint Flags,
            ushort ErrorCode);

        [StructLayout(LayoutKind.Sequential)]
        internal struct StreamEventDataRecv
        {
            internal ulong AbsoluteOffset;
            internal ulong TotalBufferLength;
            internal QuicBuffer* Buffers;
            internal uint BufferCount;
            internal byte Flags;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct StreamEventDataSendComplete
        {
            [FieldOffset(7)]
            internal byte Canceled;
            [FieldOffset(8)]
            internal IntPtr ClientContext;

            internal bool IsCanceled()
            {
                return Canceled != 0;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct StreamEventDataPeerSendAbort
        {
            internal ushort ErrorCode;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct StreamEventDataPeerRecvAbort
        {
            internal ushort ErrorCode;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct StreamEventDataSendShutdownComplete
        {
            internal bool Graceful;
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
            internal uint ReceiveAbortError => Data.PeerRecvAbort.ErrorCode;
            internal uint SendAbortError => Data.PeerSendAbort.ErrorCode;
            internal ulong AbsoluteOffset => Data.Recv.AbsoluteOffset;
            internal ulong TotalBufferLength => Data.Recv.TotalBufferLength;
            internal void CopyToBuffer(Span<byte> buffer)
            {
                var length = (int)Data.Recv.Buffers[0].Length;
                new Span<byte>(Data.Recv.Buffers[0].Buffer, length).CopyTo(buffer);
            }
            internal bool Canceled => Data.SendComplete.IsCanceled();
            internal IntPtr ClientContext => Data.SendComplete.ClientContext;
            internal bool GracefulShutdown => Data.SendShutdownComplete.Graceful;
        }

        internal delegate uint StreamCallbackDelegate(
            IntPtr Stream,
            IntPtr Context,
            ref StreamEvent Event);

        internal delegate uint StreamOpenDelegate(
            IntPtr Connection,
            uint Flags,
            StreamCallbackDelegate Handler,
            IntPtr Context,
            out IntPtr Stream);

        internal delegate uint StreamStartDelegate(
            IntPtr Stream,
            uint Flags
            );

        internal delegate uint StreamCloseDelegate(
            IntPtr Stream);

        internal delegate uint StreamShutdownDelegate(
            IntPtr Stream,
            uint Flags,
            ushort ErrorCode);

        internal delegate uint StreamSendDelegate(
            IntPtr Stream,
            QuicBuffer* Buffers,
            uint BufferCount,
            uint Flags,
            IntPtr ClientSendContext);

        internal delegate uint StreamReceiveCompleteDelegate(
            IntPtr Stream,
            ulong BufferLength);

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct QuicBuffer
        {
            internal uint Length;
            internal byte* Buffer;
        }

        public static SOCKADDR_INET Convert(IPEndPoint endpoint)
        {
            var lResult = ConvertToSocketAddrInet(endpoint.Address);
            SetPort(endpoint.Address.AddressFamily, ref lResult, endpoint.Port);
            return lResult;
        }

        private static void SetPort(AddressFamily addressFamily, ref SOCKADDR_INET socketAddrInet, int originalPort)
        {
            if (0 > originalPort || originalPort > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(originalPort), originalPort, "invalid port");
            }
            var convertedPort = (ushort)IPAddress.HostToNetworkOrder((short)originalPort);
            switch (addressFamily)
            {
                case AddressFamily.InterNetwork:
                    socketAddrInet.Ipv4.sin_port = convertedPort;
                    break;
                case AddressFamily.InterNetworkV6:
                    socketAddrInet.Ipv6.sin6_port = convertedPort;
                    break;
                default:
                    socketAddrInet.Ipv6.sin6_port = convertedPort;
                    break;
            }
        }

        public const ushort AF_INET = 2;
        public const ushort AF_INET6 = 23;
        public const ushort AF_UNSPEC = 0;

        public static SOCKADDR_INET ConvertToSocketAddrInet(IPAddress ipAddress)
        {
            var lResult = new SOCKADDR_INET();
            switch (ipAddress.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    lResult.Ipv4.sin_addr0 = ipAddress.GetAddressBytes()[0];
                    lResult.Ipv4.sin_addr1 = ipAddress.GetAddressBytes()[1];
                    lResult.Ipv4.sin_addr2 = ipAddress.GetAddressBytes()[2];
                    lResult.Ipv4.sin_addr3 = ipAddress.GetAddressBytes()[3];
                    lResult.Ipv4.sin_family = AF_UNSPEC;
                    break;
                case AddressFamily.InterNetworkV6:
                    lResult.Ipv6.sin6_addr0 = ipAddress.GetAddressBytes()[0];
                    lResult.Ipv6.sin6_addr1 = ipAddress.GetAddressBytes()[1];
                    lResult.Ipv6.sin6_addr2 = ipAddress.GetAddressBytes()[2];
                    lResult.Ipv6.sin6_addr3 = ipAddress.GetAddressBytes()[3];
                    lResult.Ipv6.sin6_addr4 = ipAddress.GetAddressBytes()[4];
                    lResult.Ipv6.sin6_addr5 = ipAddress.GetAddressBytes()[5];
                    lResult.Ipv6.sin6_addr6 = ipAddress.GetAddressBytes()[6];
                    lResult.Ipv6.sin6_addr7 = ipAddress.GetAddressBytes()[7];
                    lResult.Ipv6.sin6_addr8 = ipAddress.GetAddressBytes()[8];
                    lResult.Ipv6.sin6_addr9 = ipAddress.GetAddressBytes()[9];
                    lResult.Ipv6.sin6_addr10 = ipAddress.GetAddressBytes()[10];
                    lResult.Ipv6.sin6_addr11 = ipAddress.GetAddressBytes()[11];
                    lResult.Ipv6.sin6_addr12 = ipAddress.GetAddressBytes()[12];
                    lResult.Ipv6.sin6_addr13 = ipAddress.GetAddressBytes()[13];
                    lResult.Ipv6.sin6_addr14 = ipAddress.GetAddressBytes()[14];
                    lResult.Ipv6.sin6_addr15 = ipAddress.GetAddressBytes()[15];
                    lResult.Ipv6.sin6_family = AF_UNSPEC;
                    break;
                default:
                    throw new ArgumentException("Only IPv4 or IPv6 are supported");
            }
            return lResult;
        }

        public static IPAddress ConvertToIPAddress(SOCKADDR_INET addr)
        {
            switch (addr.si_family)
            {
                case AF_INET:
                    {
                        var result = new byte[4];
                        result[0] = addr.Ipv4.sin_addr0;
                        result[1] = addr.Ipv4.sin_addr1;
                        result[2] = addr.Ipv4.sin_addr2;
                        result[3] = addr.Ipv4.sin_addr3;
                        return new IPAddress(result);
                    }
                case AF_INET6:
                    {
                        var result = new byte[16];
                        result[0] = addr.Ipv6.sin6_addr0;
                        result[1] = addr.Ipv6.sin6_addr1;
                        result[2] = addr.Ipv6.sin6_addr2;
                        result[3] = addr.Ipv6.sin6_addr3;
                        result[4] = addr.Ipv6.sin6_addr4;
                        result[5] = addr.Ipv6.sin6_addr5;
                        result[6] = addr.Ipv6.sin6_addr6;
                        result[7] = addr.Ipv6.sin6_addr7;
                        result[8] = addr.Ipv6.sin6_addr8;
                        result[9] = addr.Ipv6.sin6_addr9;
                        result[10] = addr.Ipv6.sin6_addr10;
                        result[11] = addr.Ipv6.sin6_addr11;
                        result[12] = addr.Ipv6.sin6_addr12;
                        result[13] = addr.Ipv6.sin6_addr13;
                        result[14] = addr.Ipv6.sin6_addr14;
                        result[15] = addr.Ipv6.sin6_addr15;
                        return new IPAddress(result);
                    }
                default:
                    throw new ArgumentException("Address family not supported");
            }
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
            internal ushort sin6_family;
            internal ushort sin6_port;
            internal uint sin6_flowinfo;
            internal byte sin6_addr0;
            internal byte sin6_addr1;
            internal byte sin6_addr2;
            internal byte sin6_addr3;
            internal byte sin6_addr4;
            internal byte sin6_addr5;
            internal byte sin6_addr6;
            internal byte sin6_addr7;
            internal byte sin6_addr8;
            internal byte sin6_addr9;
            internal byte sin6_addr10;
            internal byte sin6_addr11;
            internal byte sin6_addr12;
            internal byte sin6_addr13;
            internal byte sin6_addr14;
            internal byte sin6_addr15;
            internal uint sin6_scope_id;

            internal byte[] Address
            {
                get
                {
                    return new byte[] {
                    sin6_addr0, sin6_addr1, sin6_addr2, sin6_addr3 ,
                    sin6_addr4, sin6_addr5, sin6_addr6, sin6_addr7 ,
                    sin6_addr8, sin6_addr9, sin6_addr10, sin6_addr11 ,
                    sin6_addr12, sin6_addr13, sin6_addr14, sin6_addr15 };
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
    }
}
