// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using static Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal.NativeMethods;
using static Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal.QuicStream;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    // TODO swap to safehandle for debugging hard problems
    // if another thread is calling into a safehandle
    // dispose would release handle, but it isn't release inline
    internal sealed class QuicConnection : IDisposable
    {
        private bool _disposed;
        private readonly bool _shouldOwnNativeObj;
        private IntPtr _nativeObjPtr;
        private ConnectionCallback _callback;
        private static GCHandle _handle;
        private readonly IntPtr _unmanagedFnPtrForNativeCallback;

        public QuicApi Registration { get; set; }

        public delegate QUIC_STATUS ConnectionCallback(
            QuicConnection connection,
            ref ConnectionEvent connectionEvent);

        public QuicConnection(QuicApi registration, IntPtr nativeObjPtr, bool shouldOwnNativeObj)
        {
            Registration = registration;
            _shouldOwnNativeObj = shouldOwnNativeObj;
            _nativeObjPtr = nativeObjPtr;
            ConnectionCallbackDelegate nativeCallback = NativeCallbackHandler;
            _unmanagedFnPtrForNativeCallback = Marshal.GetFunctionPointerForDelegate(nativeCallback);
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

        public void Start(
            ushort family,
            ushort serverPort)
        {
            var status = Registration.ConnectionStartDelegate(
                _nativeObjPtr,
                family,
                null,
                serverPort);
            QuicStatusException.ThrowIfFailed(status);
        }

        public QuicStream StreamOpen(
            QUIC_NEW_STREAM_FLAG flags,
            StreamCallback callback)
        {
            var streamPtr = IntPtr.Zero;
            var status = Registration.StreamOpenDelegate(
                _nativeObjPtr,
                (uint)flags,
                QuicStream.NativeCallbackHandler,
                IntPtr.Zero,
                out streamPtr);
            QuicStatusException.ThrowIfFailed(status);

            var stream = new QuicStream(Registration, streamPtr, true);
            stream.SetCallbackHandler(callback);
            return stream;
        }

        public void SetCallbackHandler(
            ConnectionCallback callback)
        {
            _handle = GCHandle.Alloc(this);
            _callback = callback;
            Registration.SetCallbackHandlerDelegate(
                _nativeObjPtr,
                _unmanagedFnPtrForNativeCallback,
                GCHandle.ToIntPtr(_handle));
        }

        public void Shutdown(
            QUIC_CONNECTION_SHUTDOWN Flags,
            ushort ErrorCode)
        {
            var status = Registration.ConnectionShutdownDelegate(
                _nativeObjPtr,
                (uint)Flags,
                ErrorCode);
            QuicStatusException.ThrowIfFailed(status);
        }

        internal static QUIC_STATUS NativeCallbackHandler(
            IntPtr connection,
            IntPtr context,
            ref ConnectionEvent connectionEventStruct)
        {
            var handle = GCHandle.FromIntPtr(context);
            var quicConnection = (QuicConnection)handle.Target;

            return quicConnection.ExecuteCallback(ref connectionEventStruct);
        }

        private QUIC_STATUS ExecuteCallback(
            ref ConnectionEvent connectionEvent)
        {
            var status = QUIC_STATUS.INTERNAL_ERROR;
            try
            {
                status = _callback(
                    this,
                    ref connectionEvent);
            }
            catch (Exception)
            {
                // TODO log
            }
            return status;
        }

        private void SetParam(
            QUIC_PARAM_CONN param,
            QuicBuffer buf)
        {
            QuicStatusException.ThrowIfFailed(Registration.UnsafeSetParam(
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

        ~QuicConnection()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (_shouldOwnNativeObj)
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
