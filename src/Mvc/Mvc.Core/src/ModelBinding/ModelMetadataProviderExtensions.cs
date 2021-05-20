// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using Microsoft.AspNetCore.Mvc.Core;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
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
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (containerType == null)
            {
                throw new ArgumentNullException(nameof(containerType));
            }

            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

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
}
