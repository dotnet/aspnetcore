// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
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
        private static readonly AsyncLocal<IJSRuntime> _currentJSRuntime = new AsyncLocal<IJSRuntime>();

        internal static IJSRuntime Current => _currentJSRuntime.Value;

        private long _nextPendingTaskId = 1; // Start at 1 because zero signals "no response needed"
        private readonly ConcurrentDictionary<long, object> _pendingTasks
            = new ConcurrentDictionary<long, object>();

        private readonly ConcurrentDictionary<long, CancellationTokenRegistration> _cancellationRegistrations =
            new ConcurrentDictionary<long, CancellationTokenRegistration>();

        internal DotNetObjectReferenceManager ObjectRefManager { get; } = new DotNetObjectReferenceManager();

        /// <summary>
        /// Gets or sets the default timeout for asynchronous JavaScript calls.
        /// </summary>
        protected TimeSpan? DefaultAsyncTimeout { get; set; }

        /// <summary>
        /// Sets the current JS runtime to the supplied instance.
        ///
        /// This is intended for framework use. Developers should not normally need to call this method.
        /// </summary>
        /// <param name="instance">The new current <see cref="IJSRuntime"/>.</param>
        public static void SetCurrentJSRuntime(IJSRuntime instance)
        {
            _currentJSRuntime.Value = instance
                ?? throw new ArgumentNullException(nameof(instance));
        }

        /// <summary>
        /// Invokes the specified JavaScript function asynchronously.
        /// <para>
        /// <see cref="JSRuntime"/> will apply timeouts to this operation based on the value configured in <see cref="DefaultAsyncTimeout"/>. To dispatch a call with a different, or no timeout,
        /// consider using <see cref="InvokeAsync{TValue}(string, CancellationToken, object[])" />.
        /// </para>
        /// </summary>
        /// <typeparam name="TValue">The JSON-serializable return type.</typeparam>
        /// <param name="identifier">An identifier for the function to invoke. For example, the value <code>"someScope.someFunction"</code> will invoke the function <code>window.someScope.someFunction</code>.</param>
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
        /// <param name="identifier">An identifier for the function to invoke. For example, the value <code>"someScope.someFunction"</code> will invoke the function <code>window.someScope.someFunction</code>.</param>
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
                    JsonSerializer.Serialize(args, JsonSerializerOptionsProvider.Options) :
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
        /// <param name="callId">The id of the JavaScript callback to execute on completion.</param>
        /// <param name="success">Whether the operation succeeded or not.</param>
        /// <param name="resultOrError">The result of the operation or an object containing error details.</param>
        /// <param name="assemblyName">The name of the method assembly if the invocation was for a static method.</param>
        /// <param name="methodIdentifier">The identifier for the method within the assembly.</param>
        /// <param name="dotNetObjectId">The tracking id of the dotnet object if the invocation was for an instance method.</param>
        protected internal abstract void EndInvokeDotNet(
            string callId,
            bool success,
            object resultOrError,
            string assemblyName,
            string methodIdentifier,
            long dotNetObjectId);

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

                    var result = JsonSerializer.Deserialize(ref jsonReader, resultType, JsonSerializerOptionsProvider.Options);
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
    }
}
