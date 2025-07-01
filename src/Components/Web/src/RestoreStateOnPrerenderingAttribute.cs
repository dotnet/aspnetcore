// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Indicates that state should be restored during prerendering.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class RestoreStateOnPrerenderingAttribute : Attribute, IPersistentStateFilter
{
    /// <inheritdoc />
    public bool ShouldRestore(IPersistentComponentStateScenario scenario)
    {
        if (scenario is not WebPersistenceContext { Reason: WebPersistenceReason.Prerendering } context)
        {
            return false;
        }

        // Prerendering state restoration only applies to interactive modes
        return context.RenderMode is InteractiveServerRenderMode or InteractiveWebAssemblyRenderMode;
    }
}