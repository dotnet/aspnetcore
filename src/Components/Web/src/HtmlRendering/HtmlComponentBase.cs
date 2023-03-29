// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.HtmlRendering.Infrastructure;

namespace Microsoft.AspNetCore.Components.Web.HtmlRendering;

/// <summary>
/// Represents the output of rendering a component as HTML. The content can change if the component instance re-renders.
/// </summary>
public class HtmlComponentBase
{
    private readonly StaticHtmlRenderer? _renderer;
    private readonly int _componentId;

    /// <summary>
    /// Constructs an instance of <see cref="HtmlComponentBase"/>.
    /// </summary>
    /// <param name="renderer">The renderer of the component.</param>
    /// <param name="componentId">The ID of the component.</param>
    public HtmlComponentBase(StaticHtmlRenderer renderer, int componentId)
    {
        _renderer = renderer;
        _componentId = componentId;
    }

    internal HtmlComponentBase()
    {
    }

    /// <summary>
    /// Gets the component ID.
    /// </summary>
    public int ComponentId => _componentId;

    /// <summary>
    /// Returns an HTML string representation of the component's latest output.
    /// </summary>
    /// <returns>An HTML string representation of the component's latest output.</returns>
    public string ToHtmlString()
    {
        if (_renderer is null)
        {
            return string.Empty;
        }

        using var writer = new StringWriter();
        WriteHtmlTo(writer);
        return writer.ToString();
    }

    /// <summary>
    /// Writes the component's latest output as HTML to the specified writer.
    /// </summary>
    /// <param name="output">The output destination.</param>
    public void WriteHtmlTo(TextWriter output)
    {
        if (_renderer is not null)
        {
            HtmlComponentWriter.Write(_renderer, _componentId, output);
        }
    }
}
