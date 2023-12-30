// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.RenderTree;

/// <summary>
/// A special subclass of <see cref="ArrayBuilder{T}"/> that contains methods optimized for appending <see cref="RenderTreeFrame"/> entries.
/// </summary>
internal sealed class RenderTreeFrameArrayBuilder : ArrayBuilder<RenderTreeFrame>
{
    // You may notice a repeated block at the top of each of these methods. This is intentionally inlined into each
    // method because doing so improves intensive rendering scenarios by around 1% (based on the FastGrid benchmark).

    public void AppendElement(int sequence, string elementName)
    {
        if (_itemsInUse == _items.Length)
        {
            GrowBuffer(_items.Length * 2);
        }

        _items[_itemsInUse++] = new RenderTreeFrame
        {
            SequenceField = sequence,
            FrameTypeField = RenderTreeFrameType.Element,
            ElementNameField = elementName,
        };
    }

    public void AppendText(int sequence, string textContent)
    {
        if (_itemsInUse == _items.Length)
        {
            GrowBuffer(_items.Length * 2);
        }

        _items[_itemsInUse++] = new RenderTreeFrame
        {
            SequenceField = sequence,
            FrameTypeField = RenderTreeFrameType.Text,
            TextContentField = textContent,
        };
    }

    public void AppendMarkup(int sequence, string markupContent)
    {
        if (_itemsInUse == _items.Length)
        {
            GrowBuffer(_items.Length * 2);
        }

        _items[_itemsInUse++] = new RenderTreeFrame
        {
            SequenceField = sequence,
            FrameTypeField = RenderTreeFrameType.Markup,
            MarkupContentField = markupContent,
        };
    }

    public void AppendAttribute(int sequence, string attributeName, object? attributeValue)
    {
        if (_itemsInUse == _items.Length)
        {
            GrowBuffer(_items.Length * 2);
        }

        _items[_itemsInUse++] = new RenderTreeFrame
        {
            SequenceField = sequence,
            FrameTypeField = RenderTreeFrameType.Attribute,
            AttributeNameField = attributeName,
            AttributeValueField = attributeValue,
        };
    }

    public void AppendComponent(int sequence, [DynamicallyAccessedMembers(Component)] Type componentType)
    {
        if (_itemsInUse == _items.Length)
        {
            GrowBuffer(_items.Length * 2);
        }

        _items[_itemsInUse++] = new RenderTreeFrame
        {
            SequenceField = sequence,
            FrameTypeField = RenderTreeFrameType.Component,
            ComponentTypeField = componentType,
        };
    }

    public void AppendElementReferenceCapture(int sequence, Action<ElementReference> elementReferenceCaptureAction)
    {
        if (_itemsInUse == _items.Length)
        {
            GrowBuffer(_items.Length * 2);
        }

        _items[_itemsInUse++] = new RenderTreeFrame
        {
            SequenceField = sequence,
            FrameTypeField = RenderTreeFrameType.ElementReferenceCapture,
            ElementReferenceCaptureActionField = elementReferenceCaptureAction,
        };
    }

    public void AppendComponentReferenceCapture(int sequence, Action<object> componentReferenceCaptureAction, int parentFrameIndexValue)
    {
        if (_itemsInUse == _items.Length)
        {
            GrowBuffer(_items.Length * 2);
        }

        _items[_itemsInUse++] = new RenderTreeFrame
        {
            SequenceField = sequence,
            FrameTypeField = RenderTreeFrameType.ComponentReferenceCapture,
            ComponentReferenceCaptureActionField = componentReferenceCaptureAction,
            ComponentReferenceCaptureParentFrameIndexField = parentFrameIndexValue,
        };
    }

    public void AppendRegion(int sequence)
    {
        if (_itemsInUse == _items.Length)
        {
            GrowBuffer(_items.Length * 2);
        }

        _items[_itemsInUse++] = new RenderTreeFrame
        {
            SequenceField = sequence,
            FrameTypeField = RenderTreeFrameType.Region,
        };
    }

    public void AppendComponentRenderMode(IComponentRenderMode renderMode)
    {
        if (_itemsInUse == _items.Length)
        {
            GrowBuffer(_items.Length * 2);
        }

        _items[_itemsInUse++] = new RenderTreeFrame
        {
            SequenceField = 0, // We're only interested in one of these, so it's not useful to optimize diffing over multiple
            FrameTypeField = RenderTreeFrameType.ComponentRenderMode,
            ComponentRenderModeField = renderMode,
        };
    }

    public void AppendNamedEvent(string eventType, string assignedName)
    {
        if (_itemsInUse == _items.Length)
        {
            GrowBuffer(_items.Length * 2);
        }

        _items[_itemsInUse++] = new RenderTreeFrame
        {
            SequenceField = 0, // We're only interested in one of these per eventType, so it's not useful to optimize diffing over multiple
            FrameTypeField = RenderTreeFrameType.NamedEvent,
            NamedEventTypeField = eventType,
            NamedEventAssignedNameField = assignedName,
        };
    }
}
