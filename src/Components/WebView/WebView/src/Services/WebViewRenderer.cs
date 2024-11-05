// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.WebView.Services;

internal sealed class WebViewRenderer : WebRenderer
{
    private static readonly RendererInfo _componentPlatform = new("WebView", isInteractive: true);
    private readonly Queue<UnacknowledgedRenderBatch> _unacknowledgedRenderBatches = new();
    private readonly Dispatcher _dispatcher;
    private readonly IpcSender _ipcSender;
    private long nextRenderBatchId = 1;

    public WebViewRenderer(
        IServiceProvider serviceProvider,
        Dispatcher dispatcher,
        IpcSender ipcSender,
        ILoggerFactory loggerFactory,
        WebViewJSRuntime jsRuntime,
        JSComponentInterop jsComponentInterop) :
        base(serviceProvider, loggerFactory, jsRuntime.ReadJsonSerializerOptions(), jsComponentInterop)
    {
        _dispatcher = dispatcher;
        _ipcSender = ipcSender;

        ElementReferenceContext = jsRuntime.ElementReferenceContext;
    }

    public override Dispatcher Dispatcher => _dispatcher;

    protected override RendererInfo RendererInfo => _componentPlatform;

    protected override int GetWebRendererId() => (int)WebRendererId.WebView;

    protected override void HandleException(Exception exception)
    {
        // Notify the JS code so it can show the in-app UI
        _ipcSender.NotifyUnhandledException(exception);
    }

    protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
    {
        var batchId = nextRenderBatchId++;
        var tcs = new TaskCompletionSource();
        _unacknowledgedRenderBatches.Enqueue(new UnacknowledgedRenderBatch
        {
            BatchId = batchId,
            CompletionSource = tcs,
        });

        _ipcSender.ApplyRenderBatch(batchId, renderBatch);
        return tcs.Task;
    }

    protected override void AttachRootComponentToBrowser(int componentId, string domElementSelector)
    {
        _ipcSender.AttachToDocument(componentId, domElementSelector);
    }

    public new int AddRootComponent(Type componentType, string domElementSelector)
       => base.AddRootComponent(componentType, domElementSelector);

    public new Task RenderRootComponentAsync(int componentId, ParameterView parameters)
       => base.RenderRootComponentAsync(componentId, parameters);

    public new void RemoveRootComponent(int componentId)
       => base.RemoveRootComponent(componentId);

    public void NotifyRenderCompleted(long batchId)
    {
        var nextUnacknowledgedBatch = _unacknowledgedRenderBatches.Dequeue();
        if (nextUnacknowledgedBatch.BatchId != batchId)
        {
            throw new InvalidOperationException($"Received unexpected acknowledgement for render batch {batchId} (next batch should be {nextUnacknowledgedBatch.BatchId})");
        }

        nextUnacknowledgedBatch.CompletionSource.SetResult();
    }

    private sealed class UnacknowledgedRenderBatch
    {
        public long BatchId { get; init; }

        public TaskCompletionSource CompletionSource { get; init; }
    }
}
