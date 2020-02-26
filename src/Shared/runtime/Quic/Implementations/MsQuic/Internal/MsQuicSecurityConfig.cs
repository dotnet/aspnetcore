// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    // TODO this will eventually be abstracted to support both Client and Server
    // certificates
    internal class MsQuicSecurityConfig : IDisposable
    {
        private bool _disposed;
        private MsQuicApi _registration;

        public MsQuicSecurityConfig(MsQuicApi registration, IntPtr nativeObjPtr)
        {
            _registration = registration;
            NativeObjPtr = nativeObjPtr;
        }

        public IntPtr NativeObjPtr { get; private set; }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _registration.SecConfigDeleteDelegate?.Invoke(NativeObjPtr);
            NativeObjPtr = IntPtr.Zero;
            _disposed = true;
        }

        ~MsQuicSecurityConfig()
        {
            Dispose(disposing: false);
        }
    }
}
