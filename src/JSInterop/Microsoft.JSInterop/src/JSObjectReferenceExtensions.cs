// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.JSInterop.Implementation;
using Microsoft.JSInterop.Infrastructure;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.JSInterop;

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
    public static async ValueTask InvokeVoidAsync(this IJSObjectReference jsObjectReference, string identifier, params object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(jsObjectReference);

        await jsObjectReference.InvokeAsync<IJSVoidResult>(identifier, args);
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
    public static ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(this IJSObjectReference jsObjectReference, string identifier, params object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(jsObjectReference);

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
    public static ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(this IJSObjectReference jsObjectReference, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(jsObjectReference);

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
    public static async ValueTask InvokeVoidAsync(this IJSObjectReference jsObjectReference, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(jsObjectReference);

        await jsObjectReference.InvokeAsync<IJSVoidResult>(identifier, cancellationToken, args);
    }

    /// <summary>
    /// Invokes the specified JavaScript function asynchronously.
    /// </summary>
    /// <param name="jsObjectReference">The <see cref="IJSObjectReference"/>.</param>
    /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>someScope.someFunction</c> on the target instance.</param>
    /// <param name="timeout">The duration after which to cancel the async operation. Overrides default timeouts (<see cref="JSRuntime.DefaultAsyncTimeout"/>).</param>
    /// <param name="args">JSON-serializable arguments.</param>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous invocation operation.</returns>
    public static async ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(this IJSObjectReference jsObjectReference, string identifier, TimeSpan timeout, params object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(jsObjectReference);

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
    public static async ValueTask InvokeVoidAsync(this IJSObjectReference jsObjectReference, string identifier, TimeSpan timeout, params object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(jsObjectReference);

        using var cancellationTokenSource = timeout == Timeout.InfiniteTimeSpan ? null : new CancellationTokenSource(timeout);
        var cancellationToken = cancellationTokenSource?.Token ?? CancellationToken.None;

        await jsObjectReference.InvokeAsync<IJSVoidResult>(identifier, cancellationToken, args);
    }

    /// <summary>
    /// Invokes the specified JavaScript constructor function asynchronously. The function is invoked with the <c>new</c> operator.
    /// </summary>
    /// <param name="jsObjectReference">The <see cref="IJSObjectReference"/>.</param>
    /// <param name="identifier">An identifier for the constructor function to invoke. For example, the value <c>"someScope.SomeClass"</c> will invoke the constructor <c>someScope.SomeClass</c>.</param>
    /// <param name="args">JSON-serializable arguments.</param>
    /// <returns>An <see cref="IJSObjectReference"/> instance that represents the created JS object.</returns>
    public static ValueTask<IJSObjectReference> InvokeNewAsync(this IJSObjectReference jsObjectReference, string identifier, params object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(jsObjectReference);

        return jsObjectReference.InvokeNewAsync(identifier, args);
    }

    /// <summary>
    /// Invokes the specified JavaScript constructor function asynchronously. The function is invoked with the <c>new</c> operator.
    /// </summary>
    /// <param name="jsObjectReference">The <see cref="IJSObjectReference"/>.</param>
    /// <param name="identifier">An identifier for the constructor function to invoke. For example, the value <c>"someScope.SomeClass"</c> will invoke the constructor <c>someScope.SomeClass</c>.</param>
    /// <param name="cancellationToken">
    /// A cancellation token to signal the cancellation of the operation. Specifying this parameter will override any default cancellations such as due to timeouts
    /// (<see cref="JSRuntime.DefaultAsyncTimeout"/>) from being applied.
    /// </param>
    /// <param name="args">JSON-serializable arguments.</param>
    /// <returns>An <see cref="IJSObjectReference"/> instance that represents the created JS object.</returns>
    public static ValueTask<IJSObjectReference> InvokeNewAsync(this IJSObjectReference jsObjectReference, string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(jsObjectReference);

        return jsObjectReference.InvokeNewAsync(identifier, cancellationToken, args);
    }

    /// <summary>
    /// Invokes the specified JavaScript constructor function asynchronously. The function is invoked with the <c>new</c> operator.
    /// </summary>
    /// <param name="jsObjectReference">The <see cref="IJSObjectReference"/>.</param>
    /// <param name="identifier">An identifier for the constructor function to invoke. For example, the value <c>"someScope.SomeClass"</c> will invoke the constructor <c>someScope.SomeClass</c>.</param>
    /// <param name="timeout">The duration after which to cancel the async operation. Overrides default timeouts (<see cref="JSRuntime.DefaultAsyncTimeout"/>).</param>
    /// <param name="args">JSON-serializable arguments.</param>
    /// <returns>An <see cref="IJSObjectReference"/> instance that represents the created JS object.</returns>
    public static ValueTask<IJSObjectReference> InvokeNewAsync(this IJSObjectReference jsObjectReference, string identifier, TimeSpan timeout, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(jsObjectReference);

        using var cancellationTokenSource = timeout == Timeout.InfiniteTimeSpan ? null : new CancellationTokenSource(timeout);
        var cancellationToken = cancellationTokenSource?.Token ?? CancellationToken.None;

        return jsObjectReference.InvokeNewAsync(identifier, cancellationToken, args);
    }

    /// <summary>
    /// Wraps the interop invocation of the JavaScript function referenced by <paramref name="jsObjectReference"/> as a .NET delegate.
    /// </summary>
    /// <param name="jsObjectReference">The JavaScript object reference that represents the function to be invoked.</param>
    /// <returns>A delegate that can be used to invoke the JavaScript function.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="jsObjectReference"/> is null.</exception>
    public static Func<ValueTask> AsFunction(this IJSObjectReference jsObjectReference)
    {
        ArgumentNullException.ThrowIfNull(jsObjectReference);

        return async () => await jsObjectReference.InvokeVoidAsync(string.Empty);
    }

    /// <summary>
    /// Wraps the interop invocation of the JavaScript function referenced by <paramref name="jsObjectReference"/> as a .NET delegate.
    /// </summary>
    /// <typeparam name="TResult">The JSON-serializable return type.</typeparam>
    /// <param name="jsObjectReference">The JavaScript object reference that represents the function to be invoked.</param>
    /// <returns>A delegate that can be used to invoke the JavaScript function.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="jsObjectReference"/> is null.</exception>
    public static Func<ValueTask<TResult>> AsFunction<[DynamicallyAccessedMembers(JsonSerialized)] TResult>(this IJSObjectReference jsObjectReference)
    {
        ArgumentNullException.ThrowIfNull(jsObjectReference);

        return async () => await jsObjectReference.InvokeAsync<TResult>(string.Empty);
    }

    /// <summary>
    /// Wraps the interop invocation of the JavaScript function referenced by <paramref name="jsObjectReference"/> as a .NET delegate.
    /// </summary>
    /// <typeparam name="T1">The JSON-serializable type of the first argument.</typeparam>
    /// <typeparam name="TResult">The JSON-serializable return type.</typeparam>
    /// <param name="jsObjectReference">The JavaScript object reference that represents the function to be invoked.</param>
    /// <returns>A delegate that can be used to invoke the JavaScript function.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="jsObjectReference"/> is null.</exception>
    public static Func<T1, ValueTask<TResult>> AsFunction<T1, [DynamicallyAccessedMembers(JsonSerialized)] TResult>(this IJSObjectReference jsObjectReference)
    {
        ArgumentNullException.ThrowIfNull(jsObjectReference);

        return async (T1 arg1) => await jsObjectReference.InvokeAsync<TResult>(string.Empty, [arg1]);
    }

    /// <summary>
    /// Wraps the interop invocation of the JavaScript function referenced by <paramref name="jsObjectReference"/> as a .NET delegate.
    /// </summary>
    /// <typeparam name="T1">The JSON-serializable type of the first argument.</typeparam>
    /// <typeparam name="T2">The JSON-serializable type of the second argument.</typeparam>
    /// <typeparam name="TResult">The JSON-serializable return type.</typeparam>
    /// <param name="jsObjectReference">The JavaScript object reference that represents the function to be invoked.</param>
    /// <returns>A delegate that can be used to invoke the JavaScript function.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="jsObjectReference"/> is null.</exception>
    public static Func<T1, T2, ValueTask<TResult>> AsFunction<T1, T2, [DynamicallyAccessedMembers(JsonSerialized)] TResult>(this IJSObjectReference jsObjectReference)
    {
        ArgumentNullException.ThrowIfNull(jsObjectReference);

        return async (T1 arg1, T2 arg2) => await jsObjectReference.InvokeAsync<TResult>(string.Empty, [arg1, arg2]);
    }

    /// <summary>
    /// Wraps the interop invocation of the JavaScript function referenced by <paramref name="jsObjectReference"/> as a .NET delegate.
    /// </summary>
    /// <typeparam name="T1">The JSON-serializable type of the first argument.</typeparam>
    /// <typeparam name="T2">The JSON-serializable type of the second argument.</typeparam>
    /// <typeparam name="T3">The JSON-serializable type of the third argument.</typeparam>
    /// <typeparam name="TResult">The JSON-serializable return type.</typeparam>
    /// <param name="jsObjectReference">The JavaScript object reference that represents the function to be invoked.</param>
    /// <returns>A delegate that can be used to invoke the JavaScript function.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="jsObjectReference"/> is null.</exception>
    public static Func<T1, T2, T3, ValueTask<TResult>> AsFunction<T1, T2, T3, [DynamicallyAccessedMembers(JsonSerialized)] TResult>(this IJSObjectReference jsObjectReference)
    {
        ArgumentNullException.ThrowIfNull(jsObjectReference);

        return async (T1 arg1, T2 arg2, T3 arg3) => await jsObjectReference.InvokeAsync<TResult>(string.Empty, [arg1, arg2, arg3]);
    }
}
