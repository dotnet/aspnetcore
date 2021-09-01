// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Numerics;
using System.Text;
using Microsoft.Net.Http.Headers;
using System.Globalization;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.HttpLogging
{
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
            // Need to calculate TimeTaken now, if applicable
            var date = elements[W3CLoggingMiddleware._dateIndex];
            var time = elements[W3CLoggingMiddleware._timeIndex];
            if (!string.IsNullOrEmpty(date) && !string.IsNullOrEmpty(time) && _loggingFields.HasFlag(W3CLoggingFields.TimeTaken))
            {
                DateTime start = DateTime.ParseExact(date + time, "yyyy-MM-ddHH:mm:ss", CultureInfo.InvariantCulture);
                var elapsed = DateTime.UtcNow.Subtract(start);
                elements[W3CLoggingMiddleware._timeTakenIndex] = elapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
            }

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
}
