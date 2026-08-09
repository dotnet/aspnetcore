// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorServerAotSample.Pages;

/// <summary>
/// Declares interactive server rendering in attribute form, applied with <c>@attribute</c>.
/// </summary>
/// <remarks>
/// The <c>@rendermode</c> directive compiles to a <em>private nested</em> attribute type, which the
/// metadata generator running in the host cannot name and therefore cannot describe. Written as an
/// ordinary public attribute, the same fact reaches the descriptor. The generator reports
/// <c>BLAZORAOT004</c> on any component that uses the directive form.
/// </remarks>
public sealed class InteractiveServerAttribute : RenderModeAttribute
{
    /// <inheritdoc />
    public override IComponentRenderMode Mode => RenderMode.InteractiveServer;
}
