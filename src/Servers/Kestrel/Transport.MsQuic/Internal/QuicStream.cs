// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal.NativeMethods;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    internal sealed class QuicStream : IDisposable
    {
        private bool _disposed;
        private readonly bool _shouldOwnNativeObj;
        private IntPtr _nativeObjPtr;
        private GCHandle _handle;
        private StreamCallback _callback;
        private readonly IntPtr _unmanagedFnPtrForNativeCallback;

        public delegate QUIC_STATUS StreamCallback(
            QuicStream stream,
            ref StreamEvent evt);

        internal StreamCallbackDelegate NativeCallback { get; private set; }

        internal static QUIC_STATUS NativeCallbackHandler(
           IntPtr stream,
           IntPtr context,
           ref StreamEvent connectionEventStruct)
        {
            var handle = GCHandle.FromIntPtr(context);
            var quicStream = (QuicStream)handle.Target;

            return quicStream.ExecuteCallback(ref connectionEventStruct);
        }

        private QUIC_STATUS ExecuteCallback(
            ref StreamEvent evt)
        {
            var status = QUIC_STATUS.INTERNAL_ERROR;
            if (evt.Type == QUIC_STREAM_EVENT.SEND_COMPLETE)
            {
                SendContext.HandleNativeCallback(evt.ClientContext);
            }
            try
            {
                status = _callback(
                    this,
                    ref evt);
            }
            catch (Exception)
            {
                // TODO log
            }
            return status;
        }

        public void SetCallbackHandler(
            StreamCallback callback)
        {
            _handle = GCHandle.Alloc(this);
            _callback = callback;
            Registration.SetCallbackHandlerDelegate(
                _nativeObjPtr,
                _unmanagedFnPtrForNativeCallback,
                GCHandle.ToIntPtr(_handle));
        }

        public unsafe Task SendAsync(
           ReadOnlySequence<byte> buffers,
           QUIC_SEND_FLAG flags,
           object clientSendContext)
        {
            var sendContext = new SendContext(buffers, clientSendContext);
            var status = (QUIC_STATUS)Registration.StreamSendDelegate(
                _nativeObjPtr,
                sendContext.Buffers,
                sendContext.BufferCount,
                (uint)flags,
                sendContext.NativeClientSendContext);
            QuicStatusException.ThrowIfFailed(status);

            // TODO for some reason the first call to SendAsync triggers the callback.
            return sendContext.TcsTask; // TODO make QuicStream implement IValueTaskSource?
        }

        public Task StartAsync(QUIC_STREAM_START flags)
        {
            var status = (QUIC_STATUS)Registration.StreamStartDelegate(
              _nativeObjPtr,
              (uint)flags);
            QuicStatusException.ThrowIfFailed(status);
            return Task.CompletedTask;
        }

        // TODO: Should not be public
        public void ReceiveComplete(int bufferLength)
        {
            Debug.Assert(bufferLength > 0);
            var status = (QUIC_STATUS)Registration.StreamReceiveComplete(_nativeObjPtr, (ulong)bufferLength);
            QuicStatusException.ThrowIfFailed(status);
        }

        public void ShutDown(
            QUIC_STREAM_SHUTDOWN flags,
            ushort errorCode)
        {
            var status = (QUIC_STATUS)Registration.StreamShutdownDelegate(
                _nativeObjPtr,
                (uint)flags,
                errorCode);
            QuicStatusException.ThrowIfFailed(status);
        }

        public void Close()
        {
            var status = (QUIC_STATUS)Registration.StreamCloseDelegate?.Invoke(_nativeObjPtr);
            QuicStatusException.ThrowIfFailed(status);
        }

        public long Handle { get => (long)_nativeObjPtr; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal QuicStream(QuicRegistration registration, IntPtr nativeObjPtr, bool shouldOwnNativeObj)
        {
            Registration = registration;
            _shouldOwnNativeObj = shouldOwnNativeObj;
            _nativeObjPtr = nativeObjPtr;
            StreamCallbackDelegate nativeCallback = NativeCallbackHandler;
            _unmanagedFnPtrForNativeCallback = Marshal.GetFunctionPointerForDelegate(nativeCallback);
        }

        public unsafe void EnableReceive()
        {
            var val = true;
            var buffer = new QuicBuffer()
            {
                Length = sizeof(bool),
                Buffer = (byte*)&val
            };
            SetParam(QUIC_PARAM_STREAM.RECEIVE_ENABLED, buffer);
        }

        private void SetParam(
              QUIC_PARAM_STREAM param,
              QuicBuffer buf)
        {
            QuicStatusException.ThrowIfFailed(Registration.UnsafeSetParam(
                _nativeObjPtr,
                (uint)QUIC_PARAM_LEVEL.SESSION,
                (uint)param,
                buf));
        }

        public QuicRegistration Registration { get; set; }

        ~QuicStream()
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
                Registration.StreamCloseDelegate?.Invoke(_nativeObjPtr);
            }

            _nativeObjPtr = IntPtr.Zero;
            Registration = null;

            _disposed = true;
        }
    }
}
