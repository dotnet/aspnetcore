// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Net.Http;
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

        public TimeSpan? DefaultLatencyTimeout { get; set; } = TimeSpan.FromMilliseconds(500);
        public TimeSpan? DefaultConnectTimeout { get; set; } = TimeSpan.FromSeconds(10);

        public Func<string, Exception> FormatError { get; set; }

        private CancellationTokenSource CancellationTokenSource { get; }

        private CancellationToken CancellationToken => CancellationTokenSource.Token;

        private TaskCompletionSource<object> TaskCompletionSource { get; }

        private CancellableOperation NextBatchReceived { get; set; }

        private CancellableOperation NextErrorReceived { get; set; }

        private CancellableOperation NextDisconnect { get; set; }

        private CancellableOperation NextJSInteropReceived { get; set; }

        private CancellableOperation NextDotNetInteropCompletionReceived { get; set; }

        public ILoggerProvider LoggerProvider { get; set; }

        public bool ConfirmRenderBatch { get; set; } = true;

        public event Action<int, string, string> JSInterop;

        public event Action<int, byte[]> RenderBatchReceived;

        public event Action<string> DotNetInteropCompletion;

        public event Action<string> OnCircuitError;

        public string CircuitId { get; set; }

        public ElementHive Hive { get; set; } = new ElementHive();

        public bool ImplicitWait => DefaultLatencyTimeout != null;

        public HubConnection HubConnection { get; set; }

        public Task PrepareForNextBatch(TimeSpan? timeout)
        {
            if (NextBatchReceived?.Completion != null)
            {
                throw new InvalidOperationException("Invalid state previous task not completed");
            }

            NextBatchReceived = new CancellableOperation(timeout);

            return NextBatchReceived.Completion.Task;
        }

        public Task PrepareForNextJSInterop(TimeSpan? timeout)
        {
            if (NextJSInteropReceived?.Completion != null)
            {
                throw new InvalidOperationException("Invalid state previous task not completed");
            }

            NextJSInteropReceived = new CancellableOperation(timeout);

            return NextJSInteropReceived.Completion.Task;
        }

        public Task PrepareForNextDotNetInterop(TimeSpan? timeout)
        {
            if (NextDotNetInteropCompletionReceived?.Completion != null)
            {
                throw new InvalidOperationException("Invalid state previous task not completed");
            }

            NextDotNetInteropCompletionReceived = new CancellableOperation(timeout);

            return NextDotNetInteropCompletionReceived.Completion.Task;
        }

        public Task PrepareForNextCircuitError(TimeSpan? timeout)
        {
            if (NextErrorReceived?.Completion != null)
            {
                throw new InvalidOperationException("Invalid state previous task not completed");
            }

            NextErrorReceived = new CancellableOperation(timeout);

            return NextErrorReceived.Completion.Task;
        }

        public Task PrepareForNextDisconnect(TimeSpan? timeout)
        {
            if (NextDisconnect?.Completion != null)
            {
                throw new InvalidOperationException("Invalid state previous task not completed");
            }

            NextDisconnect = new CancellableOperation(timeout);

            return NextDisconnect.Completion.Task;
        }

        public Task ClickAsync(string elementId, bool expectRenderBatch = true)
        {
            if (!Hive.TryFindElementById(elementId, out var elementNode))
            {
                throw new InvalidOperationException($"Could not find element with id {elementId}.");
            }
            if (expectRenderBatch)
            {
                return ExpectRenderBatch(() => elementNode.ClickAsync(HubConnection));
            }
            else
            {
                return elementNode.ClickAsync(HubConnection);
            }
        }

        public Task SelectAsync(string elementId, string value)
        {
            if (!Hive.TryFindElementById(elementId, out var elementNode))
            {
                throw new InvalidOperationException($"Could not find element with id {elementId}.");
            }

            return ExpectRenderBatch(() => elementNode.SelectAsync(HubConnection, value));
        }

        public async Task ExpectRenderBatch(Func<Task> action, TimeSpan? timeout = null)
        {
            var task = WaitForRenderBatch(timeout);
            await action();
            await task;
        }

        public async Task ExpectJSInterop(Func<Task> action, TimeSpan? timeout = null)
        {
            var task = WaitForJSInterop(timeout);
            await action();
            await task;
        }

        public async Task ExpectDotNetInterop(Func<Task> action, TimeSpan? timeout = null)
        {
            var task = WaitForDotNetInterop(timeout);
            await action();
            await task;
        }

        public async Task ExpectCircuitError(Func<Task> action, TimeSpan? timeout = null)
        {
            var task = WaitForCircuitError(timeout);
            await action();
            await task;
        }

        public async Task ExpectCircuitErrorAndDisconnect(Func<Task> action, TimeSpan? timeout = null)
        {
            // NOTE: timeout is used for each operation individually.
            await ExpectDisconnect(async () =>
            {
                await ExpectCircuitError(action, timeout);
            }, timeout);
        }

        public async Task ExpectDisconnect(Func<Task> action, TimeSpan? timeout = null)
        {
            var task = WaitForDisconnect(timeout);
            await action();
            await task;
        }

        private Task WaitForRenderBatch(TimeSpan? timeout = null)
        {
            if (ImplicitWait)
            {
                if (DefaultLatencyTimeout == null && timeout == null)
                {
                    throw new InvalidOperationException("Implicit wait without DefaultLatencyTimeout is not allowed.");
                }

                return PrepareForNextBatch(timeout ?? DefaultLatencyTimeout);
            }

            return Task.CompletedTask;
        }

        private async Task WaitForJSInterop(TimeSpan? timeout = null)
        {
            if (ImplicitWait)
            {
                if (DefaultLatencyTimeout == null && timeout == null)
                {
                    throw new InvalidOperationException("Implicit wait without DefaultLatencyTimeout is not allowed.");
                }

                await PrepareForNextJSInterop(timeout ?? DefaultLatencyTimeout);
            }
        }

        private async Task WaitForDotNetInterop(TimeSpan? timeout = null)
        {
            if (ImplicitWait)
            {
                if (DefaultLatencyTimeout == null && timeout == null)
                {
                    throw new InvalidOperationException("Implicit wait without DefaultLatencyTimeout is not allowed.");
                }

                await PrepareForNextDotNetInterop(timeout ?? DefaultLatencyTimeout);
            }
        }

        private async Task WaitForCircuitError(TimeSpan? timeout = null)
        {
            if (ImplicitWait)
            {
                if (DefaultLatencyTimeout == null && timeout == null)
                {
                    throw new InvalidOperationException("Implicit wait without DefaultLatencyTimeout is not allowed.");
                }

                await PrepareForNextCircuitError(timeout ?? DefaultLatencyTimeout);
            }
        }

        private async Task WaitForDisconnect(TimeSpan? timeout = null)
        {
            if (ImplicitWait)
            {
                if (DefaultLatencyTimeout == null && timeout == null)
                {
                    throw new InvalidOperationException("Implicit wait without DefaultLatencyTimeout is not allowed.");
                }
            }

            await PrepareForNextDisconnect(timeout ?? DefaultLatencyTimeout);
        }

        public async Task<bool> ConnectAsync(Uri uri, bool connectAutomatically = true)
        {
            var builder = new HubConnectionBuilder();
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol, IgnitorMessagePackHubProtocol>());
            builder.WithUrl(GetHubUrl(uri));
            builder.ConfigureLogging(l =>
            {
                l.SetMinimumLevel(LogLevel.Trace);
                if (LoggerProvider != null)
                {
                    l.AddProvider(LoggerProvider);
                }
            });

            HubConnection = builder.Build();
            await HubConnection.StartAsync(CancellationToken);

            HubConnection.On<int, string, string>("JS.BeginInvokeJS", OnBeginInvokeJS);
            HubConnection.On<string>("JS.EndInvokeDotNet", OnEndInvokeDotNet);
            HubConnection.On<int, byte[]>("JS.RenderBatch", OnRenderBatch);
            HubConnection.On<string>("JS.Error", OnError);
            HubConnection.Closed += OnClosedAsync;

            if (!connectAutomatically)
            {
                return true;
            }

            var descriptors = await GetPrerenderDescriptors(uri);
            await ExpectRenderBatch(
                async () => CircuitId = await HubConnection.InvokeAsync<string>("StartCircuit", uri, uri, descriptors),
                DefaultConnectTimeout);
            return CircuitId != null;
        }

        private void OnEndInvokeDotNet(string completion)
        {
            DotNetInteropCompletion?.Invoke(completion);

            NextDotNetInteropCompletionReceived?.Completion?.TrySetResult(null);
        }

        private void OnBeginInvokeJS(int asyncHandle, string identifier, string argsJson)
        {
            JSInterop?.Invoke(asyncHandle, identifier, argsJson);

            NextJSInteropReceived?.Completion?.TrySetResult(null);
        }

        private void OnRenderBatch(int batchId, byte[] batchData)
        {
            RenderBatchReceived?.Invoke(batchId, batchData);

            var batch = RenderBatchReader.Read(batchData);

            Hive.Update(batch);

            if (ConfirmRenderBatch)
            {
                _ = ConfirmBatch(batchId);
            }

            NextBatchReceived?.Completion?.TrySetResult(null);
        }

        public Task ConfirmBatch(int batchId, string error = null)
        {
            return HubConnection.InvokeAsync("OnRenderCompleted", batchId, error);
        }

        private void OnError(string error)
        {
            OnCircuitError?.Invoke(error);

            var exception = FormatError?.Invoke(error) ?? new Exception(error);
            NextBatchReceived?.Completion?.TrySetException(exception);
            NextDotNetInteropCompletionReceived?.Completion.TrySetException(exception);
            NextJSInteropReceived?.Completion.TrySetException(exception);
            NextErrorReceived?.Completion?.TrySetResult(null);
        }

        private Task OnClosedAsync(Exception ex)
        {
            NextDisconnect?.Completion?.TrySetResult(null);

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
            await ExpectDotNetInterop(() => HubConnection.InvokeAsync("BeginInvokeDotNetFromJS", callId?.ToString(), assemblyName, methodIdentifier, dotNetObjectId ?? 0, argsJson));
        }

        public async Task<string> GetPrerenderDescriptors(Uri uri)
        {
            var httpClient = new HttpClient();
            Debug.Assert(httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", "__blazor_execution_mode=server"));
            var response = await httpClient.GetAsync(uri);
            var content = await response.Content.ReadAsStringAsync();

            var match = Regex.Match(content, $"{Regex.Escape("<!--Blazor:")}(.+?){Regex.Escape("-->")}");
            return $"[{match.Groups[1].Value}]";
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
                Completion = new TaskCompletionSource<object>(TaskContinuationOptions.RunContinuationsAsynchronously);
                Completion.Task.ContinueWith(
                    (task, state) =>
                    {
                        var operation = (CancellableOperation)state;
                        operation.Dispose();
                    },
                    this,
                    TaskContinuationOptions.ExecuteSynchronously); // We need to execute synchronously to clean-up before anything else continues
                if (Timeout != null)
                {
                    Cancellation = new CancellationTokenSource(Timeout.Value);
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
