// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.HttpLogging;

internal sealed class TestW3CLoggerProcessor : W3CLoggerProcessor
{
    private int _writeCount = 0;
    private int _expectedWrites;
    private TaskCompletionSource _tcs;
    private bool _hasWritten;
    private readonly object _writeCountLock = new object();
    private string _messageLine = string.Empty;

    public TestW3CLoggerProcessor(IOptionsMonitor<W3CLoggerOptions> options, IHostEnvironment environment, ILoggerFactory factory) : base(options, environment, factory)
    {
        Lines = new List<string>();
    }

    public List<string> Lines { get; }

    internal override StreamWriter GetStreamWriter(string fileName)
    {
        return StreamWriter.Null;
    }

    internal override void OnWriteLine(string message)
    {
        Lines.Add(message);
        lock (_writeCountLock)
        {
            _writeCount++;
            if (_tcs != null && _writeCount >= _expectedWrites)
            {
                _tcs.SetResult();
            }
        }
    }

    internal override void OnWrite(string message, bool endOfLine = false)
    {
        _messageLine = $"{_messageLine}{message}";
        if (endOfLine == true)
        {
            OnWriteLine(_messageLine);
            _messageLine = string.Empty;
        }
    }

    internal override void OnFlush()
    {
        // Send end of line message
        OnWrite(string.Empty, true);
    }

    public Task WaitForWrites(int numWrites)
    {
        lock (_writeCountLock)
        {
            if (_writeCount >= numWrites)
            {
                return Task.CompletedTask;
            }
            _expectedWrites = numWrites;
            _tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }
        return _tcs.Task;
    }

    public override async Task OnFirstWrite(StreamWriter streamWriter, CancellationToken cancellationToken)
    {
        if (!_hasWritten)
        {
            await base.OnFirstWrite(streamWriter, cancellationToken);
            _hasWritten = true;
        }
    }
}
