// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Represents the output of rendering a component as HTML. The content can change if the component instance re-renders.
/// </summary>
public sealed class HtmlComponent
{
    private readonly IHtmlRendererContentProvider? _renderer;
    private readonly int _componentId;
    private readonly Task _quiescenceTask;

    /// <summary>
    /// Constructs an instance of <see cref="HtmlComponent"/>.
    /// </summary>
    /// <param name="renderer">An <see cref="IHtmlRendererContentProvider"/> that can supply content representing the component.</param>
    /// <param name="componentId">The component ID.</param>
    /// <param name="quiescenceTask">A <see cref="Task"/> that completes when the component hierarchy has completed asynchronous tasks such as loading.</param>
    public HtmlComponent(IHtmlRendererContentProvider? renderer, int componentId, Task quiescenceTask)
    {
        _renderer = renderer;
        _componentId = componentId;
        _quiescenceTask = quiescenceTask;
    }

    /// <summary>
    /// Gets an instance of <see cref="HtmlComponent"/> that produces no content.
    /// </summary>
    public static HtmlComponent Empty { get; } = new HtmlComponent(null, 0, Task.CompletedTask);

    /// <summary>
    /// Obtains a <see cref="Task"/> that completes when the component hierarchy has completed asynchronous tasks such as loading.
    /// </summary>
    /// <returns>A <see cref="Task"/> that completes when the component hierarchy has completed asynchronous tasks such as loading.</returns>
    public Task WaitForQuiescenceAsync()
        => _quiescenceTask;

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
