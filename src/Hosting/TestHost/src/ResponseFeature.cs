// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.TestHost;

internal sealed class ResponseFeature : IHttpResponseFeature, IHttpResponseBodyFeature
{
    private readonly HeaderDictionary _headers = new HeaderDictionary();
    private readonly Action<Exception> _abort;

    private Func<Task> _responseStartingAsync = () => Task.CompletedTask;
    private Func<Task> _responseCompletedAsync = () => Task.CompletedTask;
    private int _statusCode;
    private string? _reasonPhrase;

    public ResponseFeature(Action<Exception> abort)
    {
        Headers = _headers;

        // 200 is the default status code all the way down to the host, so we set it
        // here to be consistent with the rest of the hosts when writing tests.
        StatusCode = 200;
        _abort = abort;
    }

    public int StatusCode
    {
        get => _statusCode;
        set
        {
            if (HasStarted)
            {
                throw new InvalidOperationException("The status code cannot be set, the response has already started.");
            }
            if (value < 100)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "The status code cannot be set to a value less than 100");
            }

            _statusCode = value;
        }
    }

    public string? ReasonPhrase
    {
        get => _reasonPhrase;
        set
        {
            if (HasStarted)
            {
                throw new InvalidOperationException("The reason phrase cannot be set, the response has already started.");
            }

            _reasonPhrase = value;
        }
    }

    public IHeaderDictionary Headers { get; set; }

    public Stream Body { get; set; } = default!;

    public Stream Stream => Body;

    internal PipeWriter BodyWriter { get; set; } = default!;

    public PipeWriter Writer => BodyWriter;

    public bool HasStarted { get; set; }

    public void OnStarting(Func<object, Task> callback, object state)
    {
        if (HasStarted)
        {
            throw new InvalidOperationException();
        }

        var prior = _responseStartingAsync;
        _responseStartingAsync = async () =>
        {
            await callback(state);
            await prior();
        };
    }

    public void OnCompleted(Func<object, Task> callback, object state)
    {
        var prior = _responseCompletedAsync;
        _responseCompletedAsync = async () =>
        {
            try
            {
                await callback(state);
            }
            finally
            {
                await prior();
            }
        };
    }

    public async Task FireOnSendingHeadersAsync()
    {
        if (!HasStarted)
        {
            try
            {
                await _responseStartingAsync();
            }
            finally
            {
                HasStarted = true;
                _headers.IsReadOnly = true;
            }
        }
    }

    public Task FireOnResponseCompletedAsync()
    {
        return _responseCompletedAsync();
    }

    public async Task StartAsync(CancellationToken token = default)
    {
        try
        {
            await FireOnSendingHeadersAsync();
        }
        catch (Exception ex)
        {
            _abort(ex);
            throw;
        }
    }

    public void DisableBuffering()
    {
    }

    public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellation)
    {
        return SendFileFallback.SendFileAsync(Stream, path, offset, count, cancellation);
    }

    public Task CompleteAsync()
    {
        return Writer.CompleteAsync().AsTask();
    }
}
