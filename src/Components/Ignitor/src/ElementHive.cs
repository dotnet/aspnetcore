// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

#nullable enable
namespace Ignitor
{
    public class ElementHive
    {
        private const string SelectValuePropname = "_blazorSelectValue";

        public Dictionary<int, ComponentNode> Components { get; } = new Dictionary<int, ComponentNode>();

        public string SerializedValue => NodeSerializer.Serialize(this);

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

        public bool TryFindElementById(string id, [NotNullWhen(true)] out ElementNode? element)
        {
            foreach (var kvp in Components)
            {
                var component = kvp.Value;
                if (TryGetElementFromChildren(component, id, out element))
                {
                    return true;
                }
            }

            element = null;
            return false;
        }

        bool TryGetElementFromChildren(Node node, string id, [NotNullWhen(true)] out ElementNode? foundNode)
        {
            if (node is ElementNode elementNode &&
                elementNode.Attributes.TryGetValue("id", out var elementId) &&
                elementId.ToString() == id)
            {
                foundNode = elementNode;
                return true;
            }

            if (node is ContainerNode containerNode)
            {
                for (var i = 0; i < containerNode.Children.Count; i++)
                {
                    if (TryGetElementFromChildren(containerNode.Children[i], id, out foundNode))
                    {
                        return true;
                    }
                }
            }

            foundNode = null;
            return false;
        }

        private void UpdateComponent(RenderBatch batch, int componentId, ArrayBuilderSegment<RenderTreeEdit> edits)
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

        private void DisposeEventHandler(ulong eventHandlerId)
        {

        }

        private void ApplyEdits(RenderBatch batch, ContainerNode parent, int childIndex, ArrayBuilderSegment<RenderTreeEdit> edits)
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
                            InsertFrame(batch, parent, childIndexAtCurrentDepth + siblingIndex, batch.ReferenceFrames.Array, frame, edit.ReferenceFrameIndex);
                            break;
                        }

                    case RenderTreeEditType.RemoveFrame:
                        {
                            var siblingIndex = edit.SiblingIndex;
                            parent.RemoveLogicalChild(childIndexAtCurrentDepth + siblingIndex);
                            break;
                        }

