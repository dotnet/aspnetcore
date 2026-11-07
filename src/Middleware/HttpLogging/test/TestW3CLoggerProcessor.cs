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

    // Test-only gate (see ArmEntryGate) for deterministically proving enqueue-time-vs-write-time
    // semantics, e.g. that a LoggingFields change made after Log() doesn't affect an already-queued
    // entry. No effect unless a test arms it.
    private TaskCompletionSource _entryGateReached;
    private TaskCompletionSource _entryGateRelease;

    public TestW3CLoggerProcessor(IOptionsMonitor<W3CLoggerOptions> options, IHostEnvironment environment, ILoggerFactory factory) : base(options, environment, factory)
    {
        Lines = new List<string>();
    }

    public List<string> Lines { get; }

    internal override StreamWriter GetStreamWriter(string fileName)
    {
        return StreamWriter.Null;
    }

    // Header/comment lines (e.g. "#Version: 1.0") are still written as literal strings - capture them
    // directly.
    internal override Task WriteLineAsync(string message, StreamWriter streamWriter, CancellationToken cancellationToken)
    {
        OnWrite(message);
        return base.WriteLineAsync(message, streamWriter, cancellationToken);
    }

    // Arms a one-shot gate: the next call to WriteMessageAsync(W3CLogEntry, ...) will signal the
    // returned task as soon as it is entered, then block until ReleaseGatedWrite() is called, before
    // touching any of the entry's fields. Must be called before the entry is enqueued.
    public Task ArmEntryGate()
    {
        // Only the "reached" signal needs one-shot (Interlocked-exchanged) consumption below, so that
        // a later, ungated entry doesn't also block. _entryGateRelease is read directly by
        // ReleaseGatedWrite() and must stay in place after the gated write starts awaiting it.
        _entryGateRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _entryGateReached = reached;
        return reached.Task;
    }

    // Lets a write paused by ArmEntryGate() proceed.
    public void ReleaseGatedWrite() => _entryGateRelease?.TrySetResult();

    // Production never builds a formatted line for a W3CLogEntry - it writes fields straight to the
    // StreamWriter. To assert on "whole lines" without reintroducing that string in production, run
    // the real per-field write once against a throwaway in-memory buffer and read the resulting line
    // back out. The `streamWriter` parameter is always StreamWriter.Null here (see GetStreamWriter),
    // so writing to it as well would just be a second, unobservable no-op pass over the same
    // production algorithm - intentionally not done.
    internal override async Task WriteMessageAsync(W3CLogEntry message, StreamWriter streamWriter, CancellationToken cancellationToken)
    {
        var reached = Interlocked.Exchange(ref _entryGateReached, null);
        if (reached is not null)
        {
            reached.SetResult();
            await _entryGateRelease.Task;
        }

        using var captureStream = new MemoryStream();
        using (var captureWriter = new StreamWriter(captureStream, leaveOpen: true))
        {
            await base.WriteMessageAsync(message, captureWriter, cancellationToken);
        }

        captureStream.Position = 0;
        using var reader = new StreamReader(captureStream);
        var line = await reader.ReadLineAsync(cancellationToken);
        OnWrite(line ?? string.Empty);
    }

    internal override void OnWrite(string message)
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
