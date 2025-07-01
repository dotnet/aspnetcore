// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Indicates that state should be restored during enhanced navigation.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class UpdateStateOnEnhancedNavigationAttribute : Attribute, IPersistentStateFilter
{
    /// <inheritdoc />
    public bool ShouldRestore(IPersistentComponentStateScenario scenario)
    {
        if (scenario is not WebPersistenceContext { Reason: WebPersistenceReason.EnhancedNavigation } context)
        {
            return false;
        }

        // Enhanced navigation only applies to interactive modes (Server or WebAssembly)
        return context.RenderMode is InteractiveServerRenderMode or InteractiveWebAssemblyRenderMode;
    }
}