// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{

    public unsafe static class NativeMethods
    {
        internal const string dllName = "msquic.dll";
        internal const CallingConvention Api = CallingConvention.Winapi;

        [DllImport(dllName)]
        internal static extern int MsQuicOpen(int version, out NativeRegistration* registration);

        [StructLayout(LayoutKind.Sequential)]
        internal struct NativeRegistration
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

        internal delegate QUIC_STATUS SetParamDelegate(
            IntPtr Handle,
            uint Level,
            uint Param,
            uint BufferLength,
            byte* Buffer);

        internal delegate QUIC_STATUS GetParamDelegate(
            IntPtr Handle,
            uint Level,
            uint Param,
            IntPtr BufferLength,
            IntPtr Buffer);

        internal delegate QUIC_STATUS RegistrationOpenDelegate(byte[] appName, out IntPtr RegistrationContext);

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

        [UnmanagedFunctionPointer(Api)]
        internal delegate void SecConfigCreateCompleteDelegate(IntPtr Context, QUIC_STATUS Status, IntPtr SecurityConfig);

        internal delegate QUIC_STATUS SecConfigCreateDelegate(
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

        [UnmanagedFunctionPointer(Api)]
        internal delegate QUIC_STATUS ListenerCallbackDelegate(
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

        internal delegate QUIC_STATUS ListenerStartDelegate(
            IntPtr listener,
            ref WinSockNativeMethods.SOCKADDR_INET localAddress);

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
            internal QUIC_STATUS Status;
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
            internal QUIC_NEW_STREAM_FLAG Flags;
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
            public QUIC_CONNECTION_EVENT Type;
            internal ConnectionEventDataUnion Data;

            public bool EarlyDataAccepted => Data.Connected.EarlyDataAccepted;
            public ulong NumBytes => Data.IdealSendBuffer.NumBytes;
            public IPEndPoint LocalAddress => null; // TODO
            public IPEndPoint PeerAddress => null; // TODO
            public QuicStream CreateNewStream(QuicApi registration)
            {
                return new QuicStream(registration, Data.NewStream.Stream, shouldOwnNativeObj: false);
            }

            public QUIC_STATUS ShutdownBeginStatus => Data.ShutdownBegin.Status;
            public ushort ShutdownBeginPeerStatus => Data.ShutdownBeginPeer.ErrorCode;
            public bool ShutdownTimedOut => Data.ShutdownComplete.TimedOut;
            public ushort BiDirectionalCount => Data.StreamsAvailable.BiDirectionalCount;
            public ushort UniDirectionalCount => Data.StreamsAvailable.UniDirectionalCount;
            public QUIC_NEW_STREAM_FLAG StreamFlags => Data.NewStream.Flags;
        }

        [UnmanagedFunctionPointer(Api)]
        internal delegate QUIC_STATUS ConnectionCallbackDelegate(
         IntPtr Connection,
         IntPtr Context,
         ref ConnectionEvent Event);

        internal delegate QUIC_STATUS ConnectionOpenDelegate(
            IntPtr Session,
            ConnectionCallbackDelegate Handler,
            IntPtr Context,
            out IntPtr Connection);

        internal delegate QUIC_STATUS ConnectionCloseDelegate(
            IntPtr Connection);

        internal delegate QUIC_STATUS ConnectionStartDelegate(
            IntPtr Connection,
            ushort Family,
            [MarshalAs(UnmanagedType.LPStr)]
            string ServerName,
            ushort ServerPort);

        internal delegate QUIC_STATUS ConnectionShutdownDelegate(
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
        public struct StreamEvent
        {
            public QUIC_STREAM_EVENT Type;
            internal StreamEventDataUnion Data;
            public uint ReceiveAbortError => Data.PeerRecvAbort.ErrorCode;
            public uint SendAbortError => Data.PeerSendAbort.ErrorCode;
            public ulong AbsoluteOffset => Data.Recv.AbsoluteOffset;
            public ulong TotalBufferLength => Data.Recv.TotalBufferLength;
            public void CopyToBuffer(Span<byte> buffer)
            {
                var length = (int)Data.Recv.Buffers[0].Length;
                new Span<byte>(Data.Recv.Buffers[0].Buffer, length).CopyTo(buffer);
            }
            public bool Canceled => Data.SendComplete.IsCanceled();
            public IntPtr ClientContext => Data.SendComplete.ClientContext;
            public bool GracefulShutdown => Data.SendShutdownComplete.Graceful;
        }

        [UnmanagedFunctionPointer(Api)]
        internal delegate QUIC_STATUS StreamCallbackDelegate(
            IntPtr Stream,
            IntPtr Context,
            ref StreamEvent Event);

        internal delegate QUIC_STATUS StreamOpenDelegate(
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
    }
}
