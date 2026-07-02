// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components;

// Wrapper for one RenderFragment instance. Captures the frames produced by the original
// fragment, and tracks any nested RenderFragment parameters discovered inside those frames
// as child captures keyed by their attribute frame index inside the snapshot.
internal sealed class RenderFragmentCapture
{
    private readonly RenderFragment _original;
    private readonly Dictionary<int, RenderFragmentCapture> _childCaptures = new();
    private RenderTreeFrame[]? _capturedFrames;

    public RenderFragmentCapture(RenderFragment original)
    {
        _original = original;
    }

    public IReadOnlyDictionary<int, RenderFragmentCapture> ChildCaptures => _childCaptures;

    public void Invoke(RenderTreeBuilder builder)
    {
        var start = builder.GetFrames().Count;
        _original(builder);
        var end = builder.GetFrames().Count;

        // Walk the produced frames and wrap any nested RenderFragment component parameters.
        // Child captures are keyed by index relative to the snapshot start, so the keys
        // line up with the positions seen by the serializer when it walks _capturedFrames.
        WrapNestedFragments(builder, start, end);

        // Re-fetch frames after WrapNestedFragments may have mutated attribute values.
        var frames = builder.GetFrames();
        var count = end - start;
        _capturedFrames = new RenderTreeFrame[count];
        Array.Copy(frames.Array, start, _capturedFrames, 0, count);
    }

    public RenderTreeFrame[] GetCapturedFrames()
    {
        if (_capturedFrames is null)
        {
            throw new InvalidOperationException("Cannot retrieve captured frames because the RenderFragment was not invoked during rendering.");
        }
        return _capturedFrames;
    }

    private void WrapNestedFragments(RenderTreeBuilder builder, int start, int end)
    {
        var frames = builder.GetFrames();
        for (var i = start; i < end; i++)
        {
            ref readonly var frame = ref frames.Array[i];
            if (frame.FrameType is not RenderTreeFrameType.Component)
            {
                continue;
            }

            var componentSubtreeEnd = i + frame.ComponentSubtreeLength;
            // A component's attribute frames always sit right after the Component
            // frame in the render tree, with no other frame types mixed in. So we can stop as
            // soon as we hit the first non-Attribute frame.
            for (var j = i + 1; j < componentSubtreeEnd && frames.Array[j].FrameType is RenderTreeFrameType.Attribute; j++)
            {
                ref var attrFrame = ref frames.Array[j];
                if (attrFrame.AttributeValue is RenderFragment innerRf)
                {
                    var innerCapture = new RenderFragmentCapture(innerRf);
                    // Replace the original delegate in the live render buffer with the wrapper.
                    // This is required so that when the nested component later invokes its
                    // RenderFragment parameter, control flows through innerCapture.Invoke and
                    // populates innerCapture._capturedFrames. Looking up the capture by frame
                    // index in _childCaptures would otherwise find an entry whose frames were
                    // never recorded.
                    //
                    // RenderTreeFrame.AttributeValueField is internal to the core Components
                    // assembly and this shared source compiles into other assemblies, so we
                    // write to it directly through an UnsafeAccessor ref rather than widening
                    // the public API or granting InternalsVisibleTo. frames.Array is the live
                    // builder buffer, so this mutation is observed by the renderer.
                    GetAttributeValueField(ref attrFrame) = (RenderFragment)innerCapture.Invoke;
                    _childCaptures[j - start] = innerCapture;
                }
            }
        }
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "AttributeValueField")]
    private static extern ref object GetAttributeValueField(ref RenderTreeFrame frame);
}
