// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.CommandLineUtils;

namespace ClientSample;

internal class StreamingSample
{
    internal static void Register(CommandLineApplication app)
    {
        app.Command("streaming", cmd =>
        {
            cmd.Description = "Tests a streaming connection to a hub";

            var baseUrlArgument = cmd.Argument("<BASEURL>", "The URL to the Chat Hub to test");

            cmd.OnExecute(() => ExecuteAsync(baseUrlArgument.Value));
        });
    }

    public static async Task<int> ExecuteAsync(string baseUrl)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(baseUrl)
            .Build();

        await connection.StartAsync();

        var reader = await connection.StreamAsChannelAsync<int>("ChannelCounter", 10, 2000);

        while (await reader.WaitToReadAsync())
        {
            while (reader.TryRead(out var item))
            {
                Console.WriteLine($"received: {item}");
            }
        }

        return 0;
    }
}
