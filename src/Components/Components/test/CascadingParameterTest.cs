// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Xunit;

namespace Microsoft.AspNetCore.Components.Test
{
    public class CascadingParameterTest
    {
        [Fact]
        public void PassesCascadingParametersToNestedComponents()
        {
            // Arrange
            var renderer = new TestRenderer();
            var component = new TestComponent(builder =>
            {
                builder.OpenComponent<CascadingValue<string>>(0);
                builder.AddAttribute(1, "Value", "Hello");
                builder.AddAttribute(2, "ChildContent", new RenderFragment(childBuilder =>
                {
                    childBuilder.OpenComponent<CascadingParameterConsumerComponent<string>>(0);
                    childBuilder.AddAttribute(1, "RegularParameter", "Goodbye");
                    childBuilder.CloseComponent();
                }));
                builder.CloseComponent();
            });

            // Act/Assert
            var componentId = renderer.AssignRootComponentId(component);
            component.TriggerRender();
            var batch = renderer.Batches.Single();
            var nestedComponent = FindComponent<CascadingParameterConsumerComponent<string>>(batch, out var nestedComponentId);
            var nestedComponentDiff = batch.DiffsByComponentId[nestedComponentId].Single();

            // The nested component was rendered with the correct parameters
            Assert.Collection(nestedComponentDiff.Edits,
                edit =>
                {
                    Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                    AssertFrame.Text(
                        batch.ReferenceFrames[edit.ReferenceFrameIndex],
                        "CascadingParameter=Hello; RegularParameter=Goodbye");
                });
            Assert.Equal(1, nestedComponent.NumRenders);
        }

        [Fact]
        public void RetainsCascadingParametersWhenUpdatingDirectParameters()
        {
            // Arrange
            var renderer = new TestRenderer();
            var regularParameterValue = "Initial value";
            var component = new TestComponent(builder =>
            {
                builder.OpenComponent<CascadingValue<string>>(0);
                builder.AddAttribute(1, "Value", "Hello");
                builder.AddAttribute(2, "ChildContent", new RenderFragment(childBuilder =>
                {
                    childBuilder.OpenComponent<CascadingParameterConsumerComponent<string>>(0);
                    childBuilder.AddAttribute(1, "RegularParameter", regularParameterValue);
                    childBuilder.CloseComponent();
                }));
                builder.CloseComponent();
            });

            // Act 1: Render in initial state
            var componentId = renderer.AssignRootComponentId(component);
            component.TriggerRender();

            // Capture the nested component so we can verify the update later
            var firstBatch = renderer.Batches.Single();
            var nestedComponent = FindComponent<CascadingParameterConsumerComponent<string>>(firstBatch, out var nestedComponentId);
            Assert.Equal(1, nestedComponent.NumRenders);

            // Act 2: Render again with updated regular parameter
            regularParameterValue = "Changed value";
            component.TriggerRender();

            // Assert
            Assert.Equal(2, renderer.Batches.Count);
            var secondBatch = renderer.Batches[1];
            var nestedComponentDiff = secondBatch.DiffsByComponentId[nestedComponentId].Single();

            // The nested component was rendered with the correct parameters
            Assert.Collection(nestedComponentDiff.Edits,
                edit =>
                {
                    Assert.Equal(RenderTreeEditType.UpdateText, edit.Type);
                    Assert.Equal(0, edit.ReferenceFrameIndex); // This is the only change
                    AssertFrame.Text(secondBatch.ReferenceFrames[0], "CascadingParameter=Hello; RegularParameter=Changed value");
                });
            Assert.Equal(2, nestedComponent.NumRenders);
        }

