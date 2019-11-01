// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal.MsQuicNativeMethods;

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

        public delegate uint StreamCallback(
            QuicStream stream,
            ref StreamEvent evt);

        internal QuicStream(MsQuicApi registration, IntPtr nativeObjPtr, bool shouldOwnNativeObj)
        {
            Registration = registration;
            _shouldOwnNativeObj = shouldOwnNativeObj;
            _nativeObjPtr = nativeObjPtr;
            StreamCallbackDelegate nativeCallback = NativeCallbackHandler;
            _unmanagedFnPtrForNativeCallback = Marshal.GetFunctionPointerForDelegate(nativeCallback);
        }

        public MsQuicApi Registration { get; set; }

        internal StreamCallbackDelegate NativeCallback { get; private set; }

        internal static uint NativeCallbackHandler(
           IntPtr stream,
           IntPtr context,
           ref StreamEvent connectionEventStruct)
        {
            var handle = GCHandle.FromIntPtr(context);
            var quicStream = (QuicStream)handle.Target;

            return quicStream.ExecuteCallback(ref connectionEventStruct);
        }

        private uint ExecuteCallback(
            ref StreamEvent evt)
        {
            var status = MsQuicConstants.InternalError;
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
            var status = (uint)Registration.StreamSendDelegate(
                _nativeObjPtr,
                sendContext.Buffers,
                sendContext.BufferCount,
                (uint)flags,
                sendContext.NativeClientSendContext);
            MsQuicStatusException.ThrowIfFailed(status);

            return sendContext.TcsTask; // TODO make QuicStream implement IValueTaskSource?
        }

        public Task StartAsync(QUIC_STREAM_START_FLAG flags)
        {
            var status = (uint)Registration.StreamStartDelegate(
              _nativeObjPtr,
              (uint)flags);
            MsQuicStatusException.ThrowIfFailed(status);
            return Task.CompletedTask;
        }

        public void ReceiveComplete(int bufferLength)
        {
            var status = (uint)Registration.StreamReceiveComplete(_nativeObjPtr, (ulong)bufferLength);
            MsQuicStatusException.ThrowIfFailed(status);
        }

        public void ShutDown(
            QUIC_STREAM_SHUTDOWN_FLAG flags,
            ushort errorCode)
        {
            var status = (uint)Registration.StreamShutdownDelegate(
                _nativeObjPtr,
                (uint)flags,
                errorCode);
            MsQuicStatusException.ThrowIfFailed(status);
        }

        public void Close()
        {
            var status = (uint)Registration.StreamCloseDelegate?.Invoke(_nativeObjPtr);
            MsQuicStatusException.ThrowIfFailed(status);
        }

        public long Handle { get => (long)_nativeObjPtr; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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
            MsQuicStatusException.ThrowIfFailed(Registration.UnsafeSetParam(
                _nativeObjPtr,
                (uint)QUIC_PARAM_LEVEL.SESSION,
                (uint)param,
                buf));
        }


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
