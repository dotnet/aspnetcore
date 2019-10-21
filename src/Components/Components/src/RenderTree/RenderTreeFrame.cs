// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
#if !IGNITOR
using Microsoft.AspNetCore.Components.Rendering;
#endif

#if IGNITOR
namespace Ignitor
#else
namespace Microsoft.AspNetCore.Components.RenderTree
#endif
{
    /// <summary>
    /// Types in the Microsoft.AspNetCore.Components.RenderTree are not recommended for use outside
    /// of the Blazor framework. These types will change in future release.
    /// </summary>
    //
    // Represents an entry in a tree of user interface (UI) items.
    [StructLayout(LayoutKind.Explicit, Pack = 4)]
    public readonly struct RenderTreeFrame
    {
        // Note that the struct layout has to be valid in both 32-bit and 64-bit runtime platforms,
        // which means that all reference-type fields need to take up 8 bytes (except for the last
        // one, which will be sized as either 4 or 8 bytes depending on the runtime platform).

        // Although each frame type uses the slots for different purposes, the runtime does not
        // allow reference type slots to overlap with each other or with value-type slots.
        // Here's the current layout:
        //
        // Offset   Type
        // ------   ----
        // 0-3      Int32 (sequence number)
        // 4-5      Int16 (frame type)
        // 6-15     Value types (usage varies by frame type)
        // 16-23    Reference type (usage varies by frame type)
        // 24-31    Reference type (usage varies by frame type)
        // 32-39    Reference type (usage varies by frame type)
        //
        // On Mono WebAssembly, because it's 32-bit, the final slot occupies bytes 32-35,
        // so the struct length is only 36.

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

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Element"/>,
        /// gets the element's diffing key, or null if none was specified.
        /// </summary>
        [FieldOffset(24)] public readonly object ElementKey;

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
        [FieldOffset(8)] public readonly ulong AttributeEventHandlerId;

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

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Attribute"/>,
        /// and the attribute represents an event handler, gets the name of another attribute whose value
        /// can be updated to represent the UI state prior to executing the event handler. This is
        /// primarily used in two-way bindings.
        /// </summary>
        [FieldOffset(32)] public readonly string AttributeEventUpdatesAttributeName;

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
        /// gets the component's diffing key, or null if none was specified.
        /// </summary>
        [FieldOffset(32)] public readonly object ComponentKey;

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
        [FieldOffset(24)] public readonly Action<ElementReference> ElementReferenceCaptureAction;

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

        // Element constructor
        private RenderTreeFrame(int sequence, int elementSubtreeLength, string elementName, object elementKey)
            : this()
        {
            Sequence = sequence;
            FrameType = RenderTreeFrameType.Element;
            ElementSubtreeLength = elementSubtreeLength;
            ElementName = elementName;
            ElementKey = elementKey;
        }

        // Component constructor
        private RenderTreeFrame(int sequence, int componentSubtreeLength, Type componentType, ComponentState componentState, object componentKey)
            : this()
        {
            Sequence = sequence;
            FrameType = RenderTreeFrameType.Component;
            ComponentSubtreeLength = componentSubtreeLength;
            ComponentType = componentType;
            ComponentKey = componentKey;

            if (componentState != null)
            {
                ComponentState = componentState;
                ComponentId = componentState.ComponentId;
            }
        }

        // Region constructor
        private RenderTreeFrame(int sequence, int regionSubtreeLength)
            : this()
        {
            Sequence = sequence;
            FrameType = RenderTreeFrameType.Region;
            RegionSubtreeLength = regionSubtreeLength;
        }

        // Text/markup constructor
        private RenderTreeFrame(int sequence, bool isMarkup, string textOrMarkup)
            : this()
        {
            Sequence = sequence;
            if (isMarkup)
            {
                FrameType = RenderTreeFrameType.Markup;
                MarkupContent = textOrMarkup;
            }
            else
            {
                FrameType = RenderTreeFrameType.Text;
                TextContent = textOrMarkup;
            }
        }

        // Attribute constructor
        private RenderTreeFrame(int sequence, string attributeName, object attributeValue, ulong attributeEventHandlerId, string attributeEventUpdatesAttributeName)
            : this()
        {
            FrameType = RenderTreeFrameType.Attribute;
            Sequence = sequence;
            AttributeName = attributeName;
            AttributeValue = attributeValue;
            AttributeEventHandlerId = attributeEventHandlerId;
            AttributeEventUpdatesAttributeName = attributeEventUpdatesAttributeName;
        }

        // Element reference capture constructor
        private RenderTreeFrame(int sequence, Action<ElementReference> elementReferenceCaptureAction, string elementReferenceCaptureId)
            : this()
        {
            FrameType = RenderTreeFrameType.ElementReferenceCapture;
            Sequence = sequence;
            ElementReferenceCaptureAction = elementReferenceCaptureAction;
            ElementReferenceCaptureId = elementReferenceCaptureId;
        }

        // Component reference capture constructor
        private RenderTreeFrame(int sequence, Action<object> componentReferenceCaptureAction, int parentFrameIndex)
            : this()
        {
            FrameType = RenderTreeFrameType.ComponentReferenceCapture;
            Sequence = sequence;
            ComponentReferenceCaptureAction = componentReferenceCaptureAction;
            ComponentReferenceCaptureParentFrameIndex = parentFrameIndex;
        }

        internal static RenderTreeFrame Element(int sequence, string elementName)
            => new RenderTreeFrame(sequence, elementSubtreeLength: 0, elementName, null);

        internal static RenderTreeFrame Text(int sequence, string textContent)
            => new RenderTreeFrame(sequence, isMarkup: false, textOrMarkup: textContent);

        internal static RenderTreeFrame Markup(int sequence, string markupContent)
            => new RenderTreeFrame(sequence, isMarkup: true, textOrMarkup: markupContent);

        internal static RenderTreeFrame Attribute(int sequence, string name, object value)
            => new RenderTreeFrame(sequence, attributeName: name, attributeValue: value, attributeEventHandlerId: 0, attributeEventUpdatesAttributeName: null);

        internal static RenderTreeFrame ChildComponent(int sequence, Type componentType)
            => new RenderTreeFrame(sequence, componentSubtreeLength: 0, componentType, null, null);

        internal static RenderTreeFrame PlaceholderChildComponentWithSubtreeLength(int subtreeLength)
            => new RenderTreeFrame(0, componentSubtreeLength: subtreeLength, typeof(IComponent), null, null);

        internal static RenderTreeFrame Region(int sequence)
            => new RenderTreeFrame(sequence, regionSubtreeLength: 0);

        internal static RenderTreeFrame ElementReferenceCapture(int sequence, Action<ElementReference> elementReferenceCaptureAction)
            => new RenderTreeFrame(sequence, elementReferenceCaptureAction: elementReferenceCaptureAction, elementReferenceCaptureId: null);

        internal static RenderTreeFrame ComponentReferenceCapture(int sequence, Action<object> componentReferenceCaptureAction, int parentFrameIndex)
            => new RenderTreeFrame(sequence, componentReferenceCaptureAction: componentReferenceCaptureAction, parentFrameIndex: parentFrameIndex);

        internal RenderTreeFrame WithElementSubtreeLength(int elementSubtreeLength)
            => new RenderTreeFrame(Sequence, elementSubtreeLength: elementSubtreeLength, ElementName, ElementKey);

        internal RenderTreeFrame WithComponentSubtreeLength(int componentSubtreeLength)
            => new RenderTreeFrame(Sequence, componentSubtreeLength: componentSubtreeLength, ComponentType, ComponentState, ComponentKey);

        internal RenderTreeFrame WithAttributeSequence(int sequence)
            => new RenderTreeFrame(sequence, attributeName: AttributeName, AttributeValue, AttributeEventHandlerId, AttributeEventUpdatesAttributeName);

        internal RenderTreeFrame WithComponent(ComponentState componentState)
            => new RenderTreeFrame(Sequence, componentSubtreeLength: ComponentSubtreeLength, ComponentType, componentState, ComponentKey);

        internal RenderTreeFrame WithAttributeEventHandlerId(ulong eventHandlerId)
            => new RenderTreeFrame(Sequence, attributeName: AttributeName, AttributeValue, eventHandlerId, AttributeEventUpdatesAttributeName);

        internal RenderTreeFrame WithAttributeValue(object attributeValue)
            => new RenderTreeFrame(Sequence, attributeName: AttributeName, attributeValue, AttributeEventHandlerId, AttributeEventUpdatesAttributeName);

        internal RenderTreeFrame WithAttributeEventUpdatesAttributeName(string attributeUpdatesAttributeName)
            => new RenderTreeFrame(Sequence, attributeName: AttributeName, AttributeValue, AttributeEventHandlerId, attributeUpdatesAttributeName);

        internal RenderTreeFrame WithRegionSubtreeLength(int regionSubtreeLength)
            => new RenderTreeFrame(Sequence, regionSubtreeLength: regionSubtreeLength);

        internal RenderTreeFrame WithElementReferenceCaptureId(string elementReferenceCaptureId)
            => new RenderTreeFrame(Sequence, elementReferenceCaptureAction: ElementReferenceCaptureAction, elementReferenceCaptureId);

        internal RenderTreeFrame WithElementKey(object elementKey)
            => new RenderTreeFrame(Sequence, elementSubtreeLength: ElementSubtreeLength, ElementName, elementKey);

        internal RenderTreeFrame WithComponentKey(object componentKey)
            => new RenderTreeFrame(Sequence, componentSubtreeLength: ComponentSubtreeLength, ComponentType, ComponentState, componentKey);

        /// <inheritdoc />
        // Just to be nice for debugging and unit tests.
        public override string ToString()
        {
            switch (FrameType)
            {
                case RenderTreeFrameType.Attribute:
                    return $"Attribute: (seq={Sequence}, id={AttributeEventHandlerId}) '{AttributeName}'='{AttributeValue}'";

                case RenderTreeFrameType.Component:
                    return $"Component: (seq={Sequence}, key={ComponentKey ?? "(none)"}, len={ComponentSubtreeLength}) {ComponentType}";

                case RenderTreeFrameType.Element:
                    return $"Element: (seq={Sequence}, key={ElementKey ?? "(none)"}, len={ElementSubtreeLength}) {ElementName}";

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
