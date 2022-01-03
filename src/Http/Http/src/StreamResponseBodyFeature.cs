// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// An implementation of <see cref="IHttpResponseBodyFeature"/> that aproximates all of the APIs over the given Stream.
/// </summary>
public class StreamResponseBodyFeature : IHttpResponseBodyFeature
{
    private PipeWriter? _pipeWriter;
    private bool _started;
    private bool _completed;
    private bool _disposed;

    /// <summary>
    /// Wraps the given stream.
    /// </summary>
    /// <param name="stream"></param>
    public StreamResponseBodyFeature(Stream stream)
    {
        Stream = stream ?? throw new ArgumentNullException(nameof(stream));
    }

    /// <summary>
    /// Wraps the given stream and tracks the prior feature instance.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="priorFeature"></param>
    public StreamResponseBodyFeature(Stream stream, IHttpResponseBodyFeature? priorFeature)
    {
        Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        PriorFeature = priorFeature;
    }

    /// <summary>
    /// The original response body stream.
    /// </summary>
    public Stream Stream { get; }

    /// <summary>
    /// The prior feature, if any.
    /// </summary>
    public IHttpResponseBodyFeature? PriorFeature { get; }

    /// <summary>
    /// A PipeWriter adapted over the given stream.
    /// </summary>
    public PipeWriter Writer
    {
        get
        {
            if (_pipeWriter == null)
            {
                _pipeWriter = PipeWriter.Create(Stream, new StreamPipeWriterOptions(leaveOpen: true));
                if (_completed)
                {
                    _pipeWriter.Complete();
                }
            }

            return _pipeWriter;
        }
    }

    /// <summary>
    /// Opts out of write buffering for the response.
    /// </summary>
    public virtual void DisableBuffering()
    {
        PriorFeature?.DisableBuffering();
    }

    /// <summary>
    /// Copies the specified file segment to the given response stream.
    /// This calls StartAsync if it has not previously been called.
    /// </summary>
    /// <param name="path">The full disk path to the file.</param>
    /// <param name="offset">The offset in the file to start at.</param>
    /// <param name="count">The number of bytes to send, or null to send the remainder of the file.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to abort the transmission.</param>
    /// <returns></returns>
    public virtual async Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken)
    {
        if (!_started)
        {
            await StartAsync(cancellationToken);
        }
        await SendFileFallback.SendFileAsync(Stream, path, offset, count, cancellationToken);
    }

    /// <summary>
    /// Flushes the given stream if this has not previously been called.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public virtual Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!_started)
        {
            _started = true;
            return Stream.FlushAsync(cancellationToken);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// This calls StartAsync if it has not previously been called.
    /// It will complete the adapted pipe if it exists.
    /// </summary>
    /// <returns></returns>
    public virtual async Task CompleteAsync()
    {
        // CompleteAsync is registered with HttpResponse.OnCompleted and there's no way to unregister it.
        // Prevent it from running by marking as disposed.
        if (_disposed)
        {
            return;
        }
        if (_completed)
        {
            return;
        }

        if (!_started)
        {
            await StartAsync();
        }

        _completed = true;

        if (_pipeWriter != null)
        {
            await _pipeWriter.CompleteAsync();
        }
    }

    /// <summary>
    /// Prevents CompleteAsync from operating.
    /// </summary>
    public void Dispose()
    {
        _disposed = true;
    }
}
