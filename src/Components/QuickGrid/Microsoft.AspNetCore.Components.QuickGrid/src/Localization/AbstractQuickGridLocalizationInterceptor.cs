// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.QuickGrid;

/// <summary>
/// Abstract base for localization interceptors. Holds built-in resource access and optional public localizer.
/// </summary>
internal abstract class AbstractQuickGridLocalizationInterceptor : IQuickGridLocalizationInterceptor
{
    protected AbstractQuickGridLocalizationInterceptor()
    {
    }

    public abstract QuickGridLocalizedString Handle(string key, params object?[]? arguments);
}
