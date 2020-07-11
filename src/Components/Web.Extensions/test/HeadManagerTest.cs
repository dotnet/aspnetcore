// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.JSInterop;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    public class HeadManagerTest
    {
        [Fact]
        public async void HeadElementChain_StateRevertsWhenLastChangeDiscarded()
        {
            var initialState = "initial state";
            var headElementChain = new HeadElementChain(initialState);

            var element1 = new TestHeadElement();
            var element2 = new TestHeadElement();

            // Add elements to chain
            await headElementChain.ApplyChangeAsync(element1);
            await headElementChain.ApplyChangeAsync(element2);

            // Verify initial number of times applied
            Assert.Equal(1, element1.NumTimesApplied);
            Assert.Equal(1, element2.NumTimesApplied);

            // Discard most recent element
            await headElementChain.DiscardChangeAsync(element2);

            // Verify that only initial element was applied again
            Assert.Equal(2, element1.NumTimesApplied);
            Assert.Equal(1, element2.NumTimesApplied);

            // Discard the last element
            await headElementChain.DiscardChangeAsync(element1);

            // Verify that the initial state was preserved
            Assert.Equal(initialState, element1.StateAfterReset);
        }

        [Fact]
        public async void HeadElementChain_StateDoesNotUpdateWhenNonLastChangeDiscarded()
        {
            var headElementChain = new HeadElementChain(null);

            var element1 = new TestHeadElement();
            var element2 = new TestHeadElement();

            // Add elements to chain
            await headElementChain.ApplyChangeAsync(element1);
            await headElementChain.ApplyChangeAsync(element2);

            // Discard first element
            await headElementChain.DiscardChangeAsync(element1);

            // Verify that each element was only applied once
            Assert.Equal(1, element1.NumTimesApplied);
            Assert.Equal(1, element2.NumTimesApplied);
        }

        [Fact]
        public async void HeadManager_ChangesAppliedInRequestedOrder()
        {
            var headManager = new HeadManager(Mock.Of<IJSRuntime>());
            var numElements = 5;
            var maxDelay = 1000;

            var completedOrder = new ConcurrentQueue<int>();
            var blockingTaskSource = new TaskCompletionSource();

            var elements = Enumerable.Range(0, numElements)
                .Select(i => new TestHeadElement
                {
                    Key = i,
                    OnApplied = async () =>
                    {
                        // This delay would encourage changes to be applied in reverse order in a non-blocking handler
                        var delay = maxDelay - i * (maxDelay / numElements);

                        await Task.Delay(delay);

                        completedOrder.Enqueue(i);
                    }
                })
                .ToList();

            // Request changes in order of increasing element key
            foreach (var element in elements)
            {
                headManager.NotifyChanged(element);
            }

            // Add one more change that, when handled, unblocks this test
            headManager.NotifyChanged(new TestHeadElement
            {
                OnApplied = () =>
                {
                    blockingTaskSource.SetResult();
                    return Task.CompletedTask;
                }
            });

            // Wait for the head manager to finish applying all changes
            await blockingTaskSource.Task;

            // Verify that all changes have been applied
            Assert.Equal(numElements, completedOrder.Count);

            // Verify that all changes were applied in the correct order
            Assert.All(elements, e => Assert.True(completedOrder.TryDequeue(out var key) && key.Equals(e.Key)));
        }

        [Theory]
        [InlineData(null, null, null)]
        [InlineData("name", "httpEquiv", null)]
        [InlineData("name", null, "property")]
        [InlineData(null, "httpEquiv", "property")]
        [InlineData("name", "httpEquiv", "property")]
        public void Meta_ThrowsWhenGivenInvalidParameters(string name, string httpEquiv, string property)
        {
            var serviceProvider = new TestServiceProvider();
            serviceProvider.AddService(new HeadManager(Mock.Of<IJSRuntime>()));

            var testRenderer = new TestRenderer(serviceProvider);
            var rootComponent = new TestHostComponent(BuildMetaComponent(name, httpEquiv, property));
            var componentId = testRenderer.AssignRootComponentId(rootComponent);

            var ex = Assert.Throws<InvalidOperationException>(() => testRenderer.RenderRootComponent(componentId));
            Assert.Contains("parameters must contain exactly one of", ex.Message);
        }

        [Theory]
        [InlineData("name", null, null)]
        [InlineData(null, "httpEquiv", null)]
        [InlineData(null, null, "property")]
        public void Meta_DoesNotThrowWhenGivenValidParameters(string name, string httpEquiv, string property)
        {
            var serviceProvider = new TestServiceProvider();
            serviceProvider.AddService(new HeadManager(Mock.Of<IJSRuntime>()));

            var testRenderer = new TestRenderer(serviceProvider);
            var rootComponent = new TestHostComponent(BuildMetaComponent(name, httpEquiv, property));
            var componentId = testRenderer.AssignRootComponentId(rootComponent);

            testRenderer.RenderRootComponent(componentId);
        }

        private Action<RenderTreeBuilder> BuildMetaComponent(string name, string httpEquiv, string property)
            => (builder) =>
        {
            builder.OpenComponent<Meta>(0);
            builder.AddAttribute(1, nameof(Meta.Name), name);
            builder.AddAttribute(2, nameof(Meta.HttpEquiv), httpEquiv);
            builder.AddAttribute(3, nameof(Meta.Property), property);
            builder.AddAttribute(4, nameof(Meta.Content), "test content");
            builder.CloseComponent();
        };

        class TestHostComponent : AutoRenderComponent
        {
            private readonly Action<RenderTreeBuilder> _buildRenderTree;

            public TestHostComponent(Action<RenderTreeBuilder> buildRenderTree)
            {
                _buildRenderTree = buildRenderTree;
            }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
                => _buildRenderTree(builder);
        }

        class TestHeadElement : HeadElementBase
        {
            internal override object ElementKey => Key;

            public object Key { get; set; } = new object();

            public Func<Task> OnApplied { get; set; } = () => Task.CompletedTask;

            public int NumTimesApplied { get; private set; } = 0;
            public object StateAfterReset { get; private set; } = null;

            internal override async ValueTask ApplyAsync()
            {
                NumTimesApplied++;

                await OnApplied();
            }

            internal override ValueTask<object> GetInitialStateAsync()
            {
                return ValueTask.FromResult<object>(null);
            }

            internal override ValueTask ResetStateAsync(object initialState)
            {
                StateAfterReset = initialState;

                return ValueTask.CompletedTask;
            }
        }
    }
}
