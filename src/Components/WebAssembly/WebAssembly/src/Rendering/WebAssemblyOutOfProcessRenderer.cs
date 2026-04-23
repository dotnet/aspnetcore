// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web.Infrastructure;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.WebAssembly.Rendering;

/// <summary>
/// A WebAssembly renderer that serializes render batches to a binary format
/// and passes them to JavaScript via JSImport, instead of using shared memory.
/// This decouples the C# and JS heaps, enabling future Web Worker isolation.
/// </summary>
internal sealed partial class WebAssemblyOutOfProcessRenderer : WebAssemblyRenderer
{
    public WebAssemblyOutOfProcessRenderer(
        IServiceProvider serviceProvider,
        ResourceAssetCollection resourceCollection,
        ILoggerFactory loggerFactory,
        JSComponentInterop jsComponentInterop)
        : base(serviceProvider, resourceCollection, loggerFactory, jsComponentInterop)
    {
    }

    protected override int GetWebRendererId() => (int)WebRendererId.WebAssemblyOOP;

    protected override Task UpdateDisplayAsync(in RenderBatch batch)
    {
        // Serialize the render batch using the same binary format as Server rendering.
        // Unlike the shared-memory path in WebAssemblyRenderer, this creates a self-contained
        // byte[] copy that JS can process without a heap lock.
        var arrayBuilder = new ArrayBuilder<byte>(2048);
        try
        {
            using var memoryStream = new ArrayBuilderMemoryStream(arrayBuilder);
            using (var renderBatchWriter = new RenderBatchWriter(memoryStream, leaveOpen: false))
            {
                renderBatchWriter.Write(in batch);
            }

            // Copy the used portion of the buffer. The JSImport marshaller will transfer
            // this as a Uint8Array to JS, where OutOfProcessRenderBatch can parse it.
            var batchBytes = arrayBuilder.Buffer.AsSpan(0, arrayBuilder.Count).ToArray();
            RenderBatchOOP(RendererId, batchBytes);
        }
        finally
        {
            arrayBuilder.Dispose();
        }

        if (WebAssemblyCallQueue.HasUnstartedWork)
        {
            // Ensure pending JS→.NET calls are processed before acknowledging the batch,
            // consistent with the shared-memory renderer and Blazor Server behavior.
            var tcs = new TaskCompletionSource();
            WebAssemblyCallQueue.Schedule(tcs, static tcs => tcs.SetResult());
            return tcs.Task;
        }

        return Task.CompletedTask;
    }

    [JSImport("Blazor._internal.renderBatchOOP", "blazor-internal")]
    private static partial void RenderBatchOOP(int rendererId, byte[] batchData);
}
