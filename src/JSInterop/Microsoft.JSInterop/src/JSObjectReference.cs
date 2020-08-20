// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.JSInterop
{
    public class JSObjectReference : IJSRuntime, IDisposable, IAsyncDisposable
    {
        public static readonly JsonEncodedText IdKey = JsonEncodedText.Encode("__jsObjectId");

        private readonly JSRuntime _jsRuntime;

        private bool _disposed;

        internal long Id { get; }

        internal JSObjectReference(JSRuntime jsRuntime, long id)
        {
            _jsRuntime = jsRuntime;

            Id = id;
        }

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

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, params object[] args)
        {
            ThrowIfDisposed();

            return _jsRuntime.InvokeAsync<TValue>(identifier, cancellationToken, args, Id);
        }

        public void Dispose()
        {
            _ = DisposeAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;

                await _jsRuntime.InvokeVoidAsync("DotNet.jsCallDispatcher.disposeJSObjectReference", Id);
            }
        }

        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}
