// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Components.Endpoints.Generators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Components.Endpoints.Generators;

public sealed partial class RazorComponentsMetadataGenerator
{
    private static ImmutableArray<BuiltInDescriptorFactoryModel> CollectImplicitBuiltInDescriptorFactories(
        Compilation compilation,
        WellKnownTypes types,
        CancellationToken cancellationToken)
    {
        var factories = ImmutableArray.CreateBuilder<BuiltInDescriptorFactoryModel>();
        foreach (var type in SymbolHelpers.EnumerateApplicationTypes(compilation, cancellationToken))
        {
            if (TypeAccessibility.IsNameable(type, compilation.Assembly) &&
                IsComponentCandidate(type, types, allowConstructedGeneric: false) &&
                TryCreateBuiltInDescriptorFactory(type, out var factory))
            {
                factories.Add(factory);
            }
        }

        return factories.ToImmutable();
    }

    private static (
        ImmutableArray<DescribedComponentModel> Components,
        ImmutableArray<BuiltInDescriptorFactoryModel> Factories) CollectExplicitComponents(
        INamedTypeSymbol contextType,
        Compilation compilation,
        WellKnownTypes types,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics,
        CancellationToken cancellationToken)
    {
        var builder = ImmutableArray.CreateBuilder<DescribedComponentModel>();
        var factories = ImmutableArray.CreateBuilder<BuiltInDescriptorFactoryModel>();
        var generatedIn = contextType.ContainingAssembly;

        foreach (var syntaxReference in contextType.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax(cancellationToken) is not ClassDeclarationSyntax declaration)
            {
                continue;
            }

            var semanticModel = compilation.GetSemanticModel(declaration.SyntaxTree);
            foreach (var attributeList in declaration.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    var attributeName = attribute.Name.ToString();
                    if (attributeName is not ("ComponentTypeInfo" or "ComponentTypeInfoAttribute") &&
                        !attributeName.EndsWith(".ComponentTypeInfo", StringComparison.Ordinal) &&
                        !attributeName.EndsWith(".ComponentTypeInfoAttribute", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression is not TypeOfExpressionSyntax typeOfExpression ||
                        semanticModel.GetTypeInfo(typeOfExpression.Type, cancellationToken).Type is not INamedTypeSymbol componentType ||
                        !IsComponentCandidate(componentType, types, allowConstructedGeneric: true))
                    {
                        continue;
                    }

                    var hasBuiltInFactory = TryCreateBuiltInDescriptorFactory(componentType, out var factory);
                    if (hasBuiltInFactory)
                    {
                        factories.Add(factory);
                    }

                    if (!TryDescribeComponent(
                            componentType,
                            types,
                            generatedIn,
                            diagnostics,
                            publicMembersOnly: hasBuiltInFactory ||
                                IsBuiltInDescriptorAssembly(componentType.ContainingAssembly.Identity.Name),
                            out var model,
                            out var reason))
                    {
                        if (!hasBuiltInFactory)
                        {
                            diagnostics.Add(new DiagnosticInfo(
                                DiagnosticDescriptors.ComponentNotFullyDescribed.Id,
                                componentType.FullName(),
                                reason));
                        }

                        continue;
                    }

                    builder.Add(model);
                }
            }
        }

