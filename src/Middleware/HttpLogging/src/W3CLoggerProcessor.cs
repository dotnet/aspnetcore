// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.HttpLogging
{
    internal class W3CLoggerProcessor : FileLoggerProcessor
    {
        private readonly W3CLoggingFields _loggingFields;

        public W3CLoggerProcessor(IOptionsMonitor<W3CLoggerOptions> options, IHostEnvironment environment, ILoggerFactory factory) : base(options, environment, factory)
        {
            _loggingFields = options.CurrentValue.LoggingFields;
        }

        public override async Task OnFirstWrite(StreamWriter streamWriter)
        {
            await WriteMessageAsync("#Version: 1.0", streamWriter);

            await WriteMessageAsync("#Start-Date: " + DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), streamWriter);

            await WriteMessageAsync(GetFieldsDirective(), streamWriter);
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
        internal override async Task WriteMessageAsync(string message, StreamWriter streamWriter)
        {
            OnWrite(message);
            await base.WriteMessageAsync(message, streamWriter);
        }

        // Extensibility point for tests
        internal virtual void OnWrite(string message) { }
    }
}
