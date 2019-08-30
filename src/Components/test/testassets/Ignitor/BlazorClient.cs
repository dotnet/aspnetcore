// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
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
        private const string MarkerPattern = ".*?<!--Blazor:(.*?)-->.*?";

        public BlazorClient()
        {
            CancellationTokenSource = new CancellationTokenSource();
            TaskCompletionSource = new TaskCompletionSource<object>();

            CancellationTokenSource.Token.Register(() =>
            {
                TaskCompletionSource.TrySetCanceled();
            });
        }

        public TimeSpan? DefaultConnectionTimeout { get; set; } = TimeSpan.FromSeconds(10);
        public TimeSpan? DefaultOperationTimeout { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Gets or sets a value that determines whether the client will capture data such
        /// as render batches, interop calls, and errors for later inspection.
        /// </summary>
        public bool CaptureOperations { get; set; }

        /// <summary>
        /// Gets the collections of operation results that are captured when <see cref="CaptureOperations"/>
        /// is true.
        /// </summary>
        public Operations Operations { get; private set; }

        public Func<string, Exception> FormatError { get; set; }

        private CancellationTokenSource CancellationTokenSource { get; }

        private CancellationToken CancellationToken => CancellationTokenSource.Token;

        private TaskCompletionSource<object> TaskCompletionSource { get; }

        private CancellableOperation<CapturedRenderBatch> NextBatchReceived { get; set; }

        private CancellableOperation<string> NextErrorReceived { get; set; }

        private CancellableOperation<Exception> NextDisconnect { get; set; }

        private CancellableOperation<CapturedJSInteropCall> NextJSInteropReceived { get; set; }

        private CancellableOperation<string> NextDotNetInteropCompletionReceived { get; set; }

        public ILoggerProvider LoggerProvider { get; set; }

        public bool ConfirmRenderBatch { get; set; } = true;

        public event Action<CapturedJSInteropCall> JSInterop;

        public event Action<CapturedRenderBatch> RenderBatchReceived;

        public event Action<string> DotNetInteropCompletion;

        public event Action<string> OnCircuitError;

        public string CircuitId { get; set; }

        public ElementHive Hive { get; set; } = new ElementHive();

        public bool ImplicitWait => DefaultOperationTimeout != null;

        public HubConnection HubConnection { get; set; }

        public Task<CapturedRenderBatch> PrepareForNextBatch(TimeSpan? timeout)
        {
            if (NextBatchReceived?.Completion != null)
            {
                throw new InvalidOperationException("Invalid state previous task not completed");
            }

            NextBatchReceived = new CancellableOperation<CapturedRenderBatch>(timeout);

            return NextBatchReceived.Completion.Task;
        }

        public Task<CapturedJSInteropCall> PrepareForNextJSInterop(TimeSpan? timeout)
        {
            if (NextJSInteropReceived?.Completion != null)
            {
                throw new InvalidOperationException("Invalid state previous task not completed");
            }

            NextJSInteropReceived = new CancellableOperation<CapturedJSInteropCall>(timeout);

            return NextJSInteropReceived.Completion.Task;
        }

        public Task<string> PrepareForNextDotNetInterop(TimeSpan? timeout)
        {
            if (NextDotNetInteropCompletionReceived?.Completion != null)
            {
                throw new InvalidOperationException("Invalid state previous task not completed");
            }

            NextDotNetInteropCompletionReceived = new CancellableOperation<string>(timeout);

            return NextDotNetInteropCompletionReceived.Completion.Task;
        }

        public Task<string> PrepareForNextCircuitError(TimeSpan? timeout)
        {
            if (NextErrorReceived?.Completion != null)
            {
                throw new InvalidOperationException("Invalid state previous task not completed");
            }

            NextErrorReceived = new CancellableOperation<string>(timeout);

            return NextErrorReceived.Completion.Task;
        }

        public Task<Exception> PrepareForNextDisconnect(TimeSpan? timeout)
        {
            if (NextDisconnect?.Completion != null)
            {
                throw new InvalidOperationException("Invalid state previous task not completed");
            }

            NextDisconnect = new CancellableOperation<Exception>(timeout);

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

        public async Task<CapturedRenderBatch> ExpectRenderBatch(Func<Task> action, TimeSpan? timeout = null)
        {
            var task = WaitForRenderBatch(timeout);
            await action();
            return await task;
        }

        public async Task<CapturedJSInteropCall> ExpectJSInterop(Func<Task> action, TimeSpan? timeout = null)
        {
            var task = WaitForJSInterop(timeout);
            await action();
            return await task;
        }

        public async Task<string> ExpectDotNetInterop(Func<Task> action, TimeSpan? timeout = null)
        {
            var task = WaitForDotNetInterop(timeout);
            await action();
            return await task;
        }

        public async Task<string> ExpectCircuitError(Func<Task> action, TimeSpan? timeout = null)
        {
            var task = WaitForCircuitError(timeout);
            await action();
            return await task;
        }

        public async Task<Exception> ExpectDisconnect(Func<Task> action, TimeSpan? timeout = null)
        {
            var task = WaitForDisconnect(timeout);
            await action();
            return await task;
        }

        public async Task<(string error, Exception exception)> ExpectCircuitErrorAndDisconnect(Func<Task> action, TimeSpan? timeout = null)
        {
            string error = null;

            // NOTE: timeout is used for each operation individually.
            var exception = await ExpectDisconnect(async () =>
            {
                error = await ExpectCircuitError(action, timeout);
            }, timeout);

            return (error, exception);
        }

        private async Task<CapturedRenderBatch> WaitForRenderBatch(TimeSpan? timeout = null)
        {
            if (ImplicitWait)
            {
                if (DefaultOperationTimeout == null && timeout == null)
                {
                    throw new InvalidOperationException("Implicit wait without DefaultLatencyTimeout is not allowed.");
                }

                try
                {
                    return await PrepareForNextBatch(timeout ?? DefaultOperationTimeout);
                }
                catch (OperationCanceledException)
                {
                    throw FormatError("Timed out while waiting for batch.");
                }
            }

            return null;
        }

        private async Task<CapturedJSInteropCall> WaitForJSInterop(TimeSpan? timeout = null)
        {
            if (ImplicitWait)
            {
                if (DefaultOperationTimeout == null && timeout == null)
                {
                    throw new InvalidOperationException("Implicit wait without DefaultLatencyTimeout is not allowed.");
                }

                try
                {
                    return await PrepareForNextJSInterop(timeout ?? DefaultOperationTimeout);
                }
                catch (OperationCanceledException)
                {
                    throw FormatError("Timed out while waiting for JS Interop.");
                }
            }

            return null;
        }

        private async Task<string> WaitForDotNetInterop(TimeSpan? timeout = null)
        {
            if (ImplicitWait)
            {
                if (DefaultOperationTimeout == null && timeout == null)
                {
                    throw new InvalidOperationException("Implicit wait without DefaultLatencyTimeout is not allowed.");
                }

                try
                {
                    return await PrepareForNextDotNetInterop(timeout ?? DefaultOperationTimeout);
                }
                catch (OperationCanceledException)
                {
                    throw FormatError("Timed out while waiting for .NET interop.");
                }
            }

            return null;
        }

        private async Task<string> WaitForCircuitError(TimeSpan? timeout = null)
        {
            if (ImplicitWait)
            {
                if (DefaultOperationTimeout == null && timeout == null)
                {
                    throw new InvalidOperationException("Implicit wait without DefaultLatencyTimeout is not allowed.");
                }

                try
                {
                    return await PrepareForNextCircuitError(timeout ?? DefaultOperationTimeout);
                }
                catch (OperationCanceledException)
                {
                    throw FormatError("Timed out while waiting for circuit error.");
                }
            }

            return null;
        }

        private async Task<Exception> WaitForDisconnect(TimeSpan? timeout = null)
        {
            if (ImplicitWait)
            {
                if (DefaultOperationTimeout == null && timeout == null)
                {
                    throw new InvalidOperationException("Implicit wait without DefaultLatencyTimeout is not allowed.");
                }

                try
                {
                    return await PrepareForNextDisconnect(timeout ?? DefaultOperationTimeout);
                }
                catch (OperationCanceledException)
                {
                    throw FormatError("Timed out while waiting for disconnect.");
                }
            }

            return null;
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

            if (CaptureOperations)
            {
                Operations = new Operations();
            }

            if (!connectAutomatically)
            {
                return true;
            }

            var descriptors = await GetPrerenderDescriptors(uri);
            await ExpectRenderBatch(
                async () => CircuitId = await HubConnection.InvokeAsync<string>("StartCircuit", uri, uri, descriptors),
                DefaultConnectionTimeout);
            return CircuitId != null;
        }

        private void OnEndInvokeDotNet(string message)
        {
            Operations?.DotNetCompletions.Enqueue(message);
            DotNetInteropCompletion?.Invoke(message);

            NextDotNetInteropCompletionReceived?.Completion?.TrySetResult(null);
        }

        private void OnBeginInvokeJS(int asyncHandle, string identifier, string argsJson)
        {
            var call = new CapturedJSInteropCall(asyncHandle, identifier, argsJson);
            Operations?.JSInteropCalls.Enqueue(call);
            JSInterop?.Invoke(call);

            NextJSInteropReceived?.Completion?.TrySetResult(null);
        }

        private void OnRenderBatch(int id, byte[] data)
        {
            var capturedBatch = new CapturedRenderBatch(id, data);

            Operations?.Batches.Enqueue(capturedBatch);
            RenderBatchReceived?.Invoke(capturedBatch);

            var batch = RenderBatchReader.Read(data);

            Hive.Update(batch);

            if (ConfirmRenderBatch)
            {
                _ = ConfirmBatch(id);
            }

            NextBatchReceived?.Completion?.TrySetResult(null);
        }

        public Task ConfirmBatch(int batchId, string error = null)
        {
            return HubConnection.InvokeAsync("OnRenderCompleted", batchId, error);
        }

        private void OnError(string error)
        {
            Operations?.Errors.Enqueue(error);
            OnCircuitError?.Invoke(error);

            // If we get an error, forcibly terminate anything else we're waiting for. These
            // tests should only encounter errors in specific situations, and this ensures that
            // we fail with a good message.
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
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", "__blazor_execution_mode=server");
            var response = await httpClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            var match = ReadMarkers(content);
            return $"[{string.Join(", ", match)}]";
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

        private class CancellableOperation<TResult>
        {
            public CancellableOperation(TimeSpan? timeout)
            {
                Timeout = timeout;
                Initialize();
            }

            public TimeSpan? Timeout { get; }

            public TaskCompletionSource<TResult> Completion { get; set; }

            public CancellationTokenSource Cancellation { get; set; }

            public CancellationTokenRegistration CancellationRegistration { get; set; }

            private void Initialize()
            {
                Completion = new TaskCompletionSource<TResult>(TaskContinuationOptions.RunContinuationsAsynchronously);
                Completion.Task.ContinueWith(
                    (task, state) =>
                    {
                        var operation = (CancellableOperation<TResult>)state;
                        operation.Dispose();
                    },
                    this,
                    TaskContinuationOptions.ExecuteSynchronously); // We need to execute synchronously to clean-up before anything else continues
                if (Timeout != null && Timeout != System.Threading.Timeout.InfiniteTimeSpan && Timeout != TimeSpan.MaxValue)
                {
                    Cancellation = new CancellationTokenSource(Timeout.Value);
                    CancellationRegistration = Cancellation.Token.Register(
                        (self) =>
                        {
                            var operation = (CancellableOperation<TResult>)self;
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
        }

        private string[] ReadMarkers(string content)
        {
            content = content.Replace("\r\n", "").Replace("\n", "");
            var matches = Regex.Matches(content, MarkerPattern);
            var markers = matches.Select(s => (value: s.Groups[1].Value, parsed: JsonDocument.Parse(s.Groups[1].Value)))
                .Where(s =>
                {
                    var markerType = s.parsed.RootElement.GetProperty("type");
                    return markerType.ValueKind != JsonValueKind.Undefined && markerType.GetString() == "server";
                })
                .OrderBy(p => p.parsed.RootElement.GetProperty("sequence").GetInt32())
                .Select(p => p.value)
                .ToArray();

            return markers;
        }
    }
}
