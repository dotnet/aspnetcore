// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.AspNetCore.Components.Endpoints.Generators.Models;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Endpoints.Generators;

public sealed partial class RazorComponentsMetadataGenerator
{
    // Describes every [JSInvokable] method the application declares. The descriptor owns the whole
    // call, so the parameter and return types — exactly what the dispatcher cannot name without
    // reflection, and exactly what is known here — end up as concrete type arguments in the emitted
    // serializer calls.
    private static ImmutableArray<JSInvokableMethodModel> CollectJSInvokableMethods(
        Compilation compilation,
        WellKnownTypes types,
        CancellationToken cancellationToken)
    {
        if (types.JSInvokableAttribute is null)
        {
            return ImmutableArray<JSInvokableMethodModel>.Empty;
        }

        var generatedIn = compilation.Assembly;
        var builder = ImmutableArray.CreateBuilder<JSInvokableMethodModel>();

        foreach (var type in SymbolHelpers.EnumerateApplicationTypes(compilation, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!TryGetRuntimeTypeName(type, generatedIn, out var runtimeTypeName))
            {
                continue;
            }

            var requiresInheritedCoverage = RequiresInheritedCoverage(type, types.JSInvokableAttribute);
            var hasCompleteTypeCoverage = true;
            var canGenerateInvocations = !type.IsGenericType && TypeAccessibility.IsNameable(type, generatedIn);
            foreach (var member in type.GetMembers())
            {
                if (member is not IMethodSymbol method || method.IsGenericMethod ||
                    method.MethodKind is not (MethodKind.Ordinary or MethodKind.DeclareMethod))
                {
                    continue;
                }

                var attributes = method.GetAttributes();
                var invokableAttributes = ImmutableArray.CreateBuilder<AttributeData>();
                foreach (var attribute in attributes)
                {
                    if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, types.JSInvokableAttribute))
                    {
                        invokableAttributes.Add(attribute);
                    }
                }

                if (invokableAttributes.Count == 0 && method.OverriddenMethod is null)
                {
                    continue;
                }

                var canDescribeInvokable = canGenerateInvocations &&
                    method.DeclaredAccessibility is Accessibility.Public &&
                    (!method.IsStatic || SymbolHelpers.IsPubliclyAccessible(type));
                if (invokableAttributes.Count > 0 && !canDescribeInvokable)
                {
                    // A non-public [JSInvokable] would need an UnsafeAccessor per overload shape; the
                    // reflection dispatcher already handles it, so it is left undescribed. Override
                    // blockers are still emitted below when the receiver type itself is representable.
                }

                var declaredIdentifiers = new HashSet<string>(StringComparer.Ordinal);
                var describedAttributeCount = 0;
                if (canDescribeInvokable)
                {
                    for (var attributeIndex = 0; attributeIndex < invokableAttributes.Count; attributeIndex++)
                    {
                        var attribute = invokableAttributes[attributeIndex];
                        if (!TryDescribeJSInvokable(method, attribute, attributeIndex, types, generatedIn, out var model))
                        {
                            continue;
                        }

                        declaredIdentifiers.Add(model.Identifier);
                        builder.Add(model);
                        describedAttributeCount++;
                    }
                }

                if (!method.IsStatic &&
                    method.DeclaredAccessibility is Accessibility.Public &&
                    describedAttributeCount != invokableAttributes.Count)
                {
                    hasCompleteTypeCoverage = false;
                }

                if (method.OverriddenMethod is not null)
                {
                    AddOverrideBlockers(
                        method,
                        runtimeTypeName,
                        types.JSInvokableAttribute,
                        declaredIdentifiers,
                        builder);
                }
            }

