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
        private readonly SemaphoreSlim receiveDataSemaphore = new(initialCount: 1, maxCount: 1);

        internal static async ValueTask<WebAssemblyJSDataStream> CreateWebAssemblyJSDataStreamAsync(
            DefaultWebAssemblyJSRuntime runtime,
            IJSStreamReference jsStreamReference,
            long totalLength,
            long chunkSize,
            TimeSpan defaultCallTimeout,
            long pauseIncomingBytesThreshold = -1,
            long resumeIncomingBytesThreshold = -1,
            CancellationToken cancellationToken = default)
        {
            var streamId = runtime.JSDataStreamNextInstanceId++;
            var webAssemblyJSDataStream = new WebAssemblyJSDataStream(runtime, streamId, totalLength, defaultCallTimeout, pauseIncomingBytesThreshold, resumeIncomingBytesThreshold, cancellationToken);
            var dotnetStreamReference = DotNetObjectReference.Create(webAssemblyJSDataStream);
            await runtime.InvokeVoidAsync("Blazor._internal.sendJSDataStreamUsingObjectReference", jsStreamReference, streamId, chunkSize, dotnetStreamReference);
            return webAssemblyJSDataStream;
        }

        private WebAssemblyJSDataStream(
            DefaultWebAssemblyJSRuntime runtime,
            long streamId,
            long totalLength,
            TimeSpan jsInteropDefaultCallTimeout,
            long pauseIncomingBytesThreshold,
            long resumeIncomingBytesThreshold,
            CancellationToken cancellationToken) : base(runtime.JSDataStreamInstances, streamId, totalLength, jsInteropDefaultCallTimeout, pauseIncomingBytesThreshold, resumeIncomingBytesThreshold, cancellationToken)
        {
            _webAssemblyJSRuntime = runtime;
        }

        protected override void RaiseUnhandledException(Exception exception)
        {
            throw exception;
        }

        [JSInvokable("ReceiveJSDataChunk")]
        public async Task<bool> ReceiveJSDataChunk(long streamId, long chunkId, string error)
        {
            // Ensure that the DotNetDataReference still points to an active stream
            if (!_webAssemblyJSRuntime.JSDataStreamInstances.ContainsKey(streamId))
            {
                // There is no data stream with the given identifier. It may have already been disposed.
                // We notify JS that the stream has been cancelled/disposed.
                return false;
            }

            var data = Array.Empty<byte>();
            if (string.IsNullOrEmpty(error))
            {
                // Ideally this byte array would be transferred directly as a parameter on this
                // call, however that's not currently possible due to: https://github.com/dotnet/runtime/issues/53378
                data = _webAssemblyJSRuntime.InvokeUnmarshalled<byte[]>("Blazor._internal.retrieveByteArray");
            }

            try
            {
                await receiveDataSemaphore.WaitAsync(_streamCancellationToken);
                return await ReceiveData(chunkId, data, error);
            }
            finally
            {
                receiveDataSemaphore.Release();
            }
        }
    }
}
