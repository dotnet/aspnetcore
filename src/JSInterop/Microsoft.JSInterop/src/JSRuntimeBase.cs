// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop.Internal;

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Abstract base class for a JavaScript runtime.
    /// </summary>
    public abstract class JSRuntimeBase : IJSRuntime
    {
        private long _nextPendingTaskId = 1; // Start at 1 because zero signals "no response needed"
        private readonly ConcurrentDictionary<long, object> _pendingTasks
            = new ConcurrentDictionary<long, object>();

        internal DotNetObjectRefManager ObjectRefManager { get; } = new DotNetObjectRefManager();

        /// <summary>
        /// Invokes the specified JavaScript function asynchronously.
        /// </summary>
        /// <typeparam name="T">The JSON-serializable return type.</typeparam>
        /// <param name="identifier">An identifier for the function to invoke. For example, the value <code>"someScope.someFunction"</code> will invoke the function <code>window.someScope.someFunction</code>.</param>
        /// <param name="args">JSON-serializable arguments.</param>
        /// <returns>An instance of <typeparamref name="T"/> obtained by JSON-deserializing the return value.</returns>
        public Task<T> InvokeAsync<T>(string identifier, params object[] args)
        {
            // We might consider also adding a default timeout here in case we don't want to
            // risk a memory leak in the scenario where the JS-side code is failing to complete
            // the operation.

            var taskId = Interlocked.Increment(ref _nextPendingTaskId);
            var tcs = new TaskCompletionSource<T>();
            _pendingTasks[taskId] = tcs;

            try
            {
                var argsJson = args?.Length > 0 ?
                    JsonSerializer.Serialize(args, JsonSerializerOptionsProvider.Options) :
                    null;
                BeginInvokeJS(taskId, identifier, argsJson);
                return tcs.Task;
            }
            catch
            {
                _pendingTasks.TryRemove(taskId, out _);
                throw;
            }
        }

        /// <summary>
        /// Begins an asynchronous function invocation.
        /// </summary>
        /// <param name="asyncHandle">The identifier for the function invocation, or zero if no async callback is required.</param>
        /// <param name="identifier">The identifier for the function to invoke.</param>
        /// <param name="argsJson">A JSON representation of the arguments.</param>
        protected abstract void BeginInvokeJS(long asyncHandle, string identifier, string argsJson);

        internal void EndInvokeDotNet(string callId, bool success, object resultOrException)
        {
            // For failures, the common case is to call EndInvokeDotNet with the Exception object.
            // For these we'll serialize as something that's useful to receive on the JS side.
            // If the value is not an Exception, we'll just rely on it being directly JSON-serializable.
            if (!success && resultOrException is Exception)
            {
                resultOrException = resultOrException.ToString();
            }
            else if (!success && resultOrException is ExceptionDispatchInfo edi)
            {
                resultOrException = edi.SourceException.ToString();
            }

            // We pass 0 as the async handle because we don't want the JS-side code to
            // send back any notification (we're just providing a result for an existing async call)
            var args = JsonSerializer.Serialize(new[] { callId, success, resultOrException }, JsonSerializerOptionsProvider.Options);
            BeginInvokeJS(0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", args);
        }

        internal void EndInvokeJS(long asyncHandle, bool succeeded, JSAsyncCallResult asyncCallResult)
        {
            using (asyncCallResult?.JsonDocument)
            {
                if (!_pendingTasks.TryRemove(asyncHandle, out var tcs))
                {
                    throw new ArgumentException($"There is no pending task with handle '{asyncHandle}'.");
                }

                if (succeeded)
                {
                    var resultType = TaskGenericsUtil.GetTaskCompletionSourceResultType(tcs);
                    try
                    {
                        var result = asyncCallResult != null ?
                            JsonSerializer.Deserialize(asyncCallResult.JsonElement.GetRawText(), resultType, JsonSerializerOptionsProvider.Options) :
                            null;
                        TaskGenericsUtil.SetTaskCompletionSourceResult(tcs, result);
                    }
                    catch (Exception exception)
                    {
                        var message = $"An exception occurred executing JS interop: {exception.Message}. See InnerException for more details.";
                        TaskGenericsUtil.SetTaskCompletionSourceException(tcs, new JSException(message, exception));
                    }
                }
                else
                {
                    var exceptionText = asyncCallResult?.JsonElement.ToString() ?? string.Empty;
                    TaskGenericsUtil.SetTaskCompletionSourceException(tcs, new JSException(exceptionText));
                }
            }
        }
    }
}
