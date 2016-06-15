// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

//------------------------------------------------------------------------------
// <copyright file="HttpListener.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Net.Http.Server
{
    internal unsafe class NativeRequestContext : IDisposable
    {
        private const int DefaultBufferSize = 4096;
        private const int AlignmentPadding = 8;
        private UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST* _memoryBlob;
        private IntPtr _originalBlobAddress;
        private byte[] _backingBuffer;
        private int _bufferAlignment;
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
                return (uint)_backingBuffer.Length - AlignmentPadding;
            }
        }

        internal int BufferAlignment
        {
            get
            {
                return _bufferAlignment;
            }
        }

        internal IntPtr OriginalBlobAddress
        {
            get
            {
                UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST* blob = _memoryBlob;
                return blob == null ? _originalBlobAddress : (IntPtr)blob;
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
            Debug.Assert(size != 0, "unexpected size");

            _backingBuffer = new byte[size + AlignmentPadding];
        }

        private UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST* Allocate(uint size)
        {
            // We can't reuse overlapped objects
            if (_nativeOverlapped != null)
            {
                SafeNativeOverlapped nativeOverlapped = _nativeOverlapped;
                _nativeOverlapped = null;
                nativeOverlapped.Dispose();
            }

            uint newSize = size != 0 ? size : RequestBuffer == null ? DefaultBufferSize : Size;
            SetBuffer(checked((int)newSize));
            var boundHandle = _acceptResult.Server.BoundHandle;
            _nativeOverlapped = new SafeNativeOverlapped(boundHandle,
                boundHandle.AllocateNativeOverlapped(AsyncAcceptContext.IOCallback, _acceptResult, RequestBuffer));

            // HttpReceiveHttpRequest expects the request pointer to be 8-byte-aligned or it fails. On ARM
            // CLR creates buffers that are 4-byte-aligned so we need force 8-byte alignment.
            var requestAddress = Marshal.UnsafeAddrOfPinnedArrayElement(RequestBuffer, 0);
            _bufferAlignment = (int)(requestAddress.ToInt64() & 0x07);

            return (UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST*)(requestAddress + _bufferAlignment);
        }

        internal void Reset(ulong requestId, uint size)
        {
            SetBlob(Allocate(size));
            RequestBlob->RequestId = requestId;
        }
    }
}
