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
// <copyright file="HttpListenerResponse.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Framework.Logging;
using static Microsoft.Net.Http.Server.UnsafeNclNativeMethods;

namespace Microsoft.Net.Http.Server
{
    public sealed unsafe class Response
    {
        private static readonly string[] ZeroContentLength = new[] { Constants.Zero };

        private ResponseState _responseState;
        private HeaderCollection _headers;
        private string _reasonPhrase;
        private ResponseStream _nativeStream;
        private long _expectedBodyLength;
        private BoundaryType _boundaryType;
        private HttpApi.HTTP_RESPONSE_V2 _nativeResponse;
        private IList<Tuple<Func<object, Task>, object>> _onResponseStartingActions;
        private IList<Tuple<Func<object, Task>, object>> _onResponseCompletedActions;

        private RequestContext _requestContext;
        private bool _bufferingEnabled;

        internal Response(RequestContext requestContext)
        {
            // TODO: Verbose log
            _requestContext = requestContext;
            _headers = new HeaderCollection();
            Reset();
        }

        public void Reset()
        {
            if (_responseState >= ResponseState.StartedSending)
            {
                _requestContext.Abort();
                throw new InvalidOperationException("The response has already been sent. Request Aborted.");
            }
            // We haven't started yet, or we're just buffered, we can clear any data, headers, and state so
            // that we can start over (e.g. to write an error message).
            _nativeResponse = new HttpApi.HTTP_RESPONSE_V2();
            _headers.IsReadOnly = false;
            _headers.Clear();
            _reasonPhrase = null;
            _boundaryType = BoundaryType.None;
            _nativeResponse.Response_V1.StatusCode = (ushort)HttpStatusCode.OK;
            _nativeResponse.Response_V1.Version.MajorVersion = 1;
            _nativeResponse.Response_V1.Version.MinorVersion = 1;
            _responseState = ResponseState.Created;
            _onResponseStartingActions = new List<Tuple<Func<object, Task>, object>>();
            _onResponseCompletedActions = new List<Tuple<Func<object, Task>, object>>();
            _bufferingEnabled = _requestContext.Server.BufferResponses;
            _expectedBodyLength = 0;
            _nativeStream = null;
            CacheTtl = null;
        }

        private enum ResponseState
        {
            Created,
            Started,
            ComputedHeaders,
            StartedSending,
            Closed,
        }

        private RequestContext RequestContext
        {
            get
            {
                return _requestContext;
            }
        }

        private Request Request
        {
            get
            {
                return RequestContext.Request;
            }
        }

        public int StatusCode
        {
            get { return _nativeResponse.Response_V1.StatusCode; }
            set
            {
                if (value <= 100 || 999 < value)
                {
                    throw new ArgumentOutOfRangeException("value", value, string.Format(Resources.Exception_InvalidStatusCode, value));
                }
                CheckResponseStarted();
                _nativeResponse.Response_V1.StatusCode = (ushort)value;
            }
        }

        public string ReasonPhrase
        {
            get { return _reasonPhrase; }
            set
            {
                // TODO: Validate user input for illegal chars, length limit, etc.?
                CheckResponseStarted();
                _reasonPhrase = value;
            }
        }

        public bool ShouldBuffer
        {
            get { return _bufferingEnabled; }
            set
            {
                CheckResponseStarted();
                _bufferingEnabled = value;
            }
        }

        public Stream Body
        {
            get
            {
                CheckDisposed();
                EnsureResponseStream();
                return _nativeStream;
            }
        }

        internal string GetReasonPhrase(int statusCode)
        {
            string reasonPhrase = ReasonPhrase;
            if (string.IsNullOrWhiteSpace(reasonPhrase))
            {
                // if the user hasn't set this, generated on the fly, if possible.
                // We know this one is safe, no need to verify it as in the setter.
                reasonPhrase = HttpReasonPhrase.Get(statusCode) ?? string.Empty;
            }
            return reasonPhrase;
        }

