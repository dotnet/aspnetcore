// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClientSample;

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
        var uri = baseUrl == null ? new Uri("net.tcp://127.0.0.1:9001") : new Uri(baseUrl);

        Console.WriteLine("Connecting to {0}", uri);

        var connectionBuilder = new HubConnectionBuilder()
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
            });

        connectionBuilder.Services.Configure<LoggerFilterOptions>(options =>
        {
            options.MinLevel = LogLevel.Trace;
        });

        if (uri.Scheme == "net.tcp")
        {
            connectionBuilder.WithEndPoint(uri);
        }
        else
        {
            connectionBuilder.WithUrl(uri);
        }

        connectionBuilder.WithAutomaticReconnect();

        using var closedTokenSource = new CancellationTokenSource();
        var connection = connectionBuilder.Build();

        try
        {
            Console.CancelKeyPress += (sender, a) =>
            {
                a.Cancel = true;
                closedTokenSource.Cancel();
                connection.StopAsync().GetAwaiter().GetResult();
            };

            // Set up handler
            connection.On<string>("Send", Console.WriteLine);

            connection.Closed += e =>
            {
                Console.WriteLine("Connection closed...");
                closedTokenSource.Cancel();
                return Task.CompletedTask;
            };

            if (!await ConnectAsync(connection, closedTokenSource.Token))
            {
                Console.WriteLine("Failed to establish a connection to '{0}' because the CancelKeyPress event fired first. Exiting...", uri);
                return 0;
            }

            Console.WriteLine("Connected to {0}", uri);

            // Handle the connected connection
            while (true)
            {
                // If the underlying connection closes while waiting for user input, the user will not observe
                // the connection close aside from "Connection closed..." being printed to the console. That's
                // because cancelling Console.ReadLine() is a royal pain.
                var line = Console.ReadLine();

                if (line == null || closedTokenSource.Token.IsCancellationRequested)
                {
                    Console.WriteLine("Exiting...");
                    break;
                }

                try
                {
                    await connection.InvokeAsync<object>("Send", "C#", line);
                }
                catch when (closedTokenSource.IsCancellationRequested)
                {
                    // We're shutting down the client
                    Console.WriteLine("Failed to send '{0}' because the CancelKeyPress event fired first. Exiting...", line);
                    break;
                }
                catch (Exception ex)
                {
                    // Send could have failed because the connection closed
                    // Continue to loop because we should be reconnecting.
                    Console.WriteLine(ex);
                }
            }
        }
        finally
        {
            await connection.StopAsync();
        }

        return 0;
    }

    private static async Task<bool> ConnectAsync(HubConnection connection, CancellationToken token)
    {
        // Keep trying to until we can start
        while (true)
        {
            try
            {
                await connection.StartAsync(token);
                return true;
            }
            catch when (token.IsCancellationRequested)
            {
                return false;
            }
            catch
            {
                Console.WriteLine("Failed to connect, trying again in 5000(ms)");

                await Task.Delay(5000);
            }
        }
    }
}