        return (builder.ToImmutable(), factories.ToImmutable());
    }

    private static bool TryCreateBuiltInDescriptorFactory(
        INamedTypeSymbol componentType,
        out BuiltInDescriptorFactoryModel factory)
    {
        var definition = componentType.OriginalDefinition;
        var methodName = GetBuiltInDescriptorFactoryMethod(definition);
        if (methodName is not null)
        {
            factory = CreateBuiltInDescriptorFactoryModel(componentType, methodName);
            return true;
        }

        for (var current = componentType.BaseType; current is not null; current = current.BaseType)
        {
            if (string.Equals(
                    current.ContainingAssembly.Identity.Name,
                    WellKnownTypes.ComponentsAssemblyName,
                    StringComparison.Ordinal) &&
                current.MetadataName is "OwningComponentBase" or "OwningComponentBase`1")
            {
                factory = new BuiltInDescriptorFactoryModel(
                    WellKnownTypes.ComponentsAssemblyName,
                    "CreateOwningComponentBaseDescriptors",
                    [componentType.FullName()],
                    [],
                    [-1]);
                return true;
            }

            if (IsTypeDefinition(
                    current,
                    "Microsoft.AspNetCore.Components.Web",
                    "Microsoft.AspNetCore.Components.Forms",
                    "InputBase`1") &&
                !string.Equals(
                    componentType.ContainingAssembly.Identity.Name,
                    "Microsoft.AspNetCore.Components.Web",
                    StringComparison.Ordinal))
            {
                factory = new BuiltInDescriptorFactoryModel(
                    "Microsoft.AspNetCore.Components.Web",
                    "CreateInputBaseDescriptors",
                    [componentType.FullName(), current.TypeArguments[0].AnnotatedFullName()],
                    [$"where T0 : global::Microsoft.AspNetCore.Components.Forms.InputBase<T1>"],
                    [-1, 0]);
                return true;
            }

            if (IsTypeDefinition(
                    current,
                    "Microsoft.AspNetCore.Components.Web",
                    "Microsoft.AspNetCore.Components.Forms",
                    "Editor`1") &&
                !string.Equals(
                    componentType.ContainingAssembly.Identity.Name,
                    "Microsoft.AspNetCore.Components.Web",
                    StringComparison.Ordinal))
            {
                factory = new BuiltInDescriptorFactoryModel(
                    "Microsoft.AspNetCore.Components.Web",
                    "CreateEditorDescriptors",
                    [componentType.FullName(), current.TypeArguments[0].AnnotatedFullName()],
                    [$"where T0 : global::Microsoft.AspNetCore.Components.Forms.Editor<T1>"],
                    [-1, 0]);
                return true;
            }

            if (IsTypeDefinition(
                    current,
                    "Microsoft.AspNetCore.Components.QuickGrid",
                    "Microsoft.AspNetCore.Components.QuickGrid",
                    "ColumnBase`1"))
            {
                factory = new BuiltInDescriptorFactoryModel(
                    "Microsoft.AspNetCore.Components.QuickGrid",
                    "CreateColumnBaseDescriptors",
                    [current.TypeArguments[0].AnnotatedFullName(), componentType.FullName()],
                    [$"where T1 : global::Microsoft.AspNetCore.Components.QuickGrid.ColumnBase<T0>"],
                    [0, -1]);
                return true;
            }
        }

        factory = null!;
        return false;
    }

    private static string? GetBuiltInDescriptorFactoryMethod(INamedTypeSymbol definition)
    {
        if (string.Equals(
                definition.ContainingAssembly.Identity.Name,
                "Microsoft.AspNetCore.Components.Web",
                StringComparison.Ordinal))
        {
            if (string.Equals(
                    definition.ContainingNamespace.ToDisplayString(),
                    "Microsoft.AspNetCore.Components.Forms",
                    StringComparison.Ordinal))
            {
                return definition.MetadataName switch
                {
                    "InputDate`1" => "CreateInputDateDescriptors",
                    "InputNumber`1" => "CreateInputNumberDescriptors",
                    "InputRadio`1" => "CreateInputRadioDescriptors",
                    "InputRadioGroup`1" => "CreateInputRadioGroupDescriptors",
                    "InputSelect`1" => "CreateInputSelectDescriptors",
                    "Label`1" => "CreateLabelDescriptors",
                    "ValidationMessage`1" => "CreateValidationMessageDescriptors",
                    _ => null,
                };
            }

            if (string.Equals(
                    definition.ContainingNamespace.ToDisplayString(),
                    "Microsoft.AspNetCore.Components.Web.Virtualization",
                    StringComparison.Ordinal) &&
                string.Equals(definition.MetadataName, "Virtualize`1", StringComparison.Ordinal))
            {
                return "CreateVirtualizeDescriptors";
            }
        }

        if (string.Equals(
                definition.ContainingAssembly.Identity.Name,
                "Microsoft.AspNetCore.Components.QuickGrid",
                StringComparison.Ordinal))
        {
            return definition.MetadataName switch
            {
                "QuickGrid`1" => "CreateQuickGridDescriptors",
                "PropertyColumn`2" => "CreatePropertyColumnDescriptors",
                "TemplateColumn`1" => "CreateTemplateColumnDescriptors",
                "ColumnsCollectedNotifier`1" => "CreateColumnsCollectedNotifierDescriptors",
                _ => null,
            };
        }

        if (IsTypeDefinition(
                definition,
                "Microsoft.AspNetCore.Components.WebAssembly.Authentication",
                "Microsoft.AspNetCore.Components.WebAssembly.Authentication",
                "RemoteAuthenticatorViewCore`1"))
        {
            return "CreateRemoteAuthenticatorViewCoreDescriptors";
        }

        return null;
    }

    private static bool IsTypeDefinition(
        INamedTypeSymbol type,
        string assemblyName,
        string namespaceName,
        string metadataName)
    {
        var definition = type.OriginalDefinition;
        return string.Equals(definition.ContainingAssembly.Identity.Name, assemblyName, StringComparison.Ordinal) &&
               string.Equals(definition.ContainingNamespace.ToDisplayString(), namespaceName, StringComparison.Ordinal) &&
               string.Equals(definition.MetadataName, metadataName, StringComparison.Ordinal);
    }

    private static BuiltInDescriptorFactoryModel CreateBuiltInDescriptorFactoryModel(
        INamedTypeSymbol componentType,
        string methodName)
    {
        var definition = componentType.OriginalDefinition;
        var constraints = GetConstraintClauses(definition);
        for (var i = 0; i < definition.TypeParameters.Length; i++)
        {
            constraints = constraints
                .Select(clause => clause.Replace(
                    $"where {definition.TypeParameters[i].Name} ",
                    $"where T{i} "))
                .ToImmutableArray();
        }

        return new BuiltInDescriptorFactoryModel(
            definition.ContainingAssembly.Identity.Name,
            methodName,
            [.. componentType.TypeArguments.Select(static argument => argument.AnnotatedFullName())],
            constraints,
            [.. definition.TypeParameters.Select(GetDynamicallyAccessedMemberValue)]);
    }

    private static int GetDynamicallyAccessedMemberValue(ITypeParameterSymbol parameter)
    {
        foreach (var attribute in parameter.GetAttributes())
        {
            if (string.Equals(
                    attribute.AttributeClass?.ToDisplayString(),
                    "System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute",
                    StringComparison.Ordinal) &&
                attribute.ConstructorArguments.Length == 1 &&
                attribute.ConstructorArguments[0].Value is int value)
            {
                return value;
            }
        }

        return 0;
    }

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

            if (!IsComponentCandidate(type, types, allowConstructedGeneric: false))
            {
                continue;
            }

            if (!TypeAccessibility.IsNameable(type, generatedIn))
            {
                // Not describable and not diagnosable in a useful way: an inaccessible component is
                // usually a framework or generated helper the application never renders directly.
                continue;
            }

            var hasBuiltInFactory = TryCreateBuiltInDescriptorFactory(type, out _);
            if (!TryDescribeComponent(
                    type,
                    types,
                    generatedIn,
                    diagnostics,
                    publicMembersOnly: hasBuiltInFactory ||
                        IsBuiltInDescriptorAssembly(type.ContainingAssembly.Identity.Name),
                    out var model,
                    out var reason))
            {
                if (!hasBuiltInFactory &&
                    !string.IsNullOrEmpty(reason) &&
                    !SymbolHelpers.IsFrameworkAssembly(type.ContainingAssembly))
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

    private static bool IsComponentCandidate(
        INamedTypeSymbol type,
        WellKnownTypes types,
        bool allowConstructedGeneric)
    {
        if (type.TypeKind is not TypeKind.Class || type.IsAbstract || type.IsStatic ||
            type.IsImplicitlyDeclared ||
            (type.IsGenericType &&
             (!allowConstructedGeneric ||
              type.IsUnboundGenericType ||
              type.TypeArguments.Any(static argument => argument.TypeKind is TypeKind.TypeParameter))))
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
        bool publicMembersOnly,
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

                if (publicMembersOnly &&
                    IsBuiltInDescriptorAssembly(property.ContainingAssembly.Identity.Name) &&
                    (property.DeclaredAccessibility is not Accessibility.Public ||
                     property.GetMethod?.DeclaredAccessibility is not Accessibility.Public ||
                     property.SetMethod?.DeclaredAccessibility is not Accessibility.Public))
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
