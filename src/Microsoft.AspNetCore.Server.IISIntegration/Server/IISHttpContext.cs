// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.AspNetCore.WebUtilities;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    internal abstract partial class IISHttpContext : NativeRequestContext, IDisposable
    {
        private const int MinAllocBufferSize = 2048;
        private const int PauseWriterThreshold = 65536;
        private const int ResumeWriterTheshold = PauseWriterThreshold / 2;

        private static bool UpgradeAvailable = (Environment.OSVersion.Version >= new Version(6, 2));

        protected readonly IntPtr _pInProcessHandler;

        private bool _reading; // To know whether we are currently in a read operation.
        private volatile bool _hasResponseStarted;

        private int _statusCode;
        private string _reasonPhrase;
        private readonly object _onStartingSync = new object();
        private readonly object _onCompletedSync = new object();
        private readonly object _stateSync = new object();
        protected readonly object _createReadWriteBodySync = new object();

        protected Stack<KeyValuePair<Func<object, Task>, object>> _onStarting;
        protected Stack<KeyValuePair<Func<object, Task>, object>> _onCompleted;

        protected Exception _applicationException;
        private readonly MemoryPool<byte> _memoryPool;
        private readonly IISHttpServer _server;

        private GCHandle _thisHandle;
        private MemoryHandle _inputHandle;
        private IISAwaitable _operation = new IISAwaitable();
        protected Task _processBodiesTask;

        protected int _requestAborted;

        private const string NtlmString = "NTLM";
        private const string NegotiateString = "Negotiate";
        private const string BasicString = "Basic";

        internal unsafe IISHttpContext(MemoryPool<byte> memoryPool, IntPtr pInProcessHandler, IISOptions options, IISHttpServer server)
            : base((HttpApiTypes.HTTP_REQUEST*)NativeMethods.http_get_raw_request(pInProcessHandler))
        {
            _thisHandle = GCHandle.Alloc(this);

            _memoryPool = memoryPool;
            _pInProcessHandler = pInProcessHandler;
            _server = server;

            NativeMethods.http_set_managed_context(pInProcessHandler, (IntPtr)_thisHandle);
            unsafe
            {
                Method = GetVerb();

                RawTarget = GetRawUrl();
                // TODO version is slow.
                HttpVersion = GetVersion();
                Scheme = SslStatus != SslStatus.Insecure ? Constants.HttpsScheme : Constants.HttpScheme;
                KnownMethod = VerbId;

                var originalPath = RequestUriBuilder.DecodeAndUnescapePath(GetRawUrlInBytes());

                if (KnownMethod == HttpApiTypes.HTTP_VERB.HttpVerbOPTIONS && string.Equals(RawTarget, "*", StringComparison.Ordinal))
                {
                    PathBase = string.Empty;
                    Path = string.Empty;
                }
                else
                {
                    // Path and pathbase are unescaped by RequestUriBuilder
                    // The UsePathBase middleware will modify the pathbase and path correctly
                    PathBase = string.Empty;
                    Path = originalPath;
                }

                var cookedUrl = GetCookedUrl();
                QueryString = cookedUrl.GetQueryString() ?? string.Empty;

                // TODO: Avoid using long.ToString, it's pretty slow
                RequestConnectionId = ConnectionId.ToString(CultureInfo.InvariantCulture);

                // Copied from WebListener
                // This is the base GUID used by HTTP.SYS for generating the activity ID.
                // HTTP.SYS overwrites the first 8 bytes of the base GUID with RequestId to generate ETW activity ID.
                // The requestId should be set by the NativeRequestContext
                var guid = new Guid(0xffcb4c93, 0xa57f, 0x453c, 0xb6, 0x3f, 0x84, 0x71, 0xc, 0x79, 0x67, 0xbb);
                *((ulong*)&guid) = RequestId;

                // TODO: Also make this not slow
                TraceIdentifier = guid.ToString();

                var localEndPoint = GetLocalEndPoint();
                LocalIpAddress = localEndPoint.GetIPAddress();
                LocalPort = localEndPoint.GetPort();

                var remoteEndPoint = GetRemoteEndPoint();
                RemoteIpAddress = remoteEndPoint.GetIPAddress();
                RemotePort = remoteEndPoint.GetPort();
                StatusCode = 200;

                RequestHeaders = new RequestHeaders(this);
                HttpResponseHeaders = new HeaderCollection(); // TODO Optimize for known headers
                ResponseHeaders = HttpResponseHeaders;

                if (options.ForwardWindowsAuthentication)
                {
                    WindowsUser = GetWindowsPrincipal();
                    if (options.AutomaticAuthentication)
                    {
                        User = WindowsUser;
                    }
                }

                ResetFeatureCollection();
            }

            RequestBody = new IISHttpRequestBody(this);
            ResponseBody = new IISHttpResponseBody(this);

            Input = new Pipe(new PipeOptions(_memoryPool, readerScheduler: PipeScheduler.ThreadPool, minimumSegmentSize: MinAllocBufferSize));
            var pipe = new Pipe(new PipeOptions(
                _memoryPool,
                readerScheduler: PipeScheduler.ThreadPool,
                pauseWriterThreshold: PauseWriterThreshold,
                resumeWriterThreshold: ResumeWriterTheshold,
                minimumSegmentSize: MinAllocBufferSize));
            Output = new OutputProducer(pipe);
        }

        public Version HttpVersion { get; set; }
        public string Scheme { get; set; }
        public string Method { get; set; }
        public string PathBase { get; set; }
        public string Path { get; set; }
        public string QueryString { get; set; }
        public string RawTarget { get; set; }
        public CancellationToken RequestAborted { get; set; }
        public bool HasResponseStarted => _hasResponseStarted;
        public IPAddress RemoteIpAddress { get; set; }
        public int RemotePort { get; set; }
        public IPAddress LocalIpAddress { get; set; }
        public int LocalPort { get; set; }
        public string RequestConnectionId { get; set; }
        public string TraceIdentifier { get; set; }
        public ClaimsPrincipal User { get; set; }
        internal WindowsPrincipal WindowsUser { get; set; }
        public Stream RequestBody { get; set; }
        public Stream ResponseBody { get; set; }
        public Pipe Input { get; set; }
        public OutputProducer Output { get; set; }

        public IHeaderDictionary RequestHeaders { get; set; }
        public IHeaderDictionary ResponseHeaders { get; set; }
        private HeaderCollection HttpResponseHeaders { get; set; }
        internal HttpApiTypes.HTTP_VERB KnownMethod { get; }

        public int StatusCode
        {
            get { return _statusCode; }
            set
            {
                if (_hasResponseStarted)
                {
                    ThrowResponseAlreadyStartedException(nameof(StatusCode));
                }
                _statusCode = (ushort)value;
            }
        }

        public string ReasonPhrase
        {
            get { return _reasonPhrase; }
            set
            {
                if (_hasResponseStarted)
                {
                    ThrowResponseAlreadyStartedException(nameof(ReasonPhrase));
                }
                _reasonPhrase = value;
            }
        }
        
        internal IISHttpServer Server
        {
            get { return _server; }
        }

        private async Task InitializeResponseAwaited()
        {
            await FireOnStarting();

            if (_applicationException != null)
            {
                ThrowResponseAbortedException();
            }

            await ProduceStart(appCompleted: false);
        }

        private void ThrowResponseAbortedException()
        {
            throw new ObjectDisposedException("Unhandled application exception", _applicationException);
        }

        private async Task ProduceStart(bool appCompleted)
        {
            if (_hasResponseStarted)
            {
                return;
            }

            _hasResponseStarted = true;

            SendResponseHeaders(appCompleted);

            // On first flush for websockets, we need to flush the headers such that
            // IIS will know that an upgrade occured.
            // If we don't have anything on the Output pipe, the TryRead in ReadAndWriteLoopAsync
            // will fail and we will signal the upgradeTcs that we are upgrading. However, we still
            // didn't flush. To fix this, we flush 0 bytes right after writing the headers.
            Task flushTask;
            lock (_stateSync)
            {
                DisableReads();
                flushTask = Output.FlushAsync();
            }
            await flushTask;

            StartProcessingRequestAndResponseBody();
        }

        protected Task ProduceEnd()
        {
            if (_applicationException != null)
            {
                if (_hasResponseStarted)
                {
                    // We can no longer change the response, so we simply close the connection.
                    return Task.CompletedTask;
                }

                // If the request was rejected, the error state has already been set by SetBadRequestState and
                // that should take precedence.
                else
                {
                    // 500 Internal Server Error
                    SetErrorResponseHeaders(statusCode: StatusCodes.Status500InternalServerError);
                }
            }

            if (!_hasResponseStarted)
            {
                return ProduceEndAwaited();
            }

            return Task.CompletedTask;
        }

        private void SetErrorResponseHeaders(int statusCode)
        {
            StatusCode = statusCode;
            ReasonPhrase = string.Empty;
            HttpResponseHeaders.Clear();
        }

        private async Task ProduceEndAwaited()
        {
            if (_hasResponseStarted)
            {
                return;
            }

            _hasResponseStarted = true;

            SendResponseHeaders(appCompleted: true);
            StartProcessingRequestAndResponseBody();

            Task flushAsync;

            lock (_stateSync)
            {
                DisableReads();
                flushAsync = Output.FlushAsync();
            }
            await flushAsync;
        }

        public unsafe void SendResponseHeaders(bool appCompleted)
        {
            // Verifies we have sent the statuscode before writing a header
            var reasonPhraseBytes = Encoding.UTF8.GetBytes(ReasonPhrase ?? ReasonPhrases.GetReasonPhrase(StatusCode));

            fixed (byte* pReasonPhrase = reasonPhraseBytes)
            {
                // This copies data into the underlying buffer
                NativeMethods.http_set_response_status_code(_pInProcessHandler, (ushort)StatusCode, pReasonPhrase);
            }

            HttpResponseHeaders.IsReadOnly = true;
            foreach (var headerPair in HttpResponseHeaders)
            {
                var headerValues = headerPair.Value;
                var knownHeaderIndex = HttpApiTypes.HTTP_RESPONSE_HEADER_ID.IndexOfKnownHeader(headerPair.Key);
                if (knownHeaderIndex == -1)
                {
                    var headerNameBytes = Encoding.UTF8.GetBytes(headerPair.Key);
                    for (var i = 0; i < headerValues.Count; i++)
                    {
                        var headerValueBytes = Encoding.UTF8.GetBytes(headerValues[i]);
                        fixed (byte* pHeaderName = headerNameBytes)
                        {
                            fixed (byte* pHeaderValue = headerValueBytes)
                            {
                                NativeMethods.http_response_set_unknown_header(_pInProcessHandler, pHeaderName, pHeaderValue, (ushort)headerValueBytes.Length, fReplace: false);
                            }
                        }
                    }
                }
                else
                {
                    for (var i = 0; i < headerValues.Count; i++)
                    {
                        var headerValueBytes = Encoding.UTF8.GetBytes(headerValues[i]);
                        fixed (byte* pHeaderValue = headerValueBytes)
                        {
                            NativeMethods.http_response_set_known_header(_pInProcessHandler, knownHeaderIndex, pHeaderValue, (ushort)headerValueBytes.Length, fReplace: false);
                        }
                    }
                }
            }
        }

        public void Abort()
        {
            // TODO
        }

        public abstract Task<bool> ProcessRequestAsync();

        public void OnStarting(Func<object, Task> callback, object state)
        {
            lock (_onStartingSync)
            {
                if (_hasResponseStarted)
                {
                    throw new InvalidOperationException("Response already started");
                }

                if (_onStarting == null)
                {
                    _onStarting = new Stack<KeyValuePair<Func<object, Task>, object>>();
                }
                _onStarting.Push(new KeyValuePair<Func<object, Task>, object>(callback, state));
            }
        }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
            lock (_onCompletedSync)
            {
                if (_onCompleted == null)
                {
                    _onCompleted = new Stack<KeyValuePair<Func<object, Task>, object>>();
                }
                _onCompleted.Push(new KeyValuePair<Func<object, Task>, object>(callback, state));
            }
        }

        protected async Task FireOnStarting()
        {
            Stack<KeyValuePair<Func<object, Task>, object>> onStarting = null;
            lock (_onStartingSync)
            {
                onStarting = _onStarting;
                _onStarting = null;
            }
            if (onStarting != null)
            {
                try
                {
                    foreach (var entry in onStarting)
                    {
                        await entry.Key.Invoke(entry.Value);
                    }
                }
                catch (Exception ex)
                {
                    ReportApplicationError(ex);
                }
            }
        }

        protected async Task FireOnCompleted()
        {
            Stack<KeyValuePair<Func<object, Task>, object>> onCompleted = null;
            lock (_onCompletedSync)
            {
                onCompleted = _onCompleted;
                _onCompleted = null;
            }
            if (onCompleted != null)
            {
                foreach (var entry in onCompleted)
                {
                    try
                    {
                        await entry.Key.Invoke(entry.Value);
                    }
                    catch (Exception ex)
                    {
                        ReportApplicationError(ex);
                    }
                }
            }
        }

        protected void ReportApplicationError(Exception ex)
        {
            if (_applicationException == null)
            {
                _applicationException = ex;
            }
            else if (_applicationException is AggregateException)
            {
                _applicationException = new AggregateException(_applicationException, ex).Flatten();
            }
            else
            {
                _applicationException = new AggregateException(_applicationException, ex);
            }
        }

        public void PostCompletion(NativeMethods.REQUEST_NOTIFICATION_STATUS requestNotificationStatus)
        {
            Debug.Assert(!_operation.HasContinuation, "Pending async operation!");

            var hr = NativeMethods.http_set_completion_status(_pInProcessHandler, requestNotificationStatus);
            if (hr != NativeMethods.S_OK)
            {
                throw Marshal.GetExceptionForHR(hr);
            }

            hr = NativeMethods.http_post_completion(_pInProcessHandler, 0);
            if (hr != NativeMethods.S_OK)
            {
                throw Marshal.GetExceptionForHR(hr);
            }
        }

        public void IndicateCompletion(NativeMethods.REQUEST_NOTIFICATION_STATUS notificationStatus)
        {
            NativeMethods.http_indicate_completion(_pInProcessHandler, notificationStatus);
        }

        internal void OnAsyncCompletion(int hr, int cbBytes)
        {
            // Must acquire the _stateSync here as anytime we call complete, we need to hold the lock
            // to avoid races with cancellation.
            Action continuation;
            lock (_stateSync)
            {
                _reading = false;
                continuation = _operation.GetCompletion(hr, cbBytes);
            }

            continuation?.Invoke();
        }

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    _thisHandle.Free();
                }
                if (WindowsUser?.Identity is WindowsIdentity wi)
                {
                    wi.Dispose();
                }
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public override void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(disposing: true);
        }

        private void ThrowResponseAlreadyStartedException(string value)
        {
            throw new InvalidOperationException("Response already started");
        }

        private WindowsPrincipal GetWindowsPrincipal()
        {
            var hr = NativeMethods.http_get_authentication_information(_pInProcessHandler, out var authenticationType, out var token);

            if (hr == 0 && token != IntPtr.Zero && authenticationType != null)
            {
                if ((authenticationType.Equals(NtlmString, StringComparison.OrdinalIgnoreCase)
                    || authenticationType.Equals(NegotiateString, StringComparison.OrdinalIgnoreCase)
                    || authenticationType.Equals(BasicString, StringComparison.OrdinalIgnoreCase)))
                {
                    return new WindowsPrincipal(new WindowsIdentity(token, authenticationType));
                }
            }
            return null;
        }
    }
}
