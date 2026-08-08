// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Components.Endpoints.Generators.Models;

internal sealed record class MetadataContextModel(
    string? Namespace,
    ImmutableArray<ContainingTypeModel> ContainingTypes,
    string TypeName,
    string TypeKeyword,
    bool DeclaresJsonTypeInfoResolver,
    ImmutableArray<string> BuiltInJSInvokableDescriptorAssemblies,
    ImmutableArray<JSInvokableMethodModel> JSInvokableMethods)
{
    public bool Equals(MetadataContextModel? other)
        => other is not null &&
           string.Equals(Namespace, other.Namespace, StringComparison.Ordinal) &&
           string.Equals(TypeName, other.TypeName, StringComparison.Ordinal) &&
           string.Equals(TypeKeyword, other.TypeKeyword, StringComparison.Ordinal) &&
           DeclaresJsonTypeInfoResolver == other.DeclaresJsonTypeInfoResolver &&
           ModelComparer.SequenceEqual(
               BuiltInJSInvokableDescriptorAssemblies,
               other.BuiltInJSInvokableDescriptorAssemblies) &&
           ModelComparer.SequenceEqual(ContainingTypes, other.ContainingTypes) &&
           ModelComparer.SequenceEqual(JSInvokableMethods, other.JSInvokableMethods);

    public override int GetHashCode()
    {
        var hash = ModelComparer.Combine(0, Namespace);
        hash = ModelComparer.AddRange(hash, ContainingTypes);
        hash = ModelComparer.Combine(hash, TypeName);
        hash = ModelComparer.Combine(hash, TypeKeyword);
        hash = ModelComparer.AddRange(hash, BuiltInJSInvokableDescriptorAssemblies);
        return ModelComparer.AddRange(hash, JSInvokableMethods);
    }
}

internal sealed record class ContainingTypeModel(
    string Name,
    string TypeKeyword,
    ImmutableArray<string> TypeParameters,
    ImmutableArray<string> ConstraintClauses)
{
    public bool Equals(ContainingTypeModel? other)
        => other is not null &&
           string.Equals(Name, other.Name, StringComparison.Ordinal) &&
           string.Equals(TypeKeyword, other.TypeKeyword, StringComparison.Ordinal) &&
           ModelComparer.SequenceEqual(TypeParameters, other.TypeParameters) &&
           ModelComparer.SequenceEqual(ConstraintClauses, other.ConstraintClauses);

    public override int GetHashCode()
    {
        var hash = ModelComparer.Combine(0, Name);
        hash = ModelComparer.Combine(hash, TypeKeyword);
        hash = ModelComparer.AddRange(hash, TypeParameters);
        return ModelComparer.AddRange(hash, ConstraintClauses);
    }
}

internal sealed record class JSInvokableMethodModel(
    string AssemblyName,
    string TypeFullyQualifiedName,
    string Identifier,
    string MethodName,
    bool IsStatic,
    string MethodKey,
    JSInvokableMethodKind Kind,
    ImmutableArray<string> ParameterTypeFullyQualifiedNames,
    string? ReturnTypeFullyQualifiedName,
    JSInvokableReturnKind ReturnKind)
{
    public bool Equals(JSInvokableMethodModel? other)
        => other is not null &&
           string.Equals(AssemblyName, other.AssemblyName, StringComparison.Ordinal) &&
           string.Equals(TypeFullyQualifiedName, other.TypeFullyQualifiedName, StringComparison.Ordinal) &&
           string.Equals(Identifier, other.Identifier, StringComparison.Ordinal) &&
           string.Equals(MethodName, other.MethodName, StringComparison.Ordinal) &&
           IsStatic == other.IsStatic &&
           string.Equals(MethodKey, other.MethodKey, StringComparison.Ordinal) &&
           Kind == other.Kind &&
           ReturnKind == other.ReturnKind &&
           string.Equals(ReturnTypeFullyQualifiedName, other.ReturnTypeFullyQualifiedName, StringComparison.Ordinal) &&
           ModelComparer.SequenceEqual(ParameterTypeFullyQualifiedNames, other.ParameterTypeFullyQualifiedNames);

    public override int GetHashCode()
    {
        var hash = ModelComparer.Combine(0, TypeFullyQualifiedName);
        hash = ModelComparer.Combine(hash, Identifier);
        return ModelComparer.AddRange(hash, ParameterTypeFullyQualifiedNames);
    }
}

internal enum JSInvokableMethodKind
{
    Method,
    Override,
    OverrideBlocker,
    TypeCoverage,
}

internal enum JSInvokableReturnKind
{
    Void,
    Value,
    Task,
    TaskOfValue,
    ValueTask,
    ValueTaskOfValue,
}

internal static class ModelComparer
{
    public static bool SequenceEqual<T>(ImmutableArray<T> left, ImmutableArray<T> right)
    {
        var l = left.IsDefault ? ImmutableArray<T>.Empty : left;
        var r = right.IsDefault ? ImmutableArray<T>.Empty : right;
        if (l.Length != r.Length)
        {
            return false;
        }

        for (var i = 0; i < l.Length; i++)
        {
            if (!Equals(l[i], r[i]))
            {
                return false;
            }
        }

        return true;
    }

    public static int AddRange<T>(int hash, ImmutableArray<T> items)
    {
        if (items.IsDefaultOrEmpty)
        {
            return hash;
        }

        foreach (var item in items)
        {
            hash = (hash * 397) ^ (item?.GetHashCode() ?? 0);
        }

        return hash;
    }

    public static int Combine(int hash, string? value)
        => (hash * 397) ^ (value is null ? 0 : StringComparer.Ordinal.GetHashCode(value));
}
