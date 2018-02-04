// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Components;
using System;

namespace Microsoft.AspNetCore.Blazor.RenderTree
{
    // TODO: Consider coalescing properties of compatible types that don't need to be
    // used simultaneously. For example, 'ElementName' and 'AttributeName' could be replaced
    // by a single 'Name' property.

    /// <summary>
    /// Represents an entry in a tree of user interface (UI) items.
    /// </summary>
    public struct RenderTreeFrame
    {
        /// <summary>
        /// Gets the sequence number of the frame. Sequence numbers indicate the relative source
        /// positions of the instructions that inserted the frames. Sequence numbers are only
        /// comparable within the same sequence (typically, the same source method).
        /// </summary>
        public int Sequence { get; private set; }

        /// <summary>
        /// Describes the type of this frame.
        /// </summary>
        public RenderTreeFrameType FrameType { get; private set; }

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Element"/>,
        /// gets a name representing the type of the element. Otherwise, the value is <see langword="null"/>.
        /// </summary>
        public string ElementName { get; private set; }

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Element"/>,
        /// gets the index of the final descendant frame in the tree. The value is
        /// zero if the frame is of a different type, or if it has not yet been closed.
        /// </summary>
        public int ElementDescendantsEndIndex { get; private set; }

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Text"/>,
        /// gets the content of the text frame. Otherwise, the value is <see langword="null"/>.
        /// </summary>
        public string TextContent { get; private set; }

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Attribute"/>,
        /// gets the attribute name. Otherwise, the value is <see langword="null"/>.
        /// </summary>
        public string AttributeName { get; private set; }

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Attribute"/>,
        /// gets the attribute value. Otherwise, the value is <see langword="null"/>.
        /// </summary>
        public object AttributeValue { get; private set; }

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Component"/>,
        /// gets the type of the child component.
        /// </summary>
        public Type ComponentType { get; private set; }

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Component"/>,
        /// gets the child component instance identifier.
        /// </summary>
        public int ComponentId { get; private set; }

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Component"/>,
        /// gets the child component instance. Otherwise, the value is <see langword="null"/>.
        /// </summary>
        public IComponent Component { get; private set; }

        internal static RenderTreeFrame Element(int sequence, string elementName) => new RenderTreeFrame
        {
            Sequence = sequence,
            FrameType = RenderTreeFrameType.Element,
            ElementName = elementName,
        };

        internal static RenderTreeFrame Text(int sequence, string textContent) => new RenderTreeFrame
        {
            Sequence = sequence,
            FrameType = RenderTreeFrameType.Text,
            TextContent = textContent ?? string.Empty,
        };

        internal static RenderTreeFrame Attribute(int sequence, string name, string value) => new RenderTreeFrame
        {
            Sequence = sequence,
            FrameType = RenderTreeFrameType.Attribute,
            AttributeName = name,
            AttributeValue = value
        };

        internal static RenderTreeFrame Attribute(int sequence, string name, UIEventHandler value) => new RenderTreeFrame
        {
            Sequence = sequence,
            FrameType = RenderTreeFrameType.Attribute,
            AttributeName = name,
            AttributeValue = value
        };

        internal static RenderTreeFrame Attribute(int sequence, string name, object value) => new RenderTreeFrame
        {
            Sequence = sequence,
            FrameType = RenderTreeFrameType.Attribute,
            AttributeName = name,
            AttributeValue = value
        };

        internal static RenderTreeFrame ChildComponent<T>(int sequence) where T: IComponent => new RenderTreeFrame
        {
            Sequence = sequence,
            FrameType = RenderTreeFrameType.Component,
            ComponentType = typeof(T)
        };

        internal void CloseElement(int descendantsEndIndex)
        {
            ElementDescendantsEndIndex = descendantsEndIndex;
        }

        internal void SetChildComponentInstance(int componentId, IComponent component)
        {
            ComponentId = componentId;
            Component = component;
        }

        internal void SetSequence(int sequence)
        {
            // This is only used when appending attribute frames, because helpers such as @onclick
            // need to construct the attribute frame in a context where they don't know the sequence
            // number, so we assign it later
            Sequence = sequence;
        }
    }
}