        // We MUST NOT send message-body when we send responses with these Status codes
        private static readonly int[] NoResponseBody = { 100, 101, 204, 205, 304 };

        private static bool CanSendResponseBody(int responseCode)
        {
            for (int i = 0; i < NoResponseBody.Length; i++)
            {
                if (responseCode == NoResponseBody[i])
                {
                    return false;
                }
            }
            return true;
        }

        public HeaderCollection Headers
        {
            get { return _headers; }
        }

        internal long ExpectedBodyLength
        {
            get { return _expectedBodyLength; }
        }

        // Header accessors
        public long? ContentLength
        {
            get
            {
                string contentLengthString = Headers.Get(HttpKnownHeaderNames.ContentLength);
                long contentLength;
                if (!string.IsNullOrWhiteSpace(contentLengthString))
                {
                    contentLengthString = contentLengthString.Trim();
                    if (string.Equals(Constants.Zero, contentLengthString, StringComparison.Ordinal))
                    {
                        return 0;
                    }
                    else if (long.TryParse(contentLengthString, NumberStyles.None, CultureInfo.InvariantCulture.NumberFormat, out contentLength))
                    {
                        return contentLength;
                    }
                }
                return null;
            }
            set
            {
                CheckResponseStarted();
                if (!value.HasValue)
                {
                    Headers.Remove(HttpKnownHeaderNames.ContentLength);
                }
                else
                {
                    if (value.Value < 0)
                    {
                        throw new ArgumentOutOfRangeException("value", value.Value, "Cannot be negative.");
                    }

                    if (value.Value == 0)
                    {
                        ((IDictionary<string, string[]>)Headers)[HttpKnownHeaderNames.ContentLength] = ZeroContentLength;
                    }
                    else
                    {
                        Headers[HttpKnownHeaderNames.ContentLength] = value.Value.ToString(CultureInfo.InvariantCulture);
                    }
                }
            }
        }

        public string ContentType
        {
            get
            {
                return Headers.Get(HttpKnownHeaderNames.ContentType);
            }
            set
            {
                CheckResponseStarted();
                if (string.IsNullOrEmpty(value))
                {
                    Headers.Remove(HttpKnownHeaderNames.ContentType);
                }
                else
                {
                    Headers[HttpKnownHeaderNames.ContentType] = value;
                }
            }
        }

        // should only be called from RequestContext
        internal void Dispose()
        {
            if (_responseState >= ResponseState.Closed)
            {
                return;
            }
            Start();
            NotifyOnResponseCompleted();
            // TODO: Verbose log
            EnsureResponseStream();
            _nativeStream.Dispose();
            _responseState = ResponseState.Closed;
        }

        // old API, now private, and helper methods

        internal BoundaryType BoundaryType
        {
            get { return _boundaryType; }
        }

        public bool HasStarted
        {
            get { return _responseState >= ResponseState.Started; }
        }

        private void CheckResponseStarted()
        {
            if (HasStarted)
            {
                throw new InvalidOperationException("Headers already sent.");
            }
        }

        internal bool ComputedHeaders
        {
            get { return _responseState >= ResponseState.ComputedHeaders; }
        }

        public bool HasStartedSending
        {
            get { return _responseState >= ResponseState.StartedSending; }
        }

        public TimeSpan? CacheTtl { get; set; }

        private void EnsureResponseStream()
        {
            if (_nativeStream == null)
            {
                _nativeStream = new ResponseStream(RequestContext);
            }
        }

