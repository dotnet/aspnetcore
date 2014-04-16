//------------------------------------------------------------------------------
// <copyright file="HttpListener.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Net.Server
{
    internal unsafe class NativeRequestContext : IDisposable
    {
        private const int DefaultBufferSize = 4096;
        private UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST* _memoryBlob;
        private IntPtr _originalBlobAddress;
        private byte[] _backingBuffer;
        private SafeNativeOverlapped _nativeOverlapped;
        private AsyncAcceptContext _acceptResult;

        internal NativeRequestContext(AsyncAcceptContext result)
        {
            _acceptResult = result;
            UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST* requestBlob = Allocate(0);
            if (requestBlob == null)
            {
                GC.SuppressFinalize(this);
            }
            else
            {
                _memoryBlob = requestBlob;
            }
        }

        internal SafeNativeOverlapped NativeOverlapped
        {
            get
            {
                return _nativeOverlapped;
            }
        }

        internal UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST* RequestBlob
        {
            get
            {
                Debug.Assert(_memoryBlob != null || _backingBuffer == null, "RequestBlob requested after ReleasePins().");
                return _memoryBlob;
            }
        }

        internal byte[] RequestBuffer
        {
            get
            {
                return _backingBuffer;
            }
        }

        internal uint Size
        {
            get
            {
                return (uint)_backingBuffer.Length;
            }
        }

        internal IntPtr OriginalBlobAddress
        {
            get
            {
                UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST* blob = _memoryBlob;
                return (blob == null ? _originalBlobAddress : (IntPtr)blob);
            }
        }

        // ReleasePins() should be called exactly once.  It must be called before Dispose() is called, which means it must be called
        // before an object (Request) which closes the RequestContext on demand is returned to the application.
        internal void ReleasePins()
        {
            Debug.Assert(_memoryBlob != null || _backingBuffer == null, "RequestContextBase::ReleasePins()|ReleasePins() called twice.");
            _originalBlobAddress = (IntPtr)_memoryBlob;
            UnsetBlob();
            OnReleasePins();
        }

        private void OnReleasePins()
        {
            if (_nativeOverlapped != null)
            {
                SafeNativeOverlapped nativeOverlapped = _nativeOverlapped;
                _nativeOverlapped = null;
                nativeOverlapped.Dispose();
            }
        }

        public void Dispose()
        {
            Debug.Assert(_memoryBlob == null, "RequestContextBase::Dispose()|Dispose() called before ReleasePins().");
            Dispose(true);
        }

        protected void Dispose(bool disposing)
        {
            if (_nativeOverlapped != null)
            {
                Debug.Assert(!disposing, "AsyncRequestContext::Dispose()|Must call ReleasePins() before calling Dispose().");
                _nativeOverlapped.Dispose();
            }
        }

        private void SetBlob(UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST* requestBlob)
        {
            Debug.Assert(_memoryBlob != null || _backingBuffer == null, "RequestContextBase::Dispose()|SetBlob() called after ReleasePins().");
            if (requestBlob == null)
            {
                UnsetBlob();
                return;
            }

            if (_memoryBlob == null)
            {
                GC.ReRegisterForFinalize(this);
            }
            _memoryBlob = requestBlob;
        }

        private void UnsetBlob()
        {
            if (_memoryBlob != null)
            {
                GC.SuppressFinalize(this);
            }
            _memoryBlob = null;
        }

        private void SetBuffer(int size)
        {
            _backingBuffer = size == 0 ? null : new byte[size];
        }

        private UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST* Allocate(uint size)
        {
            uint newSize = size != 0 ? size : RequestBuffer == null ? DefaultBufferSize : Size;
            if (_nativeOverlapped != null && newSize != RequestBuffer.Length)
            {
                SafeNativeOverlapped nativeOverlapped = _nativeOverlapped;
                _nativeOverlapped = null;
                nativeOverlapped.Dispose();
            }
            if (_nativeOverlapped == null)
            {
                SetBuffer(checked((int)newSize));
                Overlapped overlapped = new Overlapped();
                overlapped.AsyncResult = _acceptResult;
                _nativeOverlapped = new SafeNativeOverlapped(overlapped.Pack(AsyncAcceptContext.IOCallback, RequestBuffer));
                return (UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST*)Marshal.UnsafeAddrOfPinnedArrayElement(RequestBuffer, 0);
            }
            return RequestBlob;
        }

        internal void Reset(ulong requestId, uint size)
        {
            SetBlob(Allocate(size));
            RequestBlob->RequestId = requestId;
        }
    }
}
