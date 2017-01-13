// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.Extensions.Logging;

namespace ClientSample
{
    internal class RawSample
    {
        public static async Task MainAsync(string[] args)
        {
            var baseUrl = "http://localhost:5000/chat";
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
                using (var connection = await Connection.ConnectAsync(new Uri(baseUrl), transport, httpClient, loggerFactory))
                {
                    logger.LogInformation("Connected to {0}", baseUrl);

                    var cts = new CancellationTokenSource();
                    Console.CancelKeyPress += (sender, a) =>
                    {
                        a.Cancel = true;
                        logger.LogInformation("Stopping loops...");
                        cts.Cancel();
                    };

                    // Ready to start the loops
                    var receive =
                        StartReceiving(loggerFactory.CreateLogger("ReceiveLoop"), connection, cts.Token).ContinueWith(_ => cts.Cancel());
                    var send =
                        StartSending(loggerFactory.CreateLogger("SendLoop"), connection, cts.Token).ContinueWith(_ => cts.Cancel());

                    await Task.WhenAll(receive, send);
                }
            }
        }

        private static async Task StartSending(ILogger logger, Connection connection, CancellationToken cancellationToken)
        {
            logger.LogInformation("Send loop starting");
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = Console.ReadLine();
                logger.LogInformation("Sending: {0}", line);

                await connection.Output.WriteAsync(new Message(
                    ReadableBuffer.Create(Encoding.UTF8.GetBytes("Hello World")).Preserve(),
                    Format.Text));
            }
            logger.LogInformation("Send loop terminated");
        }

        private static async Task StartReceiving(ILogger logger, Connection connection, CancellationToken cancellationToken)
        {
            logger.LogInformation("Receive loop starting");
            try
            {
                while (await connection.Input.WaitToReadAsync(cancellationToken))
                {
                    Message message;
                    if (!connection.Input.TryRead(out message))
                    {
                        continue;
                    }

                    using (message)
                    {
                        logger.LogInformation("Received: {0}", Encoding.UTF8.GetString(message.Payload.Buffer.ToArray()));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Connection is closing");
            }
            catch (Exception ex)
            {
                logger.LogError(0, ex, "Connection terminated due to an exception");
            }

            logger.LogInformation("Receive loop terminated");
        }
    }
}
