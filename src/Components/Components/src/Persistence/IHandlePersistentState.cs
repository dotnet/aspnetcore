// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Reflection;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// TODO: Docs
/// </summary>
public interface IHandlePersistentState
{
    /// <summary>
    /// TODO: Docs
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public void PersistState(IPersistentComponentState state)
    {
        PersistentComponentProperties.PersistProperties(state, this);
    }

    /// <summary>
    /// TODO: Docs
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public void RestoreState(IPersistentComponentState state)
    {
        PersistentComponentProperties.RestoreProperties(state, this);
    }
}
