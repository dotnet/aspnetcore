// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.HttpLogging;

#pragma warning disable CA1852 // Seal internal types
internal class W3CLogger : IAsyncDisposable
#pragma warning restore CA1852 // Seal internal types
{
    private readonly W3CLoggerProcessor _messageQueue;
    private readonly IOptionsMonitor<W3CLoggerOptions> _options;
    private W3CLoggingFields _loggingFields;

    public W3CLogger(IOptionsMonitor<W3CLoggerOptions> options, W3CLoggerProcessor messageQueue)
    {
        _options = options;
        _loggingFields = _options.CurrentValue.LoggingFields;
        _options.OnChange(options =>
        {
            _loggingFields = options.LoggingFields;
        });
        _messageQueue = messageQueue;
    }

    public ValueTask DisposeAsync() => _messageQueue.DisposeAsync();

    public void Log(string[] elements, string[] additionalHeaders)
    {
        // Capture the LoggingFields in effect right now, at enqueue time, so the background processor
        // writes exactly the fields that were selected when this entry was produced, even if options
        // change before the entry is dequeued.
        _messageQueue.EnqueueMessage(new W3CLogEntry(elements, additionalHeaders, _loggingFields));
    }
}
