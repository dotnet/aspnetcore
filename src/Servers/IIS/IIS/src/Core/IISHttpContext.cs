// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.AspNetCore.Server.IIS.Core.IO;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IIS.Core
{
    internal abstract partial class IISHttpContext : NativeRequestContext, IThreadPoolWorkItem, IDisposable
    {
        private const int MinAllocBufferSize = 2048;
        private const int PauseWriterThreshold = 65536;
        private const int ResumeWriterTheshold = PauseWriterThreshold / 2;

        protected readonly IntPtr _pInProcessHandler;

        private readonly IISServerOptions _options;

        protected Streams _streams;

        private volatile bool _hasResponseStarted;

        private int _statusCode;
        private string _reasonPhrase;
        // Used to synchronize callback registration and native method calls
        private readonly object _contextLock = new object();

        protected Stack<KeyValuePair<Func<object, Task>, object>> _onStarting;
        protected Stack<KeyValuePair<Func<object, Task>, object>> _onCompleted;

        protected Exception _applicationException;
        protected BadHttpRequestException _requestRejectedException;

        private readonly MemoryPool<byte> _memoryPool;
        private readonly IISHttpServer _server;

        private readonly ILogger _logger;

        private GCHandle _thisHandle;
        protected Task _readBodyTask;
        protected Task _writeBodyTask;

        private bool _wasUpgraded;

        protected Pipe _bodyInputPipe;
        protected OutputProducer _bodyOutput;

        private const string NtlmString = "NTLM";
        private const string NegotiateString = "Negotiate";
        private const string BasicString = "Basic";

        internal unsafe IISHttpContext(
            MemoryPool<byte> memoryPool,
            IntPtr pInProcessHandler,
            IISServerOptions options,
            IISHttpServer server,
            ILogger logger)
            : base((HttpApiTypes.HTTP_REQUEST*)NativeMethods.HttpGetRawRequest(pInProcessHandler))
        {
            _memoryPool = memoryPool;
            _pInProcessHandler = pInProcessHandler;
            _options = options;
            _server = server;
            _logger = logger;

            ((IHttpBodyControlFeature)this).AllowSynchronousIO = _options.AllowSynchronousIO;
        }

        public Version HttpVersion { get; set; }
        public string Scheme { get; set; }
        public string Method { get; set; }
        public string PathBase { get; set; }
        public string Path { get; set; }
        public string QueryString { get; set; }
        public string RawTarget { get; set; }

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
        public PipeWriter ResponsePipeWrapper { get; set; }

        protected IAsyncIOEngine AsyncIO { get; set; }

        public IHeaderDictionary RequestHeaders { get; set; }
        public IHeaderDictionary ResponseHeaders { get; set; }
        private HeaderCollection HttpResponseHeaders { get; set; }
        internal HttpApiTypes.HTTP_VERB KnownMethod { get; private set; }

        private bool HasStartedConsumingRequestBody { get; set; }
        public long? MaxRequestBodySize { get; set; }

        protected void InitializeContext()
        {
            _thisHandle = GCHandle.Alloc(this);

            Method = GetVerb();

            RawTarget = GetRawUrl();
            // TODO version is slow.
            HttpVersion = GetVersion();
            Scheme = SslStatus != SslStatus.Insecure ? Constants.HttpsScheme : Constants.HttpScheme;
            KnownMethod = VerbId;
            StatusCode = 200;

            var originalPath = GetOriginalPath();

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

            RequestHeaders = new RequestHeaders(this);
            HttpResponseHeaders = new HeaderCollection();
            ResponseHeaders = HttpResponseHeaders;

            if (_options.ForwardWindowsAuthentication)
            {
                WindowsUser = GetWindowsPrincipal();
                if (_options.AutomaticAuthentication)
                {
                    User = WindowsUser;
                }
            }

            MaxRequestBodySize = _options.MaxRequestBodySize;

            ResetFeatureCollection();

            if (!_server.IsWebSocketAvailable(_pInProcessHandler))
            {
                _currentIHttpUpgradeFeature = null;
            }

            _streams = new Streams(this);

            (RequestBody, ResponseBody) = _streams.Start();

            var pipe = new Pipe(
                new PipeOptions(
                    _memoryPool,
                    readerScheduler: PipeScheduler.ThreadPool,
                    pauseWriterThreshold: PauseWriterThreshold,
                    resumeWriterThreshold: ResumeWriterTheshold,
                    minimumSegmentSize: MinAllocBufferSize));
            _bodyOutput = new OutputProducer(pipe);

            NativeMethods.HttpSetManagedContext(_pInProcessHandler, (IntPtr)_thisHandle);
        }

        private string GetOriginalPath()
        {
            var rawUrlInBytes = GetRawUrlInBytes();

            // Pre Windows 10 RS2 applicationInitialization request might not have pRawUrl set, fallback to cocked url
            if (rawUrlInBytes.Length == 0)
            {
                return GetCookedUrl().GetAbsPath();
            }

            // ApplicationInitialization request might have trailing \0 character included in the length
            // check and skip it
            if (rawUrlInBytes.Length > 0 && rawUrlInBytes[rawUrlInBytes.Length - 1] == 0)
            {
                rawUrlInBytes = rawUrlInBytes.Slice(0, rawUrlInBytes.Length - 1);
            }

            var originalPath = RequestUriBuilder.DecodeAndUnescapePath(rawUrlInBytes);
            return originalPath;
        }

        public int StatusCode
        {
            get { return _statusCode; }
            set
            {
                if (HasResponseStarted)
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
                if (HasResponseStarted)
                {
                    ThrowResponseAlreadyStartedException(nameof(ReasonPhrase));
                }
                _reasonPhrase = value;
            }
        }

        internal IISHttpServer Server => _server;

        private async Task InitializeResponse(bool flushHeaders)
        {
            await FireOnStarting();

            if (_applicationException != null)
            {
                ThrowResponseAbortedException();
            }

            await ProduceStart(flushHeaders);
        }

        private async Task ProduceStart(bool flushHeaders)
        {
            Debug.Assert(_hasResponseStarted == false);

            _hasResponseStarted = true;

            SetResponseHeaders();

            EnsureIOInitialized();
            var canHaveNonEmptyBody = StatusCodeCanHaveBody();
            if (flushHeaders)
            {
                try
                {
                    await AsyncIO.FlushAsync(canHaveNonEmptyBody);
                }
                // Client might be disconnected at this point
                // don't leak the exception
                catch (ConnectionResetException)
                {
                    AbortIO(clientDisconnect: true);
                }
            }

            if (!canHaveNonEmptyBody)
            {
                _bodyOutput.Dispose();
            }
            else
            {
                _writeBodyTask = WriteBody(!flushHeaders);
            }
        }

        private bool StatusCodeCanHaveBody()
        {
            return StatusCode != 204
                && StatusCode != 304;
        }

        private void InitializeRequestIO()
        {
            Debug.Assert(!HasStartedConsumingRequestBody);

            if (RequestHeaders.ContentLength > MaxRequestBodySize)
            {
                BadHttpRequestException.Throw(RequestRejectionReason.RequestBodyTooLarge);
            }

            HasStartedConsumingRequestBody = true;

            EnsureIOInitialized();

            _bodyInputPipe = new Pipe(new PipeOptions(_memoryPool, readerScheduler: PipeScheduler.ThreadPool, minimumSegmentSize: MinAllocBufferSize));
            _readBodyTask = ReadBody();
        }

        private void EnsureIOInitialized()
        {
            // If at this point request was not upgraded just start a normal IO engine
            if (AsyncIO == null)
            {
                AsyncIO = new AsyncIOEngine(_contextLock, _pInProcessHandler);
            }
        }

        private void ThrowResponseAbortedException()
        {
            throw new ObjectDisposedException(CoreStrings.UnhandledApplicationException, _applicationException);
        }

        protected Task ProduceEnd()
        {
            if (_requestRejectedException != null || _applicationException != null)
            {
                if (HasResponseStarted)
                {
                    // We can no longer change the response, so we simply close the connection.
                    return Task.CompletedTask;
                }

                // If the request was rejected, the error state has already been set by SetBadRequestState and
                // that should take precedence.
                if (_requestRejectedException != null)
                {
                    SetErrorResponseException(_requestRejectedException);
                }
                else
                {
                    // 500 Internal Server Error
                    SetErrorResponseHeaders(statusCode: StatusCodes.Status500InternalServerError);
                }
            }

            if (!HasResponseStarted)
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
            await ProduceStart(flushHeaders: true);
            await _bodyOutput.FlushAsync(default);
        }

        public unsafe void SetResponseHeaders()
        {
            // Verifies we have sent the statuscode before writing a header
            var reasonPhrase = string.IsNullOrEmpty(ReasonPhrase) ? ReasonPhrases.GetReasonPhrase(StatusCode) : ReasonPhrase;

            // This copies data into the underlying buffer
            NativeMethods.HttpSetResponseStatusCode(_pInProcessHandler, (ushort)StatusCode, reasonPhrase);

            HttpResponseHeaders.IsReadOnly = true;
            foreach (var headerPair in HttpResponseHeaders)
            {
                var headerValues = headerPair.Value;

                if (headerPair.Value.Count == 0)
                {
                    continue;
                }

                var knownHeaderIndex = HttpApiTypes.HTTP_RESPONSE_HEADER_ID.IndexOfKnownHeader(headerPair.Key);
                for (var i = 0; i < headerValues.Count; i++)
                {
                    if (string.IsNullOrEmpty(headerValues[i]))
                    {
                        continue;
                    }

                    var isFirst = i == 0;
                    var headerValueBytes = Encoding.UTF8.GetBytes(headerValues[i]);
                    fixed (byte* pHeaderValue = headerValueBytes)
                    {
                        if (knownHeaderIndex == -1)
                        {
                            var headerNameBytes = Encoding.UTF8.GetBytes(headerPair.Key);
                            fixed (byte* pHeaderName = headerNameBytes)
                            {
                                NativeMethods.HttpResponseSetUnknownHeader(_pInProcessHandler, pHeaderName, pHeaderValue, (ushort)headerValueBytes.Length, fReplace: isFirst);
                            }
                        }
                        else
                        {
                            NativeMethods.HttpResponseSetKnownHeader(_pInProcessHandler, knownHeaderIndex, pHeaderValue, (ushort)headerValueBytes.Length, fReplace: isFirst);
                        }
                    }
                }
            }
        }

        public abstract Task ProcessRequestAsync();

        public void OnStarting(Func<object, Task> callback, object state)
        {
            lock (_contextLock)
            {
                if (HasResponseStarted)
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
            lock (_contextLock)
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
            lock (_contextLock)
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
            lock (_contextLock)
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

        public void SetBadRequestState(BadHttpRequestException ex)
        {
            Log.ConnectionBadRequest(_logger, RequestConnectionId, ex);

            if (!HasResponseStarted)
            {
                SetErrorResponseException(ex);
            }

            _requestRejectedException = ex;
        }

        private void SetErrorResponseException(BadHttpRequestException ex)
        {
            SetErrorResponseHeaders(ex.StatusCode);
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

            Log.ApplicationError(_logger, ((IHttpConnectionFeature)this).ConnectionId, TraceIdentifier, ex);
        }

        public void PostCompletion(NativeMethods.REQUEST_NOTIFICATION_STATUS requestNotificationStatus)
        {
            NativeMethods.HttpSetCompletionStatus(_pInProcessHandler, requestNotificationStatus);
            NativeMethods.HttpPostCompletion(_pInProcessHandler, 0);
        }

        internal void OnAsyncCompletion(int hr, int bytes)
        {
            AsyncIO.NotifyCompletion(hr, bytes);
        }

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _thisHandle.Free();
                }

                if (WindowsUser?.Identity is WindowsIdentity wi)
                {
                    wi.Dispose();
                }

                // Lock to prevent CancelRequestAbortedToken from attempting to cancel a disposed CTS.
                CancellationTokenSource localAbortCts = null;

                lock (_abortLock)
                {
                    localAbortCts = _abortedCts;
                    _abortedCts = null;
                }

                localAbortCts?.Dispose();

                disposedValue = true;
            }
        }

        public override void Dispose()
        {
            Dispose(disposing: true);
        }

        private void ThrowResponseAlreadyStartedException(string name)
        {
            throw new InvalidOperationException(CoreStrings.FormatParameterReadOnlyAfterResponseStarted(name));
        }

        private WindowsPrincipal GetWindowsPrincipal()
        {
            NativeMethods.HttpGetAuthenticationInformation(_pInProcessHandler, out var authenticationType, out var token);

            if (token != IntPtr.Zero && authenticationType != null)
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

        // Invoked by the thread pool
        public void Execute()
        {
            _ = HandleRequest();
        }

        private async Task HandleRequest()
        {
            try
            {
                await ProcessRequestAsync();
            }
            catch (Exception ex)
            {
               _logger.LogError(0, ex, $"Unexpected exception in {nameof(IISHttpContext)}.{nameof(HandleRequest)}.");
            }
            finally
            {
                // Dispose the context
                Dispose();
            }
        }
    }
}
