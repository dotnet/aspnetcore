// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.WebView;

public class WebViewRendererTests
{
    private const int MaxBufferedUnacknowledgedRenderBatches = Services.WebViewRenderer.MaxBufferedUnacknowledgedRenderBatches;

    [Fact]
    public async Task StopsProducingRenderBatches_WhenUnacknowledgedBatchLimitIsReached()
    {
        // Arrange
        var (webViewManager, component) = await CreateAttachedRerenderableComponentAsync();

        // Act - the initial render produced one batch; keep re-rendering without acknowledging any batch
        for (var i = 0; i < MaxBufferedUnacknowledgedRenderBatches * 2; i++)
        {
            await webViewManager.Dispatcher.InvokeAsync(component.TriggerRender);
        }

        // Assert
        Assert.Equal(MaxBufferedUnacknowledgedRenderBatches, CountRenderBatchMessages(webViewManager.SentIpcMessages));
    }

    [Fact]
    public async Task ResumesProducingRenderBatches_WhenABatchIsAcknowledged()
    {
        // Arrange - saturate the unacknowledged-batch queue, leaving pending renders behind
        var (webViewManager, component) = await CreateAttachedRerenderableComponentAsync();
        for (var i = 0; i < MaxBufferedUnacknowledgedRenderBatches * 2; i++)
        {
            await webViewManager.Dispatcher.InvokeAsync(component.TriggerRender);
        }

        // Act
        webViewManager.ReceiveRenderCompletedMessage(batchId: 1);

        // Assert - the renders that queued up while the limit was reached flush as a single new batch
        Assert.Equal(MaxBufferedUnacknowledgedRenderBatches + 1, CountRenderBatchMessages(webViewManager.SentIpcMessages));
    }

    [Fact]
    public async Task CompletesEverySkippedBatch_WhenAcknowledgementArrivesForALaterBatch()
    {
        // Arrange - produce batches 1 (initial render), 2 and 3
        var (webViewManager, component) = await CreateAttachedRerenderableComponentAsync();
        webViewManager.ReceiveRenderCompletedMessage(batchId: 1);
        await webViewManager.Dispatcher.InvokeAsync(component.TriggerRender);
        await webViewManager.Dispatcher.InvokeAsync(component.TriggerRender);

        // Act - acknowledge batch 3 directly, simulating batch 2's acknowledgement being lost in transport
        webViewManager.ReceiveRenderCompletedMessage(batchId: 3);
        await FlushRendererDispatcherAsync(webViewManager);

        // Assert - both outstanding batches completed, so OnAfterRender ran for every render
        Assert.Equal(3, component.AfterRenderCount);
        Assert.Equal(0, CountUnhandledExceptionMessages(webViewManager.SentIpcMessages));

        // Assert - the renderer is still fully operational afterwards
        await webViewManager.Dispatcher.InvokeAsync(component.TriggerRender);
        Assert.Equal(4, CountRenderBatchMessages(webViewManager.SentIpcMessages));
        webViewManager.ReceiveRenderCompletedMessage(batchId: 4);
        await FlushRendererDispatcherAsync(webViewManager);
        Assert.Equal(4, component.AfterRenderCount);
        Assert.Equal(0, CountUnhandledExceptionMessages(webViewManager.SentIpcMessages));
    }

    [Fact]
    public async Task IgnoresAcknowledgement_WhenItWasAlreadyReceived()
    {
        // Arrange
        var (webViewManager, component) = await CreateAttachedRerenderableComponentAsync();
        webViewManager.ReceiveRenderCompletedMessage(batchId: 1);

        // Act - a duplicate acknowledgement carries no new information
        webViewManager.ReceiveRenderCompletedMessage(batchId: 1);
        await FlushRendererDispatcherAsync(webViewManager);

        // Assert
        Assert.Equal(0, CountUnhandledExceptionMessages(webViewManager.SentIpcMessages));

        // Assert - the renderer is still fully operational afterwards
        await webViewManager.Dispatcher.InvokeAsync(component.TriggerRender);
        webViewManager.ReceiveRenderCompletedMessage(batchId: 2);
        await FlushRendererDispatcherAsync(webViewManager);
        Assert.Equal(2, component.AfterRenderCount);
        Assert.Equal(0, CountUnhandledExceptionMessages(webViewManager.SentIpcMessages));
    }

    [Fact]
    public async Task ReportsUnhandledException_WhenAcknowledgedBatchWasNeverProduced()
    {
        // Arrange
        var (webViewManager, component) = await CreateAttachedRerenderableComponentAsync();

        // Act
        webViewManager.ReceiveRenderCompletedMessage(batchId: 5);
        await FlushRendererDispatcherAsync(webViewManager);

        // Assert - every batch that was actually produced still completed before the failure was reported
        Assert.Equal(1, component.AfterRenderCount);
        Assert.Equal(1, CountUnhandledExceptionMessages(webViewManager.SentIpcMessages));
    }

    private static async Task<(TestWebViewManager webViewManager, RerenderableComponent component)> CreateAttachedRerenderableComponentAsync()
    {
        var services = new ServiceCollection().AddTestBlazorWebView().BuildServiceProvider();
        var webViewManager = new TestWebViewManager(services, new TestFileProvider());
        RerenderableComponent component = null;
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [nameof(RerenderableComponent.OnAttached)] = (Action<RerenderableComponent>)(attachedComponent => component = attachedComponent),
        });
        await webViewManager.AddRootComponentAsync(typeof(RerenderableComponent), "#app", parameters);
        webViewManager.ReceiveAttachPageMessage();
        Assert.NotNull(component);
        return (webViewManager, component);
    }

    private static int CountRenderBatchMessages(IReadOnlyList<string> messages)
        => messages.Count(m =>
            IpcCommon.TryDeserializeOutgoing(m, out var messageType, out _) &&
            messageType == IpcCommon.OutgoingMessageType.RenderBatch);

    private static int CountUnhandledExceptionMessages(IReadOnlyList<string> messages)
        => messages.Count(m =>
            IpcCommon.TryDeserializeOutgoing(m, out var messageType, out _) &&
            messageType == IpcCommon.OutgoingMessageType.NotifyUnhandledException);

    // Batch completions run their continuations (e.g. OnAfterRender) as queued work items on the
    // renderer's synchronization context, so drain it before asserting on their effects.
    private static Task FlushRendererDispatcherAsync(TestWebViewManager webViewManager)
        => webViewManager.Dispatcher.InvokeAsync(() => { });

    private class RerenderableComponent : IComponent, IHandleAfterRender
    {
        private RenderHandle _renderHandle;
        private int _renderCount;

        [Parameter] public Action<RerenderableComponent> OnAttached { get; set; }

        public int AfterRenderCount { get; private set; }

        public void Attach(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            parameters.SetParameterProperties(this);
            OnAttached?.Invoke(this);
            TriggerRender();
            return Task.CompletedTask;
        }

        public void TriggerRender()
            => _renderHandle.Render(builder => builder.AddContent(0, $"Render {++_renderCount}"));

        public Task OnAfterRenderAsync()
        {
            AfterRenderCount++;
            return Task.CompletedTask;
        }
    }
}