        /*
        12.3
        HttpSendHttpResponse() and HttpSendResponseEntityBody() Flag Values.
        The following flags can be used on calls to HttpSendHttpResponse() and HttpSendResponseEntityBody() API calls:

        #define HTTP_SEND_RESPONSE_FLAG_DISCONNECT          0x00000001
        #define HTTP_SEND_RESPONSE_FLAG_MORE_DATA           0x00000002
        #define HTTP_SEND_RESPONSE_FLAG_RAW_HEADER          0x00000004
        #define HTTP_SEND_RESPONSE_FLAG_VALID               0x00000007

        HTTP_SEND_RESPONSE_FLAG_DISCONNECT:
            specifies that the network connection should be disconnected immediately after
            sending the response, overriding the HTTP protocol's persistent connection features.
        HTTP_SEND_RESPONSE_FLAG_MORE_DATA:
            specifies that additional entity body data will be sent by the caller. Thus,
            the last call HttpSendResponseEntityBody for a RequestId, will have this flag reset.
        HTTP_SEND_RESPONSE_RAW_HEADER:
            specifies that a caller of HttpSendResponseEntityBody() is intentionally omitting
            a call to HttpSendHttpResponse() in order to bypass normal header processing. The
            actual HTTP header will be generated by the application and sent as entity body.
            This flag should be passed on the first call to HttpSendResponseEntityBody, and
            not after. Thus, flag is not applicable to HttpSendHttpResponse.
        */

        // TODO: Consider using HTTP_SEND_RESPONSE_RAW_HEADER with HttpSendResponseEntityBody instead of calling HttpSendHttpResponse.
        // This will give us more control of the bytes that hit the wire, including encodings, HTTP 1.0, etc..
        // It may also be faster to do this work in managed code and then pass down only one buffer.
        // What would we loose by bypassing HttpSendHttpResponse?
        //
        // TODO: Consider using the HTTP_SEND_RESPONSE_FLAG_BUFFER_DATA flag for most/all responses rather than just Opaque.
        internal unsafe uint SendHeaders(HttpApi.HTTP_DATA_CHUNK[] dataChunks,
            ResponseStreamAsyncResult asyncResult,
            HttpApi.HTTP_FLAGS flags,
            bool isOpaqueUpgrade)
        {
            Debug.Assert(!HasStartedSending, "HttpListenerResponse::SendHeaders()|SentHeaders is true.");

            _responseState = ResponseState.StartedSending;
            var reasonPhrase = GetReasonPhrase(StatusCode);

            if (RequestContext.Logger.IsEnabled(LogLevel.Verbose))
            {
                RequestContext.Logger.LogVerbose(new SendResponseLogContext(this));
            }

            /*
            if (m_BoundaryType==BoundaryType.Raw) {
                use HTTP_SEND_RESPONSE_FLAG_RAW_HEADER;
            }
            */
            uint statusCode;
            uint bytesSent;
            List<GCHandle> pinnedHeaders = SerializeHeaders(isOpaqueUpgrade);
            try
            {
                if (dataChunks != null)
                {
                    if (pinnedHeaders == null)
                    {
                        pinnedHeaders = new List<GCHandle>();
                    }
                    var handle = GCHandle.Alloc(dataChunks, GCHandleType.Pinned);
                    pinnedHeaders.Add(handle);
                    _nativeResponse.Response_V1.EntityChunkCount = (ushort)dataChunks.Length;
                    _nativeResponse.Response_V1.pEntityChunks = (HttpApi.HTTP_DATA_CHUNK*)handle.AddrOfPinnedObject();
                }
                else if (asyncResult != null && asyncResult.DataChunks != null)
                {
                    _nativeResponse.Response_V1.EntityChunkCount = asyncResult.DataChunkCount;
                    _nativeResponse.Response_V1.pEntityChunks = asyncResult.DataChunks;
                }
                else
                {
                    _nativeResponse.Response_V1.EntityChunkCount = 0;
                    _nativeResponse.Response_V1.pEntityChunks = null;
                }

                var cachePolicy = new HttpApi.HTTP_CACHE_POLICY();
                var cacheTtl = CacheTtl;
                if (cacheTtl.HasValue && cacheTtl.Value > TimeSpan.Zero)
                {
                    cachePolicy.Policy = HttpApi.HTTP_CACHE_POLICY_TYPE.HttpCachePolicyTimeToLive;
                    cachePolicy.SecondsToLive = (uint)Math.Min(cacheTtl.Value.Ticks / TimeSpan.TicksPerSecond, Int32.MaxValue);
                }

                byte[] reasonPhraseBytes = new byte[HeaderEncoding.GetByteCount(reasonPhrase)];
                fixed (byte* pReasonPhrase = reasonPhraseBytes)
                {
                    _nativeResponse.Response_V1.ReasonLength = (ushort)reasonPhraseBytes.Length;
                    HeaderEncoding.GetBytes(reasonPhrase, 0, reasonPhraseBytes.Length, reasonPhraseBytes, 0);
                    _nativeResponse.Response_V1.pReason = (sbyte*)pReasonPhrase;
                    fixed (HttpApi.HTTP_RESPONSE_V2* pResponse = &_nativeResponse)
                    {
                        statusCode =
                            HttpApi.HttpSendHttpResponse(
                                RequestContext.RequestQueueHandle,
                                Request.RequestId,
                                (uint)flags,
                                pResponse,
                                &cachePolicy,
                                &bytesSent,
                                SafeLocalFree.Zero,
                                0,
                                asyncResult == null ? SafeNativeOverlapped.Zero : asyncResult.NativeOverlapped,
                                IntPtr.Zero);

                        if (asyncResult != null &&
                            statusCode == ErrorCodes.ERROR_SUCCESS &&
                            WebListener.SkipIOCPCallbackOnSuccess)
                        {
                            asyncResult.BytesSent = bytesSent;
                            // The caller will invoke IOCompleted
                        }
                    }
                }
            }
            finally
            {
                FreePinnedHeaders(pinnedHeaders);
            }
            return statusCode;
        }