            if (requiresInheritedCoverage && hasCompleteTypeCoverage)
            {
                builder.Add(new JSInvokableMethodModel(
                    type.ContainingAssembly.Name,
                    runtimeTypeName,
                    Identifier: string.Empty,
                    MethodName: string.Empty,
                    IsStatic: false,
                    $"{type.GetDocumentationCommentId() ?? type.ToDisplayString()}#coverage",
                    JSInvokableMethodKind.TypeCoverage,
                    ImmutableArray<string>.Empty,
                    ReturnTypeFullyQualifiedName: null,
                    JSInvokableReturnKind.Void));
            }
        }

        builder.Sort(static (left, right) =>
        {
            var byType = string.CompareOrdinal(left.TypeFullyQualifiedName, right.TypeFullyQualifiedName);
            if (byType != 0)
            {
                return byType;
            }

            var byIdentifier = string.CompareOrdinal(left.Identifier, right.Identifier);
            if (byIdentifier != 0)
            {
                return byIdentifier;
            }

            var byStatic = right.IsStatic.CompareTo(left.IsStatic);
            if (byStatic != 0)
            {
                return byStatic;
            }

            var byMethod = string.CompareOrdinal(left.MethodName, right.MethodName);
            if (byMethod != 0)
            {
                return byMethod;
            }

            for (var i = 0; i < left.ParameterTypeFullyQualifiedNames.Length; i++)
            {
                if (i >= right.ParameterTypeFullyQualifiedNames.Length)
                {
                    return 1;
                }

                var byParameter = string.CompareOrdinal(
                    left.ParameterTypeFullyQualifiedNames[i],
                    right.ParameterTypeFullyQualifiedNames[i]);
                if (byParameter != 0)
                {
                    return byParameter;
                }
            }

            return left.ParameterTypeFullyQualifiedNames.Length.CompareTo(
                right.ParameterTypeFullyQualifiedNames.Length);
        });

        return builder.ToImmutable();
    }

    private static bool TryDescribeJSInvokable(
        IMethodSymbol method,
        AttributeData attribute,
        int attributeIndex,
        WellKnownTypes types,
        IAssemblySymbol generatedIn,
        out JSInvokableMethodModel model)
    {
        model = null!;

        var parameterTypes = ImmutableArray.CreateBuilder<string>(method.Parameters.Length);
        foreach (var parameter in method.Parameters)
        {
            if (parameter.RefKind is not RefKind.None || !TypeAccessibility.IsNameable(parameter.Type, generatedIn))
            {
                return false;
            }

            parameterTypes.Add(parameter.Type.FullName());
        }

        if (!TryClassifyReturn(method.ReturnType, types, generatedIn, out var returnKind, out var returnType))
        {
            return false;
        }

        var identifier = GetIdentifier(method, attribute);

        model = new JSInvokableMethodModel(
            method.ContainingType.ContainingAssembly.Name,
            method.ContainingType.FullName(),
            identifier,
            method.Name,
            method.IsStatic,
            $"{method.GetDocumentationCommentId() ?? method.ToDisplayString()}#{attributeIndex}",
            method.OverriddenMethod is null
                ? method.IsVirtual
                    ? JSInvokableMethodKind.Override
                    : JSInvokableMethodKind.Method
                : JSInvokableMethodKind.Override,
            parameterTypes.ToImmutable(),
            returnType,
            returnKind);
        return true;
    }

    private static void AddOverrideBlockers(
        IMethodSymbol method,
        string runtimeTypeName,
        INamedTypeSymbol attributeType,
        HashSet<string> declaredIdentifiers,
        ImmutableArray<JSInvokableMethodModel>.Builder builder)
    {
        var blockedIdentifiers = new HashSet<string>(StringComparer.Ordinal);
        for (var overridden = method.OverriddenMethod; overridden is not null; overridden = overridden.OverriddenMethod)
        {
            foreach (var attribute in overridden.GetAttributes())
            {
                if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
                {
                    continue;
                }

                var identifier = GetIdentifier(overridden, attribute);
                if (declaredIdentifiers.Contains(identifier) || !blockedIdentifiers.Add(identifier))
                {
                    continue;
                }

                builder.Add(new JSInvokableMethodModel(
                    method.ContainingType.ContainingAssembly.Name,
                    runtimeTypeName,
                    identifier,
                    method.Name,
                    IsStatic: false,
                    $"{method.GetDocumentationCommentId() ?? method.ToDisplayString()}#block:{identifier}",
                    JSInvokableMethodKind.OverrideBlocker,
                    ImmutableArray<string>.Empty,
                    ReturnTypeFullyQualifiedName: null,
                    JSInvokableReturnKind.Void));
            }
        }
    }

    private static bool TryGetRuntimeTypeName(
        INamedTypeSymbol type,
        IAssemblySymbol generatedIn,
        out string runtimeTypeName)
    {
        var runtimeType = type.IsGenericType ? type.ConstructUnboundGenericType() : type;
        if (TypeAccessibility.IsNameable(runtimeType, generatedIn))
        {
            runtimeTypeName = runtimeType.FullName();
            return true;
        }

        runtimeTypeName = null!;
        return false;
    }

    private static bool RequiresInheritedCoverage(
        INamedTypeSymbol type,
        INamedTypeSymbol attributeType)
    {
        for (var baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            foreach (var member in baseType.GetMembers())
            {
                if (member is not IMethodSymbol { IsStatic: false, IsGenericMethod: false } method)
                {
                    continue;
                }

                foreach (var attribute in method.GetAttributes())
                {
                    if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static string GetIdentifier(IMethodSymbol method, AttributeData attribute)
        => attribute.ConstructorArguments.Length == 1 &&
            attribute.ConstructorArguments[0].Value is string explicitIdentifier &&
            !string.IsNullOrEmpty(explicitIdentifier)
                ? explicitIdentifier
                : method.Name;

    private static bool TryClassifyReturn(
        ITypeSymbol returnType,
        WellKnownTypes types,
        IAssemblySymbol generatedIn,
        out JSInvokableReturnKind kind,
        out string? valueTypeName)
    {
        kind = JSInvokableReturnKind.Void;
        valueTypeName = null;

        if (returnType.SpecialType is SpecialType.System_Void)
        {
            return true;
        }

        if (returnType is INamedTypeSymbol named)
        {
            if (SymbolEqualityComparer.Default.Equals(named, types.Task))
            {
                kind = JSInvokableReturnKind.Task;
                return true;
            }

            if (SymbolEqualityComparer.Default.Equals(named, types.ValueTask))
            {
                kind = JSInvokableReturnKind.ValueTask;
                return true;
            }

            if (named.IsGenericType)
            {
                var definition = named.ConstructedFrom;
                if (SymbolEqualityComparer.Default.Equals(definition, types.TaskOfT) ||
                    SymbolEqualityComparer.Default.Equals(definition, types.ValueTaskOfT))
                {
                    var inner = named.TypeArguments[0];
                    if (!TypeAccessibility.IsNameable(inner, generatedIn))
                    {
                        return false;
                    }

                    kind = SymbolEqualityComparer.Default.Equals(definition, types.TaskOfT)
                        ? JSInvokableReturnKind.TaskOfValue
                        : JSInvokableReturnKind.ValueTaskOfValue;
                    valueTypeName = inner.FullName();
                    return true;
                }
            }
        }

        if (!TypeAccessibility.IsNameable(returnType, generatedIn))
        {
            return false;
        }

        kind = JSInvokableReturnKind.Value;
        valueTypeName = returnType.FullName();
        return true;
    }
}
