// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

internal sealed class NullView : IView
{
    public static readonly NullView Instance = new NullView();

    public string Path => string.Empty;

    public Task RenderAsync(ViewContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Task.CompletedTask;
    }
}
