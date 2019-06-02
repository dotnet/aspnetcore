// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Ignitor
{
    internal class ElementHive
    {
        public Dictionary<int, ComponentNode> Components { get; } = new Dictionary<int, ComponentNode>();

        public void Update(RenderBatch batch)
        {
            for (var i = 0; i < batch.UpdatedComponents.Count; i++)
            {
                var diff = batch.UpdatedComponents.Array[i];
                var componentId = diff.ComponentId;
                var edits = diff.Edits;
                UpdateComponent(batch, componentId, edits);
            }

            for (var i = 0; i < batch.DisposedComponentIDs.Count; i++)
            {
                DisposeComponent(batch.DisposedComponentIDs.Array[i]);
            }

            for (var i = 0; i < batch.DisposedEventHandlerIDs.Count; i++)
            {
                DisposeEventHandler(batch.DisposedEventHandlerIDs.Array[i]);
            }
        }

        private void UpdateComponent(RenderBatch batch, int componentId, ArraySegment<RenderTreeEdit> edits)
        {
            if (!Components.TryGetValue(componentId, out var component))
            {
                component = new ComponentNode(componentId);
                Components.Add(componentId, component);
            }

            ApplyEdits(batch, component, 0, edits);
        }

        private void DisposeComponent(int componentId)
        {

        }

        private void DisposeEventHandler(int eventHandlerId)
        {

        }

        private void ApplyEdits(RenderBatch batch, Node parent, int childIndex, ArraySegment<RenderTreeEdit> edits)
        {
            var currentDepth = 0;
            var childIndexAtCurrentDepth = childIndex;
            var permutations = new List<PermutationListEntry>();

            for (var editIndex = edits.Offset; editIndex < edits.Offset + edits.Count; editIndex++)
            {
                var edit = edits.Array[editIndex];
                switch (edit.Type)
                {
                    case RenderTreeEditType.PrependFrame:
                        {
                            var frame = batch.ReferenceFrames.Array[edit.ReferenceFrameIndex];
                            var siblingIndex = edit.SiblingIndex;
                            insertFrame(batch, parent, childIndexAtCurrentDepth + siblingIndex, frame, edit.ReferenceFrameIndex);
                            break;
                        }

                    case RenderTreeEditType.RemoveFrame:
                        {
                            var siblingIndex = edit.SiblingIndex;
                            removeLogicalChild(parent, childIndexAtCurrentDepth + siblingIndex);
                            break;
                        }

                    case RenderTreeEditType.SetAttribute:
                        {
                            var frame = batch.ReferenceFrames.Array[edit.ReferenceFrameIndex];
                            var siblingIndex = edit.SiblingIndex;
                            var element = getLogicalChild(parent, childIndexAtCurrentDepth + siblingIndex);
                            if (element is ElementNode)
                            {
                                applyAttribute(batch, element, frame);
                            }
                            else
                            {
                                throw new Exception("Cannot set attribute on non-element child");
                            }
                            break;
                        }

                    case RenderTreeEditType.RemoveAttribute:
                        {
                            // Note that we don't have to dispose the info we track about event handlers here, because the
                            // disposed event handler IDs are delivered separately (in the 'disposedEventHandlerIds' array)
                            var siblingIndex = edit.SiblingIndex;
                            var element = getLogicalChild(parent, childIndexAtCurrentDepth + siblingIndex);
                            if (element is ElementNode)
                            {
                                var attributeName = edit.RemovedAttributeName;

                                // First try to remove any special property we use for this attribute
                                if (!tryApplySpecialProperty(batch, element, attributeName, null))
                                {
                                    // If that's not applicable, it's a regular DOM attribute so remove that
                                    element.removeAttribute(attributeName);
                                }
                            }
                            else
                            {
                                throw new Exception("Cannot remove attribute from non-element child");
                            }
                            break;
                        }

                    case RenderTreeEditType.UpdateText:
                        {
                            var frame = batch.ReferenceFrames.Array[edit.ReferenceFrameIndex];
                            var siblingIndex = edit.SiblingIndex;
                            var textNode = getLogicalChild(parent, childIndexAtCurrentDepth + siblingIndex);
                            if (textNode is TextNode)
                            {
                                textNode.textContent = frame.TextContent;
                            }
                            else
                            {
                                throw new Exception("Cannot set text content on non-text child");
                            }
                            break;
                        }


                    case RenderTreeEditType.UpdateMarkup:
                        {
                            var frame = batch.ReferenceFrames.Array[edit.ReferenceFrameIndex];
                            var siblingIndex = edit.SiblingIndex;
                            removeLogicalChild(parent, childIndexAtCurrentDepth + siblingIndex);
                            insertMarkup(batch, parent, childIndexAtCurrentDepth + siblingIndex, frame);
                            break;
                        }

                    case RenderTreeEditType.StepIn:
                        {
                            var siblingIndex = edit.SiblingIndex;
                            parent = getLogicalChild(parent, childIndexAtCurrentDepth + siblingIndex);
                            currentDepth++;
                            childIndexAtCurrentDepth = 0;
                            break;
                        }

                    case RenderTreeEditType.StepOut:
                        {
                            parent = getLogicalParent(parent)!;
                            currentDepth--;
                            childIndexAtCurrentDepth = currentDepth == 0 ? childIndex : 0; // The childIndex is only ever nonzero at zero depth
                            break;
                        }

                    case RenderTreeEditType.PermutationListEntry:
                        {
                            permutations.Add(new PermutationListEntry(childIndexAtCurrentDepth + edit.SiblingIndex, childIndexAtCurrentDepth + edit.MoveToSiblingIndex));,
                            break;
                        }

                    case RenderTreeEditType.PermutationListEnd:
                        {
                            permuteLogicalChildren(parent, permutations!);
                            permutations.Clear();
                            break;
                        }

                    default:
                        {
                            throw new Exception($"Unknown edit type: '{edit.Type}'");
                        }
                }
            }
        }

        private int InsertFrame(RenderBatch batch, ContainerNode parent, int childIndex, ArraySegment<RenderTreeFrame> frames, RenderTreeFrame frame, int frameIndex)
        {
            switch (frame.FrameType)
            {
                case RenderTreeFrameType.Element:
                    {
                        InsertElement(batch, parent, childIndex, frame, frameIndex);
                        return 1;
                    }

                case RenderTreeFrameType.Text:
                    {
                        insertText(batch, parent, childIndex, frame);
                        return 1;
                    }

                case RenderTreeFrameType.Attribute:
                    {
                        throw new Exception("Attribute frames should only be present as leading children of element frames.");
                    }

                case RenderTreeFrameType.Component:
                    {
                        insertComponent(batch, parent, childIndex, frame);
                        return 1;
                    }

                case RenderTreeFrameType.Region:
                    {
                        return insertFrameRange(batch, parent, childIndex, frames, frameIndex + 1, frameIndex + frameReader.subtreeLength(frame));
                    }

                case RenderTreeFrameType.ElementReferenceCapture:
                    {
                        // No action for reference captures.
                        break;
                    }

                case RenderTreeFrameType.Markup:
                    {
                        insertMarkup(batch, parent, childIndex, frame);
                        return 1;
                    }

                default:
                    {
                        throw new Exception($"Unknown frame type: {frame.FrameType}");
                    }
            }
        }

        private void InsertElement(RenderBatch batch, ContainerNode parent, int childIndex, RenderTreeFrame frame, int frameIndex)
        {
            // Note: we don't handle SVG here
            var newElement = new ElementNode(frame.ElementName);
            parent.InsertLogicalChild(newElement, childIndex);

            // Apply attributes
            for (var i = frameIndex + 1; i < frameIndex + frame.ElementSubtreeLength; i++)
            {
                var descendantFrame = batch.ReferenceFrames.Array[i];
                if (descendantFrame.FrameType == RenderTreeFrameType.Attribute)
                {
                    applyAttribute(batch, newElement, descendantFrame);
                }
                else
                {
                    // As soon as we see a non-attribute child, all the subsequent child frames are
                    // not attributes, so bail out and insert the remnants recursively
                    insertFrameRange(batch, newElement, 0, i, frameIndex + frame.ElementSubtreeLength);
                    break;
                }
            }
        }

        private readonly struct PermutationListEntry
        {
            public readonly int From;
            public readonly int To;

            public PermutationListEntry(int from, int to)
            {
                From = from;
                To = to;
            }
        }
    }
}
