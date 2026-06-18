// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.Components.Tests.Rendering;

public class ComponentStatePrerenderTests
{
    [Fact]
    public void RenderIntoBatch_WhenPrerenderDisabled_DoesNotExecuteRenderFragment()
    {
        var renderer = CreateRenderer();
        var componentState = CreateComponentState(renderer, prerender: false);

        var batchBuilder = new RenderBatchBuilder();
        var fragmentExecuted = false;

        componentState.RenderIntoBatch(
            batchBuilder,
            builder => fragmentExecuted = true,
            out var renderFragmentException);

        Assert.False(fragmentExecuted);
        Assert.Null(renderFragmentException);
    }

    [Fact]
    public void RenderIntoBatch_WhenPrerenderEnabled_ExecutesRenderFragment()
    {
        var renderer = CreateRenderer();
        var componentState = CreateComponentState(renderer, prerender: true);

        var batchBuilder = new RenderBatchBuilder();
        var fragmentExecuted = false;

        componentState.RenderIntoBatch(
            batchBuilder,
            builder =>
            {
                fragmentExecuted = true;
                throw new InvalidOperationException("Render fragment executed");
            },
            out var renderFragmentException);

        Assert.True(fragmentExecuted);
        Assert.NotNull(renderFragmentException);
        Assert.IsType<InvalidOperationException>(renderFragmentException);
    }

    [Fact]
    public void AutoRenderMode_ParentPrerenderTrue_ChildPrerenderFalse_SkipsChildRender()
    {
        AssertChildPrerenderSkipped(
            new AutoRenderMode(prerender: true),
            new AutoRenderMode(prerender: false));
    }

    [Fact]
    public void ServerRenderMode_ParentPrerenderTrue_ChildPrerenderFalse_SkipsChildRender()
    {
        AssertChildPrerenderSkipped(
            new ServerRenderMode(prerender: true),
            new ServerRenderMode(prerender: false));
    }

    [Fact]
    public void WebAssemblyRenderMode_ParentPrerenderTrue_ChildPrerenderFalse_SkipsChildRender()
    {
        AssertChildPrerenderSkipped(
            new WebAssemblyRenderMode(prerender: true),
            new WebAssemblyRenderMode(prerender: false));
    }

    [Fact]
    public void ChildWithoutRenderMode_InheritsParentPrerenderTrue()
    {
        var renderer = CreateRenderer();
        var parentState = CreateComponentState(renderer, prerender: true);
        var childState = new ComponentState(renderer, componentId: 1, new TestComponent(), parentState);

        var batchBuilder = new RenderBatchBuilder();
        var childFragmentExecuted = false;
        childState.RenderIntoBatch(
            batchBuilder,
            builder =>
            {
                childFragmentExecuted = true;
                throw new InvalidOperationException();
            },
            out var renderFragmentException);

        Assert.True(childFragmentExecuted);
        Assert.NotNull(renderFragmentException);
    }

    private void AssertChildPrerenderSkipped(
        IComponentRenderMode parentMode,
        IComponentRenderMode childMode)
    {
        var renderer = CreateRenderer();
        var parentState = new ComponentState(
            renderer,
            componentId: 0,
            new TestComponent(),
            parentComponentState: null)
        {
            RenderMode = parentMode
        };

        var childState = new ComponentState(renderer, componentId: 1, new TestComponent(), parentState)
        {
            RenderMode = childMode
        };

        var batchBuilder = new RenderBatchBuilder();
        var childFragmentExecuted = false;

        childState.RenderIntoBatch(
            batchBuilder,
            builder => childFragmentExecuted = true,
            out var renderFragmentException);

        Assert.False(childFragmentExecuted);
        Assert.Null(renderFragmentException);
    }

    private static ComponentState CreateComponentState(Renderer renderer, bool prerender)
    {
        var component = new TestComponent();
        var state = new ComponentState(renderer, componentId: 0, component, parentComponentState: null);

        state.RenderMode = new TestRenderMode(prerender);
        return state;
    }

    private static Renderer CreateRenderer()
    {
        var services = new ServiceCollection();
        return new TestRenderer(services.BuildServiceProvider());
    }
    private sealed class TestComponent : IComponent
    {
        public void Attach(RenderHandle renderHandle) { }
        public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
    }

    private sealed class TestRenderMode : IComponentRenderMode, IPrerenderMode
    {
        public bool Prerender { get; }
        public TestRenderMode(bool prerender) => Prerender = prerender;
    }

    private sealed class AutoRenderMode : IComponentRenderMode, IPrerenderMode
    {
        public bool Prerender { get; }
        public AutoRenderMode(bool prerender) => Prerender = prerender;
    }

    private sealed class ServerRenderMode : IComponentRenderMode, IPrerenderMode
    {
        public bool Prerender { get; }
        public ServerRenderMode(bool prerender) => Prerender = prerender;
    }

    private sealed class WebAssemblyRenderMode : IComponentRenderMode, IPrerenderMode
    {
        public bool Prerender { get; }
        public WebAssemblyRenderMode(bool prerender) => Prerender = prerender;
    }

    private sealed class TestRenderer : Renderer
    {
        public TestRenderer(IServiceProvider services)
            : base(services, NullLoggerFactory.Instance)
        {
        }

        public override Dispatcher Dispatcher
            => throw new NotSupportedException();

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
            => Task.CompletedTask;

        protected override void HandleException(Exception exception)
            => throw exception;
    }
}
