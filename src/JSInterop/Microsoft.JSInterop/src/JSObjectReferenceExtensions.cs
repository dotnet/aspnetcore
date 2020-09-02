// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Extensions for <see cref="IJSObjectReference"/>.
    /// </summary>
    public static class JSObjectReferenceExtensions
    {
        /// <summary>
        /// Invokes the specified JavaScript function asynchronously.
        /// </summary>
        /// <param name="jsObjectReference">The <see cref="IJSObjectReference"/>.</param>
        /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>someScope.someFunction</c> on the target instance.</param>
        /// <param name="args">JSON-serializable arguments.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous invocation operation.</returns>
        public static async ValueTask InvokeVoidAsync(this IJSObjectReference jsObjectReference, string identifier, params object?[] args)
        {
            if (jsObjectReference is null)
            {
                throw new ArgumentNullException(nameof(jsObjectReference));
            }

            await jsObjectReference.InvokeAsync<object>(identifier, args);
        }

        /// <summary>
        /// Invokes the specified JavaScript function asynchronously.
        /// <para>
        /// <see cref="JSRuntime"/> will apply timeouts to this operation based on the value configured in <see cref="JSRuntime.DefaultAsyncTimeout"/>. To dispatch a call with a different timeout, or no timeout,
        /// consider using <see cref="IJSObjectReference.InvokeAsync{TValue}(string, CancellationToken, object[])" />.
        /// </para>
        /// </summary>
        /// <param name="jsObjectReference">The <see cref="IJSObjectReference"/>.</param>
        /// <typeparam name="TValue">The JSON-serializable return type.</typeparam>
        /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>someScope.someFunction</c> on the target instance.</param>
        /// <param name="args">JSON-serializable arguments.</param>
        /// <returns>An instance of <typeparamref name="TValue"/> obtained by JSON-deserializing the return value.</returns>
        public static ValueTask<TValue> InvokeAsync<TValue>(this IJSObjectReference jsObjectReference, string identifier, params object?[] args)
        {
            if (jsObjectReference is null)
            {
                throw new ArgumentNullException(nameof(jsObjectReference));
            }

            return jsObjectReference.InvokeAsync<TValue>(identifier, args);
        }

        /// <summary>
        /// Invokes the specified JavaScript function asynchronously.
        /// </summary>
        /// <typeparam name="TValue">The JSON-serializable return type.</typeparam>
        /// <param name="jsObjectReference">The <see cref="IJSObjectReference"/>.</param>
        /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>someScope.someFunction</c> on the target instance.</param>
        /// <param name="cancellationToken">
        /// A cancellation token to signal the cancellation of the operation. Specifying this parameter will override any default cancellations such as due to timeouts
        /// (<see cref="JSRuntime.DefaultAsyncTimeout"/>) from being applied.
        /// </param>
        /// <param name="args">JSON-serializable arguments.</param>
        /// <returns>An instance of <typeparamref name="TValue"/> obtained by JSON-deserializing the return value.</returns>
        public static ValueTask<TValue> InvokeAsync<TValue>(this IJSObjectReference jsObjectReference, string identifier, CancellationToken cancellationToken, params object?[] args)
        {
            if (jsObjectReference is null)
            {
                throw new ArgumentNullException(nameof(jsObjectReference));
            }

            return jsObjectReference.InvokeAsync<TValue>(identifier, cancellationToken, args);
        }

        /// <summary>
        /// Invokes the specified JavaScript function asynchronously.
        /// </summary>
        /// <param name="jsObjectReference">The <see cref="IJSObjectReference"/>.</param>
        /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>someScope.someFunction</c> on the target instance.</param>
        /// <param name="cancellationToken">
        /// A cancellation token to signal the cancellation of the operation. Specifying this parameter will override any default cancellations such as due to timeouts
        /// (<see cref="JSRuntime.DefaultAsyncTimeout"/>) from being applied.
        /// </param>
        /// <param name="args">JSON-serializable arguments.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous invocation operation.</returns>
        public static async ValueTask InvokeVoidAsync(this IJSObjectReference jsObjectReference, string identifier, CancellationToken cancellationToken, params object?[] args)
        {
            if (jsObjectReference is null)
            {
                throw new ArgumentNullException(nameof(jsObjectReference));
            }

            await jsObjectReference.InvokeAsync<object>(identifier, cancellationToken, args);
        }

        /// <summary>
        /// Invokes the specified JavaScript function asynchronously.
        /// </summary>
        /// <param name="jsObjectReference">The <see cref="IJSObjectReference"/>.</param>
        /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>someScope.someFunction</c> on the target instance.</param>
        /// <param name="timeout">The duration after which to cancel the async operation. Overrides default timeouts (<see cref="JSRuntime.DefaultAsyncTimeout"/>).</param>
        /// <param name="args">JSON-serializable arguments.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous invocation operation.</returns>
        public static async ValueTask<TValue> InvokeAsync<TValue>(this IJSObjectReference jsObjectReference, string identifier, TimeSpan timeout, params object?[] args)
        {
            if (jsObjectReference is null)
            {
                throw new ArgumentNullException(nameof(jsObjectReference));
            }

            using var cancellationTokenSource = timeout == Timeout.InfiniteTimeSpan ? null : new CancellationTokenSource(timeout);
            var cancellationToken = cancellationTokenSource?.Token ?? CancellationToken.None;

            return await jsObjectReference.InvokeAsync<TValue>(identifier, cancellationToken, args);
        }

        /// <summary>
        /// Invokes the specified JavaScript function asynchronously.
        /// </summary>
        /// <param name="jsObjectReference">The <see cref="IJSObjectReference"/>.</param>
        /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>someScope.someFunction</c> on the target instance.</param>
        /// <param name="timeout">The duration after which to cancel the async operation. Overrides default timeouts (<see cref="JSRuntime.DefaultAsyncTimeout"/>).</param>
        /// <param name="args">JSON-serializable arguments.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous invocation operation.</returns>
        public static async ValueTask InvokeVoidAsync(this IJSObjectReference jsObjectReference, string identifier, TimeSpan timeout, params object?[] args)
        {
            if (jsObjectReference is null)
            {
                throw new ArgumentNullException(nameof(jsObjectReference));
            }

            using var cancellationTokenSource = timeout == Timeout.InfiniteTimeSpan ? null : new CancellationTokenSource(timeout);
            var cancellationToken = cancellationTokenSource?.Token ?? CancellationToken.None;

            await jsObjectReference.InvokeAsync<object>(identifier, cancellationToken, args);
        }
    }
}
