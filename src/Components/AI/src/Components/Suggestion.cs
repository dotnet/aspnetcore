// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

public sealed class Suggestion
{
    public required string Label { get; init; }

    public required string Prompt { get; init; }

    public string? Icon { get; init; }
}
