// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Rendering;

/// <summary>
/// Describes the render mode of the component.
/// </summary>
/// <remarks>
/// The rendering mode determines how the component gets rendered on the page. It configures whether the component
/// is prerendered into the page or not and whether it simply renders static HTML on the page or if it includes the necessary
/// information to bootstrap a Blazor application from the user agent.
/// </remarks>
public enum RenderMode
{
    /// <summary>
    /// Renders the component into static HTML.
    /// </summary>
    Static = 1,

    /// <summary>
    /// Renders a marker for a Blazor server-side application. This doesn't include any output from the component.
    /// When the user-agent starts, it uses this marker to bootstrap a blazor application.
    /// </summary>
    Server = 2,

    /// <summary>
    /// Renders the component into static HTML and includes a marker for a Blazor server-side application.
    /// When the user-agent starts, it uses this marker to bootstrap a blazor application.
    /// </summary>
    ServerPrerendered = 3,

    /// <summary>
    /// Renders a marker for a Blazor webassembly application. This doesn't include any output from the component.
    /// When the user-agent starts, it uses this marker to bootstrap a blazor client-side application.
    /// </summary>
    WebAssembly = 4,

    /// <summary>
    /// Renders the component into static HTML and includes a marker for a Blazor webassembly application.
    /// When the user-agent starts, it uses this marker to bootstrap a blazor client-side application.
    /// </summary>
    WebAssemblyPrerendered = 5,
}
