// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components.Performance;

public class HtmlRendererBenchmark
{
    private HtmlRenderer _renderer;
    private Web.HtmlRendering.HtmlRootComponent _rendered;
    private StringWriter _writer;

    [GlobalSetup]
    public void Setup()
    {
        _renderer = new HtmlRenderer(new EmptyServiceProvider(), NullLoggerFactory.Instance);
        _writer = new StringWriter(new System.Text.StringBuilder(256 * 1024));
        _rendered = _renderer.Dispatcher.InvokeAsync(() => _renderer.RenderComponentAsync<TablePage>()).GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup() => _renderer.Dispose();

    [Benchmark]
    public int RenderAndWriteHtml()
    {
        // A new renderer per operation, like EndpointHtmlRenderer per request
        using var renderer = new HtmlRenderer(new EmptyServiceProvider(), NullLoggerFactory.Instance);
        return renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<TablePage>();
            _writer.GetStringBuilder().Clear();
            output.WriteHtmlTo(_writer);
            return _writer.GetStringBuilder().Length;
        }).GetAwaiter().GetResult();
    }

    [Benchmark]
    public int WriteHtmlOnly()
    {
        return _renderer.Dispatcher.InvokeAsync(() =>
        {
            _writer.GetStringBuilder().Clear();
            _rendered.WriteHtmlTo(_writer);
            return _writer.GetStringBuilder().Length;
        }).GetAwaiter().GetResult();
    }

    [Benchmark]
    public Task WriteHtmlToBufferedWriter()
    {
        return _renderer.Dispatcher.InvokeAsync(async () =>
        {
            using var writer = new Microsoft.AspNetCore.Components.Endpoints.Rendering.BufferedTextWriter(TextWriter.Null);
            _rendered.WriteHtmlTo(writer);
            await writer.FlushAsync();
        });
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object GetService(Type serviceType) => null;
    }

    private sealed class TablePage : IComponent
    {
        private RenderHandle _renderHandle;

        public void Attach(RenderHandle renderHandle) => _renderHandle = renderHandle;

        public Task SetParametersAsync(ParameterView parameters)
        {
            _renderHandle.Render(BuildRenderTree);
            return Task.CompletedTask;
        }

        private static void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "table");
            builder.AddAttribute(1, "class", "table table-striped");
            builder.OpenElement(2, "tbody");
            for (var i = 0; i < 100; i++)
            {
                builder.OpenComponent<Row>(3);
                builder.AddComponentParameter(4, nameof(Row.Index), i);
                builder.AddComponentParameter(5, nameof(Row.Name), "Customer's name #" + i);
                builder.AddComponentParameter(6, nameof(Row.Selected), i % 7 == 0);
                builder.CloseComponent();
            }
            builder.CloseElement();
            builder.CloseElement();
        }
    }

    private sealed class Row : ComponentBase
    {
        [Parameter] public int Index { get; set; }
        [Parameter] public string Name { get; set; }
        [Parameter] public bool Selected { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "tr");
            builder.AddAttribute(1, "class", Selected ? "row selected" : "row");
            builder.AddAttribute(2, "data-index", Index);
            builder.OpenElement(3, "td");
            builder.AddContent(4, Index);
            builder.CloseElement();
            builder.OpenElement(5, "td");
            builder.AddContent(6, Name);
            builder.CloseElement();
            builder.OpenElement(7, "td");
            builder.OpenElement(8, "input");
            builder.AddAttribute(9, "type", "checkbox");
            builder.AddAttribute(10, "checked", Selected);
            builder.CloseElement();
            builder.OpenElement(11, "a");
            builder.AddAttribute(12, "href", "/customers/" + Index);
            builder.AddContent(13, "Details");
            builder.CloseElement();
            builder.CloseElement();
            builder.CloseElement();
        }
    }
}
