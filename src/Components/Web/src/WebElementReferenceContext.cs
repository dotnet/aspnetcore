// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// A <see cref="ElementReferenceContext"/> for a web element.
/// </summary>
public class WebElementReferenceContext : ElementReferenceContext
{
    internal IJSRuntime JSRuntime { get; }

    /// <summary>
    /// Initialize a new instance of <see cref="WebElementReferenceContext"/>.
    /// </summary>
    /// <param name="jsRuntime">The <see cref="IJSRuntime"/>.</param>
    public WebElementReferenceContext(IJSRuntime jsRuntime)
    {
        JSRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    }
}
