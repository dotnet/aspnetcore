// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.IO.Pipes;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.Extensions.Logging;
using PipeOptions = System.IO.Pipelines.PipeOptions;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes.Internal;

internal sealed class NamedPipeConnection : TransportConnection, IConnectionNamedPipeFeature
{
    private static readonly ConnectionAbortedException SendGracefullyCompletedException = new ConnectionAbortedException("The named pipe transport's send loop completed gracefully.");
    private const int MinAllocBufferSize = 4096;
    private readonly NamedPipeConnectionListener _connectionListener;
    private readonly NamedPipeServerStream _stream;
    private readonly ILogger _log;
    private readonly IDuplexPipe _originalTransport;

    private readonly CancellationTokenSource _connectionClosedTokenSource = new CancellationTokenSource();
    private bool _connectionClosed;
    private bool _connectionShutdown;
    private bool _streamDisconnected;
    private Exception? _shutdownReason;
    private readonly object _shutdownLock = new object();

    // Internal for testing.
    internal Task _receivingTask = Task.CompletedTask;
    internal Task _sendingTask = Task.CompletedTask;

    public NamedPipeConnection(
        NamedPipeConnectionListener connectionListener,
        NamedPipeServerStream stream,
        NamedPipeEndPoint endPoint,
        ILogger logger,
        MemoryPool<byte> memoryPool,
        PipeOptions inputOptions,
        PipeOptions outputOptions)
    {
        _connectionListener = connectionListener;
        _stream = stream;
        _log = logger;
        MemoryPool = memoryPool;
        LocalEndPoint = endPoint;
        ConnectionClosed = _connectionClosedTokenSource.Token;

        var pair = DuplexPipe.CreateConnectionPair(inputOptions, outputOptions);

        Transport = _originalTransport = pair.Transport;
        Application = pair.Application;

        Features.Set<IConnectionNamedPipeFeature>(this);
    }

    public PipeWriter Input => Application.Output;
    public PipeReader Output => Application.Input;
    public override MemoryPool<byte> MemoryPool { get; }
    public NamedPipeServerStream NamedPipe => _stream;

    public void Start()
    {
        try
        {
            // Spawn send and receive logic
            _receivingTask = DoReceiveAsync();
            _sendingTask = DoSendAsync();
        }
        catch (Exception ex)
        {
            _log.LogError(0, ex, $"Unexpected exception in {nameof(NamedPipeConnection)}.{nameof(Start)}.");
        }
    }

    private async Task DoReceiveAsync()
    {
        Exception? error = null;

        try
        {
            var input = Input;
            while (true)
            {
                // Ensure we have some reasonable amount of buffer space
                var buffer = input.GetMemory(MinAllocBufferSize);
                var bytesReceived = await _stream.ReadAsync(buffer);

                if (bytesReceived == 0)
                {
                    // Read completed.
                    NamedPipeLog.ConnectionReadEnd(_log, this);
                    break;
                }

                input.Advance(bytesReceived);

                var flushTask = Input.FlushAsync();

                var paused = !flushTask.IsCompleted;

                if (paused)
                {
                    NamedPipeLog.ConnectionPause(_log, this);
                }

                var result = await flushTask;

                if (paused)
                {
                    NamedPipeLog.ConnectionResume(_log, this);
                }

                if (result.IsCompleted || result.IsCanceled)
                {
                    // Pipe consumer is shut down, do we stop writing
                    break;
                }
            }
        }
        catch (ObjectDisposedException ex)
        {
            // This exception should always be ignored because _shutdownReason should be set.
            error = ex;

            if (!_connectionShutdown)
            {
                // This is unexpected if the socket hasn't been disposed yet.
                NamedPipeLog.ConnectionError(_log, this, error);
            }
        }
        catch (Exception ex)
        {
            // This is unexpected.
            error = ex;
            NamedPipeLog.ConnectionError(_log, this, error);
        }
        finally
        {
            // If Shutdown() has already been called, assume that was the reason ProcessReceives() exited.
            Input.Complete(_shutdownReason ?? error);

            FireConnectionClosed();
        }
    }

