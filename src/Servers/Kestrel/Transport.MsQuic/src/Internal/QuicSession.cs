// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using static Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal.MsQuicNativeMethods;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    internal sealed class QuicSession : IDisposable
    {
        private bool _disposed = false;
        private IntPtr _nativeObjPtr;
        private MsQuicApi _registration;

        internal QuicSession(MsQuicApi registration, IntPtr nativeObjPtr)
        {
            _registration = registration;
            _nativeObjPtr = nativeObjPtr;
        }

        public async ValueTask<MsQuicConnection> ConnectionOpenAsync(IPEndPoint endpoint, MsQuicTransportContext context)
        {
            var status = _registration.ConnectionOpenDelegate(
                _nativeObjPtr,
                MsQuicConnection.NativeCallbackHandler,
                IntPtr.Zero,
                out var connectionPtr);

            MsQuicStatusException.ThrowIfFailed(status);

            var msQuicConnection = new MsQuicConnection(_registration, context, connectionPtr);

            await msQuicConnection.StartAsync((ushort)endpoint.AddressFamily, endpoint.Address.ToString(), (ushort)endpoint.Port);

            return msQuicConnection;
        }

        internal IntPtr ListenerOpen(ListenerCallbackDelegate callback)
        {
            var status = _registration.ListenerOpenDelegate(
                                        _nativeObjPtr,
                                        callback,
                                        IntPtr.Zero,
                                        out var listenerPointer
                                        );

            MsQuicStatusException.ThrowIfFailed(status);

            return listenerPointer;
        }

        public void ShutDown(
            QUIC_CONNECTION_SHUTDOWN_FLAG Flags,
            ushort ErrorCode)
        {
            _registration.SessionShutdownDelegate(
                _nativeObjPtr,
                (uint)Flags,
                ErrorCode);
        }

        public long Handle { get => (long)_nativeObjPtr; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void SetPeerBiDirectionalStreamCount(ushort count)
        {
            SetUshortParamter(QUIC_PARAM_SESSION.PEER_BIDI_STREAM_COUNT, count);
        }

        public void SetPeerUnidirectionalStreamCount(ushort count)
        {
            SetUshortParamter(QUIC_PARAM_SESSION.PEER_UNIDI_STREAM_COUNT, count);
        }

        private unsafe void SetUshortParamter(QUIC_PARAM_SESSION param, ushort count)
        {
            var buffer = new MsQuicNativeMethods.QuicBuffer()
            {
                Length = sizeof(ushort),
                Buffer = (byte*)&count
            };

            SetParam(param, buffer);
        }

        public void SetDisconnectTimeout(TimeSpan timeout)
        {
            SetULongParamter(QUIC_PARAM_SESSION.DISCONNECT_TIMEOUT, (ulong)timeout.TotalMilliseconds);
        }

        public void SetIdleTimeout(TimeSpan timeout)
        {
            SetULongParamter(QUIC_PARAM_SESSION.IDLE_TIMEOUT, (ulong)timeout.TotalMilliseconds);

        }
        private unsafe void SetULongParamter(QUIC_PARAM_SESSION param, ulong count)
        {
            var buffer = new MsQuicNativeMethods.QuicBuffer()
            {
                Length = sizeof(ulong),
                Buffer = (byte*)&count
            };
            SetParam(param, buffer);
        }

        private void SetParam(
          QUIC_PARAM_SESSION param,
          MsQuicNativeMethods.QuicBuffer buf)
        {
            MsQuicStatusException.ThrowIfFailed(_registration.UnsafeSetParam(
                _nativeObjPtr,
                (uint)QUIC_PARAM_LEVEL.SESSION,
                (uint)param,
                buf));
        }

        ~QuicSession()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _registration.SessionCloseDelegate?.Invoke(_nativeObjPtr);
            _nativeObjPtr = IntPtr.Zero;
            _registration = null;

            _disposed = true;
        }
    }
}
