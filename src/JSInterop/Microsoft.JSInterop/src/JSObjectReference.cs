// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.JSInterop
{
    internal class JSObjectReference : IJSObjectReference
    {
        public static readonly JsonEncodedText IdKey = JsonEncodedText.Encode("__jsObjectId");

        private readonly JSRuntime _jsRuntime;

        private bool _disposed;

        public long Id { get; }

        public JSObjectReference(JSRuntime jsRuntime, long id)
        {
            _jsRuntime = jsRuntime;

            Id = id;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            ThrowIfDisposed();

            return _jsRuntime.InvokeAsync<TValue>(Id, identifier, args);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            ThrowIfDisposed();

            return _jsRuntime.InvokeAsync<TValue>(Id, identifier, cancellationToken, args);
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;

                await _jsRuntime.InvokeVoidAsync("DotNet.jsCallDispatcher.disposeJSObjectReferenceById", Id);
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
