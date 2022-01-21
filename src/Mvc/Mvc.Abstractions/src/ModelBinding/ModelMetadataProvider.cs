// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// A provider that can supply instances of <see cref="ModelMetadata"/>.
/// </summary>
public abstract class ModelMetadataProvider : IModelMetadataProvider
{
    /// <summary>
    /// Supplies metadata describing the properties of a <see cref="Type"/>.
    /// </summary>
    /// <param name="modelType">The <see cref="Type"/>.</param>
    /// <returns>A set of <see cref="ModelMetadata"/> instances describing properties of the <see cref="Type"/>.</returns>
    public abstract IEnumerable<ModelMetadata> GetMetadataForProperties(Type modelType);

    /// <summary>
    /// Supplies metadata describing a <see cref="Type"/>.
    /// </summary>
    /// <param name="modelType">The <see cref="Type"/>.</param>
    /// <returns>A <see cref="ModelMetadata"/> instance describing the <see cref="Type"/>.</returns>
    public abstract ModelMetadata GetMetadataForType(Type modelType);

    /// <summary>
    /// Supplies metadata describing a parameter.
    /// </summary>
    /// <param name="parameter">The <see cref="ParameterInfo"/>.</param>
    /// <returns>A <see cref="ModelMetadata"/> instance describing the <paramref name="parameter"/>.</returns>
    public abstract ModelMetadata GetMetadataForParameter(ParameterInfo parameter);

    /// <summary>
    /// Supplies metadata describing a parameter.
    /// </summary>
    /// <param name="parameter">The <see cref="ParameterInfo"/></param>
    /// <param name="modelType">The actual model type.</param>
    /// <returns>A <see cref="ModelMetadata"/> instance describing the <paramref name="parameter"/>.</returns>
    public virtual ModelMetadata GetMetadataForParameter(ParameterInfo parameter, Type modelType)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Supplies metadata describing a property.
    /// </summary>
    /// <param name="propertyInfo">The <see cref="PropertyInfo"/>.</param>
    /// <param name="modelType">The actual model type.</param>
    /// <returns>A <see cref="ModelMetadata"/> instance describing the <paramref name="propertyInfo"/>.</returns>
    public virtual ModelMetadata GetMetadataForProperty(PropertyInfo propertyInfo, Type modelType)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Supplies metadata describing a constructor.
    /// </summary>
    /// <param name="constructor">The <see cref="ConstructorInfo"/>.</param>
    /// <param name="modelType">The type declaring the constructor.</param>
    /// <returns>A <see cref="ModelMetadata"/> instance describing the <paramref name="constructor"/>.</returns>
    public virtual ModelMetadata GetMetadataForConstructor(ConstructorInfo constructor, Type modelType)
    {
        throw new NotSupportedException();
    }
}
