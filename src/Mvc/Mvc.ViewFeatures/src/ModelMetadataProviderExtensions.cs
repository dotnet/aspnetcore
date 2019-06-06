// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
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
        /// <returns>
        /// A <see cref="ModelExplorer"/> for the <paramref name="modelType"/> and <paramref name="model"/>.
        /// </returns>
        public static ModelExplorer GetModelExplorerForType(
            this IModelMetadataProvider provider,
            Type modelType,
            object model)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (modelType == null)
            {
                throw new ArgumentNullException(nameof(modelType));
            }

            var modelMetadata = provider.GetMetadataForType(modelType);
            return new ModelExplorer(provider, modelMetadata, model);
        }
    }
}