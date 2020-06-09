// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    // This class is used to load the client certificate on-demand.  Because client certs are optional, all
    // failures are handled internally and reported via ClientCertException or ClientCertError.
    internal unsafe sealed class ClientCertLoader : IAsyncResult, IDisposable
    {
        private const uint CertBoblSize = 1500;
        private static readonly IOCompletionCallback IOCallback = new IOCompletionCallback(WaitCallback);
        private static readonly int RequestChannelBindStatusSize =
            Marshal.SizeOf<HttpApiTypes.HTTP_REQUEST_CHANNEL_BIND_STATUS>();

        private SafeNativeOverlapped _overlapped;
        private byte[] _backingBuffer;
        private HttpApiTypes.HTTP_SSL_CLIENT_CERT_INFO* _memoryBlob;
        private uint _size;
        private TaskCompletionSource<object> _tcs;
        private RequestContext _requestContext;

        private int _clientCertError;
        private X509Certificate2 _clientCert;
        private Exception _clientCertException;
        private CancellationTokenRegistration _cancellationRegistration;

        internal ClientCertLoader(RequestContext requestContext, CancellationToken cancellationToken)
        {
            _requestContext = requestContext;
            _tcs = new TaskCompletionSource<object>();
            // we will use this overlapped structure to issue async IO to ul
            // the event handle will be put in by the BeginHttpApi2.ERROR_SUCCESS() method
            Reset(CertBoblSize);

            if (cancellationToken.CanBeCanceled)
            {
                _cancellationRegistration = RequestContext.RegisterForCancellation(cancellationToken);
            }
        }

        internal SafeHandle RequestQueueHandle => _requestContext.Server.RequestQueue.Handle;

        internal X509Certificate2 ClientCert
        {
            get
            {
                Contract.Assert(Task.IsCompleted);
                return _clientCert;
            }
        }

        internal int ClientCertError
        {
            get
            {
                Contract.Assert(Task.IsCompleted);
                return _clientCertError;
            }
        }

        internal Exception ClientCertException
        {
            get
            {
                Contract.Assert(Task.IsCompleted);
                return _clientCertException;
            }
        }

        private RequestContext RequestContext
        {
            get
            {
                return _requestContext;
            }
        }

        private Task Task
        {
            get
            {
                return _tcs.Task;
            }
        }

        private SafeNativeOverlapped NativeOverlapped
        {
            get
            {
                return _overlapped;
            }
        }

        private HttpApiTypes.HTTP_SSL_CLIENT_CERT_INFO* RequestBlob
        {
            get
            {
                return _memoryBlob;
            }
        }

        private void Reset(uint size)
        {
            if (size == _size)
            {
                return;
            }
            if (_size != 0)
            {
                _overlapped.Dispose();
            }
            _size = size;
            if (size == 0)
            {
                _overlapped = null;
                _memoryBlob = null;
                _backingBuffer = null;
                return;
            }
            _backingBuffer = new byte[checked((int)size)];
            var boundHandle = RequestContext.Server.RequestQueue.BoundHandle;
            _overlapped = new SafeNativeOverlapped(boundHandle,
                boundHandle.AllocateNativeOverlapped(IOCallback, this, _backingBuffer));
            _memoryBlob = (HttpApiTypes.HTTP_SSL_CLIENT_CERT_INFO*)Marshal.UnsafeAddrOfPinnedArrayElement(_backingBuffer, 0);
        }

        // When you use netsh to configure HTTP.SYS with clientcertnegotiation = enable
        // which means negotiate client certificates, when the client makes the
        // initial SSL connection, the server (HTTP.SYS) requests the client certificate.
        //
        // Some apps may not want to negotiate the client cert at the beginning,
        // perhaps serving the default.htm. In this case the HTTP.SYS is configured
        // with clientcertnegotiation = disabled, which means that the client certificate is
        // optional so initially when SSL is established HTTP.SYS won't ask for client
        // certificate. This works fine for the default.htm in the case above,
        // however, if the app wants to demand a client certificate at a later time
        // perhaps showing "YOUR ORDERS" page, then the server wants to negotiate
        // Client certs. This will in turn makes HTTP.SYS to do the
        // SEC_I_RENOGOTIATE through which the client cert demand is made
        //
        // NOTE: When calling HttpReceiveClientCertificate you can get
        // ERROR_NOT_FOUND - which means the client did not provide the cert
        // If this is important, the server should respond with 403 forbidden
        // HTTP.SYS will not do this for you automatically
        internal Task LoadClientCertificateAsync()
        {
            uint size = CertBoblSize;
            bool retry;
            do
            {
                retry = false;
                uint bytesReceived = 0;

                uint statusCode =
                    HttpApi.HttpReceiveClientCertificate(
                        RequestQueueHandle,
                        RequestContext.Request.UConnectionId,
                        (uint)HttpApiTypes.HTTP_FLAGS.NONE,
                        RequestBlob,
                        size,
                        &bytesReceived,
                        NativeOverlapped);

                if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_MORE_DATA)
                {
                    HttpApiTypes.HTTP_SSL_CLIENT_CERT_INFO* pClientCertInfo = RequestBlob;
                    size = bytesReceived + pClientCertInfo->CertEncodedSize;
                    Reset(size);
                    retry = true;
                }
                else if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_NOT_FOUND)
                {
                    // The client did not send a cert.
                    Complete(0, null);
                }
                else if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS &&
                    HttpSysListener.SkipIOCPCallbackOnSuccess)
                {
                    IOCompleted(statusCode, bytesReceived);
                }
                else if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS &&
                    statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_IO_PENDING)
                {
                    // Some other bad error, possible(?) return values are:
                    // ERROR_INVALID_HANDLE, ERROR_INSUFFICIENT_BUFFER, ERROR_OPERATION_ABORTED
                    // Also ERROR_BAD_DATA if we got it twice or it reported smaller size buffer required.
                    Fail(new HttpSysException((int)statusCode));
                }
            }
            while (retry);

            return Task;
        }

        private void Complete(int certErrors, X509Certificate2 cert)
        {
            // May be null
            _clientCert = cert;
            _clientCertError = certErrors;
            Dispose();
            _tcs.TrySetResult(null);
        }

        private void Fail(Exception ex)
        {
            // TODO: Log
            _clientCertException = ex;
            Dispose();
            _tcs.TrySetResult(null);
        }

        private unsafe void IOCompleted(uint errorCode, uint numBytes)
        {
            IOCompleted(this, errorCode, numBytes);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Redirected to callback")]
        private static unsafe void IOCompleted(ClientCertLoader asyncResult, uint errorCode, uint numBytes)
        {
            RequestContext requestContext = asyncResult.RequestContext;
            try
            {
                if (errorCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_MORE_DATA)
                {
                    // There is a bug that has existed in http.sys since w2k3.  Bytesreceived will only
                    // return the size of the initial cert structure.  To get the full size,
                    // we need to add the certificate encoding size as well.

                    HttpApiTypes.HTTP_SSL_CLIENT_CERT_INFO* pClientCertInfo = asyncResult.RequestBlob;
                    asyncResult.Reset(numBytes + pClientCertInfo->CertEncodedSize);

                    uint bytesReceived = 0;
                    errorCode =
                        HttpApi.HttpReceiveClientCertificate(
                            requestContext.Server.RequestQueue.Handle,
                            requestContext.Request.UConnectionId,
                            (uint)HttpApiTypes.HTTP_FLAGS.NONE,
                            asyncResult._memoryBlob,
                            asyncResult._size,
                            &bytesReceived,
                            asyncResult._overlapped);

                    if (errorCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_IO_PENDING ||
                       (errorCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && !HttpSysListener.SkipIOCPCallbackOnSuccess))
                    {
                        return;
                    }
                }

                if (errorCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_NOT_FOUND)
                {
                    // The client did not send a cert.
                    asyncResult.Complete(0, null);
                }
                else if (errorCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS)
                {
                    asyncResult.Fail(new HttpSysException((int)errorCode));
                }
                else
                {
                    HttpApiTypes.HTTP_SSL_CLIENT_CERT_INFO* pClientCertInfo = asyncResult._memoryBlob;
                    if (pClientCertInfo == null)
                    {
                        asyncResult.Complete(0, null);
                    }
                    else
                    {
                        if (pClientCertInfo->pCertEncoded != null)
                        {
                            try
                            {
                                byte[] certEncoded = new byte[pClientCertInfo->CertEncodedSize];
                                Marshal.Copy((IntPtr)pClientCertInfo->pCertEncoded, certEncoded, 0, certEncoded.Length);
                                asyncResult.Complete((int)pClientCertInfo->CertFlags, new X509Certificate2(certEncoded));
                            }
                            catch (CryptographicException exception)
                            {
                                // TODO: Log
                                asyncResult.Fail(exception);
                            }
                            catch (SecurityException exception)
                            {
                                // TODO: Log
                                asyncResult.Fail(exception);
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                asyncResult.Fail(exception);
            }
        }

        private static unsafe void WaitCallback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
        {
            var asyncResult = (ClientCertLoader)ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped);
            IOCompleted(asyncResult, errorCode, numBytes);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationRegistration.Dispose();
                if (_overlapped != null)
                {
                    _memoryBlob = null;
                    _overlapped.Dispose();
                }
            }
        }

        public object AsyncState
        {
            get { return _tcs.Task.AsyncState; }
        }

        public WaitHandle AsyncWaitHandle
        {
            get { return ((IAsyncResult)_tcs.Task).AsyncWaitHandle; }
        }

        public bool CompletedSynchronously
        {
            get { return ((IAsyncResult)_tcs.Task).CompletedSynchronously; }
        }

        public bool IsCompleted
        {
            get { return _tcs.Task.IsCompleted; }
        }

        internal static unsafe ChannelBinding GetChannelBindingFromTls(RequestQueue requestQueue, ulong connectionId, ILogger logger)
        {
            // +128 since a CBT is usually <128 thus we need to call HRCC just once. If the CBT
            // is >128 we will get ERROR_MORE_DATA and call again
            int size = RequestChannelBindStatusSize + 128;

            Debug.Assert(size >= 0);

            byte[] blob = null;
            SafeLocalFreeChannelBinding token = null;

            uint bytesReceived = 0; ;
            uint statusCode;

            do
            {
                blob = new byte[size];
                fixed (byte* blobPtr = blob)
                {
                    // Http.sys team: ServiceName will always be null if
                    // HTTP_RECEIVE_SECURE_CHANNEL_TOKEN flag is set.
                    statusCode = HttpApi.HttpReceiveClientCertificate(
                        requestQueue.Handle,
                        connectionId,
                        (uint)HttpApiTypes.HTTP_FLAGS.HTTP_RECEIVE_SECURE_CHANNEL_TOKEN,
                        blobPtr,
                        (uint)size,
                        &bytesReceived,
                        SafeNativeOverlapped.Zero);

                    if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS)
                    {
                        int tokenOffset = GetTokenOffsetFromBlob((IntPtr)blobPtr);
                        int tokenSize = GetTokenSizeFromBlob((IntPtr)blobPtr);
                        Debug.Assert(tokenSize < Int32.MaxValue);

                        token = SafeLocalFreeChannelBinding.LocalAlloc(tokenSize);

                        Marshal.Copy(blob, tokenOffset, token.DangerousGetHandle(), tokenSize);
                    }
                    else if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_MORE_DATA)
                    {
                        int tokenSize = GetTokenSizeFromBlob((IntPtr)blobPtr);
                        Debug.Assert(tokenSize < Int32.MaxValue);

                        size = RequestChannelBindStatusSize + tokenSize;
                    }
                    else if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_INVALID_PARAMETER)
                    {
                        logger.LogError(LoggerEventIds.ChannelBindingUnSupported, "GetChannelBindingFromTls; Channel binding is not supported.");
                        return null; // old schannel library which doesn't support CBT
                    }
                    else
                    {
                        // It's up to the consumer to fail if the missing ChannelBinding matters to them.
                        logger.LogError(LoggerEventIds.ChannelBindingMissing, new HttpSysException((int)statusCode), "GetChannelBindingFromTls");
                        break;
                    }
                }
            }
            while (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS);

            return token;
        }

        private static int GetTokenOffsetFromBlob(IntPtr blob)
        {
            Debug.Assert(blob != IntPtr.Zero);
            IntPtr tokenPointer = Marshal.ReadIntPtr(blob, (int)Marshal.OffsetOf<HttpApiTypes.HTTP_REQUEST_CHANNEL_BIND_STATUS>("ChannelToken"));
            Debug.Assert(tokenPointer != IntPtr.Zero);
            return (int)IntPtrHelper.Subtract(tokenPointer, blob);
        }

        private static int GetTokenSizeFromBlob(IntPtr blob)
        {
            Debug.Assert(blob != IntPtr.Zero);
            return Marshal.ReadInt32(blob, (int)Marshal.OffsetOf<HttpApiTypes.HTTP_REQUEST_CHANNEL_BIND_STATUS>("ChannelTokenSize"));
        }
    }
}
