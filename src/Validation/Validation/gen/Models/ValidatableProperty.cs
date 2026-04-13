// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Extensions.Validation;
#pragma warning restore IDE0130 // Namespace does not match folder structure

internal sealed record class ValidatableProperty(
    ITypeSymbol ContainingType,
    ITypeSymbol Type,
    string Name,
    string? DisplayName,
    INamedTypeSymbol? DisplayResourceType,
    ImmutableArray<ValidationAttribute> Attributes
);
