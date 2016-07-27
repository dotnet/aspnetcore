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
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Net.Http.Server
{
    /// <summary>
    /// An HTTP server wrapping the Http.Sys APIs that accepts requests.
    /// </summary>
    public sealed class WebListener : IDisposable
    {
        private const long DefaultRequestQueueLength = 1000;  // Http.sys default.

        // Win8# 559317 fixed a bug in Http.sys's HttpReceiveClientCertificate method.
        // Without this fix IOCP callbacks were not being called although ERROR_IO_PENDING was
        // returned from HttpReceiveClientCertificate when using the 
        // FileCompletionNotificationModes.SkipCompletionPortOnSuccess flag.
        // This bug was only hit when the buffer passed into HttpReceiveClientCertificate
        // (1500 bytes initially) is tool small for the certificate.
        // Due to this bug in downlevel operating systems the FileCompletionNotificationModes.SkipCompletionPortOnSuccess
        // flag is only used on Win8 and later.
        internal static readonly bool SkipIOCPCallbackOnSuccess = ComNetOS.IsWin8orLater;

        // Mitigate potential DOS attacks by limiting the number of unknown headers we accept.  Numerous header names 
        // with hash collisions will cause the server to consume excess CPU.  1000 headers limits CPU time to under 
        // 0.5 seconds per request.  Respond with a 400 Bad Request.
        private const int UnknownHeaderLimit = 1000;

        private ILogger _logger;

        private volatile State _state; // m_State is set only within lock blocks, but often read outside locks.

        private bool _ignoreWriteExceptions;
        private ServerSession _serverSession;
        private UrlGroup _urlGroup;
        private RequestQueue _requestQueue;
        private TimeoutManager _timeoutManager;
        private AuthenticationManager _authManager;
        private DisconnectListener _disconnectListener;

        private object _internalLock;

        private UrlPrefixCollection _urlPrefixes;

        // The native request queue
        private long? _requestQueueLength;

        private bool _bufferResponses = true;

        public WebListener()
            : this(null)
        {
        }

        public WebListener(ILoggerFactory factory)
        {
            if (!UnsafeNclNativeMethods.HttpApi.Supported)
            {
                throw new PlatformNotSupportedException();
            }

            _logger = LogHelper.CreateLogger(factory, typeof(WebListener));

            Debug.Assert(UnsafeNclNativeMethods.HttpApi.ApiVersion ==
                UnsafeNclNativeMethods.HttpApi.HTTP_API_VERSION.Version20, "Invalid Http api version");

            _state = State.Stopped;
            _internalLock = new object();

            _urlPrefixes = new UrlPrefixCollection(this);
            _timeoutManager = new TimeoutManager(this);
            _authManager = new AuthenticationManager(this);

            // V2 initialization sequence:
            // 1. Create server session
            // 2. Create url group
            // 3. Create request queue - Done in Start()
            // 4. Add urls to url group - Done in Start()
            // 5. Attach request queue to url group - Done in Start()

            try
            {
                _serverSession = new ServerSession();

                _urlGroup = new UrlGroup(_serverSession, _logger);

                _requestQueue = new RequestQueue(_urlGroup, _logger);

                _disconnectListener = new DisconnectListener(_requestQueue, _logger);
            }
            catch (Exception exception)
            {
                // If Url group or request queue creation failed, close server session before throwing.
                _requestQueue?.Dispose();
                _urlGroup?.Dispose();
                _serverSession?.Dispose();
                LogHelper.LogException(_logger, ".Ctor", exception);
                throw;
            }
        }

        internal enum State
        {
            Stopped,
            Started,
            Disposed,
        }

        internal ILogger Logger
        {
            get { return _logger; }
        }

        internal UrlGroup UrlGroup
        {
            get { return _urlGroup; }
        }

        internal RequestQueue RequestQueue
        {
            get { return _requestQueue; }
        }

        internal DisconnectListener DisconnectListener
        {
            get { return _disconnectListener; }
        }

        // TODO: https://github.com/aspnet/WebListener/issues/173
        internal bool IgnoreWriteExceptions
        {
            get { return _ignoreWriteExceptions; }
            set
            {
                CheckDisposed();
                _ignoreWriteExceptions = value;
            }
        }

        public UrlPrefixCollection UrlPrefixes
        {
            get { return _urlPrefixes; }
        }

        public bool BufferResponses
        {
            get { return _bufferResponses; }
            set { _bufferResponses = value; }
        }

        /// <summary>
        /// Exposes the Http.Sys timeout configurations.  These may also be configured in the registry.
        /// </summary>
        public TimeoutManager TimeoutManager
        {
            get { return _timeoutManager; }
        }

        /// <summary>
        /// Http.Sys authentication settings.
        /// </summary>
        public AuthenticationManager AuthenticationManager
        {
            get { return _authManager; }
        }

        public bool IsListening
        {
            get { return _state == State.Started; }
        }

        /// <summary>
        /// Sets the maximum number of requests that will be queued up in Http.Sys.
        /// </summary>
        /// <param name="limit"></param>
        public void SetRequestQueueLimit(long limit)
        {
            CheckDisposed();
            if (limit <= 0)
            {
                throw new ArgumentOutOfRangeException("limit", limit, string.Empty);
            }

            // Don't try to change it if the new limit is the same
            if ((!_requestQueueLength.HasValue && limit == DefaultRequestQueueLength)
                || (_requestQueueLength.HasValue && limit == _requestQueueLength.Value))
            {
                return;
            }

            _requestQueueLength = limit;
            _requestQueue.SetLengthLimit(_requestQueueLength.Value);
        }

        /// <summary>
        /// Start accepting incoming requests.
        /// </summary>
        public void Start()
        {
            CheckDisposed();

            LogHelper.LogInfo(_logger, "Start");

            // Make sure there are no race conditions between Start/Stop/Abort/Close/Dispose.
            // Start needs to setup all resources. Abort/Stop must not interfere while Start is
            // allocating those resources.
            lock (_internalLock)
            {
                try
                {
                    CheckDisposed();
                    if (_state == State.Started)
                    {
                        return;
                    }

                    _requestQueue.AttachToUrlGroup();

                    // All resources are set up correctly. Now add all prefixes.
                    try
                    {
                        _urlPrefixes.RegisterAllPrefixes();
                    }
                    catch (WebListenerException)
                    {
                        // If an error occurred while adding prefixes, free all resources allocated by previous steps.
                        _requestQueue.DetachFromUrlGroup();
                        throw;
                    }

                    _state = State.Started;
                }
                catch (Exception exception)
                {
                    // Make sure the HttpListener instance can't be used if Start() failed.
                    _state = State.Disposed;
                    DisposeInternal();
                    LogHelper.LogException(_logger, "Start", exception);
                    throw;
                }
            }
        }

        private void Stop()
        {
            try
            {
                lock (_internalLock)
                {
                    CheckDisposed();
                    if (_state == State.Stopped)
                    {
                        return;
                    }

                    _urlPrefixes.UnregisterAllPrefixes();

                    _state = State.Stopped;

                    _requestQueue.DetachFromUrlGroup();
                }
            }
            catch (Exception exception)
            {
                LogHelper.LogException(_logger, "Stop", exception);
                throw;
            }
        }

        /// <summary>
        /// Stop the server and clean up.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            lock (_internalLock)
            {
                try
                {
                    if (_state == State.Disposed)
                    {
                        return;
                    }
                    LogHelper.LogInfo(_logger, "Dispose");

                    Stop();
                    DisposeInternal();
                }
                catch (Exception exception)
                {
                    LogHelper.LogException(_logger, "Dispose", exception);
                    throw;
                }
                finally
                {
                    _state = State.Disposed;
                }
            }
        }

        private void DisposeInternal()
        {
            // V2 stopping sequence:
            // 1. Detach request queue from url group - Done in Stop()/Abort()
            // 2. Remove urls from url group - Done in Stop()
            // 3. Close request queue - Done in Stop()/Abort()
            // 4. Close Url group.
            // 5. Close server session.

            _requestQueue.Dispose();

            _urlGroup.Dispose();

            Debug.Assert(_serverSession != null, "ServerSessionHandle is null in CloseV2Config");
            Debug.Assert(!_serverSession.Id.IsInvalid, "ServerSessionHandle is invalid in CloseV2Config");

            _serverSession.Dispose();
        }

        /// <summary>
        /// Accept a request from the incoming request queue.
        /// </summary>
        public Task<RequestContext> AcceptAsync()
        {
            AsyncAcceptContext asyncResult = null;
            try
            {
                CheckDisposed();
                Debug.Assert(_state != State.Stopped, "Listener has been stopped.");
                // prepare the ListenerAsyncResult object (this will have it's own
                // event that the user can wait on for IO completion - which means we
                // need to signal it when IO completes)
                asyncResult = new AsyncAcceptContext(this);
                uint statusCode = asyncResult.QueueBeginGetContext();
                if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS &&
                    statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_IO_PENDING)
                {
                    // some other bad error, possible(?) return values are:
                    // ERROR_INVALID_HANDLE, ERROR_INSUFFICIENT_BUFFER, ERROR_OPERATION_ABORTED
                    asyncResult.Dispose();
                    throw new WebListenerException((int)statusCode);
                }
            }
            catch (Exception exception)
            {
                LogHelper.LogException(_logger, "GetContextAsync", exception);
                throw;
            }

            return asyncResult.Task;
        }

        internal unsafe bool ValidateRequest(NativeRequestContext requestMemory)
        {
            // Block potential DOS attacks
            if (requestMemory.RequestBlob->Headers.UnknownHeaderCount > UnknownHeaderLimit)
            {
                SendError(requestMemory.RequestBlob->RequestId, HttpStatusCode.BadRequest, authChallenges: null);
                return false;
            }
            return true;
        }

        internal unsafe bool ValidateAuth(NativeRequestContext requestMemory)
        {
            var requestV2 = (UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST_V2*)requestMemory.RequestBlob;
            if (!AuthenticationManager.AllowAnonymous && !AuthenticationManager.CheckAuthenticated(requestV2->pRequestInfo))
            {
                SendError(requestMemory.RequestBlob->RequestId, HttpStatusCode.Unauthorized,
                    AuthenticationManager.GenerateChallenges(AuthenticationManager.AuthenticationSchemes));
                return false;
            }
            return true;
        }

        private unsafe void SendError(ulong requestId, HttpStatusCode httpStatusCode, IList<string> authChallenges)
        {
            UnsafeNclNativeMethods.HttpApi.HTTP_RESPONSE_V2 httpResponse = new UnsafeNclNativeMethods.HttpApi.HTTP_RESPONSE_V2();
            httpResponse.Response_V1.Version = new UnsafeNclNativeMethods.HttpApi.HTTP_VERSION();
            httpResponse.Response_V1.Version.MajorVersion = (ushort)1;
            httpResponse.Response_V1.Version.MinorVersion = (ushort)1;

            List<GCHandle> pinnedHeaders = null;
            GCHandle gcHandle;
            try
            {
                // Copied from the multi-value headers section of SerializeHeaders
                if (authChallenges != null && authChallenges.Count > 0)
                {
                    pinnedHeaders = new List<GCHandle>();

                    UnsafeNclNativeMethods.HttpApi.HTTP_RESPONSE_INFO[] knownHeaderInfo = null;
                    knownHeaderInfo = new UnsafeNclNativeMethods.HttpApi.HTTP_RESPONSE_INFO[1];
                    gcHandle = GCHandle.Alloc(knownHeaderInfo, GCHandleType.Pinned);
                    pinnedHeaders.Add(gcHandle);
                    httpResponse.pResponseInfo = (UnsafeNclNativeMethods.HttpApi.HTTP_RESPONSE_INFO*)gcHandle.AddrOfPinnedObject();

                    knownHeaderInfo[httpResponse.ResponseInfoCount].Type = UnsafeNclNativeMethods.HttpApi.HTTP_RESPONSE_INFO_TYPE.HttpResponseInfoTypeMultipleKnownHeaders;
                    knownHeaderInfo[httpResponse.ResponseInfoCount].Length =
                        (uint)Marshal.SizeOf<UnsafeNclNativeMethods.HttpApi.HTTP_MULTIPLE_KNOWN_HEADERS>();

                    UnsafeNclNativeMethods.HttpApi.HTTP_MULTIPLE_KNOWN_HEADERS header = new UnsafeNclNativeMethods.HttpApi.HTTP_MULTIPLE_KNOWN_HEADERS();

                    header.HeaderId = UnsafeNclNativeMethods.HttpApi.HTTP_RESPONSE_HEADER_ID.Enum.HttpHeaderWwwAuthenticate;
                    header.Flags = UnsafeNclNativeMethods.HttpApi.HTTP_RESPONSE_INFO_FLAGS.PreserveOrder; // The docs say this is for www-auth only.

                    UnsafeNclNativeMethods.HttpApi.HTTP_KNOWN_HEADER[] nativeHeaderValues = new UnsafeNclNativeMethods.HttpApi.HTTP_KNOWN_HEADER[authChallenges.Count];
                    gcHandle = GCHandle.Alloc(nativeHeaderValues, GCHandleType.Pinned);
                    pinnedHeaders.Add(gcHandle);
                    header.KnownHeaders = (UnsafeNclNativeMethods.HttpApi.HTTP_KNOWN_HEADER*)gcHandle.AddrOfPinnedObject();

                    for (int headerValueIndex = 0; headerValueIndex < authChallenges.Count; headerValueIndex++)
                    {
                        // Add Value
                        string headerValue = authChallenges[headerValueIndex];
                        byte[] bytes = HeaderEncoding.GetBytes(headerValue);
                        nativeHeaderValues[header.KnownHeaderCount].RawValueLength = (ushort)bytes.Length;
                        gcHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                        pinnedHeaders.Add(gcHandle);
                        nativeHeaderValues[header.KnownHeaderCount].pRawValue = (sbyte*)gcHandle.AddrOfPinnedObject();
                        header.KnownHeaderCount++;
                    }

                    // This type is a struct, not an object, so pinning it causes a boxed copy to be created. We can't do that until after all the fields are set.
                    gcHandle = GCHandle.Alloc(header, GCHandleType.Pinned);
                    pinnedHeaders.Add(gcHandle);
                    knownHeaderInfo[0].pInfo = (UnsafeNclNativeMethods.HttpApi.HTTP_MULTIPLE_KNOWN_HEADERS*)gcHandle.AddrOfPinnedObject();

                    httpResponse.ResponseInfoCount = 1;
                }

                httpResponse.Response_V1.StatusCode = (ushort)httpStatusCode;
                string statusDescription = HttpReasonPhrase.Get(httpStatusCode);
                uint dataWritten = 0;
                uint statusCode;
                byte[] byteReason = HeaderEncoding.GetBytes(statusDescription);
                fixed (byte* pReason = byteReason)
                {
                    httpResponse.Response_V1.pReason = (sbyte*)pReason;
                    httpResponse.Response_V1.ReasonLength = (ushort)byteReason.Length;

                    byte[] byteContentLength = new byte[] { (byte)'0' };
                    fixed (byte* pContentLength = byteContentLength)
                    {
                        (&httpResponse.Response_V1.Headers.KnownHeaders)[(int)HttpSysResponseHeader.ContentLength].pRawValue = (sbyte*)pContentLength;
                        (&httpResponse.Response_V1.Headers.KnownHeaders)[(int)HttpSysResponseHeader.ContentLength].RawValueLength = (ushort)byteContentLength.Length;
                        httpResponse.Response_V1.Headers.UnknownHeaderCount = 0;

                        statusCode =
                            UnsafeNclNativeMethods.HttpApi.HttpSendHttpResponse(
                                _requestQueue.Handle,
                                requestId,
                                0,
                                &httpResponse,
                                null,
                                &dataWritten,
                                SafeLocalFree.Zero,
                                0,
                                SafeNativeOverlapped.Zero,
                                IntPtr.Zero);
                    }
                }
                if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS)
                {
                    // if we fail to send a 401 something's seriously wrong, abort the request
                    UnsafeNclNativeMethods.HttpApi.HttpCancelHttpRequest(_requestQueue.Handle, requestId, IntPtr.Zero);
                }
            }
            finally
            {
                if (pinnedHeaders != null)
                {
                    foreach (GCHandle handle in pinnedHeaders)
                    {
                        if (handle.IsAllocated)
                        {
                            handle.Free();
                        }
                    }
                }
            }
        }

        internal void CheckDisposed()
        {
            if (_state == State.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}
