// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal sealed class RemoteJSDataStream : BaseJSDataStream
    {
        private readonly RemoteJSRuntime _remoteJSRuntime;

        public static async Task<bool> ReceiveData(RemoteJSRuntime runtime, long streamId, long chunkId, byte[] chunk, string error)
        {
            if (!runtime.JSDataStreamInstances.TryGetValue(streamId, out var instance))
            {
                // There is no data stream with the given identifier. It may have already been disposed.
                // We notify JS that the stream has been cancelled/disposed.
                return false;
            }

            return await instance.ReceiveData(chunkId, chunk, error);
        }

        public static async ValueTask<RemoteJSDataStream> CreateRemoteJSDataStreamAsync(
            RemoteJSRuntime runtime,
            IJSStreamReference jsStreamReference,
            long totalLength,
            long maximumIncomingBytes,
            TimeSpan jsInteropDefaultCallTimeout,
            long pauseIncomingBytesThreshold = -1,
            long resumeIncomingBytesThreshold = -1,
            CancellationToken cancellationToken = default)
        {
            // Enforce minimum 1 kb, maximum 50 kb, SignalR message size.
            // We budget 512 bytes overhead for the transfer, thus leaving at least 512 bytes for data
            // transfer per chunk with a 1 kb message size.
            // Additionally, to maintain interactivity, we put an upper limit of 50 kb on the message size.
            var chunkSize = maximumIncomingBytes > 1024 ?
                Math.Min(maximumIncomingBytes, 50*1024) - 512 :
                throw new ArgumentException($"SignalR MaximumIncomingBytes must be at least 1 kb.");

            var streamId = runtime.JSDataStreamNextInstanceId++;
            var remoteJSDataStream = new RemoteJSDataStream(runtime, streamId, totalLength, jsInteropDefaultCallTimeout, pauseIncomingBytesThreshold, resumeIncomingBytesThreshold, cancellationToken);
            await runtime.InvokeVoidAsync("Blazor._internal.sendJSDataStream", jsStreamReference, streamId, chunkSize);
            return remoteJSDataStream;
        }

        private RemoteJSDataStream(
            RemoteJSRuntime runtime,
            long streamId,
            long totalLength,
            TimeSpan jsInteropDefaultCallTimeout,
            long pauseIncomingBytesThreshold,
            long resumeIncomingBytesThreshold,
            CancellationToken cancellationToken) :
            base(
                runtime.JSDataStreamInstances,
                streamId,
                totalLength,
                jsInteropDefaultCallTimeout,
                pauseIncomingBytesThreshold,
                resumeIncomingBytesThreshold,
                cancellationToken)
        {
            _remoteJSRuntime = runtime;
        }

        protected override void RaiseUnhandledException(Exception exception)
        {
            _remoteJSRuntime.RaiseUnhandledException(exception);
        }
    }
}
