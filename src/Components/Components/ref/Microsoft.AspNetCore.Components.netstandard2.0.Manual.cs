// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.RenderTree
{
    // https://github.com/dotnet/arcade/pull/2033
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Explicit)]
    public readonly partial struct RenderTreeFrame
    {
        [System.Runtime.InteropServices.FieldOffsetAttribute(8)]
        public readonly int AttributeEventHandlerId;
        [System.Runtime.InteropServices.FieldOffsetAttribute(16)]
        public readonly string AttributeName;
        [System.Runtime.InteropServices.FieldOffsetAttribute(24)]
        public readonly object AttributeValue;
        [System.Runtime.InteropServices.FieldOffsetAttribute(12)]
        public readonly int ComponentId;
        [System.Runtime.InteropServices.FieldOffsetAttribute(16)]
        public readonly System.Action<object> ComponentReferenceCaptureAction;
        [System.Runtime.InteropServices.FieldOffsetAttribute(8)]
        public readonly int ComponentReferenceCaptureParentFrameIndex;
        [System.Runtime.InteropServices.FieldOffsetAttribute(8)]
        public readonly int ComponentSubtreeLength;
        [System.Runtime.InteropServices.FieldOffsetAttribute(16)]
        public readonly System.Type ComponentType;
        [System.Runtime.InteropServices.FieldOffsetAttribute(16)]
        public readonly string ElementName;
        [System.Runtime.InteropServices.FieldOffsetAttribute(24)]
        public readonly System.Action<Microsoft.AspNetCore.Components.ElementReference> ElementReferenceCaptureAction;
        [System.Runtime.InteropServices.FieldOffsetAttribute(16)]
        public readonly string ElementReferenceCaptureId;
        [System.Runtime.InteropServices.FieldOffsetAttribute(8)]
        public readonly int ElementSubtreeLength;
        [System.Runtime.InteropServices.FieldOffsetAttribute(4)]
        public readonly Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrameType FrameType;
        [System.Runtime.InteropServices.FieldOffsetAttribute(16)]
        public readonly string MarkupContent;
        [System.Runtime.InteropServices.FieldOffsetAttribute(8)]
        public readonly int RegionSubtreeLength;
        [System.Runtime.InteropServices.FieldOffsetAttribute(0)]
        public readonly int Sequence;
        [System.Runtime.InteropServices.FieldOffsetAttribute(16)]
        public readonly string TextContent;
        public Microsoft.AspNetCore.Components.IComponent Component { get { throw null; } }
        public override string ToString() { throw null; }
    }
}
