// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Google.Api;
using Grpc.Core;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal;

internal sealed class HttpContextStreamWriter<TResponse> : IServerStreamWriter<TResponse>
    where TResponse : class
{
    private readonly JsonTranscodingServerCallContext _context;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly object _writeLock;
    private Task? _writeTask;
    private bool _completed;

    public HttpContextStreamWriter(JsonTranscodingServerCallContext context, JsonSerializerOptions serializerOptions)
    {
        _context = context;
        _serializerOptions = serializerOptions;
        _writeLock = new object();
    }

    public WriteOptions? WriteOptions
    {
        get => _context.WriteOptions;
        set => _context.WriteOptions = value;
    }

    Task IAsyncStreamWriter<TResponse>.WriteAsync(TResponse message, CancellationToken cancellationToken)
    {
        return WriteAsyncCore(message, cancellationToken);
    }

    public Task WriteAsync(TResponse message)
    {
        return WriteAsyncCore(message, CancellationToken.None);
    }

    private async Task WriteAsyncCore(TResponse message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        // Register cancellation token early to ensure request is canceled if cancellation is requested.
        CancellationTokenRegistration? registration = null;
        if (cancellationToken.CanBeCanceled)
        {
            registration = cancellationToken.Register(
                static (state) => ((JsonTranscodingServerCallContext)state!).HttpContext.Abort(),
                _context);
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_completed || _context.CancellationToken.IsCancellationRequested)
            {
                throw new InvalidOperationException("Can't write the message because the request is complete.");
            }

            lock (_writeLock)
            {
                // Pending writes need to be awaited first
                if (IsWriteInProgressUnsynchronized)
                {
                    throw new InvalidOperationException("Can't write the message because the previous write is in progress.");
                }

                // Save write task to track whether it is complete. Must be set inside lock.
                _writeTask = WriteMessageAndDelimiter(message, cancellationToken);
            }

            await _writeTask;
        }
        finally
        {
            registration?.Dispose();
        }
    }

    private async Task WriteMessageAndDelimiter(TResponse message, CancellationToken cancellationToken)
    {
        if (message is HttpBody httpBody)
        {
            _context.EnsureResponseHeaders(httpBody.ContentType);
            await _context.HttpContext.Response.Body.WriteAsync(httpBody.Data.Memory, cancellationToken);
        }
        else
        {
            _context.EnsureResponseHeaders();
            await JsonRequestHelpers.SendMessage(_context, _serializerOptions, message, cancellationToken);
        }

        await _context.HttpContext.Response.Body.WriteAsync(GrpcProtocolConstants.StreamingDelimiter, cancellationToken);
    }

    public void Complete()
    {
        _completed = true;
    }

    /// <summary>
    /// A value indicating whether there is an async write already in progress.
    /// Should only check this property when holding the write lock.
    /// </summary>
    private bool IsWriteInProgressUnsynchronized
    {
        get
        {
            var writeTask = _writeTask;
            return writeTask != null && !writeTask.IsCompleted;
        }
    }
}
