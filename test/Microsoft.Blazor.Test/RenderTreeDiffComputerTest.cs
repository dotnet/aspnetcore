// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Components;
using Microsoft.Blazor.Rendering;
using Microsoft.Blazor.RenderTree;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.Blazor.Test
{
    public class RenderTreeDiffComputerTest
    {
        [Theory]
        [MemberData(nameof(RecognizesEquivalentNodesAsSameCases))]
        public void RecognizesEquivalentNodesAsSame(Action<RenderTreeBuilder> appendAction)
        {
            // Arrange
            var oldTree = new RenderTreeBuilder(new FakeRenderer());
            var newTree = new RenderTreeBuilder(new FakeRenderer());
            var diff = new RenderTreeDiffComputer();
            appendAction(oldTree);
            appendAction(newTree);

            // Act
            var result = diff.ComputeDifference(oldTree.GetNodes(), newTree.GetNodes());

            // Assert
            Assert.Empty(result.Edits);
        }

        public static IEnumerable<object[]> RecognizesEquivalentNodesAsSameCases()
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
                builder => builder.AddComponent<FakeComponent>(0)
            }.Select(x => new object[] { x });

        [Fact]
        public void RecognizesNewItemsBeingInserted()
        {
            // Arrange
            var oldTree = new RenderTreeBuilder(new FakeRenderer());
            var newTree = new RenderTreeBuilder(new FakeRenderer());
            var diff = new RenderTreeDiffComputer();
            oldTree.AddText(0, "text0");
            oldTree.AddText(2, "text2");
            newTree.AddText(0, "text0");
            newTree.AddText(1, "text1");
            newTree.AddText(2, "text2");

            // Act
            var result = diff.ComputeDifference(oldTree.GetNodes(), newTree.GetNodes());

            // Assert
            Assert.Collection(result.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependNode, 1);
                    Assert.Equal(1, entry.NewTreeIndex);
                });
        }

        [Fact]
        public void RecognizesOldItemsBeingRemoved()
        {
            // Arrange
            var oldTree = new RenderTreeBuilder(new FakeRenderer());
            var newTree = new RenderTreeBuilder(new FakeRenderer());
            var diff = new RenderTreeDiffComputer();
            oldTree.AddText(0, "text0");
            oldTree.AddText(1, "text1");
            oldTree.AddText(2, "text2");
            newTree.AddText(0, "text0");
            newTree.AddText(2, "text2");

            // Act
            var result = diff.ComputeDifference(oldTree.GetNodes(), newTree.GetNodes());

            // Assert
            Assert.Collection(result.Edits,
                entry => AssertEdit(entry, RenderTreeEditType.RemoveNode, 1));
        }

        [Fact]
        public void RecognizesTrailingSequenceWithinLoopBlockBeingRemoved()
        {
            // Arrange
            var oldTree = new RenderTreeBuilder(new FakeRenderer());
            var newTree = new RenderTreeBuilder(new FakeRenderer());
            var diff = new RenderTreeDiffComputer();
            oldTree.AddText(0, "x"); // Loop start
            oldTree.AddText(1, "x"); // Will be removed
            oldTree.AddText(2, "x"); // Will be removed
            oldTree.AddText(0, "x"); // Loop start
            newTree.AddText(0, "x"); // Loop start
            newTree.AddText(0, "x"); // Loop start

            // Act
            var result = diff.ComputeDifference(oldTree.GetNodes(), newTree.GetNodes());

            // Assert
            Assert.Collection(result.Edits,
                entry => AssertEdit(entry, RenderTreeEditType.RemoveNode, 1),
                entry => AssertEdit(entry, RenderTreeEditType.RemoveNode, 1));
        }

        [Fact]
        public void RecognizesTrailingSequenceWithinLoopBlockBeingAppended()
        {
            // Arrange
            var oldTree = new RenderTreeBuilder(new FakeRenderer());
            var newTree = new RenderTreeBuilder(new FakeRenderer());
            var diff = new RenderTreeDiffComputer();
            oldTree.AddText(0, "x"); // Loop start
            oldTree.AddText(0, "x"); // Loop start
            newTree.AddText(0, "x"); // Loop start
            newTree.AddText(1, "x"); // Will be added
            newTree.AddText(2, "x"); // Will be added
            newTree.AddText(0, "x"); // Loop start

            // Act
            var result = diff.ComputeDifference(oldTree.GetNodes(), newTree.GetNodes());

            // Assert
            Assert.Collection(result.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependNode, 1);
                    Assert.Equal(1, entry.NewTreeIndex);
                },
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependNode, 2);
                    Assert.Equal(2, entry.NewTreeIndex);
                });
        }

        [Fact]
        public void RecognizesTrailingLoopBlockBeingRemoved()
        {
            // Arrange
            var oldTree = new RenderTreeBuilder(new FakeRenderer());
            var newTree = new RenderTreeBuilder(new FakeRenderer());
            var diff = new RenderTreeDiffComputer();
            oldTree.AddText(0, "x");
            oldTree.AddText(1, "x");
            oldTree.AddText(0, "x"); // Will be removed
            oldTree.AddText(1, "x"); // Will be removed
            newTree.AddText(0, "x");
            newTree.AddText(1, "x");

            // Act
            var result = diff.ComputeDifference(oldTree.GetNodes(), newTree.GetNodes());

            // Assert
            Assert.Collection(result.Edits,
                entry => AssertEdit(entry, RenderTreeEditType.RemoveNode, 2),
                entry => AssertEdit(entry, RenderTreeEditType.RemoveNode, 2));
        }

        [Fact]
        public void RecognizesTrailingLoopBlockBeingAdded()
        {
            // Arrange
            var oldTree = new RenderTreeBuilder(new FakeRenderer());
            var newTree = new RenderTreeBuilder(new FakeRenderer());
            var diff = new RenderTreeDiffComputer();
            oldTree.AddText(0, "x");
            oldTree.AddText(1, "x");
            newTree.AddText(0, "x");
            newTree.AddText(1, "x");
            newTree.AddText(0, "x"); // Will be added
            newTree.AddText(1, "x"); // Will be added

            // Act
            var result = diff.ComputeDifference(oldTree.GetNodes(), newTree.GetNodes());

            // Assert
            Assert.Collection(result.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependNode, 2);
                    Assert.Equal(2, entry.NewTreeIndex);
                },
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependNode, 3);
                    Assert.Equal(3, entry.NewTreeIndex);
                });
        }

        [Fact]
        public void RecognizesLeadingLoopBlockItemsBeingAdded()
        {
            // Arrange
            var oldTree = new RenderTreeBuilder(new FakeRenderer());
            var newTree = new RenderTreeBuilder(new FakeRenderer());
            var diff = new RenderTreeDiffComputer();
            oldTree.AddText(2, "x");
            oldTree.AddText(2, "x"); // Note that the '0' and '1' items are not present on this iteration
            newTree.AddText(2, "x");
            newTree.AddText(0, "x");
            newTree.AddText(1, "x");
            newTree.AddText(2, "x");

            // Act
            var result = diff.ComputeDifference(oldTree.GetNodes(), newTree.GetNodes());

            // Assert
            Assert.Collection(result.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependNode, 1);
                    Assert.Equal(1, entry.NewTreeIndex);
                },
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependNode, 2);
                    Assert.Equal(2, entry.NewTreeIndex);
                });
        }

        [Fact]
        public void RecognizesLeadingLoopBlockItemsBeingRemoved()
        {
            // Arrange
            var oldTree = new RenderTreeBuilder(new FakeRenderer());
            var newTree = new RenderTreeBuilder(new FakeRenderer());
            var diff = new RenderTreeDiffComputer();
            oldTree.AddText(2, "x");
            oldTree.AddText(0, "x");
            oldTree.AddText(1, "x");
            oldTree.AddText(2, "x");
            newTree.AddText(2, "x");
            newTree.AddText(2, "x"); // Note that the '0' and '1' items are not present on this iteration

            // Act
            var result = diff.ComputeDifference(oldTree.GetNodes(), newTree.GetNodes());

            // Assert
            Assert.Collection(result.Edits,
                entry => AssertEdit(entry, RenderTreeEditType.RemoveNode, 1),
                entry => AssertEdit(entry, RenderTreeEditType.RemoveNode, 1));
        }

        [Fact]
        public void HandlesAdjacentItemsBeingRemovedAndInsertedAtOnce()
        {
            // Arrange
            var oldTree = new RenderTreeBuilder(new FakeRenderer());
            var newTree = new RenderTreeBuilder(new FakeRenderer());
            var diff = new RenderTreeDiffComputer();
            oldTree.AddText(0, "text");
            newTree.AddText(1, "text");

            // Act
            var result = diff.ComputeDifference(oldTree.GetNodes(), newTree.GetNodes());

            // Assert
            Assert.Collection(result.Edits,
                entry => AssertEdit(entry, RenderTreeEditType.RemoveNode, 0),
                entry => AssertEdit(entry, RenderTreeEditType.PrependNode, 0));
        }

        [Fact]
        public void RecognizesTextUpdates()
        {
            // Arrange
            var oldTree = new RenderTreeBuilder(new FakeRenderer());
            var newTree = new RenderTreeBuilder(new FakeRenderer());
            var diff = new RenderTreeDiffComputer();
            oldTree.AddText(123, "old text 1");
            oldTree.AddText(182, "old text 2");
            newTree.AddText(123, "new text 1");
            newTree.AddText(182, "new text 2");

            // Act
            var result = diff.ComputeDifference(oldTree.GetNodes(), newTree.GetNodes());

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
            var oldTree = new RenderTreeBuilder(new FakeRenderer());
            var newTree = new RenderTreeBuilder(new FakeRenderer());
            var diff = new RenderTreeDiffComputer();
            oldTree.OpenElement(123, "old element");
            oldTree.CloseElement();
            newTree.OpenElement(123, "new element");
            newTree.CloseElement();

            // Act
            var result = diff.ComputeDifference(oldTree.GetNodes(), newTree.GetNodes());

            // Assert
            Assert.Collection(result.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependNode, 0);
                    Assert.Equal(0, entry.NewTreeIndex);
                },
                entry => AssertEdit(entry, RenderTreeEditType.RemoveNode, 1));
        }

        [Fact]
        public void RecognizesComponentTypeChangesAtSameSequenceNumber()
        {
            // Arrange
            var oldTree = new RenderTreeBuilder(new FakeRenderer());
            var newTree = new RenderTreeBuilder(new FakeRenderer());
            var diff = new RenderTreeDiffComputer();
            oldTree.AddComponent<FakeComponent>(123);
            newTree.AddComponent<FakeComponent2>(123);

            // Act
            var result = diff.ComputeDifference(oldTree.GetNodes(), newTree.GetNodes());

            // Assert
            Assert.Collection(result.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.PrependNode, 0);
                    Assert.Equal(0, entry.NewTreeIndex);
                },
                entry => AssertEdit(entry, RenderTreeEditType.RemoveNode, 1));
        }

        [Fact]
        public void RecognizesAttributesAdded()
        {
            // Arrange
            var oldTree = new RenderTreeBuilder(new FakeRenderer());
            var newTree = new RenderTreeBuilder(new FakeRenderer());
            var diff = new RenderTreeDiffComputer();
            oldTree.OpenElement(0, "My element");
            oldTree.AddAttribute(1, "existing", "existing value");
            oldTree.CloseElement();
            newTree.OpenElement(0, "My element");
            newTree.AddAttribute(1, "existing", "existing value");
            newTree.AddAttribute(2, "added", "added value");
            newTree.CloseElement();

            // Act
            var result = diff.ComputeDifference(oldTree.GetNodes(), newTree.GetNodes());

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
            var oldTree = new RenderTreeBuilder(new FakeRenderer());
            var newTree = new RenderTreeBuilder(new FakeRenderer());
            var diff = new RenderTreeDiffComputer();
            oldTree.OpenElement(0, "My element");
            oldTree.AddAttribute(1, "will be removed", "will be removed value");
            oldTree.AddAttribute(2, "will survive", "surviving value");
            oldTree.CloseElement();
            newTree.OpenElement(0, "My element");
            newTree.AddAttribute(2, "will survive", "surviving value");
            newTree.CloseElement();

            // Act
            var result = diff.ComputeDifference(oldTree.GetNodes(), newTree.GetNodes());

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
            var oldTree = new RenderTreeBuilder(new FakeRenderer());
            var newTree = new RenderTreeBuilder(new FakeRenderer());
            var diff = new RenderTreeDiffComputer();
            oldTree.OpenElement(0, "My element");
            oldTree.AddAttribute(1, "will remain", "will remain value");
            oldTree.AddAttribute(2, "will change", "will change value");
            oldTree.CloseElement();
            newTree.OpenElement(0, "My element");
            newTree.AddAttribute(1, "will remain", "will remain value");
            newTree.AddAttribute(2, "will change", "did change value");
            newTree.CloseElement();

            // Act
            var result = diff.ComputeDifference(oldTree.GetNodes(), newTree.GetNodes());

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
            var oldTree = new RenderTreeBuilder(new FakeRenderer());
            var newTree = new RenderTreeBuilder(new FakeRenderer());
            var diff = new RenderTreeDiffComputer();
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
            var result = diff.ComputeDifference(oldTree.GetNodes(), newTree.GetNodes());

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
            var oldTree = new RenderTreeBuilder(new FakeRenderer());
            var newTree = new RenderTreeBuilder(new FakeRenderer());
            var diff = new RenderTreeDiffComputer();
            oldTree.OpenElement(0, "My element");
            oldTree.AddAttribute(1, "oldname", "same value");
            oldTree.CloseElement();
            newTree.OpenElement(0, "My element");
            newTree.AddAttribute(1, "newname", "same value");
            newTree.CloseElement();

            // Act
            var result = diff.ComputeDifference(oldTree.GetNodes(), newTree.GetNodes());

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
            var oldTree = new RenderTreeBuilder(new FakeRenderer());
            var newTree = new RenderTreeBuilder(new FakeRenderer());
            var diff = new RenderTreeDiffComputer();
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
            var result = diff.ComputeDifference(oldTree.GetNodes(), newTree.GetNodes());

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
            var oldTree = new RenderTreeBuilder(new FakeRenderer());
            var newTree = new RenderTreeBuilder(new FakeRenderer());
            var diff = new RenderTreeDiffComputer();
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
            var result = diff.ComputeDifference(oldTree.GetNodes(), newTree.GetNodes());

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
            var oldTree = new RenderTreeBuilder(new FakeRenderer());
            var newTree = new RenderTreeBuilder(new FakeRenderer());
            var diff = new RenderTreeDiffComputer();
            oldTree.AddText(10, "text1");
            oldTree.AddText(11, "text2");
            oldTree.AddText(12, "text3");
            oldTree.AddText(13, "text4");
            newTree.AddText(10, "text1");
            newTree.AddText(11, "text2modified");
            newTree.AddText(12, "text3");
            newTree.AddText(13, "text4");

            // Act
            var result = diff.ComputeDifference(oldTree.GetNodes(), newTree.GetNodes());

            // Assert
            Assert.Collection(result.Edits,
                entry =>
                {
                    AssertEdit(entry, RenderTreeEditType.UpdateText, 1);
                    Assert.Equal(1, entry.NewTreeIndex);
                });
        }

        private class FakeRenderer : Renderer
        {
            internal protected override void UpdateDisplay(int componentId, RenderTreeDiff renderTreeDiff)
                => throw new NotImplementedException();
        }

        private class FakeComponent : IComponent
        {
            public void BuildRenderTree(RenderTreeBuilder builder)
                => throw new NotImplementedException();
        }

        private class FakeComponent2 : IComponent
        {
            public void BuildRenderTree(RenderTreeBuilder builder)
                => throw new NotImplementedException();
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
