// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Represents a reference to a JavaScript object.
    /// </summary>
    public class JSObjectReference : IJSRuntime, IDisposable, IAsyncDisposable
    {
        internal static readonly JsonEncodedText IdKey = JsonEncodedText.Encode("__jsObjectId");

        private readonly JSRuntime _jsRuntime;

        private bool _disposed;

        internal long Id { get; }

        internal JSObjectReference(JSRuntime jsRuntime, long id)
        {
            _jsRuntime = jsRuntime;

            Id = id;
        }

        /// <inheritdoc />
        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, params object[] args)
        {
            ThrowIfDisposed();

            if (_jsRuntime.DefaultAsyncTimeout.HasValue)
            {
                using var cts = new CancellationTokenSource(_jsRuntime.DefaultAsyncTimeout.Value);
                return await InvokeAsync<TValue>(identifier, cts.Token, args);
            }

            return await InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        }

        /// <inheritdoc />
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, params object[] args)
        {
            ThrowIfDisposed();

            return _jsRuntime.InvokeAsync<TValue>(identifier, cancellationToken, args, Id);
        }

        /// <summary>
        /// Disposes the <see cref="JSObjectReference"/>, freeing its resources and disabling it from further use.
        /// </summary>
        public void Dispose()
        {
            _ = DisposeAsync();
        }

        /// <summary>
        /// Disposes the <see cref="JSObjectReference"/>, freeing its resources and disabling it from further use.
        /// </summary>
        /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;

                await _jsRuntime.InvokeVoidAsync("DotNet.jsCallDispatcher.disposeJSObjectReference", Id);
            }
        }

        /// <summary>
        /// Throws an exception if this instance has been disposed.
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}
