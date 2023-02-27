// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.HtmlRendering;

public class HtmlRendererTest
{
    [Fact]
    public async Task CanRenderSimpleComponent()
    {
        await using var htmlRenderer = CreateTestHtmlRenderer();
        var result = await htmlRenderer.RenderComponentAsync<SimpleComponent>();
        Assert.Equal($"Hello, world!", result.ToHtmlString());
    }

    [Fact]
    public async Task CanRenderSimpleComponentWithParameters()
    {
        await using var htmlRenderer = CreateTestHtmlRenderer();
        var parameters = new Dictionary<string, object> { { nameof(SimpleComponent.Name), "Bert" } };
        var result = await htmlRenderer.RenderComponentAsync<SimpleComponent>(ParameterView.FromDictionary(parameters));
        Assert.Equal($"Hello, Bert!", result.ToHtmlString());
    }

    // TODO: Loads of test cases to represent every behavior you can spot in HtmlRenderingContext (i.e., the
    // actual HTML-stringification of the render trees).
    // TODO: Test cases to specify the exact asynchrony/quiescence behaviors of RenderComponentAsync.
    // TODO: Test cases showing the exception-handling behaviors.
    // TODO: Support output to a some kind of stream or writer.

    HtmlRenderer CreateTestHtmlRenderer()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();
        return new HtmlRenderer(serviceProvider);
    }

    class SimpleComponent : IComponent // Using IComponent directly in at least some tests to show we don't rely on ComponentBase
    {
        private RenderHandle _renderHandle;

        [Parameter] public string Name { get; set; } = "world";

        public void Attach(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            parameters.SetParameterProperties(this);
            _renderHandle.Render(builder => builder.AddContent(0, $"Hello, {Name}!"));
            return Task.CompletedTask;
        }
    }
}
