// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Xunit;

namespace Microsoft.AspNetCore.Components.Test
{
    public class ComponentBaseTest
    {
        [Fact]
        public void RunsOnInitWhenRendered()
        {
            // Arrange
            var renderer = new TestRenderer();
            var component = new TestComponent();

            int onInitRuns = 0;
            component.OnInitLogic = c => onInitRuns++;

            // Act
            var componentId = renderer.AssignRootComponentId(component);
            renderer.RenderRootComponent(componentId);

            // Assert
            Assert.Equal(1, onInitRuns);
        }

        [Fact]
        public void RunsOnInitAsyncWhenRendered()
        {
            // Arrange
            var renderer = new TestRenderer();
            var component = new TestComponent();

            int onInitAsyncRuns = 0;
            component.RunsBaseOnInitAsync = false;
            component.OnInitAsyncLogic = c =>
            {
                onInitAsyncRuns++;
                return Task.CompletedTask;
            };

            // Act
            var componentId = renderer.AssignRootComponentId(component);
            renderer.RenderRootComponent(componentId);

            // Assert
            Assert.Equal(1, onInitAsyncRuns);
            Assert.Single(renderer.Batches);
        }

        [Fact]
        public void RunsOnInitAsyncAlsoOnBaseClassWhenRendered()
        {
            // Arrange
            var renderer = new TestRenderer();
            var component = new TestComponent();

            int onInitAsyncRuns = 0;
            component.RunsBaseOnInitAsync = true;
            component.OnInitAsyncLogic = c =>
            {
                onInitAsyncRuns++;
                return Task.CompletedTask;
            };

            // Act
            var componentId = renderer.AssignRootComponentId(component);
            renderer.RenderRootComponent(componentId);

            // Assert
            Assert.Equal(1, onInitAsyncRuns);
            Assert.Single(renderer.Batches);
        }

        [Fact]
        public void RunsOnParametersSetWhenRendered()
        {
            // Arrange
            var renderer = new TestRenderer();
            var component = new TestComponent();

            int onParametersSetRuns = 0;
            component.OnParametersSetLogic = c => onParametersSetRuns++;

            // Act
            var componentId = renderer.AssignRootComponentId(component);
            renderer.RenderRootComponent(componentId);

            // Assert
            Assert.Equal(1, onParametersSetRuns);
            Assert.Single(renderer.Batches);
        }

        [Fact]
        public void RunsOnParametersSetAsyncWhenRendered()
        {
            // Arrange
            var renderer = new TestRenderer();
            var component = new TestComponent();

            int onParametersSetAsyncRuns = 0;
            component.RunsBaseOnParametersSetAsync = false;
            component.OnParametersSetAsyncLogic = c =>
            {
                onParametersSetAsyncRuns++;
                return Task.CompletedTask;
            };

            // Act
            var componentId = renderer.AssignRootComponentId(component);
            renderer.RenderRootComponent(componentId);

            // Assert
            Assert.Equal(1, onParametersSetAsyncRuns);
            Assert.Single(renderer.Batches);
        }

        [Fact]
        public void RunsOnParametersSetAsyncAlsoOnBaseClassWhenRendered()
        {
            // Arrange
            var renderer = new TestRenderer();
            var component = new TestComponent();

            int onParametersSetAsyncRuns = 0;
            component.RunsBaseOnParametersSetAsync = true;
            component.OnParametersSetAsyncLogic = c =>
            {
                onParametersSetAsyncRuns++;
                return Task.CompletedTask;
            };

            // Act
            var componentId = renderer.AssignRootComponentId(component);
            renderer.RenderRootComponent(componentId);

            // Assert
            Assert.Equal(1, onParametersSetAsyncRuns);
            Assert.Single(renderer.Batches);
        }

        [Fact]
        public void RendersAfterParametersSetAsyncTaskIsCompleted()
        {
            // Arrange
            var renderer = new TestRenderer();
            var component = new TestComponent();

            component.Counter = 1;
            var parametersSetTask = new TaskCompletionSource<bool>();
            component.RunsBaseOnParametersSetAsync = false;
            component.OnParametersSetAsyncLogic = c => parametersSetTask.Task;

            // Act
            var componentId = renderer.AssignRootComponentId(component);
            renderer.RenderRootComponent(componentId);

            // Assert
            Assert.Single(renderer.Batches);

            // Completes task started by OnParametersSetAsync
            component.Counter = 2;
            parametersSetTask.SetResult(true);

            // Component should be rendered again
            Assert.Equal(2, renderer.Batches.Count);
        }

