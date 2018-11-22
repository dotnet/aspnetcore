// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.RenderTree
{
    /// <summary>
    /// Represents an entry in a tree of user interface (UI) items.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct RenderTreeFrame
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

        // --------------------------------------------------------------------------------
        // Common
        // --------------------------------------------------------------------------------

        /// <summary>
        /// Gets the sequence number of the frame. Sequence numbers indicate the relative source
        /// positions of the instructions that inserted the frames. Sequence numbers are only
        /// comparable within the same sequence (typically, the same source method).
        /// </summary>
        [FieldOffset(0)] public readonly int Sequence;

        /// <summary>
        /// Describes the type of this frame.
        /// </summary>
        [FieldOffset(4)] public readonly RenderTreeFrameType FrameType;

        // --------------------------------------------------------------------------------
        // RenderTreeFrameType.Element
        // --------------------------------------------------------------------------------

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Element"/>
        /// gets the number of frames in the subtree for which this frame is the root.
        /// The value is zero if the frame has not yet been closed.
        /// </summary>
        [FieldOffset(8)] public readonly int ElementSubtreeLength;

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Element"/>,
        /// gets a name representing the type of the element. Otherwise, the value is undefined.
        /// </summary>
        [FieldOffset(16)] public readonly string ElementName;

        // --------------------------------------------------------------------------------
        // RenderTreeFrameType.Text
        // --------------------------------------------------------------------------------

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Text"/>,
        /// gets the content of the text frame. Otherwise, the value is undefined.
        /// </summary>
        [FieldOffset(16)] public readonly string TextContent;

        // --------------------------------------------------------------------------------
        // RenderTreeFrameType.Attribute
        // --------------------------------------------------------------------------------

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Attribute"/>
        /// gets the ID of the corresponding event handler, if any.
        /// </summary>
        [FieldOffset(8)] public readonly int AttributeEventHandlerId;

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Attribute"/>,
        /// gets the attribute name. Otherwise, the value is undefined.
        /// </summary>
        [FieldOffset(16)] public readonly string AttributeName;

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Attribute"/>,
        /// gets the attribute value. Otherwise, the value is undefined.
        /// </summary>
        [FieldOffset(24)] public readonly object AttributeValue;

        // --------------------------------------------------------------------------------
        // RenderTreeFrameType.Component
        // --------------------------------------------------------------------------------

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Component"/>
        /// gets the number of frames in the subtree for which this frame is the root.
        /// The value is zero if the frame has not yet been closed.
        /// </summary>
        [FieldOffset(8)] public readonly int ComponentSubtreeLength;

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Component"/>,
        /// gets the child component instance identifier.
        /// </summary>
        [FieldOffset(12)] public readonly int ComponentId;

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Component"/>,
        /// gets the type of the child component.
        /// </summary>
        [FieldOffset(16)] public readonly Type ComponentType;

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Component"/>,
        /// gets the child component state object. Otherwise, the value is undefined.
        /// </summary>
        [FieldOffset(24)] internal readonly ComponentState ComponentState;

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Component"/>,
        /// gets the child component instance. Otherwise, the value is undefined.
        /// </summary>
        public IComponent Component => ComponentState?.Component;

        // --------------------------------------------------------------------------------
        // RenderTreeFrameType.Region
        // --------------------------------------------------------------------------------

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Region"/>
        /// gets the number of frames in the subtree for which this frame is the root.
        /// The value is zero if the frame has not yet been closed.
        /// </summary>
        [FieldOffset(8)] public readonly int RegionSubtreeLength;

        // --------------------------------------------------------------------------------
        // RenderTreeFrameType.ElementReferenceCapture
        // --------------------------------------------------------------------------------

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.ElementReferenceCapture"/>,
        /// gets the ID of the reference capture. Otherwise, the value is undefined.
        /// </summary>
        [FieldOffset(16)] public readonly string ElementReferenceCaptureId;

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.ElementReferenceCapture"/>,
        /// gets the action that writes the reference to its target. Otherwise, the value is undefined.
        /// </summary>
        [FieldOffset(24)] public readonly Action<ElementRef> ElementReferenceCaptureAction;

        // --------------------------------------------------------------------------------
        // RenderTreeFrameType.ComponentReferenceCapture
        // --------------------------------------------------------------------------------

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.ComponentReferenceCapture"/>,
        /// gets the index of the parent frame representing the component being captured. Otherwise, the value is undefined.
        /// WARNING: This index can only be used in the context of the frame's original render tree. If the frame is
        ///          copied elsewhere, such as to the ReferenceFrames buffer of a RenderTreeDiff, then the index will
        ///          not relate to entries in that other buffer.
        ///          Currently there's no scenario where this matters, but if there was, we could change all of the subtree
        ///          initialization logic in RenderTreeDiffBuilder to walk the frames hierarchically, then it would know
        ///          the parent index at the point where it wants to initialize the ComponentReferenceCapture frame.
        /// </summary>
        [FieldOffset(8)] public readonly int ComponentReferenceCaptureParentFrameIndex;

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.ComponentReferenceCapture"/>,
        /// gets the action that writes the reference to its target. Otherwise, the value is undefined.
        /// </summary>
        [FieldOffset(16)] public readonly Action<object> ComponentReferenceCaptureAction;

        // --------------------------------------------------------------------------------
        // RenderTreeFrameType.Markup
        // --------------------------------------------------------------------------------

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Markup"/>,
        /// gets the content of the markup frame. Otherwise, the value is undefined.
        /// </summary>
        [FieldOffset(16)] public readonly string MarkupContent;

        private RenderTreeFrame(int sequence, string elementName, int elementSubtreeLength)
            : this()
        {
            FrameType = RenderTreeFrameType.Element;
            Sequence = sequence;
            ElementName = elementName;
            ElementSubtreeLength = elementSubtreeLength;
        }

        private RenderTreeFrame(int sequence, Type componentType, int componentSubtreeLength)
            : this()
        {
            FrameType = RenderTreeFrameType.Component;
            Sequence = sequence;
            ComponentType = componentType;
            ComponentSubtreeLength = componentSubtreeLength;
        }

        private RenderTreeFrame(int sequence, Type componentType, int subtreeLength, ComponentState componentState)
            : this(sequence, componentType, subtreeLength)
        {
            ComponentId = componentState.ComponentId;
            ComponentState = componentState;
        }

        private RenderTreeFrame(int sequence, string textContent)
            : this()
        {
            FrameType = RenderTreeFrameType.Text;
            Sequence = sequence;
            TextContent = textContent;
        }

        private RenderTreeFrame(int sequence, string attributeName, object attributeValue)
            : this()
        {
            FrameType = RenderTreeFrameType.Attribute;
            Sequence = sequence;
            AttributeName = attributeName;
            AttributeValue = attributeValue;
        }

        private RenderTreeFrame(int sequence, string attributeName, object attributeValue, int eventHandlerId)
            : this()
        {
            FrameType = RenderTreeFrameType.Attribute;
            Sequence = sequence;
            AttributeName = attributeName;
            AttributeValue = attributeValue;
            AttributeEventHandlerId = eventHandlerId;
        }

        private RenderTreeFrame(int sequence, int regionSubtreeLength)
            : this()
        {
            FrameType = RenderTreeFrameType.Region;
            Sequence = sequence;
            RegionSubtreeLength = regionSubtreeLength;
        }

        private RenderTreeFrame(int sequence, Action<ElementRef> elementReferenceCaptureAction, string elementReferenceCaptureId)
            : this()
        {
            FrameType = RenderTreeFrameType.ElementReferenceCapture;
            Sequence = sequence;
            ElementReferenceCaptureAction = elementReferenceCaptureAction;
            ElementReferenceCaptureId = elementReferenceCaptureId;
        }

        private RenderTreeFrame(int sequence, Action<object> componentReferenceCaptureAction, int parentFrameIndex)
            : this()
        {
            FrameType = RenderTreeFrameType.ComponentReferenceCapture;
            Sequence = sequence;
            ComponentReferenceCaptureAction = componentReferenceCaptureAction;
            ComponentReferenceCaptureParentFrameIndex = parentFrameIndex;
        }

        // If we need further constructors whose signatures clash with the patterns above,
        // we can add extra args to this general-purpose one.
        private RenderTreeFrame(int sequence, RenderTreeFrameType frameType, string markupContent)
            : this()
        {
            FrameType = frameType;
            Sequence = sequence;
            MarkupContent = markupContent;
        }

        internal static RenderTreeFrame Element(int sequence, string elementName)
            => new RenderTreeFrame(sequence, elementName: elementName, elementSubtreeLength: 0);

        internal static RenderTreeFrame Text(int sequence, string textContent)
            => new RenderTreeFrame(sequence, textContent: textContent);

        internal static RenderTreeFrame Markup(int sequence, string markupContent)
            => new RenderTreeFrame(sequence, RenderTreeFrameType.Markup, markupContent);

        internal static RenderTreeFrame Attribute(int sequence, string name, MulticastDelegate value)
             => new RenderTreeFrame(sequence, attributeName: name, attributeValue: value);

        internal static RenderTreeFrame Attribute(int sequence, string name, object value)
            => new RenderTreeFrame(sequence, attributeName: name, attributeValue: value);

        internal static RenderTreeFrame ChildComponent(int sequence, Type componentType)
            => new RenderTreeFrame(sequence, componentType, 0);

        internal static RenderTreeFrame PlaceholderChildComponentWithSubtreeLength(int subtreeLength)
            => new RenderTreeFrame(0, typeof(IComponent), subtreeLength);

        internal static RenderTreeFrame Region(int sequence)
            => new RenderTreeFrame(sequence, regionSubtreeLength: 0);

        internal static RenderTreeFrame ElementReferenceCapture(int sequence, Action<ElementRef> elementReferenceCaptureAction)
            => new RenderTreeFrame(sequence, elementReferenceCaptureAction: elementReferenceCaptureAction, elementReferenceCaptureId: null);

        internal static RenderTreeFrame ComponentReferenceCapture(int sequence, Action<object> componentReferenceCaptureAction, int parentFrameIndex)
            => new RenderTreeFrame(sequence, componentReferenceCaptureAction: componentReferenceCaptureAction, parentFrameIndex: parentFrameIndex);

        internal RenderTreeFrame WithElementSubtreeLength(int elementSubtreeLength)
            => new RenderTreeFrame(Sequence, elementName: ElementName, elementSubtreeLength: elementSubtreeLength);

        internal RenderTreeFrame WithComponentSubtreeLength(int componentSubtreeLength)
            => new RenderTreeFrame(Sequence, componentType: ComponentType, componentSubtreeLength: componentSubtreeLength);

        internal RenderTreeFrame WithAttributeSequence(int sequence)
            => new RenderTreeFrame(sequence, attributeName: AttributeName, attributeValue: AttributeValue);

        internal RenderTreeFrame WithComponent(ComponentState componentState)
            => new RenderTreeFrame(Sequence, ComponentType, ComponentSubtreeLength, componentState);

        internal RenderTreeFrame WithAttributeEventHandlerId(int eventHandlerId)
            => new RenderTreeFrame(Sequence, AttributeName, AttributeValue, eventHandlerId);

        internal RenderTreeFrame WithRegionSubtreeLength(int regionSubtreeLength)
            => new RenderTreeFrame(Sequence, regionSubtreeLength: regionSubtreeLength);

        internal RenderTreeFrame WithElementReferenceCaptureId(string elementReferenceCaptureId)
            => new RenderTreeFrame(Sequence, ElementReferenceCaptureAction, elementReferenceCaptureId);

        /// <inheritdoc />
        // Just to be nice for debugging and unit tests.
        public override string ToString()
        {
            switch (FrameType)
            {
                case RenderTreeFrameType.Attribute:
                    return $"Attribute: (seq={Sequence}, id={AttributeEventHandlerId}) '{AttributeName}'='{AttributeValue}'";

                case RenderTreeFrameType.Component:
                    return $"Component: (seq={Sequence}, len={ComponentSubtreeLength}) {ComponentType}";

                case RenderTreeFrameType.Element:
                    return $"Element: (seq={Sequence}, len={ElementSubtreeLength}) {ElementName}";

                case RenderTreeFrameType.Region:
                    return $"Region: (seq={Sequence}, len={RegionSubtreeLength})";

                case RenderTreeFrameType.Text:
                    return $"Text: (seq={Sequence}, len=n/a) {EscapeNewlines(TextContent)}";

                case RenderTreeFrameType.Markup:
                    return $"Markup: (seq={Sequence}, len=n/a) {EscapeNewlines(TextContent)}";

                case RenderTreeFrameType.ElementReferenceCapture:
                    return $"ElementReferenceCapture: (seq={Sequence}, len=n/a) {ElementReferenceCaptureAction}";
            }

            return base.ToString();
        }

        private static string EscapeNewlines(string text)
        {
            return text.Replace("\n", "\\n").Replace("\r\n", "\\r\\n");
        }
    }
}
