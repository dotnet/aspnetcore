// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Test;

public class LayoutViewTest
{
    private readonly TestRenderer _renderer;
    private readonly LayoutView _layoutViewComponent;
    private readonly int _layoutViewComponentId;

    public LayoutViewTest()
    {
        _renderer = new TestRenderer();
        _layoutViewComponent = new LayoutView();
        _layoutViewComponentId = _renderer.AssignRootComponentId(_layoutViewComponent);
    }

    [Fact]
    public void GivenNoParameters_RendersNothing()
    {
        // Arrange/Act
        var setParametersTask = _renderer.Dispatcher.InvokeAsync(() => _layoutViewComponent.SetParametersAsync(ParameterView.Empty));
        Assert.True(setParametersTask.IsCompletedSuccessfully);
        var frames = _renderer.GetCurrentRenderTreeFrames(_layoutViewComponentId).AsEnumerable();

        // Assert
        Assert.Single(_renderer.Batches);
        Assert.Empty(frames);
    }

    [Fact]
    public void GivenContentButNoLayout_RendersContent()
    {
        // Arrange/Act
        var setParametersTask = _renderer.Dispatcher.InvokeAsync(() => _layoutViewComponent.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(LayoutView.ChildContent), (RenderFragment)(builder => {
                    builder.AddContent(123, "Hello");
                    builder.AddContent(456, "Goodbye");
                })}
            })));
        Assert.True(setParametersTask.IsCompletedSuccessfully);
        var frames = _renderer.GetCurrentRenderTreeFrames(_layoutViewComponentId).AsEnumerable();

        // Assert
        Assert.Single(_renderer.Batches);
        Assert.Collection(frames,
            frame => AssertFrame.Text(frame, "Hello", 123),
            frame => AssertFrame.Text(frame, "Goodbye", 456));
    }

    [Fact]
    public void GivenLayoutButNoContent_RendersLayoutWithEmptyBody()
    {
        // Arrange/Act
        var setParametersTask = _renderer.Dispatcher.InvokeAsync(() => _layoutViewComponent.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(LayoutView.Layout), typeof(RootLayout) }
            })));

        // Assert
        Assert.True(setParametersTask.IsCompletedSuccessfully);
        var batch = _renderer.Batches.Single();

        var layoutViewFrames = _renderer.GetCurrentRenderTreeFrames(_layoutViewComponentId).AsEnumerable();
        Assert.Collection(layoutViewFrames,
            frame => AssertFrame.Component<RootLayout>(frame, subtreeLength: 2, sequence: 0),
            frame => AssertFrame.Attribute(frame, nameof(LayoutComponentBase.Body), sequence: 1));

        var rootLayoutComponentId = batch.GetComponentFrames<RootLayout>().Single().ComponentId;
        var rootLayoutFrames = _renderer.GetCurrentRenderTreeFrames(rootLayoutComponentId).AsEnumerable();
        Assert.Collection(rootLayoutFrames,
            frame => AssertFrame.Text(frame, "RootLayout starts here", sequence: 0),
            frame => AssertFrame.Region(frame, subtreeLength: 1), // i.e., empty region
            frame => AssertFrame.Text(frame, "RootLayout ends here", sequence: 2));
    }

    [Fact]
    public void RendersContentInsideLayout()
    {
        // Arrange/Act
        var setParametersTask = _renderer.Dispatcher.InvokeAsync(() => _layoutViewComponent.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(LayoutView.Layout), typeof(RootLayout) },
                { nameof(LayoutView.ChildContent), (RenderFragment)(builder => {
                    builder.AddContent(123, "Hello");
                    builder.AddContent(456, "Goodbye");
                })}
            })));

        // Assert
        Assert.True(setParametersTask.IsCompletedSuccessfully);
        var batch = _renderer.Batches.Single();

        var layoutViewFrames = _renderer.GetCurrentRenderTreeFrames(_layoutViewComponentId).AsEnumerable();
        Assert.Collection(layoutViewFrames,
            frame => AssertFrame.Component<RootLayout>(frame, subtreeLength: 2, sequence: 0),
            frame => AssertFrame.Attribute(frame, nameof(LayoutComponentBase.Body), sequence: 1));

        var rootLayoutComponentId = batch.GetComponentFrames<RootLayout>().Single().ComponentId;
        var rootLayoutFrames = _renderer.GetCurrentRenderTreeFrames(rootLayoutComponentId).AsEnumerable();
        Assert.Collection(rootLayoutFrames,
            frame => AssertFrame.Text(frame, "RootLayout starts here", sequence: 0),
            frame => AssertFrame.Region(frame, subtreeLength: 3),
            frame => AssertFrame.Text(frame, "Hello", sequence: 123),
            frame => AssertFrame.Text(frame, "Goodbye", sequence: 456),
            frame => AssertFrame.Text(frame, "RootLayout ends here", sequence: 2));
    }

    [Fact]
    public void RendersContentInsideNestedLayout()
    {
        // Arrange/Act
        var setParametersTask = _renderer.Dispatcher.InvokeAsync(() => _layoutViewComponent.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(LayoutView.Layout), typeof(NestedLayout) },
                { nameof(LayoutView.ChildContent), (RenderFragment)(builder => {
                    builder.AddContent(123, "Hello");
                    builder.AddContent(456, "Goodbye");
                })}
            })));

        // Assert
        Assert.True(setParametersTask.IsCompletedSuccessfully);
        var batch = _renderer.Batches.Single();

        var layoutViewFrames = _renderer.GetCurrentRenderTreeFrames(_layoutViewComponentId).AsEnumerable();
        Assert.Collection(layoutViewFrames,
            frame => AssertFrame.Component<RootLayout>(frame, subtreeLength: 2, sequence: 0),
            frame => AssertFrame.Attribute(frame, nameof(LayoutComponentBase.Body), sequence: 1));

        var rootLayoutComponentId = batch.GetComponentFrames<RootLayout>().Single().ComponentId;
        var rootLayoutFrames = _renderer.GetCurrentRenderTreeFrames(rootLayoutComponentId).AsEnumerable();
        Assert.Collection(rootLayoutFrames,
            frame => AssertFrame.Text(frame, "RootLayout starts here", sequence: 0),
            frame => AssertFrame.Region(frame, subtreeLength: 3, sequence: 1),
            frame => AssertFrame.Component<NestedLayout>(frame, subtreeLength: 2, sequence: 0),
            frame => AssertFrame.Attribute(frame, nameof(LayoutComponentBase.Body), sequence: 1),
            frame => AssertFrame.Text(frame, "RootLayout ends here", sequence: 2));

        var nestedLayoutComponentId = batch.GetComponentFrames<NestedLayout>().Single().ComponentId;
        var nestedLayoutFrames = _renderer.GetCurrentRenderTreeFrames(nestedLayoutComponentId).AsEnumerable();
        Assert.Collection(nestedLayoutFrames,
            frame => AssertFrame.Text(frame, "NestedLayout starts here", sequence: 0),
            frame => AssertFrame.Region(frame, subtreeLength: 3, sequence: 1),
            frame => AssertFrame.Text(frame, "Hello", sequence: 123),
            frame => AssertFrame.Text(frame, "Goodbye", sequence: 456),
            frame => AssertFrame.Text(frame, "NestedLayout ends here", sequence: 2));
    }

    [Fact]
    public void CanChangeContentWithSameLayout()
    {
        // Arrange
        var setParametersTask = _renderer.Dispatcher.InvokeAsync(() => _layoutViewComponent.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(LayoutView.Layout), typeof(NestedLayout) },
                { nameof(LayoutView.ChildContent), (RenderFragment)(builder => {
                    builder.AddContent(0, "Initial content");
                })}
            })));

        // Act
        Assert.True(setParametersTask.IsCompletedSuccessfully);
        _renderer.Dispatcher.InvokeAsync(() => _layoutViewComponent.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(LayoutView.Layout), typeof(NestedLayout) },
                { nameof(LayoutView.ChildContent), (RenderFragment)(builder => {
                    builder.AddContent(0, "Changed content");
                })}
            })));

        // Assert
        Assert.Equal(2, _renderer.Batches.Count);
        var batch = _renderer.Batches[1];
        Assert.Empty(batch.DisposedComponentIDs);
        Assert.Collection(batch.DiffsInOrder,
            diff => Assert.Empty(diff.Edits), // LayoutView rerendered, but with no changes
            diff => Assert.Empty(diff.Edits), // RootLayout rerendered, but with no changes
            diff =>
            {
                // NestedLayout rerendered, patching content in place
                Assert.Collection(diff.Edits, edit =>
                {
                    Assert.Equal(RenderTreeEditType.UpdateText, edit.Type);
                    Assert.Equal(1, edit.SiblingIndex);
                    AssertFrame.Text(
                        batch.ReferenceFrames[edit.ReferenceFrameIndex],
                        "Changed content",
                        sequence: 0);
                });
            });
    }

    [Fact]
    public void CanChangeLayout()
    {
        // Arrange
        var setParametersTask1 = _renderer.Dispatcher.InvokeAsync(() => _layoutViewComponent.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(LayoutView.Layout), typeof(NestedLayout) },
                { nameof(LayoutView.ChildContent), (RenderFragment)(builder => {
                    builder.AddContent(0, "Some content");
                })}
            })));
        Assert.True(setParametersTask1.IsCompletedSuccessfully);

        // Act
        var setParametersTask2 = _renderer.Dispatcher.InvokeAsync(() => _layoutViewComponent.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(LayoutView.Layout), typeof(OtherNestedLayout) },
            })));

        // Assert
        Assert.True(setParametersTask2.IsCompletedSuccessfully);
        Assert.Equal(2, _renderer.Batches.Count);
        var batch = _renderer.Batches[1];
        Assert.Single(batch.DisposedComponentIDs); // Disposes NestedLayout
        Assert.Collection(batch.DiffsInOrder,
            diff => Assert.Empty(diff.Edits), // LayoutView rerendered, but with no changes
            diff =>
            {
                // RootLayout rerendered, changing child
                Assert.Collection(diff.Edits,
                edit =>
                {
                    Assert.Equal(RenderTreeEditType.RemoveFrame, edit.Type);
                    Assert.Equal(1, edit.SiblingIndex);
                },
                edit =>
                {
                    Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                    Assert.Equal(1, edit.SiblingIndex);
                    AssertFrame.Component<OtherNestedLayout>(
                        batch.ReferenceFrames[edit.ReferenceFrameIndex],
                        sequence: 0);
                });
            },
            diff =>
            {
                // Inserts new OtherNestedLayout
                Assert.Collection(diff.Edits,
                edit =>
                {
                    Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                    Assert.Equal(0, edit.SiblingIndex);
                    AssertFrame.Text(
                        batch.ReferenceFrames[edit.ReferenceFrameIndex],
                        "OtherNestedLayout starts here");
                },
                edit =>
                {
                    Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                    Assert.Equal(1, edit.SiblingIndex);
                    AssertFrame.Text(
                        batch.ReferenceFrames[edit.ReferenceFrameIndex],
                        "Some content");
                },
                edit =>
                {
                    Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                    Assert.Equal(2, edit.SiblingIndex);
                    AssertFrame.Text(
                        batch.ReferenceFrames[edit.ReferenceFrameIndex],
                        "OtherNestedLayout ends here");
                });
            });
    }

    private class RootLayout : AutoRenderComponent
    {
        [Parameter]
        public RenderFragment Body { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (Body == null)
            {
                // Prove that we don't expect layouts to tolerate null values for Body
                throw new InvalidOperationException("Got a null body when not expecting it");
            }

            builder.AddContent(0, "RootLayout starts here");
            builder.AddContent(1, Body);
            builder.AddContent(2, "RootLayout ends here");
        }
    }

    [Layout(typeof(RootLayout))]
    private class NestedLayout : AutoRenderComponent
    {
        [Parameter]
        public RenderFragment Body { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, "NestedLayout starts here");
            builder.AddContent(1, Body);
            builder.AddContent(2, "NestedLayout ends here");
        }
    }

    [Layout(typeof(RootLayout))]
    private class OtherNestedLayout : AutoRenderComponent
    {
        [Parameter]
        public RenderFragment Body { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, "OtherNestedLayout starts here");
            builder.AddContent(1, Body);
            builder.AddContent(2, "OtherNestedLayout ends here");
        }
    }
}
