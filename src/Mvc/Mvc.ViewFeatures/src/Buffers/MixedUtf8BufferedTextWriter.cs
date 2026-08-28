// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;

internal sealed class MixedUtf8BufferedTextWriter : TextWriter, IUtf8TextWriter
{
    private readonly PagedBufferedTextWriter _bufferedWriter;
    private readonly HttpResponseStreamWriter _responseWriter;
    private bool _hasBufferedCharacters;

    public MixedUtf8BufferedTextWriter(ArrayPool<char> pool, HttpResponseStreamWriter responseWriter)
    {
        _bufferedWriter = new PagedBufferedTextWriter(pool, responseWriter);
        _responseWriter = responseWriter;
    }

    public override Encoding Encoding => _responseWriter.Encoding;

    public override void Flush()
    {
        _bufferedWriter.Flush();
    }

    public override Task FlushAsync()
    {
        return FlushBufferedCharactersAsync();
    }

    public override void Write(char value)
    {
        _hasBufferedCharacters = true;
        _bufferedWriter.Write(value);
    }

    public override void Write(char[] buffer, int index, int count)
    {
        if (count > 0)
        {
            _hasBufferedCharacters = true;
        }

        _bufferedWriter.Write(buffer, index, count);
    }

    public override void Write(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _hasBufferedCharacters = true;
        }

        _bufferedWriter.Write(value);
    }

    public override Task WriteAsync(char value)
    {
        if (!_hasBufferedCharacters)
        {
            return _responseWriter.WriteAsync(value);
        }

        return ResetBufferedCharactersAfterWriteAsync(_bufferedWriter.WriteAsync(value));
    }

    public override Task WriteAsync(char[] buffer, int index, int count)
    {
        if (!_hasBufferedCharacters)
        {
            return _responseWriter.WriteAsync(buffer, index, count);
        }

        return ResetBufferedCharactersAfterWriteAsync(_bufferedWriter.WriteAsync(buffer, index, count));
    }

    public override Task WriteAsync(string value)
    {
        var flushTask = FlushBufferedCharactersAsync();
        if (flushTask.IsCompletedSuccessfully)
        {
            return _responseWriter.WriteUtf8EncodedAsync(value);
        }

        return WriteAsyncAfterFlush(flushTask, value);
    }

    private async Task WriteAsyncAfterFlush(Task flushTask, string value)
    {
        await flushTask;
        await _responseWriter.WriteUtf8EncodedAsync(value);
    }

    void IUtf8TextWriter.WriteUtf8(ReadOnlySpan<byte> utf8Value)
    {
        if (_hasBufferedCharacters)
        {
            _bufferedWriter.WriteUtf8(utf8Value);
            _hasBufferedCharacters = false;
            return;
        }

        _responseWriter.WriteUtf8(utf8Value);
    }

    Task IUtf8TextWriter.WriteUtf8Async(ReadOnlyMemory<byte> utf8Value)
    {
        if (!_hasBufferedCharacters)
        {
            return _responseWriter.WriteUtf8Async(utf8Value);
        }

        return ResetBufferedCharactersAfterWriteAsync(_bufferedWriter.WriteUtf8Async(utf8Value));
    }

    private Task FlushBufferedCharactersAsync()
    {
        if (!_hasBufferedCharacters)
        {
            return Task.CompletedTask;
        }

        return ResetBufferedCharactersAfterWriteAsync(_bufferedWriter.FlushAsync());
    }

    private Task ResetBufferedCharactersAfterWriteAsync(Task writeTask)
    {
        if (writeTask.IsCompletedSuccessfully)
        {
            _hasBufferedCharacters = false;
            return Task.CompletedTask;
        }

        return ResetBufferedCharactersAfterWriteAsyncCore(writeTask);
    }

    private async Task ResetBufferedCharactersAfterWriteAsyncCore(Task writeTask)
    {
        await writeTask;
        _hasBufferedCharacters = false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _bufferedWriter.Dispose();
        }

        base.Dispose(disposing);
    }
}
