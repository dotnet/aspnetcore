// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Abstractions.Features;
using Microsoft.AspNetCore.Connections.Features;
using static Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal.MsQuicNativeMethods;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    internal class MsQuicConnection : TransportConnection, IQuicStreamListenerFeature, IQuicCreateStreamFeature, IDisposable
    {
        private bool _disposed;
        private readonly MsQuicTransportContext _context;
        private IntPtr _nativeObjPtr;
        private static GCHandle _handle;
        private readonly IntPtr _unmanagedFnPtrForNativeCallback;
        private TaskCompletionSource<object> _tcsConnection;

        private readonly Channel<MsQuicStream> _acceptQueue = Channel.CreateUnbounded<MsQuicStream>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });

        public MsQuicConnection(MsQuicApi registration, MsQuicTransportContext context, IntPtr nativeObjPtr)
        {
            Registration = registration;
            _context = context;
            _nativeObjPtr = nativeObjPtr;

            ConnectionCallbackDelegate nativeCallback = NativeCallbackHandler;
            _unmanagedFnPtrForNativeCallback = Marshal.GetFunctionPointerForDelegate(nativeCallback);
            SetCallbackHandler();
            SetIdleTimeout(TimeSpan.FromMinutes(10));

            Features.Set<IQuicStreamListenerFeature>(this);
            Features.Set<IQuicCreateStreamFeature>(this);

            _handle = GCHandle.Alloc(this);
        }

        internal uint HandleEvent(ref MsQuicNativeMethods.ConnectionEvent connectionEvent)
        {
            var status = MsQuicConstants.Success;
            switch (connectionEvent.Type)
            {
                case QUIC_CONNECTION_EVENT.CONNECTED:
                    {
                        status = HandleEventConnected(
                            connectionEvent);
                    }
                    break;

                case QUIC_CONNECTION_EVENT.SHUTDOWN_BEGIN:
                    {
                        status = HandleEventShutdownBegin(
                            connectionEvent);
                    }
                    break;

                case QUIC_CONNECTION_EVENT.SHUTDOWN_BEGIN_PEER:
                    {
                        status = HandleEventShutdownBeginPeer(
                            connectionEvent);
                    }
                    break;

                case QUIC_CONNECTION_EVENT.SHUTDOWN_COMPLETE:
                    {
                        status = HandleEventShutdownComplete(
                            connectionEvent);
                    }
                    break;

                case QUIC_CONNECTION_EVENT.LOCAL_ADDR_CHANGED:
                    {
                        status = HandleEventLocalAddrChanged(
                            connectionEvent);
                    }
                    break;


                case QUIC_CONNECTION_EVENT.PEER_ADDR_CHANGED:
                    {
                        status = HandleEventPeerAddrChanged(
                            connectionEvent);
                    }
                    break;

                case QUIC_CONNECTION_EVENT.NEW_STREAM:
                    {
                        status = HandleEventNewStream(
                            connectionEvent);
                    }
                    break;

                case QUIC_CONNECTION_EVENT.STREAMS_AVAILABLE:
                    {
                        status = HandleEventStreamsAvailable(
                            connectionEvent);
                    }
                    break;

                case QUIC_CONNECTION_EVENT.PEER_NEEDS_STREAMS:
                    {
                    }
                    break;

                case QUIC_CONNECTION_EVENT.IDEAL_SEND_BUFFER:
                    {
                        status = HandleEventIdealSendBuffer(
                            connectionEvent);
                    }
                    break;

                default:
                    break;
            }
            return status;
        }

        protected virtual uint HandleEventConnected(
            MsQuicNativeMethods.ConnectionEvent connectionEvent)
        {
            return MsQuicConstants.Success;
        }

        protected virtual uint HandleEventShutdownBegin(
            MsQuicNativeMethods.ConnectionEvent connectionEvent)
        {
            return MsQuicConstants.Success;
        }

        protected virtual uint HandleEventShutdownBeginPeer(
            MsQuicNativeMethods.ConnectionEvent connectionEvent)
        {
            return MsQuicConstants.Success;
        }

        protected virtual uint HandleEventShutdownComplete(
            MsQuicNativeMethods.ConnectionEvent connectionEvent)
        {
            return MsQuicConstants.Success;
        }

        protected virtual uint HandleEventLocalAddrChanged(
            MsQuicNativeMethods.ConnectionEvent connectionEvent)
        {
            return MsQuicConstants.Success;
        }

        protected virtual uint HandleEventPeerAddrChanged(
            MsQuicNativeMethods.ConnectionEvent connectionEvent)
        {
            return MsQuicConstants.Success;
        }

        protected virtual uint HandleEventNewStream(
            MsQuicNativeMethods.ConnectionEvent connectionEvent)
        {
            //var stream = new QuicStream(connection.Registration, connectionEvent.Data.NewStream.Stream, shouldOwnNativeObj: false);
            var msQuicStream = new MsQuicStream(Registration, this, _context, connectionEvent.StreamFlags, connectionEvent.Data.NewStream.Stream);

            _acceptQueue.Writer.TryWrite(msQuicStream);

            return MsQuicConstants.Success;
        }

        protected virtual uint HandleEventStreamsAvailable(
            MsQuicNativeMethods.ConnectionEvent connectionEvent)
        {
            return MsQuicConstants.Success;
        }

        protected virtual uint HandleEventIdealSendBuffer(
            MsQuicNativeMethods.ConnectionEvent connectionEvent)
        {
            return MsQuicConstants.Success;
        }

        public async ValueTask<ConnectionContext> AcceptAsync()
        {
            while (await _acceptQueue.Reader.WaitToReadAsync())
            {
                while (_acceptQueue.Reader.TryRead(out var stream))
                {
                    return stream;
                }
            }

            return null;
        }

        public ValueTask<ConnectionContext> StartUnidirectionalStreamAsync()
        {
            return StartStreamAsync(QUIC_STREAM_OPEN_FLAG.UNIDIRECTIONAL);
        }

        public ValueTask<ConnectionContext> StartBidirectionalStreamAsync()
        {
            return StartStreamAsync(QUIC_STREAM_OPEN_FLAG.NONE);
        }

        private async ValueTask<ConnectionContext> StartStreamAsync(QUIC_STREAM_OPEN_FLAG flags)
        {
            var stream = StreamOpen(flags);
            await stream.StartAsync(QUIC_STREAM_START_FLAG.NONE);
            return stream;
        }


        public MsQuicApi Registration { get; set; }

        public unsafe void SetIdleTimeout(TimeSpan timeout)
        {
            var msTime = (ulong)timeout.TotalMilliseconds;
            var buffer = new QuicBuffer()
            {
                Length = sizeof(ulong),
                Buffer = (byte*)&msTime
            };
            SetParam(QUIC_PARAM_CONN.IDLE_TIMEOUT, buffer);
        }

        public void SetPeerBiDirectionalStreamCount(ushort count)
        {
            SetUshortParamter(QUIC_PARAM_CONN.PEER_BIDI_STREAM_COUNT, count);
        }

        public void SetPeerUnidirectionalStreamCount(ushort count)
        {
            SetUshortParamter(QUIC_PARAM_CONN.PEER_UNIDI_STREAM_COUNT, count);
        }

        public void SetLocalBidirectionalStreamCount(ushort count)
        {
            SetUshortParamter(QUIC_PARAM_CONN.LOCAL_BIDI_STREAM_COUNT, count);
        }

        public void SetLocalUnidirectionalStreamCount(ushort count)
        {
            SetUshortParamter(QUIC_PARAM_CONN.LOCAL_UNIDI_STREAM_COUNT, count);
        }

        private unsafe void SetUshortParamter(QUIC_PARAM_CONN param, ushort count)
        {
            var buffer = new QuicBuffer()
            {
                Length = sizeof(ushort),
                Buffer = (byte*)&count
            };
            SetParam(param, buffer);
        }

        public unsafe void EnableBuffering()
        {
            var val = true;
            var buffer = new QuicBuffer()
            {
                Length = sizeof(bool),
                Buffer = (byte*)&val
            };
            SetParam(QUIC_PARAM_CONN.USE_SEND_BUFFER, buffer);
        }

        public unsafe void DisableBuffering()
        {
            var val = false;
            var buffer = new QuicBuffer()
            {
                Length = sizeof(bool),
                Buffer = (byte*)&val
            };
            SetParam(QUIC_PARAM_CONN.USE_SEND_BUFFER, buffer);
        }

        public Task StartAsync(
            ushort family,
            string serverName,
            ushort serverPort)
        {
            _tcsConnection = new TaskCompletionSource<object>();

            var status = Registration.ConnectionStartDelegate(
                _nativeObjPtr,
                family,
                serverName,
                serverPort);

            MsQuicStatusException.ThrowIfFailed(status);

            return _tcsConnection.Task;
        }

        public MsQuicStream StreamOpen(
            QUIC_STREAM_OPEN_FLAG flags)
        {
            var streamPtr = IntPtr.Zero;
            var status = Registration.StreamOpenDelegate(
                _nativeObjPtr,
                (uint)flags,
                MsQuicStream.NativeCallbackHandler,
                IntPtr.Zero,
                out streamPtr);
            MsQuicStatusException.ThrowIfFailed(status);

            return new MsQuicStream(Registration, this, _context, flags, streamPtr);
        }

        public void SetCallbackHandler()
        {
            Registration.SetCallbackHandlerDelegate(
                _nativeObjPtr,
                _unmanagedFnPtrForNativeCallback,
                GCHandle.ToIntPtr(_handle));
        }

        public void Shutdown(
            QUIC_CONNECTION_SHUTDOWN_FLAG Flags,
            ushort ErrorCode)
        {
            var status = Registration.ConnectionShutdownDelegate(
                _nativeObjPtr,
                (uint)Flags,
                ErrorCode);
            MsQuicStatusException.ThrowIfFailed(status);
        }

        internal static uint NativeCallbackHandler(
            IntPtr connection,
            IntPtr context,
            ref ConnectionEvent connectionEventStruct)
        {
            var handle = GCHandle.FromIntPtr(context);
            var quicConnection = (MsQuicConnection)handle.Target;
            return quicConnection.HandleEvent(ref connectionEventStruct);
        }

        private void SetParam(
            QUIC_PARAM_CONN param,
            QuicBuffer buf)
        {
            MsQuicStatusException.ThrowIfFailed(Registration.UnsafeSetParam(
                _nativeObjPtr,
                (uint)QUIC_PARAM_LEVEL.CONNECTION,
                (uint)param,
                buf));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MsQuicConnection()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (_nativeObjPtr != IntPtr.Zero)
            {
                Registration.ConnectionCloseDelegate?.Invoke(_nativeObjPtr);
            }

            _nativeObjPtr = IntPtr.Zero;
            Registration = null;

            _handle.Free();
            _disposed = true;
        }
    }
}