        internal void Start()
        {
            if (!HasStarted)
            {
                // Notify that this is absolutely the last chance to make changes.
                NotifyOnSendingHeaders();
                Headers.IsReadOnly = true; // Prohibit further modifications.
                _responseState = ResponseState.Started;
            }
        }

        internal HttpApi.HTTP_FLAGS ComputeHeaders(bool endOfRequest = false, int bufferedBytes = 0)
        {
            Headers.IsReadOnly = false; // Temporarily allow modification.

            // 401
            if (StatusCode == (ushort)HttpStatusCode.Unauthorized)
            {
                RequestContext.Server.AuthenticationManager.SetAuthenticationChallenge(RequestContext);
            }

            var flags = HttpApi.HTTP_FLAGS.NONE;
            Debug.Assert(!ComputedHeaders, "HttpListenerResponse::ComputeHeaders()|ComputedHeaders is true.");
            _responseState = ResponseState.ComputedHeaders;

            // Gather everything from the request that affects the response:
            var requestVersion = Request.ProtocolVersion;
            var requestConnectionString = Request.Headers.Get(HttpKnownHeaderNames.Connection);
            var isHeadRequest = Request.IsHeadMethod;
            var requestCloseSet = Matches(Constants.Close, requestConnectionString);

            // Gather everything the app may have set on the response:
            // Http.Sys does not allow us to specify the response protocol version, assume this is a HTTP/1.1 response when making decisions.
            var responseConnectionString = Headers.Get(HttpKnownHeaderNames.Connection);
            var transferEncodingString = Headers.Get(HttpKnownHeaderNames.TransferEncoding);
            var responseContentLength = ContentLength;
            var responseCloseSet = Matches(Constants.Close, responseConnectionString);
            var responseChunkedSet = Matches(Constants.Chunked, transferEncodingString);
            var statusCanHaveBody = CanSendResponseBody(_requestContext.Response.StatusCode);

            // Determine if the connection will be kept alive or closed.
            var keepConnectionAlive = true;
            if (requestVersion <= Constants.V1_0 // Http.Sys does not support "Keep-Alive: true" or "Connection: Keep-Alive"
                || (requestVersion == Constants.V1_1 && requestCloseSet)
                || responseCloseSet)
            {
                keepConnectionAlive = false;
            }

            // Determine the body format. If the user asks to do something, let them, otherwise choose a good default for the scenario.
            if (responseContentLength.HasValue)
            {
                _boundaryType = BoundaryType.ContentLength;
                // ComputeLeftToWrite checks for HEAD requests when setting _leftToWrite
                _expectedBodyLength = responseContentLength.Value;
            }
            else if (responseChunkedSet)
            {
                // The application is performing it's own chunking.
                _boundaryType = BoundaryType.PassThrough;
            }
            else if (endOfRequest && !(isHeadRequest && statusCanHaveBody)) // HEAD requests should always end without a body. Assume a GET response would have a body.
            {
                if (bufferedBytes > 0)
                {
                    Headers[HttpKnownHeaderNames.ContentLength] = bufferedBytes.ToString(CultureInfo.InvariantCulture);
                }
                else if (statusCanHaveBody)
                {
                    Headers[HttpKnownHeaderNames.ContentLength] = Constants.Zero;
                }
                _boundaryType = BoundaryType.ContentLength;
                _expectedBodyLength = bufferedBytes;
            }
            else if (keepConnectionAlive && requestVersion == Constants.V1_1)
            {
                _boundaryType = BoundaryType.Chunked;
                Headers[HttpKnownHeaderNames.TransferEncoding] = Constants.Chunked;
            }
            else
            {
                // The length cannot be determined, so we must close the connection
                keepConnectionAlive = false;
                _boundaryType = BoundaryType.Close;
            }

            // Managed connection lifetime
            if (!keepConnectionAlive)
            {
                // All Http.Sys responses are v1.1, so use 1.1 response headers
                // Note that if we don't add this header, Http.Sys will often do it for us.
                if (!responseCloseSet)
                {
                    Headers.Append(HttpKnownHeaderNames.Connection, Constants.Close);
                }
                flags = HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_DISCONNECT;
            }

            Headers.IsReadOnly = true; // Prohibit further modifications.
            return flags;
        }

