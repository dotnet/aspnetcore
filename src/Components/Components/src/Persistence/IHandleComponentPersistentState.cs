// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// TODO: Docs
/// </summary>
public interface IHandleComponentPersistentState : IHandlePersistentState, IComponent
{
    /// <summary>
    /// TODO: Docs
    /// </summary>
    [CascadingParameter] public PersistentScope Scope { get; set; }

    /// <summary>
    /// TODO: Docs
    /// </summary>
    public void Register(PersistentScope scope)
    {
        scope.Register(this);
    }
}
