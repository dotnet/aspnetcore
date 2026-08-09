// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Components.Endpoints.Generators.Models;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Endpoints.Generators;

public sealed partial class RazorComponentsMetadataGenerator
{
    private static ImmutableArray<DescribedComponentModel> CollectComponents(
        Compilation compilation,
        WellKnownTypes types,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics,
        CancellationToken cancellationToken)
    {
        var generatedIn = compilation.Assembly;
        var builder = ImmutableArray.CreateBuilder<DescribedComponentModel>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var type in SymbolHelpers.EnumerateApplicationTypes(compilation, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (SymbolHelpers.IsFrameworkAssembly(type.ContainingAssembly))
            {
                continue;
            }

            if (!IsComponentCandidate(type, types))
            {
                continue;
            }

            if (!TypeAccessibility.IsNameable(type, generatedIn))
            {
                // Not describable and not diagnosable in a useful way: an inaccessible component is
                // usually a framework or generated helper the application never renders directly.
                continue;
            }

            if (!TryDescribeComponent(type, types, generatedIn, diagnostics, out var model, out var reason))
            {
                if (!string.IsNullOrEmpty(reason))
                {
                    diagnostics.Add(new DiagnosticInfo(
                        DiagnosticDescriptors.ComponentNotFullyDescribed.Id,
                        type.FullName(),
                        reason));
                }

                continue;
            }

            if (seen.Add(model.TypeFullyQualifiedName))
            {
                builder.Add(model);
            }
        }

        builder.Sort(static (left, right) =>
            string.CompareOrdinal(left.TypeFullyQualifiedName, right.TypeFullyQualifiedName));

        return builder.ToImmutable();
    }

    private static bool IsComponentCandidate(INamedTypeSymbol type, WellKnownTypes types)
    {
        if (type.TypeKind is not TypeKind.Class || type.IsAbstract || type.IsStatic ||
            type.IsGenericType || type.IsImplicitlyDeclared)
        {
            return false;
        }

        return type.AllInterfaces.Contains(types.ComponentInterface!, SymbolEqualityComparer.Default);
    }

    // A component is described completely or not at all: if any member the framework would bind cannot
    // be reached from generated code, no descriptor is produced and the runtime reflects over the type
    // exactly as it does today. A partial descriptor would be worse than none, because the framework
    // trusts a descriptor's member lists and would silently skip whatever the generator dropped.
    private static bool TryDescribeComponent(
        INamedTypeSymbol type,
        WellKnownTypes types,
        IAssemblySymbol generatedIn,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics,
        out DescribedComponentModel model,
        out string reason)
    {
        model = null!;
        reason = string.Empty;

        var canConstruct = SymbolHelpers.HasPublicParameterlessConstructor(type);

        var parameters = ImmutableArray.CreateBuilder<ComponentParameterModel>();
        var injectables = ImmutableArray.CreateBuilder<ComponentInjectableModel>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (var current = type; current is not null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                if (member is not IPropertySymbol property || property.IsStatic || property.IsIndexer)
                {
                    continue;
                }

                // Most-derived declaration wins, matching how the reflection binder walks the chain.
                if (!seen.Add(property.Name))
                {
                    continue;
                }

                var injectAttribute = SymbolHelpers.FindAttribute(property, types.InjectAttribute);
                if (injectAttribute is not null)
                {
                    if (!TryDescribeInjectable(property, injectAttribute, generatedIn, out var injectable, out reason))
                    {
                        return false;
                    }

                    injectables.Add(injectable);
                    continue;
                }

                // A cascading attribute wins over [Parameter] when a property carries both, because it is
                // what gives the property its role: [Parameter] alongside [SupplyParameterFromQuery] only
                // means the property may also be set directly, which the framework infers from the
                // cascading attribute's own type.
                var parameterAttribute = SymbolHelpers.FindAttributeDerivedFrom(property, types.CascadingParameterAttributeBase)
                    ?? SymbolHelpers.FindAttribute(property, types.ParameterAttribute);
                if (parameterAttribute is null)
                {
                    continue;
                }

                if (!TryDescribeParameter(property, parameterAttribute, types, generatedIn, out var parameter, out reason))
                {
                    return false;
                }

                parameters.Add(parameter);
            }
        }

        if (!TryCollectComponentMetadata(type, types, generatedIn, diagnostics, out var metadata))
        {
            return false;
        }

        model = new DescribedComponentModel(
            type.FullName(),
            canConstruct,
            parameters.ToImmutable(),
            injectables.ToImmutable(),
            metadata);

