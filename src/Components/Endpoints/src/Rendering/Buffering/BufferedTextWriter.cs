// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;

namespace Microsoft.AspNetCore.Components.Endpoints.Rendering;

internal class BufferedTextWriter : TextWriter
{
    private const int PageSize = 256;

    // Strings up to this length are copied into the output buffer so that they can be coalesced
    // with adjacent writes. Longer strings (e.g., large markup blocks) are held by reference.
    private const int MaxCopiedStringLength = 128;
    private readonly TextWriter _underlying;
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
        => _currentOutput.Add(new ReadOnlySpan<char>(in value));

    public override void Write(char[] buffer, int index, int count)
        => _currentOutput.Add(buffer.AsSpan(index, count));

    public override void Write(ReadOnlySpan<char> buffer)
        => _currentOutput.Add(buffer);

    public override void Write(string? value)
    {
        if (value is null)
        {
            return;
        }

        if (value.Length <= MaxCopiedStringLength)
        {
            _currentOutput.Add(value.AsSpan());
        }
        else
        {
            _currentOutput.Add(new TextChunk(value));
        }
    }

    public override void Write(int value)
    {
        // Non-negative ints format as ASCII digits in every culture, so they can be copied eagerly.
        // Negative ints keep the deferred path so the culture's NegativeSign is resolved at flush time, as before.
        Span<char> buffer = stackalloc char[10];
        if (value >= 0 && value.TryFormat(buffer, out var charsWritten, provider: CultureInfo.InvariantCulture))
        {
            _currentOutput.Add(buffer.Slice(0, charsWritten));
        }
        else
        {
            _currentOutput.Add(new TextChunk(value));
        }
    }

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

        await outputToFlush.WriteToAsync(_underlying);
        outputToFlush.Clear();
        await _underlying.FlushAsync();
    }
}
