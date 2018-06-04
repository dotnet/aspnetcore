// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.CommandLineUtils;

namespace ClientSample
{
    internal class UploadSample
    {
        internal static void Register(CommandLineApplication app)
        {
            app.Command("uploading", cmd =>
            {
                cmd.Description = "Tests a streaming invocation from client to hub";

                CommandArgument baseUrlArgument = cmd.Argument("<BASEURL>", "The URL to the Chat Hub to test");

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
            //await FileUploadExample(connection);
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
            // Andrew please add the updated code from your laptop here

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

                // tryComplete triggers the end of this upload's relayLoop
                // which sends a StreamComplete to the server
                source.TryComplete();
            }
        }

        public static async Task FileUploadExample(HubConnection connection)
        {
            var fileNameSource = @"C:\Users\t-dygray\Pictures\weeg.jpg";
            var fileNameDest = @"C:\Users\t-dygray\Pictures\TargetFolder\weeg.jpg";

            var channel = Channel.CreateUnbounded<byte[]>();
            var invocation = connection.InvokeAsync<string>("UploadFile", fileNameDest, channel.Reader);

            using (var file = new FileStream(fileNameSource, FileMode.Open, FileAccess.Read))
            {
                foreach (var chunk in GetChunks(file, kilobytesPerChunk: 5))
                {
                    await channel.Writer.WriteAsync(chunk);
                }
            }
            channel.Writer.TryComplete();

            Debug.WriteLine(await invocation);
        }

        public static IEnumerable<byte[]> GetChunks(FileStream fileStream, double kilobytesPerChunk)
        {
            var chunkSize = (int)kilobytesPerChunk * 1024;

            var position = 0;
            while (true)
            {
                if (position + chunkSize > fileStream.Length)
                {
                    var lastChunk = new byte[fileStream.Length - position];
                    fileStream.Read(lastChunk, 0, lastChunk.Length);
                    yield return lastChunk;
                    break;
                }

                var chunk = new byte[chunkSize];
                position += fileStream.Read(chunk, 0, chunk.Length);
                yield return chunk;
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
}

