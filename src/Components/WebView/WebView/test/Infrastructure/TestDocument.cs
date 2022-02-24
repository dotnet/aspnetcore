// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.WebView.Document;

public class TestDocument
{
    private const string SelectValuePropname = "_blazorSelectValue";

    private readonly Dictionary<long, ComponentNode> _componentsById = new();

    public void AddRootComponent(int componentId, string selector)
    {
        if (_componentsById.ContainsKey(componentId))
        {
            throw new InvalidOperationException($"Component with Id '{componentId}' already exists.");
        }

        _componentsById.Add(componentId, new RootComponentNode(componentId, selector));
    }

    public void ApplyChanges(RenderBatch batch)
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

    private void UpdateComponent(RenderBatch batch, int componentId, ArrayBuilderSegment<RenderTreeEdit> edits)
    {
        if (!_componentsById.TryGetValue(componentId, out var component))
        {
            component = new ComponentNode(componentId);
            _componentsById.Add(componentId, component);
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
                            textNode.Text = frame.TextContent;
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
                    return InsertFrameRange(batch, parent, childIndex, frames, frameIndex + 1, frameIndex + frame.RegionSubtreeLength);
                }

            case RenderTreeFrameType.ElementReferenceCapture:
                {
                    if (parent is ElementNode)
                    {
                        return 0; // A "capture" is a child in the diff, but has no node in the DOM
                    }
                    else
                    {
                        throw new Exception("Reference capture frames can only be children of element frames.");
                    }
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

        _componentsById[childComponentId] = containerElement;
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

        return childIndex - origChildIndex; // Total number of children inserted
    }

    private void InsertElement(RenderBatch batch, ContainerNode parent, int childIndex, ArraySegment<RenderTreeFrame> frames, RenderTreeFrame frame, int frameIndex)
    {
        // Note: we don't handle SVG here
        var newElement = new ElementNode(frame.ElementName);

        var inserted = false;

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
                parent.InsertLogicalChild(newElement, childIndex);
                inserted = true;

                // As soon as we see a non-attribute child, all the subsequent child frames are
                // not attributes, so bail out and insert the remnants recursively
                InsertFrameRange(batch, newElement, 0, frames, i, frameIndex + frame.ElementSubtreeLength);
                break;
            }
        }

        // this element did not have any children, so it's not inserted yet.
        if (!inserted)
        {
            parent.InsertLogicalChild(newElement, childIndex);
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

    public string GetHtml()
    {
        var builder = new StringBuilder();
        foreach (var root in _componentsById.Values.OfType<RootComponentNode>())
        {
            Render(root, builder);
        }

        return builder.ToString();
    }

    private void Render(TestNode node, StringBuilder builder)
    {
        if (node is TextNode t)
        {
            builder.Append(HtmlEncoder.Default.Encode(t.Text));
        }
        else if (node is MarkupNode m)
        {
            builder.Append(m.Content);
        }
        else if (node is ElementNode e)
        {
            builder.Append("<");
            builder.Append(e.TagName);
            foreach (var (name, value) in e.Attributes)
            {
                builder.Append(" ");
                builder.Append(name);
                builder.Append("=");
                builder.Append(HtmlEncoder.Default.Encode(value.ToString()));
            }
            builder.Append(">");
            RenderDescendants(e, builder);
            if (e.Children.Count > 0)
            {
                builder.Append("</");
                builder.Append(e.TagName);
                builder.Append(">");
            }
        }
        else if (node is ContainerNode c)
        {
            RenderDescendants(c, builder);
        }

        void RenderDescendants(ContainerNode e, StringBuilder builder)
        {
            foreach (var child in e.Children)
            {
                Render(child, builder);
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
