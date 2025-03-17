// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Components;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
internal sealed class PersistentServiceRegistration<TService>(IComponentRenderMode componentRenderMode) : IPersistentServiceRegistration
{
    public string Assembly => typeof(TService).Assembly.GetName().Name!;
    public string FullTypeName => typeof(TService).FullName!;

    public IComponentRenderMode? GetRenderModeOrDefault() => componentRenderMode;

    private string GetDebuggerDisplay() => $"{Assembly}::{FullTypeName}";
}
