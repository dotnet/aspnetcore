// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.HtmlRendering.Infrastructure;

namespace Microsoft.AspNetCore.Components.Web.HtmlRendering;

/// <summary>
/// Represents the output of rendering a root component as HTML. The content can change if the component instance re-renders.
/// </summary>
public readonly struct HtmlRootComponent
{
    private readonly StaticHtmlRenderer? _renderer;
    private readonly int _componentId;

    internal HtmlRootComponent(StaticHtmlRenderer renderer, int componentId, Task quiescenceTask)
    {
        _renderer = renderer;
        _componentId = componentId;
        QuiescenceTask = quiescenceTask;
    }

    /// <summary>
    /// Gets a <see cref="Task"/> that completes when the component hierarchy has completed asynchronous tasks such as loading.
    /// </summary>
    public Task QuiescenceTask { get; }

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
        => _renderer?.WriteComponentHtml(_componentId, output);
}
