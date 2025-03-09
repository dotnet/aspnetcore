// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Http.ValidationsGenerator;

internal sealed record class ValidatableProperty(
    ITypeSymbol ContainingType,
    ITypeSymbol Type,
    string Name,
    string DisplayName,
    ImmutableArray<ValidationAttribute> Attributes
);
