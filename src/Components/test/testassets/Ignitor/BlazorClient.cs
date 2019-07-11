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

            ImplicitWait = DefaultLatencyTimeout != null;
        }

        public TimeSpan? DefaultLatencyTimeout { get; set; } = TimeSpan.FromMilliseconds(500);

        private CancellationTokenSource CancellationTokenSource { get; }

        private CancellationToken CancellationToken => CancellationTokenSource.Token;

        private TaskCompletionSource<object> TaskCompletionSource { get; }

        private CancellableOperation NextBatchReceived { get; set; }

        private CancellableOperation NextJSInteropReceived { get; set; }

        public bool ConfirmRenderBatch { get; set; } = true;

        public event Action<int, string, string> JSInterop;

        public event Action<int, int, byte[]> RenderBatchReceived;

        public event Action<Error> OnCircuitError;

        public string CircuitId { get; set; }

        public ElementHive Hive { get; set; } = new ElementHive();

        public bool ImplicitWait { get; set; }

        public HubConnection HubConnection { get; set; }

        public Task PrepareForNextBatch()
        {
            if (NextBatchReceived?.Completion != null)
            {
                throw new InvalidOperationException("Invalid state previous task not completed");
            }

            NextBatchReceived = new CancellableOperation(DefaultLatencyTimeout);

            return NextBatchReceived.Completion.Task;
        }

        public Task PrepareForNextJSInterop()
        {
            if (NextJSInteropReceived?.Completion != null)
            {
                throw new InvalidOperationException("Invalid state previous task not completed");
            }

            NextJSInteropReceived = new CancellableOperation(DefaultLatencyTimeout);

            return NextJSInteropReceived.Completion.Task;
        }

        public async Task ClickAsync(string elementId)
        {
            if (!Hive.TryFindElementById(elementId, out var elementNode))
            {
                throw new InvalidOperationException($"Could not find element with id {elementId}.");
            }

            await elementNode.ClickAsync(HubConnection);
            await WaitForRenderBatch();
        }

        public async Task SelectAsync(string elementId, string value)
        {
            if (!Hive.TryFindElementById(elementId, out var elementNode))
            {
                throw new InvalidOperationException($"Could not find element with id {elementId}.");
            }

            await elementNode.SelectAsync(HubConnection, value);
            await WaitForRenderBatch();
        }

        public async Task ExpectRenderBatch(Func<Task> action)
        {
            var task = WaitForRenderBatch();
            await action();
            await task;
        }

        public async Task ExpectJSInterop(Func<Task> action)
        {
            var task = WaitForJSInterop();
            await action();
            await task;
        }

        private async Task WaitForRenderBatch()
        {
            if (ImplicitWait)
            {
                if (DefaultLatencyTimeout == null)
                {
                    throw new InvalidOperationException("Implicit wait without DefaultLatencyTimeout is not allowed.");
                }

                await PrepareForNextBatch();
            }
        }

        private async Task WaitForJSInterop()
        {
            if (ImplicitWait)
            {
                if (DefaultLatencyTimeout == null)
                {
                    throw new InvalidOperationException("Implicit wait without DefaultLatencyTimeout is not allowed.");
                }

                await PrepareForNextJSInterop();
            }
        }

        public async Task<bool> ConnectAsync(Uri uri, bool prerendered)
        {
            var builder = new HubConnectionBuilder();
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol, IgnitorMessagePackHubProtocol>());
            builder.WithUrl(GetHubUrl(uri));
            builder.ConfigureLogging(l => l.AddConsole().SetMinimumLevel(LogLevel.Trace));

            HubConnection = builder.Build();
            await HubConnection.StartAsync(CancellationToken);

            HubConnection.On<int, string, string>("JS.BeginInvokeJS", OnBeginInvokeJS);
            HubConnection.On<int, int, byte[]>("JS.RenderBatch", OnRenderBatch);
            HubConnection.On<Error>("JS.OnError", OnError);
            HubConnection.Closed += OnClosedAsync;

            // Now everything is registered so we can start the circuit.
            if (prerendered)
            {
                CircuitId = await GetPrerenderedCircuitIdAsync(uri);
                var result = await HubConnection.InvokeAsync<bool>("ConnectCircuit", CircuitId);
                await WaitForRenderBatch();
                return result;
            }
            else
            {
                CircuitId = await HubConnection.InvokeAsync<string>("StartCircuit", new Uri(uri.GetLeftPart(UriPartial.Authority)), uri);
                await WaitForRenderBatch();
                return CircuitId != null;
            }
        }

        private void OnBeginInvokeJS(int asyncHandle, string identifier, string argsJson)
        {
            try
            {
                JSInterop?.Invoke(asyncHandle, identifier, argsJson);

                NextJSInteropReceived?.Completion?.TrySetResult(null);
            }
            catch (Exception e)
            {
                NextJSInteropReceived?.Completion?.TrySetResult(e);
            }
        }

        private void OnRenderBatch(int browserRendererId, int batchId, byte[] batchData)
        {
            try
            {
                RenderBatchReceived?.Invoke(browserRendererId, batchId, batchData);

                var batch = RenderBatchReader.Read(batchData);

                Hive.Update(batch);

                if (ConfirmRenderBatch)
                {
                    HubConnection.InvokeAsync("OnRenderCompleted", batchId, /* error */ null);
                }

                NextBatchReceived?.Completion?.TrySetResult(null);
            }
            catch (Exception e)
            {
                NextBatchReceived?.Completion?.TrySetResult(e);
            }
        }

        private void OnError(Error error)
        {
            OnCircuitError?.Invoke(error);
        }

        private Task OnClosedAsync(Exception ex)
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

        public async Task InvokeDotNetMethod(object callId, string assemblyName, string methodIdentifier, object dotNetObjectId, string argsJson)
        {
            await HubConnection.InvokeAsync("BeginInvokeDotNetFromJS", callId?.ToString(), assemblyName, methodIdentifier, dotNetObjectId ?? 0, argsJson);
            await WaitForJSInterop();
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
            if (!Hive.TryFindElementById(id, out var element))
            {
                throw new InvalidOperationException("Element not found.");
            }

            return element;
        }

        private class CancellableOperation
        {
            public CancellableOperation(TimeSpan? timeout)
            {
                Timeout = timeout;
                Initialize();
            }

            private void Initialize()
            {
                Completion = new TaskCompletionSource<object>();
                Completion.Task.ContinueWith(
                    (task, state) =>
                    {
                        var operation = (CancellableOperation)state;
                        operation.Dispose();
                    },
                    this);
                if (Timeout != null)
                {
                    Cancellation = new CancellationTokenSource();
                    CancellationRegistration = Cancellation.Token.Register(
                        (self) =>
                        {
                            var operation = (CancellableOperation)self;
                            operation.Completion.TrySetCanceled(operation.Cancellation.Token);
                            operation.Cancellation.Dispose();
                            operation.CancellationRegistration.Dispose();
                        },
                        this);
                }
            }

            private void Dispose()
            {
                Completion = null;
                Cancellation.Dispose();
                CancellationRegistration.Dispose();
            }

            public TimeSpan? Timeout { get; }

            public TaskCompletionSource<object> Completion { get; set; }

            public CancellationTokenSource Cancellation { get; set; }

            public CancellationTokenRegistration CancellationRegistration { get; set; }
        }
    }
}
