// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Mvc.Core;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// Extensions methods for <see cref="IModelMetadataProvider"/>.
/// </summary>
public static class ModelMetadataProviderExtensions
{
    /// <summary>
    /// Gets a <see cref="ModelMetadata"/> for property identified by the provided
    /// <paramref name="containerType"/> and <paramref name="propertyName"/>.
    /// </summary>
    /// <param name="provider">The <see cref="ModelMetadata"/>.</param>
    /// <param name="containerType">The <see cref="Type"/> for which the property is defined.</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns>A <see cref="ModelMetadata"/> for the property.</returns>
    public static ModelMetadata GetMetadataForProperty(
        this IModelMetadataProvider provider,
        Type containerType,
        string propertyName)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(containerType);
        ArgumentNullException.ThrowIfNull(propertyName);

        var containerMetadata = provider.GetMetadataForType(containerType);

        var propertyMetadata = containerMetadata.Properties[propertyName];
        if (propertyMetadata == null)
        {
            var message = Resources.FormatCommon_PropertyNotFound(containerType, propertyName);
            throw new ArgumentException(message, nameof(propertyName));
        }

        return propertyMetadata;
    }
}
