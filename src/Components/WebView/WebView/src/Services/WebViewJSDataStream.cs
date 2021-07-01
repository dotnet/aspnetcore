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
        //private readonly WebViewJSRuntime _webViewRuntime;

        public static async Task<bool> ReceiveData(WebViewJSRuntime runtime, long streamId, long chunkId, byte[] chunk, string error)
        {
            if (!runtime.JSDataStreamInstances.TryGetValue(streamId, out var instance))
            {
                // There is no data stream with the given identifier. It may have already been disposed.
                // We notify JS that the stream has been cancelled/disposed.
                return false;
            }

            return await instance.ReceiveData(chunkId, chunk, error);
        }

        internal static async ValueTask<WebViewJSDataStream> CreateWebViewJSDataStreamAsync(
            WebViewJSRuntime runtime,
            IJSStreamReference jsStreamReference,
            long totalLength,
            long maxBufferSize,
            long chunkSize,
            TimeSpan defaultCallTimeout,
            CancellationToken cancellationToken = default)
        {
            var streamId = runtime.JSDataStreamNextInstanceId++;
            var webViewJSDataStream = new WebViewJSDataStream(runtime, streamId, totalLength, maxBufferSize, defaultCallTimeout, cancellationToken);

            var dotnetObjectReferenceForDataStream = DotNetObjectReference.Create(webViewJSDataStream);
            await runtime.InvokeVoidAsync("Blazor._internal.sendJSDataStreamWebview", jsStreamReference, streamId, chunkSize, dotnetObjectReferenceForDataStream);

            return webViewJSDataStream;
        }

        private WebViewJSDataStream(
            WebViewJSRuntime runtime,
            long streamId,
            long totalLength,
            long maxBufferSize,
            TimeSpan jsInteropDefaultCallTimeout,
            CancellationToken cancellationToken) : base(runtime.JSDataStreamInstances, streamId, totalLength, maxBufferSize, jsInteropDefaultCallTimeout, cancellationToken)
        {
        }

        protected override void RaiseUnhandledException(TimeoutException timeoutException)
        {
            // TODO
            // _webviewJSRuntime.RaiseUnhandledException(timeoutException);
        }

        [JSInvokable("ReceiveData")]
        public Task<bool> ReceiveDataFromJS(long chunkId, byte[] chunk, string error)
            => ReceiveData(chunkId, chunk, error);
    }
}
