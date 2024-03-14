// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components.Test;

public class RenderTreeDiffBuilderTest : IDisposable
{
    private readonly Renderer renderer;
    private readonly RenderTreeBuilder oldTree;
    private readonly RenderTreeBuilder newTree;
    private RenderBatchBuilder batchBuilder;

    public RenderTreeDiffBuilderTest()
    {
        renderer = new FakeRenderer();
        oldTree = new RenderTreeBuilder();
        newTree = new RenderTreeBuilder();
    }

    void IDisposable.Dispose()
    {
        renderer.Dispose();
        ((IDisposable)oldTree).Dispose();
        ((IDisposable)newTree).Dispose();
        batchBuilder?.Dispose();
    }

    [Theory]
    [MemberData(nameof(RecognizesEquivalentFramesAsSameCases))]
    public void RecognizesEquivalentFramesAsSame(RenderFragment appendFragment)
    {
        // Arrange
        appendFragment(oldTree);
        appendFragment(newTree);

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent(initializeFromFrames: true);

        // Assert
        Assert.Empty(result.Edits);
    }

    public static IEnumerable<object[]> RecognizesEquivalentFramesAsSameCases()
        => new RenderFragment[]
        {
                builder => builder.AddContent(0, "Hello"),
                builder =>
                {
                    builder.OpenElement(0, "Some Element");
                    builder.CloseElement();
                },
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
        oldTree.AddContent(0, "text0");
        oldTree.AddContent(2, "text2");
        newTree.AddContent(0, "text0");
        newTree.AddContent(1, "text1");
        newTree.AddContent(2, "text2");

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
        oldTree.AddContent(0, "text0");
        oldTree.AddContent(1, "text1");
        oldTree.AddContent(2, "text2");
        newTree.AddContent(0, "text0");
        newTree.AddContent(2, "text2");

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(result.Edits,
            entry => AssertEdit(entry, RenderTreeEditType.RemoveFrame, 1));
    }

