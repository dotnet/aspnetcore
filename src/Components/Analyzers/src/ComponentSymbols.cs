// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Analyzers;

internal sealed class ComponentSymbols
{
    public static bool TryCreate(Compilation compilation, out ComponentSymbols symbols)
    {
        if (compilation == null)
        {
            throw new ArgumentNullException(nameof(compilation));
        }

        var parameterAttribute = compilation.GetTypeByMetadataName(ComponentsApi.ParameterAttribute.MetadataName);
        if (parameterAttribute == null)
        {
            symbols = null;
            return false;
        }

        var cascadingParameterAttribute = compilation.GetTypeByMetadataName(ComponentsApi.CascadingParameterAttribute.MetadataName);
        if (cascadingParameterAttribute == null)
        {
            symbols = null;
            return false;
        }

        var icomponentType = compilation.GetTypeByMetadataName(ComponentsApi.IComponent.MetadataName);
        if (icomponentType == null)
        {
            symbols = null;
            return false;
        }

        var dictionary = compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2");
        var @string = compilation.GetSpecialType(SpecialType.System_String);
        var @object = compilation.GetSpecialType(SpecialType.System_Object);
        if (dictionary == null || @string == null || @object == null)
        {
            symbols = null;
            return false;
        }

        var parameterCaptureUnmatchedValuesRuntimeType = dictionary.Construct(@string, @object);

        // Try to get optional symbols for SupplyParameterFromForm and PersistentState analyzers
        var supplyParameterFromFormAttribute = compilation.GetTypeByMetadataName(ComponentsApi.SupplyParameterFromFormAttribute.MetadataName);
        var persistentStateAttribute = compilation.GetTypeByMetadataName(ComponentsApi.PersistentStateAttribute.MetadataName);
        var componentBaseType = compilation.GetTypeByMetadataName(ComponentsApi.ComponentBase.MetadataName);

        symbols = new ComponentSymbols(
            parameterAttribute,
            cascadingParameterAttribute,
            supplyParameterFromFormAttribute,
            persistentStateAttribute,
            componentBaseType,
            parameterCaptureUnmatchedValuesRuntimeType,
            icomponentType);
        return true;
    }

    private ComponentSymbols(
        INamedTypeSymbol parameterAttribute,
        INamedTypeSymbol cascadingParameterAttribute,
        INamedTypeSymbol supplyParameterFromFormAttribute,
        INamedTypeSymbol persistentStateAttribute,
        INamedTypeSymbol componentBaseType,
        INamedTypeSymbol parameterCaptureUnmatchedValuesRuntimeType,
        INamedTypeSymbol icomponentType)
    {
        ParameterAttribute = parameterAttribute;
        CascadingParameterAttribute = cascadingParameterAttribute;
        SupplyParameterFromFormAttribute = supplyParameterFromFormAttribute; // Can be null
        PersistentStateAttribute = persistentStateAttribute; // Can be null
        ComponentBaseType = componentBaseType; // Can be null
        ParameterCaptureUnmatchedValuesRuntimeType = parameterCaptureUnmatchedValuesRuntimeType;
        IComponentType = icomponentType;
    }

    public INamedTypeSymbol ParameterAttribute { get; }

    // Dictionary<string, object>
    public INamedTypeSymbol ParameterCaptureUnmatchedValuesRuntimeType { get; }

    public INamedTypeSymbol CascadingParameterAttribute { get; }

    public INamedTypeSymbol SupplyParameterFromFormAttribute { get; } // Can be null if not available

    public INamedTypeSymbol PersistentStateAttribute { get; } // Can be null if not available

    public INamedTypeSymbol ComponentBaseType { get; } // Can be null if not available

    public INamedTypeSymbol IComponentType { get; }
}
