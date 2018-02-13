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
    public class RenderTreeDiffBuilderTest
    {
        private readonly Renderer renderer;
        private readonly RenderTreeBuilder oldTree;
        private readonly RenderTreeBuilder newTree;

        public RenderTreeDiffBuilderTest()
        {
            renderer = new FakeRenderer();
            oldTree = new RenderTreeBuilder(renderer);
            newTree = new RenderTreeBuilder(renderer);
        }

        [Theory]
        [MemberData(nameof(RecognizesEquivalentFramesAsSameCases))]
        public void RecognizesEquivalentFramesAsSame(Action<RenderTreeBuilder> appendAction)
        {
            // Arrange
            appendAction(oldTree);
            appendAction(newTree);

            // Act
            var (result, referenceFrames) = GetSingleUpdatedComponent(initializeFromFrames: true);

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
                builder =>
                {
                    builder.OpenComponent<FakeComponent>(0);
                    builder.CloseComponent();
                }
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
            var (result, referenceFrames) = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependFrame, 1);
                    Assert.Equal(0, entry.ReferenceFrameIndex);
                    AssertFrame.Text(referenceFrames[0], "text1", 1);
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
            var (result, referenceFrames) = GetSingleUpdatedComponent();

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
            var (result, referenceFrames) = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry => AssertEdit(entry, RenderTreeEditType.RemoveFrame, 1),
                entry => AssertEdit(entry, RenderTreeEditType.RemoveFrame, 1));
        }

        [Fact]
        public void RecognizesTrailingSequenceWithinLoopBlockBeingAppended()
        {
            // Arrange
            oldTree.AddText(10, "x"); // Loop start
            oldTree.AddText(10, "x"); // Loop start
            newTree.AddText(10, "x"); // Loop start
            newTree.AddText(11, "x"); // Will be added
            newTree.AddText(12, "x"); // Will be added
            newTree.AddText(10, "x"); // Loop start

            // Act
            var (result, referenceFrames) = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependFrame, 1);
                    Assert.Equal(0, entry.ReferenceFrameIndex);
                },
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependFrame, 2);
                    Assert.Equal(1, entry.ReferenceFrameIndex);
                });
            AssertFrame.Text(referenceFrames[0], "x", 11);
            AssertFrame.Text(referenceFrames[1], "x", 12);
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
            var (result, referenceFrames) = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry => AssertEdit(entry, RenderTreeEditType.RemoveFrame, 2),
                entry => AssertEdit(entry, RenderTreeEditType.RemoveFrame, 2));
        }

        [Fact]
        public void RecognizesTrailingLoopBlockBeingAdded()
        {
            // Arrange
            oldTree.AddText(10, "x");
            oldTree.AddText(11, "x");
            newTree.AddText(10, "x");
            newTree.AddText(11, "x");
            newTree.AddText(10, "x"); // Will be added
            newTree.AddText(11, "x"); // Will be added

            // Act
            var (result, referenceFrames) = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependFrame, 2);
                    Assert.Equal(0, entry.ReferenceFrameIndex);
                },
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependFrame, 3);
                    Assert.Equal(1, entry.ReferenceFrameIndex);
                });
            AssertFrame.Text(referenceFrames[0], "x", 10);
            AssertFrame.Text(referenceFrames[1], "x", 11);
        }

        [Fact]
        public void RecognizesLeadingLoopBlockItemsBeingAdded()
        {
            // Arrange
            oldTree.AddText(12, "x");
            oldTree.AddText(12, "x"); // Note that the '0' and '1' items are not present on this iteration
            newTree.AddText(12, "x");
            newTree.AddText(10, "x");
            newTree.AddText(11, "x");
            newTree.AddText(12, "x");

            // Act
            var (result, referenceFrames) = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependFrame, 1);
                    Assert.Equal(0, entry.ReferenceFrameIndex);
                },
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependFrame, 2);
                    Assert.Equal(1, entry.ReferenceFrameIndex);
                });
            AssertFrame.Text(referenceFrames[0], "x", 10);
            AssertFrame.Text(referenceFrames[1], "x", 11);
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
            var (result, referenceFrames) = GetSingleUpdatedComponent();

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
            var (result, referenceFrames) = GetSingleUpdatedComponent();

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
            var (result, referenceFrames) = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.UpdateText, 0);
                    Assert.Equal(0, entry.ReferenceFrameIndex);
                },
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.UpdateText, 1);
                    Assert.Equal(1, entry.ReferenceFrameIndex);
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
            var (result, referenceFrames) = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry => AssertEdit(entry, RenderTreeEditType.RemoveFrame, 0),
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependFrame, 0);
                    Assert.Equal(0, entry.ReferenceFrameIndex);
                });
        }

        [Fact]
        public void RecognizesComponentTypeChangesAtSameSequenceNumber()
        {
            // Arrange
            oldTree.OpenComponent<FakeComponent>(123);
            oldTree.CloseComponent();
            GetRenderedBatch(new RenderTreeBuilder(renderer), oldTree, false); // Assign initial IDs
            newTree.OpenComponent<FakeComponent2>(123);
            newTree.CloseComponent();
            var batchBuilder = new RenderBatchBuilder();

            // Act
            var diff = RenderTreeDiffBuilder.ComputeDiff(renderer, batchBuilder, 0, oldTree.GetFrames(), newTree.GetFrames());

            // Assert: We're going to dispose the old component and render the new one
            Assert.Equal(new[] { 0 }, batchBuilder.ComponentDisposalQueue);
            Assert.Collection(diff.Edits,
                entry => AssertEdit(entry, RenderTreeEditType.RemoveFrame, 0),
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependFrame, 0);
                    Assert.IsType<FakeComponent2>(batchBuilder.ReferenceFramesBuffer.Buffer[entry.ReferenceFrameIndex].Component);
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
            var (result, referenceFrames) = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.SetAttribute, 0);
                    Assert.Equal(0, entry.ReferenceFrameIndex);
                });
            AssertFrame.Attribute(referenceFrames[0], "added", "added value");
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
            var (result, referenceFrames) = GetSingleUpdatedComponent();

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
            var (result, referenceFrames) = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.SetAttribute, 0);
                    Assert.Equal(0, entry.ReferenceFrameIndex);
                });
            AssertFrame.Attribute(referenceFrames[0], "will change", "did change value");
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
            var (result, referenceFrames) = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.SetAttribute, 0);
                    Assert.Equal(0, entry.ReferenceFrameIndex);
                });
            AssertFrame.Attribute(referenceFrames[0], "will change", addedHandler);
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
            var (result, referenceFrames) = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.RemoveAttribute, 0);
                    Assert.Equal("oldname", entry.RemovedAttributeName);
                },
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.SetAttribute, 0);
                    Assert.Equal(0, entry.ReferenceFrameIndex);
                });
            AssertFrame.Attribute(referenceFrames[0], "newname", "same value");
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
            var (result, referenceFrames) = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry => AssertEdit(entry, RenderTreeEditType.StepIn, 1),
                entry => AssertEdit(entry, RenderTreeEditType.StepIn, 0),
                entry => AssertEdit(entry, RenderTreeEditType.StepIn, 0),
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.UpdateText, 0);
                    Assert.Equal(0, entry.ReferenceFrameIndex);
                },
                entry => AssertEdit(entry, RenderTreeEditType.StepOut, 0),
                entry => AssertEdit(entry, RenderTreeEditType.StepOut, 0),
                entry => AssertEdit(entry, RenderTreeEditType.StepOut, 0));
            AssertFrame.Text(referenceFrames[0], "grandchild new text", 13);
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
            var (result, referenceFrames) = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry => AssertEdit(entry, RenderTreeEditType.StepIn, 0),
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.UpdateText, 0);
                    Assert.Equal(0, entry.ReferenceFrameIndex);
                },
                entry => AssertEdit(entry, RenderTreeEditType.StepOut, 0));
            AssertFrame.Text(referenceFrames[0], "Text that has changed", 11);
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
            var (result, referenceFrames) = GetSingleUpdatedComponent();

            // Assert
            Assert.Collection(result.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.UpdateText, 1);
                    Assert.Equal(0, entry.ReferenceFrameIndex);
                });
            AssertFrame.Text(referenceFrames[0], "text2modified", 11);
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
            newTree.OpenComponent<FakeComponent>(12);           //  2:   <FakeComponent>
            newTree.CloseComponent();                           //       </FakeComponent>
            newTree.OpenComponent<FakeComponent2>(13);          //  3:   <FakeComponent2>
            newTree.CloseComponent();                           //       </FakeComponent2>
            newTree.CloseElement();                             //     </container>

            // Act
            var renderBatch = GetRenderedBatch();

            // Assert
            var diff = renderBatch.UpdatedComponents.Single();
            Assert.Collection(diff.Edits,
                entry => AssertEdit(entry, RenderTreeEditType.StepIn, 1),
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependFrame, 0);
                    Assert.Equal(0, entry.ReferenceFrameIndex);
                },
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependFrame, 1);
                    Assert.Equal(1, entry.ReferenceFrameIndex);
                },
                entry => AssertEdit(entry, RenderTreeEditType.StepOut, 0));
            AssertFrame.ComponentWithInstance<FakeComponent>(renderBatch.ReferenceFrames.Array[0], 0, 12);
            AssertFrame.ComponentWithInstance<FakeComponent2>(renderBatch.ReferenceFrames.Array[1], 1, 13);
        }

        [Fact]
        public void SetsParametersOnChildComponents()
        {
            // Arrange
            var testObject = new object();
            newTree.OpenComponent<FakeComponent>(0);
            newTree.AddAttribute(1, nameof(FakeComponent.IntProperty), 123);
            newTree.AddAttribute(2, nameof(FakeComponent.StringProperty), "some string");
            newTree.AddAttribute(3, nameof(FakeComponent.ObjectProperty), testObject);
            newTree.CloseComponent();

            // Act
            var renderBatch = GetRenderedBatch();
            var componentInstance = newTree.GetFrames().First().Component as FakeComponent;

            // Assert
            Assert.Equal(1, renderBatch.UpdatedComponents.Count);
            var rootComponentDiff = renderBatch.UpdatedComponents.Array[0];
            AssertEdit(rootComponentDiff.Edits.Single(), RenderTreeEditType.PrependFrame, 0);
            Assert.NotNull(componentInstance);
            Assert.Equal(123, componentInstance.IntProperty);
            Assert.Equal("some string", componentInstance.StringProperty);
            Assert.Same(testObject, componentInstance.ObjectProperty);
        }

        [Fact]
        public void RetainsChildComponentsForExistingFrames()
        {
            // Arrange
            oldTree.AddText(10, "text1");                       //  0: text1
            oldTree.OpenElement(11, "container");               //  1: <container>
            oldTree.OpenComponent<FakeComponent>(12);           //  2:   <FakeComponent>
            oldTree.CloseComponent();                           //       </FakeComponent>
            oldTree.OpenComponent<FakeComponent2>(13);          //  3:   <FakeComponent2>
            oldTree.CloseComponent();                           //       </FakeComponent2>
            oldTree.CloseElement();                             //     </container>
            newTree.AddText(10, "text1");                       //  0: text1
            newTree.OpenElement(11, "container");               //  1: <container>
            newTree.OpenComponent<FakeComponent>(12);           //  2:   <FakeComponent>
            newTree.CloseComponent();                           //       </FakeComponent>
            newTree.OpenComponent<FakeComponent2>(13);          //  3:   <FakeComponent2>
            newTree.CloseComponent();                           //       </FakeComponent2>
            newTree.CloseElement();                             //     </container>

            RenderTreeDiffBuilder.ComputeDiff(renderer, new RenderBatchBuilder(), 0, new RenderTreeBuilder(renderer).GetFrames(), oldTree.GetFrames());
            var originalFakeComponentInstance = oldTree.GetFrames().Array[2].Component;
            var originalFakeComponent2Instance = oldTree.GetFrames().Array[3].Component;

            // Act
            var (result, referenceFrames) = GetSingleUpdatedComponent();
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
        public void SetsUpdatedParametersOnChildComponents()
        {
            // Arrange
            var objectWillNotChange = new object();
            oldTree.OpenComponent<FakeComponent>(12);
            oldTree.AddAttribute(13, nameof(FakeComponent.StringProperty), "String will change");
            oldTree.AddAttribute(14, nameof(FakeComponent.ObjectProperty), objectWillNotChange);
            oldTree.CloseComponent();
            newTree.OpenComponent<FakeComponent>(12);
            newTree.AddAttribute(13, nameof(FakeComponent.StringProperty), "String did change");
            newTree.AddAttribute(14, nameof(FakeComponent.ObjectProperty), objectWillNotChange);
            newTree.CloseComponent();

            RenderTreeDiffBuilder.ComputeDiff(renderer, new RenderBatchBuilder(), 0, new RenderTreeBuilder(renderer).GetFrames(), oldTree.GetFrames());
            var originalComponentInstance = (FakeComponent)oldTree.GetFrames().Array[0].Component;

            // Act
            var renderBatch = GetRenderedBatch();
            var newComponentInstance = (FakeComponent)oldTree.GetFrames().Array[0].Component;

            // Assert
            Assert.Equal(1, renderBatch.UpdatedComponents.Count); // Because the diff builder only queues child component renders; it doesn't actually perfom them itself
            Assert.Same(originalComponentInstance, newComponentInstance);
            Assert.Equal("String did change", newComponentInstance.StringProperty);
            Assert.Same(objectWillNotChange, newComponentInstance.ObjectProperty);
        }

        [Fact]
        public void QueuesRemovedChildComponentsForDisposal()
        {
            // Arrange
            oldTree.OpenComponent<DisposableComponent>(10);       // <DisposableComponent>
            oldTree.CloseComponent();                             // </DisposableComponent>
            oldTree.OpenComponent<NonDisposableComponent>(20);    // <NonDisposableComponent>
            oldTree.CloseComponent();                             // </NonDisposableComponent>
            oldTree.OpenComponent<DisposableComponent>(30);       // <DisposableComponent>
            oldTree.CloseComponent();                             // </DisposableComponent>
            newTree.OpenComponent<DisposableComponent>(30);       // <DisposableComponent>
            newTree.CloseComponent();                             // </DisposableComponent>

            var batchBuilder = new RenderBatchBuilder();
            RenderTreeDiffBuilder.ComputeDiff(renderer, batchBuilder, 0, new RenderTreeBuilder(renderer).GetFrames(), oldTree.GetFrames());

            // Act/Assert
            // Note that we track NonDisposableComponent was disposed even though it's not IDisposable,
            // because it's up to the upstream renderer to decide what "disposing" a component means
            Assert.Empty(batchBuilder.ComponentDisposalQueue);
            RenderTreeDiffBuilder.ComputeDiff(renderer, batchBuilder, 0, oldTree.GetFrames(), newTree.GetFrames());
            Assert.Equal(new[] { 0, 1 }, batchBuilder.ComponentDisposalQueue);
        }

        private (RenderTreeDiff, RenderTreeFrame[]) GetSingleUpdatedComponent(bool initializeFromFrames = false)
        {
            var batch = GetRenderedBatch(initializeFromFrames);
            var diffsInBatch = batch.UpdatedComponents;
            Assert.Equal(1, diffsInBatch.Count);
            return (diffsInBatch.Array[0], batch.ReferenceFrames.ToArray());
        }

        private RenderBatch GetRenderedBatch(bool initializeFromFrames = false)
            => GetRenderedBatch(oldTree, newTree, initializeFromFrames);

        private RenderBatch GetRenderedBatch(RenderTreeBuilder from, RenderTreeBuilder to, bool initializeFromFrames)
        {
            if (initializeFromFrames)
            {
                var emptyFrames = new RenderTreeBuilder(renderer).GetFrames();
                var oldFrames = from.GetFrames();
                RenderTreeDiffBuilder.ComputeDiff(renderer, new RenderBatchBuilder(), 0, emptyFrames, oldFrames);
            }

            var batchBuilder = new RenderBatchBuilder();
            var diff = RenderTreeDiffBuilder.ComputeDiff(renderer, batchBuilder, 0, from.GetFrames(), to.GetFrames());
            batchBuilder.UpdatedComponentDiffs.Append(diff);
            return batchBuilder.ToBatch();
        }

        private class FakeRenderer : Renderer
        {
            protected override void UpdateDisplay(RenderBatch renderBatch)
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

            public void Init(RenderHandle renderHandle) { }
            public void SetParameters(ParameterCollection parameters)
            {
                parameters.AssignToProperties(this);
            }
            public void BuildRenderTree(RenderTreeBuilder builder) { }
        }

        private class FakeComponent2 : IComponent
        {
            public void Init(RenderHandle renderHandle)
            {
            }

            public void SetParameters(ParameterCollection parameters)
            {
            }

            public void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.AddText(100, $"Hello from {nameof(FakeComponent2)}");
            }
        }

        private class DisposableComponent : IComponent, IDisposable
        {
            public int DisposalCount { get; private set; }
            public void Dispose() => DisposalCount++;

            public void Init(RenderHandle renderHandle) { }

            public void SetParameters(ParameterCollection parameters) { }

            public void BuildRenderTree(RenderTreeBuilder builder) { }
        }

        private class NonDisposableComponent : IComponent
        {
            public void Init(RenderHandle renderHandle) { }

            public void SetParameters(ParameterCollection parameters) { }

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
