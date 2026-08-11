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
    // Describes every type named by a [BindableModel] on the context, and every type reachable from it
    // through instance members and single-argument indexers. One attribute per EditForm model therefore
    // covers every expression that form can produce, including `row.Name` inside a loop over a
    // collection property, which is the shape that otherwise forces Expression.Compile.
    private static ImmutableArray<BindableTypeModel> CollectBindableTypes(
        INamedTypeSymbol contextType,
        WellKnownTypes types,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics,
        CancellationToken cancellationToken)
    {
        if (types.BindableModelAttribute is null)
        {
            return ImmutableArray<BindableTypeModel>.Empty;
        }

        var generatedIn = contextType.ContainingAssembly;
        var roots = new List<INamedTypeSymbol>();

        foreach (var attribute in contextType.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, types.BindableModelAttribute))
            {
                continue;
            }

            foreach (var named in attribute.NamedArguments)
            {
                if (string.Equals(named.Key, "ModelType", StringComparison.Ordinal) &&
                    named.Value.Value is INamedTypeSymbol modelType)
                {
                    roots.Add(modelType);
                }
            }
        }

        if (roots.Count == 0)
        {
            return ImmutableArray<BindableTypeModel>.Empty;
        }

        var results = new Dictionary<string, BindableTypeModel>(StringComparer.Ordinal);
        var visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        var queue = new Queue<INamedTypeSymbol>();

        foreach (var root in roots)
        {
            if (!TypeAccessibility.IsNameable(root, generatedIn))
            {
                diagnostics.Add(new DiagnosticInfo(
                    DiagnosticDescriptors.BindableModelNotDescribed.Id,
                    root.FullName(),
                    "it is not accessible from the application"));
                continue;
            }

            queue.Enqueue(root);
        }

        while (queue.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var type = queue.Dequeue();
            if (!visited.Add(type) || IsLeafType(type))
            {
                continue;
            }

            var members = ImmutableArray.CreateBuilder<BindableMemberModel>();
            var indexers = ImmutableArray.CreateBuilder<BindableIndexerModel>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            for (var current = type; current is { SpecialType: not SpecialType.System_Object }; current = current.BaseType)
            {
                foreach (var member in current.GetMembers())
                {
                    switch (member)
                    {
                        case IPropertySymbol { IsStatic: false, IsIndexer: true } indexer
                            when indexer.Parameters.Length == 1 && indexer.GetMethod is not null:
                            if (TypeAccessibility.IsDirectlyAccessible(indexer.GetMethod, generatedIn) &&
                                TypeAccessibility.IsNameable(indexer.Parameters[0].Type, generatedIn) &&
                                TypeAccessibility.IsNameable(indexer.Type, generatedIn))
                            {
                                indexers.Add(new BindableIndexerModel(
                                    indexer.Parameters[0].Type.FullName(),
                                    indexer.Type.FullName()));
                                Enqueue(indexer.Type, queue);
                            }

                            break;

                        case IPropertySymbol { IsStatic: false, IsIndexer: false, CanBeReferencedByName: true, GetMethod: not null } property:
                            if (seen.Add(property.Name) && TypeAccessibility.IsNameable(property.Type, generatedIn))
                            {
                                members.Add(new BindableMemberModel(
                                    property.Name,
                                    property.Type.FullName(),
                                    IsField: false,
                                    RequiresGetAccessor: !TypeAccessibility.IsDirectlyAccessible(property.GetMethod, generatedIn)));
                                Enqueue(property.Type, queue);
                            }

                            break;

                        case IFieldSymbol { IsStatic: false, IsImplicitlyDeclared: false, CanBeReferencedByName: true, AssociatedSymbol: null } field:
                            if (seen.Add(field.Name) && TypeAccessibility.IsNameable(field.Type, generatedIn))
                            {
                                members.Add(new BindableMemberModel(
                                    field.Name,
                                    field.Type.FullName(),
                                    IsField: true,
                                    RequiresGetAccessor: !TypeAccessibility.IsDirectlyAccessible(field, generatedIn)));
                                Enqueue(field.Type, queue);
                            }

                            break;
                    }
                }
            }

            var name = type.FullName();
            if (!results.ContainsKey(name))
            {
                results.Add(name, new BindableTypeModel(name, members.ToImmutable(), indexers.ToImmutable()));
            }
        }

        var ordered = ImmutableArray.CreateBuilder<BindableTypeModel>(results.Count);
        foreach (var key in Sorted(results.Keys))
        {
            ordered.Add(results[key]);
        }

        return ordered.ToImmutable();
    }

    private static void Enqueue(ITypeSymbol type, Queue<INamedTypeSymbol> queue)
    {
        // An expression walks into the element type of a collection as often as into a property, so a
        // generic argument is followed as eagerly as a member type.
        if (type is IArrayTypeSymbol array)
        {
            Enqueue(array.ElementType, queue);
            return;
        }

        if (type is not INamedTypeSymbol named)
        {
            return;
        }

        if (!IsLeafType(named))
        {
            queue.Enqueue(named);
        }

        foreach (var argument in named.TypeArguments)
        {
            Enqueue(argument, queue);
        }
    }

    // The walk stops at framework primitives: they are the leaves of every form model, describing them
    // would multiply the generated output, and no binding expression reaches through them.
    private static bool IsLeafType(INamedTypeSymbol type)
    {
        if (type.SpecialType is not SpecialType.None)
        {
            return true;
        }

        if (type.TypeKind is TypeKind.Enum or TypeKind.Delegate or TypeKind.Interface)
        {
            return true;
        }

        var containingAssembly = type.ContainingAssembly;
        return containingAssembly is not null && SymbolHelpers.IsFrameworkAssembly(containingAssembly);
    }

    private static IEnumerable<string> Sorted(IEnumerable<string> values)
    {
        var list = new List<string>(values);
        list.Sort(StringComparer.Ordinal);
        return list;
    }
}
