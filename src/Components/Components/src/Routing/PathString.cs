// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Routing;

internal readonly struct PathString(string? value)
{
    public string? Value { get; } = value;

}
