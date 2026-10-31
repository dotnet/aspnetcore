// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.Components.Sections;

public class SectionRegistryTest
{
    private static readonly object SectionId = new();

    [Fact]
    public void OutletThenContent_DifferentRenderModes_LogsWarning()
    {
        var sink = new TestSink();
        var renderer = new SectionsTestRenderer(sink)
        {
            OutletRenderMode = new RenderModeA(),
            ContentRenderMode = new RenderModeB(),
        };

        Render(renderer, outletFirst: true);

        var write = Assert.Single(GetWrites(sink, "SectionRenderModeMismatch"));
        Assert.Equal(LogLevel.Warning, write.LogLevel);
        Assert.Contains("RenderModeA", write.Message);
        Assert.Contains("RenderModeB", write.Message);
    }

    [Fact]
    public void ContentThenOutlet_DifferentRenderModes_LogsWarning()
    {
        var sink = new TestSink();
        var renderer = new SectionsTestRenderer(sink)
        {
            OutletRenderMode = new RenderModeA(),
            ContentRenderMode = new RenderModeB(),
        };

        Render(renderer, outletFirst: false);

        var write = Assert.Single(GetWrites(sink, "SectionRenderModeMismatch"));
        Assert.Equal(LogLevel.Warning, write.LogLevel);
    }

    [Fact]
    public void StaticOutlet_InteractiveContent_LogsWarning()
    {
        var sink = new TestSink();
        var renderer = new SectionsTestRenderer(sink)
        {
            OutletRenderMode = null,
            ContentRenderMode = new RenderModeA(),
        };

        Render(renderer, outletFirst: true);

        var write = Assert.Single(GetWrites(sink, "SectionRenderModeMismatch"));
        Assert.Contains("static", write.Message);
    }

    [Fact]
    public void OutletAndContent_SameRenderMode_DoesNotWarn()
    {
        var sink = new TestSink();
        var renderer = new SectionsTestRenderer(sink)
        {
            OutletRenderMode = new RenderModeA(),
            ContentRenderMode = new RenderModeA(),
        };

        Render(renderer, outletFirst: true);

        Assert.Empty(GetWrites(sink, "SectionRenderModeMismatch"));
    }

    private static IReadOnlyList<WriteContext> GetWrites(TestSink sink, string eventName)
        => sink.Writes.Where(w => w.EventId.Name == eventName).ToList();

    private static void Render(
        SectionsTestRenderer renderer,
        bool outletFirst)
    {
        RenderFragment content = builder => builder.AddContent(0, "Section content");

        RenderFragment host = builder =>
        {
            void RenderOutlet(RenderTreeBuilder b)
            {
                b.OpenComponent<SectionOutlet>(0);
                b.AddComponentParameter(1, nameof(SectionOutlet.SectionId), SectionId);
                b.CloseComponent();
            }

            void RenderContent(RenderTreeBuilder b)
            {
                b.OpenComponent<SectionContent>(2);
                b.AddComponentParameter(3, nameof(SectionContent.SectionId), SectionId);
                b.AddComponentParameter(4, nameof(SectionContent.ChildContent), content);
                b.CloseComponent();
            }

            if (outletFirst)
            {
                RenderOutlet(builder);
                RenderContent(builder);
            }
            else
            {
                RenderContent(builder);
                RenderOutlet(builder);
            }
        };

        var hostComponent = new PassthroughComponent(host);
        var componentId = renderer.AssignRootComponentId(hostComponent);
        renderer.RenderRootComponent(componentId);
    }

    private sealed class RenderModeA : IComponentRenderMode;

    private sealed class RenderModeB : IComponentRenderMode;

    private sealed class PassthroughComponent(RenderFragment renderFragment) : IComponent
    {
        private RenderHandle _renderHandle;

        public void Attach(RenderHandle renderHandle)
            => _renderHandle = renderHandle;

        public Task SetParametersAsync(ParameterView parameters)
        {
            _renderHandle.Render(renderFragment);
            return Task.CompletedTask;
        }
    }

    private sealed class SectionsTestRenderer : Renderer
    {
        public SectionsTestRenderer(TestSink sink)
            : base(new ServiceCollection().BuildServiceProvider(), new TestLoggerFactory(sink, enabled: true))
        {
            Dispatcher = Dispatcher.CreateDefault();
        }

        public IComponentRenderMode OutletRenderMode { get; set; }

        public IComponentRenderMode ContentRenderMode { get; set; }

        public override Dispatcher Dispatcher { get; }

        public void RenderRootComponent(int componentId)
        {
            try
            {
                Dispatcher.InvokeAsync(() => RenderRootComponentAsync(componentId)).GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }
        }

        public new int AssignRootComponentId(IComponent component)
            => base.AssignRootComponentId(component);

        protected internal override IComponentRenderMode GetComponentRenderMode(IComponent component)
            => component switch
            {
                SectionOutlet => OutletRenderMode,
                SectionContent => ContentRenderMode,
                _ => null,
            };

        protected override void HandleException(Exception exception)
            => ExceptionDispatchInfo.Capture(exception).Throw();

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
            => Task.CompletedTask;
    }
}
