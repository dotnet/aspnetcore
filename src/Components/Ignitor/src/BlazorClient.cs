// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
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
using Microsoft.Extensions.Logging.Abstractions;

#nullable enable
namespace Ignitor
{
    public class BlazorClient : IAsyncDisposable
    {
        private const string MarkerPattern = ".*?<!--Blazor:(.*?)-->.*?";
        private HubConnection? _hubConnection;

        public BlazorClient()
        {
            CancellationTokenSource = new CancellationTokenSource();
            CancellationToken = CancellationTokenSource.Token;
            TaskCompletionSource = new TaskCompletionSource<object?>();

            CancellationTokenSource.Token.Register(() =>
            {
                TaskCompletionSource.TrySetCanceled();
            });
        }

        public TimeSpan? DefaultConnectionTimeout { get; set; } = Debugger.IsAttached ?
            Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(20);
        public TimeSpan? DefaultOperationTimeout { get; set; } = Debugger.IsAttached ?
            Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Gets or sets a value that determines whether the client will capture data such
        /// as render batches, interop calls, and errors for later inspection.
        /// </summary>
        public bool CaptureOperations { get; set; }

        /// <summary>
        /// Gets the collections of operation results that are captured when <see cref="CaptureOperations"/>
        /// is true.
        /// </summary>
        public Operations Operations { get; } = new Operations();

        public Func<string, Exception>? FormatError { get; set; }

        private CancellationTokenSource CancellationTokenSource { get; }

        private CancellationToken CancellationToken { get; }

        private TaskCompletionSource<object?> TaskCompletionSource { get; }

        private CancellableOperation<CapturedAttachComponentCall>? NextAttachComponentReceived { get; set; }

        private CancellableOperation<CapturedRenderBatch?>? NextBatchReceived { get; set; }

        private CancellableOperation<string?>? NextErrorReceived { get; set; }

        private CancellableOperation<Exception?>? NextDisconnect { get; set; }

        private CancellableOperation<CapturedJSInteropCall?>? NextJSInteropReceived { get; set; }

        private CancellableOperation<string?>? NextDotNetInteropCompletionReceived { get; set; }

        public ILoggerProvider LoggerProvider { get; set; } = NullLoggerProvider.Instance;

        public bool ConfirmRenderBatch { get; set; } = true;

        public event Action<CapturedJSInteropCall>? JSInterop;

        public event Action<CapturedRenderBatch>? RenderBatchReceived;

        public event Action<string>? DotNetInteropCompletion;

        public event Action<string>? OnCircuitError;

        public string? CircuitId { get; private set; }

        public ElementHive Hive { get; } = new ElementHive();

        public bool ImplicitWait => DefaultOperationTimeout != null;

        public HubConnection HubConnection => _hubConnection ?? throw new InvalidOperationException("HubConnection has not been initialized.");

        public Task<CapturedRenderBatch?> PrepareForNextBatch(TimeSpan? timeout)
        {
            if (NextBatchReceived != null && !NextBatchReceived.Disposed)
            {
                throw new InvalidOperationException("Invalid state previous task not completed");
            }

            NextBatchReceived = new CancellableOperation<CapturedRenderBatch?>(timeout, CancellationToken);
            return NextBatchReceived.Completion.Task;
        }

        public Task<CapturedJSInteropCall?> PrepareForNextJSInterop(TimeSpan? timeout)
        {
            if (NextJSInteropReceived != null && !NextJSInteropReceived.Disposed)
            {
                throw new InvalidOperationException("Invalid state previous task not completed");
            }

            NextJSInteropReceived = new CancellableOperation<CapturedJSInteropCall?>(timeout, CancellationToken);

            return NextJSInteropReceived.Completion.Task;
        }

        public Task<string?> PrepareForNextDotNetInterop(TimeSpan? timeout)
        {
            if (NextDotNetInteropCompletionReceived != null && !NextDotNetInteropCompletionReceived.Disposed)
            {
                throw new InvalidOperationException("Invalid state previous task not completed");
            }

            NextDotNetInteropCompletionReceived = new CancellableOperation<string?>(timeout, CancellationToken);

            return NextDotNetInteropCompletionReceived.Completion.Task;
        }

