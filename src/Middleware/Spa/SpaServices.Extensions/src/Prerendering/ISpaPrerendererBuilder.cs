// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SpaServices.Prerendering;

/// <summary>
/// Represents the ability to build a Single Page Application (SPA) on demand
/// so that it can be prerendered. This is only intended to be used at development
/// time. In production, a SPA should already have been built during publishing.
/// </summary>
[Obsolete("Prerendering is no longer supported out of box")]
public interface ISpaPrerendererBuilder
{
    /// <summary>
    /// Builds the Single Page Application so that a JavaScript entrypoint file
    /// exists on disk. Prerendering middleware can then execute that file in
    /// a Node environment.
    /// </summary>
    /// <param name="spaBuilder">The <see cref="ISpaBuilder"/>.</param>
    /// <returns>A <see cref="Task"/> representing completion of the build process.</returns>
    Task Build(ISpaBuilder spaBuilder);
}
