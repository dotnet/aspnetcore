// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.IO.Pipes;
using System.Net;
using System.Threading.Channels;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using NamedPipeOptions = System.IO.Pipes.PipeOptions;
using PipeOptions = System.IO.Pipelines.PipeOptions;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes.Internal;

internal sealed class NamedPipeConnectionListener : IConnectionListener
{
    private readonly ILogger _log;
    private readonly NamedPipeEndPoint _endpoint;
    private readonly NamedPipeTransportOptions _options;
    private readonly ObjectPool<NamedPipeServerStream> _namedPipeServerStreamPool;
    private readonly CancellationTokenSource _listeningTokenSource = new CancellationTokenSource();
    private readonly CancellationToken _listeningToken;
    private readonly Channel<ConnectionContext> _acceptedQueue;
    private readonly MemoryPool<byte> _memoryPool;
    private readonly PipeOptions _inputOptions;
    private readonly PipeOptions _outputOptions;
    private readonly NamedPipeServerStreamPoolPolicy _poolPolicy;
    private Task? _completeListeningTask;
    private int _disposed;

    public NamedPipeConnectionListener(
        NamedPipeEndPoint endpoint,
        NamedPipeTransportOptions options,
        ILoggerFactory loggerFactory,
        ObjectPoolProvider objectPoolProvider)
    {
        _log = loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes");
        _endpoint = endpoint;
        _options = options;
        _memoryPool = options.MemoryPoolFactory();
        _listeningToken = _listeningTokenSource.Token;
        // Have to create the pool here (instead of DI) because the pool is specific to an endpoint.
        _poolPolicy = new NamedPipeServerStreamPoolPolicy(endpoint, options);
        _namedPipeServerStreamPool = objectPoolProvider.Create(_poolPolicy);

        // The OS maintains a backlog of clients that are waiting to connect, so the app queue only stores a single connection.
        // We want to have a queue plus a background task that populates the queue, rather than creating NamedPipeServerStream
        // when AcceptAsync is called, so that the server is always the owner of the pipe name.
        _acceptedQueue = Channel.CreateBounded<ConnectionContext>(new BoundedChannelOptions(capacity: 1));

        var maxReadBufferSize = _options.MaxReadBufferSize ?? 0;
        var maxWriteBufferSize = _options.MaxWriteBufferSize ?? 0;

        _inputOptions = new PipeOptions(_memoryPool, PipeScheduler.ThreadPool, PipeScheduler.Inline, maxReadBufferSize, maxReadBufferSize / 2, useSynchronizationContext: false);
        _outputOptions = new PipeOptions(_memoryPool, PipeScheduler.Inline, PipeScheduler.ThreadPool, maxWriteBufferSize, maxWriteBufferSize / 2, useSynchronizationContext: false);
    }

    internal void ReturnStream(NamedPipeServerStream stream)
    {
        Debug.Assert(!stream.IsConnected, "Stream should have been successfully disconnected to reach this point.");

        // The stream is automatically disposed if there isn't space in the pool.
        _namedPipeServerStreamPool.Return(stream);
    }

    public void Start()
    {
        Debug.Assert(_completeListeningTask == null, "Already started");

        var listeningTasks = new Task[_options.ListenerQueueCount];

        for (var i = 0; i < listeningTasks.Length; i++)
        {
            // Start first stream inline to catch creation errors.
            var initialStream = _namedPipeServerStreamPool.Get();
            _poolPolicy.SetFirstPipeStarted();

            listeningTasks[i] = Task.Run(() => StartAsync(initialStream));
        }

        _completeListeningTask = Task.Run(async () =>
        {
            try
            {
                await Task.WhenAll(listeningTasks);
                _acceptedQueue.Writer.TryComplete();
            }
            catch (Exception ex)
            {
                _acceptedQueue.Writer.TryComplete(ex);
                NamedPipeLog.ConnectionListenerAborted(_log, ex);
            }
        });
    }

    public EndPoint EndPoint => _endpoint;

