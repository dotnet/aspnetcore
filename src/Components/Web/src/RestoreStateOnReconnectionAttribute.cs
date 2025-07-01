// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Indicates that state should be restored after server reconnection.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class RestoreStateOnReconnectionAttribute : Attribute, IPersistentStateFilter
{
    /// <inheritdoc />
    public bool ShouldRestore(IPersistentComponentStateScenario scenario)
    {
        if (scenario is not WebPersistenceContext { Reason: WebPersistenceReason.Reconnection } context)
        {
            return false;
        }

        // Reconnection only applies to server-side interactive mode
        return context.RenderMode is InteractiveServerRenderMode;
    }
}