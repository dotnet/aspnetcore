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
            _messageQueue.EnqueueMessage(elements);
        }
    }
}
