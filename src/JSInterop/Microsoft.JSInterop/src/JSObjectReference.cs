// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.JSInterop
{
    public class JSObjectReference
    {
        public static readonly JsonEncodedText JSObjectIdKey = JsonEncodedText.Encode("__jsObjectId");

        private readonly JSRuntime _jsRuntime;

        private readonly long _jsObjectId;

        internal JSObjectReference(JSRuntime jsRuntime, long jsObjectId)
        {
            _jsRuntime = jsRuntime;
            _jsObjectId = jsObjectId;
        }

        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, params object[] args)
        {
            if (_jsRuntime.DefaultAsyncTimeout.HasValue)
            {
                using var cts = new CancellationTokenSource(_jsRuntime.DefaultAsyncTimeout.Value);
                // We need to await here due to the using
                return await InvokeAsync<TValue>(identifier, cts.Token, args);
            }

            return await InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, params object[] args)
            => _jsRuntime.InvokeAsync<TValue>(identifier, cancellationToken, args, _jsObjectId);
    }
}
