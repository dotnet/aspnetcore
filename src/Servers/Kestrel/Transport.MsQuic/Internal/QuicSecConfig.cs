// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    internal class QuicSecConfig : IDisposable
    {
        private bool _disposed;
        private QuicRegistration _registration;

        public QuicSecConfig(QuicRegistration registration, IntPtr nativeObjPtr)
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

        ~QuicSecConfig()
        {
            Dispose(disposing: false);
        }
    }
}
