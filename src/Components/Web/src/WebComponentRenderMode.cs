// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Indicates how a component should be rendered.
/// </summary>
public sealed class WebComponentRenderMode : ComponentRenderMode
{
    // Make sure that any newly-added modes have numeric values that don't clash with others
    // in the inheritance hierarchy of this type.

    // TODO: Add ServerPrerendered, WebAssemblyPrerendered

    /// <summary>
    /// Indicates that the component should run interactively on the server.
    /// </summary>
    public static readonly WebComponentRenderMode Server = new WebComponentRenderMode(32);

    /// <summary>
    /// Indicates that the component should run interactively using WebAssembly.
    /// </summary>
    public static readonly WebComponentRenderMode WebAssembly = new WebComponentRenderMode(33);

    private WebComponentRenderMode(byte numericValue) : base(numericValue)
    {
    }
}