                    case RenderTreeEditType.SetAttribute:
                        {
                            var frame = batch.ReferenceFrames.Array[edit.ReferenceFrameIndex];
                            var siblingIndex = edit.SiblingIndex;
                            var node = parent.Children[childIndexAtCurrentDepth + siblingIndex];
                            if (node is ElementNode element)
                            {
                                ApplyAttribute(batch, element, frame);
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
                            var node = parent.Children[childIndexAtCurrentDepth + siblingIndex];
                            if (node is ElementNode element)
                            {
                                var attributeName = edit.RemovedAttributeName;

                                // First try to remove any special property we use for this attribute
                                if (!TryApplySpecialProperty(batch, element, attributeName, default))
                                {
                                    // If that's not applicable, it's a regular DOM attribute so remove that
                                    element.RemoveAttribute(attributeName);
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
                            var node = parent.Children[childIndexAtCurrentDepth + siblingIndex];
                            if (node is TextNode textNode)
                            {
                                textNode.TextContent = frame.TextContent;
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
                            parent.RemoveLogicalChild(childIndexAtCurrentDepth + siblingIndex);
                            InsertMarkup(parent, childIndexAtCurrentDepth + siblingIndex, frame);
                            break;
                        }

                    case RenderTreeEditType.StepIn:
                        {
                            var siblingIndex = edit.SiblingIndex;
                            parent = (ContainerNode)parent.Children[childIndexAtCurrentDepth + siblingIndex];
                            currentDepth++;
                            childIndexAtCurrentDepth = 0;
                            break;
                        }

                    case RenderTreeEditType.StepOut:
                        {
                            parent = parent.Parent ?? throw new InvalidOperationException($"Cannot step out of {parent}");
                            currentDepth--;
                            childIndexAtCurrentDepth = currentDepth == 0 ? childIndex : 0; // The childIndex is only ever nonzero at zero depth
                            break;
                        }

                    case RenderTreeEditType.PermutationListEntry:
                        {
                            permutations.Add(new PermutationListEntry(childIndexAtCurrentDepth + edit.SiblingIndex, childIndexAtCurrentDepth + edit.MoveToSiblingIndex));
                            break;
                        }

                    case RenderTreeEditType.PermutationListEnd:
                        {
                            throw new NotSupportedException();
                            //permuteLogicalChildren(parent, permutations!);
                            //permutations.Clear();
                            //break;
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
                        InsertElement(batch, parent, childIndex, frames, frame, frameIndex);
                        return 1;
                    }

                case RenderTreeFrameType.Text:
                    {
                        InsertText(parent, childIndex, frame);
                        return 1;
                    }

                case RenderTreeFrameType.Attribute:
                    {
                        throw new Exception("Attribute frames should only be present as leading children of element frames.");
                    }

                case RenderTreeFrameType.Component:
                    {
                        InsertComponent(parent, childIndex, frame);
                        return 1;
                    }

                case RenderTreeFrameType.Region:
                    {
                        return InsertFrameRange(batch, parent, childIndex, frames, frameIndex + 1, frameIndex + CountDescendantFrames(frame));
                    }

                case RenderTreeFrameType.ElementReferenceCapture:
                    {
                        // No action for reference captures.
                        break;
                    }

                case RenderTreeFrameType.Markup:
                    {
                        InsertMarkup(parent, childIndex, frame);
                        return 1;
                    }

            }

            throw new Exception($"Unknown frame type: {frame.FrameType}");
        }

        private void InsertText(ContainerNode parent, int childIndex, RenderTreeFrame frame)
        {
            var textContent = frame.TextContent;
            var newTextNode = new TextNode(textContent);
            parent.InsertLogicalChild(newTextNode, childIndex);
        }

        private void InsertComponent(ContainerNode parent, int childIndex, RenderTreeFrame frame)
        {
            // All we have to do is associate the child component ID with its location. We don't actually
            // do any rendering here, because the diff for the child will appear later in the render batch.
            var childComponentId = frame.ComponentId;
            var containerElement = parent.CreateAndInsertComponent(childComponentId, childIndex);

            Components[childComponentId] = containerElement;
        }

        private int InsertFrameRange(RenderBatch batch, ContainerNode parent, int childIndex, ArraySegment<RenderTreeFrame> frames, int startIndex, int endIndexExcl)
        {
            var origChildIndex = childIndex;
            for (var index = startIndex; index < endIndexExcl; index++)
            {
                var frame = batch.ReferenceFrames.Array[index];
                var numChildrenInserted = InsertFrame(batch, parent, childIndex, frames, frame, index);
                childIndex += numChildrenInserted;

                // Skip over any descendants, since they are already dealt with recursively
                index += CountDescendantFrames(frame);
            }

            return (childIndex - origChildIndex); // Total number of children inserted
        }

        private void InsertElement(RenderBatch batch, ContainerNode parent, int childIndex, ArraySegment<RenderTreeFrame> frames, RenderTreeFrame frame, int frameIndex)
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
                    ApplyAttribute(batch, newElement, descendantFrame);
                }
                else
                {
                    // As soon as we see a non-attribute child, all the subsequent child frames are
                    // not attributes, so bail out and insert the remnants recursively
                    InsertFrameRange(batch, newElement, 0, frames, i, frameIndex + frame.ElementSubtreeLength);
                    break;
                }
            }
        }

        private void ApplyAttribute(RenderBatch batch, ElementNode elementNode, RenderTreeFrame attributeFrame)
        {
            var attributeName = attributeFrame.AttributeName;
            var eventHandlerId = attributeFrame.AttributeEventHandlerId;

            if (eventHandlerId != 0)
            {
                var firstTwoChars = attributeName.Substring(0, 2);
                var eventName = attributeName.Substring(2);
                if (firstTwoChars != "on" || string.IsNullOrEmpty(eventName))
                {
                    throw new InvalidOperationException($"Attribute has nonzero event handler ID, but attribute name '${attributeName}' does not start with 'on'.");
                }
                var descriptor = new ElementNode.ElementEventDescriptor(eventName, eventHandlerId);
                elementNode.SetEvent(eventName, descriptor);

                return;
            }

            // First see if we have special handling for this attribute
            if (!TryApplySpecialProperty(batch, elementNode, attributeName, attributeFrame))
            {
                // If not, treat it as a regular string-valued attribute
                elementNode.SetAttribute(
                  attributeName,
                  attributeFrame.AttributeValue);
            }
        }

        private bool TryApplySpecialProperty(RenderBatch batch, ElementNode element, string attributeName, RenderTreeFrame attributeFrame)
        {
            switch (attributeName)
            {
                case "value":
                    return TryApplyValueProperty(element, attributeFrame);
                case "checked":
                    return TryApplyCheckedProperty(element, attributeFrame);
                default:
                    return false;
            }
        }



        private bool TryApplyValueProperty(ElementNode element, RenderTreeFrame attributeFrame)
        {
            // Certain elements have built-in behaviour for their 'value' property
            switch (element.TagName)
            {
                case "INPUT":
                case "SELECT":
                case "TEXTAREA":
                    {
                        var value = attributeFrame.AttributeValue;
                        element.SetProperty("value", value);

                        if (element.TagName == "SELECT")
                        {
                            // <select> is special, in that anything we write to .value will be lost if there
                            // isn't yet a matching <option>. To maintain the expected behavior no matter the
                            // element insertion/update order, preserve the desired value separately so
                            // we can recover it when inserting any matching <option>.
                            element.SetProperty(SelectValuePropname, value);
                        }
                        return true;
                    }
                case "OPTION":
                    {
                        var value = attributeFrame.AttributeValue;
                        if (value != null)
                        {
                            element.SetAttribute("value", value);
                        }
                        else
                        {
                            element.RemoveAttribute("value");
                        }
                        return true;
                    }
                default:
                    return false;
            }
        }

        private bool TryApplyCheckedProperty(ElementNode element, RenderTreeFrame attributeFrame)
        {
            // Certain elements have built-in behaviour for their 'checked' property
            if (element.TagName == "INPUT")
            {
                var value = attributeFrame.AttributeValue;
                element.SetProperty("checked", value);
                return true;
            }

            return false;
        }

        private void InsertMarkup(ContainerNode parent, int childIndex, RenderTreeFrame markupFrame)
        {
            var markupContainer = parent.CreateAndInsertContainer(childIndex);
            var markupContent = markupFrame.MarkupContent;
            var markupNode = new MarkupNode(markupContent);
            markupContainer.InsertLogicalChild(markupNode, childIndex);
        }

        private int CountDescendantFrames(RenderTreeFrame frame)
        {
            switch (frame.FrameType)
            {
                // The following frame types have a subtree length. Other frames may use that memory slot
                // to mean something else, so we must not read it. We should consider having nominal subtypes
                // of RenderTreeFramePointer that prevent access to non-applicable fields.
                case RenderTreeFrameType.Component:
                    return frame.ComponentSubtreeLength - 1;
                case RenderTreeFrameType.Element:
                    return frame.ElementSubtreeLength - 1;
                case RenderTreeFrameType.Region:
                    return frame.RegionSubtreeLength - 1;
                default:
                    return 0;
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
#nullable restore
