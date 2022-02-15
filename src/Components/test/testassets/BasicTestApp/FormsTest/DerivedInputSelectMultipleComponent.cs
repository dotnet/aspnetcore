// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace BasicTestApp.FormsTest;

public class DerivedInputSelectMultipleComponent : InputSelect<DayOfWeek[]>
{
    public static readonly DayOfWeek[] FixedValue = new DayOfWeek[] { DayOfWeek.Monday, DayOfWeek.Tuesday };

    // Supports InputsTwoWayBindingComponent test
    // Repro for https://github.com/dotnet/aspnetcore/issues/40097

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        CurrentValue = FixedValue;

        base.BuildRenderTree(builder);
    }
}
