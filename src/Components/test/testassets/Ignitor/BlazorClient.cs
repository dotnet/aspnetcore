// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Ignitor
{
    public class BlazorClient
    {
        public BlazorClient()
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

        private TaskCompletionSource<object> NextBatchAppliedCompletionSource { get; set; }

        public bool ConfirmRenderBatch { get; set; } = true;

        public event Action<int, string, string> JSInterop;
        public event Action<int, int, byte[]> RenderBatchReceived;

        public string CircuitId { get; set; }

        public HubConnection HubConnection { get; set; }

        public Task PrepareForNextBatch()
        {
            NextBatchAppliedCompletionSource = NextBatchAppliedCompletionSource?.Task?.IsCompleted == false ?
                throw new InvalidOperationException("Invalid state previous task not completed") :
                NextBatchAppliedCompletionSource = new TaskCompletionSource<object>();

            return NextBatchAppliedCompletionSource.Task;
        }

        public Task ClickAsync(string elementId)
        {
            if (!Hive.TryFindElementById(elementId, out var elementNode))
            {
                throw new InvalidOperationException($"Could not find element with id {elementId}.");
            }

            return elementNode.ClickAsync(HubConnection);
        }

        public Task SelectAsync(string elementId, string value)
        {
            if (!Hive.TryFindElementById(elementId, out var elementNode))
            {
                throw new InvalidOperationException($"Could not find element with id {elementId}.");
            }

            return elementNode.SelectAsync(HubConnection, value);
        }

        public ElementHive Hive { get; set; } = new ElementHive();

        public async Task<bool> ConnectAsync(Uri uri, bool prerendered)
        {
            var builder = new HubConnectionBuilder();
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol, IgnitorMessagePackHubProtocol>());
            builder.WithUrl(GetHubUrl(uri));
            builder.ConfigureLogging(l => l.AddConsole().SetMinimumLevel(LogLevel.Trace));

            HubConnection = builder.Build();
            await HubConnection.StartAsync(CancellationToken);
            Console.WriteLine("Connected");

            HubConnection.On<int, string, string>("JS.BeginInvokeJS", OnBeginInvokeJS);
            HubConnection.On<int, int, byte[]>("JS.RenderBatch", OnRenderBatch);
            HubConnection.On<Error>("JS.OnError", OnError);
            HubConnection.Closed += OnClosedAsync;

            // Now everything is registered so we can start the circuit.
            if (prerendered)
            {
                CircuitId = await GetPrerenderedCircuitIdAsync(uri);
                return await HubConnection.InvokeAsync<bool>("ConnectCircuit", CircuitId);
            }
            else
            {
                CircuitId = await HubConnection.InvokeAsync<string>("StartCircuit", new Uri(uri.GetLeftPart(UriPartial.Authority)), uri);
                return CircuitId != null;
            }

            void OnBeginInvokeJS(int asyncHandle, string identifier, string argsJson)
            {
                JSInterop?.Invoke(asyncHandle, identifier, argsJson);
            }

            void OnRenderBatch(int browserRendererId, int batchId, byte[] batchData)
            {
                try
                {
                    var batch = RenderBatchReader.Read(batchData);
                    RenderBatchReceived?.Invoke(browserRendererId, batchId, batchData);

                    Hive.Update(batch);

                    if (ConfirmRenderBatch)
                    {
                        HubConnection.InvokeAsync("OnRenderCompleted", batchId, /* error */ null);
                    }

                    NextBatchAppliedCompletionSource?.TrySetResult(null);
                }
                catch (Exception e)
                {
                    NextBatchAppliedCompletionSource?.TrySetResult(e);
                }
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

        private Uri GetHubUrl(Uri uri)
        {
            if (uri.Segments.Length == 1)
            {
                return new Uri(uri, "_blazor");
            }
            else
            {
                var builder = new UriBuilder(uri);
                builder.Path += builder.Path.EndsWith("/") ? "_blazor" : "/_blazor";
                return builder.Uri;
            }
        }

        public void InvokeDotNetMethod(object callId, string assemblyName, string methodIdentifier, object dotNetObjectId, string argsJson)
        {
            HubConnection.InvokeAsync("BeginInvokeDotNetFromJS", callId?.ToString(), assemblyName, methodIdentifier, dotNetObjectId ?? 0, argsJson);
        }

        private static async Task<string> GetPrerenderedCircuitIdAsync(Uri uri)
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(uri);
            var content = await response.Content.ReadAsStringAsync();

            // <!-- M.A.C.Component:{"circuitId":"CfDJ8KZCIaqnXmdF...PVd6VVzfnmc1","rendererId":"0","componentId":"0"} -->
            var match = Regex.Match(content, $"{Regex.Escape("<!-- M.A.C.Component:")}(.+?){Regex.Escape(" -->")}");
            var json = JsonDocument.Parse(match.Groups[1].Value);
            var circuitId = json.RootElement.GetProperty("circuitId").GetString();
            return circuitId;
        }

        public void Cancel()
        {
            CancellationTokenSource.Cancel();
            CancellationTokenSource.Dispose();
        }

        public ElementNode FindElementById(string id)
        {
            if(!Hive.TryFindElementById(id, out var element))
            {
                throw new InvalidOperationException("Element not found.");
            }

            return element;
        }
    }
}
