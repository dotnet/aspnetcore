// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.Extensions.Validation;

internal sealed record class ValidatableProperty(
    string ContainingTypeFQN,
    string TypeFQN,
    string Name,
    string? DisplayName,
    bool HasResourceDisplayAttribute,
    bool IsUnionValue = false,
    ImmutableArray<string> SkippedUnionCaseTypes = default
);
