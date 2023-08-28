// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;

namespace Microsoft.AspNetCore.Components.Endpoints.Rendering;

internal readonly struct TextChunk
{
    public readonly TextChunkType Type;
    public readonly string? StringValue;
    public readonly char CharValue;
    public readonly ArraySegment<char> CharArraySegmentValue;

    public TextChunk(string value)
    {
        Type = TextChunkType.String;
        StringValue = value;
    }

    public TextChunk(char value)
    {
        Type = TextChunkType.Char;
        CharValue = value;
    }

    public TextChunk(ArraySegment<char> value)
    {
        Type = TextChunkType.CharArraySegment;
        CharArraySegmentValue = value;
    }

    public enum TextChunkType { String, Char, CharArraySegment };
}

internal class TextChunkPage(TextChunk[] _buffer)
{
    private int _count;

    public TextChunk[] Buffer => _buffer;
    public int Count => _count;

    public bool TryAdd(TextChunk value)
    {
        if (_count < _buffer.Length - 1)
        {
            _buffer[_count++] = value;
            return true;
        }
        else
        {
            return false;
        }
    }

    public void Clear()
    {
        Array.Clear(_buffer, 0, _count);
        _count = 0;
    }
}

internal readonly struct TextChunkPagePool(ArrayPool<TextChunk> underlyingPool, int pageLength) : IDisposable
{
    private readonly List<TextChunkPage> _allPages = new();
    private readonly Stack<TextChunkPage> _availablePages = new();

    public TextChunkPage Rent()
    {
        if (_availablePages.TryPop(out var existing))
        {
            return existing;
        }
        else
        {
            var newRawPage = underlyingPool.Rent(pageLength);
            var result = new TextChunkPage(newRawPage);
            _allPages.Add(result);
            return result;
        }
    }

    public void Return(TextChunkPage page)
    {
        page.Clear();
        _availablePages.Push(page);
    }

    public void Dispose()
    {
        foreach (var page in _allPages)
        {
            page.Clear();
            underlyingPool.Return(page.Buffer);
        }
    }
}

internal class StringListBuilder(ArrayPool<TextChunk> pool, int pageLength) : IDisposable
{
    private readonly TextChunkPagePool _pageSource = new TextChunkPagePool(pool, pageLength);
    private TextChunkPage? _currentPage;
    private List<TextChunkPage>? _priorPages;

    public void Add(TextChunk value)
    {
        if (_currentPage is null)
        {
            _currentPage = _pageSource.Rent();
        }

        if (!_currentPage.TryAdd(value))
        {
            _priorPages ??= new();
            _priorPages.Add(_currentPage);
            _currentPage = _pageSource.Rent();
            if (!_currentPage.TryAdd(value))
            {
                throw new InvalidOperationException("New page didn't accept write");
            }
        }
    }

    public IEnumerable<TextChunk> GetContents()
    {
        if (_priorPages is not null)
        {
            foreach (var page in _priorPages)
            {
                var (count, buffer) = (page.Count, page.Buffer);
                for (var i = 0; i < count; i++)
                {
                    yield return buffer[i];
                }
            }
        }

        if (_currentPage is not null)
        {
            var (count, buffer) = (_currentPage.Count, _currentPage.Buffer);
            for (var i = 0; i < count; i++)
            {
                yield return buffer[i];
            }
        }
    }

    public void Clear()
    {
        if (_currentPage is not null)
        {
            _pageSource.Return(_currentPage);
            _currentPage = null;
        }

        if (_priorPages is not null)
        {
            foreach (var page in _priorPages)
            {
                _pageSource.Return(page);
            }

            _priorPages.Clear();
        }
    }

    public void Dispose()
    {
        Clear();
    }
}

internal class BufferedTextWriter : TextWriter
{
    private const int PageSize = 64;
    private readonly TextWriter _underlying;
    private StringListBuilder _currentOutput;
    private StringListBuilder? _previousOutput;
    private Task _currentFlushAsyncTask = Task.CompletedTask;

    public BufferedTextWriter(TextWriter underlying)
    {
        _underlying = underlying;
        _currentOutput = new(ArrayPool<TextChunk>.Shared, PageSize);
    }

    public override void Write(char value)
    {
        _currentOutput.Add(new TextChunk(value));
    }

    public override void Write(char[] buffer, int index, int count)
    {
        _currentOutput.Add(new TextChunk(new ArraySegment<char>(buffer, index, count)));
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void Flush()
        => throw new NotSupportedException();

    public override Task FlushAsync()
    {
        _currentFlushAsyncTask = FlushAsyncCore(_currentFlushAsyncTask);
        return _currentFlushAsyncTask;
    }

    private async Task FlushAsyncCore(Task priorTask)
    {
        if (!priorTask.IsCompletedSuccessfully)
        {
            await priorTask;
        }

        // Swap buffers
        var outputToFlush = _currentOutput;
        _currentOutput = _previousOutput ?? new(ArrayPool<TextChunk>.Shared, PageSize);
        _previousOutput = outputToFlush;

        foreach (var entry in outputToFlush.GetContents())
        {
            switch (entry.Type)
            {
                case TextChunk.TextChunkType.String:
                    await _underlying.WriteAsync(entry.StringValue);
                    break;
                case TextChunk.TextChunkType.Char:
                    await _underlying.WriteAsync(entry.CharValue);
                    break;
                case TextChunk.TextChunkType.CharArraySegment:
                    await _underlying.WriteAsync(entry.CharArraySegmentValue);
                    break;
            }
        }

        outputToFlush.Clear();
        await _underlying.FlushAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _currentOutput.Dispose();
            _previousOutput?.Dispose();
        }

        base.Dispose(disposing);
    }
}
