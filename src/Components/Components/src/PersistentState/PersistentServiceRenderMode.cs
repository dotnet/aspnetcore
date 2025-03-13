// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

internal class PersistentServiceRenderMode(IComponentRenderMode componentRenderMode)
{
    public IComponentRenderMode ComponentRenderMode { get; } = componentRenderMode;
}