        return true;
    }

    private static bool TryDescribeParameter(
        IPropertySymbol property,
        AttributeData attribute,
        WellKnownTypes types,
        IAssemblySymbol generatedIn,
        out ComponentParameterModel model,
        out string reason)
    {
        model = null!;

        if (!TypeAccessibility.IsNameable(property.Type, generatedIn))
        {
            reason = $"the type of parameter '{property.Name}' is not accessible from the application";
            return false;
        }

        if (property.GetMethod is null || property.SetMethod is null)
        {
            reason = $"parameter '{property.Name}' is missing a getter or a setter";
            return false;
        }

        if (!AttributeExpressionWriter.TryWrite(attribute, generatedIn, out var attributeExpression))
        {
            reason = $"the attribute on parameter '{property.Name}' cannot be reconstructed";
            return false;
        }

        reason = string.Empty;
        model = new ComponentParameterModel(
            property.Name,
            property.ContainingType.FullName(),
            property.Type.FullName(),
            property.Type.AnnotatedFullName(),
            attributeExpression,
            SymbolHelpers.FindAttribute(property, types.PersistentStateAttribute) is not null,
            HasDynamicallyAccessedMembersAttribute(property),
            RequiresGetAccessor: !TypeAccessibility.IsDirectlyAccessible(property.GetMethod, generatedIn),
            RequiresSetAccessor: !TypeAccessibility.IsDirectlyAccessible(property.SetMethod, generatedIn));
        return true;
    }

    private static bool HasDynamicallyAccessedMembersAttribute(IPropertySymbol property)
        => property.GetAttributes().Any(static attribute =>
            string.Equals(
                attribute.AttributeClass?.ToDisplayString(),
                "System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute",
                StringComparison.Ordinal));

    private static bool TryDescribeInjectable(
        IPropertySymbol property,
        AttributeData attribute,
        IAssemblySymbol generatedIn,
        out ComponentInjectableModel model,
        out string reason)
    {
        model = null!;

        if (!TypeAccessibility.IsNameable(property.Type, generatedIn))
        {
            reason = $"the service type of '{property.Name}' is not accessible from the application";
            return false;
        }

        if (property.SetMethod is null)
        {
            reason = $"injected property '{property.Name}' has no setter";
            return false;
        }

        if (!AttributeExpressionWriter.TryWrite(attribute, generatedIn, out var attributeExpression))
        {
            reason = $"the [Inject] attribute on '{property.Name}' cannot be reconstructed";
            return false;
        }

        reason = string.Empty;
        model = new ComponentInjectableModel(
            property.Name,
            property.ContainingType.FullName(),
            property.Type.FullName(),
            attributeExpression,
            RequiresSetAccessor: !TypeAccessibility.IsDirectlyAccessible(property.SetMethod, generatedIn));
        return true;
    }

    // Endpoint metadata is intentionally open-ended. Preserve every reconstructable attribute so
    // authorization, caching, routing, and future endpoint conventions observe reflection parity.
    private static bool TryCollectComponentMetadata(
        INamedTypeSymbol type,
        WellKnownTypes types,
        IAssemblySymbol generatedIn,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics,
        out ImmutableArray<string> metadata)
    {
        ImmutableArray<string>.Builder? builder = null;
        var seenNonMultipleAttributeTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        for (var current = type; current is not null; current = current.BaseType)
        {
            foreach (var attribute in current.GetAttributes())
            {
                var attributeClass = attribute.AttributeClass;
                if (attributeClass is null)
                {
                    continue;
                }

                var (allowMultiple, inherited) = GetAttributeUsage(attributeClass, types);
                if ((!SymbolEqualityComparer.Default.Equals(current, type) && !inherited) ||
                    (!allowMultiple && !seenNonMultipleAttributeTypes.Add(attributeClass)))
                {
                    continue;
                }

                if (!AttributeExpressionWriter.TryWrite(attribute, generatedIn, out var expression))
                {
                    diagnostics.Add(new DiagnosticInfo(
                        DiagnosticDescriptors.ComponentAttributeNotDescribed.Id,
                        type.FullName(),
                        attributeClass.Name));
                    metadata = [];
                    return false;
                }

                builder ??= ImmutableArray.CreateBuilder<string>();
                builder.Add(expression);
            }
        }

        metadata = builder is null ? ImmutableArray<string>.Empty : builder.ToImmutable();
        return true;
    }

    private static (bool AllowMultiple, bool Inherited) GetAttributeUsage(
        INamedTypeSymbol attributeType,
        WellKnownTypes types)
    {
        for (var current = attributeType; current is not null; current = current.BaseType)
        {
            var usage = SymbolHelpers.FindAttribute(current, types.AttributeUsageAttribute);
            if (usage is null)
            {
                continue;
            }

            var allowMultiple = false;
            var inherited = true;
            foreach (var argument in usage.NamedArguments)
            {
                switch (argument.Key)
                {
                    case nameof(AttributeUsageAttribute.AllowMultiple):
                        allowMultiple = (bool)argument.Value.Value!;
                        break;
                    case nameof(AttributeUsageAttribute.Inherited):
                        inherited = (bool)argument.Value.Value!;
                        break;
                }
            }

            return (allowMultiple, inherited);
        }

        return (AllowMultiple: false, Inherited: true);
    }
}