        public Task<string?> PrepareForNextCircuitError(TimeSpan? timeout)
        {
            if (NextErrorReceived != null && !NextErrorReceived.Disposed)
            {
                throw new InvalidOperationException("Invalid state previous task not completed");
            }

            NextErrorReceived = new CancellableOperation<string?>(timeout, CancellationToken);

            return NextErrorReceived.Completion.Task;
        }

        public Task<Exception?> PrepareForNextDisconnect(TimeSpan? timeout)
        {
            if (NextDisconnect != null && !NextDisconnect.Disposed)
            {
                throw new InvalidOperationException("Invalid state previous task not completed");
            }

            NextDisconnect = new CancellableOperation<Exception?>(timeout, CancellationToken);

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

        public async Task<CapturedRenderBatch?> ExpectRenderBatch(Func<Task> action, TimeSpan? timeout = null)
        {
            var task = WaitForRenderBatch(timeout);
            await action();
            return await task;
        }

        public async Task<CapturedJSInteropCall?> ExpectJSInterop(Func<Task> action, TimeSpan? timeout = null)
        {
            var task = WaitForJSInterop(timeout);
            await action();
            return await task;
        }

        public async Task<string?> ExpectDotNetInterop(Func<Task> action, TimeSpan? timeout = null)
        {
            var task = WaitForDotNetInterop(timeout);
            await action();
            return await task;
        }

        public async Task<string?> ExpectCircuitError(Func<Task> action, TimeSpan? timeout = null)
        {
            var task = WaitForCircuitError(timeout);
            await action();
            return await task;
        }

        public async Task<Exception?> ExpectDisconnect(Func<Task> action, TimeSpan? timeout = null)
        {
            var task = WaitForDisconnect(timeout);
            await action();
            return await task;
        }

        public async Task<(string? error, Exception? exception)> ExpectCircuitErrorAndDisconnect(Func<Task> action, TimeSpan? timeout = null)
        {
            string? error = default;

            // NOTE: timeout is used for each operation individually.
            var exception = await ExpectDisconnect(async () =>
            {
                error = await ExpectCircuitError(action, timeout);
            }, timeout);

            return (error, exception);
        }

        private async Task<CapturedRenderBatch?> WaitForRenderBatch(TimeSpan? timeout = null)
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
                catch (TimeoutException) when (FormatError != null)
                {
                    throw FormatError("Timed out while waiting for batch.");
                }
            }

            return null;
        }

