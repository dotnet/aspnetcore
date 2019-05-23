// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Layouts;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Components.Test
{
    public class PageDisplayTest
    {
        private TestRenderer _renderer = new TestRenderer();
        private PageDisplay _pageDisplayComponent = new PageDisplay();
        private int _pageDisplayComponentId;

        public PageDisplayTest()
        {
            _renderer = new TestRenderer();
            _pageDisplayComponent = new PageDisplay();
            _pageDisplayComponentId = _renderer.AssignRootComponentId(_pageDisplayComponent);
        }

        [Fact]
        public void DisplaysComponentInsideLayout()
        {
            // Arrange/Act
            _renderer.Invoke(() => _pageDisplayComponent.SetParametersAsync(ParameterCollection.FromDictionary(new Dictionary<string, object>
            {
                { PageDisplay.NameOfPage, typeof(ComponentWithLayout) }
            })));

            // Assert
            var batch = _renderer.Batches.Single();
            Assert.Collection(batch.DiffsInOrder,
                diff =>
                {
                    // First is the LayoutDisplay component, which contains a RootLayout
                    var singleEdit = diff.Edits.Single();
                    Assert.Equal(RenderTreeEditType.PrependFrame, singleEdit.Type);
                    AssertFrame.Component<RootLayout>(
                        batch.ReferenceFrames[singleEdit.ReferenceFrameIndex]);
                },
                diff =>
                {
                    // ... then a RootLayout which contains a ComponentWithLayout
                    // First is the LayoutDisplay component, which contains a RootLayout
                    Assert.Collection(diff.Edits,
                        edit =>
                        {
                            Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                            AssertFrame.Text(
                                batch.ReferenceFrames[edit.ReferenceFrameIndex],
                                "RootLayout starts here");
                        },
                        edit =>
                        {
                            Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                            AssertFrame.Component<ComponentWithLayout>(
                                batch.ReferenceFrames[edit.ReferenceFrameIndex]);
                        },
                        edit =>
                        {
                            Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                            AssertFrame.Text(
                                batch.ReferenceFrames[edit.ReferenceFrameIndex],
                                "RootLayout ends here");
                        });
                },
                diff =>
                {
                    // ... then the ComponentWithLayout
                    var singleEdit = diff.Edits.Single();
                    Assert.Equal(RenderTreeEditType.PrependFrame, singleEdit.Type);
                    AssertFrame.Text(
                        batch.ReferenceFrames[singleEdit.ReferenceFrameIndex],
                        $"{nameof(ComponentWithLayout)} is here.");
                });
        }

        [Fact]
        public void DisplaysComponentInsideNestedLayout()
        {
            // Arrange/Act
            _renderer.Invoke(() => _pageDisplayComponent.SetParametersAsync(ParameterCollection.FromDictionary(new Dictionary<string, object>
            {
                { PageDisplay.NameOfPage, typeof(ComponentWithNestedLayout) }
            })));

            // Assert
            var batch = _renderer.Batches.Single();
            Assert.Collection(batch.DiffsInOrder,
                // First, a LayoutDisplay containing a RootLayout
                diff => AssertFrame.Component<RootLayout>(
                    batch.ReferenceFrames[diff.Edits[0].ReferenceFrameIndex]),
                // Then a RootLayout containing a NestedLayout
                diff => AssertFrame.Component<NestedLayout>(
                    batch.ReferenceFrames[diff.Edits[1].ReferenceFrameIndex]),
                // Then a NestedLayout containing a ComponentWithNestedLayout
                diff => AssertFrame.Component<ComponentWithNestedLayout>(
                    batch.ReferenceFrames[diff.Edits[1].ReferenceFrameIndex]),
                // Then the ComponentWithNestedLayout
                diff => AssertFrame.Text(
                    batch.ReferenceFrames[diff.Edits[0].ReferenceFrameIndex],
                    $"{nameof(ComponentWithNestedLayout)} is here."));
        }

        [Fact]
        public void CanChangeDisplayedPageWithSameLayout()
        {
            // Arrange
            _renderer.Invoke(() => _pageDisplayComponent.SetParametersAsync(ParameterCollection.FromDictionary(new Dictionary<string, object>
            {
                { PageDisplay.NameOfPage, typeof(ComponentWithLayout) }
            })));

            // Act
            _renderer.Invoke(() => _pageDisplayComponent.SetParametersAsync(ParameterCollection.FromDictionary(new Dictionary<string, object>
            {
                { PageDisplay.NameOfPage, typeof(DifferentComponentWithLayout) }
            })));

            // Assert
            Assert.Equal(2, _renderer.Batches.Count);
            var batch = _renderer.Batches[1];
            Assert.Equal(1, batch.DisposedComponentIDs.Count); // Disposed only the inner page component
            Assert.Collection(batch.DiffsInOrder,
                diff => Assert.Empty(diff.Edits), // LayoutDisplay rerendered, but with no changes
                diff =>
                {
                    // RootLayout rerendered
                    Assert.Collection(diff.Edits,
                        edit =>
                        {
                            // Removed old page
                            Assert.Equal(RenderTreeEditType.RemoveFrame, edit.Type);
                            Assert.Equal(1, edit.SiblingIndex);
                        },
                        edit =>
                        {
                            // Inserted new one
                            Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                            Assert.Equal(1, edit.SiblingIndex);
                            AssertFrame.Component<DifferentComponentWithLayout>(
                                batch.ReferenceFrames[edit.ReferenceFrameIndex]);
                        });
                },
                diff =>
                {
                    // New page rendered
                    var singleEdit = diff.Edits.Single();
                    Assert.Equal(RenderTreeEditType.PrependFrame, singleEdit.Type);
                    AssertFrame.Text(
                        batch.ReferenceFrames[singleEdit.ReferenceFrameIndex],
                        $"{nameof(DifferentComponentWithLayout)} is here.");
                });
        }

        [Fact]
        public void CanChangeDisplayedPageWithDifferentLayout()
        {
            // Arrange
            _renderer.Invoke(() => _pageDisplayComponent.SetParametersAsync(ParameterCollection.FromDictionary(new Dictionary<string, object>
            {
                { PageDisplay.NameOfPage, typeof(ComponentWithLayout) }
            })));

            // Act
            _renderer.Invoke(() => _pageDisplayComponent.SetParametersAsync(ParameterCollection.FromDictionary(new Dictionary<string, object>
            {
                { PageDisplay.NameOfPage, typeof(ComponentWithNestedLayout) }
            })));

            // Assert
            Assert.Equal(2, _renderer.Batches.Count);
            var batch = _renderer.Batches[1];
            Assert.Equal(1, batch.DisposedComponentIDs.Count); // Disposed only the inner page component
            Assert.Collection(batch.DiffsInOrder,
                diff => Assert.Empty(diff.Edits), // LayoutDisplay rerendered, but with no changes
                diff =>
                {
                    // RootLayout rerendered
                    Assert.Collection(diff.Edits,
                        edit =>
                        {
                            // Removed old page
                            Assert.Equal(RenderTreeEditType.RemoveFrame, edit.Type);
                            Assert.Equal(1, edit.SiblingIndex);
                        },
                        edit =>
                        {
                            // Inserted new nested layout
                            Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                            Assert.Equal(1, edit.SiblingIndex);
                            AssertFrame.Component<NestedLayout>(
                                batch.ReferenceFrames[edit.ReferenceFrameIndex]);
                        });
                },
                diff =>
                {
                    // New nested layout rendered
                    var edit = diff.Edits[1];
                    Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                    AssertFrame.Component<ComponentWithNestedLayout>(
                        batch.ReferenceFrames[edit.ReferenceFrameIndex]);
                },
                diff =>
                {
                    // New inner page rendered
                    var singleEdit = diff.Edits.Single();
                    Assert.Equal(RenderTreeEditType.PrependFrame, singleEdit.Type);
                    AssertFrame.Text(
                        batch.ReferenceFrames[singleEdit.ReferenceFrameIndex],
                        $"{nameof(ComponentWithNestedLayout)} is here.");
                });
        }

        private class RootLayout : AutoRenderComponent
        {
            [Parameter]
            RenderFragment Body { get; set; }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.AddContent(0, "RootLayout starts here");
                builder.AddContent(1, Body);
                builder.AddContent(2, "RootLayout ends here");
            }
        }

        [Layout(typeof(RootLayout))]
        private class NestedLayout : AutoRenderComponent
        {
            [Parameter]
            RenderFragment Body { get; set; }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.AddContent(0, "NestedLayout starts here");
                builder.AddContent(1, Body);
                builder.AddContent(2, "NestedLayout ends here");
            }
        }

        [Layout(typeof(RootLayout))]
        private class ComponentWithLayout : AutoRenderComponent
        {
            protected override void BuildRenderTree(RenderTreeBuilder builder)
                => builder.AddContent(0, $"{nameof(ComponentWithLayout)} is here.");
        }

        [Layout(typeof(RootLayout))]
        private class DifferentComponentWithLayout : AutoRenderComponent
        {
            protected override void BuildRenderTree(RenderTreeBuilder builder)
                => builder.AddContent(0, $"{nameof(DifferentComponentWithLayout)} is here.");
        }

        [Layout(typeof(NestedLayout))]
        private class ComponentWithNestedLayout : AutoRenderComponent
        {
            protected override void BuildRenderTree(RenderTreeBuilder builder)
                => builder.AddContent(0, $"{nameof(ComponentWithNestedLayout)} is here.");
        }
    }
}
