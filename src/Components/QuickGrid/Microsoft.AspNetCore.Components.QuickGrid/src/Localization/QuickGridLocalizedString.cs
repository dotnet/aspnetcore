// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.QuickGrid;

/// <summary>
/// Lightweight localized string used internally by QuickGrid localization pipeline.
/// </summary>
internal readonly struct QuickGridLocalizedString
{
    public string Name { get; }
    public string Value { get; }
    public bool ResourceNotFound { get; }

    public QuickGridLocalizedString(string name, string value, bool resourceNotFound)
    {
        Name = name;
        Value = value;
        ResourceNotFound = resourceNotFound;
    }
}
