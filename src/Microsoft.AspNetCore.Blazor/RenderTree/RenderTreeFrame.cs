// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Components;
using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Blazor.RenderTree
{
    /// <summary>
    /// Represents an entry in a tree of user interface (UI) items.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct RenderTreeFrame
    {
        // Note that the struct layout has to be valid in both 32-bit and 64-bit runtime platforms,
        // which means that all reference-type fields need to take up 8 bytes (except for the last
        // one, which will be sized as either 4 or 8 bytes depending on the runtime platform).
        // This is not optimal for the Mono-WebAssembly case because that's always 32-bit so the
        // reference-type fields could be reduced to 4 bytes each. We could use ifdefs to have
        // different fields offsets for the 32 and 64 bit compile targets, but then we'd have the
        // complexity of needing different binaries when loaded into Mono-WASM vs desktop.
        // Eventually we might stop using this shared memory interop altogether (and would have to
        // if running as a web worker) so for now to keep things simple, treat reference types as
        // 8 bytes here.

        // Common
        [FieldOffset(0)] int _sequence;
        [FieldOffset(4)] RenderTreeFrameType _frameType;

        // RenderTreeFrameType.Element
        [FieldOffset(8)] private int _elementDescendantsEndIndex;
        [FieldOffset(16)] string _elementName;

        // RenderTreeFrameType.Text
        [FieldOffset(16)] private string _textContent;

        // RenderTreeFrameType.Attribute
        [FieldOffset(16)] private string _attributeName;
        [FieldOffset(24)] private object _attributeValue;

        // RenderTreeFrameType.Component
        [FieldOffset(8)] private int _componentDescendantsEndIndex;
        [FieldOffset(12)] private int _componentId;
        [FieldOffset(16)] private Type _componentType;
        [FieldOffset(24)] private IComponent _component;

        /// <summary>
        /// Gets the sequence number of the frame. Sequence numbers indicate the relative source
        /// positions of the instructions that inserted the frames. Sequence numbers are only
        /// comparable within the same sequence (typically, the same source method).
        /// </summary>
        public int Sequence => _sequence;

        /// <summary>
        /// Describes the type of this frame.
        /// </summary>
        public RenderTreeFrameType FrameType => _frameType;

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Element"/>,
        /// gets a name representing the type of the element. Otherwise, the value is <see langword="null"/>.
        /// </summary>
        public string ElementName => _elementName;

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Element"/>,
        /// gets the index of the final descendant frame in the tree. The value is
        /// zero if the frame is of a different type, or if it has not yet been closed.
        /// </summary>
        public int ElementDescendantsEndIndex => _elementDescendantsEndIndex;

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Text"/>,
        /// gets the content of the text frame. Otherwise, the value is <see langword="null"/>.
        /// </summary>
        public string TextContent => _textContent;

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Attribute"/>,
        /// gets the attribute name. Otherwise, the value is <see langword="null"/>.
        /// </summary>
        public string AttributeName => _attributeName;

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Attribute"/>,
        /// gets the attribute value. Otherwise, the value is <see langword="null"/>.
        /// </summary>
        public object AttributeValue => _attributeValue;

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Component"/>,
        /// gets the type of the child component.
        /// </summary>
        public Type ComponentType => _componentType;

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Component"/>,
        /// gets the child component instance identifier.
        /// </summary>
        public int ComponentId => _componentId;

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Component"/>,
        /// gets the child component instance. Otherwise, the value is <see langword="null"/>.
        /// </summary>
        public IComponent Component => _component;

        internal static RenderTreeFrame Element(int sequence, string elementName) => new RenderTreeFrame
        {
            _sequence = sequence,
            _frameType = RenderTreeFrameType.Element,
            _elementName = elementName,
        };

        internal static RenderTreeFrame Text(int sequence, string textContent) => new RenderTreeFrame
        {
            _sequence = sequence,
            _frameType = RenderTreeFrameType.Text,
            _textContent = textContent ?? string.Empty,
        };

        internal static RenderTreeFrame Attribute(int sequence, string name, string value) => new RenderTreeFrame
        {
            _sequence = sequence,
            _frameType = RenderTreeFrameType.Attribute,
            _attributeName = name,
            _attributeValue = value
        };

        internal static RenderTreeFrame Attribute(int sequence, string name, UIEventHandler value) => new RenderTreeFrame
        {
            _sequence = sequence,
            _frameType = RenderTreeFrameType.Attribute,
            _attributeName = name,
            _attributeValue = value
        };

        internal static RenderTreeFrame Attribute(int sequence, string name, object value) => new RenderTreeFrame
        {
            _sequence = sequence,
            _frameType = RenderTreeFrameType.Attribute,
            _attributeName = name,
            _attributeValue = value
        };

        internal static RenderTreeFrame ChildComponent<T>(int sequence) where T : IComponent => new RenderTreeFrame
        {
            _sequence = sequence,
            _frameType = RenderTreeFrameType.Component,
            _componentType = typeof(T)
        };

        internal void CloseElement(int descendantsEndIndex)
        {
            _elementDescendantsEndIndex = descendantsEndIndex;
        }

        internal void SetChildComponentInstance(int componentId, IComponent component)
        {
            _componentId = componentId;
            _component = component;
        }

        internal void SetSequence(int sequence)
        {
            // This is only used when appending attribute frames, because helpers such as @onclick
            // need to construct the attribute frame in a context where they don't know the sequence
            // number, so we assign it later
            _sequence = sequence;
        }
    }
}
