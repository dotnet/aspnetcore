// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components.HtmlRendering;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Web;

internal class HtmlRenderingContext
{
    private static readonly HashSet<string> SelfClosingElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr"
    };

    private readonly PassiveHtmlRenderer _renderer;
    private readonly int _rootComponentId;
    private readonly TextWriter _output;
    private readonly HtmlEncoder _htmlEncoder;
    private string? _closestSelectValueAsString;

    public HtmlRenderingContext(PassiveHtmlRenderer renderer, int componentId, TextWriter output, HtmlEncoder htmlEncoder)
    {
        _renderer = renderer;
        _rootComponentId = componentId;
        _output = output;
        _htmlEncoder = htmlEncoder;
    }

    public void Render()
    {
        var frames = _renderer.GetCurrentRenderTreeFrames(_rootComponentId);
        RenderFrames(frames, 0, frames.Count);
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

    private int RenderElement(
        ArrayRange<RenderTreeFrame> frames,
        int position)
    {
        ref var frame = ref frames.Array[position];
        _output.Write('<');
        _output.Write(frame.ElementName);
        var afterAttributes = RenderAttributes(frames, position + 1, frame.ElementSubtreeLength - 1, out var capturedValueAttribute);

        // When we see an <option> as a descendant of a <select>, and the option's "value" attribute matches the
        // "value" attribute on the <select>, then we auto-add the "selected" attribute to that option. This is
        // a way of converting Blazor's select binding feature to regular static HTML.
        if (_closestSelectValueAsString is not null
            && string.Equals(frame.ElementName, "option", StringComparison.OrdinalIgnoreCase)
            && string.Equals(capturedValueAttribute, _closestSelectValueAsString, StringComparison.Ordinal))
        {
            _output.Write(" selected");
        }

        var remainingElements = frame.ElementSubtreeLength + position - afterAttributes;
        if (remainingElements > 0)
        {
            _output.Write('>');

            var isSelect = string.Equals(frame.ElementName, "select", StringComparison.OrdinalIgnoreCase);
            if (isSelect)
            {
                _closestSelectValueAsString = capturedValueAttribute;
            }

            var isTextArea = string.Equals(frame.ElementName, "textarea", StringComparison.OrdinalIgnoreCase);
            int afterElement;
            if (isTextArea && !string.IsNullOrEmpty(capturedValueAttribute))
            {
                // Textarea is a special type of form field where the value is given as text content instead of a 'value' attribute
                // So, if we captured a value attribute, use that instead of any child content
                _htmlEncoder.Encode(_output, capturedValueAttribute);
                afterElement = position + frame.ElementSubtreeLength; // Skip descendants
            }
            else
            {
                // Render descendants
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
        ArrayRange<RenderTreeFrame> frames, int position, int maxElements, out string? capturedValueAttribute)
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
                return candidateIndex;
            }

            if (frame.AttributeName.Equals("value", StringComparison.OrdinalIgnoreCase))
            {
                capturedValueAttribute = frame.AttributeValue as string;
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
                    _output.Write("=");
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

    private int RenderChildComponent(ArrayRange<RenderTreeFrame> frames, int position)
    {
        ref var frame = ref frames.Array[position];

        var childFrames = _renderer.GetCurrentRenderTreeFrames(frame.ComponentId);
        RenderFrames(childFrames, 0, childFrames.Count);

        return position + frame.ComponentSubtreeLength;
    }
}
