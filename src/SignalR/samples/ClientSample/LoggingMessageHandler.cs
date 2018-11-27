// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ClientSample
{
    internal class LoggingMessageHandler : DelegatingHandler
    {
        private readonly ILogger<LoggingMessageHandler> _logger;

        public LoggingMessageHandler(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<LoggingMessageHandler>();
        }

        public LoggingMessageHandler(ILoggerFactory loggerFactory, HttpMessageHandler innerHandler) : base(innerHandler)
        {
            _logger = loggerFactory.CreateLogger<LoggingMessageHandler>();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Send: {0} {1}", request.Method, request.RequestUri);
            var result = await base.SendAsync(request, cancellationToken);
            _logger.LogDebug("Recv: {0} {1}", (int)result.StatusCode, request.RequestUri);
            return result;
        }
    }
}