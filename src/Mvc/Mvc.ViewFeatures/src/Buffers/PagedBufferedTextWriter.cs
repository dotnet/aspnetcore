// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;

internal sealed class PagedBufferedTextWriter : TextWriter
{
    private readonly TextWriter _inner;
    private readonly PagedCharBuffer _charBuffer;

    public PagedBufferedTextWriter(ArrayPool<char> pool, TextWriter inner)
    {
        _charBuffer = new PagedCharBuffer(new ArrayPoolBufferSource(pool));
        _inner = inner;
    }

    public override Encoding Encoding => _inner.Encoding;

    public override void Flush()
    {
        // Don't do anything. We'll call FlushAsync.
    }

    public override Task FlushAsync() => FlushAsyncCore();

    // private non-virtual for internal calling.
    // It first does a fast check to see if async is necessary, we inline this check.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Task FlushAsyncCore()
    {
        var length = _charBuffer.Length;
        if (length == 0)
        {
            // If nothing sync buffered return CompletedTask,
            // so we can fast-path skip async state-machine creation
            return Task.CompletedTask;
        }

        return FlushAsyncAwaited();
    }

    private async Task FlushAsyncAwaited()
    {
        var length = _charBuffer.Length;
        Debug.Assert(length > 0);

        var pages = _charBuffer.Pages;
        var count = pages.Count;
        for (var i = 0; i < count; i++)
        {
            var page = pages[i];
            var pageLength = Math.Min(length, page.Length);
            if (pageLength != 0)
            {
                await _inner.WriteAsync(page, index: 0, count: pageLength);
            }

            length -= pageLength;
        }

        Debug.Assert(length == 0);
        _charBuffer.Clear();
    }

    public override void Write(char value)
    {
        _charBuffer.Append(value);
    }

    public override void Write(char[] buffer)
    {
        if (buffer == null)
        {
            return;
        }

        _charBuffer.Append(buffer, 0, buffer.Length);
    }

    public override void Write(char[] buffer, int index, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        _charBuffer.Append(buffer, index, count);
    }

    public override void Write(string value)
    {
        if (value == null)
        {
            return;
        }

        _charBuffer.Append(value);
    }

    public override Task WriteAsync(char value)
    {
        var flushTask = FlushAsyncCore();

        return flushTask.IsCompletedSuccessfully ?
            _inner.WriteAsync(value) :
            WriteAsyncAwaited(flushTask, value);
    }

    private async Task WriteAsyncAwaited(Task flushTask, char value)
    {
        await flushTask;
        await _inner.WriteAsync(value);
    }

    public override Task WriteAsync(char[] buffer, int index, int count)
    {
        var flushTask = FlushAsyncCore();

        return flushTask.IsCompletedSuccessfully ?
            _inner.WriteAsync(buffer, index, count) :
            WriteAsyncAwaited(flushTask, buffer, index, count);
    }

    private async Task WriteAsyncAwaited(Task flushTask, char[] buffer, int index, int count)
    {
        await flushTask;
        await _inner.WriteAsync(buffer, index, count);
    }

    public override Task WriteAsync(string value)
    {
        var flushTask = FlushAsyncCore();

        return flushTask.IsCompletedSuccessfully ?
            _inner.WriteAsync(value) :
            WriteAsyncAwaited(flushTask, value);
    }

    private async Task WriteAsyncAwaited(Task flushTask, string value)
    {
        await flushTask;
        await _inner.WriteAsync(value);
    }

    /// <summary>
    /// Writes pre-encoded UTF-8 bytes directly through to the inner writer, bypassing char buffering.
    /// </summary>
    internal void WriteUtf8(ReadOnlySpan<byte> utf8Value)
    {
        if (utf8Value.IsEmpty)
        {
            return;
        }

        // Flush any buffered chars to maintain write ordering
        FlushSyncInternal();

        // Forward to inner writer if it supports direct UTF-8
        if (_inner is HttpResponseStreamWriter responseWriter)
        {
            responseWriter.WriteUtf8(utf8Value);
        }
        else
        {
            // Fallback for writers that don't support direct UTF-8
            _inner.Write(Encoding.UTF8.GetString(utf8Value));
        }
    }

    /// <summary>
    /// Asynchronously writes pre-encoded UTF-8 bytes directly through to the inner writer.
    /// </summary>
    internal async Task WriteUtf8Async(ReadOnlyMemory<byte> utf8Value)
    {
        if (utf8Value.IsEmpty)
        {
            return;
        }

        // Flush buffered chars to maintain write ordering
        await FlushAsyncCore();

        // Forward to inner writer if it supports direct UTF-8
        if (_inner is HttpResponseStreamWriter responseWriter)
        {
            await responseWriter.WriteUtf8Async(utf8Value);
        }
        else
        {
            // Fallback for writers that don't support direct UTF-8
            await _inner.WriteAsync(Encoding.UTF8.GetString(utf8Value.Span));
        }
    }

    // Synchronous flush of buffered char pages to the inner writer.
    // Unlike FlushAsyncAwaited, this writes synchronously and is only used
    // by WriteUtf8 to ensure char data is flushed before raw bytes.
    private void FlushSyncInternal()
    {
        var length = _charBuffer.Length;
        if (length == 0)
        {
            return;
        }

        var pages = _charBuffer.Pages;
        var count = pages.Count;
        for (var i = 0; i < count; i++)
        {
            var page = pages[i];
            var pageLength = Math.Min(length, page.Length);
            if (pageLength != 0)
            {
                _inner.Write(page, index: 0, count: pageLength);
            }

            length -= pageLength;
        }

        Debug.Assert(length == 0);
        _charBuffer.Clear();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _charBuffer.Dispose();
    }
}