    private async Task DoSendAsync()
    {
        Exception? shutdownReason = null;
        Exception? unexpectedError = null;

        try
        {
            while (true)
            {
                var result = await Output.ReadAsync();

                if (result.IsCanceled)
                {
                    break;
                }
                var buffer = result.Buffer;

                if (buffer.IsSingleSegment)
                {
                    // Fast path when the buffer is a single segment.
                    await _stream.WriteAsync(buffer.First);
                }
                else
                {
                    foreach (var segment in buffer)
                    {
                        await _stream.WriteAsync(segment);
                    }
                }

                Output.AdvanceTo(buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }
        }
        catch (ObjectDisposedException ex)
        {
            // This should always be ignored since Shutdown() must have already been called by Abort().
            shutdownReason = ex;
        }
        catch (Exception ex)
        {
            shutdownReason = ex;
            unexpectedError = ex;
            NamedPipeLog.ConnectionError(_log, this, unexpectedError);
        }
        finally
        {
            Shutdown(shutdownReason);

            // Complete the output after disposing the socket
            Output.Complete(unexpectedError);

            // Cancel any pending flushes so that the input loop is un-paused
            Input.CancelPendingFlush();
        }
    }

    private void Shutdown(Exception? shutdownReason)
    {
        lock (_shutdownLock)
        {
            if (_connectionShutdown)
            {
                return;
            }

            // Make sure to close the connection only after the _aborted flag is set.
            // Without this, the RequestsCanBeAbortedMidRead test will sometimes fail when
            // a BadHttpRequestException is thrown instead of a TaskCanceledException.
            _connectionShutdown = true;

            // shutdownReason should only be null if the output was completed gracefully, so no one should ever
            // ever observe the nondescript ConnectionAbortedException except for connection middleware attempting
            // to half close the connection which is currently unsupported.
            _shutdownReason = shutdownReason ?? SendGracefullyCompletedException;
            NamedPipeLog.ConnectionDisconnect(_log, this, _shutdownReason.Message);

            try
            {
                // Try to gracefully disconnect the pipe even for aborts to match other transport behavior.
                _stream.Disconnect();
                _streamDisconnected = true;
            }
            catch
            {
                // Ignore any errors from NamedPipeServerStream.Disconnect() since we're tearing down the connection anyway.
                // _streamDisconnected is not set to true so the stream won't be pooled for reuse.
            }
        }
    }

    private void FireConnectionClosed()
    {
        // Guard against scheduling this multiple times
        lock (_shutdownLock)
        {
            if (_connectionClosed)
            {
                return;
            }

            _connectionClosed = true;
        }

        CancelConnectionClosedToken();
    }

    private void CancelConnectionClosedToken()
    {
        try
        {
            _connectionClosedTokenSource.Cancel();
        }
        catch (Exception ex)
        {
            _log.LogError(0, ex, $"Unexpected exception in {nameof(NamedPipeConnection)}.{nameof(CancelConnectionClosedToken)}.");
        }
    }

    public override void Abort(ConnectionAbortedException abortReason)
    {
        // Try to gracefully close the socket to match libuv behavior.
        Shutdown(abortReason);

        // Cancel ProcessSends loop after calling shutdown to ensure the correct _shutdownReason gets set.
        Output.CancelPendingRead();
    }

    public override async ValueTask DisposeAsync()
    {
        _originalTransport.Input.Complete();
        _originalTransport.Output.Complete();

        try
        {
            // Now wait for both to complete
            await _receivingTask;
            await _sendingTask;
        }
        catch (Exception ex)
        {
            _log.LogError(0, ex, $"Unexpected exception in {nameof(NamedPipeConnection)}.{nameof(Start)}.");
            _stream.Dispose();
            return;
        }

        if (!_streamDisconnected)
        {
            _stream.Dispose();
        }
        else
        {
            _connectionListener.ReturnStream(_stream);
        }
    }
}
