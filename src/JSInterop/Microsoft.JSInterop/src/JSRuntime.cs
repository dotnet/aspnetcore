// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Abstract base class for a JavaScript runtime.
    /// </summary>
    public abstract partial class JSRuntime : IJSRuntime
    {
        private long _nextObjectReferenceId = 0; // 0 signals no object, but we increment prior to assignment. The first tracked object should have id 1
        private long _nextPendingTaskId = 1; // Start at 1 because zero signals "no response needed"
        private readonly ConcurrentDictionary<long, object> _pendingTasks = new ConcurrentDictionary<long, object>();
        private readonly ConcurrentDictionary<long, IDotNetObjectReference> _trackedRefsById = new ConcurrentDictionary<long, IDotNetObjectReference>();
        private readonly ConcurrentDictionary<long, CancellationTokenRegistration> _cancellationRegistrations =
            new ConcurrentDictionary<long, CancellationTokenRegistration>();

        /// <summary>
        /// Initializes a new instance of <see cref="JSRuntime"/>.
        /// </summary>
        protected JSRuntime()
        {
            JsonSerializerOptions = new JsonSerializerOptions
            {
                MaxDepth = 32,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                Converters =
                {
                    new DotNetObjectReferenceJsonConverterFactory(this),
                }
            };
        }

        /// <summary>
        /// Gets the <see cref="System.Text.Json.JsonSerializerOptions"/> used to serialize and deserialize interop payloads.
        /// </summary>
        protected internal JsonSerializerOptions JsonSerializerOptions { get; }

        /// <summary>
        /// Gets or sets the default timeout for asynchronous JavaScript calls.
        /// </summary>
        protected TimeSpan? DefaultAsyncTimeout { get; set; }

        /// <summary>
        /// Invokes the specified JavaScript function asynchronously.
        /// <para>
        /// <see cref="JSRuntime"/> will apply timeouts to this operation based on the value configured in <see cref="DefaultAsyncTimeout"/>. To dispatch a call with a different, or no timeout,
        /// consider using <see cref="InvokeAsync{TValue}(string, CancellationToken, object[])" />.
        /// </para>
        /// </summary>
        /// <typeparam name="TValue">The JSON-serializable return type.</typeparam>
        /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>window.someScope.someFunction</c>.</param>
        /// <param name="args">JSON-serializable arguments.</param>
        /// <returns>An instance of <typeparamref name="TValue"/> obtained by JSON-deserializing the return value.</returns>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
        {
            if (DefaultAsyncTimeout.HasValue)
            {
                return InvokeWithDefaultCancellation<TValue>(identifier, args);
            }

            return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        }

        /// <summary>
        /// Invokes the specified JavaScript function asynchronously.
        /// </summary>
        /// <typeparam name="TValue">The JSON-serializable return type.</typeparam>
        /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>window.someScope.someFunction</c>.</param>
        /// <param name="cancellationToken">
        /// A cancellation token to signal the cancellation of the operation. Specifying this parameter will override any default cancellations such as due to timeouts
        /// (<see cref="JSRuntime.DefaultAsyncTimeout"/>) from being applied.
        /// </param>
        /// <param name="args">JSON-serializable arguments.</param>
        /// <returns>An instance of <typeparamref name="TValue"/> obtained by JSON-deserializing the return value.</returns>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args)
        {
            var taskId = Interlocked.Increment(ref _nextPendingTaskId);
            var tcs = new TaskCompletionSource<TValue>(TaskContinuationOptions.RunContinuationsAsynchronously);
            if (cancellationToken != default)
            {
                _cancellationRegistrations[taskId] = cancellationToken.Register(() =>
                {
                    tcs.TrySetCanceled(cancellationToken);
                    CleanupTasksAndRegistrations(taskId);
                });
            }
            _pendingTasks[taskId] = tcs;

            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    tcs.TrySetCanceled(cancellationToken);
                    CleanupTasksAndRegistrations(taskId);

                    return new ValueTask<TValue>(tcs.Task);
                }

                var argsJson = args?.Any() == true ?
                    JsonSerializer.Serialize(args, JsonSerializerOptions) :
                    null;
                BeginInvokeJS(taskId, identifier, argsJson);

                return new ValueTask<TValue>(tcs.Task);
            }
            catch
            {
                CleanupTasksAndRegistrations(taskId);
                throw;
            }
        }

        private void CleanupTasksAndRegistrations(long taskId)
        {
            _pendingTasks.TryRemove(taskId, out _);
            if (_cancellationRegistrations.TryRemove(taskId, out var registration))
            {
                registration.Dispose();
            }
        }

        private async ValueTask<T> InvokeWithDefaultCancellation<T>(string identifier, object[] args)
        {
            using (var cts = new CancellationTokenSource(DefaultAsyncTimeout.Value))
            {
                // We need to await here due to the using
                return await InvokeAsync<T>(identifier, cts.Token, args);
            }
        }

        /// <summary>
        /// Begins an asynchronous function invocation.
        /// </summary>
        /// <param name="taskId">The identifier for the function invocation, or zero if no async callback is required.</param>
        /// <param name="identifier">The identifier for the function to invoke.</param>
        /// <param name="argsJson">A JSON representation of the arguments.</param>
        protected abstract void BeginInvokeJS(long taskId, string identifier, string argsJson);

        /// <summary>
        /// Completes an async JS interop call from JavaScript to .NET
        /// </summary>
        /// <param name="invocationInfo">The <see cref="DotNetInvocationInfo"/>.</param>
        /// <param name="invocationResult">The <see cref="DotNetInvocationResult"/>.</param>
        protected internal abstract void EndInvokeDotNet(
            DotNetInvocationInfo invocationInfo,
            in DotNetInvocationResult invocationResult);

        internal void EndInvokeJS(long taskId, bool succeeded, ref Utf8JsonReader jsonReader)
        {
            if (!_pendingTasks.TryRemove(taskId, out var tcs))
            {
                // We should simply return if we can't find an id for the invocation.
                // This likely means that the method that initiated the call defined a timeout and stopped waiting.
                return;
            }

            CleanupTasksAndRegistrations(taskId);

            try
            {
                if (succeeded)
                {
                    var resultType = TaskGenericsUtil.GetTaskCompletionSourceResultType(tcs);

                    var result = JsonSerializer.Deserialize(ref jsonReader, resultType, JsonSerializerOptions);
                    TaskGenericsUtil.SetTaskCompletionSourceResult(tcs, result);
                }
                else
                {
                    var exceptionText = jsonReader.GetString() ?? string.Empty;
                    TaskGenericsUtil.SetTaskCompletionSourceException(tcs, new JSException(exceptionText));
                }
            }
            catch (Exception exception)
            {
                var message = $"An exception occurred executing JS interop: {exception.Message}. See InnerException for more details.";
                TaskGenericsUtil.SetTaskCompletionSourceException(tcs, new JSException(message, exception));
            }
        }

        internal long TrackObjectReference<TValue>(DotNetObjectReference<TValue> dotNetObjectReference) where TValue : class
        {
            if (dotNetObjectReference == null)
            {
                throw new ArgumentNullException(nameof(dotNetObjectReference));
            }

            dotNetObjectReference.ThrowIfDisposed();

            var jsRuntime = dotNetObjectReference.JSRuntime;
            if (jsRuntime is null)
            {
                var dotNetObjectId = Interlocked.Increment(ref _nextObjectReferenceId);

                dotNetObjectReference.JSRuntime = this;
                dotNetObjectReference.ObjectId = dotNetObjectId;

                _trackedRefsById[dotNetObjectId] = dotNetObjectReference;
            }
            else if (!ReferenceEquals(this, jsRuntime))
            {
                throw new InvalidOperationException($"{dotNetObjectReference.GetType().Name} is already being tracked by a different instance of {nameof(JSRuntime)}." +
                    $" A common cause is caching an instance of {nameof(DotNetObjectReference<TValue>)} globally. Consider creating instances of {nameof(DotNetObjectReference<TValue>)} at the JSInterop callsite.");
            }

            Debug.Assert(dotNetObjectReference.ObjectId != 0);
            return dotNetObjectReference.ObjectId;
        }

        internal IDotNetObjectReference GetObjectReference(long dotNetObjectId)
        {
            return _trackedRefsById.TryGetValue(dotNetObjectId, out var dotNetObjectRef)
                ? dotNetObjectRef
                : throw new ArgumentException($"There is no tracked object with id '{dotNetObjectId}'. Perhaps the DotNetObjectReference instance was already disposed.", nameof(dotNetObjectId));
        }

        /// <summary>
        /// Stops tracking the specified .NET object reference.
        /// This may be invoked either by disposing a DotNetObjectRef in .NET code, or via JS interop by calling "dispose" on the corresponding instance in JavaScript code
        /// </summary>
        /// <param name="dotNetObjectId">The ID of the <see cref="DotNetObjectReference{TValue}"/>.</param>
        internal void ReleaseObjectReference(long dotNetObjectId) => _trackedRefsById.TryRemove(dotNetObjectId, out _);
    }
}