        private async Task<CapturedJSInteropCall?> WaitForJSInterop(TimeSpan? timeout = null)
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
                catch (TimeoutException) when (FormatError != null)
                {
                    throw FormatError("Timed out while waiting for JS Interop.");
                }
            }

            return null;
        }

        private async Task<string?> WaitForDotNetInterop(TimeSpan? timeout = null)
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
                catch (TimeoutException) when (FormatError != null)
                {
                    throw FormatError("Timed out while waiting for .NET interop.");
                }
            }

            return null;
        }

        private async Task<string?> WaitForCircuitError(TimeSpan? timeout = null)
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
                catch (TimeoutException) when (FormatError != null)
                {
                    throw FormatError("Timed out while waiting for circuit error.");
                }
            }

            return null;
        }

        private async Task<Exception?> WaitForDisconnect(TimeSpan? timeout = null)
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
                catch (TimeoutException) when (FormatError != null)
                {
                    throw FormatError("Timed out while waiting for disconnect.");
                }
            }

            return null;
        }

        public async Task<bool> ConnectAsync(Uri uri, bool connectAutomatically = true, Action<HubConnectionBuilder, Uri>? configure = null)
        {
            var builder = new HubConnectionBuilder();
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol, IgnitorMessagePackHubProtocol>());
            var hubUrl = GetHubUrl(uri);
            builder.WithUrl(hubUrl);
            builder.ConfigureLogging(l =>
            {
                l.SetMinimumLevel(LogLevel.Trace);
                if (LoggerProvider != null)
                {
                    l.AddProvider(LoggerProvider);
                }
            });

            configure?.Invoke(builder, hubUrl);

            _hubConnection = builder.Build();

            HubConnection.On<int, string>("JS.AttachComponent", OnAttachComponent);
            HubConnection.On<int, string, string, int, long>("JS.BeginInvokeJS", OnBeginInvokeJS);
            HubConnection.On<string>("JS.EndInvokeDotNet", OnEndInvokeDotNet);
            HubConnection.On<int, byte[]>("JS.RenderBatch", OnRenderBatch);
            HubConnection.On<string>("JS.Error", OnError);
            HubConnection.Closed += OnClosedAsync;

            for (var i = 0; i < 10; i++)
            {
                try
                {
                    await HubConnection.StartAsync(CancellationToken);
                    break;
                }
                catch
                {
                    await Task.Delay(500);
                    // Retry 10 times
                }
            }

            if (!connectAutomatically)
            {
                return true;
            }

            var descriptors = await GetPrerenderDescriptors(uri);
            await ExpectRenderBatch(
                async () => CircuitId = await HubConnection.InvokeAsync<string>("StartCircuit", uri, uri, descriptors, CancellationToken),
                DefaultConnectionTimeout);
            return CircuitId != null;
        }

        private void OnEndInvokeDotNet(string message)
        {
            Operations?.DotNetCompletions.Enqueue(message);
            DotNetInteropCompletion?.Invoke(message);

            NextDotNetInteropCompletionReceived?.Completion?.TrySetResult(null);
        }

        private void OnAttachComponent(int componentId, string domSelector)
        {
            var call = new CapturedAttachComponentCall(componentId, domSelector);
            Operations?.AttachComponent.Enqueue(call);

            NextAttachComponentReceived?.Completion?.TrySetResult(call);
        }

        private void OnBeginInvokeJS(int asyncHandle, string identifier, string argsJson, int resultType, long targetInstanceId)
        {
            var call = new CapturedJSInteropCall(asyncHandle, identifier, argsJson, resultType, targetInstanceId);
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

        public Task ConfirmBatch(int batchId, string? error = null)
        {
            return HubConnection.InvokeAsync("OnRenderCompleted", batchId, error, CancellationToken);
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
            NextAttachComponentReceived?.Completion?.TrySetException(exception);
            NextErrorReceived?.Completion?.TrySetResult(null);
        }

        private Task OnClosedAsync(Exception? ex)
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
                builder.Path += builder.Path.EndsWith("/", StringComparison.Ordinal) ? "_blazor" : "/_blazor";
                return builder.Uri;
            }
        }

        public async Task InvokeDotNetMethod(object callId, string assemblyName, string methodIdentifier, object dotNetObjectId, string argsJson)
        {
            await ExpectDotNetInterop(() => HubConnection.InvokeAsync(
                "BeginInvokeDotNetFromJS",
                callId?.ToString(),
                assemblyName,
                methodIdentifier,
                dotNetObjectId ?? 0,
                argsJson,
                CancellationToken));
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
            if (!CancellationTokenSource.IsCancellationRequested)
            {
                CancellationTokenSource.Cancel();
                CancellationTokenSource.Dispose();
            }
        }

        public ElementNode FindElementById(string id)
        {
            if (!Hive.TryFindElementById(id, out var element))
            {
                throw new InvalidOperationException($"Element with id '{id}' was not found.");
            }

            return element;
        }

        private string[] ReadMarkers(string content)
        {
            content = content.Replace("\r\n", "").Replace("\n", "");
            var matches = Regex.Matches(content, MarkerPattern);
            var markers = matches.Select(s => (value: s.Groups[1].Value, parsed: JsonDocument.Parse(s.Groups[1].Value)))
                .Where(s =>
                {
                    return s.parsed.RootElement.TryGetProperty("type", out var markerType) &&
                        markerType.ValueKind != JsonValueKind.Undefined &&
                        markerType.GetString() == "server";
                })
                .OrderBy(p => p.parsed.RootElement.GetProperty("sequence").GetInt32())
                .Select(p => p.value)
                .ToArray();

            return markers;
        }

        public async ValueTask DisposeAsync()
        {
            Cancel();
            if (HubConnection != null)
            {
                await HubConnection.DisposeAsync();
            }
        }
    }
}

#nullable restore
