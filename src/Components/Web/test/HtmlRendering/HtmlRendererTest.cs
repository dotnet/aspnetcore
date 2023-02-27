// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.HtmlRendering;

public class HtmlRendererTest
{
    [Fact]
    public async Task CanRenderSimpleComponentAsHtml()
    {
        await using var htmlRenderer = CreateTestHtmlRenderer();
        var result = await htmlRenderer.RenderComponentAsync<SimpleComponent>();
        Assert.Equal($"Hello from {nameof(SimpleComponent)}", result.ToHtmlString());
    }

    // TODO: Support passing parameters.
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

    class SimpleComponent : IComponent
    {
        public void Attach(RenderHandle renderHandle) => renderHandle.Render(builder =>
        {
            builder.AddContent(0, $"Hello from {nameof(SimpleComponent)}");
        });

        public Task SetParametersAsync(ParameterView parameters)
            => Task.CompletedTask;
    }
}
