// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Web;

// This is OK to be a struct because it never gets passed around anywhere. Other code can't even get an instance
// of it. It just keeps track of some contextual information during a single synchronous HTML output operation.
internal ref struct HtmlComponentWriter
{
    private static readonly HashSet<string> SelfClosingElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr"
    };

    private static readonly HtmlEncoder _htmlEncoder = HtmlEncoder.Default;
    private readonly IHtmlRendererContentProvider _renderer;
    private readonly TextWriter _output;
    private string? _closestSelectValueAsString;

    public static void Write(IHtmlRendererContentProvider renderer, int componentId, TextWriter output)
    {
        // We're about to walk over some buffers inside the renderer that can be mutated during rendering.
        // So, we require exclusive access to the renderer during this synchronous process.
        renderer.Dispatcher.AssertAccess();

        var context = new HtmlComponentWriter(renderer, output);
        context.RenderComponent(componentId);
    }

    private HtmlComponentWriter(IHtmlRendererContentProvider renderer, TextWriter output)
    {
        _renderer = renderer;
        _output = output;
    }

    private int RenderFrames(ArrayRange<RenderTreeFrame> frames, int position, int maxElements)
    {
        var nextPosition = position;
        var endPosition = position + maxElements;
        while (position < endPosition)
        {
            nextPosition = RenderCore(frames, position);
            if (position == nextPosition)
            {
                throw new InvalidOperationException("We didn't consume any input.");
            }
            position = nextPosition;
        }

        return nextPosition;
    }

    private int RenderCore(
        ArrayRange<RenderTreeFrame> frames,
        int position)
    {
        ref var frame = ref frames.Array[position];
        switch (frame.FrameType)
        {
            case RenderTreeFrameType.Element:
                return RenderElement(frames, position);
            case RenderTreeFrameType.Attribute:
                throw new InvalidOperationException($"Attributes should only be encountered within {nameof(RenderElement)}");
            case RenderTreeFrameType.Text:
                _htmlEncoder.Encode(_output, frame.TextContent);
                return ++position;
            case RenderTreeFrameType.Markup:
                _output.Write(frame.MarkupContent);
                return ++position;
            case RenderTreeFrameType.Component:
                return RenderChildComponent(frames, position);
            case RenderTreeFrameType.Region:
                return RenderFrames(frames, position + 1, frame.RegionSubtreeLength - 1);
            case RenderTreeFrameType.ElementReferenceCapture:
            case RenderTreeFrameType.ComponentReferenceCapture:
                return ++position;
            default:
                throw new InvalidOperationException($"Invalid element frame type '{frame.FrameType}'.");
        }
    }

    private int RenderElement(ArrayRange<RenderTreeFrame> frames, int position)
    {
        ref var frame = ref frames.Array[position];
        _output.Write('<');
        _output.Write(frame.ElementName);
        int afterElement;
        var isTextArea = string.Equals(frame.ElementName, "textarea", StringComparison.OrdinalIgnoreCase);
        // We don't want to include value attribute of textarea element.
        var afterAttributes = RenderAttributes(frames, position + 1, frame.ElementSubtreeLength - 1, !isTextArea, out var capturedValueAttribute);

        // When we see an <option> as a descendant of a <select>, and the option's "value" attribute matches the
        // "value" attribute on the <select>, then we auto-add the "selected" attribute to that option. This is
        // a way of converting Blazor's select binding feature to regular static HTML.
        if (_closestSelectValueAsString != null
            && string.Equals(frame.ElementName, "option", StringComparison.OrdinalIgnoreCase)
            && string.Equals(capturedValueAttribute, _closestSelectValueAsString, StringComparison.Ordinal))
        {
            _output.Write(" selected");
        }

        var remainingElements = frame.ElementSubtreeLength + position - afterAttributes;
        if (remainingElements > 0 || isTextArea)
        {
            _output.Write('>');

            var isSelect = string.Equals(frame.ElementName, "select", StringComparison.OrdinalIgnoreCase);
            if (isSelect)
            {
                _closestSelectValueAsString = capturedValueAttribute;
            }

            if (isTextArea && !string.IsNullOrEmpty(capturedValueAttribute))
            {
                // Textarea is a special type of form field where the value is given as text content instead of a 'value' attribute
                // So, if we captured a value attribute, use that instead of any child content
                _htmlEncoder.Encode(_output, capturedValueAttribute);
                afterElement = position + frame.ElementSubtreeLength; // Skip descendants
            }
            else
            {
                afterElement = RenderChildren(frames, afterAttributes, remainingElements);
            }

            if (isSelect)
            {
                // There's no concept of nested <select> elements, so as soon as we're exiting one of them,
                // we can safely say there is no longer any value for this
                _closestSelectValueAsString = null;
            }

            _output.Write("</");
            _output.Write(frame.ElementName);
            _output.Write('>');
            Debug.Assert(afterElement == position + frame.ElementSubtreeLength);
            return afterElement;
        }
        else
        {
            if (SelfClosingElements.Contains(frame.ElementName))
            {
                _output.Write(" />");
            }
            else
            {
                _output.Write("></");
                _output.Write(frame.ElementName);
                _output.Write('>');
            }
            Debug.Assert(afterAttributes == position + frame.ElementSubtreeLength);
            return afterAttributes;
        }
    }

    private int RenderAttributes(
        ArrayRange<RenderTreeFrame> frames, int position, int maxElements, bool includeValueAttribute, out string? capturedValueAttribute)
    {
        capturedValueAttribute = null;

        if (maxElements == 0)
        {
            return position;
        }

        for (var i = 0; i < maxElements; i++)
        {
            var candidateIndex = position + i;
            ref var frame = ref frames.Array[candidateIndex];

            if (frame.FrameType != RenderTreeFrameType.Attribute)
            {
                if (frame.FrameType == RenderTreeFrameType.ElementReferenceCapture)
                {
                    continue;
                }

                return candidateIndex;
            }

            if (frame.AttributeName.Equals("value", StringComparison.OrdinalIgnoreCase))
            {
                capturedValueAttribute = frame.AttributeValue as string;

                if (!includeValueAttribute)
                {
                    continue;
                }
            }

            switch (frame.AttributeValue)
            {
                case bool flag when flag:
                    _output.Write(' ');
                    _output.Write(frame.AttributeName);
                    break;
                case string value:
                    _output.Write(' ');
                    _output.Write(frame.AttributeName);
                    _output.Write('=');
                    _output.Write('\"');
                    _htmlEncoder.Encode(_output, value);
                    _output.Write('\"');
                    break;
                default:
                    break;
            }
        }

        return position + maxElements;
    }

    private int RenderChildren(ArrayRange<RenderTreeFrame> frames, int position, int maxElements)
    {
        if (maxElements == 0)
        {
            return position;
        }

        return RenderFrames(frames, position, maxElements);
    }

    private void RenderComponent(int componentId)
    {
        var frames = _renderer.GetCurrentRenderTreeFrames(componentId);
        RenderFrames(frames, 0, frames.Count);
    }

    private int RenderChildComponent(ArrayRange<RenderTreeFrame> frames, int position)
    {
        ref var frame = ref frames.Array[position];

        RenderComponent(frame.ComponentId);

        return position + frame.ComponentSubtreeLength;
    }
}
