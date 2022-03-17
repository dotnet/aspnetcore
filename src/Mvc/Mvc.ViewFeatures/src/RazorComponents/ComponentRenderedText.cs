// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Components.Rendering;

internal readonly struct ComponentRenderedText
{
    public ComponentRenderedText(int componentId, IHtmlContent htmlContent)
    {
        ComponentId = componentId;
        HtmlContent = htmlContent;
    }

    public int ComponentId { get; }

    public IHtmlContent HtmlContent { get; }
}
