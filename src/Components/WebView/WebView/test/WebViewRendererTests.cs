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

    private class RerenderableComponent : IComponent
    {
        private RenderHandle _renderHandle;
        private int _renderCount;

        [Parameter] public Action<RerenderableComponent> OnAttached { get; set; }

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
    }
}
