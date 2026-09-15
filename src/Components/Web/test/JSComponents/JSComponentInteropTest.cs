// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Moq;

namespace Microsoft.AspNetCore.Components.Web;

public class JSComponentInteropTest
{
    [Fact]
    public async Task SetRootComponentParameters_RejectsComponentNotAddedByJavaScript()
    {
        var (renderer, interop) = CreateRenderer();
        var componentId = await renderer.Dispatcher.InvokeAsync(
            () => renderer.AddRootComponent(typeof(TestComponent), "server-rendered"));
        var parameters = JsonDocument.Parse("""{"value":"updated"}""").RootElement;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => renderer.Dispatcher.InvokeAsync(
                () => interop.SetRootComponentParameters(componentId, 1, parameters, JsonSerializerOptions.Web)));

        Assert.Equal($"Root component with ID '{componentId}' was not added by JavaScript.", exception.Message);
    }

    [Fact]
    public async Task SetRootComponentParameters_AllowsComponentAddedByJavaScript()
    {
        var (renderer, interop) = CreateRenderer();
        var componentId = await renderer.Dispatcher.InvokeAsync(
            () => interop.AddRootComponent("test-component", "js-rendered"));
        var parameters = JsonDocument.Parse("""{"value":"updated"}""").RootElement;

        await renderer.Dispatcher.InvokeAsync(
            () => interop.SetRootComponentParameters(componentId, 1, parameters, JsonSerializerOptions.Web));

        Assert.Equal("updated", renderer.GetComponent<TestComponent>(componentId).Value);
    }

    [Fact]
    public async Task RemoveRootComponent_RejectsComponentNotAddedByJavaScript()
    {
        var (renderer, interop) = CreateRenderer();
        var componentId = await renderer.Dispatcher.InvokeAsync(
            () => renderer.AddRootComponent(typeof(TestComponent), "server-rendered"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => renderer.Dispatcher.InvokeAsync(() => interop.RemoveRootComponent(componentId)));

        Assert.Equal($"Root component with ID '{componentId}' was not added by JavaScript.", exception.Message);
    }

    [Fact]
    public async Task RemoveRootComponent_AllowsComponentAddedByJavaScript()
    {
        var (renderer, interop) = CreateRenderer();
        var componentId = await renderer.Dispatcher.InvokeAsync(
            () => interop.AddRootComponent("test-component", "js-rendered"));

        await renderer.Dispatcher.InvokeAsync(() => interop.RemoveRootComponent(componentId));

        Assert.False(renderer.HasComponent(componentId));
    }

    private static (TestWebRenderer Renderer, JSComponentInterop Interop) CreateRenderer()
    {
        var configuration = new JSComponentConfigurationStore();
        configuration.Add(typeof(TestComponent), "test-component");
        var interop = new JSComponentInterop(configuration);
        var services = new ServiceCollection()
            .AddSingleton(Mock.Of<IJSRuntime>())
            .BuildServiceProvider();
        return (new TestWebRenderer(services, interop), interop);
    }

    private sealed class TestWebRenderer(IServiceProvider services, JSComponentInterop interop)
        : WebRenderer(services, NullLoggerFactory.Instance, JsonSerializerOptions.Web, interop)
    {
        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        public new int AddRootComponent(Type componentType, string selector)
            => base.AddRootComponent(componentType, selector);

        public TComponent GetComponent<TComponent>(int componentId)
            where TComponent : IComponent
            => (TComponent)GetComponentState(componentId).Component;

        public bool HasComponent(int componentId)
        {
            try
            {
                _ = GetComponentState(componentId);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        protected override void AttachRootComponentToBrowser(int componentId, string domElementSelector)
        {
        }

        protected override void HandleException(Exception exception)
            => throw exception;

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
            => Task.CompletedTask;
    }

    private sealed class TestComponent : ComponentBase
    {
        [Parameter]
        public string Value { get; set; }
    }
}
