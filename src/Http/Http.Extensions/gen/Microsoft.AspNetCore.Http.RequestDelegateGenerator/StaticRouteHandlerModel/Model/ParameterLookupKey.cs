// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;

internal sealed class ParameterLookupKey
{
    public ParameterLookupKey(string name, ITypeSymbol type)
    {
        Name = name;
        Type = type;
    }

    public string Name { get; }
    public ITypeSymbol Type { get; }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is ParameterLookupKey other)
        {
            return SymbolEqualityComparer.Default.Equals(Type, other.Type) &&
                string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }
}
