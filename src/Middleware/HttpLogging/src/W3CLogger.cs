// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

        public W3CLogger(IOptionsMonitor<W3CLoggerOptions> options, IHostEnvironment environment, ILoggerFactory factory)
        {
            _options = options;
            _messageQueue = InitializeMessageQueue(_options, environment, factory);
        }

        // Virtual for testing
        internal virtual W3CLoggerProcessor InitializeMessageQueue(IOptionsMonitor<W3CLoggerOptions> options, IHostEnvironment environment, ILoggerFactory factory)
        {
            return new W3CLoggerProcessor(options, environment, factory);
        }

        public async ValueTask DisposeAsync() => await _messageQueue.DisposeAsync();

        public void Log(string[] elements)
        {
            _messageQueue.EnqueueMessage(Format(elements));
        }

        private string Format(string[] elements)
        {
            // Need to calculate TimeTaken now, if applicable
            var date = elements[W3CLoggingMiddleware._fieldIndices[W3CLoggingFields.Date]];
            var time = elements[W3CLoggingMiddleware._fieldIndices[W3CLoggingFields.Time]];
            if (!string.IsNullOrEmpty(date) && !string.IsNullOrEmpty(time) && _options.CurrentValue.LoggingFields.HasFlag(W3CLoggingFields.TimeTaken))
            {
                DateTime start = DateTime.ParseExact(date + time, "yyyy-MM-ddHH:mm:ss", CultureInfo.InvariantCulture);
                var elapsed = DateTime.Now.Subtract(start);
                elements[W3CLoggingMiddleware._fieldIndices[W3CLoggingFields.TimeTaken]] = elapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
            }

            // 200 is around the length of an average cookie-less entry
            var sb = new ValueStringBuilder(200);
            var firstElement = true;
            for (var i = 0; i < elements.Length; i++)
            {
                if (_options.CurrentValue.LoggingFields.HasFlag((W3CLoggingFields)(1 << i)))
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
