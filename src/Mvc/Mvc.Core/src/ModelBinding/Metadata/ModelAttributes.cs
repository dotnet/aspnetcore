// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// Provides access to the combined list of attributes associated with a <see cref="Type"/>, property, or parameter.
/// </summary>
public class ModelAttributes
{
    internal static readonly ModelAttributes Empty = new ModelAttributes(Array.Empty<object>());

    /// <summary>
    /// Creates a new <see cref="ModelAttributes"/>.
    /// </summary>
    internal ModelAttributes(IReadOnlyList<object> attributes)
    {
        Attributes = attributes;
    }

    /// <summary>
    /// Creates a new <see cref="ModelAttributes"/>.
    /// </summary>
    /// <param name="typeAttributes">
    /// If this instance represents a type, the set of attributes for that type.
    /// If this instance represents a property, the set of attributes for the property's <see cref="Type"/>.
    /// Otherwise, <c>null</c>.
    /// </param>
    /// <param name="propertyAttributes">
    /// If this instance represents a property, the set of attributes for that property.
    /// Otherwise, <c>null</c>.
    /// </param>
    /// <param name="parameterAttributes">
    /// If this instance represents a parameter, the set of attributes for that parameter.
    /// Otherwise, <c>null</c>.
    /// </param>
    internal ModelAttributes(
        IEnumerable<object> typeAttributes,
        IEnumerable<object>? propertyAttributes,
        IEnumerable<object>? parameterAttributes)
    {
        if (propertyAttributes != null)
        {
            // Represents a property
            ArgumentNullException.ThrowIfNull(typeAttributes);

            PropertyAttributes = propertyAttributes.ToArray();
            TypeAttributes = typeAttributes.ToArray();
            Attributes = PropertyAttributes.Concat(TypeAttributes).ToArray();
        }
        else if (parameterAttributes != null)
        {
            // Represents a parameter
            ArgumentNullException.ThrowIfNull(typeAttributes);

            ParameterAttributes = parameterAttributes.ToArray();
            TypeAttributes = typeAttributes.ToArray();
            Attributes = ParameterAttributes.Concat(TypeAttributes).ToArray();
        }
        else if (typeAttributes != null)
        {
            Attributes = TypeAttributes = typeAttributes.ToArray();
        }
        else
        {
            Attributes = Array.Empty<object>();
        }
    }

    /// <summary>
    /// Gets the set of all attributes. If this instance represents the attributes for a property, the attributes
    /// on the property definition are before those on the property's <see cref="Type"/>. If this instance
    /// represents the attributes for a parameter, the attributes on the parameter definition are before those on
    /// the parameter's <see cref="Type"/>.
    /// </summary>
    public IReadOnlyList<object> Attributes { get; }

    /// <summary>
    /// Gets the set of attributes on the property, or <c>null</c> if this instance does not represent the attributes
    /// for a property.
    /// </summary>
    public IReadOnlyList<object>? PropertyAttributes { get; }

    /// <summary>
    /// Gets the set of attributes on the parameter, or <c>null</c> if this instance does not represent the attributes
    /// for a parameter.
    /// </summary>
    public IReadOnlyList<object>? ParameterAttributes { get; }

    /// <summary>
    /// Gets the set of attributes on the <see cref="Type"/>. If this instance represents a property, then
    /// <see cref="TypeAttributes"/> contains attributes retrieved from <see cref="PropertyInfo.PropertyType"/>.
    /// If this instance represents a parameter, then contains attributes retrieved from
    /// <see cref="ParameterInfo.ParameterType"/>.
    /// </summary>
    public IReadOnlyList<object>? TypeAttributes { get; }

    /// <summary>
    /// Gets the attributes for the given <paramref name="property"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> in which caller found <paramref name="property"/>.
    /// </param>
    /// <param name="property">A <see cref="PropertyInfo"/> for which attributes need to be resolved.
    /// </param>
    /// <returns>
    /// A <see cref="ModelAttributes"/> instance with the attributes of the property and its <see cref="Type"/>.
    /// </returns>
    public static ModelAttributes GetAttributesForProperty(Type type, PropertyInfo property)
    {
        return GetAttributesForProperty(type, property, property.PropertyType);
    }

