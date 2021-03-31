// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop.Infrastructure;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

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
                    new JSObjectReferenceJsonConverter(this),
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
        public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(0, identifier, args);

        /// <summary>
        /// Invokes the specified JavaScript function asynchronously.
        /// </summary>
        /// <typeparam name="TValue">The JSON-serializable return type.</typeparam>
        /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>window.someScope.someFunction</c>.</param>
        /// <param name="cancellationToken">
        /// A cancellation token to signal the cancellation of the operation. Specifying this parameter will override any default cancellations such as due to timeouts
        /// (<see cref="DefaultAsyncTimeout"/>) from being applied.
        /// </param>
        /// <param name="args">JSON-serializable arguments.</param>
        /// <returns>An instance of <typeparamref name="TValue"/> obtained by JSON-deserializing the return value.</returns>
        public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(0, identifier, cancellationToken, args);

        internal async ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(long targetInstanceId, string identifier, object?[]? args)
        {
            if (DefaultAsyncTimeout.HasValue)
            {
                using var cts = new CancellationTokenSource(DefaultAsyncTimeout.Value);
                // We need to await here due to the using
                return await InvokeAsync<TValue>(targetInstanceId, identifier, cts.Token, args);
            }

            return await InvokeAsync<TValue>(targetInstanceId, identifier, CancellationToken.None, args);
        }

        internal ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(
            long targetInstanceId,
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            var taskId = Interlocked.Increment(ref _nextPendingTaskId);
            var tcs = new TaskCompletionSource<TValue>();
            if (cancellationToken.CanBeCanceled)
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

                var argsJson = args is not null && args.Length != 0 ?
                    JsonSerializer.Serialize(args, JsonSerializerOptions) :
                    null;
                var resultType = JSCallResultTypeHelper.FromGeneric<TValue>();

                BeginInvokeJS(taskId, identifier, argsJson, resultType, targetInstanceId);

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

        /// <summary>
        /// Begins an asynchronous function invocation.
        /// </summary>
        /// <param name="taskId">The identifier for the function invocation, or zero if no async callback is required.</param>
        /// <param name="identifier">The identifier for the function to invoke.</param>
        /// <param name="argsJson">A JSON representation of the arguments.</param>
        protected virtual void BeginInvokeJS(long taskId, string identifier, string? argsJson)
            => BeginInvokeJS(taskId, identifier, argsJson, JSCallResultType.Default, 0);

        /// <summary>
        /// Begins an asynchronous function invocation.
        /// </summary>
        /// <param name="taskId">The identifier for the function invocation, or zero if no async callback is required.</param>
        /// <param name="identifier">The identifier for the function to invoke.</param>
        /// <param name="argsJson">A JSON representation of the arguments.</param>
        /// <param name="resultType">The type of result expected from the invocation.</param>
        /// <param name="targetInstanceId">The instance ID of the target JS object.</param>
        protected abstract void BeginInvokeJS(long taskId, string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId);

        /// <summary>
        /// Completes an async JS interop call from JavaScript to .NET
        /// </summary>
        /// <param name="invocationInfo">The <see cref="DotNetInvocationInfo"/>.</param>
        /// <param name="invocationResult">The <see cref="DotNetInvocationResult"/>.</param>
        protected internal abstract void EndInvokeDotNet(
            DotNetInvocationInfo invocationInfo,
            in DotNetInvocationResult invocationResult);

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072:RequiresUnreferencedCode", Justification = "We enforce trimmer attributes for JSON deserialized types on InvokeAsync.")]
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
