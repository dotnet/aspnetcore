// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Extensions methods for <see cref="IModelMetadataProvider"/>.
    /// </summary>
    public static class ModelMetadataProviderExtensions
    {
        /// <summary>
        /// Gets a <see cref="ModelExplorer"/> for the provided <paramref name="modelType"/> and
        /// <paramref name="model"/>.
        /// </summary>
        /// <param name="provider">The <see cref="IModelMetadataProvider"/>.</param>
        /// <param name="modelType">The declared <see cref="Type"/> of the model object.</param>
        /// <param name="model">The model object.</param>
        /// <returns></returns>
        public static ModelExplorer GetModelExplorerForType(
            [NotNull] this IModelMetadataProvider provider,
            [NotNull] Type modelType,
            object model)
        {
            var modelMetadata = provider.GetMetadataForType(modelType);
            return new ModelExplorer(provider, modelMetadata, model);
        }

        /// <summary>
        /// Gets a <see cref="ModelMetadata"/> for property identified by the provided
        /// <paramref name="containerType"/> and <paramref name="propertyName"/>.
        /// </summary>
        /// <param name="provider">The <see cref="ModelMetadata"/>.</param>
        /// <param name="containerType">The <see cref="Type"/> for which the property is defined.</param>
        /// <param name="propertyName">The property name.</param>
        /// <returns>A <see cref="ModelMetadata"/> for the property.</returns>
        public static ModelMetadata GetMetadataForProperty(
            [NotNull] this IModelMetadataProvider provider,
            [NotNull] Type containerType,
            [NotNull] string propertyName)
        {
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