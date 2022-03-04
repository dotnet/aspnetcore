// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IntegrationTesting.IIS
{
    public class LoggingHandler: DelegatingHandler
    {
        private readonly int _maxBodyLogSize = 16 * 1024;
        private readonly ILogger _logger;

        public LoggingHandler(HttpMessageHandler innerHandler, ILogger logger)
            : base(innerHandler)
        {
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug(request.ToString());
            var response = await base.SendAsync(request, cancellationToken);

            await LogResponse(response.IsSuccessStatusCode ? LogLevel.Debug : LogLevel.Warning, response);
            return response;
        }

        private async Task LogResponse(LogLevel logLevel, HttpResponseMessage response)
        {
            _logger.Log(logLevel, response.ToString());
            if (response.Content != null)
            {
                await response.Content.LoadIntoBufferAsync();
                var readAsStreamAsync = await response.Content.ReadAsStreamAsync();
                var buffer = new byte[_maxBodyLogSize];
                var offset = 0;
                var count = 0;
                do
                {
                    count = await readAsStreamAsync.ReadAsync(buffer, offset, buffer.Length - offset);
                    offset += count;
                } while (count != 0 && offset != buffer.Length);

                readAsStreamAsync.Position = 0;
                _logger.Log(logLevel, Encoding.ASCII.GetString(buffer, 0, offset));
            }
        }
    }
}
