// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

internal class RootComponentUpdateSet
{
    public IEnumerable<RootComponentOperation>? Operations { get; set; }

    public string? CircuitComponentValidation { get; set; }
}
