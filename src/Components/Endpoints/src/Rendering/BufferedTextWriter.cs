// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;

namespace Microsoft.AspNetCore.Components.Endpoints.Rendering;

internal class BufferedTextWriter : TextWriter
{
    private readonly TextWriter _underlying;
    private MemoryStream _currentOutput;
    private Task _currentFlushAsyncTask = Task.CompletedTask;

    public BufferedTextWriter(TextWriter underlying, Encoding encoding)
    {
        _underlying = underlying;
        _currentOutput = new();
        Encoding = encoding;
    }

    public override Encoding Encoding { get; }

    public override void Write(char value)
    {
        _currentOutput.Write(Encoding.GetBytes(new[] { value }));
    }

    public override void Write(char[] buffer, int index, int count)
    {
        _currentOutput.Write(Encoding.GetBytes(buffer, index, count));
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
        await priorTask;

        var outputToFlush = _currentOutput;
        _currentOutput = new MemoryStream();

        await outputToFlush.FlushAsync();
        var charsToEmit = Encoding.GetChars(outputToFlush.ToArray());
        await _underlying.WriteAsync(charsToEmit);
        await _underlying.FlushAsync();
    }
}
