// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using static Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal.QuicListener;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    // TODO I still need to finish a small refactor here to remove allocations per connection.
    internal sealed class QuicSession : IDisposable
    {
        private bool _disposed = false;
        private IntPtr _nativeObjPtr;
        private byte[] _alpn;
        private QuicRegistration _registration;

        internal QuicSession(QuicRegistration registration, IntPtr nativeObjPtr, byte[] alpn)
        {
            _registration = registration;
            _alpn = alpn;
            _nativeObjPtr = nativeObjPtr;
        }

        public QuicListener ListenerOpen(ListenerCallback callback)
        {
            var listenerPtr = IntPtr.Zero;
            var status = (QUIC_STATUS)_registration.ListenerOpenDelegate(
                _nativeObjPtr,
                NativeCallbackHandler,
                IntPtr.Zero,
                out listenerPtr
                );

            QuicStatusException.ThrowIfFailed(status);
            var listener = new QuicListener(
                _registration,
                listenerPtr,
                true);

            listener.SetCallbackHandler(callback);
            return listener;
        }

        public ValueTask<QuicConnection> ConnectionOpenAsync(
            QuicConnection.ConnectionCallback callback)
        {
            var status = _registration.ConnectionOpenDelegate(
                _nativeObjPtr,
                QuicConnection.NativeCallbackHandler,
                IntPtr.Zero,
                out var connectionPtr);

            QuicStatusException.ThrowIfFailed(status);
            var connection = new QuicConnection(_registration, connectionPtr, true);
            connection.SetCallbackHandler(callback);
            return new ValueTask<QuicConnection>(connection);
        }

        public void ShutDown(
            QUIC_CONNECTION_SHUTDOWN Flags,
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
            var buffer = new NativeMethods.QuicBuffer()
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
            var buffer = new NativeMethods.QuicBuffer()
            {
                Length = sizeof(ulong),
                Buffer = (byte*)&count
            };
            SetParam(param, buffer);
        }

        private void SetParam(
          QUIC_PARAM_SESSION param,
          NativeMethods.QuicBuffer buf)
        {
            QuicStatusException.ThrowIfFailed(_registration.UnsafeSetParam(
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
            _alpn = null;
            _registration = null;

            _disposed = true;
        }
    }
}
