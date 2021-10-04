// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Net.Quic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal
{
    internal partial class QuicStreamContext : TransportConnection, IPooledStream, IDisposable
    {
        // Internal for testing.
        internal Task _processingTask = Task.CompletedTask;

        private QuicStream? _stream;
        private readonly QuicConnectionContext _connection;
        private readonly QuicTransportContext _context;
        private readonly Pipe _inputPipe;
        private readonly Pipe _outputPipe;
        private readonly IDuplexPipe _originalTransport;
        private readonly IDuplexPipe _originalApplication;
        private readonly CompletionPipeReader _transportPipeReader;
        private readonly CompletionPipeWriter _transportPipeWriter;
        private readonly QuicTrace _log;
        private CancellationTokenSource _streamClosedTokenSource = default!;
        private string? _connectionId;
        private const int MinAllocBufferSize = 4096;
        private volatile Exception? _shutdownReadReason;
        private volatile Exception? _shutdownWriteReason;
        private volatile Exception? _shutdownReason;
        private volatile Exception? _writeAbortException;
        private bool _streamClosed;
        private bool _serverAborted;
        private bool _clientAbort;
        private TaskCompletionSource _waitForConnectionClosedTcs = default!;
        private readonly object _shutdownLock = new object();

        public QuicStreamContext(QuicConnectionContext connection, QuicTransportContext context)
        {
            _connection = connection;
            _context = context;
            _log = context.Log;
            MemoryPool = connection.MemoryPool;
            MultiplexedConnectionFeatures = connection.Features;

            RemoteEndPoint = connection.RemoteEndPoint;
            LocalEndPoint = connection.LocalEndPoint;

            var maxReadBufferSize = context.Options.MaxReadBufferSize ?? 0;
            var maxWriteBufferSize = context.Options.MaxWriteBufferSize ?? 0;

            // TODO should we allow these PipeScheduler to be configurable here?
            var inputOptions = new PipeOptions(MemoryPool, PipeScheduler.ThreadPool, PipeScheduler.Inline, maxReadBufferSize, maxReadBufferSize / 2, useSynchronizationContext: false);
            var outputOptions = new PipeOptions(MemoryPool, PipeScheduler.Inline, PipeScheduler.ThreadPool, maxWriteBufferSize, maxWriteBufferSize / 2, useSynchronizationContext: false);

            _inputPipe = new Pipe(inputOptions);
            _outputPipe = new Pipe(outputOptions);

            _transportPipeReader = new CompletionPipeReader(_inputPipe.Reader);
            _transportPipeWriter = new CompletionPipeWriter(_outputPipe.Writer);

            _originalApplication = new DuplexPipe(_outputPipe.Reader, _inputPipe.Writer);
            _originalTransport = new DuplexPipe(_transportPipeReader, _transportPipeWriter);
        }

        public override MemoryPool<byte> MemoryPool { get; }
        private PipeWriter Input => Application.Output;
        private PipeReader Output => Application.Input;

        public bool CanReuse { get; private set; }

        public void Initialize(QuicStream stream)
        {
            Debug.Assert(_stream == null);

            _stream = stream;

            if (!(_streamClosedTokenSource?.TryReset() ?? false))
            {
                _streamClosedTokenSource = new CancellationTokenSource();
            }

            ConnectionClosed = _streamClosedTokenSource.Token;

            InitializeFeatures();

            CanRead = _stream.CanRead;
            CanWrite = _stream.CanWrite;
            _error = null;
            StreamId = _stream.StreamId;
            PoolExpirationTicks = 0;

            Transport = _originalTransport;
            Application = _originalApplication;

            _transportPipeReader.Reset();
            _transportPipeWriter.Reset();

            _connectionId = null;
            _shutdownReason = null;
            _writeAbortException = null;
            _streamClosed = false;
            _serverAborted = false;
            _clientAbort = false;
            // TODO - resetable TCS
            _waitForConnectionClosedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            // Only reset pipes if the stream has been reused.
            if (CanReuse)
            {
                _inputPipe.Reset();
                _outputPipe.Reset();
            }

            CanReuse = false;
        }

        public override string ConnectionId
        {
            get => _connectionId ??= StringUtilities.ConcatAsHexSuffix(_connection.ConnectionId, ':', (uint)StreamId);
            set => _connectionId = value;
        }

        public long PoolExpirationTicks { get; set; }

        public void Start()
        {
            Debug.Assert(_processingTask.IsCompletedSuccessfully);

            _processingTask = StartAsync();
        }

        private async Task StartAsync()
        {
            Debug.Assert(_stream != null);

            try
            {
                // Spawn send and receive logic
                // Streams may or may not have reading/writing, so only start tasks accordingly
                var receiveTask = Task.CompletedTask;
                var sendTask = Task.CompletedTask;

                if (_stream.CanRead)
                {
                    receiveTask = DoReceive();
                }

                if (_stream.CanWrite)
                {
                    sendTask = DoSend();
                }

                // Now wait for both to complete
                await receiveTask;
                await sendTask;

                await FireStreamClosedAsync();
            }
            catch (Exception ex)
            {
                _log.LogError(0, ex, $"Unexpected exception in {nameof(QuicStreamContext)}.{nameof(StartAsync)}.");
            }
        }

        private async Task WaitForWritesCompleted()
        {
            Debug.Assert(_stream != null);

            try
            {
                await _stream.WaitForWriteCompletionAsync();
            }
            catch (Exception ex)
            {
                // Send error to DoSend loop.
                _writeAbortException = ex;
            }
            finally
            {
                Output.CancelPendingRead();
            }
        }

        private async Task DoReceive()
        {
            Debug.Assert(_stream != null);

            Exception? error = null;

            try
            {
                var input = Input;
                while (true)
                {
                    var buffer = input.GetMemory(MinAllocBufferSize);
                    var bytesReceived = await _stream.ReadAsync(buffer);

                    if (bytesReceived == 0)
                    {
                        // Read completed.
                        break;
                    }

                    input.Advance(bytesReceived);

                    ValueTask<FlushResult> flushTask;

                    if (_stream.ReadsCompleted)
                    {
                        // If the data returned from ReadAsync is the final chunk on the stream then
                        // flush data and end pipe together with CompleteAsync.
                        //
                        // Getting data and complete together is important for HTTP/3 when parsing headers.
                        // It is important that it knows that there is no body after the headers.
                        var completeTask = input.CompleteAsync();
                        if (completeTask.IsCompletedSuccessfully)
                        {
                            // Fast path. CompleteAsync completed immediately.
                            // Most implementations of ValueTask reset state in GetResult.
                            completeTask.GetAwaiter().GetResult();

                            flushTask = ValueTask.FromResult(new FlushResult(isCanceled: false, isCompleted: true));
                        }
                        else
                        {
                            flushTask = AwaitCompleteTaskAsync(completeTask);
                        }
                    }
                    else
                    {
                        flushTask = input.FlushAsync();
                    }

                    var paused = !flushTask.IsCompleted;

                    if (paused)
                    {
                        _log.StreamPause(this);
                    }

                    var result = await flushTask;

                    if (paused)
                    {
                        _log.StreamResume(this);
                    }

                    if (result.IsCompleted || result.IsCanceled)
                    {
                        // Pipe consumer is shut down, do we stop writing
                        break;
                    }
                }
            }
            catch (QuicStreamAbortedException ex)
            {
                // Abort from peer.
                _error = ex.ErrorCode;
                _log.StreamAbortedRead(this, ex.ErrorCode);

                // This could be ignored if _shutdownReason is already set.
                error = new ConnectionResetException(ex.Message, ex);

                _clientAbort = true;
            }
            catch (QuicConnectionAbortedException ex)
            {
                // Abort from peer.
                _error = ex.ErrorCode;
                _log.StreamAbortedRead(this, ex.ErrorCode);

                // This could be ignored if _shutdownReason is already set.
                error = new ConnectionResetException(ex.Message, ex);

                _clientAbort = true;
            }
            catch (QuicOperationAbortedException ex)
            {
                // AbortRead has been called for the stream.
                error = new ConnectionAbortedException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                // This is unexpected.
                error = ex;
                _log.StreamError(this, error);
            }
            finally
            {
                // If Shutdown() has already bee called, assume that was the reason ProcessReceives() exited.
                Input.Complete(ResolveCompleteReceiveException(error));
            }

            async static ValueTask<FlushResult> AwaitCompleteTaskAsync(ValueTask completeTask)
            {
                await completeTask;
                return new FlushResult(isCanceled: false, isCompleted: true);
            }
        }

        private Exception? ResolveCompleteReceiveException(Exception? error)
        {
            return _shutdownReadReason ?? _shutdownReason ?? error;
        }

        private Task FireStreamClosedAsync()
        {
            // Guard against scheduling this multiple times
            if (_streamClosed)
            {
                return Task.CompletedTask;
            }

            _streamClosed = true;

            ThreadPool.UnsafeQueueUserWorkItem(state =>
            {
                state.CancelConnectionClosedToken();

                state._waitForConnectionClosedTcs.TrySetResult();
            },
            this,
            preferLocal: false);

            return _waitForConnectionClosedTcs.Task;
        }

        private void CancelConnectionClosedToken()
        {
            try
            {
                _streamClosedTokenSource.Cancel();
            }
            catch (Exception ex)
            {
                _log.LogError(0, ex, $"Unexpected exception in {nameof(QuicStreamContext)}.{nameof(CancelConnectionClosedToken)}.");
            }
        }

        private async Task DoSend()
        {
            Debug.Assert(_stream != null);

            Exception? shutdownReason = null;
            Exception? unexpectedError = null;

            var sendCompletedTask = WaitForWritesCompleted();

            try
            {
                // Resolve `output` PipeReader via the IDuplexPipe interface prior to loop start for performance.
                var output = Output;
                while (true)
                {
                    var result = await output.ReadAsync();

                    if (result.IsCanceled)
                    {
                        // WaitForWritesCompleted provides immediate notification that write-side of stream has completed.
                        // If the stream or connection is aborted then exception will be available to rethrow.
                        if (_writeAbortException != null)
                        {
                            ExceptionDispatchInfo.Throw(_writeAbortException);
                        }

                        break;
                    }

                    var buffer = result.Buffer;

                    var end = buffer.End;
                    var isCompleted = result.IsCompleted;
                    if (!buffer.IsEmpty)
                    {
                        await _stream.WriteAsync(buffer, endStream: isCompleted);
                    }

                    output.AdvanceTo(end);

                    if (isCompleted)
                    {
                        // Once the stream pipe is closed, shutdown the stream.
                        break;
                    }
                }
            }
            catch (QuicStreamAbortedException ex)
            {
                // Abort from peer.
                _error = ex.ErrorCode;
                _log.StreamAbortedWrite(this, ex.ErrorCode);

                // This could be ignored if _shutdownReason is already set.
                shutdownReason = new ConnectionResetException(ex.Message, ex);

                _clientAbort = true;
            }
            catch (QuicConnectionAbortedException ex)
            {
                // Abort from peer.
                _error = ex.ErrorCode;
                _log.StreamAbortedWrite(this, ex.ErrorCode);

                // This could be ignored if _shutdownReason is already set.
                shutdownReason = new ConnectionResetException(ex.Message, ex);

                _clientAbort = true;
            }
            catch (QuicOperationAbortedException ex)
            {
                // AbortWrite has been called for the stream.
                // Possibily might also get here from connection closing.
                // System.Net.Quic exception handling not finalized.
                shutdownReason = new ConnectionResetException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                shutdownReason = ex;
                unexpectedError = ex;
                _log.StreamError(this, unexpectedError);
            }
            finally
            {
                ShutdownWrite(_shutdownWriteReason ?? _shutdownReason ?? shutdownReason);
                await sendCompletedTask;

                // Complete the output after completing stream sends
                Output.Complete(unexpectedError);

                // Cancel any pending flushes so that the input loop is un-paused
                Input.CancelPendingFlush();
            }
        }

        public override void Abort(ConnectionAbortedException abortReason)
        {
            // This abort is called twice, make sure that doesn't happen.
            // Don't call _stream.Shutdown and _stream.Abort at the same time.
            if (_serverAborted)
            {
                return;
            }

            _serverAborted = true;
            _shutdownReason = abortReason;

            var resolvedErrorCode = _error ?? 0;
            _log.StreamAbort(this, resolvedErrorCode, abortReason.Message);

            lock (_shutdownLock)
            {
                if (_stream != null)
                {
                    if (_stream.CanRead)
                    {
                        _stream.AbortRead(resolvedErrorCode);
                    }
                    if (_stream.CanWrite)
                    {
                        _stream.AbortWrite(resolvedErrorCode);
                    }
                }
            }

            // Cancel ProcessSends loop after calling shutdown to ensure the correct _shutdownReason gets set.
            Output.CancelPendingRead();
        }

        private void ShutdownWrite(Exception? shutdownReason)
        {
            Debug.Assert(_stream != null);

            try
            {
                lock (_shutdownLock)
                {
                    // TODO: Exception is always allocated. Consider only allocating if receive hasn't completed.
                    _shutdownReason = shutdownReason ?? new ConnectionAbortedException("The QUIC transport's send loop completed gracefully.");
                    _log.StreamShutdownWrite(this, _shutdownReason.Message);

                    _stream.Shutdown();
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Stream failed to gracefully shutdown.");
                // Ignore any errors from Shutdown() since we're tearing down the stream anyway.
            }
        }

        public override async ValueTask DisposeAsync()
        {
            if (_stream == null)
            {
                return;
            }

            _originalTransport.Input.Complete();
            _originalTransport.Output.Complete();

            await _processingTask;

            lock (_shutdownLock)
            {
                // CanReuse must not be calculated while draining stream. It is possible for
                // an abort to be received while any methods are still running.
                // It is safe to calculate CanReuse after processing is completed.
                //
                // Be conservative about what can be pooled.
                // Only pool bidirectional streams whose pipes have completed successfully and haven't been aborted.
                CanReuse = _stream.CanRead && _stream.CanWrite
                    && _transportPipeReader.IsCompletedSuccessfully
                    && _transportPipeWriter.IsCompletedSuccessfully
                    && !_clientAbort
                    && !_serverAborted
                    && _shutdownReadReason == null
                    && _shutdownWriteReason == null;

                if (!CanReuse)
                {
                    DisposeCore();
                }

                _stream.Dispose();
                _stream = null!;
            }
        }

        public void Dispose()
        {
            if (!_connection.TryReturnStream(this))
            {
                // Dispose when one of:
                // - Stream is not bidirection
                // - Stream didn't complete gracefully
                // - Pool is full
                DisposeCore();
            }
        }

        // Called when the stream is no longer reused.
        public void DisposeCore()
        {
            _streamClosedTokenSource.Dispose();
        }
    }
}
