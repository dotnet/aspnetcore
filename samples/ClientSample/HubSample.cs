// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.Extensions.Logging;

namespace ClientSample
{
    internal class HubSample
    {
        public static async Task MainAsync(string[] args)
        {
            var baseUrl = "http://localhost:5000/hubs";
            if (args.Length > 0)
            {
                baseUrl = args[0];
            }

            var loggerFactory = new LoggerFactory();
            loggerFactory.AddConsole(LogLevel.Debug);
            var logger = loggerFactory.CreateLogger<Program>();

            using (var httpClient = new HttpClient(new LoggingMessageHandler(loggerFactory, new HttpClientHandler())))
            {
                logger.LogInformation("Connecting to {0}", baseUrl);
                var transport = new LongPollingTransport(httpClient, loggerFactory);
                using (var connection = await HubConnection.ConnectAsync(new Uri(baseUrl),
                    new JsonNetInvocationAdapter(), transport, httpClient, loggerFactory))
                {
                    logger.LogInformation("Connected to {0}", baseUrl);

                    var cts = new CancellationTokenSource();
                    Console.CancelKeyPress += (sender, a) =>
                    {
                        a.Cancel = true;
                        logger.LogInformation("Stopping loops...");
                        cts.Cancel();
                    };

                    // Set up handler
                    connection.On("Send", new[] { typeof(string) }, a =>
                    {
                        var message = (string)a[0];
                        Console.WriteLine("RECEIVED: " + message);
                    });

                    while (!cts.Token.IsCancellationRequested)
                    {
                        var line = Console.ReadLine();
                        logger.LogInformation("Sending: {0}", line);

                        await connection.Invoke<object>("Send", line);
                    }
                }
            }
        }
    }
}