    [Fact]
    public void RecognizesKeyedElementInsertions()
    {
        // Arrange
        oldTree.OpenElement(0, "container");
        oldTree.SetKey("retained key");
        oldTree.AddContent(1, "Existing");
        oldTree.CloseElement();

        newTree.OpenElement(0, "container");
        newTree.SetKey("new key");
        newTree.AddContent(1, "Inserted");
        newTree.CloseElement();

        newTree.OpenElement(0, "container");
        newTree.SetKey("retained key");
        newTree.AddContent(1, "Existing");
        newTree.CloseElement();

        // Without the key, it would change the text "Existing" to "Inserted", then insert a new "Existing" below it
        // With the key, it just inserts a new "Inserted" at the top

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(result.Edits,
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.PrependFrame, 0);
                Assert.Equal(0, entry.ReferenceFrameIndex);
                Assert.Equal("new key", referenceFrames[entry.ReferenceFrameIndex].ElementKey);
            });
    }

    [Fact]
    public void RecognizesKeyedElementDeletions()
    {
        // Arrange
        oldTree.OpenElement(0, "container");
        oldTree.SetKey("will delete");
        oldTree.AddContent(1, "First");
        oldTree.CloseElement();

        oldTree.OpenElement(0, "container");
        oldTree.SetKey("will retain");
        oldTree.AddContent(1, "Second");
        oldTree.CloseElement();

        newTree.OpenElement(0, "container");
        newTree.SetKey("will retain");
        newTree.AddContent(1, "Second");
        newTree.CloseElement();

        // Without the key, it changes the text content of "First" to "Second", then deletes the other "Second"
        // With the key, it just deletes "First"

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(result.Edits,
            entry => AssertEdit(entry, RenderTreeEditType.RemoveFrame, 0));
    }

    [Fact]
    public void RecognizesSimultaneousKeyedElementInsertionsAndDeletions()
    {
        // Arrange
        oldTree.OpenElement(0, "container");
        oldTree.SetKey("original key");
        oldTree.AddContent(1, "Original");
        oldTree.CloseElement();

        newTree.OpenElement(0, "container");
        newTree.SetKey("new key");
        newTree.AddContent(1, "Inserted");
        newTree.CloseElement();

        // Without the key, it would change the text "Original" to "Inserted"
        // With the key, it deletes the old element and inserts the new element

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(result.Edits,
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.PrependFrame, 0);
                Assert.Equal(0, entry.ReferenceFrameIndex);
                Assert.Equal("new key", referenceFrames[entry.ReferenceFrameIndex].ElementKey);
            },
            entry => AssertEdit(entry, RenderTreeEditType.RemoveFrame, 1));
    }

    [Fact]
    public void RecognizesKeyedComponentInsertions()
    {
        // Arrange
        oldTree.OpenComponent<CaptureSetParametersComponent>(0);
        oldTree.SetKey("retained key");
        oldTree.AddComponentParameter(1, "ParamName", "Param old value");
        oldTree.CloseComponent();
        using var initial = new RenderTreeBuilder();
        GetRenderedBatch(initial, oldTree, false); // Assign initial IDs
        var oldComponent = GetComponents<CaptureSetParametersComponent>(oldTree).Single();

        newTree.OpenComponent<CaptureSetParametersComponent>(0);
        newTree.SetKey("new key");
        newTree.AddComponentParameter(1, "ParamName", "New component param value");
        newTree.CloseComponent();

        newTree.OpenComponent<CaptureSetParametersComponent>(0);
        newTree.SetKey("retained key");
        newTree.AddComponentParameter(1, "ParamName", "Param new value");
        newTree.CloseComponent();

        // Without the key, it would modify the param on the first component,
        // then insert a new second component.
        // With the key, it inserts a new first component, then modifies the
        // param on the second component.

        // Act
        var batchBuilder = GetRenderedBatch(initializeFromFrames: false);
        var newComponents = GetComponents<CaptureSetParametersComponent>(newTree);

        // Assert: Inserts new component at position 0
        Assert.Equal(1, batchBuilder.UpdatedComponents.Count);
        Assert.Collection(batchBuilder.UpdatedComponents.Array[0].Edits,
            entry => AssertEdit(entry, RenderTreeEditType.PrependFrame, 0));

        // Assert: Retains old component instance in position 1, and updates its params
        Assert.Same(oldComponent, newComponents[1]);
        Assert.Equal(2, oldComponent.SetParametersCallCount);
    }

    [Fact]
    public void RecognizesKeyedComponentDeletions()
    {
        // Arrange
        oldTree.OpenComponent<FakeComponent>(0);
        oldTree.SetKey("will delete");
        oldTree.AddComponentParameter(1, nameof(FakeComponent.StringProperty), "Anything");
        oldTree.CloseComponent();

        oldTree.OpenComponent<FakeComponent>(0);
        oldTree.SetKey("will retain");
        oldTree.AddComponentParameter(1, nameof(FakeComponent.StringProperty), "Retained param value");
        oldTree.CloseComponent();

        // Instantiate initial components
        using var initial = new RenderTreeBuilder();
        GetRenderedBatch(initial, oldTree, false);
        var oldComponents = GetComponents(oldTree);

        newTree.OpenComponent<FakeComponent>(0);
        newTree.SetKey("will retain");
        newTree.AddComponentParameter(1, nameof(FakeComponent.StringProperty), "Retained param value");
        newTree.CloseComponent();

        // Without the key, it updates the param on the first component, then
        // deletes the second.
        // With the key, it just deletes the first.

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();
        var newComponent = GetComponents(newTree).Single();

        // Assert
        Assert.Same(oldComponents[1], newComponent);
        Assert.Collection(result.Edits,
            entry => AssertEdit(entry, RenderTreeEditType.RemoveFrame, 0));
    }

    [Fact]
    public void RecognizesKeyedComponentDeletionsBeforeUnchangedNonKeyedComponent()
    {
        // Arrange
        oldTree.OpenComponent<FakeComponent>(0);
        oldTree.SetKey("will delete");
        oldTree.AddComponentParameter(1, nameof(FakeComponent.StringProperty), "Will delete");
        oldTree.CloseComponent();

        oldTree.OpenComponent<FakeComponent>(2);
        oldTree.AddComponentParameter(3, nameof(FakeComponent.StringProperty), "Retained param value");
        oldTree.CloseComponent();

        // Instantiate initial components
        using var initial = new RenderTreeBuilder();
        GetRenderedBatch(initial, oldTree, false);
        var oldComponents = GetComponents(oldTree);

        newTree.OpenComponent<FakeComponent>(2);
        newTree.AddComponentParameter(3, nameof(FakeComponent.StringProperty), "Retained param value");
        newTree.CloseComponent();

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();
        var newComponent = GetComponents(newTree).Single();

        // Assert
        Assert.Same(oldComponents[1], newComponent);
        Assert.Collection(result.Edits,
            entry => AssertEdit(entry, RenderTreeEditType.RemoveFrame, 0));
    }

    [Fact]
    public void RecognizesKeyedComponentInsertionsBeforeUnchangedNonKeyedComponent()
    {
        // Arrange
        oldTree.OpenComponent<FakeComponent>(1);
        oldTree.CloseComponent();

        // Instantiate initial components
        using var initial = new RenderTreeBuilder();
        GetRenderedBatch(initial, oldTree, false);
        var oldComponents = GetComponents(oldTree);

        newTree.OpenComponent<FakeComponent>(0);
        newTree.SetKey("will insert");
        newTree.CloseComponent();

        newTree.OpenComponent<FakeComponent>(1);
        newTree.CloseComponent();

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();
        var newComponents = GetComponents(newTree);

        // Assert
        Assert.Same(oldComponents[0], newComponents[1]);
        Assert.Collection(result.Edits,
            entry => AssertEdit(entry, RenderTreeEditType.PrependFrame, 0));
    }

    [Fact]
    public void RecognizesSimultaneousKeyedComponentInsertionsAndDeletions()
    {
        // Arrange
        oldTree.OpenComponent<FakeComponent>(0);
        oldTree.SetKey("original key");
        oldTree.CloseComponent();

        // Instantiate initial component
        using var renderTreeBuilder = new RenderTreeBuilder();
        GetRenderedBatch(renderTreeBuilder, oldTree, false);
        var oldComponent = GetComponents(oldTree).Single();
        Assert.NotNull(oldComponent);

        newTree.OpenComponent<FakeComponent>(0);
        newTree.SetKey("new key");
        newTree.CloseComponent();

        // Without the key, it would retain the component
        // With the key, it deletes the old component and inserts the new component

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();
        var newComponent = GetComponents(newTree).Single();

        // Assert
        Assert.NotNull(newComponent);
        Assert.NotSame(oldComponent, newComponent);
        Assert.Collection(result.Edits,
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.PrependFrame, 0);
                Assert.Equal(0, entry.ReferenceFrameIndex);
                Assert.Equal("new key", referenceFrames[entry.ReferenceFrameIndex].ComponentKey);
            },
            entry => AssertEdit(entry, RenderTreeEditType.RemoveFrame, 1));
    }

    [Fact]
    public void RejectsClashingKeysInOldTree()
    {
        // Arrange
        AddWithKey(oldTree, "key1", "attrib1a");
        AddWithKey(oldTree, "key2", "attrib2");
        AddWithKey(oldTree, "key1", "attrib3");

        AddWithKey(newTree, "key1", "attrib1b");
        AddWithKey(newTree, "key2", "attrib2");
        AddWithKey(newTree, "key3", "attrib3");

        // Act/Assert
        var ex = Assert.Throws<InvalidOperationException>(() => GetSingleUpdatedComponent());
        Assert.Equal("More than one sibling of element 'el' has the same key value, 'key1'. Key values must be unique.", ex.Message);
    }

    [Fact]
    public void RejectsClashingKeysInNewTree()
    {
        // Arrange
        AddWithKey(oldTree, "key1", "attrib1a");
        AddWithKey(oldTree, "key2", "attrib2");
        AddWithKey(oldTree, "key3", "attrib3");

        AddWithKey(newTree, "key1", "attrib1b");
        AddWithKey(newTree, "key2", "attrib2");
        AddWithKey(newTree, "key1", "attrib3");

        // Act/Assert
        var ex = Assert.Throws<InvalidOperationException>(() => GetSingleUpdatedComponent());
        Assert.Equal("More than one sibling of element 'el' has the same key value, 'key1'. Key values must be unique.", ex.Message);
    }

    [Fact]
    public void RejectsClashingKeysEvenIfAllPairsMatch()
    {
        // This sort of scenario would happen if you accidentally used a constant value for @key

        // Arrange
        AddWithKey(oldTree, "key1", "attrib1a");
        AddWithKey(oldTree, "key1", "attrib1b");

        AddWithKey(newTree, "key1", "attrib1a");
        AddWithKey(newTree, "key1", "attrib1b");

        // Act/Assert
        var ex = Assert.Throws<InvalidOperationException>(() => GetSingleUpdatedComponent());
        Assert.Equal("More than one sibling of element 'el' has the same key value, 'key1'. Key values must be unique.", ex.Message);
    }

    [Fact]
    public void HandlesInsertionOfUnkeyedItemsAroundKey()
    {
        // The fact that the new sequence numbers are descending makes this
        // problematic if it prefers matching by sequence over key.
        // However, since the policy is to prefer key over sequence, it works OK.

        // Arrange
        oldTree.OpenElement(1, "el");
        oldTree.SetKey("some key");
        oldTree.CloseElement();

        newTree.OpenElement(2, "other");
        newTree.CloseElement();

        newTree.OpenElement(1, "el");
        newTree.SetKey("some key");
        newTree.CloseElement();

        newTree.OpenElement(0, "other 2");
        newTree.CloseElement();

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(result.Edits,
            edit => AssertEdit(edit, RenderTreeEditType.PrependFrame, 0),
            edit => AssertEdit(edit, RenderTreeEditType.PrependFrame, 2));
    }

    [Fact]
    public void HandlesDeletionOfUnkeyedItemsAroundKey()
    {
        // The fact that the old sequence numbers are descending makes this
        // problematic if it prefers matching by sequence over key.
        // However, since the policy is to prefer key over sequence, it works OK.

        // Arrange
        oldTree.OpenElement(2, "other");
        oldTree.CloseElement();

        oldTree.OpenElement(1, "el");
        oldTree.SetKey("some key");
        oldTree.CloseElement();

        oldTree.OpenElement(0, "other 2");
        oldTree.CloseElement();

        newTree.OpenElement(1, "el");
        newTree.SetKey("some key");
        newTree.CloseElement();

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(result.Edits,
            edit => AssertEdit(edit, RenderTreeEditType.RemoveFrame, 0),
            edit => AssertEdit(edit, RenderTreeEditType.RemoveFrame, 1));
    }

    [Fact]
    public void HandlesKeyBeingAdded()
    {
        // This is an anomalous situation that can't occur with .razor components.
        // It represents the case where, for the same sequence number, we have an
        // old frame without a key and a new frame with a key.

        // Arrange
        oldTree.OpenElement(0, "el");
        oldTree.CloseElement();

        newTree.OpenElement(0, "el");
        newTree.SetKey("some key");
        newTree.CloseElement();

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(result.Edits,
            // Insert new
            edit =>
            {
                AssertEdit(edit, RenderTreeEditType.PrependFrame, 0);
                Assert.Equal("some key", referenceFrames[edit.ReferenceFrameIndex].ElementKey);
            },
            // Delete old
            edit => AssertEdit(edit, RenderTreeEditType.RemoveFrame, 1));
    }

    [Fact]
    public void HandlesKeyBeingRemoved()
    {
        // This is an anomalous situation that can't occur with .razor components.
        // It represents the case where, for the same sequence number, we have an
        // old frame with a key and a new frame without a key.

        // Arrange
        oldTree.OpenElement(0, "el");
        oldTree.SetKey("some key");
        oldTree.CloseElement();

        newTree.OpenElement(0, "el");
        newTree.CloseElement();

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(result.Edits,
            // Insert new
            edit => AssertEdit(edit, RenderTreeEditType.RemoveFrame, 0),
            // Delete old
            edit => AssertEdit(edit, RenderTreeEditType.PrependFrame, 0));
    }

    [Fact]
    public void RecognizesTrailingSequenceWithinLoopBlockBeingRemoved()
    {
        // Arrange
        oldTree.AddContent(0, "x"); // Loop start
        oldTree.AddContent(1, "x"); // Will be removed
        oldTree.AddContent(2, "x"); // Will be removed
        oldTree.AddContent(0, "x"); // Loop start
        newTree.AddContent(0, "x"); // Loop start
        newTree.AddContent(0, "x"); // Loop start

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
        oldTree.AddContent(10, "x"); // Loop start
        oldTree.AddContent(10, "x"); // Loop start
        newTree.AddContent(10, "x"); // Loop start
        newTree.AddContent(11, "x"); // Will be added
        newTree.AddContent(12, "x"); // Will be added
        newTree.AddContent(10, "x"); // Loop start

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
        oldTree.AddContent(0, "x");
        oldTree.AddContent(1, "x");
        oldTree.AddContent(0, "x"); // Will be removed
        oldTree.AddContent(1, "x"); // Will be removed
        newTree.AddContent(0, "x");
        newTree.AddContent(1, "x");

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
        oldTree.AddContent(10, "x");
        oldTree.AddContent(11, "x");
        newTree.AddContent(10, "x");
        newTree.AddContent(11, "x");
        newTree.AddContent(10, "x"); // Will be added
        newTree.AddContent(11, "x"); // Will be added

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
        oldTree.AddContent(12, "x");
        oldTree.AddContent(12, "x"); // Note that the '0' and '1' items are not present on this iteration
        newTree.AddContent(12, "x");
        newTree.AddContent(10, "x");
        newTree.AddContent(11, "x");
        newTree.AddContent(12, "x");

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
        oldTree.AddContent(2, "x");
        oldTree.AddContent(0, "x");
        oldTree.AddContent(1, "x");
        oldTree.AddContent(2, "x");
        newTree.AddContent(2, "x");
        newTree.AddContent(2, "x"); // Note that the '0' and '1' items are not present on this iteration

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
        oldTree.AddContent(0, "text");
        newTree.AddContent(1, "text");

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
        oldTree.AddContent(123, "old text 1");
        oldTree.AddContent(182, "old text 2");
        newTree.AddContent(123, "new text 1");
        newTree.AddContent(182, "new text 2");

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
    public void RecognizesMarkupChanges()
    {
        // Arrange
        oldTree.AddMarkupContent(1, "preserved");
        oldTree.AddMarkupContent(3, "will be updated");
        oldTree.AddMarkupContent(4, "will be removed");
        newTree.AddMarkupContent(1, "preserved");
        newTree.AddMarkupContent(2, "was inserted");
        newTree.AddMarkupContent(3, "was updated");

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(result.Edits,
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.PrependFrame, 1);
                Assert.Equal(0, entry.ReferenceFrameIndex);
                Assert.Equal("was inserted", referenceFrames[entry.ReferenceFrameIndex].MarkupContent);
            },
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.UpdateMarkup, 2);
                Assert.Equal(1, entry.ReferenceFrameIndex);
                Assert.Equal("was updated", referenceFrames[entry.ReferenceFrameIndex].MarkupContent);
            },
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.RemoveFrame, 3);
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
        using var initial = new RenderTreeBuilder();
        GetRenderedBatch(initial, oldTree, false); // Assign initial IDs
        newTree.OpenComponent<FakeComponent2>(123);
        newTree.CloseComponent();
        using var batchBuilder = new RenderBatchBuilder();

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
        Action<EventArgs> retainedHandler = _ => { };
        Action<EventArgs> removedHandler = _ => { };
        Action<EventArgs> addedHandler = _ => { };
        oldTree.OpenElement(0, "My element");
        oldTree.AddAttribute(1, "onfoo", retainedHandler);
        oldTree.AddAttribute(2, "onbar", removedHandler);
        oldTree.CloseElement();
        newTree.OpenElement(0, "My element");
        newTree.AddAttribute(1, "onfoo", retainedHandler);
        newTree.AddAttribute(2, "onbar", addedHandler);
        newTree.CloseElement();

        // Act
        var (result, referenceFrames, batchBuilder) = GetSingleUpdatedComponentWithBatch(initializeFromFrames: true);
        var removedEventHandlerFrame = oldTree.GetFrames().Array[2];

        // Assert
        Assert.Collection(result.Edits,
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.SetAttribute, 0);
                Assert.Equal(0, entry.ReferenceFrameIndex);
            });
        AssertFrame.Attribute(referenceFrames[0], "onbar", addedHandler);
        Assert.NotEqual(default, removedEventHandlerFrame.AttributeEventHandlerId);
        Assert.Equal(
            new[] { removedEventHandlerFrame.AttributeEventHandlerId },
            batchBuilder.DisposedEventHandlerIDs.AsEnumerable());
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
    public void AttributeDiff_WithSameSequenceNumber_AttributeAddedAtStart()
    {
        // Arrange
        oldTree.OpenElement(0, "My element");
        oldTree.AddAttribute(0, "attr2", "value2");
        oldTree.AddAttribute(0, "attr3", "value3");
        oldTree.CloseElement();
        newTree.OpenElement(0, "My element");
        newTree.AddAttribute(0, "attr1", "value1");
        newTree.AddAttribute(0, "attr2", "value2");
        newTree.AddAttribute(0, "attr3", "value3");
        newTree.CloseElement();

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(
            result.Edits,
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.SetAttribute, 0);
                Assert.Equal(0, entry.ReferenceFrameIndex);
            });
        Assert.Collection(
            referenceFrames,
            frame => AssertFrame.Attribute(frame, "attr1", 0));
    }

    [Fact]
    public void AttributeDiff_WithSameSequenceNumber_AttributeAddedInMiddle()
    {
        // Arrange
        oldTree.OpenElement(0, "My element");
        oldTree.AddAttribute(0, "attr1", "value1");
        oldTree.AddAttribute(0, "attr3", "value3");
        oldTree.CloseElement();
        newTree.OpenElement(0, "My element");
        newTree.AddAttribute(0, "attr1", "value1");
        newTree.AddAttribute(0, "attr2", "value2");
        newTree.AddAttribute(0, "attr3", "value3");
        newTree.CloseElement();

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(
            result.Edits,
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.SetAttribute, 0);
                Assert.Equal(0, entry.ReferenceFrameIndex);
            });

        Assert.Collection(
            referenceFrames,
            frame => AssertFrame.Attribute(frame, "attr2", 0));
    }

    [Fact]
    public void AttributeDiff_WithSameSequenceNumber_AttributeAddedAtEnd()
    {
        // Arrange
        oldTree.OpenElement(0, "My element");
        oldTree.AddAttribute(0, "attr1", "value1");
        oldTree.AddAttribute(0, "attr2", "value2");
        oldTree.CloseElement();
        newTree.OpenElement(0, "My element");
        newTree.AddAttribute(0, "attr1", "value1");
        newTree.AddAttribute(0, "attr2", "value2");
        newTree.AddAttribute(0, "attr3", "value3");
        newTree.CloseElement();

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(
            result.Edits,
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.SetAttribute, 0);
                Assert.Equal(0, entry.ReferenceFrameIndex);
            });

        Assert.Collection(
            referenceFrames,
            frame => AssertFrame.Attribute(frame, "attr3", 0));
    }

    [Fact]
    public void AttributeDiff_WithSequentialSequenceNumber_AttributeAddedAtStart()
    {
        // Arrange
        oldTree.OpenElement(0, "My element");
        oldTree.AddAttribute(2, "attr2", "value2");
        oldTree.AddAttribute(3, "attr3", "value3");
        oldTree.CloseElement();
        newTree.OpenElement(0, "My element");
        newTree.AddAttribute(1, "attr1", "value1");
        newTree.AddAttribute(2, "attr2", "value2");
        newTree.AddAttribute(3, "attr3", "value3");
        newTree.CloseElement();

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(
            result.Edits,
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.SetAttribute, 0);
                Assert.Equal(0, entry.ReferenceFrameIndex);
            });
        Assert.Collection(
            referenceFrames,
            frame => AssertFrame.Attribute(frame, "attr1", 1));
    }

    [Fact]
    public void AttributeDiff_WithSequentialSequenceNumber_AttributeAddedInMiddle()
    {
        // Arrange
        oldTree.OpenElement(0, "My element");
        oldTree.AddAttribute(1, "attr1", "value1");
        oldTree.AddAttribute(3, "attr3", "value3");
        oldTree.CloseElement();
        newTree.OpenElement(0, "My element");
        newTree.AddAttribute(1, "attr1", "value1");
        newTree.AddAttribute(2, "attr2", "value2");
        newTree.AddAttribute(3, "attr3", "value3");
        newTree.CloseElement();

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(
            result.Edits,
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.SetAttribute, 0);
                Assert.Equal(0, entry.ReferenceFrameIndex);
            });

        Assert.Collection(
            referenceFrames,
            frame => AssertFrame.Attribute(frame, "attr2", 2));
    }

    [Fact]
    public void AttributeDiff_WithSequentialSequenceNumber_AttributeAddedAtEnd()
    {
        // Arrange
        oldTree.OpenElement(0, "My element");
        oldTree.AddAttribute(1, "attr1", "value1");
        oldTree.AddAttribute(2, "attr2", "value2");
        oldTree.CloseElement();
        newTree.OpenElement(0, "My element");
        newTree.AddAttribute(1, "attr1", "value1");
        newTree.AddAttribute(2, "attr2", "value2");
        newTree.AddAttribute(3, "attr3", "value3");
        newTree.CloseElement();

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(
            result.Edits,
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.SetAttribute, 0);
                Assert.Equal(0, entry.ReferenceFrameIndex);
            });

        Assert.Collection(
            referenceFrames,
            frame => AssertFrame.Attribute(frame, "attr3", 3));
    }

    [Fact]
    public void AttributeDiff_WithSameSequenceNumber_AttributeRemovedAtStart()
    {
        // Arrange
        oldTree.OpenElement(0, "My element");
        oldTree.AddAttribute(0, "attr1", "value1");
        oldTree.AddAttribute(0, "attr2", "value2");
        oldTree.AddAttribute(0, "attr3", "value3");
        oldTree.CloseElement();
        newTree.OpenElement(0, "My element");
        newTree.AddAttribute(0, "attr2", "value2");
        newTree.AddAttribute(0, "attr3", "value3");
        newTree.CloseElement();

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(
            result.Edits,
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.RemoveAttribute, 0);
                Assert.Equal("attr1", entry.RemovedAttributeName);
            });
    }

    [Fact]
    public void AttributeDiff_WithSameSequenceNumber_AttributeRemovedInMiddle()
    {
        // Arrange
        oldTree.OpenElement(0, "My element");
        oldTree.AddAttribute(0, "attr1", "value1");
        oldTree.AddAttribute(0, "attr2", "value2");
        oldTree.AddAttribute(0, "attr3", "value3");
        oldTree.CloseElement();
        newTree.OpenElement(0, "My element");
        newTree.AddAttribute(0, "attr1", "value1");
        newTree.AddAttribute(0, "attr3", "value3");
        newTree.CloseElement();

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(
            result.Edits,
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.RemoveAttribute, 0);
                Assert.Equal("attr2", entry.RemovedAttributeName);
            });
    }

    [Fact]
    public void AttributeDiff_WithSameSequenceNumber_AttributeRemovedAtEnd()
    {
        // Arrange
        oldTree.OpenElement(0, "My element");
        oldTree.AddAttribute(0, "attr1", "value1");
        oldTree.AddAttribute(0, "attr2", "value2");
        oldTree.AddAttribute(0, "attr3", "value3");
        oldTree.CloseElement();
        newTree.OpenElement(0, "My element");
        newTree.AddAttribute(0, "attr1", "value1");
        newTree.AddAttribute(0, "attr2", "value2");
        newTree.CloseElement();

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(
            result.Edits,
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.RemoveAttribute, 0);
                Assert.Equal("attr3", entry.RemovedAttributeName);
            });
    }

    [Fact]
    public void AttributeDiff_WithSequentialSequenceNumber_AttributeRemovedAtStart()
    {
        // Arrange
        oldTree.OpenElement(0, "My element");
        oldTree.AddAttribute(1, "attr1", "value1");
        oldTree.AddAttribute(2, "attr2", "value2");
        oldTree.AddAttribute(3, "attr3", "value3");
        oldTree.CloseElement();
        newTree.OpenElement(0, "My element");
        newTree.AddAttribute(2, "attr2", "value2");
        newTree.AddAttribute(3, "attr3", "value3");
        newTree.CloseElement();

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(
            result.Edits,
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.RemoveAttribute, 0);
                Assert.Equal("attr1", entry.RemovedAttributeName);
            });
    }

    [Fact]
    public void AttributeDiff_WithSequentialSequenceNumber_AttributeRemovedInMiddle()
    {
        // Arrange
        oldTree.OpenElement(0, "My element");
        oldTree.AddAttribute(1, "attr1", "value1");
        oldTree.AddAttribute(2, "attr2", "value2");
        oldTree.AddAttribute(3, "attr3", "value3");
        oldTree.CloseElement();
        newTree.OpenElement(0, "My element");
        newTree.AddAttribute(1, "attr1", "value1");
        newTree.AddAttribute(3, "attr3", "value3");
        newTree.CloseElement();

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(
            result.Edits,
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.RemoveAttribute, 0);
                Assert.Equal("attr2", entry.RemovedAttributeName);
            });
    }

    [Fact]
    public void AttributeDiff_WithSequentialSequenceNumber_AttributeRemovedAtEnd()
    {
        // Arrange
        oldTree.OpenElement(0, "My element");
        oldTree.AddAttribute(1, "attr1", "value1");
        oldTree.AddAttribute(2, "attr2", "value2");
        oldTree.AddAttribute(3, "attr3", "value3");
        oldTree.CloseElement();
        newTree.OpenElement(0, "My element");
        newTree.AddAttribute(1, "attr1", "value1");
        newTree.AddAttribute(2, "attr2", "value2");
        newTree.CloseElement();

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(
            result.Edits,
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.RemoveAttribute, 0);
                Assert.Equal("attr3", entry.RemovedAttributeName);
            });
    }

    [Fact]
    public void DiffsElementsHierarchically()
    {
        // Arrange
        oldTree.AddContent(09, "unrelated");
        oldTree.OpenElement(10, "root");
        oldTree.OpenElement(11, "child");
        oldTree.OpenElement(12, "grandchild");
        oldTree.AddContent(13, "grandchild old text");
        oldTree.CloseElement();
        oldTree.CloseElement();
        oldTree.CloseElement();

        newTree.AddContent(09, "unrelated");
        newTree.OpenElement(10, "root");
        newTree.OpenElement(11, "child");
        newTree.OpenElement(12, "grandchild");
        newTree.AddContent(13, "grandchild new text");
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
        oldTree.AddContent(11, "Text that will change");
        oldTree.OpenElement(12, "Subtree that will not change");
        oldTree.OpenElement(13, "Another");
        oldTree.AddContent(14, "Text that will not change");
        oldTree.CloseElement();
        oldTree.CloseElement();
        oldTree.CloseElement();

        newTree.OpenElement(10, "root");
        newTree.AddContent(11, "Text that has changed");
        newTree.OpenElement(12, "Subtree that will not change");
        newTree.OpenElement(13, "Another");
        newTree.AddContent(14, "Text that will not change");
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
        oldTree.AddContent(10, "text1");
        oldTree.AddContent(11, "text2");
        oldTree.AddContent(12, "text3");
        oldTree.AddContent(13, "text4");
        newTree.AddContent(10, "text1");
        newTree.AddContent(11, "text2modified");
        newTree.AddContent(12, "text3");
        newTree.AddContent(13, "text4");

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
    public void PassesThroughRegionsInsidePrependedElements()
    {
        // Arrange
        oldTree.AddContent(0, "Will not change");
        newTree.AddContent(0, "Will not change");
        newTree.OpenElement(1, "root");
        newTree.OpenRegion(2);
        newTree.AddContent(0, "text1");
        newTree.CloseRegion();
        newTree.CloseElement();

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(result.Edits,
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.PrependFrame, 1);
                Assert.Equal(0, entry.ReferenceFrameIndex);
            });
        Assert.Collection(referenceFrames,
            frame => AssertFrame.Element(frame, "root", 3, 1),
            frame => AssertFrame.Region(frame, 2, 2),
            frame => AssertFrame.Text(frame, "text1"));
    }

    [Fact]
    public void RecognizesInsertedRegions()
    {
        // Arrange
        oldTree.AddContent(1, "Start");
        oldTree.AddContent(3, "End");
        newTree.AddContent(1, "Start");
        newTree.OpenRegion(2);
        newTree.AddContent(4, "Text inside region"); // Sequence number is unrelated to outside the region
        newTree.OpenRegion(5);
        newTree.AddContent(6, "Text inside nested region");
        newTree.CloseRegion();
        newTree.CloseRegion();
        newTree.AddContent(3, "End");

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(result.Edits,
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.PrependFrame, 1);
                AssertFrame.Text(
                    referenceFrames[entry.ReferenceFrameIndex], "Text inside region");
            },
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.PrependFrame, 2);
                AssertFrame.Text(
                    referenceFrames[entry.ReferenceFrameIndex], "Text inside nested region");
            });
    }

    [Fact]
    public void RecognizesRemovedRegions()
    {
        // Arrange
        oldTree.AddContent(1, "Start");
        oldTree.OpenRegion(2);
        oldTree.AddContent(4, "Text inside region"); // Sequence number is unrelated to outside the region
        oldTree.OpenRegion(5);
        oldTree.AddContent(6, "Text inside nested region");
        oldTree.CloseRegion();
        oldTree.CloseRegion();
        oldTree.AddContent(3, "End");
        newTree.AddContent(1, "Start");
        newTree.AddContent(3, "End");

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(result.Edits,
            entry => AssertEdit(entry, RenderTreeEditType.RemoveFrame, 1),
            entry => AssertEdit(entry, RenderTreeEditType.RemoveFrame, 1));
    }

    [Fact]
    public void RecognizesEquivalentRegions()
    {
        // Arrange
        oldTree.AddContent(1, "Start");
        oldTree.OpenRegion(2);
        oldTree.AddContent(4, "Text inside region");
        oldTree.AddContent(5, "Text to move");
        oldTree.OpenRegion(6);
        oldTree.CloseRegion();
        oldTree.CloseRegion();
        oldTree.AddContent(3, "End");
        newTree.AddContent(1, "Start");
        newTree.OpenRegion(2);
        newTree.AddContent(4, "Changed text inside region");
        newTree.OpenRegion(6);
        newTree.AddContent(5, "Text to move"); // Although it's the same sequence and content, it's now in a different region so not the same
        newTree.CloseRegion();
        newTree.CloseRegion();
        newTree.AddContent(3, "End");

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(result.Edits,
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.UpdateText, 1);
                AssertFrame.Text(
                    referenceFrames[entry.ReferenceFrameIndex], "Changed text inside region");
            },
            entry => AssertEdit(entry, RenderTreeEditType.RemoveFrame, 2),
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.PrependFrame, 2);
                AssertFrame.Text(
                    referenceFrames[entry.ReferenceFrameIndex], "Text to move");
            });
    }

    [Fact]
    public void InstantiatesChildComponentsForInsertedFrames()
    {
        // Arrange
        oldTree.AddContent(10, "text1");                    //  0: text1
        oldTree.OpenElement(11, "container");               //  1: <container>
        oldTree.CloseElement();                             //     </container>
        newTree.AddContent(10, "text1");                    //  0: text1
        newTree.OpenElement(11, "container");               //  1: <container>
        newTree.OpenComponent<FakeComponent>(12);           //  2:   <FakeComponent>
        newTree.CloseComponent();                           //       </FakeComponent>
        newTree.OpenComponent<FakeComponent2>(13);          //  3:   <FakeComponent2>
        newTree.CloseComponent();                           //       </FakeComponent2>
        newTree.CloseElement();                             //     </container>

        // Act
        var renderBatch = GetRenderedBatch();

        // Assert
        var diff = renderBatch.UpdatedComponents.AsEnumerable().Single();
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
        AssertFrame.ComponentWithInstance<FakeComponent>(renderBatch.ReferenceFrames.Array[0], 0, null, 12);
        AssertFrame.ComponentWithInstance<FakeComponent2>(renderBatch.ReferenceFrames.Array[1], 1, null, 13);
    }

    [Fact]
    public void SetsParametersOnChildComponents()
    {
        // Arrange
        var testObject = new object();
        newTree.OpenComponent<FakeComponent>(0);
        newTree.AddComponentParameter(1, nameof(FakeComponent.IntProperty), 123);
        newTree.AddComponentParameter(2, nameof(FakeComponent.StringProperty), "some string");
        newTree.AddComponentParameter(3, nameof(FakeComponent.ObjectProperty), testObject);
        newTree.CloseComponent();

        // Act
        var renderBatch = GetRenderedBatch();
        var componentInstance = newTree.GetFrames().AsEnumerable().First().Component as FakeComponent;

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
        oldTree.AddContent(10, "text1");                    //  0: text1
        oldTree.OpenElement(11, "container");               //  1: <container>
        oldTree.OpenComponent<FakeComponent>(12);           //  2:   <FakeComponent>
        oldTree.CloseComponent();                           //       </FakeComponent>
        oldTree.OpenComponent<FakeComponent2>(13);          //  3:   <FakeComponent2>
        oldTree.CloseComponent();                           //       </FakeComponent2>
        oldTree.CloseElement();                             //     </container>
        newTree.AddContent(10, "text1");                    //  0: text1
        newTree.OpenElement(11, "container");               //  1: <container>
        newTree.OpenComponent<FakeComponent>(12);           //  2:   <FakeComponent>
        newTree.CloseComponent();                           //       </FakeComponent>
        newTree.OpenComponent<FakeComponent2>(13);          //  3:   <FakeComponent2>
        newTree.CloseComponent();                           //       </FakeComponent2>
        newTree.CloseElement();                             //     </container>

        using var batchBuilder = new RenderBatchBuilder();
        using var renderTreeBuilder = new RenderTreeBuilder();
        RenderTreeDiffBuilder.ComputeDiff(renderer, batchBuilder, 0, renderTreeBuilder.GetFrames(), oldTree.GetFrames());
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
    public void PreservesEventHandlerIdsForRetainedEventHandlers()
    {
        // Arrange
        Action<EventArgs> retainedHandler = _ => { };
        oldTree.OpenElement(0, "My element");
        oldTree.AddAttribute(1, "ontest", retainedHandler);
        oldTree.CloseElement();
        newTree.OpenElement(0, "My element");
        newTree.AddAttribute(1, "ontest", retainedHandler);
        newTree.CloseElement();

        // Act
        var (result, referenceFrames, batchBuilder) = GetSingleUpdatedComponentWithBatch(initializeFromFrames: true);
        var oldAttributeFrame = oldTree.GetFrames().Array[1];
        var newAttributeFrame = newTree.GetFrames().Array[1];

        // Assert
        Assert.Empty(result.Edits);
        AssertFrame.Attribute(oldAttributeFrame, "ontest", retainedHandler);
        AssertFrame.Attribute(newAttributeFrame, "ontest", retainedHandler);
        Assert.NotEqual(default, oldAttributeFrame.AttributeEventHandlerId);
        Assert.Equal(oldAttributeFrame.AttributeEventHandlerId, newAttributeFrame.AttributeEventHandlerId);
        Assert.Empty(batchBuilder.DisposedEventHandlerIDs.AsEnumerable());
    }

    [Fact]
    public void PreservesEventHandlerIdsForRetainedEventHandlers_SlowPath()
    {
        // Arrange
        Action<EventArgs> retainedHandler = _ => { };
        oldTree.OpenElement(0, "My element");
        oldTree.AddAttribute(0, "ontest", retainedHandler);
        oldTree.CloseElement();
        newTree.OpenElement(0, "My element");
        newTree.AddAttribute(0, "another-attribute", "go down the slow path please");
        newTree.AddAttribute(0, "ontest", retainedHandler);
        newTree.CloseElement();

        // Act
        var (result, referenceFrames, batchBuilder) = GetSingleUpdatedComponentWithBatch(initializeFromFrames: true);
        var oldAttributeFrame = oldTree.GetFrames().Array[1];
        var newAttributeFrame = newTree.GetFrames().Array[2];

        // Assert
        Assert.Single(result.Edits);
        AssertFrame.Attribute(oldAttributeFrame, "ontest", retainedHandler);
        AssertFrame.Attribute(newAttributeFrame, "ontest", retainedHandler);
        Assert.NotEqual(default, oldAttributeFrame.AttributeEventHandlerId);
        Assert.Equal(oldAttributeFrame.AttributeEventHandlerId, newAttributeFrame.AttributeEventHandlerId);
        Assert.Empty(batchBuilder.DisposedEventHandlerIDs.AsEnumerable());
    }

    [Fact]
    public void SetsUpdatedParametersOnChildComponents()
    {
        // Arrange
        var objectWillNotChange = new object();
        oldTree.OpenComponent<FakeComponent>(12);
        oldTree.AddComponentParameter(13, nameof(FakeComponent.StringProperty), "String will change");
        oldTree.AddComponentParameter(14, nameof(FakeComponent.ObjectProperty), objectWillNotChange);
        oldTree.CloseComponent();
        newTree.OpenComponent<FakeComponent>(12);
        newTree.AddComponentParameter(13, nameof(FakeComponent.StringProperty), "String did change");
        newTree.AddComponentParameter(14, nameof(FakeComponent.ObjectProperty), objectWillNotChange);
        newTree.CloseComponent();

        using var batchBuilder = new RenderBatchBuilder();
        using var renderTree = new RenderTreeBuilder();
        RenderTreeDiffBuilder.ComputeDiff(renderer, batchBuilder, 0, renderTree.GetFrames(), oldTree.GetFrames());
        var originalComponentInstance = (FakeComponent)oldTree.GetFrames().Array[0].Component;

        // Act
        var renderBatch = GetRenderedBatch();
        var newComponentInstance = (FakeComponent)oldTree.GetFrames().Array[0].Component;

        // Assert
        Assert.Equal(1, renderBatch.UpdatedComponents.Count); // Because the diff builder only queues child component renders; it doesn't actually perform them itself
        Assert.Same(originalComponentInstance, newComponentInstance);
        Assert.Equal("String did change", newComponentInstance.StringProperty);
        Assert.Same(objectWillNotChange, newComponentInstance.ObjectProperty);
    }

    [Fact]
    public void SkipsUpdatingParametersOnChildComponentsIfAllAreDefinitelyImmutableAndUnchanged()
    {
        // We only know that types are immutable if either Type.IsPrimitive, or it's one of
        // a known set of common immutable types.

        // Arrange: Populate old and new with equivalent content
        RenderFragment fragmentWillNotChange = builder => throw new NotImplementedException();
        var dateTimeWillNotChange = DateTime.Now;
        foreach (var tree in new[] { oldTree, newTree })
        {
            tree.OpenComponent<CaptureSetParametersComponent>(0);
            tree.AddComponentParameter(1, "MyString", "Some fixed string");
            tree.AddComponentParameter(1, "MyByte", (byte)123);
            tree.AddComponentParameter(1, "MyInt", int.MaxValue);
            tree.AddComponentParameter(1, "MyLong", long.MaxValue);
            tree.AddComponentParameter(1, "MyBool", true);
            tree.AddComponentParameter(1, "MyFloat", float.MaxValue);
            tree.AddComponentParameter(1, "MyDouble", double.MaxValue);
            tree.AddComponentParameter(1, "MyDecimal", decimal.MinusOne);
            tree.AddComponentParameter(1, "MyDate", dateTimeWillNotChange);
            tree.AddComponentParameter(1, "MyGuid", Guid.Empty);
            tree.AddComponentParameter(1, "MySByte", (sbyte)123);
            tree.AddComponentParameter(1, "MyShort", (short)123);
            tree.AddComponentParameter(1, "MyUShort", (ushort)123);
            tree.AddComponentParameter(1, "MyUInt", uint.MaxValue);
            tree.AddComponentParameter(1, "MyULong", ulong.MaxValue);
            tree.AddComponentParameter(1, "MyChar", 'c');
            tree.AddComponentParameter(1, "MyEnum", StringComparison.OrdinalIgnoreCase);
            tree.AddComponentParameter(1, "MyEventCallback", EventCallback.Empty);
            tree.AddComponentParameter(1, "MyEventCallbackOfT", EventCallback<int>.Empty);
            tree.CloseComponent();
        }

        using var batchBuilder = new RenderBatchBuilder();
        using var renderTreeBuilder = new RenderTreeBuilder();
        RenderTreeDiffBuilder.ComputeDiff(renderer, batchBuilder, 0, renderTreeBuilder.GetFrames(), oldTree.GetFrames());
        var originalComponentInstance = (CaptureSetParametersComponent)oldTree.GetFrames().Array[0].Component;
        Assert.Equal(1, originalComponentInstance.SetParametersCallCount);

        // Act
        var renderBatch = GetRenderedBatch();
        var newComponentInstance = (CaptureSetParametersComponent)oldTree.GetFrames().Array[0].Component;

        // Assert
        Assert.Same(originalComponentInstance, newComponentInstance);
        Assert.Equal(1, originalComponentInstance.SetParametersCallCount); // Received no further parameter change notification
    }

    [Fact]
    public void AlwaysRegardsRenderFragmentAsPossiblyChanged()
    {
        // Even if the RenderFragment instance itself is unchanged, the output you get
        // when invoking it might have changed (they aren't pure functions in general)

        // Arrange: Populate old and new with equivalent content
        RenderFragment fragmentWillNotChange = builder => throw new NotImplementedException();
        foreach (var tree in new[] { oldTree, newTree })
        {
            tree.OpenComponent<CaptureSetParametersComponent>(0);
            tree.AddComponentParameter(1, "MyFragment", fragmentWillNotChange);
            tree.CloseComponent();
        }

        using var batchBuilder = new RenderBatchBuilder();
        using var renderTreeBuilder = new RenderTreeBuilder();
        RenderTreeDiffBuilder.ComputeDiff(renderer, batchBuilder, 0, renderTreeBuilder.GetFrames(), oldTree.GetFrames());
        var componentInstance = (CaptureSetParametersComponent)oldTree.GetFrames().Array[0].Component;
        Assert.Equal(1, componentInstance.SetParametersCallCount);

        // Act
        var renderBatch = GetRenderedBatch();

        // Assert
        Assert.Equal(2, componentInstance.SetParametersCallCount);
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

        using var batchBuilder = new RenderBatchBuilder();
        using var renderTree = new RenderTreeBuilder();
        RenderTreeDiffBuilder.ComputeDiff(renderer, batchBuilder, 0, renderTree.GetFrames(), oldTree.GetFrames());

        // Act/Assert
        // Note that we track NonDisposableComponent was disposed even though it's not IDisposable,
        // because it's up to the upstream renderer to decide what "disposing" a component means
        Assert.Empty(batchBuilder.ComponentDisposalQueue);
        RenderTreeDiffBuilder.ComputeDiff(renderer, batchBuilder, 0, oldTree.GetFrames(), newTree.GetFrames());
        Assert.Equal(new[] { 0, 1 }, batchBuilder.ComponentDisposalQueue);
    }

    [Fact]
    public void AssignsDistinctIdToNewElementReferenceCaptures()
    {
        // Arrange
        ElementReference ref1 = default, ref2 = default;
        Action<ElementReference> capture1 = val => { ref1 = val; };
        Action<ElementReference> capture2 = val => { ref2 = val; };
        newTree.OpenElement(0, "My element");
        newTree.AddElementReferenceCapture(1, capture1);
        newTree.AddElementReferenceCapture(2, capture2);
        newTree.CloseElement();

        // Act
        var (diff, referenceFrames) = GetSingleUpdatedComponent();

        // Assert: Distinct nonnull IDs
        Assert.NotNull(ref1.Id);
        Assert.NotNull(ref2.Id);
        Assert.NotEqual(ref1.Id, ref2.Id);

        // Assert: Also specified in diff
        Assert.Collection(diff.Edits, edit =>
        {
            AssertEdit(edit, RenderTreeEditType.PrependFrame, 0);
            Assert.Equal(0, edit.ReferenceFrameIndex);
        });
        Assert.Collection(referenceFrames,
            frame => AssertFrame.Element(frame, "My element", 3),
            frame =>
            {
                AssertFrame.ElementReferenceCapture(frame, capture1);
                Assert.Equal(ref1.Id, frame.ElementReferenceCaptureId);
            },
            frame =>
            {
                AssertFrame.ElementReferenceCapture(frame, capture2);
                Assert.Equal(ref2.Id, frame.ElementReferenceCaptureId);
            });
    }

    [Fact]
    public void PreservesIdsOnRetainedElementReferenceCaptures()
    {
        // Arrange
        var refWriteCount = 0;
        ElementReference ref1 = default;
        Action<ElementReference> capture1 = val => { ref1 = val; refWriteCount++; };
        oldTree.OpenElement(0, "My element");
        oldTree.AddElementReferenceCapture(1, capture1);
        oldTree.CloseElement();
        newTree.OpenElement(0, "My element");
        newTree.AddElementReferenceCapture(1, capture1);
        newTree.CloseElement();

        // Act
        var (diff, referenceFrames) = GetSingleUpdatedComponent(initializeFromFrames: true);

        // Assert: Did not invoke the capture action a second time
        // Note: We're not preserving the ReferenceCaptureId on the actual RenderTreeFrames in the same
        //       way we do for event handler IDs, simply because there's no need to do so. We only do
        //       anything with ReferenceCaptureId when frames are first inserted into the document.
        Assert.NotNull(ref1.Id);
        Assert.Equal(1, refWriteCount);
        Assert.Empty(diff.Edits);
        Assert.Empty(referenceFrames);
    }

    [Fact]
    public void InvokesAssignerForComponentReferenceCapturesOnInsertion()
    {
        // Arrange
        FakeComponent capturedInstance1 = null, capturedInstance2 = null;
        Action<object> assigner1 = val => { capturedInstance1 = (FakeComponent)val; };
        Action<object> assigner2 = val => { capturedInstance2 = (FakeComponent)val; };
        newTree.OpenComponent<FakeComponent>(0);
        newTree.AddComponentReferenceCapture(1, assigner1);
        newTree.AddComponentReferenceCapture(2, assigner2);
        newTree.CloseComponent();

        // Act
        var (diff, referenceFrames) = GetSingleUpdatedComponent();

        // Assert: Assigned references
        Assert.NotNull(capturedInstance1);
        Assert.NotNull(capturedInstance2);
        Assert.IsType<FakeComponent>(capturedInstance1);
        Assert.IsType<FakeComponent>(capturedInstance2);
        Assert.Same(capturedInstance1, capturedInstance2);

        // Assert: Also in diff, even though we have no use for it there
        // (it would be costly to exclude given how the array range is copied)
        Assert.Collection(diff.Edits, edit =>
        {
            AssertEdit(edit, RenderTreeEditType.PrependFrame, 0);
            Assert.Equal(0, edit.ReferenceFrameIndex);
        });
        Assert.Collection(referenceFrames,
            frame =>
            {
                AssertFrame.Component<FakeComponent>(frame, 3, 0);
                Assert.Same(capturedInstance1, frame.Component);
            },
            frame => AssertFrame.ComponentReferenceCapture(frame, assigner1, 1),
            frame => AssertFrame.ComponentReferenceCapture(frame, assigner2, 2));
    }

    [Fact]
    public void DoesNotInvokeAssignerAgainForRetainedComponents()
    {
        // Arrange
        var refWriteCount = 0;
        FakeComponent capturedInstance = null;
        Action<object> assigner = val => { capturedInstance = (FakeComponent)val; refWriteCount++; };
        oldTree.OpenComponent<FakeComponent>(0);
        oldTree.AddComponentReferenceCapture(1, assigner);
        oldTree.CloseComponent();
        newTree.OpenComponent<FakeComponent>(0);
        newTree.AddComponentReferenceCapture(1, assigner);
        newTree.CloseComponent();

        // Act
        var (diff, referenceFrames) = GetSingleUpdatedComponent(initializeFromFrames: true);

        // Assert: Did not invoke the capture action a second time
        Assert.NotNull(capturedInstance);
        Assert.IsType<FakeComponent>(capturedInstance);
        Assert.Equal(1, refWriteCount);
        Assert.Empty(diff.Edits);
        Assert.Empty(referenceFrames);
    }

    [Fact]
    public void RecognizesKeyedElementMoves()
    {
        // Arrange
        oldTree.OpenElement(0, "container");
        oldTree.SetKey("first key");
        oldTree.AddContent(1, "First");
        oldTree.CloseElement();

        oldTree.AddContent(2, "Unkeyed item");

        oldTree.OpenElement(0, "container");
        oldTree.SetKey("second key");
        oldTree.AddContent(1, "Second");
        oldTree.CloseElement();

        newTree.OpenElement(0, "container");
        newTree.SetKey("second key");
        newTree.AddContent(1, "Second");
        newTree.CloseElement();

        newTree.AddContent(2, "Unkeyed item");

        newTree.OpenElement(0, "container");
        newTree.SetKey("first key");
        newTree.AddContent(1, "First modified");
        newTree.CloseElement();

        // Without the key, it changes the text contents of both
        // With the key, it reorders them and just updates the text content of one

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(result.Edits,
            // First we update the modified descendants in place
            entry => AssertEdit(entry, RenderTreeEditType.StepIn, 0),
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.UpdateText, 0);
                Assert.Equal(0, entry.ReferenceFrameIndex);
                Assert.Equal("First modified", referenceFrames[entry.ReferenceFrameIndex].TextContent);
            },
            entry => AssertEdit(entry, RenderTreeEditType.StepOut, 0),

            // Then we have the permutation list
            entry => AssertPermutationListEntry(entry, 0, 2),
            entry => AssertPermutationListEntry(entry, 2, 0),
            entry => AssertEdit(entry, RenderTreeEditType.PermutationListEnd, 0));
    }

    [Fact]
    public void RecognizesKeyedComponentMoves()
    {
        // Arrange
        oldTree.OpenComponent<CaptureSetParametersComponent>(0);
        oldTree.SetKey("first key");
        oldTree.AddComponentParameter(1, nameof(FakeComponent.StringProperty), "First param");
        oldTree.CloseComponent();

        oldTree.AddContent(2, "Unkeyed item");

        oldTree.OpenComponent<CaptureSetParametersComponent>(0);
        oldTree.SetKey("second key");
        oldTree.AddComponentParameter(1, nameof(FakeComponent.StringProperty), "Second param");
        oldTree.CloseComponent();

        using var renderTreeBuilder = new RenderTreeBuilder();
        GetRenderedBatch(renderTreeBuilder, oldTree, false); // Assign initial IDs
        var oldComponents = GetComponents<CaptureSetParametersComponent>(oldTree);

        newTree.OpenComponent<CaptureSetParametersComponent>(0);
        newTree.SetKey("second key");
        newTree.AddComponentParameter(1, nameof(FakeComponent.StringProperty), "Second param");
        newTree.CloseComponent();

        newTree.AddContent(2, "Unkeyed item");

        newTree.OpenComponent<CaptureSetParametersComponent>(0);
        newTree.SetKey("first key");
        newTree.AddComponentParameter(1, nameof(FakeComponent.StringProperty), "First param modified");
        newTree.CloseComponent();

        // Without the key, it changes the parameter on both
        // With the key, it reorders them and just updates the parameter of one

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();
        var newComponents = GetComponents<CaptureSetParametersComponent>(newTree);

        // Assert: Retains component instances
        Assert.Same(oldComponents[0], newComponents[1]);
        Assert.Same(oldComponents[1], newComponents[0]);

        // Assert: Supplies updated params only to (originally) first component
        Assert.Equal(2, oldComponents[0].SetParametersCallCount);
        Assert.Equal(1, oldComponents[1].SetParametersCallCount);

        // Assert: Correct diff
        Assert.Collection(result.Edits,
            entry => AssertPermutationListEntry(entry, 0, 2),
            entry => AssertPermutationListEntry(entry, 2, 0),
            entry => AssertEdit(entry, RenderTreeEditType.PermutationListEnd, 0));
    }

    [Fact]
    public void CanMoveBeforeInsertedItem()
    {
        // Arrange
        AddWithKey(oldTree, "will retain");
        AddWithKey(oldTree, "will move");

        AddWithKey(newTree, "will move");
        AddWithKey(newTree, "newly inserted");
        AddWithKey(newTree, "will retain");

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(result.Edits,
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.PrependFrame, 1);
                Assert.Equal(0, entry.ReferenceFrameIndex);
                Assert.Equal("newly inserted", referenceFrames[entry.ReferenceFrameIndex].ElementKey);
            },
            entry => AssertPermutationListEntry(entry, 0, 2),
            entry => AssertPermutationListEntry(entry, 2, 0),
            entry => AssertEdit(entry, RenderTreeEditType.PermutationListEnd, 0));
    }

    [Fact]
    public void CanMoveBeforeDeletedItem()
    {
        // Arrange
        AddWithKey(oldTree, "will retain");
        AddWithKey(oldTree, "will delete");
        AddWithKey(oldTree, "will move");

        AddWithKey(newTree, "will move");
        AddWithKey(newTree, "will retain");

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(result.Edits,
            entry => AssertEdit(entry, RenderTreeEditType.RemoveFrame, 1),
            entry => AssertPermutationListEntry(entry, 0, 1),
            entry => AssertPermutationListEntry(entry, 1, 0),
            entry => AssertEdit(entry, RenderTreeEditType.PermutationListEnd, 0));
    }

    [Fact]
    public void CanMoveAfterInsertedItem()
    {
        // Arrange
        AddWithKey(oldTree, "will move");
        AddWithKey(oldTree, "will retain");

        AddWithKey(newTree, "newly inserted");
        AddWithKey(newTree, "will retain");
        AddWithKey(newTree, "will move");

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(result.Edits,
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.PrependFrame, 0);
                Assert.Equal(0, entry.ReferenceFrameIndex);
                Assert.Equal("newly inserted", referenceFrames[entry.ReferenceFrameIndex].ElementKey);
            },
            entry => AssertPermutationListEntry(entry, 1, 2),
            entry => AssertPermutationListEntry(entry, 2, 1),
            entry => AssertEdit(entry, RenderTreeEditType.PermutationListEnd, 0));
    }

    [Fact]
    public void CanMoveAfterDeletedItem()
    {
        // Arrange
        AddWithKey(oldTree, "will move");
        AddWithKey(oldTree, "will delete");
        AddWithKey(oldTree, "will retain");

        AddWithKey(newTree, "will retain");
        AddWithKey(newTree, "will move");

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(result.Edits,
            entry => AssertEdit(entry, RenderTreeEditType.RemoveFrame, 1),
            entry => AssertPermutationListEntry(entry, 0, 1),
            entry => AssertPermutationListEntry(entry, 1, 0),
            entry => AssertEdit(entry, RenderTreeEditType.PermutationListEnd, 0));
    }

    [Fact]
    public void CanChangeFrameTypeWithMatchingSequenceNumber()
    {
        oldTree.OpenElement(0, "some elem");
        oldTree.AddContent(1, "Hello!");
        oldTree.CloseElement();

        newTree.AddContent(0, "some text");

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(result.Edits,
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.PrependFrame, 0);
                Assert.Equal(0, entry.ReferenceFrameIndex);
                Assert.Equal("some text", referenceFrames[entry.ReferenceFrameIndex].TextContent);
            },
            entry => AssertEdit(entry, RenderTreeEditType.RemoveFrame, 1));
    }

    [Fact]
    public void CanChangeFrameTypeWithMatchingKey()
    {
        oldTree.OpenComponent<FakeComponent>(0);
        oldTree.CloseComponent();

        newTree.OpenElement(0, "some elem");
        newTree.SetKey("my key");
        newTree.CloseElement();

        // Act
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(result.Edits,
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.PrependFrame, 0);
                Assert.Equal(0, entry.ReferenceFrameIndex);
                Assert.Equal("some elem", referenceFrames[entry.ReferenceFrameIndex].ElementName);
            },
            entry => AssertEdit(entry, RenderTreeEditType.RemoveFrame, 1));
    }

    [Fact]
    public void RecognizesNamedEventBeingAdded()
    {
        oldTree.OpenElement(0, "existing");
        oldTree.AddAttribute(1, "attr1", "unrelated val1");
        oldTree.CloseElement();

        newTree.OpenElement(0, "existing");
        newTree.AddAttribute(1, "attr1", "unrelated val1");
        newTree.AddNamedEvent("someevent1", "added to existing element");
        newTree.CloseElement();
        newTree.OpenElement(2, "new element");
        newTree.AddNamedEvent("someevent2", "added with new element");
        newTree.CloseElement();

        // Act
        var (result, referenceFrames, batch) = GetSingleUpdatedComponentWithBatch(componentId: 123);

        // Assert
        Assert.Collection(result.Edits,
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.PrependFrame, 1);
                Assert.Equal(0, entry.ReferenceFrameIndex);
                Assert.Equal("new element", referenceFrames[entry.ReferenceFrameIndex].ElementName);
            });
        Assert.Collection(batch.NamedEventChanges.Value.AsEnumerable(),
            entry => AssertNamedEventChange(entry, NamedEventChangeType.Added, 123, 2, "someevent1", "added to existing element"),
            entry => AssertNamedEventChange(entry, NamedEventChangeType.Added, 123, 4, "someevent2", "added with new element"));
    }

    [Fact]
    public void RecognizesNamedEventBeingRemoved()
    {
        oldTree.OpenElement(0, "retaining");
        oldTree.AddAttribute(1, "attr1", "unrelated val1");
        oldTree.AddNamedEvent("someevent1", "removing from retained element");
        oldTree.CloseElement();
        oldTree.OpenElement(2, "removing");
        oldTree.AddNamedEvent("someevent2", "removed because element was removed");
        oldTree.CloseElement();

        newTree.OpenElement(0, "retaining");
        newTree.AddAttribute(1, "attr1", "unrelated val1");
        newTree.CloseElement();

        // Act
        var (result, referenceFrames, batch) = GetSingleUpdatedComponentWithBatch(componentId: 123);

        // Assert
        Assert.Collection(result.Edits,
            entry => AssertEdit(entry, RenderTreeEditType.RemoveFrame, 1));
        Assert.Collection(batch.NamedEventChanges.Value.AsEnumerable(),
            entry => AssertNamedEventChange(entry, NamedEventChangeType.Removed, 123, 2, "someevent1", "removing from retained element"),
            entry => AssertNamedEventChange(entry, NamedEventChangeType.Removed, 123, 4, "someevent2", "removed because element was removed"));
    }

    [Fact]
    public void RecognizesNamedEventBeingMoved()
    {
        oldTree.OpenElement(0, "elem");
        oldTree.AddNamedEvent("eventname", "assigned name");
        oldTree.CloseElement();

        newTree.OpenElement(0, "elem");
        newTree.AddAttribute(1, "attr1", "unrelated val1");
        newTree.AddNamedEvent("eventname", "assigned name");
        newTree.CloseElement();

        // Act
        var (result, referenceFrames, batch) = GetSingleUpdatedComponentWithBatch(componentId: 123);

        // Assert
        Assert.Collection(result.Edits,
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.SetAttribute, 0);
                Assert.Equal(0, entry.ReferenceFrameIndex);
                Assert.Equal("attr1", referenceFrames[entry.ReferenceFrameIndex].AttributeName);
            });
        Assert.Collection(batch.NamedEventChanges.Value.AsEnumerable(),
            entry => AssertNamedEventChange(entry, NamedEventChangeType.Removed, 123, 1, "eventname", "assigned name"),
            entry => AssertNamedEventChange(entry, NamedEventChangeType.Added, 123, 2, "eventname", "assigned name"));
    }

    [Fact]
    public void RecognizesNamedEventChangingAssignedName()
    {
        oldTree.OpenElement(0, "elem");
        oldTree.AddNamedEvent("eventname1", "original name");
        oldTree.AddNamedEvent("eventname2", "will be left unchanged");
        oldTree.CloseElement();

        newTree.OpenElement(0, "elem");
        newTree.AddNamedEvent("eventname1", "changed name");
        newTree.AddNamedEvent("eventname2", "will be left unchanged");
        newTree.CloseElement();

        // Act
        var (result, referenceFrames, batch) = GetSingleUpdatedComponentWithBatch(componentId: 123);

        // Assert
        Assert.Empty(result.Edits);
        Assert.Collection(batch.NamedEventChanges.Value.AsEnumerable(),
            entry => AssertNamedEventChange(entry, NamedEventChangeType.Removed, 123, 1, "eventname1", "original name"),
            entry => AssertNamedEventChange(entry, NamedEventChangeType.Added, 123, 1, "eventname1", "changed name"));
    }

    [Fact]
    public void CanAddNewAttributeAtArrayBuilderSizeBoundary()
    {
        // Represents https://github.com/dotnet/aspnetcore/issues/49192

        // Arrange: old and new trees go exactly up to the array builder capacity
        oldTree.OpenElement(0, "elem");
        for (var i = 0; oldTree.GetFrames().Count < oldTree.GetFrames().Array.Length; i++)
        {
            oldTree.AddAttribute(1, $"myattribute_{i}", "value");
        }
        newTree.OpenElement(0, "elem");
        for (var i = 0; newTree.GetFrames().Count < newTree.GetFrames().Array.Length; i++)
        {
            newTree.AddAttribute(1, $"myattribute_{i}", "value");
        }

        // ... then the new tree gets one more attribute that crosses the builder size boundary, forcing buffer expansion
        newTree.AddAttribute(1, $"myattribute_final", "value");

        // Act
        oldTree.CloseElement();
        newTree.CloseElement();
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(result.Edits,
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.SetAttribute, 0);
                Assert.Equal(0, entry.ReferenceFrameIndex);
                AssertFrame.Attribute(referenceFrames[0], "myattribute_final", "value", 1);
            });
    }

    [Fact]
    public void CanRemoveOldAttributeAtArrayBuilderSizeBoundary()
    {
        // Arrange: old and new trees go exactly up to the array builder capacity
        oldTree.OpenElement(0, "elem");
        for (var i = 0; oldTree.GetFrames().Count < oldTree.GetFrames().Array.Length; i++)
        {
            oldTree.AddAttribute(1, $"myattribute_{i}", "value");
        }
        newTree.OpenElement(0, "elem");
        for (var i = 0; newTree.GetFrames().Count < newTree.GetFrames().Array.Length; i++)
        {
            newTree.AddAttribute(1, $"myattribute_{i}", "value");
        }

        // ... then the old tree gets one more attribute that crosses the builder size boundary, forcing buffer expansion
        oldTree.AddAttribute(1, $"myattribute_final", "value");

        // Act
        oldTree.CloseElement();
        newTree.CloseElement();
        var (result, referenceFrames) = GetSingleUpdatedComponent();

        // Assert
        Assert.Collection(result.Edits,
            entry =>
            {
                AssertEdit(entry, RenderTreeEditType.RemoveAttribute, 0);
                Assert.Equal("myattribute_final", entry.RemovedAttributeName);
            });
    }

    private (RenderTreeDiff, RenderTreeFrame[]) GetSingleUpdatedComponent(bool initializeFromFrames = false)
    {
        var result = GetSingleUpdatedComponentWithBatch(initializeFromFrames);
        return (result.Item1, result.Item2);
    }

    private (RenderTreeDiff, RenderTreeFrame[], RenderBatch) GetSingleUpdatedComponentWithBatch(bool initializeFromFrames = false, int componentId = 0)
    {
        var batch = GetRenderedBatch(initializeFromFrames, componentId);
        var diffsInBatch = batch.UpdatedComponents;
        Assert.Equal(1, diffsInBatch.Count);
        return (diffsInBatch.Array[0], batch.ReferenceFrames.AsEnumerable().ToArray(), batch);
    }

    private RenderBatch GetRenderedBatch(bool initializeFromFrames = false, int componentId = 0)
        => GetRenderedBatch(oldTree, newTree, initializeFromFrames, componentId);

    private RenderBatch GetRenderedBatch(RenderTreeBuilder from, RenderTreeBuilder to, bool initializeFromFrames, int componentId = 0)
    {
        if (initializeFromFrames)
        {
            using var renderTreeBuilder = new RenderTreeBuilder();
            using var initializeBatchBuilder = new RenderBatchBuilder();

            var emptyFrames = renderTreeBuilder.GetFrames();
            var oldFrames = from.GetFrames();

            RenderTreeDiffBuilder.ComputeDiff(renderer, initializeBatchBuilder, 0, emptyFrames, oldFrames);
        }

        batchBuilder?.Dispose();
        // This gets disposed as part of the test type's Dispose
        batchBuilder = new RenderBatchBuilder();

        var diff = RenderTreeDiffBuilder.ComputeDiff(renderer, batchBuilder, componentId, from.GetFrames(), to.GetFrames());
        batchBuilder.UpdatedComponentDiffs.Append(diff);
        return batchBuilder.ToBatch();
    }

    private static IList<IComponent> GetComponents(RenderTreeBuilder builder)
        => GetComponents<IComponent>(builder);

    private static IList<T> GetComponents<T>(RenderTreeBuilder builder) where T : IComponent
        => builder.GetFrames().AsEnumerable()
            .Where(x => x.FrameType == RenderTreeFrameType.Component)
            .Select(x => (T)x.Component)
            .ToList();

    private static void AddWithKey(RenderTreeBuilder builder, object key, string attributeValue = null)
    {
        builder.OpenElement(0, "el");
        builder.SetKey(key);

        if (attributeValue != null)
        {
            builder.AddAttribute(1, "attrib", attributeValue);
        }

        builder.CloseElement();
    }

    private class FakeRenderer : Renderer
    {
        public FakeRenderer() : base(new TestServiceProvider(), NullLoggerFactory.Instance)
        {
        }

        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        protected override void HandleException(Exception exception)
            => throw new NotImplementedException();

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
            => Task.CompletedTask;
    }

    private class FakeComponent : IComponent
    {
        [Parameter]
        public int IntProperty { get; set; }

        [Parameter]
        public string StringProperty { get; set; }

        [Parameter]
        public object ObjectProperty { get; set; }

        [Parameter]
        public string ReadonlyProperty { get; set; }

        [Parameter]
        public string PrivateProperty { get; set; }

        public string NonParameterProperty { get; set; }

        public void Attach(RenderHandle renderHandle) { }
        public Task SetParametersAsync(ParameterView parameters)
        {
            parameters.SetParameterProperties(this);
            return Task.CompletedTask;
        }
    }

    private class FakeComponent2 : IComponent
    {
        public void Attach(RenderHandle renderHandle)
        {
        }

        public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
    }

    private class CaptureSetParametersComponent : IComponent
    {
        public int SetParametersCallCount { get; private set; }

        public void Attach(RenderHandle renderHandle)
        {
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            SetParametersCallCount++;
            return Task.CompletedTask;
        }
    }

    private class DisposableComponent : IComponent, IDisposable
    {
        public int DisposalCount { get; private set; }
        public void Dispose() => DisposalCount++;

        public void Attach(RenderHandle renderHandle) { }

        public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
    }

    private class NonDisposableComponent : IComponent
    {
        public void Attach(RenderHandle renderHandle) { }

        public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
    }

    private static void AssertEdit(
        RenderTreeEdit edit,
        RenderTreeEditType type,
        int siblingIndex)
    {
        Assert.Equal(type, edit.Type);
        Assert.Equal(siblingIndex, edit.SiblingIndex);
    }

    private static void AssertPermutationListEntry(
        RenderTreeEdit edit,
        int fromSiblingIndex,
        int toSiblingIndex)
    {
        Assert.Equal(RenderTreeEditType.PermutationListEntry, edit.Type);
        Assert.Equal(fromSiblingIndex, edit.SiblingIndex);
        Assert.Equal(toSiblingIndex, edit.MoveToSiblingIndex);
    }

    private static void AssertNamedEventChange(
        NamedEventChange namedEvent,
        NamedEventChangeType type,
        int componentId,
        int frameIndex,
        string eventType,
        string assignedName)
    {
        Assert.Equal(type, namedEvent.ChangeType);
        Assert.Equal(componentId, namedEvent.ComponentId);
        Assert.Equal(frameIndex, namedEvent.FrameIndex);
        Assert.Equal(eventType, namedEvent.EventType);
        Assert.Equal(assignedName, namedEvent.AssignedName);
    }
}
