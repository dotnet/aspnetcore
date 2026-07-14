// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Validation;

internal readonly struct ValidateContextMutableState
{
    public required int Depth { get; init; }
    public required string Path { get; init; }
    public required string DisplayName { get; init; }
    public required string? MemberName { get; init; }
}