        [Fact]
        public void RendersAfterParametersSetAndInitAsyncTasksAreCompleted()
        {
            // Arrange
            var renderer = new TestRenderer();
            var component = new TestComponent();

            component.Counter = 1;
            var initTask = new TaskCompletionSource<bool>();
            var parametersSetTask = new TaskCompletionSource<bool>();
            component.RunsBaseOnInitAsync = true;
            component.RunsBaseOnParametersSetAsync = true;
            component.OnInitAsyncLogic = c => initTask.Task;
            component.OnParametersSetAsyncLogic = c => parametersSetTask.Task;

            // Act
            var componentId = renderer.AssignRootComponentId(component);
            renderer.RenderRootComponent(componentId);

            // Assert
            Assert.Single(renderer.Batches);

            // Completes task started by OnParametersSetAsync
            component.Counter = 2;
            parametersSetTask.SetResult(false);

            // Component should be rendered again
            Assert.Equal(2, renderer.Batches.Count);

            // Completes task started by OnInitAsync
            // NOTE: We will probably change this behavior. It would make more sense for the base class
            // to wait until InitAsync is completed before proceeding with SetParametersAsync, rather
            // that running the two lifecycle methods in parallel. This will come up as a requirement
            // when implementing async server-side prerendering.
            component.Counter = 3;
            initTask.SetResult(true);

            // Component should be rendered again
            Assert.Equal(3, renderer.Batches.Count);
        }

        [Fact]
        public void DoesNotRenderAfterOnInitAsyncTaskIsCancelled()
        {
            // Arrange
            var renderer = new TestRenderer();
            var component = new TestComponent() { Counter = 1 };
            var initTask = new TaskCompletionSource<object>();
            component.OnInitAsyncLogic = _ => initTask.Task;

            // Act
            var componentId = renderer.AssignRootComponentId(component);
            renderer.RenderRootComponent(componentId);

            // Assert
            Assert.Single(renderer.Batches);

            // Cancel task started by OnInitAsync
            component.Counter = 2;
            initTask.SetCanceled();

            // Component should not be rendered again
            Assert.Single(renderer.Batches);
        }

        [Fact]
        public void DoesNotRenderAfterOnParametersSetAsyncTaskIsCancelled()
        {
            // Arrange
            var renderer = new TestRenderer();
            var component = new TestComponent() { Counter = 1 };
            var onParametersSetTask = new TaskCompletionSource<object>();
            component.OnParametersSetAsyncLogic = _ => onParametersSetTask.Task;

            // Act
            var componentId = renderer.AssignRootComponentId(component);
            renderer.RenderRootComponent(componentId);

            // Assert
            Assert.Single(renderer.Batches);

            // Cancel task started by OnParametersSet
            component.Counter = 2;
            onParametersSetTask.SetCanceled();

            // Component should not be rendered again
            Assert.Single(renderer.Batches);
        }

        private class TestComponent : ComponentBase
        {
            public bool RunsBaseOnInit { get; set; } = true;

            public bool RunsBaseOnInitAsync { get; set; } = true;

            public bool RunsBaseOnParametersSet { get; set; } = true;

            public bool RunsBaseOnParametersSetAsync { get; set; } = true;

            public Action<TestComponent> OnInitLogic { get; set; }

            public Func<TestComponent, Task> OnInitAsyncLogic { get; set; }

            public Action<TestComponent> OnParametersSetLogic { get; set; }

            public Func<TestComponent, Task> OnParametersSetAsyncLogic { get; set; }

            public int Counter { get; set; }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                base.BuildRenderTree(builder);
                builder.OpenElement(0, "p");
                builder.AddContent(1, Counter);
                builder.CloseElement();
            }

            protected override void OnInit()
            {
                if (RunsBaseOnInit)
                {
                    base.OnInit();
                }

                OnInitLogic?.Invoke(this);
            }

            protected override async Task OnInitAsync()
            {
                if (RunsBaseOnInitAsync)
                {
                    await base.OnInitAsync();
                }

                await OnInitAsyncLogic?.Invoke(this);
            }

            protected override void OnParametersSet()
            {
                if (RunsBaseOnParametersSet)
                {
                    base.OnParametersSet();
                }

                OnParametersSetLogic?.Invoke(this);
            }

            protected override async Task OnParametersSetAsync()
            {
                if (RunsBaseOnParametersSetAsync)
                {
                    await base.OnParametersSetAsync();
                }

                await OnParametersSetAsyncLogic?.Invoke(this);
            }
        }
    }
}
