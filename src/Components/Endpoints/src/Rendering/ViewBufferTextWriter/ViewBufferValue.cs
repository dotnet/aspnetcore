// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
 * The implementation here matches the one present at https://github.com/dotnet/aspnetcore/blob/88180f6f487a1222b3af8c111aa6b5f8aa278633/src/Mvc/Mvc.ViewFeatures/src/Buffers/ViewBufferValue.cs
 * but implements a struct with internal accessibility to avoid introducing public API and circular dependencies
 * between Mvc.Razor and Components.Endpoints.
 */

using System.Diagnostics;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// Encapsulates a string or <see cref="IHtmlContent"/> value.
/// </summary>
[DebuggerDisplay("{DebuggerToString()}")]
internal readonly struct ViewBufferValue
{
    /// <summary>
    /// Initializes a new instance of <see cref="ViewBufferValue"/> with a <c>string</c> value.
    /// </summary>
    /// <param name="value">The value.</param>
    public ViewBufferValue(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ViewBufferValue"/> with a <see cref="IHtmlContent"/> value.
    /// </summary>
    /// <param name="content">The <see cref="IHtmlContent"/>.</param>
    public ViewBufferValue(IHtmlContent content)
    {
        Value = content;
    }

    /// <summary>
    /// Gets the value.
    /// </summary>
    public object Value { get; }

    private string DebuggerToString()
    {
        using (var writer = new StringWriter())
        {
            if (Value is string valueAsString)
            {
                writer.Write(valueAsString);
                return writer.ToString();
            }

            if (Value is IHtmlContent valueAsContent)
            {
                valueAsContent.WriteTo(writer, HtmlEncoder.Default);
                return writer.ToString();
            }

            return "(null)";
        }
    }
}
