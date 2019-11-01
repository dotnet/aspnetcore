// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using static Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal.MsQuicNativeMethods;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    internal class QuicListener : IDisposable
    {
        private bool _disposed;
        private readonly bool _shouldOwnNativeObj;
        private IntPtr _nativeObjPtr;
        private MsQuicApi _registration;
        public ListenerCallback _callback;
        private readonly IntPtr _unmanagedFnPtrForNativeCallback;
        private GCHandle _handle;

        public QuicListener(MsQuicApi registration, IntPtr nativeObjPtr, bool shouldOwnNativeObj)
        {
            _registration = registration;
            _shouldOwnNativeObj = shouldOwnNativeObj;
            ListenerCallbackDelegate nativeCallback = NativeCallbackHandler;
            _unmanagedFnPtrForNativeCallback = Marshal.GetFunctionPointerForDelegate(nativeCallback);
            _nativeObjPtr = nativeObjPtr;
        }

        public delegate uint ListenerCallback(
            ref ListenerEvent evt);

        public void Start(IPEndPoint localIpEndpoint)
        {
            var localAddress = PhysicalAddress.Parse(localIpEndpoint.Address.ToString());
            MsQuicStatusException.ThrowIfFailed(_registration.ListenerStartDelegate(
                _nativeObjPtr,
                ref localAddress.GetAddressBytes()));
        }

        internal static uint NativeCallbackHandler(
            IntPtr listener,
            IntPtr context,
            ref ListenerEvent connectionEventStruct)
        {
            var handle = GCHandle.FromIntPtr(context);
            var quicListener = (QuicListener)handle.Target;

            return quicListener.ExecuteCallback(ref connectionEventStruct);
        }

        private uint ExecuteCallback(
           ref ListenerEvent connectionEvent)
        {
            var status = MsQuicConstants.InternalError;
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
