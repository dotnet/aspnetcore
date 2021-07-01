// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.WebAssembly.Infrastructure
{
    internal sealed class WebAssemblyJSDataStream : BaseJSDataStream
    {
        private readonly DefaultWebAssemblyJSRuntime _webAssemblyJSRuntime;

        public static async Task<bool> ReceiveData(DefaultWebAssemblyJSRuntime runtime, long streamId, long chunkId, byte[] chunk, string error)
        {
            if (!runtime.JSDataStreamInstances.TryGetValue(streamId, out var instance))
            {
                // There is no data stream with the given identifier. It may have already been disposed.
                // We notify JS that the stream has been cancelled/disposed.
                return false;
            }

            return await instance.ReceiveData(chunkId, chunk, error);
        }

        internal static async ValueTask<WebAssemblyJSDataStream> CreateWebAssemblyJSDataStreamAsync(
            DefaultWebAssemblyJSRuntime runtime,
            IJSStreamReference jsStreamReference,
            long totalLength,
            long maxBufferSize,
            long chunkSize,
            TimeSpan defaultCallTimeout,
            CancellationToken cancellationToken = default)
        {
            var streamId = runtime.JSDataStreamNextInstanceId++;
            var webAssemblyJSDataStream = new WebAssemblyJSDataStream(runtime, streamId, totalLength, maxBufferSize, defaultCallTimeout, cancellationToken);
            await runtime.InvokeVoidAsync("Blazor._internal.sendJSDataStream", jsStreamReference, streamId, chunkSize);
            return webAssemblyJSDataStream;
        }

        private WebAssemblyJSDataStream(
            DefaultWebAssemblyJSRuntime runtime,
            long streamId,
            long totalLength,
            long maxBufferSize,
            TimeSpan jsInteropDefaultCallTimeout,
            CancellationToken cancellationToken) : base(runtime.JSDataStreamInstances, streamId, totalLength, maxBufferSize, jsInteropDefaultCallTimeout, cancellationToken)
        {
            _webAssemblyJSRuntime = runtime;
        }

        protected override void RaiseUnhandledException(Exception exception)
        {
            throw exception; // TODO: test
        }
    }
}
