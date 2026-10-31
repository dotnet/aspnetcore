// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
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
        _messageQueue.EnqueueMessage(Format(elements, additionalHeaders));
    }

    private string Format(string[] elements, string[] additionalHeaders)
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

        for (var i = 0; i < additionalHeaders.Length; i++)
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
            if (string.IsNullOrEmpty(additionalHeaders[i]))
            {
                sb.Append('-');
            }
            else
            {
                sb.Append(additionalHeaders[i]);
            }
        }
        return sb.ToString();
    }
}
