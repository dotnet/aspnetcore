// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// An <see cref="ActionResult"/> that renders a Razor Component.
/// </summary>
public class RazorComponentResult<TComponent> : RazorComponentResult where TComponent: IComponent
{
    /// <summary>
    /// Constructs an instance of <see cref="RazorComponentResult"/>.
    /// </summary>
    public RazorComponentResult() : base(typeof(TComponent))
    {
    }
}