        [Fact]
        public void NotifiesDescendantsOfUpdatedCascadingParameterValuesAndPreservesDirectParameters()
        {
            // Arrange
            var providedValue = "Initial value";
            var renderer = new TestRenderer();
            var component = new TestComponent(builder =>
            {
                builder.OpenComponent<CascadingValue<string>>(0);
                builder.AddAttribute(1, "Value", providedValue);
                builder.AddAttribute(2, "ChildContent", new RenderFragment(childBuilder =>
                {
                    childBuilder.OpenComponent<CascadingParameterConsumerComponent<string>>(0);
                    childBuilder.AddAttribute(1, "RegularParameter", "Goodbye");
                    childBuilder.CloseComponent();
                }));
                builder.CloseComponent();
            });

            // Act 1: Initial render; capture nested component ID
            var componentId = renderer.AssignRootComponentId(component);
            component.TriggerRender();
            var firstBatch = renderer.Batches.Single();
            var nestedComponent = FindComponent<CascadingParameterConsumerComponent<string>>(firstBatch, out var nestedComponentId);
            Assert.Equal(1, nestedComponent.NumRenders);

            // Act 2: Re-render CascadingValue with new value
            providedValue = "Updated value";
            component.TriggerRender();

            // Assert: We re-rendered CascadingParameterConsumerComponent
            Assert.Equal(2, renderer.Batches.Count);
            var secondBatch = renderer.Batches[1];
            var nestedComponentDiff = secondBatch.DiffsByComponentId[nestedComponentId].Single();

            // The nested component was rendered with the correct parameters
            Assert.Collection(nestedComponentDiff.Edits,
                edit =>
                {
                    Assert.Equal(RenderTreeEditType.UpdateText, edit.Type);
                    Assert.Equal(0, edit.ReferenceFrameIndex); // This is the only change
                    AssertFrame.Text(secondBatch.ReferenceFrames[0], "CascadingParameter=Updated value; RegularParameter=Goodbye");
                });
            Assert.Equal(2, nestedComponent.NumRenders);
        }

        [Fact]
        public void DoesNotNotifyDescendantsIfCascadingParameterValuesAreImmutableAndUnchanged()
        {
            // Arrange
            var renderer = new TestRenderer();
            var component = new TestComponent(builder =>
            {
                builder.OpenComponent<CascadingValue<string>>(0);
                builder.AddAttribute(1, "Value", "Unchanging value");
                builder.AddAttribute(2, "ChildContent", new RenderFragment(childBuilder =>
                {
                    childBuilder.OpenComponent<CascadingParameterConsumerComponent<string>>(0);
                    childBuilder.AddAttribute(1, "RegularParameter", "Goodbye");
                    childBuilder.CloseComponent();
                }));
                builder.CloseComponent();
            });

            // Act 1: Initial render
            var componentId = renderer.AssignRootComponentId(component);
            component.TriggerRender();
            var firstBatch = renderer.Batches.Single();
            var nestedComponent = FindComponent<CascadingParameterConsumerComponent<string>>(firstBatch, out _);
            Assert.Equal(3, firstBatch.DiffsByComponentId.Count); // Root + CascadingValue + nested
            Assert.Equal(1, nestedComponent.NumRenders);

            // Act/Assert: Re-render the CascadingValue; observe nested component wasn't re-rendered
            component.TriggerRender();

            // Assert: We did not re-render CascadingParameterConsumerComponent
            Assert.Equal(2, renderer.Batches.Count);
            var secondBatch = renderer.Batches[1];
            Assert.Equal(2, secondBatch.DiffsByComponentId.Count); // Root + CascadingValue, but not nested one
            Assert.Equal(1, nestedComponent.NumRenders);
        }

