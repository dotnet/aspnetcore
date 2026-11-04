// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IntegrationTesting.IIS;

public class LoggingHandler : DelegatingHandler
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
            int count;
            do
            {
                count = await readAsStreamAsync.ReadAsync(buffer.AsMemory(offset));
                offset += count;
            } while (count != 0 && offset != buffer.Length);

            readAsStreamAsync.Position = 0;
            _logger.Log(logLevel, Encoding.ASCII.GetString(buffer, 0, offset));
        }
    }
}
