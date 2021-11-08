// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics;
using Microsoft.AspNetCore.Routing.Template;

namespace Microsoft.AspNetCore.Routing.Tree;

/// <summary>
/// A candidate route to match incoming URLs in a <see cref="TreeRouter"/>.
/// </summary>
[DebuggerDisplay("{DebuggerToString(),nq}")]
public class InboundMatch
{
    /// <summary>
    /// Gets or sets the <see cref="InboundRouteEntry"/>.
    /// </summary>
    public InboundRouteEntry Entry { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="TemplateMatcher"/>.
    /// </summary>
    public TemplateMatcher TemplateMatcher { get; set; }

    private string DebuggerToString()
    {
        return TemplateMatcher?.Template?.TemplateText;
    }
}
