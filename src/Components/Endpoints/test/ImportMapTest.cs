// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class ImportMapTest
{
    private readonly ImportMap _importMap;
    private readonly TestRenderer _renderer;

    public ImportMapTest()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        var serviceProvider = services.BuildServiceProvider();

        _renderer = new TestRenderer(serviceProvider)
        {
            ShouldHandleExceptions = true
        };
        _importMap = (ImportMap)_renderer.InstantiateComponent<ImportMap>();
    }

    [Fact]
    public async Task CanRenderImportMap()
    {
        // Arrange
        var importMap = new ImportMap();
        var importMapDefinition = new ImportMapDefinition(
            new Dictionary<string, string>
            {
                { "jquery", "https://code.jquery.com/jquery-3.5.1.min.js" },
                { "bootstrap", "https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.min.js" }
            },
            new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["development"] = new Dictionary<string, string>
                {
                    { "jquery", "https://code.jquery.com/jquery-3.5.1.js" },
                    { "bootstrap", "https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.js" }
                }.AsReadOnly()
            },
            new Dictionary<string, string>
            {
                { "https://code.jquery.com/jquery-3.5.1.js", "sha384-jquery" },
                { "https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.js", "sha256-bootstrap" }
            });

        importMap.ImportMapDefinition = importMapDefinition;
        importMap.AdditionalAttributes = new Dictionary<string, object> { ["nonce"] = "random" }.AsReadOnly();

        var id = _renderer.AssignRootComponentId(importMap);
        // Act
        await _renderer.Dispatcher.InvokeAsync(() => _renderer.RenderRootComponent(id));

        // Assert
        var frames = _renderer.GetCurrentRenderTreeFrames(id);
        Assert.Equal(4, frames.Count);
        Assert.Equal(RenderTreeFrameType.Element, frames.Array[0].FrameType);
        Assert.Equal("script", frames.Array[0].ElementName);
        Assert.Equal(RenderTreeFrameType.Attribute, frames.Array[1].FrameType);
        Assert.Equal("type", frames.Array[1].AttributeName);
        Assert.Equal("importmap", frames.Array[1].AttributeValue);
        Assert.Equal("nonce", frames.Array[2].AttributeName);
        Assert.Equal("random", frames.Array[2].AttributeValue);
        Assert.Equal(RenderTreeFrameType.Markup, frames.Array[3].FrameType);
        Assert.Equal(importMapDefinition.ToJson(), frames.Array[3].TextContent);
    }

    [Fact]
    public async Task ResolvesImportMap_FromHttpContext()
    {
        // Arrange
        var importMap = new ImportMap();
        var importMapDefinition = new ImportMapDefinition(
            new Dictionary<string, string>
            {
                { "jquery", "https://code.jquery.com/jquery-3.5.1.min.js" },
                { "bootstrap", "https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.min.js" }
            },
            new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["development"] = new Dictionary<string, string>
                {
                    { "jquery", "https://code.jquery.com/jquery-3.5.1.js" },
                    { "bootstrap", "https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.js" }
                }.AsReadOnly()
            },
            new Dictionary<string, string>
            {
                { "https://code.jquery.com/jquery-3.5.1.js", "sha384-jquery" },
                { "https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.js", "sha256-bootstrap" }
            });

        var id = _renderer.AssignRootComponentId(importMap);
        var context = new DefaultHttpContext();
        context.SetEndpoint(new Endpoint((ctx) => Task.CompletedTask, new EndpointMetadataCollection(importMapDefinition), "Test"));
        importMap.HttpContext = context;

        // Act
        await _renderer.Dispatcher.InvokeAsync(() => _renderer.RenderRootComponent(id));

        // Assert
        var frames = _renderer.GetCurrentRenderTreeFrames(id);
        Assert.Equal(3, frames.Count);
        Assert.Equal(RenderTreeFrameType.Element, frames.Array[0].FrameType);
        Assert.Equal("script", frames.Array[0].ElementName);
        Assert.Equal(RenderTreeFrameType.Attribute, frames.Array[1].FrameType);
        Assert.Equal("type", frames.Array[1].AttributeName);
        Assert.Equal("importmap", frames.Array[1].AttributeValue);
        Assert.Equal(RenderTreeFrameType.Markup, frames.Array[2].FrameType);
        Assert.Equal(importMapDefinition.ToJson(), frames.Array[2].TextContent);
    }

    [Fact]
    public async Task Rerenders_WhenImportmapChanges()
    {
        // Arrange
        var importMap = new ImportMap();
        var importMapDefinition = new ImportMapDefinition(
            new Dictionary<string, string>
            {
                { "jquery", "https://code.jquery.com/jquery-3.5.1.min.js" },
                { "bootstrap", "https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.min.js" }
            },
            new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["development"] = new Dictionary<string, string>
                {
                    { "jquery", "https://code.jquery.com/jquery-3.5.1.js" },
                    { "bootstrap", "https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.js" }
                }.AsReadOnly()
            },
            new Dictionary<string, string>
            {
                { "https://code.jquery.com/jquery-3.5.1.js", "sha384-jquery" },
                { "https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.js", "sha256-bootstrap" }
            });

        var otherImportMapDefinition = new ImportMapDefinition(
            new Dictionary<string, string>
            {
                { "jquery", "./jquery-3.5.1.js" },
                { "bootstrap", "./bootstrap/4.5.2/js/bootstrap.min.js" }
            },
            ReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>.Empty,
            ReadOnlyDictionary<string, string>.Empty);

        var id = _renderer.AssignRootComponentId(importMap);
        var context = new DefaultHttpContext();
        context.SetEndpoint(new Endpoint((ctx) => Task.CompletedTask, new EndpointMetadataCollection(importMapDefinition), "Test"));
        importMap.HttpContext = context;

        // Act
        await _renderer.Dispatcher.InvokeAsync(() => _renderer.RenderRootComponent(id));

        var component = importMap as IComponent;
        await _renderer.Dispatcher.InvokeAsync(async () =>
        {
            await component.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(ImportMap.ImportMapDefinition), otherImportMapDefinition }
            }));
        });

        await _renderer.Dispatcher.InvokeAsync(_renderer.ProcessPendingRender);

        // Assert
        var frames = _renderer.GetCurrentRenderTreeFrames(id);
        Assert.Equal(3, frames.Count);
        Assert.Equal(RenderTreeFrameType.Element, frames.Array[0].FrameType);
        Assert.Equal("script", frames.Array[0].ElementName);
        Assert.Equal(RenderTreeFrameType.Attribute, frames.Array[1].FrameType);
        Assert.Equal("type", frames.Array[1].AttributeName);
        Assert.Equal("importmap", frames.Array[1].AttributeValue);
        Assert.Equal(RenderTreeFrameType.Markup, frames.Array[2].FrameType);
        Assert.Equal(otherImportMapDefinition.ToJson(), frames.Array[2].TextContent);
    }

    [Fact]
    public async Task DoesNot_Rerender_WhenImportmap_DoesNotChange()
    {
        // Arrange
        var importMap = new ImportMap();
        var importMapDefinition = new ImportMapDefinition(
            new Dictionary<string, string>
            {
                { "jquery", "https://code.jquery.com/jquery-3.5.1.min.js" },
                { "bootstrap", "https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.min.js" }
            },
            new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["development"] = new Dictionary<string, string>
                {
                    { "jquery", "https://code.jquery.com/jquery-3.5.1.js" },
                    { "bootstrap", "https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.js" }
                }.AsReadOnly()
            },
            new Dictionary<string, string>
            {
                { "https://code.jquery.com/jquery-3.5.1.js", "sha384-jquery" },
                { "https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.js", "sha256-bootstrap" }
            });

        var id = _renderer.AssignRootComponentId(importMap);
        var context = new DefaultHttpContext();
        context.SetEndpoint(new Endpoint((ctx) => Task.CompletedTask, new EndpointMetadataCollection(importMapDefinition), "Test"));
        importMap.HttpContext = context;

        // Act
        await _renderer.Dispatcher.InvokeAsync(() => _renderer.RenderRootComponent(id));

        var component = importMap as IComponent;
        await _renderer.Dispatcher.InvokeAsync(async () =>
        {
            await component.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(ImportMap.ImportMapDefinition), importMapDefinition }
            }));
        });

        await _renderer.Dispatcher.InvokeAsync(_renderer.ProcessPendingRender);

        // Assert
        Assert.Equal(1, _renderer.CapturedBatch.UpdatedComponents.Count);
        Assert.Equal(0, _renderer.CapturedBatch.UpdatedComponents.Array[0].Edits.Count);

        var frames = _renderer.GetCurrentRenderTreeFrames(id);
        Assert.Equal(3, frames.Count);
        Assert.Equal(RenderTreeFrameType.Element, frames.Array[0].FrameType);
        Assert.Equal("script", frames.Array[0].ElementName);
        Assert.Equal(RenderTreeFrameType.Attribute, frames.Array[1].FrameType);
        Assert.Equal("type", frames.Array[1].AttributeName);
        Assert.Equal("importmap", frames.Array[1].AttributeValue);
        Assert.Equal(RenderTreeFrameType.Markup, frames.Array[2].FrameType);
        Assert.Equal(importMapDefinition.ToJson(), frames.Array[2].TextContent);
    }

    public class TestRenderer : Renderer
    {
        public TestRenderer(IServiceProvider serviceProvider) : base(serviceProvider, NullLoggerFactory.Instance)
        {
            Dispatcher = Dispatcher.CreateDefault();
        }

        public TestRenderer(IServiceProvider serviceProvider, IComponentActivator componentActivator)
            : base(serviceProvider, NullLoggerFactory.Instance, componentActivator)
        {
            Dispatcher = Dispatcher.CreateDefault();
        }

        public override Dispatcher Dispatcher { get; }

        public Action OnExceptionHandled { get; set; }

        public Action<RenderBatch> OnUpdateDisplay { get; set; }

        public Action OnUpdateDisplayComplete { get; set; }

        public List<Exception> HandledExceptions { get; } = new List<Exception>();

        public bool ShouldHandleExceptions { get; set; }

        public Task NextRenderResultTask { get; set; } = Task.CompletedTask;

        public RenderBatch CapturedBatch { get; set; }

        private HashSet<TestRendererComponentState> UndisposedComponentStates { get; } = new();

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
        {
            CapturedBatch = renderBatch;
            return Task.CompletedTask;
        }

        public new int AssignRootComponentId(IComponent component)
            => base.AssignRootComponentId(component);

        public new void RemoveRootComponent(int componentId)
            => base.RemoveRootComponent(componentId);

        public new ArrayRange<RenderTreeFrame> GetCurrentRenderTreeFrames(int componentId)
            => base.GetCurrentRenderTreeFrames(componentId);

        public void RenderRootComponent(int componentId, ParameterView? parameters = default)
        {
            var task = Dispatcher.InvokeAsync(() => base.RenderRootComponentAsync(componentId, parameters ?? ParameterView.Empty));
            UnwrapTask(task);
        }

        public new Task RenderRootComponentAsync(int componentId)
            => Dispatcher.InvokeAsync(() => base.RenderRootComponentAsync(componentId));

        public new Task RenderRootComponentAsync(int componentId, ParameterView parameters)
            => Dispatcher.InvokeAsync(() => base.RenderRootComponentAsync(componentId, parameters));

        public Task DispatchEventAsync(ulong eventHandlerId, EventArgs args)
            => Dispatcher.InvokeAsync(() => base.DispatchEventAsync(eventHandlerId, null, args));

        public new Task DispatchEventAsync(ulong eventHandlerId, EventFieldInfo eventFieldInfo, EventArgs args)
            => Dispatcher.InvokeAsync(() => base.DispatchEventAsync(eventHandlerId, eventFieldInfo, args));

        private static Task UnwrapTask(Task task)
        {
            // This should always be run synchronously
            Assert.True(task.IsCompleted);
            if (task.IsFaulted)
            {
                var exception = task.Exception.Flatten().InnerException;
                while (exception is AggregateException e)
                {
                    exception = e.InnerException;
                }

                ExceptionDispatchInfo.Capture(exception).Throw();
            }

            return task;
        }

        public IComponent InstantiateComponent<T>()
            => InstantiateComponent(typeof(T));

        protected override void HandleException(Exception exception)
        {
            if (!ShouldHandleExceptions)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }

            HandledExceptions.Add(exception);
            OnExceptionHandled?.Invoke();
        }

        public new void ProcessPendingRender()
            => base.ProcessPendingRender();

        protected override ComponentState CreateComponentState(int componentId, IComponent component, ComponentState parentComponentState)
            => new TestRendererComponentState(this, componentId, component, parentComponentState);

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (UndisposedComponentStates.Count > 0)
            {
                throw new InvalidOperationException("Did not dispose all the ComponentState instances. This could lead to ArrayBuffer not returning buffers to its pool.");
            }
        }

        class TestRendererComponentState : ComponentState, IAsyncDisposable
        {
            private readonly TestRenderer _renderer;

            public TestRendererComponentState(Renderer renderer, int componentId, IComponent component, ComponentState parentComponentState)
                : base(renderer, componentId, component, parentComponentState)
            {
                _renderer = (TestRenderer)renderer;
                _renderer.UndisposedComponentStates.Add(this);
            }

            public override ValueTask DisposeAsync()
            {
                _renderer.UndisposedComponentStates.Remove(this);
                return base.DisposeAsync();
            }
        }
    }
}
