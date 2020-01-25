// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.Extensions.Primitives;
using static Microsoft.AspNetCore.HttpSys.Internal.UnsafeNclNativeMethods;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal sealed class Response
    {
        // Support is assumed until we get an error and turn it off.
        private static bool SupportsGoAway = true;

        private ResponseState _responseState;
        private string _reasonPhrase;
        private ResponseBody _nativeStream;
        private AuthenticationSchemes _authChallenges;
        private TimeSpan? _cacheTtl;
        private long _expectedBodyLength;
        private BoundaryType _boundaryType;
        private HttpApiTypes.HTTP_RESPONSE_V2 _nativeResponse;
        private HeaderCollection _trailers;

        internal Response(RequestContext requestContext)
        {
            // TODO: Verbose log
            RequestContext = requestContext;
            Headers = new HeaderCollection();
            // We haven't started yet, or we're just buffered, we can clear any data, headers, and state so
            // that we can start over (e.g. to write an error message).
            _nativeResponse = new HttpApiTypes.HTTP_RESPONSE_V2();
            Headers.IsReadOnly = false;
            Headers.Clear();
            _reasonPhrase = null;
            _boundaryType = BoundaryType.None;
            _nativeResponse.Response_V1.StatusCode = (ushort)StatusCodes.Status200OK;
            _nativeResponse.Response_V1.Version.MajorVersion = 1;
            _nativeResponse.Response_V1.Version.MinorVersion = 1;
            _responseState = ResponseState.Created;
            _expectedBodyLength = 0;
            _nativeStream = null;
            _cacheTtl = null;
            _authChallenges = RequestContext.Server.Options.Authentication.Schemes;
        }

        private enum ResponseState
        {
            Created,
            ComputedHeaders,
            Started,
            Closed,
        }

        private RequestContext RequestContext { get; }

        private Request Request => RequestContext.Request;

        public int StatusCode
        {
            get { return _nativeResponse.Response_V1.StatusCode; }
            set
            {
                // Http.Sys automatically sends 100 Continue responses when you read from the request body.
                if (value <= 100 || 999 < value)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, string.Format(Resources.Exception_InvalidStatusCode, value));
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

        public Stream Body
        {
            get
            {
                EnsureResponseStream();
                return _nativeStream;
            }
        }

        internal bool BodyIsFinished => _nativeStream?.IsDisposed ?? _responseState >= ResponseState.Closed;

        /// <summary>
        /// The authentication challenges that will be added to the response if the status code is 401.
        /// This must be a subset of the AuthenticationSchemes enabled on the server.
        /// </summary>
        public AuthenticationSchemes AuthenticationChallenges
        {
            get { return _authChallenges; }
            set
            {
                CheckResponseStarted();
                _authChallenges = value;
            }
        }

        private string GetReasonPhrase(int statusCode)
        {
            string reasonPhrase = ReasonPhrase;
            if (string.IsNullOrWhiteSpace(reasonPhrase))
            {
                // If the user hasn't set this then it is generated on the fly if possible.
                reasonPhrase = HttpReasonPhrase.Get(statusCode) ?? string.Empty;
            }
            return reasonPhrase;
        }

        // We MUST NOT send message-body when we send responses with these Status codes
        private static readonly int[] StatusWithNoResponseBody = { 100, 101, 204, 205, 304 };

        private static bool CanSendResponseBody(int responseCode)
        {
            for (int i = 0; i < StatusWithNoResponseBody.Length; i++)
            {
                if (responseCode == StatusWithNoResponseBody[i])
                {
                    return false;
                }
            }
            return true;
        }

        public HeaderCollection Headers { get; }

        public HeaderCollection Trailers => _trailers ??= new HeaderCollection(checkTrailers: true) { IsReadOnly = BodyIsFinished };

        internal bool HasTrailers => _trailers?.Count > 0;

        // Trailers are supported on this OS, it's HTTP/2, and the app added a Trailer response header to announce trailers were intended.
        // Needed to delay the completion of Content-Length responses.
        internal bool TrailersExpected => HasTrailers
            || (HttpApi.SupportsTrailers && Request.ProtocolVersion >= HttpVersion.Version20
                    && Headers.ContainsKey(HttpKnownHeaderNames.Trailer));

        internal long ExpectedBodyLength
        {
            get { return _expectedBodyLength; }
        }

        // Header accessors
        public long? ContentLength
        {
            get { return Headers.ContentLength; }
            set { Headers.ContentLength = value; }
        }

        /// <summary>
        /// Enable kernel caching for the response with the given timeout. Http.Sys determines if the response
        /// can be cached.
        /// </summary>
        public TimeSpan? CacheTtl
        {
            get { return _cacheTtl; }
            set
            {
                CheckResponseStarted();
                _cacheTtl = value;
            }
        }

        // The response is being finished with or without trailers. Mark them as readonly to inform
        // callers if they try to add them too late. E.g. after Content-Length or CompleteAsync().
        internal void MakeTrailersReadOnly()
        {
            if (_trailers != null)
            {
                _trailers.IsReadOnly = true;
            }
        }

        internal void Abort()
        {
            // Update state for HasStarted. Do not attempt a graceful Dispose.
            _responseState = ResponseState.Closed;
        }

        // should only be called from RequestContext
        internal void Dispose()
        {
            if (_responseState >= ResponseState.Closed)
            {
                return;
            }
            // TODO: Verbose log
            EnsureResponseStream();
            _nativeStream.Dispose();
            _responseState = ResponseState.Closed;
        }

        internal BoundaryType BoundaryType
        {
            get { return _boundaryType; }
        }

        internal bool HasComputedHeaders
        {
            get { return _responseState >= ResponseState.ComputedHeaders; }
        }

        /// <summary>
        /// Indicates if the response status, reason, and headers are prepared to send and can
        /// no longer be modified. This is caused by the first write or flush to the response body.
        /// </summary>
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

        private void EnsureResponseStream()
        {
            if (_nativeStream == null)
            {
                _nativeStream = new ResponseBody(RequestContext);
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
        internal unsafe uint SendHeaders(HttpApiTypes.HTTP_DATA_CHUNK[] dataChunks,
            ResponseStreamAsyncResult asyncResult,
            HttpApiTypes.HTTP_FLAGS flags,
            bool isOpaqueUpgrade)
        {
            Debug.Assert(!HasStarted, "HttpListenerResponse::SendHeaders()|SentHeaders is true.");

            _responseState = ResponseState.Started;
            var reasonPhrase = GetReasonPhrase(StatusCode);

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
                    _nativeResponse.Response_V1.pEntityChunks = (HttpApiTypes.HTTP_DATA_CHUNK*)handle.AddrOfPinnedObject();
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

                var cachePolicy = new HttpApiTypes.HTTP_CACHE_POLICY();
                if (_cacheTtl.HasValue && _cacheTtl.Value > TimeSpan.Zero)
                {
                    cachePolicy.Policy = HttpApiTypes.HTTP_CACHE_POLICY_TYPE.HttpCachePolicyTimeToLive;
                    cachePolicy.SecondsToLive = (uint)Math.Min(_cacheTtl.Value.Ticks / TimeSpan.TicksPerSecond, Int32.MaxValue);
                }

                byte[] reasonPhraseBytes = HeaderEncoding.GetBytes(reasonPhrase);
                fixed (byte* pReasonPhrase = reasonPhraseBytes)
                {
                    _nativeResponse.Response_V1.ReasonLength = (ushort)reasonPhraseBytes.Length;
                    _nativeResponse.Response_V1.pReason = (byte*)pReasonPhrase;
                    fixed (HttpApiTypes.HTTP_RESPONSE_V2* pResponse = &_nativeResponse)
                    {
                        statusCode =
                            HttpApi.HttpSendHttpResponse(
                                RequestContext.Server.RequestQueue.Handle,
                                Request.RequestId,
                                (uint)flags,
                                pResponse,
                                &cachePolicy,
                                &bytesSent,
                                IntPtr.Zero,
                                0,
                                asyncResult == null ? SafeNativeOverlapped.Zero : asyncResult.NativeOverlapped,
                                IntPtr.Zero);

                        // GoAway is only supported on later versions. Retry.
                        if (statusCode == ErrorCodes.ERROR_INVALID_PARAMETER
                            && (flags & HttpApiTypes.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_GOAWAY) != 0)
                        {
                            flags &= ~HttpApiTypes.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_GOAWAY;
                            statusCode =
                                HttpApi.HttpSendHttpResponse(
                                    RequestContext.Server.RequestQueue.Handle,
                                    Request.RequestId,
                                    (uint)flags,
                                    pResponse,
                                    &cachePolicy,
                                    &bytesSent,
                                    IntPtr.Zero,
                                    0,
                                    asyncResult == null ? SafeNativeOverlapped.Zero : asyncResult.NativeOverlapped,
                                    IntPtr.Zero);

                            // Succeeded without GoAway, disable them.
                            if (statusCode != ErrorCodes.ERROR_INVALID_PARAMETER)
                            {
                                SupportsGoAway = false;
                            }
                        }

                        if (asyncResult != null &&
                            statusCode == ErrorCodes.ERROR_SUCCESS &&
                            HttpSysListener.SkipIOCPCallbackOnSuccess)
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

        internal HttpApiTypes.HTTP_FLAGS ComputeHeaders(long writeCount, bool endOfRequest = false)
        {
            Headers.IsReadOnly = false; // Temporarily unlock
            if (StatusCode == (ushort)StatusCodes.Status401Unauthorized)
            {
                RequestContext.Server.Options.Authentication.SetAuthenticationChallenge(RequestContext);
            }

            var flags = HttpApiTypes.HTTP_FLAGS.NONE;
            Debug.Assert(!HasComputedHeaders, nameof(HasComputedHeaders) + " is true.");
            _responseState = ResponseState.ComputedHeaders;

            // Gather everything from the request that affects the response:
            var requestVersion = Request.ProtocolVersion;
            var requestConnectionString = Request.Headers[HttpKnownHeaderNames.Connection];
            var isHeadRequest = Request.IsHeadMethod;
            var requestCloseSet = Matches(Constants.Close, requestConnectionString);

            // Gather everything the app may have set on the response:
            // Http.Sys does not allow us to specify the response protocol version, assume this is a HTTP/1.1 response when making decisions.
            var responseConnectionString = Headers[HttpKnownHeaderNames.Connection];
            var transferEncodingString = Headers[HttpKnownHeaderNames.TransferEncoding];
            var responseContentLength = ContentLength;
            var responseCloseSet = Matches(Constants.Close, responseConnectionString);
            var responseChunkedSet = Matches(Constants.Chunked, transferEncodingString);
            var statusCanHaveBody = CanSendResponseBody(RequestContext.Response.StatusCode);

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
                if (_expectedBodyLength == writeCount && !isHeadRequest && !TrailersExpected)
                {
                    // A single write with the whole content-length. Http.Sys will set the content-length for us in this scenario.
                    // If we don't remove it then range requests served from cache will have two.
                    // https://github.com/aspnet/HttpSysServer/issues/167
                    ContentLength = null;
                }
            }
            else if (responseChunkedSet)
            {
                // The application is performing it's own chunking.
                _boundaryType = BoundaryType.PassThrough;
            }
            else if (endOfRequest)
            {
                if (!isHeadRequest && statusCanHaveBody)
                {
                    Headers[HttpKnownHeaderNames.ContentLength] = Constants.Zero;
                }
                _boundaryType = BoundaryType.ContentLength;
                _expectedBodyLength = 0;
            }
            else if (requestVersion == Constants.V1_1)
            {
                _boundaryType = BoundaryType.Chunked;
                Headers[HttpKnownHeaderNames.TransferEncoding] = Constants.Chunked;
            }
            else
            {
                // v1.0 and the length cannot be determined, so we must close the connection after writing data
                // Or v2.0 and chunking isn't required.
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
                flags = HttpApiTypes.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_DISCONNECT;
                if (responseCloseSet && requestVersion >= Constants.V2 && SupportsGoAway)
                {
                    flags |= HttpApiTypes.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_GOAWAY;
                }
            }

            Headers.IsReadOnly = true;
            return flags;
        }

        private static bool Matches(string knownValue, string input)
        {
            return string.Equals(knownValue, input?.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private unsafe List<GCHandle> SerializeHeaders(bool isOpaqueUpgrade)
        {
            Headers.IsReadOnly = true; // Prohibit further modifications.
            HttpApiTypes.HTTP_UNKNOWN_HEADER[] unknownHeaders = null;
            HttpApiTypes.HTTP_RESPONSE_INFO[] knownHeaderInfo = null;
            List<GCHandle> pinnedHeaders;
            GCHandle gcHandle;

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
            foreach (var headerPair in Headers)
            {
                if (headerPair.Value.Count == 0)
                {
                    continue;
                }
                // See if this is an unknown header
                lookup = HttpApiTypes.HTTP_RESPONSE_HEADER_ID.IndexOfKnownHeader(headerPair.Key);

                // Http.Sys doesn't let us send the Connection: Upgrade header as a Known header.
                if (lookup == -1 ||
                    (isOpaqueUpgrade && lookup == (int)HttpApiTypes.HTTP_RESPONSE_HEADER_ID.Enum.HttpHeaderConnection))
                {
                    numUnknownHeaders += headerPair.Value.Count;
                }
                else if (headerPair.Value.Count > 1)
                {
                    numKnownMultiHeaders++;
                }
                // else known single-value header.
            }

            try
            {
                fixed (HttpApiTypes.HTTP_KNOWN_HEADER* pKnownHeaders = &_nativeResponse.Response_V1.Headers.KnownHeaders)
                {
                    foreach (var headerPair in Headers)
                    {
                        if (headerPair.Value.Count == 0)
                        {
                            continue;
                        }
                        headerName = headerPair.Key;
                        StringValues headerValues = headerPair.Value;
                        lookup = HttpApiTypes.HTTP_RESPONSE_HEADER_ID.IndexOfKnownHeader(headerName);

                        // Http.Sys doesn't let us send the Connection: Upgrade header as a Known header.
                        if (lookup == -1 ||
                            (isOpaqueUpgrade && lookup == (int)HttpApiTypes.HTTP_RESPONSE_HEADER_ID.Enum.HttpHeaderConnection))
                        {
                            if (unknownHeaders == null)
                            {
                                unknownHeaders = new HttpApiTypes.HTTP_UNKNOWN_HEADER[numUnknownHeaders];
                                gcHandle = GCHandle.Alloc(unknownHeaders, GCHandleType.Pinned);
                                pinnedHeaders.Add(gcHandle);
                                _nativeResponse.Response_V1.Headers.pUnknownHeaders = (HttpApiTypes.HTTP_UNKNOWN_HEADER*)gcHandle.AddrOfPinnedObject();
                            }

                            for (int headerValueIndex = 0; headerValueIndex < headerValues.Count; headerValueIndex++)
                            {
                                // Add Name
                                bytes = HeaderEncoding.GetBytes(headerName);
                                unknownHeaders[_nativeResponse.Response_V1.Headers.UnknownHeaderCount].NameLength = (ushort)bytes.Length;
                                gcHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                                pinnedHeaders.Add(gcHandle);
                                unknownHeaders[_nativeResponse.Response_V1.Headers.UnknownHeaderCount].pName = (byte*)gcHandle.AddrOfPinnedObject();

                                // Add Value
                                headerValue = headerValues[headerValueIndex] ?? string.Empty;
                                bytes = HeaderEncoding.GetBytes(headerValue);
                                unknownHeaders[_nativeResponse.Response_V1.Headers.UnknownHeaderCount].RawValueLength = (ushort)bytes.Length;
                                gcHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                                pinnedHeaders.Add(gcHandle);
                                unknownHeaders[_nativeResponse.Response_V1.Headers.UnknownHeaderCount].pRawValue = (byte*)gcHandle.AddrOfPinnedObject();
                                _nativeResponse.Response_V1.Headers.UnknownHeaderCount++;
                            }
                        }
                        else if (headerPair.Value.Count == 1)
                        {
                            headerValue = headerValues[0] ?? string.Empty;
                            bytes = HeaderEncoding.GetBytes(headerValue);
                            pKnownHeaders[lookup].RawValueLength = (ushort)bytes.Length;
                            gcHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                            pinnedHeaders.Add(gcHandle);
                            pKnownHeaders[lookup].pRawValue = (byte*)gcHandle.AddrOfPinnedObject();
                        }
                        else
                        {
                            if (knownHeaderInfo == null)
                            {
                                knownHeaderInfo = new HttpApiTypes.HTTP_RESPONSE_INFO[numKnownMultiHeaders];
                                gcHandle = GCHandle.Alloc(knownHeaderInfo, GCHandleType.Pinned);
                                pinnedHeaders.Add(gcHandle);
                                _nativeResponse.pResponseInfo = (HttpApiTypes.HTTP_RESPONSE_INFO*)gcHandle.AddrOfPinnedObject();
                            }

                            knownHeaderInfo[_nativeResponse.ResponseInfoCount].Type = HttpApiTypes.HTTP_RESPONSE_INFO_TYPE.HttpResponseInfoTypeMultipleKnownHeaders;
                            knownHeaderInfo[_nativeResponse.ResponseInfoCount].Length = (uint)Marshal.SizeOf<HttpApiTypes.HTTP_MULTIPLE_KNOWN_HEADERS>();

                            HttpApiTypes.HTTP_MULTIPLE_KNOWN_HEADERS header = new HttpApiTypes.HTTP_MULTIPLE_KNOWN_HEADERS();

                            header.HeaderId = (HttpApiTypes.HTTP_RESPONSE_HEADER_ID.Enum)lookup;
                            header.Flags = HttpApiTypes.HTTP_RESPONSE_INFO_FLAGS.PreserveOrder; // TODO: The docs say this is for www-auth only.

                            HttpApiTypes.HTTP_KNOWN_HEADER[] nativeHeaderValues = new HttpApiTypes.HTTP_KNOWN_HEADER[headerValues.Count];
                            gcHandle = GCHandle.Alloc(nativeHeaderValues, GCHandleType.Pinned);
                            pinnedHeaders.Add(gcHandle);
                            header.KnownHeaders = (HttpApiTypes.HTTP_KNOWN_HEADER*)gcHandle.AddrOfPinnedObject();

                            for (int headerValueIndex = 0; headerValueIndex < headerValues.Count; headerValueIndex++)
                            {
                                // Add Value
                                headerValue = headerValues[headerValueIndex] ?? string.Empty;
                                bytes = HeaderEncoding.GetBytes(headerValue);
                                nativeHeaderValues[header.KnownHeaderCount].RawValueLength = (ushort)bytes.Length;
                                gcHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                                pinnedHeaders.Add(gcHandle);
                                nativeHeaderValues[header.KnownHeaderCount].pRawValue = (byte*)gcHandle.AddrOfPinnedObject();
                                header.KnownHeaderCount++;
                            }

                            // This type is a struct, not an object, so pinning it causes a boxed copy to be created. We can't do that until after all the fields are set.
                            gcHandle = GCHandle.Alloc(header, GCHandleType.Pinned);
                            pinnedHeaders.Add(gcHandle);
                            knownHeaderInfo[_nativeResponse.ResponseInfoCount].pInfo = (HttpApiTypes.HTTP_MULTIPLE_KNOWN_HEADERS*)gcHandle.AddrOfPinnedObject();

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

        internal unsafe void SerializeTrailers(HttpApiTypes.HTTP_DATA_CHUNK[] dataChunks, int currentChunk, List<GCHandle> pins)
        {
            Debug.Assert(currentChunk == dataChunks.Length - 1);
            Debug.Assert(HasTrailers);
            MakeTrailersReadOnly();
            var trailerCount = 0;

            foreach (var trailerPair in Trailers)
            {
                trailerCount += trailerPair.Value.Count;
            }

            var pinnedHeaders = new List<GCHandle>();

            var unknownHeaders = new HttpApiTypes.HTTP_UNKNOWN_HEADER[trailerCount];
            var gcHandle = GCHandle.Alloc(unknownHeaders, GCHandleType.Pinned);
            pinnedHeaders.Add(gcHandle);
            dataChunks[currentChunk].DataChunkType = HttpApiTypes.HTTP_DATA_CHUNK_TYPE.HttpDataChunkTrailers;
            dataChunks[currentChunk].trailers.trailerCount = (ushort)trailerCount;
            dataChunks[currentChunk].trailers.pTrailers = gcHandle.AddrOfPinnedObject();

            try
            {
                var unknownHeadersOffset = 0;

                foreach (var headerPair in Trailers)
                {
                    if (headerPair.Value.Count == 0)
                    {
                        continue;
                    }

                    var headerName = headerPair.Key;
                    var headerValues = headerPair.Value;

                    for (int headerValueIndex = 0; headerValueIndex < headerValues.Count; headerValueIndex++)
                    {
                        // Add Name
                        var bytes = HeaderEncoding.GetBytes(headerName);
                        unknownHeaders[unknownHeadersOffset].NameLength = (ushort)bytes.Length;
                        gcHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                        pinnedHeaders.Add(gcHandle);
                        unknownHeaders[unknownHeadersOffset].pName = (byte*)gcHandle.AddrOfPinnedObject();

                        // Add Value
                        var headerValue = headerValues[headerValueIndex] ?? string.Empty;
                        bytes = HeaderEncoding.GetBytes(headerValue);
                        unknownHeaders[unknownHeadersOffset].RawValueLength = (ushort)bytes.Length;
                        gcHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                        pinnedHeaders.Add(gcHandle);
                        unknownHeaders[unknownHeadersOffset].pRawValue = (byte*)gcHandle.AddrOfPinnedObject();
                        unknownHeadersOffset++;
                    }
                }

                Debug.Assert(unknownHeadersOffset == trailerCount);
            }
            catch
            {
                FreePinnedHeaders(pinnedHeaders);
                throw;
            }

            // Success, keep the pins.
            pins.AddRange(pinnedHeaders);
        }

        // Subset of ComputeHeaders
        internal void SendOpaqueUpgrade()
        {
            _boundaryType = BoundaryType.Close;

            // TODO: Send headers async?
            ulong errorCode = SendHeaders(null, null,
                HttpApiTypes.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_OPAQUE |
                HttpApiTypes.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA |
                HttpApiTypes.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_BUFFER_DATA,
                true);

            if (errorCode != ErrorCodes.ERROR_SUCCESS)
            {
                throw new HttpSysException((int)errorCode);
            }
        }

        internal void CancelLastWrite()
        {
            _nativeStream?.CancelLastWrite();
        }

        public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancel)
        {
            EnsureResponseStream();
            return _nativeStream.SendFileAsync(path, offset, count, cancel);
        }

        internal void SwitchToOpaqueMode()
        {
            EnsureResponseStream();
            _nativeStream.SwitchToOpaqueMode();
        }
    }
}
