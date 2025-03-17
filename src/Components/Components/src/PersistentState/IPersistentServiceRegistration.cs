// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

// Represents a component that is registered for state persistence.
internal interface IPersistentServiceRegistration
{
    public string Assembly { get; }
    public string FullTypeName { get; }

    public IComponentRenderMode? GetRenderModeOrDefault();
}
