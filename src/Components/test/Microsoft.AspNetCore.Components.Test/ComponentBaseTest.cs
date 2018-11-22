// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
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
            var service = new TestService();
            var component = new TestComponent(service);

            int onInitRuns = 0;
            service.OnInit = c => onInitRuns++;

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
            var service = new TestService();
            var component = new TestComponent(service);

            int onInitAsyncRuns = 0;
            service.RunsBaseOnInitAsync = false;
            service.OnInitAsync = c =>
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
            var service = new TestService();
            var component = new TestComponent(service);

            int onInitAsyncRuns = 0;
            service.RunsBaseOnInitAsync = true;
            service.OnInitAsync = c =>
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
            var service = new TestService();
            var component = new TestComponent(service);

            int onParametersSetRuns = 0;
            service.OnParametersSet = c => onParametersSetRuns++;

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
            var service = new TestService();
            var component = new TestComponent(service);

            int onParametersSetAsyncRuns = 0;
            service.RunsBaseOnParametersSetAsync = false;
            service.OnParametersSetAsync = c =>
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
            var service = new TestService();
            var component = new TestComponent(service);

            int onParametersSetAsyncRuns = 0;
            service.RunsBaseOnParametersSetAsync = true;
            service.OnParametersSetAsync = c =>
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
            var service = new TestService();
            var component = new TestComponent(service);

            component.Counter = 1;
            var parametersSetTask = new TaskCompletionSource<bool>();
            service.RunsBaseOnParametersSetAsync = false;
            service.OnParametersSetAsync = c => parametersSetTask.Task;

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
            var service = new TestService();
            var component = new TestComponent(service);

            component.Counter = 1;
            var initTask = new TaskCompletionSource<bool>();
            var parametersSetTask = new TaskCompletionSource<bool>();
            service.RunsBaseOnInitAsync = true;
            service.RunsBaseOnParametersSetAsync = true;
            service.OnInitAsync = c => initTask.Task;
            service.OnParametersSetAsync = c => parametersSetTask.Task;

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
            component.Counter = 3;
            initTask.SetResult(true);

            // Component should be rendered again
            Assert.Equal(3, renderer.Batches.Count);
        }

        private class TestComponent : ComponentBase
        {
            private readonly TestService _service;

            public TestComponent(TestService service)
            {
                _service = service;
            }

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
                if (_service?.RunsBaseOnInit ?? false)
                {
                    base.OnInit();
                }

                _service?.OnInit?.Invoke(this);
            }

            protected override Task OnInitAsync()
            {
                return _service?.OnInitAsync == null ? base.OnInitAsync() : OnInitServiceAsync();
            }

            protected override void OnParametersSet()
            {
                if (_service?.RunsBaseOnParametersSet ?? false)
                {
                    base.OnParametersSet();
                }

                _service?.OnParametersSet?.Invoke(this);
            }

            protected override Task OnParametersSetAsync()
            {
                return _service?.OnParametersSetAsync == null ? base.OnParametersSetAsync() : OnParametersSetServiceAsync();
            }

            private async Task OnInitServiceAsync()
            {
                if (_service.RunsBaseOnInitAsync)
                {
                    await base.OnInitAsync();
                }

                await _service.OnInitAsync(this);
            }

            private async Task OnParametersSetServiceAsync()
            {
                if (_service.RunsBaseOnParametersSetAsync)
                {
                    await base.OnParametersSetAsync();
                }

                await _service.OnParametersSetAsync(this);
            }
        }

        private class TestService
        {
            public bool RunsBaseOnInit { get; set; } = true;

            public bool RunsBaseOnInitAsync { get; set; } = true;

            public bool RunsBaseOnParametersSet { get; set; } = true;

            public bool RunsBaseOnParametersSetAsync { get; set; } = true;

            public Action<TestComponent> OnInit { get; set; }

            public Func<TestComponent, Task> OnInitAsync { get; set; }

            public Action<TestComponent> OnParametersSet { get; set; }

            public Func<TestComponent, Task> OnParametersSetAsync { get; set; }
        }
    }
}
