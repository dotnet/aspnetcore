// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Http.ValidationsGenerator;

internal sealed record class ValidatableMember(
    string Name,
    string DisplayName,
    bool IsEnumerable,
    bool IsNullable,
    bool HasValidatableType,
    ImmutableArray<ValidationAttribute> Attributes
);
