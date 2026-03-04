// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Validation;

internal static class ISymbolExtensions
{
    public static bool IsEqualityContract(this IPropertySymbol prop, WellKnownTypes wellKnownTypes) =>
        prop.Name == "EqualityContract"
        && SymbolEqualityComparer.Default.Equals(prop.Type, wellKnownTypes.Get(WellKnownTypeData.WellKnownType.System_Type))
        && prop.DeclaredAccessibility == Accessibility.Protected;
}