    /// <summary>
    /// Gets the attributes for the given <paramref name="property"/> with the specified <paramref name="modelType"/>.
    /// </summary>
    /// <param name="containerType">The <see cref="Type"/> in which caller found <paramref name="property"/>.
    /// </param>
    /// <param name="property">A <see cref="PropertyInfo"/> for which attributes need to be resolved.
    /// </param>
    /// <param name="modelType">The model type</param>
    /// <returns>
    /// A <see cref="ModelAttributes"/> instance with the attributes of the property and its <see cref="Type"/>.
    /// </returns>
    public static ModelAttributes GetAttributesForProperty(Type containerType, PropertyInfo property, Type modelType)
    {
        ArgumentNullException.ThrowIfNull(containerType);
        ArgumentNullException.ThrowIfNull(property);

        var propertyAttributes = property.GetCustomAttributes();
        var typeAttributes = modelType.GetCustomAttributes();

        var metadataType = GetMetadataType(containerType);
        if (metadataType != null)
        {
            var metadataProperty = metadataType.GetRuntimeProperty(property.Name);
            if (metadataProperty != null)
            {
                propertyAttributes = propertyAttributes.Concat(metadataProperty.GetCustomAttributes());
            }
        }

        return new ModelAttributes(typeAttributes, propertyAttributes, parameterAttributes: null);
    }

    /// <summary>
    /// Gets the attributes for the given <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> for which attributes need to be resolved.
    /// </param>
    /// <returns>A <see cref="ModelAttributes"/> instance with the attributes of the <see cref="Type"/>.</returns>
    public static ModelAttributes GetAttributesForType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var attributes = type.GetCustomAttributes();

        var metadataType = GetMetadataType(type);
        if (metadataType != null)
        {
            attributes = attributes.Concat(metadataType.GetCustomAttributes());
        }

        return new ModelAttributes(attributes, propertyAttributes: null, parameterAttributes: null);
    }

    /// <summary>
    /// Gets the attributes for the given <paramref name="parameterInfo"/>.
    /// </summary>
    /// <param name="parameterInfo">
    /// The <see cref="ParameterInfo"/> for which attributes need to be resolved.
    /// </param>
    /// <returns>
    /// A <see cref="ModelAttributes"/> instance with the attributes of the parameter and its <see cref="Type"/>.
    /// </returns>
    public static ModelAttributes GetAttributesForParameter(ParameterInfo parameterInfo)
    {
        // Prior versions called IModelMetadataProvider.GetMetadataForType(...) and therefore
        // GetAttributesForType(...) for parameters. Maintain that set of attributes (including those from an
        // ModelMetadataTypeAttribute reference) for back-compatibility.
        var typeAttributes = GetAttributesForType(parameterInfo.ParameterType).TypeAttributes!;
        var parameterAttributes = parameterInfo.GetCustomAttributes();

        return new ModelAttributes(typeAttributes, propertyAttributes: null, parameterAttributes);
    }

    /// <summary>
    /// Gets the attributes for the given <paramref name="parameterInfo"/> with the specified <paramref name="modelType"/>.
    /// </summary>
    /// <param name="parameterInfo">
    /// The <see cref="ParameterInfo"/> for which attributes need to be resolved.
    /// </param>
    /// <param name="modelType">The model type.</param>
    /// <returns>
    /// A <see cref="ModelAttributes"/> instance with the attributes of the parameter and its <see cref="Type"/>.
    /// </returns>
    public static ModelAttributes GetAttributesForParameter(ParameterInfo parameterInfo, Type modelType)
    {
        ArgumentNullException.ThrowIfNull(parameterInfo);
        ArgumentNullException.ThrowIfNull(modelType);

        // Prior versions called IModelMetadataProvider.GetMetadataForType(...) and therefore
        // GetAttributesForType(...) for parameters. Maintain that set of attributes (including those from an
        // ModelMetadataTypeAttribute reference) for back-compatibility.
        var typeAttributes = GetAttributesForType(modelType).TypeAttributes!;
        var parameterAttributes = parameterInfo.GetCustomAttributes();

        return new ModelAttributes(typeAttributes, propertyAttributes: null, parameterAttributes);
    }

    private static Type? GetMetadataType(Type type)
    {
        // GetCustomAttribute will examine the members inheritance chain
        // for attributes of a particular type by default. Meaning that
        // in the following scenario, the `ModelMetadataType` attribute on
        // both the derived _and_ base class will be returned.
        // [ModelMetadataType<BaseModel>]
        // private class BaseViewModel { }
        // [ModelMetadataType<DerivedModel>]
        // private class DerivedViewModel : BaseViewModel { }
        // To avoid this, we call `GetCustomAttributes` directly
        // to avoid examining the inheritance hierarchy.
        // See https://source.dot.net/#System.Private.CoreLib/src/System/Attribute.CoreCLR.cs,677
        var modelMetadataTypeAttributes = type.GetCustomAttributes<ModelMetadataTypeAttribute>(inherit: false);
        try
        {
            return modelMetadataTypeAttributes?.SingleOrDefault()?.MetadataType;
        }
        catch (InvalidOperationException e)
        {
            throw new InvalidOperationException("Only one ModelMetadataType attribute is permitted per type.", e);
        }
    }
}
