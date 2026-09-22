// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Text;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.AI.Tests.TestFramework;

internal sealed class HtmlContentWriter
{
    private static readonly HashSet<string> VoidElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "area", "base", "br", "col", "embed", "hr", "img", "input",
        "link", "meta", "param", "source", "track", "wbr"
    };

    private readonly TestRenderer _renderer;
    private readonly StringBuilder _sb = new();

    internal HtmlContentWriter(TestRenderer renderer)
    {
        _renderer = renderer;
    }

    internal void WriteFrames(ArrayRange<RenderTreeFrame> frames, int start, int end)
    {
        var i = start;
        while (i < end)
        {
            ref var frame = ref frames.Array[i];
            switch (frame.FrameType)
            {
                case RenderTreeFrameType.Element:
                    WriteElement(frames, ref i);
                    break;
                case RenderTreeFrameType.Text:
                    _sb.Append(WebUtility.HtmlEncode(frame.TextContent));
                    i++;
                    break;
                case RenderTreeFrameType.Markup:
                    _sb.Append(frame.MarkupContent);
                    i++;
                    break;
                case RenderTreeFrameType.Component:
                    WriteComponentContent(frame.ComponentId);
                    i += frame.ComponentSubtreeLength;
                    break;
                case RenderTreeFrameType.Region:
                    WriteFrames(frames, i + 1, i + frame.RegionSubtreeLength);
                    i += frame.RegionSubtreeLength;
                    break;
                default:
                    i++;
                    break;
            }
        }
    }

    internal string GetResult() => _sb.ToString();

    private void WriteElement(ArrayRange<RenderTreeFrame> frames, ref int i)
    {
        ref var elementFrame = ref frames.Array[i];
        var elementName = elementFrame.ElementName;
        var subtreeEnd = i + elementFrame.ElementSubtreeLength;

        _sb.Append('<');
        _sb.Append(elementName);

        // Write attributes (immediately follow the element frame)
        var childStart = i + 1;
        while (childStart < subtreeEnd
            && frames.Array[childStart].FrameType == RenderTreeFrameType.Attribute)
        {
            WriteAttribute(ref frames.Array[childStart]);
            childStart++;
        }

        if (VoidElements.Contains(elementName))
        {
            _sb.Append(" />");
        }
        else
        {
            _sb.Append('>');
            WriteFrames(frames, childStart, subtreeEnd);
            _sb.Append("</");
            _sb.Append(elementName);
            _sb.Append('>');
        }

        i = subtreeEnd;
    }

    private void WriteAttribute(ref RenderTreeFrame frame)
    {
        // Skip event handlers — they don't produce HTML attributes
        if (frame.AttributeEventHandlerId > 0)
        {
            return;
        }

        var name = frame.AttributeName;
        var value = frame.AttributeValue;

        if (value is bool boolValue)
        {
            if (boolValue)
            {
                _sb.Append(' ');
                _sb.Append(name);
            }
            // false → omit the attribute entirely
        }
        else if (value is string stringValue)
        {
            _sb.Append(' ');
            _sb.Append(name);
            _sb.Append("=\"");
            _sb.Append(WebUtility.HtmlEncode(stringValue));
            _sb.Append('"');
        }
        else if (value is null)
        {
            // Omit null attributes
        }
        else if (value is MulticastDelegate)
        {
            // Event handler delegate without an assigned handler ID — skip
        }
        else if (value is EventCallback)
        {
            // Unbound EventCallback — skip
        }
        else
        {
            _sb.Append(' ');
            _sb.Append(name);
            _sb.Append("=\"");
            _sb.Append(WebUtility.HtmlEncode(value.ToString() ?? ""));
            _sb.Append('"');
        }
    }

    private void WriteComponentContent(int componentId)
    {
        var frames = _renderer.GetCurrentRenderTreeFrames(componentId);
        WriteFrames(frames, 0, frames.Count);
    }
}
