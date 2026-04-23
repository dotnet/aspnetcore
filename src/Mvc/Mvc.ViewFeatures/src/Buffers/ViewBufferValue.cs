// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;

/// <summary>
/// Encapsulates a string, <see cref="IHtmlContent"/>, or UTF-8 encoded value.
/// </summary>
[DebuggerDisplay("{DebuggerToString()}")]
public readonly struct ViewBufferValue
{
    private readonly object _value;
    private readonly ReadOnlyMemory<byte> _utf8Value;
    private readonly ViewBufferValueType _valueType;

    /// <summary>
    /// Initializes a new instance of <see cref="ViewBufferValue"/> with a <c>string</c> value.
    /// </summary>
    /// <param name="value">The value.</param>
    public ViewBufferValue(string value)
    {
        _value = value;
        _utf8Value = default;
        _valueType = ViewBufferValueType.String;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ViewBufferValue"/> with a <see cref="IHtmlContent"/> value.
    /// </summary>
    /// <param name="content">The <see cref="IHtmlContent"/>.</param>
    public ViewBufferValue(IHtmlContent content)
    {
        _value = content;
        _utf8Value = default;
        _valueType = ViewBufferValueType.HtmlContent;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ViewBufferValue"/> with a UTF-8 encoded value.
    /// </summary>
    /// <param name="utf8Value">The UTF-8 encoded value.</param>
    public ViewBufferValue(ReadOnlyMemory<byte> utf8Value)
    {
        _value = null;
        _utf8Value = utf8Value;
        _valueType = ViewBufferValueType.Utf8;
    }

    /// <summary>
    /// Gets a value that indicates whether this instance contains a UTF-8 encoded value.
    /// </summary>
    public bool IsUtf8Value => _valueType == ViewBufferValueType.Utf8;

    /// <summary>
    /// Gets the UTF-8 encoded value.
    /// </summary>
    public ReadOnlyMemory<byte> Utf8Value => _utf8Value;

    internal ViewBufferValueType ValueType => _valueType;

    internal string StringValue => (string)_value;

    internal IHtmlContent HtmlContentValue => (IHtmlContent)_value;

    internal enum ViewBufferValueType : byte
    {
        None,
        String,
        HtmlContent,
        Utf8,
    }

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <remarks>
    /// When <see cref="IsUtf8Value"/> is <see langword="true"/>, this converts the UTF-8 value to a string and returns it. Use <see cref="Utf8Value"/> to access the value directly without conversion.
    /// </remarks>
    public object Value => _valueType == ViewBufferValueType.Utf8 ? Encoding.UTF8.GetString(_utf8Value.Span) : _value;

    private string DebuggerToString()
    {
        using (var writer = new StringWriter())
        {
            if (_valueType == ViewBufferValueType.String)
            {
                writer.Write((string)_value);
                return writer.ToString();
            }

            if (_valueType == ViewBufferValueType.HtmlContent)
            {
                ((IHtmlContent)_value).WriteTo(writer, HtmlEncoder.Default);
                return writer.ToString();
            }

            if (_valueType == ViewBufferValueType.Utf8)
            {
                writer.Write(Encoding.UTF8.GetString(_utf8Value.Span));
                return writer.ToString();
            }

            return "(null)";
        }
    }
}
