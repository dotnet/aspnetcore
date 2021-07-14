// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Net.Quic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal
{
    internal class QuicStreamContext : TransportConnection, IStreamDirectionFeature, IProtocolErrorCodeFeature, IStreamIdFeature, IPooledStream
    {
        // Internal for testing.
        internal Task _processingTask = Task.CompletedTask;

        private QuicStream _stream = default!;
        private readonly QuicConnectionContext _connection;
        private readonly QuicTransportContext _context;
        private readonly Pipe _inputPipe;
        private readonly Pipe _outputPipe;
        private readonly IDuplexPipe _originalTransport;
        private readonly IDuplexPipe _originalApplication;
        private readonly CompletionPipeReader _transportPipeReader;
        private readonly CompletionPipeWriter _transportPipeWriter;
        private readonly IQuicTrace _log;
        private CancellationTokenSource _streamClosedTokenSource = default!;
        private string? _connectionId;
        private const int MinAllocBufferSize = 4096;
        private volatile Exception? _shutdownReason;
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

        public bool CanRead { get; private set; }
        public bool CanWrite { get; private set; }

        public long StreamId { get; private set; }
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
            Features.Set<IStreamDirectionFeature>(this);
            Features.Set<IProtocolErrorCodeFeature>(this);
            Features.Set<IStreamIdFeature>(this);

            // TODO populate the ITlsConnectionFeature (requires client certs).
            Features.Set<ITlsConnectionFeature>(new FakeTlsConnectionFeature());
            CanRead = _stream.CanRead;
            CanWrite = _stream.CanWrite;
            Error = 0;
            StreamId = _stream.StreamId;
            PoolExpirationTicks = 0;

            Transport = _originalTransport;
            Application = _originalApplication;

            _connectionId = null;
            _shutdownReason = null;
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
            get => _connectionId ??= $"{_connection.ConnectionId}:{StreamId}";
            set => _connectionId = value;
        }

        public long Error { get; set; }

        public long PoolExpirationTicks { get; set; }

        public void Start()
        {
            _processingTask = StartAsync();
        }

        private async Task StartAsync()
        {
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

                CanReuse = _stream.CanRead && _stream.CanWrite
                    && _transportPipeReader.IsCompletedSuccessfully
                    && _transportPipeWriter.IsCompletedSuccessfully
                    && !_clientAbort;
            }
            catch (Exception ex)
            {
                _log.LogError(0, ex, $"Unexpected exception in {nameof(QuicStreamContext)}.{nameof(StartAsync)}.");
            }
        }

        private async Task DoReceive()
        {
            Exception? error = null;

            try
            {
                await ProcessReceives();
            }
            catch (QuicStreamAbortedException ex)
            {
                // Abort from peer.
                _log.StreamAborted(this, ex);

                // This could be ignored if _shutdownReason is already set.
                error = new ConnectionResetException(ex.Message, ex);

                _clientAbort = true;
            }
            catch (QuicOperationAbortedException ex)
            {
                // AbortRead has been called for the stream.
                // Possibily might also get here from connection closing.
                // System.Net.Quic exception handling not finalized.
                error = ex;
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
                Input.Complete(_shutdownReason ?? error);

                FireStreamClosed();

                await _waitForConnectionClosedTcs.Task;
            }
        }

        private async Task ProcessReceives()
        {
            var input = Input;
            while (true)
            {
                var buffer = Input.GetMemory(MinAllocBufferSize);
                var bytesReceived = await _stream.ReadAsync(buffer);

                if (bytesReceived == 0)
                {
                    // Read completed.
                    break;
                }

                input.Advance(bytesReceived);

                var flushTask = input.FlushAsync();

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

        private void FireStreamClosed()
        {
            // Guard against scheduling this multiple times
            if (_streamClosed)
            {
                return;
            }

            _streamClosed = true;

            ThreadPool.UnsafeQueueUserWorkItem(state =>
            {
                state.CancelConnectionClosedToken();

                state._waitForConnectionClosedTcs.TrySetResult();
            },
            this,
            preferLocal: false);
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
            Exception? shutdownReason = null;
            Exception? unexpectedError = null;

            try
            {
                await ProcessSends();
            }
            catch (QuicStreamAbortedException ex)
            {
                // Abort from peer.
                _log.StreamAborted(this, ex);

                // This could be ignored if _shutdownReason is already set.
                shutdownReason = new ConnectionResetException(ex.Message, ex);

                _clientAbort = true;
            }
            catch (QuicOperationAbortedException ex)
            {
                // AbortWrite has been called for the stream.
                // Possibily might also get here from connection closing.
                // System.Net.Quic exception handling not finalized.
                unexpectedError = ex;
            }
            catch (Exception ex)
            {
                shutdownReason = ex;
                unexpectedError = ex;
                _log.StreamError(this, unexpectedError);
            }
            finally
            {
                await ShutdownWrite(shutdownReason);

                // Complete the output after disposing the stream
                Output.Complete(unexpectedError ?? shutdownReason);

                // Cancel any pending flushes so that the input loop is un-paused
                Input.CancelPendingFlush();
            }
        }

        private async Task ProcessSends()
        {
            // Resolve `output` PipeReader via the IDuplexPipe interface prior to loop start for performance.
            var output = Output;
            while (true)
            {
                var result = await output.ReadAsync();

                if (result.IsCanceled)
                {
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

        public override void Abort(ConnectionAbortedException abortReason)
        {
            // This abort is called twice, make sure that doesn't happen.
            // Don't call _stream.Shutdown and _stream.Abort at the same time.
            if (_serverAborted)
            {
                return;
            }

            _serverAborted = true;

            _log.StreamAbort(this, abortReason.Message);

            lock (_shutdownLock)
            {
                if (_stream.CanRead)
                {
                    _stream.AbortRead(Error);
                }
                if (_stream.CanWrite)
                {
                    _stream.AbortWrite(Error);
                }
            }

            // Cancel ProcessSends loop after calling shutdown to ensure the correct _shutdownReason gets set.
            Output.CancelPendingRead();
        }

        private async ValueTask ShutdownWrite(Exception? shutdownReason)
        {
            try
            {
                lock (_shutdownLock)
                {
                    // TODO: Exception is always allocated. Consider only allocating if receive hasn't completed.
                    _shutdownReason = shutdownReason ?? new ConnectionAbortedException("The QUIC transport's send loop completed gracefully.");
                    _log.StreamShutdownWrite(this, _shutdownReason.Message);

                    _stream.Shutdown();
                }

                try
                {
                    // TODO: Take cancellation token?
                    await _stream.ShutdownCompleted();
                }
                catch (QuicOperationAbortedException)
                {
                    // AbortRead or AbortWrite has been called before
                    // or during ShutdownCompleted.
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
            _originalTransport.Input.Complete();
            _originalTransport.Output.Complete();

            await _processingTask;

            _stream.Dispose();
            _stream = null!;

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
