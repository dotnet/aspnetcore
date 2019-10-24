// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Runtime.InteropServices;
using static Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal.NativeMethods;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    internal class QuicListener : IDisposable
    {
        private bool _disposed;
        private readonly bool _shouldOwnNativeObj;
        private IntPtr _nativeObjPtr;
        private QuicRegistration _registration;
        public ListenerCallback _callback;
        private readonly IntPtr _unmanagedFnPtrForNativeCallback;
        private GCHandle _handle;

        public QuicListener(QuicRegistration registration, IntPtr nativeObjPtr, bool shouldOwnNativeObj)
        {
            _registration = registration;
            _shouldOwnNativeObj = shouldOwnNativeObj;
            ListenerCallbackDelegate nativeCallback = NativeCallbackHandler;
            _unmanagedFnPtrForNativeCallback = Marshal.GetFunctionPointerForDelegate(nativeCallback);
            _nativeObjPtr = nativeObjPtr;
        }

        public delegate QUIC_STATUS ListenerCallback(
            ref ListenerEvent evt);

        public void Start(IPEndPoint localIpEndpoint)
        {
            var localAddress = WinSockNativeMethods.Convert(localIpEndpoint);
            QuicStatusException.ThrowIfFailed(_registration.ListenerStartDelegate(
                _nativeObjPtr,
                ref localAddress));
        }

        internal static QUIC_STATUS NativeCallbackHandler(
            IntPtr listener,
            IntPtr context,
            ref ListenerEvent connectionEventStruct)
        {
            var handle = GCHandle.FromIntPtr(context);
            var quicListener = (QuicListener)handle.Target;

            return quicListener.ExecuteCallback(ref connectionEventStruct);
        }

        private QUIC_STATUS ExecuteCallback(
           ref ListenerEvent connectionEvent)
        {
            var status = QUIC_STATUS.INTERNAL_ERROR;
            try
            {
                status = _callback(ref connectionEvent);
            }
            catch (Exception)
            {
                // TODO log
            }
            return status;
        }

        public void SetCallbackHandler(
            ListenerCallback callback)
        {
            _handle = GCHandle.Alloc(this);
            _callback = callback;
            _registration.SetCallbackHandlerDelegate(
                _nativeObjPtr,
                _unmanagedFnPtrForNativeCallback,
                GCHandle.ToIntPtr(_handle));
        }

        public void Stop()
        {
            _registration.ListenerStopDelegate(_nativeObjPtr);
        }

        ~QuicListener()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (_shouldOwnNativeObj)
            {
                _registration.ListenerCloseDelegate?.Invoke(_nativeObjPtr);
            }
            _nativeObjPtr = IntPtr.Zero;
            _registration = null;

            _disposed = true;
        }
    }
}