        [Fact]
        public void StopsNotifyingDescendantsIfTheyAreRemoved()
        {
            // Arrange
            var providedValue = "Initial value";
            var displayNestedComponent = true;
            var renderer = new TestRenderer();
            var component = new TestComponent(builder =>
            {
                // At the outer level, have an unrelated fixed cascading value to show we can deal with combining both types
                builder.OpenComponent<CascadingValue<int>>(0);
                builder.AddAttribute(1, "Value", 123);
                builder.AddAttribute(2, "IsFixed", true);
                builder.AddAttribute(3, "ChildContent", new RenderFragment(builder2 =>
                {
                    // Then also have a non-fixed cascading value so we can show that unsubscription works
                    builder2.OpenComponent<CascadingValue<string>>(0);
                    builder2.AddAttribute(1, "Value", providedValue);
                    builder2.AddAttribute(2, "ChildContent", new RenderFragment(builder3 =>
                    {
                        if (displayNestedComponent)
                        {
                            builder3.OpenComponent<SecondCascadingParameterConsumerComponent<string, int>>(0);
                            builder3.AddAttribute(1, "RegularParameter", "Goodbye");
                            builder3.CloseComponent();
                        }
                    }));
                    builder2.CloseComponent();
                }));
                builder.CloseComponent();
            });

            // Act 1: Initial render; capture nested component ID
            var componentId = renderer.AssignRootComponentId(component);
            component.TriggerRender();
            var firstBatch = renderer.Batches.Single();
            var nestedComponent = FindComponent<CascadingParameterConsumerComponent<string>>(firstBatch, out var nestedComponentId);
            Assert.Equal(1, nestedComponent.NumSetParametersCalls);
            Assert.Equal(1, nestedComponent.NumRenders);

            // Act/Assert 2: Re-render the CascadingValue; observe nested component wasn't re-rendered
            providedValue = "Updated value";
            displayNestedComponent = false; // Remove the nested component
            component.TriggerRender();

            // Assert: We did not render the nested component now it's been removed
            Assert.Equal(2, renderer.Batches.Count);
            var secondBatch = renderer.Batches[1];
            Assert.Equal(1, nestedComponent.NumRenders);
            Assert.Equal(3, secondBatch.DiffsByComponentId.Count); // Root + CascadingValue + CascadingValue, but not nested component

            // We *did* send updated params during the first render where it was removed,
            // because the params are sent before the disposal logic runs. We could avoid
            // this by moving the notifications into the OnAfterRender phase, but then we'd
            // often render descendants twice (once because they are descendants and some
            // direct parameter might have changed, then once because a cascading parameter
            // changed). We can't have it both ways, so optimize for the case when the
            // nested component *hasn't* just been removed.
            Assert.Equal(2, nestedComponent.NumSetParametersCalls);

            // Act 3: However, after disposal, the subscription is removed, so we won't send
            // updated params on subsequent CascadingValue renders.
            providedValue = "Updated value 2";
            component.TriggerRender();
            Assert.Equal(2, nestedComponent.NumSetParametersCalls);
        }

        [Fact]
        public void DoesNotNotifyDescendantsOfUpdatedCascadingParameterValuesWhenFixed()
        {
            // Arrange
            var providedValue = "Initial value";
            var shouldIncludeChild = true;
            var renderer = new TestRenderer();
            var component = new TestComponent(builder =>
            {
                builder.OpenComponent<CascadingValue<string>>(0);
                builder.AddAttribute(1, "Value", providedValue);
                builder.AddAttribute(2, "IsFixed", true);
                builder.AddAttribute(3, "ChildContent", new RenderFragment(childBuilder =>
                {
                    if (shouldIncludeChild)
                    {
                        childBuilder.OpenComponent<CascadingParameterConsumerComponent<string>>(0);
                        childBuilder.AddAttribute(1, "RegularParameter", "Goodbye");
                        childBuilder.CloseComponent();
                    }
                }));
                builder.CloseComponent();
            });

            // Act 1: Initial render; capture nested component ID
            var componentId = renderer.AssignRootComponentId(component);
            component.TriggerRender();
            var firstBatch = renderer.Batches.Single();
            var nestedComponent = FindComponent<CascadingParameterConsumerComponent<string>>(firstBatch, out var nestedComponentId);
            Assert.Equal(1, nestedComponent.NumRenders);

            // Assert: Initial value is supplied to descendant
            var nestedComponentDiff = firstBatch.DiffsByComponentId[nestedComponentId].Single();
            Assert.Collection(nestedComponentDiff.Edits, edit =>
            {
                Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                AssertFrame.Text(
                    firstBatch.ReferenceFrames[edit.ReferenceFrameIndex],
                    "CascadingParameter=Initial value; RegularParameter=Goodbye");
            });

            // Act 2: Re-render CascadingValue with new value
            providedValue = "Updated value";
            component.TriggerRender();

            // Assert: We did not re-render the descendant
            Assert.Equal(2, renderer.Batches.Count);
            var secondBatch = renderer.Batches[1];
            Assert.Equal(2, secondBatch.DiffsByComponentId.Count); // Root + CascadingValue, but not nested one
            Assert.Equal(1, nestedComponent.NumSetParametersCalls);
            Assert.Equal(1, nestedComponent.NumRenders);

            // Act 3: Dispose
            shouldIncludeChild = false;
            component.TriggerRender();

            // Assert: Absence of an exception here implies we didn't cause a problem by
            // trying to remove a non-existent subscription
        }

        [Fact]
        public void CascadingValueThrowsIfFixedFlagChangesToTrue()
        {
            // Arrange
            var renderer = new TestRenderer();
            var isFixed = false;
            var component = new TestComponent(builder =>
            {
                builder.OpenComponent<CascadingValue<object>>(0);
                builder.AddAttribute(1, "IsFixed", isFixed);
                builder.AddAttribute(2, "Value", new object());
                builder.CloseComponent();
            });
            renderer.AssignRootComponentId(component);
            component.TriggerRender();

            // Act/Assert
            isFixed = true;
            var ex = Assert.Throws<InvalidOperationException>(() => component.TriggerRender());
            Assert.Equal("The value of IsFixed cannot be changed dynamically.", ex.Message);
        }

