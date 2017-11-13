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
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    internal abstract partial class IISHttpContext : NativeRequestContext, IDisposable
    {
        private const int MinAllocBufferSize = 2048;

        private static bool UpgradeAvailable = (Environment.OSVersion.Version >= new Version(6, 2));

        protected readonly IntPtr _pHttpContext;

        private bool _wasUpgraded;
        private int _statusCode;
        private string _reasonPhrase;
        private readonly object _onStartingSync = new object();
        private readonly object _onCompletedSync = new object();

        protected Stack<KeyValuePair<Func<object, Task>, object>> _onStarting;
        protected Stack<KeyValuePair<Func<object, Task>, object>> _onCompleted;

        protected Exception _applicationException;
        private readonly BufferPool _bufferPool;

        private GCHandle _thisHandle;
        private MemoryHandle _inputHandle;
        private IISAwaitable _operation = new IISAwaitable();

        private IISAwaitable _readWebSocketsOperation;
        private IISAwaitable _writeWebSocketsOperation;

        private TaskCompletionSource<object> _upgradeTcs;

        protected Task _readingTask;
        protected Task _writingTask;

        protected int _requestAborted;

        private CurrentOperationType _currentOperationType;
        private Task _currentOperation = Task.CompletedTask;

        private const string NtlmString = "NTLM";
        private const string NegotiateString = "Negotiate";
        private const string BasicString = "Basic";

        internal unsafe IISHttpContext(BufferPool bufferPool, IntPtr pHttpContext, IISOptions options)
            : base((HttpApiTypes.HTTP_REQUEST*)NativeMethods.http_get_raw_request(pHttpContext))
        {
            _thisHandle = GCHandle.Alloc(this);

            _bufferPool = bufferPool;
            _pHttpContext = pHttpContext;

            NativeMethods.http_set_managed_context(_pHttpContext, (IntPtr)_thisHandle);
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

            Input = new Pipe(new PipeOptions(_bufferPool, readerScheduler: TaskRunScheduler.Default));
            var pipe = new Pipe(new PipeOptions(_bufferPool,  readerScheduler: TaskRunScheduler.Default));
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
        public bool HasResponseStarted { get; set; }
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
        public IPipe Input { get; set; }
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

        private IISAwaitable DoFlushAsync()
        {
            unsafe
            {
                var hr = 0;
                hr = NativeMethods.http_flush_response_bytes(_pHttpContext, out var fCompletionExpected);
                if (!fCompletionExpected)
                {
                    _operation.Complete(hr, 0);
                }
                return _operation;
            }
        }

        public async Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await InitializeResponse(0);
            await Output.FlushAsync(cancellationToken);
        }

        public async Task UpgradeAsync()
        {
            if (_upgradeTcs == null)
            {
                _upgradeTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                await FlushAsync();
                await _upgradeTcs.Task;
            }
        }

        public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            StartReadingRequestBody();

            while (true)
            {
                var result = await Input.Reader.ReadAsync();
                var readableBuffer = result.Buffer;
                try
                {
                    if (!readableBuffer.IsEmpty)
                    {
                        var actual = Math.Min(readableBuffer.Length, count);
                        readableBuffer = readableBuffer.Slice(0, actual);
                        readableBuffer.CopyTo(buffer);
                        return (int)actual;
                    }
                    else if (result.IsCompleted)
                    {
                        return 0;
                    }
                }
                finally
                {
                    Input.Reader.Advance(readableBuffer.End, readableBuffer.End);
                }
            }
        }

        public Task WriteAsync(ArraySegment<byte> data, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!HasResponseStarted)
            {
                return WriteAsyncAwaited(data, cancellationToken);
            }

            // VerifyAndUpdateWrite(data.Count);
            return Output.WriteAsync(data, cancellationToken: cancellationToken);
        }

        public async Task WriteAsyncAwaited(ArraySegment<byte> data, CancellationToken cancellationToken)
        {
            await InitializeResponseAwaited(data.Count);

            // WriteAsyncAwaited is only called for the first write to the body.
            // Ensure headers are flushed if Write(Chunked)Async isn't called.
            await Output.WriteAsync(data, cancellationToken: cancellationToken);
        }

        public Task InitializeResponse(int firstWriteByteCount)
        {
            if (HasResponseStarted)
            {
                return Task.CompletedTask;
            }

            if (_onStarting != null)
            {
                return InitializeResponseAwaited(firstWriteByteCount);
            }

            if (_applicationException != null)
            {
                ThrowResponseAbortedException();
            }

            ProduceStart(appCompleted: false);

            return Task.CompletedTask;
        }

        private async Task InitializeResponseAwaited(int firstWriteByteCount)
        {
            await FireOnStarting();

            if (_applicationException != null)
            {
                ThrowResponseAbortedException();
            }

            ProduceStart(appCompleted: false);
        }

        private void ThrowResponseAbortedException()
        {
            throw new ObjectDisposedException("Unhandled application exception", _applicationException);
        }

        private void ProduceStart(bool appCompleted)
        {
            if (HasResponseStarted)
            {
                return;
            }

            HasResponseStarted = true;

            SendResponseHeaders(appCompleted);

            StartWritingResponseBody();
        }

        protected Task ProduceEnd()
        {
            if (_applicationException != null)
            {
                if (HasResponseStarted)
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
            ProduceStart(appCompleted: true);

            // Force flush
            await Output.FlushAsync();
        }

        public unsafe void SendResponseHeaders(bool appCompleted)
        {
            // Verifies we have sent the statuscode before writing a header
            var reasonPhraseBytes = Encoding.UTF8.GetBytes(ReasonPhrase ?? ReasonPhrases.GetReasonPhrase(StatusCode));

            fixed (byte* pReasonPhrase = reasonPhraseBytes)
            {
                // This copies data into the underlying buffer
                NativeMethods.http_set_response_status_code(_pHttpContext, (ushort)StatusCode, pReasonPhrase);
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
                                NativeMethods.http_response_set_unknown_header(_pHttpContext, pHeaderName, pHeaderValue, (ushort)headerValueBytes.Length, fReplace: false);
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
                            NativeMethods.http_response_set_known_header(_pHttpContext, knownHeaderIndex, pHeaderValue, (ushort)headerValueBytes.Length, fReplace: false);
                        }
                    }
                }
            }
        }

        public void Abort()
        {
            // TODO
        }

        public void StartReadingRequestBody()
        {
            if (_readingTask == null)
            {
                _readingTask = ProcessRequestBody();
            }
        }

        private async Task ProcessRequestBody()
        {
            try
            {
                while (true)
                {
                    // These buffers are pinned
                    var wb = Input.Writer.Alloc(MinAllocBufferSize);
                    _inputHandle = wb.Buffer.Retain(true);

                    try
                    {
                        int read = 0;
                        if (_wasUpgraded)
                        {
                            read = await ReadWebSocketsAsync(wb.Buffer.Length);
                        }
                        else
                        {
                            _currentOperation = _currentOperation.ContinueWith(async (t) =>
                            {
                                _currentOperationType = CurrentOperationType.Read;
                                read = await ReadAsync(wb.Buffer.Length);
                            }).Unwrap();
                            await _currentOperation;
                        }

                        if (read == 0)
                        {
                            break;
                        }

                        wb.Advance(read);
                    }
                    finally
                    {
                        wb.Commit();
                        _inputHandle.Dispose();
                    }

                    var result = await wb.FlushAsync();

                    if (result.IsCompleted || result.IsCancelled)
                    {
                        break;
                    }
                }

                Input.Writer.Complete();
            }
            catch (Exception ex)
            {
                Input.Writer.Complete(ex);
            }
        }

        public void StartWritingResponseBody()
        {
            if (_writingTask == null)
            {
                _writingTask = ProcessResponseBody();
            }
        }

        private async Task ProcessResponseBody()
        {
            while (true)
            {
                ReadResult result;

                try
                {
                    result = await Output.Reader.ReadAsync();
                }
                catch
                {
                    Output.Reader.Complete();
                    return;
                }

                var buffer = result.Buffer;
                var consumed = buffer.End;

                try
                {
                    if (result.IsCancelled)
                    {
                        break;
                    }

                    if (!buffer.IsEmpty)
                    {
                        if (_wasUpgraded)
                        {
                            await WriteAsync(buffer);
                        }
                        else
                        {
                            _currentOperation = _currentOperation.ContinueWith(async (t) =>
                            {
                                _currentOperationType = CurrentOperationType.Write;
                                await WriteAsync(buffer);
                            }).Unwrap();
                            await _currentOperation;
                        }
                    }
                    else if (result.IsCompleted)
                    {
                        break;
                    }
                    else
                    {
                        _currentOperation = _currentOperation.ContinueWith(async (t) =>
                        {
                            _currentOperationType = CurrentOperationType.Flush;
                            await DoFlushAsync();
                        }).Unwrap();
                        await _currentOperation;
                    }

                    _upgradeTcs?.TrySetResult(null);
                }
                finally
                {
                    Output.Reader.Advance(consumed);
                }
            }
            Output.Reader.Complete();
        }

        private unsafe IISAwaitable WriteAsync(ReadableBuffer buffer)
        {
            var fCompletionExpected = false;
            var hr = 0;
            var nChunks = 0;

            if (buffer.IsSingleSpan)
            {
                nChunks = 1;
            }
            else
            {
                foreach (var memory in buffer)
                {
                    nChunks++;
                }
            }

            if (buffer.IsSingleSpan)
            {
                var pDataChunks = stackalloc HttpApiTypes.HTTP_DATA_CHUNK[1];

                fixed (byte* pBuffer = &buffer.First.Span.DangerousGetPinnableReference())
                {
                    ref var chunk = ref pDataChunks[0];

                    chunk.DataChunkType = HttpApiTypes.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                    chunk.fromMemory.pBuffer = (IntPtr)pBuffer;
                    chunk.fromMemory.BufferLength = (uint)buffer.Length;
                    if (_wasUpgraded)
                    {
                        hr = NativeMethods.http_websockets_write_bytes(_pHttpContext, pDataChunks, nChunks, IISAwaitable.WriteCallback, (IntPtr)_thisHandle, out fCompletionExpected);
                    }
                    else
                    {
                        hr = NativeMethods.http_write_response_bytes(_pHttpContext, pDataChunks, nChunks, out fCompletionExpected);
                    }
                }
            }
            else
            {
                // REVIEW: Do we need to guard against this getting too big? It seems unlikely that we'd have more than say 10 chunks in real life
                var pDataChunks = stackalloc HttpApiTypes.HTTP_DATA_CHUNK[nChunks];
                var currentChunk = 0;

                // REVIEW: We don't really need this list since the memory is already pinned with the default pool,
                // but shouldn't assume the pool implementation right now. Unfortunately, this causes a heap allocation...
                var handles = new MemoryHandle[nChunks];

                foreach (var b in buffer)
                {
                    ref var handle = ref handles[currentChunk];
                    ref var chunk = ref pDataChunks[currentChunk];

                    handle = b.Retain(true);

                    chunk.DataChunkType = HttpApiTypes.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                    chunk.fromMemory.BufferLength = (uint)b.Length;
                    chunk.fromMemory.pBuffer = (IntPtr)handle.Pointer;

                    currentChunk++;
                }
                if (_wasUpgraded)
                {
                    hr = NativeMethods.http_websockets_write_bytes(_pHttpContext, pDataChunks, nChunks, IISAwaitable.WriteCallback, (IntPtr)_thisHandle, out fCompletionExpected);
                }
                else
                {
                    hr = NativeMethods.http_write_response_bytes(_pHttpContext, pDataChunks, nChunks, out fCompletionExpected);
                }
                // Free the handles
                foreach (var handle in handles)
                {
                    handle.Dispose();
                }
            }

            if (_wasUpgraded)
            {
                if (!fCompletionExpected)
                {
                    CompleteWriteWebSockets(hr, 0);
                }
                return _writeWebSocketsOperation;
            }
            else
            {
                if (!fCompletionExpected)
                {
                    _operation.Complete(hr, 0);
                }
                return _operation;
            }
        }

        private unsafe IISAwaitable ReadAsync(int length)
        {
            var hr = NativeMethods.http_read_request_bytes(
                            _pHttpContext,
                            (byte*)_inputHandle.Pointer,
                            length,
                            out var dwReceivedBytes,
                            out bool fCompletionExpected);
            if (!fCompletionExpected)
            {
                _operation.Complete(hr, dwReceivedBytes);
            }
            return _operation;
        }

        private unsafe IISAwaitable ReadWebSocketsAsync(int length)
        {
            var hr = 0;
            int dwReceivedBytes;
            bool fCompletionExpected;
            hr = NativeMethods.http_websockets_read_bytes(
                                      _pHttpContext,
                                      (byte*)_inputHandle.Pointer,
                                      length,
                                      IISAwaitable.ReadCallback,
                                      (IntPtr)_thisHandle,
                                      out dwReceivedBytes,
                                      out fCompletionExpected);
            if (!fCompletionExpected)
            {
                CompleteReadWebSockets(hr, dwReceivedBytes);
            }
            return _readWebSocketsOperation;
        }

        public abstract Task<bool> ProcessRequestAsync();

        public void OnStarting(Func<object, Task> callback, object state)
        {
            lock (_onStartingSync)
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

            var hr = NativeMethods.http_set_completion_status(_pHttpContext, requestNotificationStatus);
            if (hr != NativeMethods.S_OK)
            {
                throw Marshal.GetExceptionForHR(hr);
            }

            hr = NativeMethods.http_post_completion(_pHttpContext, 0);
            if (hr != NativeMethods.S_OK)
            {
                throw Marshal.GetExceptionForHR(hr);
            }
        }

        public void IndicateCompletion(NativeMethods.REQUEST_NOTIFICATION_STATUS notificationStatus)
        {
            NativeMethods.http_indicate_completion(_pHttpContext, notificationStatus);
        }

        internal void OnAsyncCompletion(int hr, int cbBytes)
        {
            switch (_currentOperationType)
            {
                case CurrentOperationType.Read:
                case CurrentOperationType.Write:
                    _operation.Complete(hr, cbBytes);
                    break;
                case CurrentOperationType.Flush:
                    _operation.Complete(hr, cbBytes);
                    break;
            }
        }

        internal void CompleteWriteWebSockets(int hr, int cbBytes)
        {
            _writeWebSocketsOperation.Complete(hr, cbBytes);
        }

        internal void CompleteReadWebSockets(int hr, int cbBytes)
        {
            _readWebSocketsOperation.Complete(hr, cbBytes);
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
            Dispose(true);
        }

        private void ThrowResponseAlreadyStartedException(string value)
        {
            throw new InvalidOperationException("Response already started");
        }

        private enum CurrentOperationType
        {
            None,
            Read,
            Write,
            Flush
        }

        private WindowsPrincipal GetWindowsPrincipal()
        {
            var hr = NativeMethods.http_get_authentication_information(_pHttpContext, out var authenticationType, out var token);

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
