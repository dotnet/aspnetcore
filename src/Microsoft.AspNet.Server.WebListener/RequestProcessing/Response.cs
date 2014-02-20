//------------------------------------------------------------------------------
// <copyright file="HttpListenerResponse.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Server.WebListener
{
    internal sealed unsafe class Response : IDisposable
    {
        private ResponseState _responseState;
        private IDictionary<string, string[]> _headers;
        private string _reasonPhrase;
        private ResponseStream _nativeStream;
        private long _contentLength;
        private BoundaryType _boundaryType;
        private UnsafeNclNativeMethods.HttpApi.HTTP_RESPONSE _nativeResponse;
        private IList<Tuple<Action<object>, object>> _onSendingHeadersActions;

        private RequestContext _requestContext;

        internal Response(RequestContext httpContext)
        {
            // TODO: Verbose log
            _requestContext = httpContext;
            _nativeResponse = new UnsafeNclNativeMethods.HttpApi.HTTP_RESPONSE();
            _headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            _boundaryType = BoundaryType.None;
            _nativeResponse.StatusCode = (ushort)HttpStatusCode.OK;
            _nativeResponse.Version.MajorVersion = 1;
            _nativeResponse.Version.MinorVersion = 1;
            _responseState = ResponseState.Created;
            _onSendingHeadersActions = new List<Tuple<Action<object>, object>>();
        }

        private enum ResponseState
        {
            Created,
            ComputedHeaders,
            SentHeaders,
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
            get { return _nativeResponse.StatusCode; }
            set
            {
                if (value <= 100 || 999 < value)
                {
                    throw new ArgumentOutOfRangeException("value", value, string.Format(Resources.Exception_InvalidStatusCode, value));
                }
                CheckResponseStarted();
                _nativeResponse.StatusCode = (ushort)value;
            }
        }

        private void CheckResponseStarted()
        {
            if (_responseState >= ResponseState.SentHeaders)
            {
                throw new InvalidOperationException("Headers already sent.");
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

        internal ResponseStream Body
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

        internal EntitySendFormat EntitySendFormat
        {
            get
            {
                return (EntitySendFormat)_boundaryType;
            }
        }

        public IDictionary<string, string[]> Headers
        {
            get { return _headers; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _headers = value;
            }
        }

        internal long ContentLength64
        {
            get
            {
                return _contentLength;
            }
        }

        private Version GetProtocolVersion()
        {
            /*
            Version requestVersion = Request.ProtocolVersion;
            Version responseVersion = requestVersion;
            string protocolVersion = RequestContext.Environment.Get<string>(Constants.HttpResponseProtocolKey);

            // Optional
            if (!string.IsNullOrWhiteSpace(protocolVersion))
            {
                if (string.Equals("HTTP/1.1", protocolVersion, StringComparison.OrdinalIgnoreCase))
                {
                    responseVersion = Constants.V1_1;
                }
                if (string.Equals("HTTP/1.0", protocolVersion, StringComparison.OrdinalIgnoreCase))
                {
                    responseVersion = Constants.V1_0;
                }
                else
                {
                    // TODO: Just log? It's too late to get this to user code.
                    throw new ArgumentException(string.Empty, Constants.HttpResponseProtocolKey);
                }
            }

            if (requestVersion == responseVersion)
            {
                return requestVersion;
            }

            // Return the lesser of the two versions. There are only two, so it it will always be 1.0.
            return Constants.V1_0;*/

            // TODO: IHttpResponseInformation does not define a response protocol version. Http.Sys doesn't let
            // us send anything but 1.1 anyways, but we could at least use it to set things like the connection header.
            return Request.ProtocolVersion;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
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
        }

        // old API, now private, and helper methods

        internal BoundaryType BoundaryType
        {
            get
            {
                return _boundaryType;
            }
        }

        internal bool SentHeaders
        {
            get
            {
                return _responseState >= ResponseState.SentHeaders;
            }
        }

        internal bool ComputedHeaders
        {
            get
            {
                return _responseState >= ResponseState.ComputedHeaders;
            }
        }

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
        internal unsafe uint SendHeaders(UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK* pDataChunk,
            ResponseStreamAsyncResult asyncResult,
            UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS flags,
            bool isOpaqueUpgrade)
        {
            Debug.Assert(!SentHeaders, "HttpListenerResponse::SendHeaders()|SentHeaders is true.");

            // TODO: Verbose log headers
            _responseState = ResponseState.SentHeaders;
            string reasonPhrase = GetReasonPhrase(_nativeResponse.StatusCode);

            /*
            if (m_BoundaryType==BoundaryType.Raw) {
                use HTTP_SEND_RESPONSE_FLAG_RAW_HEADER;
            }
            */
            uint statusCode;
            uint bytesSent;
            List<GCHandle> pinnedHeaders = SerializeHeaders(ref _nativeResponse.Headers, isOpaqueUpgrade);
            try
            {
                if (pDataChunk != null)
                {
                    _nativeResponse.EntityChunkCount = 1;
                    _nativeResponse.pEntityChunks = pDataChunk;
                }
                else if (asyncResult != null && asyncResult.DataChunks != null)
                {
                    _nativeResponse.EntityChunkCount = asyncResult.DataChunkCount;
                    _nativeResponse.pEntityChunks = asyncResult.DataChunks;
                }
                else
                {
                    _nativeResponse.EntityChunkCount = 0;
                    _nativeResponse.pEntityChunks = null;
                }

                if (reasonPhrase.Length > 0)
                {
                    byte[] reasonPhraseBytes = new byte[HeaderEncoding.GetByteCount(reasonPhrase)];
                    fixed (byte* pReasonPhrase = reasonPhraseBytes)
                    {
                        _nativeResponse.ReasonLength = (ushort)reasonPhraseBytes.Length;
                        HeaderEncoding.GetBytes(reasonPhrase, 0, reasonPhraseBytes.Length, reasonPhraseBytes, 0);
                        _nativeResponse.pReason = (sbyte*)pReasonPhrase;
                        fixed (UnsafeNclNativeMethods.HttpApi.HTTP_RESPONSE* pResponse = &_nativeResponse)
                        {
                            statusCode =
                                UnsafeNclNativeMethods.HttpApi.HttpSendHttpResponse(
                                    RequestContext.RequestQueueHandle,
                                    Request.RequestId,
                                    (uint)flags,
                                    pResponse,
                                    null,
                                    &bytesSent,
                                    SafeLocalFree.Zero,
                                    0,
                                    asyncResult == null ? SafeNativeOverlapped.Zero : asyncResult.NativeOverlapped,
                                    IntPtr.Zero);

                            if (asyncResult != null &&
                                statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS &&
                                OwinWebListener.SkipIOCPCallbackOnSuccess)
                            {
                                asyncResult.BytesSent = bytesSent;
                                // The caller will invoke IOCompleted
                            }
                        }
                    }
                }
                else
                {
                    fixed (UnsafeNclNativeMethods.HttpApi.HTTP_RESPONSE* pResponse = &_nativeResponse)
                    {
                        statusCode =
                            UnsafeNclNativeMethods.HttpApi.HttpSendHttpResponse(
                                RequestContext.RequestQueueHandle,
                                Request.RequestId,
                                (uint)flags,
                                pResponse,
                                null,
                                &bytesSent,
                                SafeLocalFree.Zero,
                                0,
                                asyncResult == null ? SafeNativeOverlapped.Zero : asyncResult.NativeOverlapped,
                                IntPtr.Zero);

                        if (asyncResult != null &&
                            statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS &&
                            OwinWebListener.SkipIOCPCallbackOnSuccess)
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

        internal UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS ComputeHeaders(bool endOfRequest = false)
        {
            // Notify that this is absolutely the last chance to make changes.
            NotifyOnSendingHeaders();

            // 401
            if (StatusCode == (ushort)HttpStatusCode.Unauthorized)
            {
                RequestContext.Server.AuthenticationManager.SetAuthenticationChallenge(this);
            }

            UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS flags = UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.NONE;
            Debug.Assert(!ComputedHeaders, "HttpListenerResponse::ComputeHeaders()|ComputedHeaders is true.");
            _responseState = ResponseState.ComputedHeaders;
            /*
            // here we would check for BoundaryType.Raw, in this case we wouldn't need to do anything
            if (m_BoundaryType==BoundaryType.Raw) {
                return flags;
            }
            */

            // Check the response headers to determine the correct keep alive and boundary type.
            Version responseVersion = GetProtocolVersion();
            _nativeResponse.Version.MajorVersion = (ushort)responseVersion.Major;
            _nativeResponse.Version.MinorVersion = (ushort)responseVersion.Minor;
            bool keepAlive = responseVersion >= Constants.V1_1;
            string connectionString = Headers.Get(HttpKnownHeaderNames.Connection);
            string keepAliveString = Headers.Get(HttpKnownHeaderNames.KeepAlive);
            bool closeSet = false;
            bool keepAliveSet = false;

            if (!string.IsNullOrWhiteSpace(connectionString) && string.Equals("close", connectionString.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                keepAlive = false;
                closeSet = true;
            }
            else if (!string.IsNullOrWhiteSpace(keepAliveString) && string.Equals("true", keepAliveString.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                keepAlive = true;
                keepAliveSet = true;
            }

            // Content-Length takes priority
            string contentLengthString = Headers.Get(HttpKnownHeaderNames.ContentLength);
            string transferEncodingString = Headers.Get(HttpKnownHeaderNames.TransferEncoding);

            if (responseVersion == Constants.V1_0 && !string.IsNullOrEmpty(transferEncodingString)
                && string.Equals("chunked", transferEncodingString.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                // A 1.0 client can't process chunked responses.
                Headers.Remove(HttpKnownHeaderNames.TransferEncoding);
                transferEncodingString = null;
            }

            if (!string.IsNullOrWhiteSpace(contentLengthString))
            {
                contentLengthString = contentLengthString.Trim();
                if (string.Equals("0", contentLengthString, StringComparison.Ordinal))
                {
                    _boundaryType = BoundaryType.ContentLength;
                    _contentLength = 0;
                    flags = UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.NONE;
                }
                else if (long.TryParse(contentLengthString, NumberStyles.None, CultureInfo.InvariantCulture.NumberFormat, out _contentLength))
                {
                    _boundaryType = BoundaryType.ContentLength;
                    if (_contentLength == 0)
                    {
                        flags = UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.NONE;
                    }
                }
                else
                {
                    _boundaryType = BoundaryType.Invalid;
                }
            }
            else if (!string.IsNullOrWhiteSpace(transferEncodingString)
                && string.Equals("chunked", transferEncodingString.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                // Then Transfer-Encoding: chunked
                _boundaryType = BoundaryType.Chunked;
            }
            else if (endOfRequest)
            {
                // The request is ending without a body, add a Content-Length: 0 header.
                Headers[HttpKnownHeaderNames.ContentLength] = new string[] { "0" };
                _boundaryType = BoundaryType.ContentLength;
                _contentLength = 0;
                flags = UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.NONE;
            }
            else
            {
                // Then fall back to Connection:Close transparent mode.
                _boundaryType = BoundaryType.None;
                flags = UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.NONE; // seems like HTTP_SEND_RESPONSE_FLAG_MORE_DATA but this hangs the app;
                if (responseVersion == Constants.V1_0)
                {
                    keepAlive = false;
                }
                else
                {
                    Headers[HttpKnownHeaderNames.TransferEncoding] = new string[] { "chunked" };
                    _boundaryType = BoundaryType.Chunked;
                }

                if (CanSendResponseBody(_requestContext.Response.StatusCode))
                {
                    _contentLength = -1;
                }
                else
                {
                    Headers[HttpKnownHeaderNames.ContentLength] = new string[] { "0" };
                    _contentLength = 0;
                    _boundaryType = BoundaryType.ContentLength;
                }
            }

            // Also, Keep-Alive vs Connection Close

            if (!keepAlive)
            {
                if (!closeSet)
                {
                    Headers.Append(HttpKnownHeaderNames.Connection, "close");
                }
                if (flags == UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.NONE)
                {
                    flags = UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_DISCONNECT;
                }
            }
            else
            {
                if (Request.ProtocolVersion.Minor == 0 && !keepAliveSet)
                {
                    Headers[HttpKnownHeaderNames.KeepAlive] = new string[] { "true" };
                }
            }
            return flags;
        }

        private List<GCHandle> SerializeHeaders(ref UnsafeNclNativeMethods.HttpApi.HTTP_RESPONSE_HEADERS headers,
            bool isOpaqueUpgrade)
        {
            UnsafeNclNativeMethods.HttpApi.HTTP_UNKNOWN_HEADER[] unknownHeaders = null;
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

            //---------------------------------------------------
            // DTS Issue: 609383:
            // The Set-Cookie headers are being merged into one. 
            // There are two issues here. 
            // 1. When Set-Cookie headers are set through SetCookie method on the ListenerResponse,
            // there is code in the SetCookie method and the methods it calls to flatten the Set-Cookie
            // values. This blindly concatenates the cookies with a comma delimiter. There could be 
            // a cookie value that contains comma, but we don't escape it with %XX value
            //  
            // As an alternative users can add the Set-Cookie header through the AddHeader method
            // like ListenerResponse.Headers.Add("name", "value")
            // That way they can add multiple headers - AND They can format the value like they want it.
            //
            // 2. Now that the header collection contains multiple Set-Cookie name, value pairs
            // you would think the problem would go away. However here is an interesting thing.
            // For NameValueCollection, when you add 
            // "Set-Cookie", "value1"
            // "Set-Cookie", "value2"
            //  The NameValueCollection.Count == 1. Because there is only one key
            //  NameValueCollection.Get("Set-Cookie") would conveniently take these two values
            //  concatenate them with a comma like 
            //  value1,value2. 
            //  In order to get individual values, you need to use 
            //  string[] values = NameValueCollection.GetValues("Set-Cookie");
            //
            //  -------------------------------------------------------------
            //  So here is the proposed fix here.
            //  We must first to loop through all the NameValueCollection keys
            //  and if the name is a unknown header, we must compute the number of 
            //  values it has. Then, we should allocate that many unknown header array 
            //  elements.
            //  
            //  Note that a part of the fix here is to treat Set-Cookie as an unknown header
            //
            //
            //-----------------------------------------------------------
            int numUnknownHeaders = 0;
            foreach (KeyValuePair<string, string[]> headerPair in Headers)
            {
                // See if this is an unknown header
                lookup = UnsafeNclNativeMethods.HttpApi.HTTP_RESPONSE_HEADER_ID.IndexOfKnownHeader(headerPair.Key);

                // TODO: WWW-Authentiate header
                // TODO: HTTP_RESPONSE_V2 has a HTTP_MULTIPLE_KNOWN_HEADERS option where you can supply multiple values.

                // TODO: Consider any 'known' header that has multiple values as 'unknown'?
                // Treat Set-Cookie as well as Connection header in opaque mode as unknown
                if (lookup == (int)HttpSysResponseHeader.SetCookie ||
                    (isOpaqueUpgrade && lookup == (int)HttpSysResponseHeader.Connection))
                {
                    lookup = -1;
                }

                if (lookup == -1)
                {
                    numUnknownHeaders += headerPair.Value.Length;
                }
            }

            try
            {
                fixed (UnsafeNclNativeMethods.HttpApi.HTTP_KNOWN_HEADER* pKnownHeaders = &headers.KnownHeaders)
                {
                    foreach (KeyValuePair<string, string[]> headerPair in Headers)
                    {
                        headerName = headerPair.Key;
                        lookup = UnsafeNclNativeMethods.HttpApi.HTTP_RESPONSE_HEADER_ID.IndexOfKnownHeader(headerName);
                        if (lookup == (int)HttpSysResponseHeader.SetCookie ||
                            (isOpaqueUpgrade && lookup == (int)HttpSysResponseHeader.Connection))
                        {
                            lookup = -1;
                        }

                        if (lookup == -1)
                        {
                            if (unknownHeaders == null)
                            {
                                //----------------------------------------
                                // *** This following comment is no longer true ***
                                // we waste some memory here (up to 32*41=1312 bytes) but we gain speed
                                // unknownHeaders = new UnsafeNclNativeMethods.HttpApi.HTTP_UNKNOWN_HEADER[Headers.Count-index];
                                //--------------------------------------------
                                unknownHeaders = new UnsafeNclNativeMethods.HttpApi.HTTP_UNKNOWN_HEADER[numUnknownHeaders];
                                gcHandle = GCHandle.Alloc(unknownHeaders, GCHandleType.Pinned);
                                pinnedHeaders.Add(gcHandle);
                                headers.pUnknownHeaders = (UnsafeNclNativeMethods.HttpApi.HTTP_UNKNOWN_HEADER*)gcHandle.AddrOfPinnedObject();
                            }

                            //----------------------------------------
                            // FOR UNKNOWN HEADERS
                            // ALLOW MULTIPLE HEADERS to be added 
                            //---------------------------------------
                            string[] headerValues = headerPair.Value;
                            for (int headerValueIndex = 0; headerValueIndex < headerValues.Length; headerValueIndex++)
                            {
                                // Add Name
                                bytes = new byte[HeaderEncoding.GetByteCount(headerName)];
                                unknownHeaders[headers.UnknownHeaderCount].NameLength = (ushort)bytes.Length;
                                HeaderEncoding.GetBytes(headerName, 0, bytes.Length, bytes, 0);
                                gcHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                                pinnedHeaders.Add(gcHandle);
                                unknownHeaders[headers.UnknownHeaderCount].pName = (sbyte*)gcHandle.AddrOfPinnedObject();

                                // Add Value
                                headerValue = headerValues[headerValueIndex];
                                bytes = new byte[HeaderEncoding.GetByteCount(headerValue)];
                                unknownHeaders[headers.UnknownHeaderCount].RawValueLength = (ushort)bytes.Length;
                                HeaderEncoding.GetBytes(headerValue, 0, bytes.Length, bytes, 0);
                                gcHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                                pinnedHeaders.Add(gcHandle);
                                unknownHeaders[headers.UnknownHeaderCount].pRawValue = (sbyte*)gcHandle.AddrOfPinnedObject();
                                headers.UnknownHeaderCount++;
                            }
                        }
                        else
                        {
                            string[] headerValues = headerPair.Value;
                            headerValue = headerValues.Length == 1 ? headerValues[0] : string.Join(", ", headerValues);
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
            // TODO: Should we do this notification earlier when you still have a chance to change the status code to avoid an upgrade?
            // Notify that this is absolutely the last chance to make changes.
            NotifyOnSendingHeaders();

            // TODO: Send headers async?
            ulong errorCode = SendHeaders(null, null,
                UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_OPAQUE |
                UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA |
                UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_BUFFER_DATA,
                true);

            if (errorCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS)
            {
                throw new WebListenerException((int)errorCode);
            }
        }

        private void CheckDisposed()
        {
            if (_responseState >= ResponseState.Closed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
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
            _nativeStream.SwitchToOpaqueMode();
        }

        public void OnSendingHeaders(Action<object> callback, object state)
        {
            IList<Tuple<Action<object>, object>> actions = _onSendingHeadersActions;
            if (actions == null)
            {
                throw new InvalidOperationException("Headers already sent");
            }

            actions.Add(new Tuple<Action<object>, object>(callback, state));
        }

        private void NotifyOnSendingHeaders()
        {
            var actions = Interlocked.Exchange(ref _onSendingHeadersActions, null);
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
    }
}
