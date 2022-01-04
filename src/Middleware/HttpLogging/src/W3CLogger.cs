// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.HttpLogging;

internal class W3CLogger : IAsyncDisposable
{
    private readonly W3CLoggerProcessor _messageQueue;
    private readonly IOptionsMonitor<W3CLoggerOptions> _options;
    private W3CLoggingFields _loggingFields;

    public W3CLogger(IOptionsMonitor<W3CLoggerOptions> options, IHostEnvironment environment, ILoggerFactory factory)
    {
        _options = options;
        _loggingFields = _options.CurrentValue.LoggingFields;
        _options.OnChange(options =>
        {
            _loggingFields = options.LoggingFields;
        });
        _messageQueue = InitializeMessageQueue(_options, environment, factory);
    }

    // Virtual for testing
    internal virtual W3CLoggerProcessor InitializeMessageQueue(IOptionsMonitor<W3CLoggerOptions> options, IHostEnvironment environment, ILoggerFactory factory)
    {
        return new W3CLoggerProcessor(options, environment, factory);
    }

    public ValueTask DisposeAsync() => _messageQueue.DisposeAsync();

    public void Log(string[] elements)
    {
        _messageQueue.EnqueueMessage(Format(elements));
    }

    private string Format(string[] elements)
    {
        // 200 is around the length of an average cookie-less entry
        var sb = new ValueStringBuilder(200);
        var firstElement = true;
        for (var i = 0; i < elements.Length; i++)
        {
            if (_loggingFields.HasFlag((W3CLoggingFields)(1 << i)))
            {
                if (!firstElement)
                {
                    sb.Append(' ');
                }
                else
                {
                    firstElement = false;
                }
                // If the element was not logged, or was the empty string, we log it as a dash
                if (string.IsNullOrEmpty(elements[i]))
                {
                    sb.Append('-');
                }
                else
                {
                    sb.Append(elements[i]);
                }
            }
        }
        return sb.ToString();
    }
}
