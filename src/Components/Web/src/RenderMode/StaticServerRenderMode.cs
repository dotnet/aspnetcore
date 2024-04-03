// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// A <see cref="IComponentRenderMode"/> indicating that the component should be rendered to HTML on the server. In this render mode,
/// the component cannot process any client-side events.
/// </summary>
public class StaticServerRenderMode : IComponentRenderMode
{
}
