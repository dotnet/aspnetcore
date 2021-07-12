// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.HttpLogging
{
    internal class W3CLoggerProcessor : FileLoggerProcessor
    {
        private readonly W3CLoggingFields _loggingFields;
        internal const string W3CSeparator = "#w3c#";

        public W3CLoggerProcessor(IOptionsMonitor<W3CLoggerOptions> options, IHostEnvironment environment, ILoggerFactory factory) : base(options, environment, factory)
        {
            _loggingFields = options.CurrentValue.LoggingFields;
        }

        public override async Task OnFirstWrite(StreamWriter streamWriter, CancellationToken cancellationToken)
        {
            await WriteMessageAsync("#Version: 1.0", streamWriter, cancellationToken);

            await WriteMessageAsync("#Start-Date: " + DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), streamWriter, cancellationToken);

            await WriteMessageAsync(GetFieldsDirective(), streamWriter, cancellationToken);
        }

        private async Task WriteFieldDirectiveMessage(StreamWriter streamWriter, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await streamWriter.WriteAsync("#Fields:".AsMemory(), cancellationToken);
            if (_loggingFields.HasFlag(W3CLoggingFields.Date))
            {
                await streamWriter.WriteAsync(" date".AsMemory(), cancellationToken);
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.Time))
            {
                await streamWriter.WriteAsync(" time".AsMemory(), cancellationToken);
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.ClientIpAddress))
            {
                await streamWriter.WriteAsync(" c-ip".AsMemory(), cancellationToken);
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.UserName))
            {
                await streamWriter.WriteAsync("#Fields:".AsMemory(), cancellationToken);
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.ServerName))
            {
                await streamWriter.WriteAsync(" s-computername".AsMemory(), cancellationToken);
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.ServerIpAddress))
            {
                await streamWriter.WriteAsync(" s-ip".AsMemory(), cancellationToken);
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.ServerPort))
            {
                await streamWriter.WriteAsync(" s-port".AsMemory(), cancellationToken);
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.Method))
            {
                await streamWriter.WriteAsync(" cs-method".AsMemory(), cancellationToken);
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.UriStem))
            {
                await streamWriter.WriteAsync(" cs-uri-stem".AsMemory(), cancellationToken);
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.UriQuery))
            {
                await streamWriter.WriteAsync(" cs-uri-query".AsMemory(), cancellationToken);
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.ProtocolStatus))
            {
                await streamWriter.WriteAsync(" sc-status".AsMemory(), cancellationToken);
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.TimeTaken))
            {
                await streamWriter.WriteAsync(" time-taken".AsMemory(), cancellationToken);
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.ProtocolVersion))
            {
                await streamWriter.WriteAsync(" cs-version".AsMemory(), cancellationToken);
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.Host))
            {
                await streamWriter.WriteAsync(" cs-host".AsMemory(), cancellationToken);
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.UserAgent))
            {
                await streamWriter.WriteAsync(" cs(User-Agent)".AsMemory(), cancellationToken);
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.Cookie))
            {
                await streamWriter.WriteAsync(" cs(Cookie)".AsMemory(), cancellationToken);
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.Referer))
            {
                await streamWriter.WriteAsync(" cs(Referer)".AsMemory(), cancellationToken);
            }

            await streamWriter.FlushAsync();
        }

        internal async Task WriteField(string messageField, StreamWriter streamWriter, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await streamWriter.WriteAsync(messageField.AsMemory(), cancellationToken);
            await streamWriter.FlushAsync();
        }

        private string GetFieldsDirective()
        {
            // 152 is the length of the default fields directive
            var sb = new ValueStringBuilder(152);
            sb.Append("#Fields:");
            if (_loggingFields.HasFlag(W3CLoggingFields.Date))
            {
                sb.Append(" date");
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.Time))
            {
                sb.Append(" time");
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.ClientIpAddress))
            {
                sb.Append(" c-ip");
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.UserName))
            {
                sb.Append(" cs-username");
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.ServerName))
            {
                sb.Append(" s-computername");
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.ServerIpAddress))
            {
                sb.Append(" s-ip");
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.ServerPort))
            {
                sb.Append(" s-port");
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.Method))
            {
                sb.Append(" cs-method");
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.UriStem))
            {
                sb.Append(" cs-uri-stem");
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.UriQuery))
            {
                sb.Append(" cs-uri-query");
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.ProtocolStatus))
            {
                sb.Append(" sc-status");
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.TimeTaken))
            {
                sb.Append(" time-taken");
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.ProtocolVersion))
            {
                sb.Append(" cs-version");
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.Host))
            {
                sb.Append(" cs-host");
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.UserAgent))
            {
                sb.Append(" cs(User-Agent)");
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.Cookie))
            {
                sb.Append(" cs(Cookie)");
            }
            if (_loggingFields.HasFlag(W3CLoggingFields.Referer))
            {
                sb.Append(" cs(Referer)");
            }

            return sb.ToString();
        }

        // For testing
        internal override Task WriteMessageAsync(string message, StreamWriter streamWriter, CancellationToken cancellationToken)
        {
            OnWrite(message);

            if (message.IndexOf(W3CSeparator, 0, System.StringComparison.InvariantCulture) < 0)
            {
                return base.WriteMessageAsync(message, streamWriter, cancellationToken);
            }

            return WriteW3CMessageAsync(message, streamWriter, cancellationToken);
        }

        // Extensibility point for tests
        internal virtual void OnWrite(string message) { }

        public void EnqueueMessage(string[] messages)
        {
            if (!_messageQueue.IsAddingCompleted)
            {
                try
                {
                    _messageQueue.Add(string.Join(W3CSeparator, messages));
                    return;
                }
                catch (InvalidOperationException) { }
            }
        }

        internal async Task WriteW3CMessageAsync(string message, StreamWriter streamWriter, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var elements = message.Split(W3CSeparator, options: System.StringSplitOptions.None);
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
            var firstElement = true;
            for (var i = 0; i < elements.Length; i++)
            {
                if (_loggingFields.HasFlag((W3CLoggingFields)(1 << i)))
                {
                    if (!firstElement)
                    {
                        await streamWriter.WriteAsync(' ');
                    }
                    else
                    {
                        firstElement = false;
                    }
                    // If the element was not logged, or was the empty string, we log it as a dash
                    if (string.IsNullOrEmpty(elements[i]))
                    {
                        await streamWriter.WriteAsync('-');
                    }
                    else
                    {
                        await streamWriter.WriteAsync(elements[i].AsMemory(), cancellationToken);
                    }
                }
            }


            await streamWriter.FlushAsync();
        }
    }
}
