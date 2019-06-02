// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR.Client;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Components.Server.BlazorPack;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace Ignitor
{
    internal class Program
    {
        public static async Task<int> Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("a uri is required");
                return 1;
            }

            Console.WriteLine("Press the ANY key to begin.");
            Console.ReadLine();

            var uri = new Uri(args[0]);

            var program = new Program();
            Console.CancelKeyPress += (sender, e) => { program.Cancel(); };

            await program.ExecuteAsync(uri);
            return 0;
        }

        public Program()
        {
            CancellationTokenSource = new CancellationTokenSource();
            TaskCompletionSource = new TaskCompletionSource<object>();

            CancellationTokenSource.Token.Register(() =>
            {
                TaskCompletionSource.TrySetCanceled();
            });
        }

        private CancellationTokenSource CancellationTokenSource { get; }
        private CancellationToken CancellationToken => CancellationTokenSource.Token;
        private TaskCompletionSource<object> TaskCompletionSource { get; }

        public async Task ExecuteAsync(Uri uri)
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(uri);
            var content = await response.Content.ReadAsStringAsync();

            // <!-- M.A.C.Component:{"circuitId":"CfDJ8KZCIaqnXmdF...PVd6VVzfnmc1","rendererId":"0","componentId":"0"} -->
            var match = Regex.Match(content, $"{Regex.Escape("<!-- M.A.C.Component:")}(.+?){Regex.Escape(" -->")}");
            var json = JsonDocument.Parse(match.Groups[1].Value);
            var circuitId = json.RootElement.GetProperty("circuitId").GetString();

            var builder = new HubConnectionBuilder();
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol, BlazorPackHubProtocol>());
            builder.WithUrl(new Uri(uri, "_blazor/"));
            builder.ConfigureLogging(l => l.AddConsole().SetMinimumLevel(LogLevel.Trace));

            await using (var connection = builder.Build())
            {
                await connection.StartAsync(CancellationToken);
                Console.WriteLine("Connected");

                connection.On<int, string, string>("JS.BeginInvokeJS", OnBeginInvokeJS);
                connection.On<int, int, byte[]>("JS.RenderBatch", OnRenderBatch);
                connection.On<Error>("JS.OnError", OnError);
                connection.Closed += OnClosedAsync;

                // Now everything is registered so we can start the circuit.
                var success = await connection.InvokeAsync<bool>("ConnectCircuit", circuitId);

                await TaskCompletionSource.Task;

            }

            void OnBeginInvokeJS(int asyncHandle, string identifier, string argsJson)
            {
                Console.WriteLine();
            }

            void OnRenderBatch(int browserRendererId, int batchId, byte[] batchData)
            {
                var batch = RenderBatchReader.Read(batchData);
                Console.WriteLine();
            }

            void OnError(Error error)
            {
                Console.WriteLine();
            }

            Task OnClosedAsync(Exception ex)
            {
                if (ex == null)
                {
                    TaskCompletionSource.TrySetResult(null);
                }
                else
                {
                    TaskCompletionSource.TrySetException(ex);
                }

                return Task.CompletedTask;
            }
        }

        public void Cancel()
        {
            CancellationTokenSource.Cancel();
            CancellationTokenSource.Dispose();
        }

        private class Error
        {
            public string Stack { get; set; }
        }
    }
}