        [Fact]
        public void CascadingValueThrowsIfFixedFlagChangesToFalse()
        {
            // Arrange
            var renderer = new TestRenderer();
            var isFixed = true;
            var component = new TestComponent(builder =>
            {
                builder.OpenComponent<CascadingValue<object>>(0);
                if (isFixed) // Showing also that "unset" is treated as "false"
                {
                    builder.AddAttribute(1, "IsFixed", true);
                }
                builder.AddAttribute(2, "Value", new object());
                builder.CloseComponent();
            });
            renderer.AssignRootComponentId(component);
            component.TriggerRender();

            // Act/Assert
            isFixed = false;
            var ex = Assert.Throws<InvalidOperationException>(() => component.TriggerRender());
            Assert.Equal("The value of IsFixed cannot be changed dynamically.", ex.Message);
        }

        [Fact]
        public void ParameterViewSuppliedWithCascadingParametersCannotBeUsedAfterSynchronousReturn()
        {
            // Arrange
            var providedValue = "Initial value";
            var renderer = new TestRenderer();
            var component = new TestComponent(builder =>
            {
                builder.OpenComponent<CascadingValue<string>>(0);
                builder.AddAttribute(1, "Value", providedValue);
                builder.AddAttribute(2, "ChildContent", new RenderFragment(childBuilder =>
                {
                    childBuilder.OpenComponent<CascadingParameterConsumerComponent<string>>(0);
                    childBuilder.CloseComponent();
                }));
                builder.CloseComponent();
            });

            // Initial render; capture nested component
            var componentId = renderer.AssignRootComponentId(component);
            component.TriggerRender();
            var firstBatch = renderer.Batches.Single();
            var nestedComponent = FindComponent<CascadingParameterConsumerComponent<string>>(firstBatch, out var nestedComponentId);

            // Re-render CascadingValue with new value, so it gets a new ParameterView
            providedValue = "Updated value";
            component.TriggerRender();
            Assert.Equal(2, renderer.Batches.Count);

            // It's no longer able to access anything in the ParameterView it just received
            var ex = Assert.Throws<InvalidOperationException>(nestedComponent.AttemptIllegalAccessToLastParameterView);
            Assert.Equal($"The {nameof(ParameterView)} instance can no longer be read because it has expired. {nameof(ParameterView)} can only be read synchronously and must not be stored for later use.", ex.Message);
        }

        private static T FindComponent<T>(CapturedBatch batch, out int componentId)
        {
            var componentFrame = batch.ReferenceFrames.Single(
                frame => frame.FrameType == RenderTreeFrameType.Component
                         && frame.Component is T);
            componentId = componentFrame.ComponentId;
            return (T)componentFrame.Component;
        }

        class TestComponent : AutoRenderComponent
        {
            private readonly RenderFragment _renderFragment;

            public TestComponent(RenderFragment renderFragment)
            {
                _renderFragment = renderFragment;
            }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
                => _renderFragment(builder);
        }

        class CascadingParameterConsumerComponent<T> : AutoRenderComponent
        {
            private ParameterView lastParameterView;

            public int NumSetParametersCalls { get; private set; }
            public int NumRenders { get; private set; }

            [CascadingParameter] T CascadingParameter { get; set; }
            [Parameter] public string RegularParameter { get; set; }

            public override async Task SetParametersAsync(ParameterView parameters)
            {
                lastParameterView = parameters;
                NumSetParametersCalls++;
                await base.SetParametersAsync(parameters);
            }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                NumRenders++;
                builder.AddContent(0, $"CascadingParameter={CascadingParameter}; RegularParameter={RegularParameter}");
            }

            public void AttemptIllegalAccessToLastParameterView()
            {
                // You're not allowed to hold onto a ParameterView and access it later,
                // so this should throw
                lastParameterView.TryGetValue<object>("anything", out _);
            }
        }

        class SecondCascadingParameterConsumerComponent<T1, T2> : CascadingParameterConsumerComponent<T1>
        {
            [CascadingParameter] T2 SecondCascadingParameter { get; set; }
        }
    }
}
