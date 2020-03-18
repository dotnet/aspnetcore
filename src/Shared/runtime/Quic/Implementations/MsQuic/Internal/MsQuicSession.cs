// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal sealed class MsQuicSession : IDisposable
    {
        private bool _disposed = false;
        private IntPtr _nativeObjPtr;
        private bool _opened;

        internal MsQuicSession()
        {
            if (!MsQuicApi.IsQuicSupported)
            {
                throw new NotSupportedException(SR.net_quic_notsupported);
            }
        }

        public IntPtr ConnectionOpen(QuicClientConnectionOptions options)
        {
            if (!_opened)
            {
                OpenSession(options.ClientAuthenticationOptions!.ApplicationProtocols![0].Protocol.ToArray(),
                    (ushort)options.MaxBidirectionalStreams,
                    (ushort)options.MaxUnidirectionalStreams);
            }

            QuicExceptionHelpers.ThrowIfFailed(MsQuicApi.Api.ConnectionOpenDelegate(
                _nativeObjPtr,
                MsQuicConnection.NativeCallbackHandler,
                IntPtr.Zero,
                out IntPtr connectionPtr),
                "Could not open the connection.");

            return connectionPtr;
        }

        private void OpenSession(byte[] alpn, ushort bidirectionalStreamCount, ushort undirectionalStreamCount)
        {
            _opened = true;
            _nativeObjPtr = MsQuicApi.Api.SessionOpen(alpn);
            SetPeerBiDirectionalStreamCount(bidirectionalStreamCount);
            SetPeerUnidirectionalStreamCount(undirectionalStreamCount);
        }

        // TODO allow for a callback to select the certificate (SNI).
        public IntPtr ListenerOpen(QuicListenerOptions options)
        {
            if (!_opened)
            {
                OpenSession(options.ServerAuthenticationOptions!.ApplicationProtocols![0].Protocol.ToArray(),
                                    (ushort)options.MaxBidirectionalStreams,
                                    (ushort)options.MaxUnidirectionalStreams);
            }

            QuicExceptionHelpers.ThrowIfFailed(MsQuicApi.Api.ListenerOpenDelegate(
                _nativeObjPtr,
                MsQuicListener.NativeCallbackHandler,
                IntPtr.Zero,
                out IntPtr listenerPointer),
                "Could not open listener.");

            return listenerPointer;
        }

        // TODO call this for graceful shutdown?
        public void ShutDown(
            QUIC_CONNECTION_SHUTDOWN_FLAG Flags,
            ushort ErrorCode)
        {
            MsQuicApi.Api.SessionShutdownDelegate(
                _nativeObjPtr,
                (uint)Flags,
                ErrorCode);
        }

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
            QuicExceptionHelpers.ThrowIfFailed(MsQuicApi.Api.UnsafeSetParam(
                _nativeObjPtr,
                (uint)QUIC_PARAM_LEVEL.SESSION,
                (uint)param,
                buf),
                "Could not set parameter on session.");
        }

        ~MsQuicSession()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            MsQuicApi.Api.SessionCloseDelegate?.Invoke(_nativeObjPtr);
            _nativeObjPtr = IntPtr.Zero;

            _disposed = true;
        }
    }
}
