// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Temporary attribute for indicating that a component should render interactively on the server.
/// This will later be replaced by a @rendermode directive.
/// </summary>
public class RenderModeServerAttribute : RenderModeAttribute
{
    /// <summary>
    /// Constructs an instance of <see cref="RenderModeServerAttribute"/>.
    /// </summary>
    public RenderModeServerAttribute() : this(true)
    {
    }

    /// <summary>
    /// Constructs an instance of <see cref="RenderModeServerAttribute"/>.
    /// </summary>
    /// <param name="prerender">A flag indicating whether to prerender the component on the server. The default value is true.</param>
    public RenderModeServerAttribute(bool prerender)
    {
        Mode = new ServerRenderMode(prerender);
    }

    /// <inheritdoc />
    public override IComponentRenderMode Mode { get; }
}

/// <summary>
/// Temporary attribute for indicating that a component should render interactively using WebAssembly.
/// This will later be replaced by a @rendermode directive.
/// </summary>
public class RenderModeWebAssemblyAttribute : RenderModeAttribute
{
    /// <summary>
    /// Constructs an instance of <see cref="RenderModeWebAssemblyAttribute"/>.
    /// </summary>
    public RenderModeWebAssemblyAttribute() : this(true)
    {
    }

    /// <summary>
    /// Constructs an instance of <see cref="RenderModeWebAssemblyAttribute"/>.
    /// </summary>
    /// <param name="prerender">A flag indicating whether to prerender the component on the server. The default value is true.</param>
    public RenderModeWebAssemblyAttribute(bool prerender)
    {
        Mode = new WebAssemblyRenderMode(prerender);
    }

    /// <inheritdoc />
    public override IComponentRenderMode Mode { get; }
}

/// <summary>
/// Temporary attribute for indicating that a component should render interactively, with
/// a mode automatically determined.
/// This will later be replaced by a @rendermode directive.
/// </summary>
public class RenderModeAutoAttribute : RenderModeAttribute
{
    /// <summary>
    /// Constructs an instance of <see cref="RenderModeAutoAttribute"/>.
    /// </summary>
    public RenderModeAutoAttribute() : this(true)
    {
    }

    /// <summary>
    /// Constructs an instance of <see cref="RenderModeAutoAttribute"/>.
    /// </summary>
    /// <param name="prerender">A flag indicating whether to prerender the component on the server. The default value is true.</param>
    public RenderModeAutoAttribute(bool prerender)
    {
        Mode = new AutoRenderMode(prerender);
    }

    /// <inheritdoc />
    public override IComponentRenderMode Mode { get; }
}
