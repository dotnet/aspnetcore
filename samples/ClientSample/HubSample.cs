// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace ClientSample
{
    internal class HubSample
    {
        internal static void Register(CommandLineApplication app)
        {
            app.Command("hub", cmd =>
            {
                cmd.Description = "Tests a connection to a hub";

                var baseUrlArgument = cmd.Argument("<BASEURL>", "The URL to the Chat Hub to test");

                cmd.OnExecute(() => ExecuteAsync(baseUrlArgument.Value));
            });
        }

        public static async Task<int> ExecuteAsync(string baseUrl)
        {
            baseUrl = string.IsNullOrEmpty(baseUrl) ? "http://localhost:5000/default" : baseUrl;

            Console.WriteLine("Connecting to {0}", baseUrl);
            HubConnection connection = await ConnectAsync(baseUrl);
            Console.WriteLine("Connected to {0}", baseUrl);

            try
            {
                var sendCts = new CancellationTokenSource();

                Console.CancelKeyPress += async (sender, a) =>
                {
                    a.Cancel = true;
                    Console.WriteLine("Stopping loops...");
                    sendCts.Cancel();
                    await connection.DisposeAsync();
                };

                // Set up handler
                connection.On<string>("Send", Console.WriteLine);

                while (!connection.Closed.IsCompleted)
                {
                    var completedTask = await Task.WhenAny(Task.Run(() => Console.ReadLine()), connection.Closed);
                    if (completedTask == connection.Closed)
                    {
                        break;
                    }

                    var line = await (Task<string>)completedTask;

                    if (line == null)
                    {
                        break;
                    }

                    await connection.InvokeAsync<object>("Send", line, sendCts.Token);
                }
            }
            catch (AggregateException aex) when (aex.InnerExceptions.All(e => e is OperationCanceledException))
            {
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                await connection.DisposeAsync();
            }
            return 0;
        }

        private static async Task<HubConnection> ConnectAsync(string baseUrl)
        {
            // Keep trying to until we can start
            while (true)
            {
                var connection = new HubConnectionBuilder()
                                .WithUrl(baseUrl)
                                .WithConsoleLogger(LogLevel.Trace)
                                .Build();
                try
                {
                    await connection.StartAsync();
                    return connection;
                }
                catch (Exception)
                {
                    await Task.Delay(1000);
                }
            }
        }
    }
}