        private static bool Matches(string knownValue, string input)
        {
            return string.Equals(knownValue, input?.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private List<GCHandle> SerializeHeaders(bool isOpaqueUpgrade)
        {
            Headers.IsReadOnly = true; // Prohibit further modifications.
            HttpApi.HTTP_UNKNOWN_HEADER[] unknownHeaders = null;
            HttpApi.HTTP_RESPONSE_INFO[] knownHeaderInfo = null;
            List<GCHandle> pinnedHeaders;
            GCHandle gcHandle;
            /*
            // here we would check for BoundaryType.Raw, in this case we wouldn't need to do anything
            if (m_BoundaryType==BoundaryType.Raw) {
                return null;
            }
            */
            if (Headers.Count == 0)
            {
                return null;
            }
            string headerName;
            string headerValue;
            int lookup;
            byte[] bytes = null;
            pinnedHeaders = new List<GCHandle>();

            int numUnknownHeaders = 0;
            int numKnownMultiHeaders = 0;
            foreach (KeyValuePair<string, string[]> headerPair in Headers)
            {
                if (headerPair.Value.Length == 0)
                {
                    // TODO: Have the collection exclude empty headers.
                    continue;
                }
                // See if this is an unknown header
                lookup = HttpApi.HTTP_RESPONSE_HEADER_ID.IndexOfKnownHeader(headerPair.Key);

                // Http.Sys doesn't let us send the Connection: Upgrade header as a Known header.
                if (lookup == -1 ||
                    (isOpaqueUpgrade && lookup == (int)HttpApi.HTTP_RESPONSE_HEADER_ID.Enum.HttpHeaderConnection))
                {
                    numUnknownHeaders += headerPair.Value.Length;
                }
                else if (headerPair.Value.Length > 1)
                {
                    numKnownMultiHeaders++;
                }
                // else known single-value header.
            }

            try
            {
                fixed (HttpApi.HTTP_KNOWN_HEADER* pKnownHeaders = &_nativeResponse.Response_V1.Headers.KnownHeaders)
                {
                    foreach (KeyValuePair<string, string[]> headerPair in Headers)
                    {
                        if (headerPair.Value.Length == 0)
                        {
                            // TODO: Have the collection exclude empty headers.
                            continue;
                        }
                        headerName = headerPair.Key;
                        string[] headerValues = headerPair.Value;
                        lookup = HttpApi.HTTP_RESPONSE_HEADER_ID.IndexOfKnownHeader(headerName);

                        // Http.Sys doesn't let us send the Connection: Upgrade header as a Known header.
                        if (lookup == -1 ||
                            (isOpaqueUpgrade && lookup == (int)HttpApi.HTTP_RESPONSE_HEADER_ID.Enum.HttpHeaderConnection))
                        {
                            if (unknownHeaders == null)
                            {
                                unknownHeaders = new HttpApi.HTTP_UNKNOWN_HEADER[numUnknownHeaders];
                                gcHandle = GCHandle.Alloc(unknownHeaders, GCHandleType.Pinned);
                                pinnedHeaders.Add(gcHandle);
                                _nativeResponse.Response_V1.Headers.pUnknownHeaders = (HttpApi.HTTP_UNKNOWN_HEADER*)gcHandle.AddrOfPinnedObject();
                            }

                            for (int headerValueIndex = 0; headerValueIndex < headerValues.Length; headerValueIndex++)
                            {
                                // Add Name
                                bytes = new byte[HeaderEncoding.GetByteCount(headerName)];
                                unknownHeaders[_nativeResponse.Response_V1.Headers.UnknownHeaderCount].NameLength = (ushort)bytes.Length;
                                HeaderEncoding.GetBytes(headerName, 0, bytes.Length, bytes, 0);
                                gcHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                                pinnedHeaders.Add(gcHandle);
                                unknownHeaders[_nativeResponse.Response_V1.Headers.UnknownHeaderCount].pName = (sbyte*)gcHandle.AddrOfPinnedObject();

                                // Add Value
                                headerValue = headerValues[headerValueIndex];
                                bytes = new byte[HeaderEncoding.GetByteCount(headerValue)];
                                unknownHeaders[_nativeResponse.Response_V1.Headers.UnknownHeaderCount].RawValueLength = (ushort)bytes.Length;
                                HeaderEncoding.GetBytes(headerValue, 0, bytes.Length, bytes, 0);
                                gcHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                                pinnedHeaders.Add(gcHandle);
                                unknownHeaders[_nativeResponse.Response_V1.Headers.UnknownHeaderCount].pRawValue = (sbyte*)gcHandle.AddrOfPinnedObject();
                                _nativeResponse.Response_V1.Headers.UnknownHeaderCount++;
                            }
                        }
                        else if (headerPair.Value.Length == 1)
                        {
                            headerValue = headerValues[0];
                            if (headerValue != null)
                            {
                                bytes = new byte[HeaderEncoding.GetByteCount(headerValue)];
                                pKnownHeaders[lookup].RawValueLength = (ushort)bytes.Length;
                                HeaderEncoding.GetBytes(headerValue, 0, bytes.Length, bytes, 0);
                                gcHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                                pinnedHeaders.Add(gcHandle);
                                pKnownHeaders[lookup].pRawValue = (sbyte*)gcHandle.AddrOfPinnedObject();
                            }
                        }
                        else
                        {
                            if (knownHeaderInfo == null)
                            {
                                knownHeaderInfo = new HttpApi.HTTP_RESPONSE_INFO[numKnownMultiHeaders];
                                gcHandle = GCHandle.Alloc(knownHeaderInfo, GCHandleType.Pinned);
                                pinnedHeaders.Add(gcHandle);
                                _nativeResponse.pResponseInfo = (HttpApi.HTTP_RESPONSE_INFO*)gcHandle.AddrOfPinnedObject();
                            }

                            knownHeaderInfo[_nativeResponse.ResponseInfoCount].Type = HttpApi.HTTP_RESPONSE_INFO_TYPE.HttpResponseInfoTypeMultipleKnownHeaders;
                            knownHeaderInfo[_nativeResponse.ResponseInfoCount].Length = (uint)Marshal.SizeOf<HttpApi.HTTP_MULTIPLE_KNOWN_HEADERS>();

                            HttpApi.HTTP_MULTIPLE_KNOWN_HEADERS header = new HttpApi.HTTP_MULTIPLE_KNOWN_HEADERS();

                            header.HeaderId = (HttpApi.HTTP_RESPONSE_HEADER_ID.Enum)lookup;
                            header.Flags = HttpApi.HTTP_RESPONSE_INFO_FLAGS.PreserveOrder; // TODO: The docs say this is for www-auth only.

                            HttpApi.HTTP_KNOWN_HEADER[] nativeHeaderValues = new HttpApi.HTTP_KNOWN_HEADER[headerValues.Length];
                            gcHandle = GCHandle.Alloc(nativeHeaderValues, GCHandleType.Pinned);
                            pinnedHeaders.Add(gcHandle);
                            header.KnownHeaders = (HttpApi.HTTP_KNOWN_HEADER*)gcHandle.AddrOfPinnedObject();

                            for (int headerValueIndex = 0; headerValueIndex < headerValues.Length; headerValueIndex++)
                            {
                                // Add Value
                                headerValue = headerValues[headerValueIndex];
                                bytes = new byte[HeaderEncoding.GetByteCount(headerValue)];
                                nativeHeaderValues[header.KnownHeaderCount].RawValueLength = (ushort)bytes.Length;
                                HeaderEncoding.GetBytes(headerValue, 0, bytes.Length, bytes, 0);
                                gcHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                                pinnedHeaders.Add(gcHandle);
                                nativeHeaderValues[header.KnownHeaderCount].pRawValue = (sbyte*)gcHandle.AddrOfPinnedObject();
                                header.KnownHeaderCount++;
                            }

                            // This type is a struct, not an object, so pinning it causes a boxed copy to be created. We can't do that until after all the fields are set.
                            gcHandle = GCHandle.Alloc(header, GCHandleType.Pinned);
                            pinnedHeaders.Add(gcHandle);
                            knownHeaderInfo[_nativeResponse.ResponseInfoCount].pInfo = (HttpApi.HTTP_MULTIPLE_KNOWN_HEADERS*)gcHandle.AddrOfPinnedObject();

                            _nativeResponse.ResponseInfoCount++;
                        }
                    }
                }
            }
            catch
            {
                FreePinnedHeaders(pinnedHeaders);
                throw;
            }
            return pinnedHeaders;
        }

        private static void FreePinnedHeaders(List<GCHandle> pinnedHeaders)
        {
            if (pinnedHeaders != null)
            {
                foreach (GCHandle gcHandle in pinnedHeaders)
                {
                    if (gcHandle.IsAllocated)
                    {
                        gcHandle.Free();
                    }
                }
            }
        }

        // Subset of ComputeHeaders
        internal void SendOpaqueUpgrade()
        {
            // Notify that this is absolutely the last chance to make changes.
            Start();
            _boundaryType = BoundaryType.Close;

            // TODO: Send headers async?
            ulong errorCode = SendHeaders(null, null,
                HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_OPAQUE |
                HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA |
                HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_BUFFER_DATA,
                true);

            if (errorCode != ErrorCodes.ERROR_SUCCESS)
            {
                throw new WebListenerException((int)errorCode);
            }
        }

        private void CheckDisposed()
        {
            if (_responseState >= ResponseState.Closed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        internal void CancelLastWrite(SafeHandle requestQueueHandle)
        {
            if (_nativeStream != null)
            {
                _nativeStream.CancelLastWrite(requestQueueHandle);
            }
        }

        public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancel)
        {
            EnsureResponseStream();
            return _nativeStream.SendFileAsync(path, offset, count, cancel);
        }

        internal void SwitchToOpaqueMode()
        {
            EnsureResponseStream();
            _bufferingEnabled = false;
            _nativeStream.SwitchToOpaqueMode();
        }

        public void OnResponseStarting(Func<object, Task> callback, object state)
        {
            var actions = _onResponseStartingActions;
            if (actions == null)
            {
                throw new InvalidOperationException("Response already started");
            }

            actions.Add(new Tuple<Func<object, Task>, object>(callback, state));
        }

        public void OnResponseCompleted(Func<object, Task> callback, object state)
        {
            var actions = _onResponseCompletedActions;
            if (actions == null)
            {
                throw new InvalidOperationException("Response already completed");
            }

            actions.Add(new Tuple<Func<object, Task>, object>(callback, state));
        }

        private void NotifyOnSendingHeaders()
        {
            var actions = Interlocked.Exchange(ref _onResponseStartingActions, null);
            if (actions == null)
            {
                // Something threw the first time, do not try again.
                return;
            }

            // Execute last to first. This mimics a stack unwind.
            foreach (var actionPair in actions.Reverse())
            {
                actionPair.Item1(actionPair.Item2);
            }
        }

        private void NotifyOnResponseCompleted()
        {
            var actions = Interlocked.Exchange(ref _onResponseCompletedActions, null);
            if (actions == null)
            {
                // Something threw the first time, do not try again.
                return;
            }

            foreach (var actionPair in actions)
            {
                try
                {
                    actionPair.Item1(actionPair.Item2);
                }
                catch (Exception ex)
                {
                    RequestContext.Logger.LogWarning(
                        String.Format(Resources.Warning_ExceptionInOnResponseCompletedAction, nameof(OnResponseCompleted)),
                        ex);
                }
            }
        }

        private class SendResponseLogContext : ILogValues
        {
            private readonly Response _response;

            internal SendResponseLogContext(Response response)
            {
                _response = response;
            }

            public string Protocol { get { return "HTTP/1.1"; } } // HTTP.SYS only allows 1.1 responses.
            public string StatusCode { get { return _response.StatusCode.ToString(CultureInfo.InvariantCulture); } }
            public string ReasonPhrase { get { return _response.ReasonPhrase ?? _response.GetReasonPhrase(_response.StatusCode); } }
            public IEnumerable Headers { get { return new HeadersLogStructure(_response.Headers); } }

            public IEnumerable<KeyValuePair<string, object>> GetValues()
            {
                return new[]
                {
                    new KeyValuePair<string, object>("Protocol", Protocol),
                    new KeyValuePair<string, object>("StatusCode", StatusCode),
                    new KeyValuePair<string, object>("ReasonPhrase", ReasonPhrase),
                    new KeyValuePair<string, object>("Headers", Headers),
                };
            }

            public override string ToString()
            {
                // HTTP/1.1 200 OK
                var responseBuilder = new StringBuilder("Sending Response: ");
                responseBuilder.Append(Protocol);
                responseBuilder.Append(" ");
                responseBuilder.Append(StatusCode);
                responseBuilder.Append(" ");
                responseBuilder.Append(ReasonPhrase);
                responseBuilder.Append("; Headers: { ");

                foreach (var header in _response.Headers)
                {
                    foreach (var value in header.Value)
                    {
                        responseBuilder.Append(header.Key);
                        responseBuilder.Append(": ");
                        responseBuilder.Append(value);
                        responseBuilder.Append("; ");
                    }
                }
                responseBuilder.Append("}");
                return responseBuilder.ToString();
            }
        }
    }
}
