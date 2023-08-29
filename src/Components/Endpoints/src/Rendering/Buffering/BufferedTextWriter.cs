// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Components.Endpoints.Rendering;

internal class BufferedTextWriter : TextWriter
{
    private const int PageSize = 256;
    private readonly TextWriter _underlying;
    private readonly StringBuilder _charArraySegmentBuilder = new();
    private TextChunkListBuilder _currentOutput;
    private TextChunkListBuilder? _previousOutput;
    private Task _currentFlushAsyncTask = Task.CompletedTask;

    public BufferedTextWriter(TextWriter underlying)
    {
        _underlying = underlying;
        _currentOutput = new(PageSize);
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value)
        => _currentOutput.Add(new TextChunk(value));

    public override void Write(char[] buffer, int index, int count)
        => _currentOutput.Add(new TextChunk(new ArraySegment<char>(buffer, index, count), _charArraySegmentBuilder));

    public override void Write(string? value)
    {
        if (value is not null)
        {
            _currentOutput.Add(new TextChunk(value));
        }
    }

    public override void Write(int value)
        => _currentOutput.Add(new TextChunk(value));

    public override void Flush()
        => throw new NotSupportedException();

    public override Task FlushAsync()
    {
        _currentFlushAsyncTask = FlushAsyncCore(_currentFlushAsyncTask);
        return _currentFlushAsyncTask;
    }

    private async Task FlushAsyncCore(Task priorTask)
    {
        // Must always wait for prior flushes to complete first, since they are
        // using _previousOutput and nothing else is allowed to do so
        if (!priorTask.IsCompletedSuccessfully)
        {
            await priorTask;
        }

        // Swap buffers
        var outputToFlush = _currentOutput;
        _currentOutput = _previousOutput ?? new(PageSize);
        _previousOutput = outputToFlush;

        await outputToFlush.WriteToAsync(_underlying, _charArraySegmentBuilder.ToString());
        outputToFlush.Clear();
        await _underlying.FlushAsync();
    }
}
