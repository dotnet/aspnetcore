// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
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

            await BasicInvoke(connection);
            //await MultiParamInvoke(connection);
            //await AdditionalArgs(connection);

            return 0;
        }

        public static async Task BasicInvoke(HubConnection connection)
        {
            var channel = Channel.CreateUnbounded<string>();
            var invokeTask = connection.InvokeAsync<string>("UploadWord", channel.Reader);

            foreach (var c in "hello")
            {
                await channel.Writer.WriteAsync(c.ToString());
            }
            channel.Writer.TryComplete();

            var result = await invokeTask;
            Debug.WriteLine($"You message was: {result}");
        }

        private static async Task WriteStreamAsync<T>(IEnumerable<T> sequence, ChannelWriter<T> writer)
        {
            foreach (T element in sequence)
            {
                await writer.WriteAsync(element);
                await Task.Delay(100);
            }

            writer.TryComplete();
        }

        public static async Task MultiParamInvoke(HubConnection connection)
        {
            var letters = Channel.CreateUnbounded<string>();
            var numbers = Channel.CreateUnbounded<int>();

            _ = WriteStreamAsync(new[] { "h", "i", "!" }, letters.Writer);
            _ = WriteStreamAsync(new[] { 1, 2, 3, 4, 5 }, numbers.Writer);

            var result = await connection.InvokeAsync<string>("DoubleStreamUpload", letters.Reader, numbers.Reader);

            Debug.WriteLine(result);
        }

        public static async Task AdditionalArgs(HubConnection connection)
        {
            var channel = Channel.CreateUnbounded<char>();
            _ = WriteStreamAsync<char>("main message".ToCharArray(), channel.Writer);

            var result = await connection.InvokeAsync<string>("UploadWithSuffix", channel.Reader, " + wooh I'm a suffix");
            Debug.WriteLine($"Your message was: {result}");
        }
    }
}

