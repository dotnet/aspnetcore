// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
