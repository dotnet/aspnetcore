// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.QuickGrid;
public readonly struct SortedProperty
{
    public required string PropertyName { get; init; }
    public SortDirection Direction { get; init; }
}
