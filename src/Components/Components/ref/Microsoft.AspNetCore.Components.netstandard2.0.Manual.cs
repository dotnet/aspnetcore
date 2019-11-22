// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.RenderTree
{
    // https://github.com/dotnet/arcade/pull/2033
    [StructLayout(LayoutKind.Explicit, Pack = 4)]
    public readonly partial struct RenderTreeFrame
    {
        [FieldOffset(0)] public readonly int Sequence;

        [FieldOffset(4)] public readonly RenderTreeFrameType FrameType;

        [FieldOffset(8)] public readonly int ElementSubtreeLength;

        [FieldOffset(16)] public readonly string ElementName;

        [FieldOffset(24)] public readonly object ElementKey;

        [FieldOffset(16)] public readonly string TextContent;

        [FieldOffset(8)] public readonly ulong AttributeEventHandlerId;

        [FieldOffset(16)] public readonly string AttributeName;

        [FieldOffset(24)] public readonly object AttributeValue;

        [FieldOffset(32)] public readonly string AttributeEventUpdatesAttributeName;

        [FieldOffset(8)] public readonly int ComponentSubtreeLength;

        [FieldOffset(12)] public readonly int ComponentId;

        [FieldOffset(16)] public readonly Type ComponentType;

        [FieldOffset(32)] public readonly object ComponentKey;

        public IComponent Component => null;

        [FieldOffset(8)] public readonly int RegionSubtreeLength;

        [FieldOffset(16)] public readonly string ElementReferenceCaptureId;

        [FieldOffset(24)] public readonly Action<ElementReference> ElementReferenceCaptureAction;

        [FieldOffset(8)] public readonly int ComponentReferenceCaptureParentFrameIndex;

        [FieldOffset(16)] public readonly Action<object> ComponentReferenceCaptureAction;

        [FieldOffset(16)] public readonly string MarkupContent;

        public override string ToString() => null;
    }
}
