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
            baseUrl = string.IsNullOrEmpty(baseUrl) ? "http://localhost:5000/hubs" : baseUrl;

            var loggerFactory = new LoggerFactory();

            Console.WriteLine("Connecting to {0}", baseUrl);
            var connection = new HubConnection(new Uri(baseUrl), loggerFactory);
            try
            {
                await connection.StartAsync();
                Console.WriteLine("Connected to {0}", baseUrl);

                var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (sender, a) =>
                {
                    a.Cancel = true;
                    Console.WriteLine("Stopping loops...");
                    cts.Cancel();
                };

                // Set up handler
                connection.On("Send", new[] { typeof(string) }, a =>
                {
                    var message = (string)a[0];
                    Console.WriteLine(message);
                });

                while (!cts.Token.IsCancellationRequested)
                {
                    var line = await Task.Run(() => Console.ReadLine(), cts.Token);

                    if (line == null)
                    {
                        break;
                    }

                    await connection.Invoke<object>("Send", cts.Token, line);
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
    }
}
