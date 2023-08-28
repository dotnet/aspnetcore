// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;

namespace Microsoft.AspNetCore.Components.Endpoints.Rendering;

internal readonly struct TextChunk
{
    private readonly TextChunkType _type;
    private readonly string? _stringValue;
    private readonly char _charValue;
    private readonly ArraySegment<char> _charArraySegmentValue;

    public TextChunk(string value)
    {
        _type = TextChunkType.String;
        _stringValue = value;
    }

    public TextChunk(char value)
    {
        _type = TextChunkType.Char;
        _charValue = value;
    }

    public TextChunk(ArraySegment<char> value)
    {
        _type = TextChunkType.CharArraySegment;
        _charArraySegmentValue = value;
    }

    private enum TextChunkType { String, Char, CharArraySegment };

    public Task WriteToAsync(TextWriter writer)
    {
        switch (_type)
        {
            case TextChunkType.String:
                return writer.WriteAsync(_stringValue);
            case TextChunkType.Char:
                return writer.WriteAsync(_charValue);
            case TextChunkType.CharArraySegment:
                return writer.WriteAsync(_charArraySegmentValue);
            default:
                throw new InvalidOperationException($"Unknown type {_type}");
        }
    }
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

internal class TextChunkListBuilder(ArrayPool<TextChunk> pool, int pageLength) : IDisposable
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

    public async Task WriteToAsync(TextWriter writer)
    {
        if (_priorPages is not null)
        {
            foreach (var page in _priorPages)
            {
                var (count, buffer) = (page.Count, page.Buffer);
                for (var i = 0; i < count; i++)
                {
                    await buffer[i].WriteToAsync(writer);
                }
            }
        }

        if (_currentPage is not null)
        {
            var (count, buffer) = (_currentPage.Count, _currentPage.Buffer);
            for (var i = 0; i < count; i++)
            {
                await buffer[i].WriteToAsync(writer);
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
    private TextChunkListBuilder _currentOutput;
    private TextChunkListBuilder? _previousOutput;
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

        await outputToFlush.WriteToAsync(_underlying);
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
