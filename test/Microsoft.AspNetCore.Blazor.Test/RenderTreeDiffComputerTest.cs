// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.Rendering;
using Microsoft.AspNetCore.Blazor.RenderTree;
using Microsoft.AspNetCore.Blazor.Test.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Test
{
    public class RenderTreeDiffComputerTest
    {
        private readonly Renderer renderer;
        private readonly RenderTreeBuilder oldTree;
        private readonly RenderTreeBuilder newTree;
        private RenderTreeDiffComputer diff;

        public RenderTreeDiffComputerTest()
        {
            renderer = new FakeRenderer();
            oldTree = new RenderTreeBuilder(renderer);
            newTree = new RenderTreeBuilder(renderer);
            diff = new RenderTreeDiffComputer(renderer);
        }

        [Theory]
        [MemberData(nameof(RecognizesEquivalentFramesAsSameCases))]
        public void RecognizesEquivalentFramesAsSame(Action<RenderTreeBuilder> appendAction)
        {
            // Arrange
            appendAction(oldTree);
            appendAction(newTree);

            // Act
            var result = GetSingleUpdatedComponent();

            // Assert
            Assert.Empty(result.Edits);
        }

        public static IEnumerable<object[]> RecognizesEquivalentFramesAsSameCases()
            => new Action<RenderTreeBuilder>[]
            {
                builder => builder.AddText(0, "Hello"),
                builder => builder.OpenElement(0, "Some Element"),
                builder =>
                {
                    builder.OpenElement(0, "Some Element");
                    builder.AddAttribute(1, "My attribute", "My value");
                    builder.CloseElement();
                },
                builder => builder.OpenComponentElement<FakeComponent>(0)
            }.Select(x => new object[] { x });

        [Fact]
        public void RecognizesNewItemsBeingInserted()
        {
            // Arrange
            oldTree.AddText(0, "text0");
            oldTree.AddText(2, "text2");
            newTree.AddText(0, "text0");
            newTree.AddText(1, "text1");
            newTree.AddText(2, "text2");

            // Act
            var result = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependFrame, 1);
                    Assert.Equal(1, entry.NewTreeIndex);
                });
        }

        [Fact]
        public void RecognizesOldItemsBeingRemoved()
        {
            // Arrange
            oldTree.AddText(0, "text0");
            oldTree.AddText(1, "text1");
            oldTree.AddText(2, "text2");
            newTree.AddText(0, "text0");
            newTree.AddText(2, "text2");

            // Act
            var result = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry => AssertEdit(entry, RenderTreeEditType.RemoveFrame, 1));
        }

        [Fact]
        public void RecognizesTrailingSequenceWithinLoopBlockBeingRemoved()
        {
            // Arrange
            oldTree.AddText(0, "x"); // Loop start
            oldTree.AddText(1, "x"); // Will be removed
            oldTree.AddText(2, "x"); // Will be removed
            oldTree.AddText(0, "x"); // Loop start
            newTree.AddText(0, "x"); // Loop start
            newTree.AddText(0, "x"); // Loop start

            // Act
            var result = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry => AssertEdit(entry, RenderTreeEditType.RemoveFrame, 1),
                entry => AssertEdit(entry, RenderTreeEditType.RemoveFrame, 1));
        }

        [Fact]
        public void RecognizesTrailingSequenceWithinLoopBlockBeingAppended()
        {
            // Arrange
            oldTree.AddText(0, "x"); // Loop start
            oldTree.AddText(0, "x"); // Loop start
            newTree.AddText(0, "x"); // Loop start
            newTree.AddText(1, "x"); // Will be added
            newTree.AddText(2, "x"); // Will be added
            newTree.AddText(0, "x"); // Loop start

            // Act
            var result = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependFrame, 1);
                    Assert.Equal(1, entry.NewTreeIndex);
                },
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependFrame, 2);
                    Assert.Equal(2, entry.NewTreeIndex);
                });
        }

        [Fact]
        public void RecognizesTrailingLoopBlockBeingRemoved()
        {
            // Arrange
            oldTree.AddText(0, "x");
            oldTree.AddText(1, "x");
            oldTree.AddText(0, "x"); // Will be removed
            oldTree.AddText(1, "x"); // Will be removed
            newTree.AddText(0, "x");
            newTree.AddText(1, "x");

            // Act
            var result = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry => AssertEdit(entry, RenderTreeEditType.RemoveFrame, 2),
                entry => AssertEdit(entry, RenderTreeEditType.RemoveFrame, 2));
        }

        [Fact]
        public void RecognizesTrailingLoopBlockBeingAdded()
        {
            // Arrange
            oldTree.AddText(0, "x");
            oldTree.AddText(1, "x");
            newTree.AddText(0, "x");
            newTree.AddText(1, "x");
            newTree.AddText(0, "x"); // Will be added
            newTree.AddText(1, "x"); // Will be added

            // Act
            var result = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependFrame, 2);
                    Assert.Equal(2, entry.NewTreeIndex);
                },
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependFrame, 3);
                    Assert.Equal(3, entry.NewTreeIndex);
                });
        }

        [Fact]
        public void RecognizesLeadingLoopBlockItemsBeingAdded()
        {
            // Arrange
            oldTree.AddText(2, "x");
            oldTree.AddText(2, "x"); // Note that the '0' and '1' items are not present on this iteration
            newTree.AddText(2, "x");
            newTree.AddText(0, "x");
            newTree.AddText(1, "x");
            newTree.AddText(2, "x");

            // Act
            var result = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependFrame, 1);
                    Assert.Equal(1, entry.NewTreeIndex);
                },
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependFrame, 2);
                    Assert.Equal(2, entry.NewTreeIndex);
                });
        }

        [Fact]
        public void RecognizesLeadingLoopBlockItemsBeingRemoved()
        {
            // Arrange
            oldTree.AddText(2, "x");
            oldTree.AddText(0, "x");
            oldTree.AddText(1, "x");
            oldTree.AddText(2, "x");
            newTree.AddText(2, "x");
            newTree.AddText(2, "x"); // Note that the '0' and '1' items are not present on this iteration

            // Act
            var result = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry => AssertEdit(entry, RenderTreeEditType.RemoveFrame, 1),
                entry => AssertEdit(entry, RenderTreeEditType.RemoveFrame, 1));
        }

        [Fact]
        public void HandlesAdjacentItemsBeingRemovedAndInsertedAtOnce()
        {
            // Arrange
            oldTree.AddText(0, "text");
            newTree.AddText(1, "text");

            // Act
            var result = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry => AssertEdit(entry, RenderTreeEditType.RemoveFrame, 0),
                entry => AssertEdit(entry, RenderTreeEditType.PrependFrame, 0));
        }

        [Fact]
        public void RecognizesTextUpdates()
        {
            // Arrange
            oldTree.AddText(123, "old text 1");
            oldTree.AddText(182, "old text 2");
            newTree.AddText(123, "new text 1");
            newTree.AddText(182, "new text 2");

            // Act
            var result = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.UpdateText, 0);
                    Assert.Equal(0, entry.NewTreeIndex);
                },
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.UpdateText, 1);
                    Assert.Equal(1, entry.NewTreeIndex);
                });
        }

        [Fact]
        public void RecognizesElementNameChangesAtSameSequenceNumber()
        {
            // Note: It's not possible to trigger this scenario from a Razor component, because
            // a given source sequence can only have a single fixed element name. We might later
            // decide just to throw in this scenario, since it's unnecessary to support it.

            // Arrange
            oldTree.OpenElement(123, "old element");
            oldTree.CloseElement();
            newTree.OpenElement(123, "new element");
            newTree.CloseElement();

            // Act
            var result = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependFrame, 0);
                    Assert.Equal(0, entry.NewTreeIndex);
                },
                entry => AssertEdit(entry, RenderTreeEditType.RemoveFrame, 1));
        }

        [Fact]
        public void RecognizesComponentTypeChangesAtSameSequenceNumber()
        {
            // Arrange
            oldTree.OpenComponentElement<FakeComponent>(123);
            newTree.OpenComponentElement<FakeComponent2>(123);

            // Act
            var renderBatch = GetRenderedBatch();

            // Assert: Even though we didn't assign IDs to the components, this
            // shows that FakeComponent was disposed
            Assert.Collection(renderBatch.DisposedComponentIDs,
                disposedComponentId => Assert.Equal(0, disposedComponentId));

            // Assert: First updated component is the root with one child being
            // prepended, and its earlier incarnation being removed
            Assert.Equal(2, renderBatch.UpdatedComponents.Count);
            var updatedComponent1 = renderBatch.UpdatedComponents.Array[0];
            Assert.Collection(updatedComponent1.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependFrame, 0);
                    Assert.Equal(0, entry.NewTreeIndex);
                    Assert.IsType<FakeComponent2>(updatedComponent1.CurrentState.Array[0].Component);
                },
                entry => AssertEdit(entry, RenderTreeEditType.RemoveFrame, 1));

            // Assert: Second updated component is the new FakeComponent2
            var updatedComponent2 = renderBatch.UpdatedComponents.Array[1];
            Assert.Collection(updatedComponent2.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependFrame, 0);
                    Assert.Equal(0, entry.NewTreeIndex);
                });
        }

        [Fact]
        public void RecognizesAttributesAdded()
        {
            // Arrange
            oldTree.OpenElement(0, "My element");
            oldTree.AddAttribute(1, "existing", "existing value");
            oldTree.CloseElement();
            newTree.OpenElement(0, "My element");
            newTree.AddAttribute(1, "existing", "existing value");
            newTree.AddAttribute(2, "added", "added value");
            newTree.CloseElement();

            // Act
            var result = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.SetAttribute, 0);
                    Assert.Equal(2, entry.NewTreeIndex);
                });
        }

        [Fact]
        public void RecognizesAttributesRemoved()
        {
            // Arrange
            oldTree.OpenElement(0, "My element");
            oldTree.AddAttribute(1, "will be removed", "will be removed value");
            oldTree.AddAttribute(2, "will survive", "surviving value");
            oldTree.CloseElement();
            newTree.OpenElement(0, "My element");
            newTree.AddAttribute(2, "will survive", "surviving value");
            newTree.CloseElement();

            // Act
            var result = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.RemoveAttribute, 0);
                    Assert.Equal("will be removed", entry.RemovedAttributeName);
                });
        }

        [Fact]
        public void RecognizesAttributeStringValuesChanged()
        {
            // Arrange
            oldTree.OpenElement(0, "My element");
            oldTree.AddAttribute(1, "will remain", "will remain value");
            oldTree.AddAttribute(2, "will change", "will change value");
            oldTree.CloseElement();
            newTree.OpenElement(0, "My element");
            newTree.AddAttribute(1, "will remain", "will remain value");
            newTree.AddAttribute(2, "will change", "did change value");
            newTree.CloseElement();

            // Act
            var result = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.SetAttribute, 0);
                    Assert.Equal(2, entry.NewTreeIndex);
                });
        }

        [Fact]
        public void RecognizesAttributeEventHandlerValuesChanged()
        {
            // Arrange
            UIEventHandler retainedHandler = _ => { };
            UIEventHandler removedHandler = _ => { };
            UIEventHandler addedHandler = _ => { };
            oldTree.OpenElement(0, "My element");
            oldTree.AddAttribute(1, "will remain", retainedHandler);
            oldTree.AddAttribute(2, "will change", removedHandler);
            oldTree.CloseElement();
            newTree.OpenElement(0, "My element");
            newTree.AddAttribute(1, "will remain", retainedHandler);
            newTree.AddAttribute(2, "will change", addedHandler);
            newTree.CloseElement();

            // Act
            var result = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.SetAttribute, 0);
                    Assert.Equal(2, entry.NewTreeIndex);
                });
        }

        [Fact]
        public void RecognizesAttributeNamesChangedAtSameSourceSequence()
        {
            // Arrange
            oldTree.OpenElement(0, "My element");
            oldTree.AddAttribute(1, "oldname", "same value");
            oldTree.CloseElement();
            newTree.OpenElement(0, "My element");
            newTree.AddAttribute(1, "newname", "same value");
            newTree.CloseElement();

            // Act
            var result = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.SetAttribute, 0);
                    Assert.Equal(1, entry.NewTreeIndex);
                },
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.RemoveAttribute, 0);
                    Assert.Equal("oldname", entry.RemovedAttributeName);
                });
        }

        [Fact]
        public void DiffsElementsHierarchically()
        {
            // Arrange
            oldTree.AddText(09, "unrelated");
            oldTree.OpenElement(10, "root");
            oldTree.OpenElement(11, "child");
            oldTree.OpenElement(12, "grandchild");
            oldTree.AddText(13, "grandchild old text");
            oldTree.CloseElement();
            oldTree.CloseElement();
            oldTree.CloseElement();

            newTree.AddText(09, "unrelated");
            newTree.OpenElement(10, "root");
            newTree.OpenElement(11, "child");
            newTree.OpenElement(12, "grandchild");
            newTree.AddText(13, "grandchild new text");
            newTree.CloseElement();
            newTree.CloseElement();
            newTree.CloseElement();

            // Act
            var result = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry => AssertEdit(entry, RenderTreeEditType.StepIn, 1),
                entry => AssertEdit(entry, RenderTreeEditType.StepIn, 0),
                entry => AssertEdit(entry, RenderTreeEditType.StepIn, 0),
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.UpdateText, 0);
                    Assert.Equal(4, entry.NewTreeIndex);
                },
                entry => AssertEdit(entry, RenderTreeEditType.StepOut, 0),
                entry => AssertEdit(entry, RenderTreeEditType.StepOut, 0),
                entry => AssertEdit(entry, RenderTreeEditType.StepOut, 0));
        }

        [Fact]
        public void SkipsUnmodifiedSubtrees()
        {
            // Arrange
            oldTree.OpenElement(10, "root");
            oldTree.AddText(11, "Text that will change");
            oldTree.OpenElement(12, "Subtree that will not change");
            oldTree.OpenElement(13, "Another");
            oldTree.AddText(14, "Text that will not change");
            oldTree.CloseElement();
            oldTree.CloseElement();
            oldTree.CloseElement();

            newTree.OpenElement(10, "root");
            newTree.AddText(11, "Text that has changed");
            newTree.OpenElement(12, "Subtree that will not change");
            newTree.OpenElement(13, "Another");
            newTree.AddText(14, "Text that will not change");
            newTree.CloseElement();
            newTree.CloseElement();
            newTree.CloseElement();

            // Act
            var result = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry => AssertEdit(entry, RenderTreeEditType.StepIn, 0),
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.UpdateText, 0);
                    Assert.Equal(1, entry.NewTreeIndex);
                },
                entry => AssertEdit(entry, RenderTreeEditType.StepOut, 0));
        }

        [Fact]
        public void SkipsUnmodifiedTrailingSiblings()
        {
            // Arrange
            oldTree.AddText(10, "text1");
            oldTree.AddText(11, "text2");
            oldTree.AddText(12, "text3");
            oldTree.AddText(13, "text4");
            newTree.AddText(10, "text1");
            newTree.AddText(11, "text2modified");
            newTree.AddText(12, "text3");
            newTree.AddText(13, "text4");

            // Act
            var result = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.UpdateText, 1);
                    Assert.Equal(1, entry.NewTreeIndex);
                });
        }

        [Fact]
        public void InstantiatesChildComponentsForInsertedFrames()
        {
            // Arrange
            oldTree.AddText(10, "text1");                       //  0: text1
            oldTree.OpenElement(11, "container");               //  1: <container>
            oldTree.CloseElement();                             //     </container>
            newTree.AddText(10, "text1");                       //  0: text1
            newTree.OpenElement(11, "container");               //  1: <container>
            newTree.OpenComponentElement<FakeComponent>(12);    //  2:   <FakeComponent>
            newTree.CloseElement();                             //       </FakeComponent>
            newTree.OpenComponentElement<FakeComponent2>(13);   //  3:   <FakeComponent2>
            newTree.CloseElement();                             //       </FakeComponent2>
            newTree.CloseElement();                             //     </container>

            // Act
            var renderBatch = GetRenderedBatch();

            // Assert
            Assert.Equal(3, renderBatch.UpdatedComponents.Count);

            // First component is the root one
            var firstComponentDiff = renderBatch.UpdatedComponents.Array[0];
            Assert.Collection(firstComponentDiff.Edits,
                entry => AssertEdit(entry, RenderTreeEditType.StepIn, 1),
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependFrame, 0);
                    Assert.Equal(2, entry.NewTreeIndex);

                    var newTreeFrame = newTree.GetFrames().Array[entry.NewTreeIndex];
                    Assert.Equal(0, newTreeFrame.ComponentId);
                    Assert.IsType<FakeComponent>(newTreeFrame.Component);
                },
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependFrame, 1);
                    Assert.Equal(3, entry.NewTreeIndex);

                    var newTreeFrame = newTree.GetFrames().Array[entry.NewTreeIndex];
                    Assert.Equal(1, newTreeFrame.ComponentId);
                    Assert.IsType<FakeComponent2>(newTreeFrame.Component);
                },
                entry => AssertEdit(entry, RenderTreeEditType.StepOut, 0));

            // Second in batch is the first child component
            var secondComponentDiff = renderBatch.UpdatedComponents.Array[1];
            Assert.Equal(0, secondComponentDiff.ComponentId);
            Assert.Empty(secondComponentDiff.Edits); // Because FakeComponent produces no frames
            Assert.Empty(secondComponentDiff.CurrentState); // Because FakeComponent produces no frames

            // Third in batch is the second child component
            var thirdComponentDiff = renderBatch.UpdatedComponents.Array[2];
            Assert.Equal(1, thirdComponentDiff.ComponentId);
            Assert.Collection(thirdComponentDiff.Edits,
                entry => AssertEdit(entry, RenderTreeEditType.PrependFrame, 0));
            Assert.Collection(thirdComponentDiff.CurrentState,
                frame => AssertFrame.Text(frame, $"Hello from {nameof(FakeComponent2)}"));
        }

        [Fact]
        public void SetsKnownPropertiesOnChildComponents()
        {
            // Arrange
            var testObject = new object();
            newTree.OpenComponentElement<FakeComponent>(0);
            newTree.AddAttribute(1, nameof(FakeComponent.IntProperty), 123);
            newTree.AddAttribute(2, nameof(FakeComponent.StringProperty), "some string");
            newTree.AddAttribute(3, nameof(FakeComponent.ObjectProperty), testObject);
            newTree.CloseElement();

            // Act
            var renderBatch = GetRenderedBatch();
            var componentInstance = newTree.GetFrames().First().Component as FakeComponent;

            // Assert
            Assert.Equal(2, renderBatch.UpdatedComponents.Count);

            var rootComponentDiff = renderBatch.UpdatedComponents.Array[0];
            AssertEdit(rootComponentDiff.Edits.Single(), RenderTreeEditType.PrependFrame, 0);
            Assert.NotNull(componentInstance);
            Assert.Equal(123, componentInstance.IntProperty);
            Assert.Equal("some string", componentInstance.StringProperty);
            Assert.Same(testObject, componentInstance.ObjectProperty);
        }

        [Fact]
        public void ThrowsIfAssigningUnknownPropertiesToChildComponents()
        {
            // Arrange
            var testObject = new object();
            newTree.OpenComponentElement<FakeComponent>(0);
            newTree.AddAttribute(1, "SomeUnknownProperty", 123);
            newTree.CloseElement();

            // Act/Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                diff.ApplyNewRenderTreeVersion(new RenderBatchBuilder(), 0, oldTree.GetFrames(), newTree.GetFrames());
            });
            Assert.Equal($"Component of type '{typeof(FakeComponent).FullName}' does not have a property matching the name 'SomeUnknownProperty'.", ex.Message);
        }

        [Fact]
        public void ThrowsIfAssigningReadOnlyPropertiesToChildComponents()
        {
            // Arrange
            var testObject = new object();
            newTree.OpenComponentElement<FakeComponent>(0);
            newTree.AddAttribute(1, nameof(FakeComponent.ReadonlyProperty), 123);
            newTree.CloseElement();

            // Act/Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                diff.ApplyNewRenderTreeVersion(new RenderBatchBuilder(), 0, oldTree.GetFrames(), newTree.GetFrames());
            });
            Assert.StartsWith($"Unable to set property '{nameof(FakeComponent.ReadonlyProperty)}' on " +
                $"component of type '{typeof(FakeComponent).FullName}'.", ex.Message);
        }

        [Fact]
        public void RetainsChildComponentsForExistingFrames()
        {
            // Arrange
            oldTree.AddText(10, "text1");                       //  0: text1
            oldTree.OpenElement(11, "container");               //  1: <container>
            oldTree.OpenComponentElement<FakeComponent>(12);    //  2:   <FakeComponent>
            oldTree.CloseElement();                             //       </FakeComponent>
            oldTree.OpenComponentElement<FakeComponent2>(13);   //  3:   <FakeComponent2>
            oldTree.CloseElement();                             //       </FakeComponent2>
            oldTree.CloseElement();                             //     </container
            newTree.AddText(10, "text1");                       //  0: text1
            newTree.OpenElement(11, "container");               //  1: <container>
            newTree.OpenComponentElement<FakeComponent>(12);    //  2:   <FakeComponent>
            newTree.CloseElement();                             //       </FakeComponent>
            newTree.OpenComponentElement<FakeComponent2>(13);   //  3:   <FakeComponent2>
            newTree.CloseElement();                             //       </FakeComponent2>
            newTree.CloseElement();                             //     </container

            diff.ApplyNewRenderTreeVersion(new RenderBatchBuilder(), 0, new RenderTreeBuilder(renderer).GetFrames(), oldTree.GetFrames());
            var originalFakeComponentInstance = oldTree.GetFrames().Array[2].Component;
            var originalFakeComponent2Instance = oldTree.GetFrames().Array[3].Component;

            // Act
            var result = GetSingleUpdatedComponent();
            var newFrame1 = newTree.GetFrames().Array[2];
            var newFrame2 = newTree.GetFrames().Array[3];

            // Assert
            Assert.Empty(result.Edits);
            Assert.Equal(0, newFrame1.ComponentId);
            Assert.Equal(1, newFrame2.ComponentId);
            Assert.Same(originalFakeComponentInstance, newFrame1.Component);
            Assert.Same(originalFakeComponent2Instance, newFrame2.Component);
        }

        [Fact]
        public void UpdatesChangedPropertiesOnRetainedChildComponents()
        {
            // Arrange
            var objectWillNotChange = new object();
            oldTree.OpenComponentElement<FakeComponent>(12);
            oldTree.AddAttribute(13, nameof(FakeComponent.StringProperty), "String will change");
            oldTree.AddAttribute(14, nameof(FakeComponent.ObjectProperty), objectWillNotChange);
            oldTree.CloseElement();
            newTree.OpenComponentElement<FakeComponent>(12);
            newTree.AddAttribute(13, nameof(FakeComponent.StringProperty), "String did change");
            newTree.AddAttribute(14, nameof(FakeComponent.ObjectProperty), objectWillNotChange);
            newTree.CloseElement();

            diff.ApplyNewRenderTreeVersion(new RenderBatchBuilder(), 0, new RenderTreeBuilder(renderer).GetFrames(), oldTree.GetFrames());
            var originalComponentInstance = (FakeComponent)oldTree.GetFrames().Array[0].Component;
            originalComponentInstance.ObjectProperty = null; // So we can see it doesn't get reassigned 

            // Act
            var renderBatch = GetRenderedBatch();
            var newComponentInstance = (FakeComponent)oldTree.GetFrames().Array[0].Component;

            // Assert
            Assert.Equal(2, renderBatch.UpdatedComponents.Count);
            Assert.Same(originalComponentInstance, newComponentInstance);
            Assert.Equal("String did change", newComponentInstance.StringProperty);
            Assert.Null(newComponentInstance.ObjectProperty); // To observe that the property wasn't even written, we nulled it out on the original
        }

        [Fact]
        public void NotifiesIHandlePropertiesChangedBeforeFirstRender()
        {
            // Arrange
            newTree.OpenComponentElement<HandlePropertiesChangedComponent>(0);
            newTree.CloseElement();

            // Act
            var batch = GetRenderedBatch();
            var diffForChildComponent = batch.UpdatedComponents.Array[1];

            // Assert
            Assert.Collection(diffForChildComponent.CurrentState,
                frame => AssertFrame.Text(frame, "Notifications: 1", 0));
        }

        [Fact]
        public void NotifiesIHandlePropertiesChangedWhenChanged()
        {
            // Arrange
            var newTree1 = new RenderTreeBuilder(renderer);
            var newTree2 = new RenderTreeBuilder(renderer);
            oldTree.OpenComponentElement<HandlePropertiesChangedComponent>(0);
            oldTree.AddAttribute(1, nameof(HandlePropertiesChangedComponent.IntProperty), 123);
            oldTree.CloseElement();
            newTree1.OpenComponentElement<HandlePropertiesChangedComponent>(0);
            newTree1.AddAttribute(1, nameof(HandlePropertiesChangedComponent.IntProperty), 123);
            newTree1.CloseElement();
            newTree2.OpenComponentElement<HandlePropertiesChangedComponent>(0);
            newTree2.AddAttribute(1, nameof(HandlePropertiesChangedComponent.IntProperty), 456);
            newTree2.CloseElement();

            // Act/Assert 0: Initial render
            var batch0 = GetRenderedBatch(new RenderTreeBuilder(renderer), oldTree);
            var diffForChildComponent0 = batch0.UpdatedComponents.Array[1];
            var childComponentFrame = batch0.UpdatedComponents.Array[0].CurrentState.Array[0];
            var childComponentInstance = (HandlePropertiesChangedComponent)childComponentFrame.Component;
            Assert.Equal(1, childComponentInstance.NotificationsCount);
            Assert.Collection(diffForChildComponent0.CurrentState,
                frame => AssertFrame.Text(frame, "Notifications: 1", 0));

            // Act/Assert 1: If properties didn't change, we don't notify
            GetRenderedBatch(oldTree, newTree1);
            Assert.Equal(1, childComponentInstance.NotificationsCount);

            // Act/Assert 2: If properties did change, we do notify
            var batch2 = GetRenderedBatch(newTree1, newTree2);
            var diffForChildComponent2 = batch2.UpdatedComponents.Array[1];
            Assert.Equal(2, childComponentInstance.NotificationsCount);
            Assert.Collection(diffForChildComponent2.CurrentState,
                frame => AssertFrame.Text(frame, "Notifications: 2", 0));
        }

        [Fact]
        public void CallsDisposeOnlyOnRemovedChildComponents()
        {
            // Arrange
            oldTree.OpenComponentElement<DisposableComponent>(10);       // <DisposableComponent>
            oldTree.CloseElement();                                      // </DisposableComponent>
            oldTree.OpenComponentElement<NonDisposableComponent>(20);    // <NonDisposableComponent>
            oldTree.CloseElement();                                      // </NonDisposableComponent>
            oldTree.OpenComponentElement<DisposableComponent>(30);       // <DisposableComponent>
            oldTree.CloseElement();                                      // </DisposableComponent>
            newTree.OpenComponentElement<DisposableComponent>(30);       // <DisposableComponent>
            newTree.CloseElement();                                      // </DisposableComponent>

            diff.ApplyNewRenderTreeVersion(new RenderBatchBuilder(), 0, new RenderTreeBuilder(renderer).GetFrames(), oldTree.GetFrames());
            var disposableComponent1 = (DisposableComponent)oldTree.GetFrames().Array[0].Component;
            var nonDisposableComponent = (NonDisposableComponent)oldTree.GetFrames().Array[1].Component;
            var disposableComponent2 = (DisposableComponent)oldTree.GetFrames().Array[2].Component;

            // Act
            var renderedBatch = GetRenderedBatch();

            // Assert: We track NonDisposableComponent was disposed even though it's not IDisposable
            Assert.Equal(renderedBatch.DisposedComponentIDs, new[] { 0, 1 });

            // Assert: We did call Dispose on the disposed DisposableComponent
            Assert.Equal(1, disposableComponent1.DisposalCount);

            // Assert: We didn't dispose the retained component
            Assert.Equal(0, disposableComponent2.DisposalCount);
        }

        private RenderTreeDiff GetSingleUpdatedComponent()
        {
            var diffsInBatch = GetRenderedBatch().UpdatedComponents;
            Assert.Equal(1, diffsInBatch.Count);
            return diffsInBatch.Array[0];
        }

        private RenderBatch GetRenderedBatch()
            => GetRenderedBatch(oldTree, newTree);

        private RenderBatch GetRenderedBatch(RenderTreeBuilder from, RenderTreeBuilder to)
        {
            var batchBuilder = new RenderBatchBuilder();
            diff.ApplyNewRenderTreeVersion(batchBuilder, 0, from.GetFrames(), to.GetFrames());
            return batchBuilder.ToBatch();
        }

        private class FakeRenderer : Renderer
        {
            internal protected override void UpdateDisplay(RenderBatch renderBatch)
            {
            }
        }

        private class FakeComponent : IComponent
        {
            public int IntProperty { get; set; }
            public string StringProperty { get; set; }
            public object ObjectProperty { get; set; }
            public string ReadonlyProperty { get; private set; }
            private string PrivateProperty { get; set; }

            public void BuildRenderTree(RenderTreeBuilder builder)
            {
            }
        }

        private class FakeComponent2 : IComponent
        {
            public void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.AddText(100, $"Hello from {nameof(FakeComponent2)}");
            }
        }

        private class HandlePropertiesChangedComponent : IComponent, IHandlePropertiesChanged
        {
            public int NotificationsCount { get; private set; }

            public int IntProperty { get; set; }

            public void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.AddText(0, $"Notifications: {NotificationsCount}");
            }

            public void OnPropertiesChanged()
            {
                NotificationsCount++;
            }
        }

        private class DisposableComponent : IComponent, IDisposable
        {
            public int DisposalCount { get; private set; }
            public void Dispose() => DisposalCount++;
            public void BuildRenderTree(RenderTreeBuilder builder) { }
        }

        private class NonDisposableComponent : IComponent
        {
            public void BuildRenderTree(RenderTreeBuilder builder) { }
        }


        private static void AssertEdit(
            RenderTreeEdit edit,
            RenderTreeEditType type,
            int siblingIndex)
        {
            Assert.Equal(type, edit.Type);
            Assert.Equal(siblingIndex, edit.SiblingIndex);
        }
    }
}
