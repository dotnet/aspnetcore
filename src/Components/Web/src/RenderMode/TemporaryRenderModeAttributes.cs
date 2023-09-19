// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Temporary attribute for indicating that a component should render interactively on the server.
/// This will later be replaced by a @rendermode directive.
/// </summary>
public class RenderModeInteractiveServerAttribute : RenderModeAttribute
{
    /// <summary>
    /// Constructs an instance of <see cref="RenderModeInteractiveServerAttribute"/>.
    /// </summary>
    public RenderModeInteractiveServerAttribute() : this(true)
    {
    }

    /// <summary>
    /// Constructs an instance of <see cref="RenderModeInteractiveServerAttribute"/>.
    /// </summary>
    /// <param name="prerender">A flag indicating whether to prerender the component on the server. The default value is true.</param>
    public RenderModeInteractiveServerAttribute(bool prerender)
    {
        Mode = new InteractiveServerRenderMode(prerender);
    }

    /// <inheritdoc />
    public override IComponentRenderMode Mode { get; }
}

/// <summary>
/// Temporary attribute for indicating that a component should render interactively using WebAssembly.
/// This will later be replaced by a @rendermode directive.
/// </summary>
public class RenderModeInteractiveWebAssemblyAttribute : RenderModeAttribute
{
    /// <summary>
    /// Constructs an instance of <see cref="RenderModeInteractiveWebAssemblyAttribute"/>.
    /// </summary>
    public RenderModeInteractiveWebAssemblyAttribute() : this(true)
    {
    }

    /// <summary>
    /// Constructs an instance of <see cref="RenderModeInteractiveWebAssemblyAttribute"/>.
    /// </summary>
    /// <param name="prerender">A flag indicating whether to prerender the component on the server. The default value is true.</param>
    public RenderModeInteractiveWebAssemblyAttribute(bool prerender)
    {
        Mode = new InteractiveWebAssemblyRenderMode(prerender);
    }

    /// <inheritdoc />
    public override IComponentRenderMode Mode { get; }
}

/// <summary>
/// Temporary attribute for indicating that a component should render interactively, with
/// a mode automatically determined.
/// This will later be replaced by a @rendermode directive.
/// </summary>
public class RenderModeInteractiveAutoAttribute : RenderModeAttribute
{
    /// <summary>
    /// Constructs an instance of <see cref="RenderModeInteractiveAutoAttribute"/>.
    /// </summary>
    public RenderModeInteractiveAutoAttribute() : this(true)
    {
    }

    /// <summary>
    /// Constructs an instance of <see cref="RenderModeInteractiveAutoAttribute"/>.
    /// </summary>
    /// <param name="prerender">A flag indicating whether to prerender the component on the server. The default value is true.</param>
    public RenderModeInteractiveAutoAttribute(bool prerender)
    {
        Mode = new InteractiveAutoRenderMode(prerender);
    }

    /// <inheritdoc />
    public override IComponentRenderMode Mode { get; }
}
