// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.Http.Connections.Client.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Client.Tests;

public partial class HttpConnectionTests
{
    private static HttpConnection CreateConnection(
        HttpMessageHandler httpHandler = null,
        ILoggerFactory loggerFactory = null,
        string url = null,
        ITransport transport = null,
        ITransportFactory transportFactory = null,
        HttpTransportType? transportType = null,
        TransferFormat transferFormat = TransferFormat.Text,
        Func<Task<string>> accessTokenProvider = null)
    {
        var httpOptions = new HttpConnectionOptions
        {
            Transports = transportType ?? HttpTransportType.LongPolling,
            HttpMessageHandlerFactory = (httpMessageHandler) => httpHandler ?? TestHttpMessageHandler.CreateDefault(),
            AccessTokenProvider = accessTokenProvider,
        };
        if (url != null)
        {
            httpOptions.Url = new Uri(url);
        }

        return CreateConnection(httpOptions, loggerFactory, transport, transportFactory, transferFormat);
    }

    private static HttpConnection CreateConnection(
        HttpConnectionOptions httpConnectionOptions,
        ILoggerFactory loggerFactory = null,
        ITransport transport = null,
        ITransportFactory transportFactory = null,
        TransferFormat transferFormat = TransferFormat.Text)
    {
        loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        httpConnectionOptions.Url ??= new Uri("http://fakeuri.org/");
        httpConnectionOptions.DefaultTransferFormat = transferFormat;

        if (transportFactory == null && transport != null)
        {
            transportFactory = new TestTransportFactory(transport);
        }

        if (transportFactory != null)
        {
            return new HttpConnection(httpConnectionOptions, loggerFactory, transportFactory);
        }
        else
        {
            // Use the public constructor to get the default transport factory.
            return new HttpConnection(httpConnectionOptions, loggerFactory);
        }
    }

    private static async Task WithConnectionAsync(HttpConnection connection, Func<HttpConnection, Task> body)
    {
        try
        {
            // Using OrTimeout here will hide any timeout issues in the test :(.
            await body(connection);
        }
        finally
        {
            await connection.DisposeAsync().DefaultTimeout();
        }
    }
}

