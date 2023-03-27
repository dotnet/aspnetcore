// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.HtmlRendering.Infrastructure;

namespace Microsoft.AspNetCore.Components.Web.HtmlRendering;

/// <summary>
/// Represents the output of rendering a root component as HTML. The content can change if the component instance re-renders.
/// </summary>
public sealed class HtmlRootComponent : HtmlComponent
{
    /// <summary>
    /// Gets an instance of <see cref="HtmlRootComponent"/> that produces no content.
    /// </summary>
    public static HtmlRootComponent Empty { get; } = new HtmlRootComponent();

    internal HtmlRootComponent(StaticHtmlRenderer renderer, int componentId, Task quiescenceTask)
        : base(renderer, componentId)
    {
        QuiescenceTask = quiescenceTask;
    }

    internal HtmlRootComponent() : base()
    {
        QuiescenceTask = Task.CompletedTask;
    }

    /// <summary>
    /// Gets a <see cref="Task"/> that completes when the component hierarchy has completed asynchronous tasks such as loading.
    /// </summary>
    public Task QuiescenceTask { get; }
}
