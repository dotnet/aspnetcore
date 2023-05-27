// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

/// <summary>
/// A key type which identifies a <see cref="ModelMetadata"/>.
/// </summary>
public readonly struct ModelMetadataIdentity : IEquatable<ModelMetadataIdentity>
{
    private ModelMetadataIdentity(
        Type modelType,
        string? name = null,
        Type? containerType = null,
        object? fieldInfo = null,
        ConstructorInfo? constructorInfo = null)
    {
        ModelType = modelType;
        Name = name;
        ContainerType = containerType;
        FieldInfo = fieldInfo;
        ConstructorInfo = constructorInfo;
    }

    /// <summary>
    /// Creates a <see cref="ModelMetadataIdentity"/> for the provided model <see cref="Type"/>.
    /// </summary>
    /// <param name="modelType">The model <see cref="Type"/>.</param>
    /// <returns>A <see cref="ModelMetadataIdentity"/>.</returns>
    public static ModelMetadataIdentity ForType(Type modelType)
    {
        ArgumentNullException.ThrowIfNull(modelType);

        return new ModelMetadataIdentity(modelType);
    }

    /// <summary>
    /// Creates a <see cref="ModelMetadataIdentity"/> for the provided property.
    /// </summary>
    /// <param name="modelType">The model type.</param>
    /// <param name="name">The name of the property.</param>
    /// <param name="containerType">The container type of the model property.</param>
    /// <returns>A <see cref="ModelMetadataIdentity"/>.</returns>
    [Obsolete("This API is obsolete and may be removed in a future release. Please use the overload that takes a PropertyInfo object.")] // Remove after .NET 6.
    public static ModelMetadataIdentity ForProperty(
        Type modelType,
        string name,
        Type containerType)
    {
        ArgumentNullException.ThrowIfNull(modelType);
        ArgumentNullException.ThrowIfNull(containerType);
        ArgumentException.ThrowIfNullOrEmpty(name);

        return new ModelMetadataIdentity(modelType, name, containerType);
    }

    /// <summary>
    /// Creates a <see cref="ModelMetadataIdentity"/> for the provided property.
    /// </summary>
    /// <param name="modelType">The model type.</param>
    /// <param name="propertyInfo">The property.</param>
    /// <param name="containerType">The container type of the model property.</param>
    /// <returns>A <see cref="ModelMetadataIdentity"/>.</returns>
    public static ModelMetadataIdentity ForProperty(
        PropertyInfo propertyInfo,
        Type modelType,
        Type containerType)
    {
        ArgumentNullException.ThrowIfNull(propertyInfo);
        ArgumentNullException.ThrowIfNull(modelType);
        ArgumentNullException.ThrowIfNull(containerType);

        return new ModelMetadataIdentity(modelType, propertyInfo.Name, containerType, fieldInfo: propertyInfo);
    }

    /// <summary>
    /// Creates a <see cref="ModelMetadataIdentity"/> for the provided parameter.
    /// </summary>
    /// <param name="parameter">The <see cref="ParameterInfo" />.</param>
    /// <returns>A <see cref="ModelMetadataIdentity"/>.</returns>
    public static ModelMetadataIdentity ForParameter(ParameterInfo parameter)
        => ForParameter(parameter, parameter.ParameterType);

    /// <summary>
    /// Creates a <see cref="ModelMetadataIdentity"/> for the provided parameter with the specified
    /// model type.
    /// </summary>
    /// <param name="parameter">The <see cref="ParameterInfo" />.</param>
    /// <param name="modelType">The model type.</param>
    /// <returns>A <see cref="ModelMetadataIdentity"/>.</returns>
    public static ModelMetadataIdentity ForParameter(ParameterInfo parameter, Type modelType)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        ArgumentNullException.ThrowIfNull(modelType);

        return new ModelMetadataIdentity(modelType, parameter.Name, fieldInfo: parameter);
    }

    /// <summary>
    /// Creates a <see cref="ModelMetadataIdentity"/> for the provided parameter with the specified
    /// model type.
    /// </summary>
    /// <param name="constructor">The <see cref="ConstructorInfo" />.</param>
    /// <param name="modelType">The model type.</param>
    /// <returns>A <see cref="ModelMetadataIdentity"/>.</returns>
    public static ModelMetadataIdentity ForConstructor(ConstructorInfo constructor, Type modelType)
    {
        ArgumentNullException.ThrowIfNull(constructor);
        ArgumentNullException.ThrowIfNull(modelType);

        return new ModelMetadataIdentity(modelType, constructor.Name, constructorInfo: constructor);
    }

    /// <summary>
    /// Gets the <see cref="Type"/> defining the model property represented by the current
    /// instance, or <c>null</c> if the current instance does not represent a property.
    /// </summary>
    public Type? ContainerType { get; }

    /// <summary>
    /// Gets the <see cref="Type"/> represented by the current instance.
    /// </summary>
    public Type ModelType { get; }

    /// <summary>
    /// Gets a value indicating the kind of metadata represented by the current instance.
    /// </summary>
    public ModelMetadataKind MetadataKind
    {
        get
        {
            if (ParameterInfo != null)
            {
                return ModelMetadataKind.Parameter;
            }
            else if (ConstructorInfo != null)
            {
                return ModelMetadataKind.Constructor;
            }
            else if (ContainerType != null && Name != null)
            {
                return ModelMetadataKind.Property;
            }
            else
            {
                return ModelMetadataKind.Type;
            }
        }
    }

    /// <summary>
    /// Gets the name of the current instance if it represents a parameter or property, or <c>null</c> if
    /// the current instance represents a type.
    /// </summary>
    public string? Name { get; }

    private object? FieldInfo { get; }

    /// <summary>
    /// Gets a descriptor for the parameter, or <c>null</c> if this instance
    /// does not represent a parameter.
    /// </summary>
    public ParameterInfo? ParameterInfo => FieldInfo as ParameterInfo;

    /// <summary>
    /// Gets a descriptor for the property, or <c>null</c> if this instance
    /// does not represent a property.
    /// </summary>
    public PropertyInfo? PropertyInfo => FieldInfo as PropertyInfo;

    /// <summary>
    /// Gets a descriptor for the constructor, or <c>null</c> if this instance
    /// does not represent a constructor.
    /// </summary>
    public ConstructorInfo? ConstructorInfo { get; }

    /// <inheritdoc />
    public bool Equals(ModelMetadataIdentity other)
    {
        return
            ContainerType == other.ContainerType &&
            ModelType == other.ModelType &&
            Name == other.Name &&
            ParameterInfo == other.ParameterInfo &&
            PropertyInfo == other.PropertyInfo &&
            ConstructorInfo == other.ConstructorInfo;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        var other = obj as ModelMetadataIdentity?;
        return other.HasValue && Equals(other.Value);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ContainerType);
        hash.Add(ModelType);
        hash.Add(Name, StringComparer.Ordinal);
        hash.Add(ParameterInfo);
        hash.Add(PropertyInfo);
        hash.Add(ConstructorInfo);
        return hash.ToHashCode();
    }
}
