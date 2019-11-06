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
        public MsQuicApi _api;
        private bool _disposed;
        private readonly MsQuicTransportContext _context;
        private IntPtr _nativeObjPtr;
        private static GCHandle _handle;
        private readonly IntPtr _unmanagedFnPtrForNativeCallback;

        private readonly Channel<MsQuicStream> _acceptQueue = Channel.CreateUnbounded<MsQuicStream>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });

        public MsQuicConnection(MsQuicApi api, MsQuicTransportContext context, IntPtr nativeObjPtr)
        {
            _api = api;
            _context = context;
            _nativeObjPtr = nativeObjPtr;

            _unmanagedFnPtrForNativeCallback = Marshal.GetFunctionPointerForDelegate((ConnectionCallbackDelegate)NativeCallbackHandler);

            SetCallbackHandler();

            SetIdleTimeout(_context.Options.IdleTimeout);

            Features.Set<IQuicStreamListenerFeature>(this);
            Features.Set<IQuicCreateStreamFeature>(this);
        }

        internal uint HandleEvent(ref ConnectionEvent connectionEvent)
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

                default:
                    break;
            }
            return status;
        }

        protected virtual uint HandleEventConnected(ConnectionEvent connectionEvent)
        {
            return MsQuicConstants.Success;
        }

        protected virtual uint HandleEventShutdownBegin(ConnectionEvent connectionEvent)
        {
            return MsQuicConstants.Success;
        }

        protected virtual uint HandleEventShutdownBeginPeer(ConnectionEvent connectionEvent)
        {
            return MsQuicConstants.Success;
        }

        protected virtual uint HandleEventShutdownComplete(ConnectionEvent connectionEvent)
        {
            return MsQuicConstants.Success;
        }

        protected virtual uint HandleEventNewStream(ConnectionEvent connectionEvent)
        {
            var msQuicStream = new MsQuicStream(_api, this, _context, connectionEvent.StreamFlags, connectionEvent.Data.NewStream.Stream);

            _acceptQueue.Writer.TryWrite(msQuicStream);

            return MsQuicConstants.Success;
        }

        protected virtual uint HandleEventStreamsAvailable(ConnectionEvent connectionEvent)
        {
            return MsQuicConstants.Success;
        }

        public async ValueTask<ConnectionContext> AcceptAsync()
        {
            if (await _acceptQueue.Reader.WaitToReadAsync())
            {
                if (_acceptQueue.Reader.TryRead(out var stream))
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
            await stream.StartAsync();
            return stream;
        }

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

        public ValueTask StartAsync(
            ushort family,
            string serverName,
            ushort serverPort)
        {
            var status = _api.ConnectionStartDelegate(
                _nativeObjPtr,
                family,
                serverName,
                serverPort);

            MsQuicStatusException.ThrowIfFailed(status);

            return new ValueTask();
        }

        public MsQuicStream StreamOpen(
            QUIC_STREAM_OPEN_FLAG flags)
        {
            var streamPtr = IntPtr.Zero;
            var status = _api.StreamOpenDelegate(
                _nativeObjPtr,
                (uint)flags,
                MsQuicStream.NativeCallbackHandler,
                IntPtr.Zero,
                out streamPtr);
            MsQuicStatusException.ThrowIfFailed(status);

            return new MsQuicStream(_api, this, _context, flags, streamPtr);
        }

        public void SetCallbackHandler()
        {
            _handle = GCHandle.Alloc(this);
            _api.SetCallbackHandlerDelegate(
                _nativeObjPtr,
                _unmanagedFnPtrForNativeCallback,
                GCHandle.ToIntPtr(_handle));
        }

        public void Shutdown(
            QUIC_CONNECTION_SHUTDOWN_FLAG Flags,
            ushort ErrorCode)
        {
            var status = _api.ConnectionShutdownDelegate(
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
                _api.ConnectionCloseDelegate?.Invoke(_nativeObjPtr);
            }

            _nativeObjPtr = IntPtr.Zero;
            _api = null;

            _handle.Free();
            _disposed = true;
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

        private void SetParam(
            QUIC_PARAM_CONN param,
            QuicBuffer buf)
        {
            MsQuicStatusException.ThrowIfFailed(_api.UnsafeSetParam(
                _nativeObjPtr,
                (uint)QUIC_PARAM_LEVEL.CONNECTION,
                (uint)param,
                buf));
        }
    }
}
