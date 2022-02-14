// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace BasicTestApp.FormsTest;

public class DerivedInputCheckboxComponent : InputCheckbox
{
    // Repro for https://github.com/dotnet/aspnetcore/issues/40097

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        CurrentValue = false; // always unchecked

        base.BuildRenderTree(builder);
    }
}
