// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components.HtmlRendering;

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Represents the output of rendering a component as HTML. The content can change if the component instance re-renders.
/// </summary>
public sealed class HtmlContent
{
    private readonly PassiveHtmlRenderer _renderer;
    private readonly int _componentId;

    internal HtmlContent(PassiveHtmlRenderer renderer, int componentId)
    {
        _renderer = renderer;
        _componentId = componentId;
    }

    /// <summary>
    /// Returns an HTML string representation of the component's latest output.
    /// </summary>
    /// <returns>An HTML string.</returns>
    public string ToHtmlString()
    {
        using var writer = new StringWriter();
        new HtmlRenderingContext(_renderer, _componentId, writer, HtmlEncoder.Default).Render();
        return writer.ToString();
    }
}
