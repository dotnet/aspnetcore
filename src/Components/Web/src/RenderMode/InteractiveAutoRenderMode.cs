// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// A <see cref="IComponentRenderMode"/> indicating that the component should be interactive, with its hosting platform determined automatically based on a policy.
/// </summary>
public class InteractiveAutoRenderMode : IComponentRenderMode
{
    /// <summary>
    /// Constructs an instance of <see cref="InteractiveAutoRenderMode"/>.
    /// </summary>
    public InteractiveAutoRenderMode() : this(true)
    {
    }

    /// <summary>
    /// Constructs an instance of <see cref="InteractiveAutoRenderMode"/>
    /// </summary>
    /// <param name="prerender">A flag indicating whether the component should first prerender on the server. The default value is true.</param>
    public InteractiveAutoRenderMode(bool prerender)
    {
        Prerender = prerender;
    }

    /// <summary>
    /// A flag indicating whether the component should first prerender on the server. The default value is true.
    /// </summary>
    public bool Prerender { get; }
}
