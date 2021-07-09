// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.WebView.Services
{
    internal sealed class WebViewJSDataStream : BaseJSDataStream
    {
        private readonly WebViewJSRuntime _webviewJSRuntime;
        private readonly SemaphoreSlim receiveDataSemaphore = new(initialCount: 1, maxCount: 1);

        internal static async ValueTask<WebViewJSDataStream> CreateWebViewJSDataStreamAsync(
            WebViewJSRuntime runtime,
            IJSStreamReference jsStreamReference,
            long totalLength,
            long chunkSize,
            TimeSpan defaultCallTimeout,
            long pauseIncomingBytesThreshold = -1,
            long resumeIncomingBytesThreshold = -1,
            CancellationToken cancellationToken = default)
        {
            var streamId = runtime.JSDataStreamNextInstanceId++;
            var webViewJSDataStream = new WebViewJSDataStream(runtime, streamId, totalLength, defaultCallTimeout, pauseIncomingBytesThreshold, resumeIncomingBytesThreshold, cancellationToken);

            var dotnetStreamReference = DotNetObjectReference.Create(webViewJSDataStream);
            await runtime.InvokeVoidAsync("Blazor._internal.sendJSDataStreamUsingObjectReference", jsStreamReference, streamId, chunkSize, dotnetStreamReference);

            return webViewJSDataStream;
        }

        private WebViewJSDataStream(
            WebViewJSRuntime runtime,
            long streamId,
            long totalLength,
            TimeSpan jsInteropDefaultCallTimeout,
            long pauseIncomingBytesThreshold,
            long resumeIncomingBytesThreshold,
            CancellationToken cancellationToken) : base(runtime.JSDataStreamInstances, streamId, totalLength, jsInteropDefaultCallTimeout, pauseIncomingBytesThreshold, resumeIncomingBytesThreshold, cancellationToken)
        {
            _webviewJSRuntime = runtime;
        }

        protected override void RaiseUnhandledException(Exception exception)
        {
            _webviewJSRuntime.RaiseUnhandledException(exception);
        }

        [JSInvokable("ReceiveJSDataChunk")]
        public async Task<bool> ReceiveJSDataChunk(long streamId, long chunkId, byte[] chunk, string error)
        {
            // Ensure that the DotNetDataReference still points to an active stream
            if (!_webviewJSRuntime.JSDataStreamInstances.ContainsKey(streamId))
            {
                // There is no data stream with the given identifier. It may have already been disposed.
                // We notify JS that the stream has been cancelled/disposed.
                return false;
            }

            try
            {
                await receiveDataSemaphore.WaitAsync(_streamCancellationToken);
                return await ReceiveData(chunkId, chunk, error);
            }
            finally
            {
                receiveDataSemaphore.Release();
            }
        }
    }
}
