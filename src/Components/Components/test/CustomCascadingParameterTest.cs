using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Xunit;

namespace Microsoft.AspNetCore.Components.Test
{
    public class CustomCascadingParameterTest
    {
        [Fact]
        public void PassesCascadingParametersToNestedComponents()
        {
            // Arrange
            var serviceProvider = new TestServiceProvider();
            var data = new CustomComponentData();
            data.AddValue(typeof(string), "Hello", null);
            serviceProvider.AddService(data);
            var renderer = new TestRenderer(serviceProvider);
            var component = new TestComponent(builder =>
            {
                builder.OpenComponent<CustomCascadingComponent>(0);
                builder.AddAttribute(1, "ChildContent", new RenderFragment(childBuilder =>
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
            var serviceProvider = new TestServiceProvider();
            var data = new CustomComponentData();
            data.AddValue(typeof(string), "Hello", null);
            serviceProvider.AddService(data);
            var renderer = new TestRenderer(serviceProvider);
            var regularParameterValue = "Initial value";
            var component = new TestComponent(builder =>
            {
                builder.OpenComponent<CustomCascadingComponent>(0);
                builder.AddAttribute(1, "ChildContent", new RenderFragment(childBuilder =>
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
            var serviceProvider = new TestServiceProvider();
            var data = new CustomComponentData();
            data.AddValue(typeof(string), "Initial value", null);
            serviceProvider.AddService(data);
            var renderer = new TestRenderer(serviceProvider);
            var component = new TestComponent(builder =>
            {
                builder.OpenComponent<CustomCascadingComponent>(0);
                builder.AddAttribute(1, "ChildContent", new RenderFragment(childBuilder =>
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
            data.UpdateValue("Updated value");
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
            var serviceProvider = new TestServiceProvider();
            var data = new CustomComponentData();
            data.AddValue("Unchanging value", null);
            serviceProvider.AddService(data);
            var renderer = new TestRenderer(serviceProvider);
            var component = new TestComponent(builder =>
            {
                builder.OpenComponent<CustomCascadingComponent>(0);
                builder.AddAttribute(1, "ChildContent", new RenderFragment(childBuilder =>
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
            var displayNestedComponent = true;
            var serviceProvider = new TestServiceProvider();
            var data = new CustomComponentData();
            data.AddValue("Initial value", null);
            serviceProvider.AddService(data);
            var renderer = new TestRenderer(serviceProvider);
            var component = new TestComponent(builder =>
            {
                // At the outer level, have an unrelated fixed cascading value to show we can deal with combining both types
                builder.OpenComponent<CascadingValue<int>>(0);
                builder.AddAttribute(1, "Value", 123);
                builder.AddAttribute(2, "IsFixed", true);
                builder.AddAttribute(3, "ChildContent", new RenderFragment(builder2 =>
                {
                    // Then also have a non-fixed cascading value so we can show that unsubscription works
                    builder2.OpenComponent<CustomCascadingComponent>(0);
                    builder2.AddAttribute(1, "ChildContent", new RenderFragment(builder3 =>
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
            data.UpdateValue("Updated value");
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
            data.UpdateValue("Updated value 2");
            component.TriggerRender();
            Assert.Equal(2, nestedComponent.NumSetParametersCalls);
        }

        [Fact]
        public void DoesNotNotifyDescendantsOfUpdatedCascadingParameterValuesWhenFixed()
        {
            // Arrange
            var shouldIncludeChild = true;
            var serviceProvider = new TestServiceProvider();
            var data = new CustomComponentData();
            data.AddValue("Initial value", null);
            serviceProvider.AddService(data);
            var renderer = new TestRenderer(serviceProvider);
            var component = new TestComponent(builder =>
            {
                builder.OpenComponent<CustomCascadingComponent>(0);
                builder.AddAttribute(1, "IsFixed", true);
                builder.AddAttribute(2, "ChildContent", new RenderFragment(childBuilder =>
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
            data.UpdateValue("Updated value");
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

        private static T FindComponent<T>(CapturedBatch batch, out int componentId)
        {
            var componentFrame = batch.ReferenceFrames.Single(
                frame => frame.FrameType == RenderTreeFrameType.Component
                         && frame.Component is T);
            componentId = componentFrame.ComponentId;
            return (T)componentFrame.Component;
        }

        class CustomComponentData
        {
            private readonly Dictionary<(Type valueType, string valueName), object> _values =
                new Dictionary<(Type valueType, string valueName), object>();

            private bool _updated = true;

            public CustomComponentData AddValue(Type valueType, object value, string valueName = null)
            {
                _values.Add((valueType, valueName), value);
                _updated = true;
                return this;
            }

            public CustomComponentData AddValue<T>(T value, string valueName = null)
            {
                _values.Add((typeof(T), valueName), value);
                _updated = true;
                return this;
            }

            public CustomComponentData UpdateValue(Type valueType, object value, string valueName = null)
            {
                if (!_values.TryGetValue((valueType, valueName), out value)) return this;
                _updated = true;
                _values[(valueType, valueName)] = value;
                return this;
            }

            public CustomComponentData UpdateValue<T>(T value, string valueName = null)
            {
                if (!_values.TryGetValue((typeof(T), valueName), out var valueObject)) return this;
                _updated = true;
                _values[(typeof(T), valueName)] = value;
                return this;
            }

            internal IReadOnlyDictionary<(Type valueType, string valueName), object> Values => _values;
            internal bool IsUpdated => _updated;

            public bool HasValue(Type valueType, string valueName)
            {
                return _values.ContainsKey((valueType, valueName)) || _values.ContainsKey((valueType, null));
            }

            public object GetValue(Type valueType, string valueName)
            {
                return _values.TryGetValue((valueType, valueName), out var value) ? value : _values.TryGetValue((valueType, null), out value) ? value : null;
            }

            internal void UpdateCompleted() => _updated = false;
        }

        class CustomCascadingComponent : AutoRenderComponent, ICascadingValueComponent
        {
            private HashSet<IComponentState> _subscribers;
            [Parameter] public bool IsFixed { get; set; }

            [Inject] public CustomComponentData Data { get; set; }

            /// <summary>
            /// The content to which the value should be provided.
            /// </summary>
            [Parameter] public RenderFragment ChildContent { get; set; }

            public bool HasValue(Type valueType, string valueName)
            {
                return Data.HasValue(valueType, valueName);
            }

            public object GetValue(Type valueType, string valueName)
            {
                return Data.GetValue(valueType, valueName);
            }

            public void Subscribe(IComponentState subscriber)
            {
                _subscribers ??= new HashSet<IComponentState>();
                _subscribers.Add(subscriber);
            }

            public void Unsubscribe(IComponentState subscriber)
            {
                _subscribers.Remove(subscriber);
            }

            protected void UpdateSubscribers(in ParameterView parameters)
            {
                if (_subscribers is null) return;
                foreach (var subscriber in _subscribers)
                {
                    subscriber.NotifyChanged(parameters);
                }
            }

            public override async Task SetParametersAsync(ParameterView parameters)
            {
                await base.SetParametersAsync(parameters);
                if (Data.IsUpdated)
                {
                    Data.UpdateCompleted();
                    UpdateSubscribers(parameters);
                }
            }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                ChildContent(builder);
            }
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