    private async Task StartAsync(NamedPipeServerStream nextStream)
    {
        while (true)
        {
            try
            {
                var stream = nextStream;

                await stream.WaitForConnectionAsync(_listeningToken);

                var connection = new NamedPipeConnection(this, stream, _endpoint, _log, _memoryPool, _inputOptions, _outputOptions);
                connection.Start();

                // Create the next stream before writing connected stream to the channel.
                // This ensures there is always a created stream and another process can't
                // create a stream with the same name with different a access policy.
                nextStream = _namedPipeServerStreamPool.Get();

                while (!_acceptedQueue.Writer.TryWrite(connection))
                {
                    if (!await _acceptedQueue.Writer.WaitToWriteAsync(_listeningToken))
                    {
                        throw new InvalidOperationException("Accept queue writer was unexpectedly closed.");
                    }
                }
            }
            catch (IOException ex) when (!_listeningToken.IsCancellationRequested)
            {
                // WaitForConnectionAsync can throw IOException when the pipe is broken.
                NamedPipeLog.ConnectionListenerBrokenPipe(_log, ex);

                // Dispose existing pipe, create a new one and continue accepting.
                nextStream.Dispose();
                nextStream = _namedPipeServerStreamPool.Get();
            }
            catch (OperationCanceledException) when (_listeningToken.IsCancellationRequested)
            {
                // Token was canceled. The listener is shutting down.
                break;
            }
        }

        NamedPipeLog.ConnectionListenerQueueExited(_log);
        nextStream.Dispose();
    }

    public async ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default)
    {
        while (await _acceptedQueue.Reader.WaitToReadAsync(cancellationToken))
        {
            if (_acceptedQueue.Reader.TryRead(out var connection))
            {
                NamedPipeLog.AcceptedConnection(_log, connection);
                return connection;
            }
        }

        return null;
    }

    public ValueTask UnbindAsync(CancellationToken cancellationToken = default) => DisposeAsync();

    public async ValueTask DisposeAsync()
    {
        // A stream may be waiting on WaitForConnectionAsync when dispose happens.
        // Cancel the token before dispose to ensure StartAsync exits.
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            _listeningTokenSource.Cancel();
        }

        _listeningTokenSource.Dispose();
        if (_completeListeningTask != null)
        {
            await _completeListeningTask;
        }

        // Dispose pool after listening tasks are complete so there is no chance a stream is fetched from the pool after the pool is disposed.
        // Important to dispose because this empties and disposes streams in the pool.
        (_namedPipeServerStreamPool as IDisposable)?.Dispose();
    }

    private sealed class NamedPipeServerStreamPoolPolicy : IPooledObjectPolicy<NamedPipeServerStream>
    {
        private readonly NamedPipeEndPoint _endpoint;
        private readonly NamedPipeTransportOptions _options;
        private bool _hasFirstPipeStarted;

        public NamedPipeServerStreamPoolPolicy(NamedPipeEndPoint endpoint, NamedPipeTransportOptions options)
        {
            _endpoint = endpoint;
            _options = options;
        }

        public NamedPipeServerStream Create()
        {
            var pipeOptions = NamedPipeOptions.Asynchronous | NamedPipeOptions.WriteThrough;
            if (!_hasFirstPipeStarted)
            {
                // The first server stream created should validate that no one else is listening with a given name.
                // Only the first server stream should make this test. The listener will almost always create multiple streams
                // to listen on multiple threads and to handle parallel requests. The pool policy must be updated that the
                // setting isn't needed after the first stream.
                pipeOptions |= NamedPipeOptions.FirstPipeInstance;
            }
            if (_options.CurrentUserOnly)
            {
                pipeOptions |= NamedPipeOptions.CurrentUserOnly;
            }

            var context = new CreateNamedPipeServerStreamContext
            {
                NamedPipeEndPoint = _endpoint,
                PipeOptions = pipeOptions,
                PipeSecurity = _options.PipeSecurity
            };
            return _options.CreateNamedPipeServerStream(context);
        }

        public bool Return(NamedPipeServerStream obj) => !obj.IsConnected;

        public void SetFirstPipeStarted()
        {
            _hasFirstPipeStarted = true;
        }
    }
}
