// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

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

            var client = new BlazorClient();
            client.JSInterop += OnJSInterop;
            Console.CancelKeyPress += (sender, e) => client.Cancel();

            var done = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Click the counter button 1000 times
            client.RenderBatchReceived += (batch) =>
            {
                if (batch.Id < 1000)
                {
                    var _ = client.ClickAsync("thecounter");
                }
                else
                {
                    done.TrySetResult(true);
                }
            };

            await client.ConnectAsync(uri);
            await done.Task;

            return 0;
        }

        private static void OnJSInterop(CapturedJSInteropCall call) =>
            Console.WriteLine("JS Invoke: " + call.Identifier + " (" + call.ArgsJson + ")");

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
            var builder = new HubConnectionBuilder();
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol, IgnitorMessagePackHubProtocol>());
            builder.WithUrl(new Uri(uri, "_blazor/"));
            builder.ConfigureLogging(l => l.AddConsole().SetMinimumLevel(LogLevel.Trace));
            var hive = new ElementHive();

            await using var connection = builder.Build();
            await connection.StartAsync(CancellationToken);
            Console.WriteLine("Connected");

            connection.On<int, string, string>("JS.BeginInvokeJS", OnBeginInvokeJS);
            connection.On<int, int, byte[]>("JS.RenderBatch", OnRenderBatch);
            connection.On<Error>("JS.OnError", OnError);
            connection.Closed += OnClosedAsync;

            // Now everything is registered so we can start the circuit.
            var success = await connection.InvokeAsync<bool>("StartCircuit", uri.AbsoluteUri, uri.GetLeftPart(UriPartial.Authority));

            await TaskCompletionSource.Task;

            void OnBeginInvokeJS(int asyncHandle, string identifier, string argsJson)
            {
                Console.WriteLine("JS Invoke: " + identifier + " (" + argsJson + ")");
            }

            void OnRenderBatch(int browserRendererId, int batchId, byte[] batchData)
            {
                var batch = RenderBatchReader.Read(batchData);
                hive.Update(batch);

                // This will click the Counter component repeatedly resulting in infinite requests.
                _ = ClickAsync("thecounter", hive, connection);
            }

            void OnError(Error error)
            {
                Console.WriteLine("ERROR: " + error.Stack);
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

        private static async Task ClickAsync(string id, ElementHive hive, HubConnection connection)
        {
            if (!hive.TryFindElementById(id, out var elementNode))
            {
                Console.WriteLine("Could not find the counter to perform a click. Exiting.");
                return;
            }

            await elementNode.ClickAsync(connection);
        }

        public void Cancel()
        {
            CancellationTokenSource.Cancel();
            CancellationTokenSource.Dispose();
        }
    }
}
