// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Indicates how a component should be rendered.
/// </summary>
public static class WebComponentRenderMode
{
    // For layering reasons, we need WebComponentRenderMode to be distinct from ComponentRenderMode,
    // since M.A.Components itself doesn't know about concepts like "server" or "webassembly".
    //
    // For overload resolution reasons (i.e., making AddAttribute("rendermode", WebComponentRenderMode.Server) work),
    // we need WebComponentRenderMode values to be ComponentRenderMode values (e.g., of an inherited type). But
    // unfortunately C# doesn't do inheritance for enums, nor does it support implicit conversions between them.
    //
    // For attribute usage reasons (i.e., making [ComponentRenderMode(WebComponentRenderMode.Server)] work), we need
    // the values to be compile-time constants.
    //
    // All these requirements are difficult to reconcile, but one technique that does suffice is having ComponentRenderMode
    // be an enum, and having "derived types" actually just be consts that cast other numeric values to that enum type.
    // When consumed in application code, it looks equivalent to WebComponentRenderMode being a derived enum type.

    /// <summary>
    /// Indicates that the component should run interactively on the server.
    /// </summary>
    public const ComponentRenderMode Server = (ComponentRenderMode)32;

    /// <summary>
    /// Indicates that the component should run interactively using WebAssembly.
    /// </summary>
    public const ComponentRenderMode WebAssembly = (ComponentRenderMode)33;

    /// <summary>
    /// Indicates that the component should run interactively using WebAssembly if already loaded,
    /// otherwise on the server.
    /// </summary>
    public const ComponentRenderMode Auto = (ComponentRenderMode)34;
}
