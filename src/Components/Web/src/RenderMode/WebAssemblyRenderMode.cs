// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// A <see cref="IComponentRenderMode"/> indicating that the component should be rendered on the client using WebAssembly.
/// </summary>
public class WebAssemblyRenderMode : IComponentRenderMode
{
    /// <summary>
    /// Constructs an instance of <see cref="WebAssemblyRenderMode"/>.
    /// </summary>
    public WebAssemblyRenderMode() : this(true)
    {
    }

    /// <summary>
    /// Constructs an instance of <see cref="WebAssemblyRenderMode"/>
    /// </summary>
    /// <param name="prerender">A flag indicating whether the component should first prerender on the server. The default value is true.</param>
    public WebAssemblyRenderMode(bool prerender)
    {
        Prerender = prerender;
    }

    /// <summary>
    /// A flag indicating whether the component should first prerender on the server. The default value is true.
    /// </summary>
    public bool Prerender { get; }
}
