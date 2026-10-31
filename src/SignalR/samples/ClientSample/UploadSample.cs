// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.CommandLineUtils;

namespace ClientSample;

internal class UploadSample
{
    internal static void Register(CommandLineApplication app)
    {
        app.Command("uploading", cmd =>
        {
            cmd.Description = "Tests a streaming invocation from client to hub";

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

        //await BasicInvoke(connection);
        //await ScoreTrackerExample(connection);
        await StreamingEcho(connection);

        return 0;
    }

    public static async Task BasicInvoke(HubConnection connection)
    {
        var channel = Channel.CreateUnbounded<string>();
        var invokeTask = connection.InvokeAsync<string>("UploadWord", channel.Reader);

        foreach (var c in "hello")
        {
            await channel.Writer.WriteAsync(c.ToString());
            await Task.Delay(1000);
        }
        channel.Writer.TryComplete();

        var result = await invokeTask;
        Debug.WriteLine($"You message was: {result}");
    }

    public static async Task ScoreTrackerExample(HubConnection connection)
    {
        var channel_one = Channel.CreateBounded<int>(2);
        var channel_two = Channel.CreateBounded<int>(2);
        _ = WriteItemsAsync(channel_one.Writer, new[] { 2, 2, 3 });
        _ = WriteItemsAsync(channel_two.Writer, new[] { -2, 5, 3 });

        var result = await connection.InvokeAsync<string>("ScoreTracker", channel_one.Reader, channel_two.Reader);
        Debug.WriteLine(result);

        async Task WriteItemsAsync(ChannelWriter<int> source, IEnumerable<int> scores)
        {
            await Task.Delay(1000);
            foreach (var c in scores)
            {
                await source.WriteAsync(c);
                await Task.Delay(250);
            }

            // TryComplete triggers the end of this upload's relayLoop
            // which sends a StreamComplete to the server
            source.TryComplete();
        }
    }

    public static async Task StreamingEcho(HubConnection connection)
    {
        var channel = Channel.CreateUnbounded<string>();

        _ = Task.Run(async () =>
        {
            foreach (var phrase in new[] { "one fish", "two fish", "red fish", "blue fish" })
            {
                await channel.Writer.WriteAsync(phrase);
            }
        });

        var outputs = await connection.StreamAsChannelAsync<string>("StreamEcho", channel.Reader);

        while (await outputs.WaitToReadAsync())
        {
            while (outputs.TryRead(out var item))
            {
                Debug.WriteLine($"received '{item}'.");
            }
        }
    }
}

